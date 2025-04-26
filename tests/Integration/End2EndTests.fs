module BAREWire.Tests.Integration.End2EndTests

open Expecto
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Core.Binary
open BAREWire.Core.Utf8
open BAREWire.Core.Uuid
open BAREWire.Encoding.Encoder
open BAREWire.Encoding.Decoder
open BAREWire.Encoding.Codec
open BAREWire.Memory.Region
open BAREWire.Memory.View
open BAREWire.Network.Frame
open BAREWire.Schema
open BAREWire.Schema.DSL

// Sample message types for testing
type TestFieldsRecord = {
    Int32Field: int
    StringField: string
    OptionalField: int option
}

type TestRecordWithNested = {
    PrimitiveField: int
    NestedField: TestFieldsRecord
}

// Define a sample schema
let createTestSchema() =
    schema "TestMessage"
    |> withType "TestFields" (struct' [
        field "Int32Field" i32
        field "StringField" string
        field "OptionalField" (optional i32)
    ])
    |> withType "TestMessage" (struct' [
        field "PrimitiveField" i32
        field "NestedField" (userType "TestFields")
    ])
    |> validate
    |> function
        | Ok schema -> schema
        | Error errors -> 
            let errorMessages = errors |> List.map Validation.errorToString |> String.concat ", "
            failwith $"Schema validation failed: {errorMessages}"

[<Tests>]
let encodingDecodingTests =
    testList "Encoding and Decoding Tests" [
        testCase "Binary Conversion Functions" <| fun _ ->
            // Test float32 <-> int32 conversion
            let original = 123.456f
            let bits = singleToInt32Bits original
            let roundTrip = int32BitsToSingle bits
            Expect.equal roundTrip original "Float32 <-> Int32Bits conversion should be lossless"
            
            // Test float <-> int64 conversion
            let original = 123.456
            let bits = doubleToInt64Bits original
            let roundTrip = int64BitsToDouble bits
            Expect.equal roundTrip original "Double <-> Int64Bits conversion should be lossless"
            
            // Test array conversion functions
            let originalInt32 = 0x12345678
            let bytes = getInt32Bytes originalInt32
            let roundTrip = toInt32 bytes 0
            Expect.equal roundTrip originalInt32 "Int32 <-> Bytes conversion should be lossless"
            
            let originalInt64 = 0x123456789ABCDEF0L
            let bytes = getInt64Bytes originalInt64
            let roundTrip = toInt64 bytes 0
            Expect.equal roundTrip originalInt64 "Int64 <-> Bytes conversion should be lossless"
            
        testCase "UTF-8 String Encoding and Decoding" <| fun _ ->
            let testStrings = [
                "Hello, world!"; // ASCII
                "Привет, мир!"; // Cyrillic
                "你好，世界！"; // Chinese
                "こんにちは世界！"; // Japanese
                "안녕하세요 세계!"; // Korean
                "👋🌍!"; // Emoji
            ]
            
            for original in testStrings do
                let bytes = getBytes original
                let roundTrip = getString bytes
                Expect.equal roundTrip original $"UTF-8 conversion should be lossless: {original}"
                
        testCase "UUID Generation and Formatting" <| fun _ ->
            let uuid = newUuid()
            let uuidStr = toString uuid
            
            Expect.equal uuidStr.Length 36 "UUID string should be 36 characters"
            Expect.isTrue (uuidStr.[8] = '-') "UUID string should have dash at position 8"
            Expect.isTrue (uuidStr.[13] = '-') "UUID string should have dash at position 13"
            Expect.isTrue (uuidStr.[18] = '-') "UUID string should have dash at position 18"
            Expect.isTrue (uuidStr.[23] = '-') "UUID string should have dash at position 23"
            
            // Parse back
            let parsedUuid = fromString uuidStr
            Expect.isTrue (equals uuid parsedUuid) "UUID parsing should be lossless"
            
        testCase "Primitive Type Encoding and Decoding" <| fun _ ->
            // Test buffer creation
            let buffer = Buffer<obj>.Create(100)
            
            // Test int32 write and read
            let originalInt32 = 12345
            writeI32 buffer originalInt32
            
            // Create memory from buffer
            let memory = fromArray<obj, region> buffer.Data
            
            // Read back
            let readValue, _ = readI32 memory 0<offset>
            
            Expect.equal readValue originalInt32 "Int32 encoding/decoding should be lossless"
            
        testCase "String Encoding and Decoding" <| fun _ ->
            // Test buffer creation
            let buffer = Buffer<obj>.Create(100)
            
            // Test string write and read
            let originalString = "Hello, BAREWire!"
            writeString buffer originalString
            
            // Create memory from buffer
            let memory = fromArray<obj, region> buffer.Data
            
            // Read back
            let readValue, _ = readString memory 0<offset>
            
            Expect.equal readValue originalString "String encoding/decoding should be lossless"
            
        testCase "Optional Value Encoding and Decoding" <| fun _ ->
            // Test buffer creation
            let buffer = Buffer<obj>.Create(100)
            
            // Test Some value
            let originalSome = Some 42
            writeOptional buffer originalSome writeI32
            
            // Create memory from buffer
            let memory = fromArray<obj, region> buffer.Data
            
            // Read back
            let readSome, offset = readOptional memory 0<offset> readI32
            
            Expect.equal readSome originalSome "Some value encoding/decoding should be lossless"
            
            // Test None value
            let originalNone = None
            writeOptional buffer originalNone writeI32
            
            // Create memory from another buffer
            let memory2 = fromArray<obj, region> buffer.Data
            
            // Read back
            let readNone, _ = readOptional memory2 offset readI32
            
            Expect.equal readNone originalNone "None value encoding/decoding should be lossless"
    ]
    
[<Tests>]
let memoryRegionViewTests =
    testList "Memory Region and View Tests" [
        testCase "Creating and slicing memory regions" <| fun _ ->
            // Create a memory region
            let data = Array.init 100 byte
            let region = create<int, region> data
            
            // Check size
            Expect.equal (getSize region) 100<bytes> "Region size should match data length"
            
            // Create a slice
            let sliceResult = slice<int, string, region> region 10<offset> 20<bytes>
            
            match sliceResult with
            | Ok slice ->
                Expect.equal (getSize slice) 20<bytes> "Slice size should be 20 bytes"
            | Error e ->
                failwith $"Slice creation failed: {toString e}"
                
        testCase "Memory region copying" <| fun _ ->
            // Create source and destination regions
            let sourceData = Array.init 20 (fun i -> byte i)
            let destData = Array.zeroCreate 20
            
            let source = create<int, region> sourceData
            let dest = create<int, region> destData
            
            // Copy data
            match copy source dest 10<bytes> with
            | Ok () ->
                // Check the first 10 bytes were copied
                for i = 0 to 9 do
                    Expect.equal destData.[i] sourceData.[i] $"Byte {i} should be copied correctly"
                    
                // Check the remaining bytes are still zero
                for i = 10 to 19 do
                    Expect.equal destData.[i] 0uy $"Byte {i} should remain zero"
            | Error e ->
                failwith $"Copy operation failed: {toString e}"
                
        testCase "Memory view field access" <| fun _ ->
            // Create a schema for testing
            let schema = createTestSchema()
            
            // Create a buffer and serialize a test object
            let buffer = Buffer<obj>.Create(1000)
            
            // Write field values directly to test memory view
            writeI32 buffer 42  // PrimitiveField
            writeI32 buffer 123 // Int32Field
            writeString buffer "Test string" // StringField
            writeOptional buffer (Some 456) writeI32 // OptionalField
            
            // Create memory region from buffer
            let memory = fromArray<obj, region> buffer.Data
            
            // Create memory view
            let view = View.create<TestRecordWithNested, region> memory schema
            
            // Read field values
            match View.getField<TestRecordWithNested, int, region> view ["PrimitiveField"] with
            | Ok value ->
                Expect.equal value 42 "Should read primitive field correctly"
            | Error e ->
                failwith $"Failed to read PrimitiveField: {toString e}"
                
            // Read nested field
            match View.getField<TestRecordWithNested, string, region> view ["NestedField"; "StringField"] with
            | Ok value ->
                Expect.equal value "Test string" "Should read nested string field correctly"
            | Error e ->
                failwith $"Failed to read StringField: {toString e}"
                
        testCase "Frame encoding and decoding" <| fun _ ->
            // Create a test frame
            let payload = [| 1uy; 2uy; 3uy; 4uy; 5uy |]
            let frame = createRequest payload
            
            // Encode the frame
            let encoded = encode frame
            
            // Decode the frame
            match decode encoded with
            | Ok decodedFrame ->
                Expect.equal decodedFrame.Header.MessageType frame.Header.MessageType "Message type should match"
                Expect.equal decodedFrame.Header.PayloadLength frame.Header.PayloadLength "Payload length should match"
                Expect.equal decodedFrame.Payload frame.Payload "Payload should match"
            | Error e ->
                failwith $"Frame decoding failed: {e.Message}"
    ]
    
[<Tests>]
let schemaTests =
    testList "Schema Definition and Validation Tests" [
        testCase "Creating and validating a schema" <| fun _ ->
            // Create a schema using DSL
            let schemaResult = 
                schema "TestRoot"
                |> withType "TestRoot" (struct' [
                    field "IntField" i32
                    field "StringField" string
                    field "NestedField" (userType "NestedType")
                ])
                |> withType "NestedType" (struct' [
                    field "BoolField" bool
                    field "OptionalField" (optional i32)
                ])
                |> validate
                
            match schemaResult with
            | Ok validSchema ->
                Expect.equal validSchema.Root "TestRoot" "Root type should be TestRoot"
                Expect.isTrue (Map.containsKey "TestRoot" validSchema.Types) "TestRoot type should exist"
                Expect.isTrue (Map.containsKey "NestedType" validSchema.Types) "NestedType should exist"
            | Error errors ->
                let errorMessages = errors |> List.map Validation.errorToString |> String.concat ", "
                failwith $"Schema validation failed: {errorMessages}"
                
        testCase "Schema validation errors" <| fun _ ->
            // Create an invalid schema with a cycle
            let cyclicSchemaResult = 
                schema "CyclicRoot"
                |> withType "CyclicRoot" (struct' [
                    field "Self" (userType "CyclicRoot")
                ])
                |> validate
                
            match cyclicSchemaResult with
            | Ok _ ->
                failwith "Cyclic schema should not validate"
            | Error errors ->
                Expect.isNonEmpty errors "Should have validation errors"
                let isCyclicError = 
                    errors 
                    |> List.exists (function 
                        | Validation.CyclicTypeReference _ -> true 
                        | _ -> false)
                Expect.isTrue isCyclicError "Should detect cyclic reference"
                
            // Create an invalid schema with undefined type
            let undefinedTypeSchemaResult = 
                schema "UndefinedTypeRoot"
                |> withType "UndefinedTypeRoot" (struct' [
                    field "Missing" (userType "NonExistentType")
                ])
                |> validate
                
            match undefinedTypeSchemaResult with
            | Ok _ ->
                failwith "Schema with undefined type should not validate"
            | Error errors ->
                Expect.isNonEmpty errors "Should have validation errors"
                let isUndefinedTypeError = 
                    errors 
                    |> List.exists (function 
                        | Validation.UndefinedType _ -> true 
                        | _ -> false)
                Expect.isTrue isUndefinedTypeError "Should detect undefined type"
    ]

[<EntryPoint>]
let main args =
    runTestsWithArgs defaultConfig args encodingDecodingTests