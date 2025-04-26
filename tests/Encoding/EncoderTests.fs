module BAREWire.Tests.Encoding.EncoderTests

open Expecto
open BAREWire.Core
open BAREWire.Encoding.Encoder
open System
open System.Text

[<Measure>] type test

/// A simple implementation of Buffer for testing
type TestBuffer() =
    let mutable data = Array.zeroCreate<byte> 1024
    let mutable position = 0
    
    member _.Data = data
    member _.Position = position
    
    interface Buffer<test> with
        member _.Write(value: byte) =
            data.[position] <- value
            position <- position + 1
        
        member _.WriteSpan(span: ReadOnlySpan<byte>) =
            for i in 0 .. span.Length - 1 do
                data.[position + i] <- span.[i]
            position <- position + span.Length
        
        member _.Position = position

let createBuffer() = TestBuffer() :> Buffer<test>

[<Tests>]
let primitiveEncodingTests =
    testList "Primitive Encoding Tests" [
        test "writeUInt encodes zero correctly" {
            let buffer = createBuffer()
            writeUInt buffer 0UL
            
            let result = (buffer :?> TestBuffer).Data
            Expect.equal result.[0] 0uy "ULEB128 encoding of 0 should be [0]"
            Expect.equal (buffer :?> TestBuffer).Position 1 "Position should be incremented by 1"
        }
        
        test "writeUInt encodes small values correctly" {
            let buffer = createBuffer()
            writeUInt buffer 127UL
            
            let result = (buffer :?> TestBuffer).Data
            Expect.equal result.[0] 127uy "ULEB128 encoding of 127 should be [127]"
            Expect.equal (buffer :?> TestBuffer).Position 1 "Position should be incremented by 1"
        }
        
        test "writeUInt encodes large values correctly" {
            let buffer = createBuffer()
            writeUInt buffer 128UL
            
            let result = (buffer :?> TestBuffer).Data
            Expect.equal result.[0] 128uy "First byte of ULEB128 encoding of 128 should be 128"
            Expect.equal result.[1] 1uy "Second byte of ULEB128 encoding of 128 should be 1"
            Expect.equal (buffer :?> TestBuffer).Position 2 "Position should be incremented by 2"
        }
        
        test "writeUInt encodes max uint64 correctly" {
            let buffer = createBuffer()
            writeUInt buffer UInt64.MaxValue
            
            let result = (buffer :?> TestBuffer).Data
            // Check each byte of the encoding
            for i in 0 .. 8 do
                if i < 8 then
                    Expect.equal result.[i] 255uy $"Byte {i} should be 255"
                else
                    Expect.equal result.[i] 1uy $"Byte {i} should be 1"
            
            Expect.equal (buffer :?> TestBuffer).Position 10 "Position should be incremented by 10"
        }
        
        test "writeInt encodes zero correctly" {
            let buffer = createBuffer()
            writeInt buffer 0L
            
            let result = (buffer :?> TestBuffer).Data
            Expect.equal result.[0] 0uy "Zigzag encoding of 0 should be [0]"
            Expect.equal (buffer :?> TestBuffer).Position 1 "Position should be incremented by 1"
        }
        
        test "writeInt encodes positive values correctly" {
            let buffer = createBuffer()
            writeInt buffer 63L
            
            let result = (buffer :?> TestBuffer).Data
            Expect.equal result.[0] 126uy "Zigzag encoding of 63 should be [126]"
            Expect.equal (buffer :?> TestBuffer).Position 1 "Position should be incremented by 1"
        }
        
        test "writeInt encodes negative values correctly" {
            let buffer = createBuffer()
            writeInt buffer -1L
            
            let result = (buffer :?> TestBuffer).Data
            Expect.equal result.[0] 1uy "Zigzag encoding of -1 should be [1]"
            Expect.equal (buffer :?> TestBuffer).Position 1 "Position should be incremented by 1"
        }
        
        test "writeBool encodes true correctly" {
            let buffer = createBuffer()
            writeBool buffer true
            
            let result = (buffer :?> TestBuffer).Data
            Expect.equal result.[0] 1uy "Boolean true should be encoded as 1"
            Expect.equal (buffer :?> TestBuffer).Position 1 "Position should be incremented by 1"
        }
        
        test "writeBool encodes false correctly" {
            let buffer = createBuffer()
            writeBool buffer false
            
            let result = (buffer :?> TestBuffer).Data
            Expect.equal result.[0] 0uy "Boolean false should be encoded as 0"
            Expect.equal (buffer :?> TestBuffer).Position 1 "Position should be incremented by 1"
        }
        
        test "writeString encodes empty string correctly" {
            let buffer = createBuffer()
            writeString buffer ""
            
            let result = (buffer :?> TestBuffer).Data
            Expect.equal result.[0] 0uy "Empty string length should be encoded as 0"
            Expect.equal (buffer :?> TestBuffer).Position 1 "Position should be incremented by 1"
        }
        
        test "writeString encodes ASCII string correctly" {
            let buffer = createBuffer()
            let str = "Hello"
            writeString buffer str
            
            let result = (buffer :?> TestBuffer).Data
            Expect.equal result.[0] 5uy "String length should be encoded as 5"
            
            let bytes = Encoding.UTF8.GetBytes(str)
            for i in 0 .. bytes.Length - 1 do
                Expect.equal result.[i + 1] bytes.[i] $"Byte {i} should match"
            
            Expect.equal (buffer :?> TestBuffer).Position (1 + bytes.Length) "Position should be incremented correctly"
        }
        
        test "writeString encodes Unicode string correctly" {
            let buffer = createBuffer()
            let str = "Hello 世界"
            writeString buffer str
            
            let result = (buffer :?> TestBuffer).Data
            let bytes = Encoding.UTF8.GetBytes(str)
            Expect.equal result.[0] (byte bytes.Length) "String length should be encoded correctly"
            
            for i in 0 .. bytes.Length - 1 do
                Expect.equal result.[i + 1] bytes.[i] $"Byte {i} should match"
            
            Expect.equal (buffer :?> TestBuffer).Position (1 + bytes.Length) "Position should be incremented correctly"
        }
        
        test "writeData encodes empty data correctly" {
            let buffer = createBuffer()
            writeData buffer [||]
            
            let result = (buffer :?> TestBuffer).Data
            Expect.equal result.[0] 0uy "Empty data length should be encoded as 0"
            Expect.equal (buffer :?> TestBuffer).Position 1 "Position should be incremented by 1"
        }
        
        test "writeData encodes non-empty data correctly" {
            let buffer = createBuffer()
            let data = [|1uy; 2uy; 3uy; 4uy; 5uy|]
            writeData buffer data
            
            let result = (buffer :?> TestBuffer).Data
            Expect.equal result.[0] 5uy "Data length should be encoded as 5"
            
            for i in 0 .. data.Length - 1 do
                Expect.equal result.[i + 1] data.[i] $"Byte {i} should match"
            
            Expect.equal (buffer :?> TestBuffer).Position (1 + data.Length) "Position should be incremented correctly"
        }
        
        test "writeFixedData encodes data of correct length" {
            let buffer = createBuffer()
            let data = [|1uy; 2uy; 3uy|]
            writeFixedData buffer data 3
            
            let result = (buffer :?> TestBuffer).Data
            for i in 0 .. data.Length - 1 do
                Expect.equal result.[i] data.[i] $"Byte {i} should match"
            
            Expect.equal (buffer :?> TestBuffer).Position data.Length "Position should be incremented correctly"
        }
        
        test "writeFixedData throws exception for incorrect length" {
            let buffer = createBuffer()
            let data = [|1uy; 2uy; 3uy|]
            
            Expect.throws (fun () -> writeFixedData buffer data 4) "Should throw exception for incorrect length"
        }
        
        test "writeU8 encodes byte correctly" {
            let buffer = createBuffer()
            writeU8 buffer 42uy
            
            let result = (buffer :?> TestBuffer).Data
            Expect.equal result.[0] 42uy "Byte should be encoded directly"
            Expect.equal (buffer :?> TestBuffer).Position 1 "Position should be incremented by 1"
        }
        
        test "writeU16 encodes uint16 correctly" {
            let buffer = createBuffer()
            let value = 0x1234us
            writeU16 buffer value
            
            let result = (buffer :?> TestBuffer).Data
            Expect.equal result.[0] 0x34uy "First byte should be least significant"
            Expect.equal result.[1] 0x12uy "Second byte should be most significant"
            Expect.equal (buffer :?> TestBuffer).Position 2 "Position should be incremented by 2"
        }
        
        test "writeU32 encodes uint32 correctly" {
            let buffer = createBuffer()
            let value = 0x12345678u
            writeU32 buffer value
            
            let result = (buffer :?> TestBuffer).Data
            Expect.equal result.[0] 0x78uy "Bytes should be in little-endian order"
            Expect.equal result.[1] 0x56uy "Bytes should be in little-endian order"
            Expect.equal result.[2] 0x34uy "Bytes should be in little-endian order"
            Expect.equal result.[3] 0x12uy "Bytes should be in little-endian order"
            Expect.equal (buffer :?> TestBuffer).Position 4 "Position should be incremented by 4"
        }
        
        test "writeU64 encodes uint64 correctly" {
            let buffer = createBuffer()
            let value = 0x1234567890ABCDEFuL
            writeU64 buffer value
            
            let result = (buffer :?> TestBuffer).Data
            Expect.equal result.[0] 0xEFuy "Bytes should be in little-endian order"
            Expect.equal result.[1] 0xCDuy "Bytes should be in little-endian order"
            Expect.equal result.[2] 0xABuy "Bytes should be in little-endian order"
            Expect.equal result.[3] 0x90uy "Bytes should be in little-endian order"
            Expect.equal result.[4] 0x78uy "Bytes should be in little-endian order"
            Expect.equal result.[5] 0x56uy "Bytes should be in little-endian order"
            Expect.equal result.[6] 0x34uy "Bytes should be in little-endian order"
            Expect.equal result.[7] 0x12uy "Bytes should be in little-endian order"
            Expect.equal (buffer :?> TestBuffer).Position 8 "Position should be incremented by 8"
        }
        
        test "writeI8 encodes sbyte correctly" {
            let buffer = createBuffer()
            writeI8 buffer -42y
            
            let result = (buffer :?> TestBuffer).Data
            Expect.equal result.[0] 214uy "Sbyte should be encoded as two's complement"
            Expect.equal (buffer :?> TestBuffer).Position 1 "Position should be incremented by 1"
        }
        
        test "writeI16 encodes int16 correctly" {
            let buffer = createBuffer()
            let value = -12345s
            writeI16 buffer value
            
            let result = (buffer :?> TestBuffer).Data
            Expect.equal result.[0] 0xC7uy "First byte should be least significant"
            Expect.equal result.[1] 0xCFuy "Second byte should be most significant"
            Expect.equal (buffer :?> TestBuffer).Position 2 "Position should be incremented by 2"
        }
        
        test "writeI32 encodes int32 correctly" {
            let buffer = createBuffer()
            let value = -12345678
            writeI32 buffer value
            
            let result = (buffer :?> TestBuffer).Data
            // Expected bytes in little-endian order
            let bytes = BitConverter.GetBytes(value)
            for i in 0 .. 3 do
                Expect.equal result.[i] bytes.[i] $"Byte {i} should match"
            
            Expect.equal (buffer :?> TestBuffer).Position 4 "Position should be incremented by 4"
        }
        
        test "writeI64 encodes int64 correctly" {
            let buffer = createBuffer()
            let value = -1234567890123456789L
            writeI64 buffer value
            
            let result = (buffer :?> TestBuffer).Data
            // Expected bytes in little-endian order
            let bytes = BitConverter.GetBytes(value)
            for i in 0 .. 7 do
                Expect.equal result.[i] bytes.[i] $"Byte {i} should match"
            
            Expect.equal (buffer :?> TestBuffer).Position 8 "Position should be incremented by 8"
        }
        
        test "writeF32 encodes float32 correctly" {
            let buffer = createBuffer()
            let value = 3.14159f
            writeF32 buffer value
            
            let result = (buffer :?> TestBuffer).Data
            // Expected bytes in little-endian order
            let bytes = BitConverter.GetBytes(value)
            for i in 0 .. 3 do
                Expect.equal result.[i] bytes.[i] $"Byte {i} should match"
            
            Expect.equal (buffer :?> TestBuffer).Position 4 "Position should be incremented by 4"
        }
        
        test "writeF64 encodes double correctly" {
            let buffer = createBuffer()
            let value = 3.14159265358979
            writeF64 buffer value
            
            let result = (buffer :?> TestBuffer).Data
            // Expected bytes in little-endian order
            let bytes = BitConverter.GetBytes(value)
            for i in 0 .. 7 do
                Expect.equal result.[i] bytes.[i] $"Byte {i} should match"
            
            Expect.equal (buffer :?> TestBuffer).Position 8 "Position should be incremented by 8"
        }
    ]

[<Tests>]
let aggregateEncodingTests =
    testList "Aggregate Encoding Tests" [
        test "writeOptional encodes None correctly" {
            let buffer = createBuffer()
            writeOptional buffer None (fun b v -> writeInt b v)
            
            let result = (buffer :?> TestBuffer).Data
            Expect.equal result.[0] 0uy "None should be encoded as 0"
            Expect.equal (buffer :?> TestBuffer).Position 1 "Position should be incremented by 1"
        }
        
        test "writeOptional encodes Some correctly" {
            let buffer = createBuffer()
            writeOptional buffer (Some 42L) (fun b v -> writeInt b v)
            
            let result = (buffer :?> TestBuffer).Data
            Expect.equal result.[0] 1uy "Some should be encoded as 1"
            Expect.equal result.[1] 84uy "Value should be encoded after tag"
            Expect.equal (buffer :?> TestBuffer).Position 2 "Position should be incremented correctly"
        }
        
        test "writeList encodes empty list correctly" {
            let buffer = createBuffer()
            writeList buffer [] (fun b v -> writeInt b v)
            
            let result = (buffer :?> TestBuffer).Data
            Expect.equal result.[0] 0uy "Empty list should have count 0"
            Expect.equal (buffer :?> TestBuffer).Position 1 "Position should be incremented by 1"
        }
        
        test "writeList encodes non-empty list correctly" {
            let buffer = createBuffer()
            writeList buffer [1L; 2L; 3L] (fun b v -> writeInt b v)
            
            let result = (buffer :?> TestBuffer).Data
            Expect.equal result.[0] 3uy "List count should be 3"
            // Each value is zigzag encoded, so 1 -> 2, 2 -> 4, 3 -> 6
            Expect.equal result.[1] 2uy "First value should be encoded"
            Expect.equal result.[2] 4uy "Second value should be encoded"
            Expect.equal result.[3] 6uy "Third value should be encoded"
            Expect.equal (buffer :?> TestBuffer).Position 4 "Position should be incremented correctly"
        }
        
        test "writeFixedList encodes list of correct length" {
            let buffer = createBuffer()
            writeFixedList buffer [1L; 2L; 3L] 3 (fun b v -> writeInt b v)
            
            let result = (buffer :?> TestBuffer).Data
            // Each value is zigzag encoded, so 1 -> 2, 2 -> 4, 3 -> 6
            Expect.equal result.[0] 2uy "First value should be encoded"
            Expect.equal result.[1] 4uy "Second value should be encoded"
            Expect.equal result.[2] 6uy "Third value should be encoded"
            Expect.equal (buffer :?> TestBuffer).Position 3 "Position should be incremented correctly"
        }
        
        test "writeFixedList throws exception for incorrect length" {
            let buffer = createBuffer()
            
            Expect.throws (fun () -> writeFixedList buffer [1L; 2L] 3 (fun b v -> writeInt b v)) 
                "Should throw exception for incorrect length"
        }
        
        test "writeMap encodes empty map correctly" {
            let buffer = createBuffer()
            writeMap buffer [] 
                (fun b v -> writeString b v) 
                (fun b v -> writeInt b v)
            
            let result = (buffer :?> TestBuffer).Data
            Expect.equal result.[0] 0uy "Empty map should have count 0"
            Expect.equal (buffer :?> TestBuffer).Position 1 "Position should be incremented by 1"
        }
        
        test "writeMap encodes non-empty map correctly" {
            let buffer = createBuffer()
            writeMap buffer [("a", 1L); ("b", 2L)] 
                (fun b v -> writeString b v) 
                (fun b v -> writeInt b v)
            
            let result = (buffer :?> TestBuffer).Data
            Expect.equal result.[0] 2uy "Map count should be 2"
            
            // Position should be after all entries
            let pos = (buffer :?> TestBuffer).Position
            Expect.isGreaterThan pos 5 "Position should be advanced past all entries"
        }
        
        test "writeUnion encodes tag and value correctly" {
            let buffer = createBuffer()
            writeUnion buffer 42u "hello" (fun b v -> writeString b v)
            
            let result = (buffer :?> TestBuffer).Data
            Expect.equal result.[0] 42uy "Tag should be encoded first"
            Expect.equal result.[1] 5uy "String length should follow tag"
            
            let pos = (buffer :?> TestBuffer).Position
            Expect.equal pos 7 "Position should be advanced past tag and value"
        }
        
        test "writeEnum encodes enum value correctly" {
            let buffer = createBuffer()
            writeEnum buffer 42UL
            
            let result = (buffer :?> TestBuffer).Data
            Expect.equal result.[0] 42uy "Enum value should be encoded as uint"
            Expect.equal (buffer :?> TestBuffer).Position 1 "Position should be incremented by 1"
        }
    ]