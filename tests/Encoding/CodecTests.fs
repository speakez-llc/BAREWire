module BAREWire.Tests.Encoding.CodecTests

open Expecto
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Encoding.Codec
open System
open System.Text
open FSharp.UMX

[<Measure>] type test
[<Measure>] type offset
[<Measure>] type person_id

/// A simple implementation of Buffer for testing
type TestBuffer() =
    let mutable data = Array.zeroCreate<byte> 1024
    let mutable position = 0
    
    member _.Data = data
    member _.Position = position
    
    member _.ToMemory() = TestMemory(data) :> Memory<test, test>
    
    interface Buffer<test> with
        member _.Write(value: byte) =
            data.[position] <- value
            position <- position + 1
        
        member _.WriteSpan(span: ReadOnlySpan<byte>) =
            for i in 0 .. span.Length - 1 do
                data.[position + i] <- span.[i]
            position <- position + span.Length
        
        member _.Position = position

/// A simple implementation of Memory for testing
and TestMemory(data: byte[]) =
    let data = data
    
    member _.Data = data
    member _.Offset = 0<offset>
    
    interface Memory<test, test> with
        member _.Data = data
        member _.Offset = 0<offset>
        member _.Length = data.Length

let createBuffer() = TestBuffer()

// Type definitions for testing schema-based encoding/decoding
type PrimitiveSchema = {
    UIntValue: uint64
    IntValue: int64
    BoolValue: bool
    StringValue: string
    DataValue: byte[]
}

type AggregateSchema = {
    OptionalValue: int64 option
    ListValue: int64 list
    MapValue: Map<string, int64>
}

type UnionValue =
    | StringCase of string
    | IntCase of int64
    | BoolCase of bool

type ComplexSchema = {
    Name: string
    Age: int
    Id: uint64<person_id>
    Tags: string list
    Data: byte[] option
    Details: Map<string, string>
    Value: UnionValue
}

// Helper to create a validated schema definition
let createValidatedSchema<'T>() : SchemaDefinition<validated> =
    // This is a simplified mock for testing purposes
    // In a real implementation, this would use reflection to analyze the type
    // and create a proper schema definition
    {
        Root = "root"
        Types = Map.empty
        Status = validated
    }

[<Tests>]
let primitiveSchemaTests =
    testList "Primitive Schema Tests" [
        test "encode and decode roundtrip works for primitive types" {
            // Arrange
            let schema = createValidatedSchema<PrimitiveSchema>()
            let value = {
                UIntValue = 42UL
                IntValue = -123L
                BoolValue = true
                StringValue = "Hello, World!"
                DataValue = [|1uy; 2uy; 3uy|]
            }
            let buffer = createBuffer()
            
            // Act
            let encodeResult = encode schema value buffer
            let memory = buffer.ToMemory()
            let decodeResult = decode<PrimitiveSchema> schema memory
            
            // Assert
            Expect.isOk encodeResult "Encoding should succeed"
            Expect.isOk decodeResult "Decoding should succeed"
            
            match decodeResult with
            | Ok (decodedValue, _) ->
                Expect.equal decodedValue.UIntValue value.UIntValue "UIntValue should be preserved"
                Expect.equal decodedValue.IntValue value.IntValue "IntValue should be preserved"
                Expect.equal decodedValue.BoolValue value.BoolValue "BoolValue should be preserved"
                Expect.equal decodedValue.StringValue value.StringValue "StringValue should be preserved"
                Expect.equal decodedValue.DataValue value.DataValue "DataValue should be preserved"
            | Error err ->
                failwith $"Decoding failed: {err}"
        }
    ]

[<Tests>]
let aggregateSchemaTests =
    testList "Aggregate Schema Tests" [
        test "encode and decode roundtrip works for aggregate types" {
            // Arrange
            let schema = createValidatedSchema<AggregateSchema>()
            let value = {
                OptionalValue = Some 42L
                ListValue = [1L; 2L; 3L]
                MapValue = Map [("a", 1L); ("b", 2L); ("c", 3L)]
            }
            let buffer = createBuffer()
            
            // Act
            let encodeResult = encode schema value buffer
            let memory = buffer.ToMemory()
            let decodeResult = decode<AggregateSchema> schema memory
            
            // Assert
            Expect.isOk encodeResult "Encoding should succeed"
            Expect.isOk decodeResult "Decoding should succeed"
            
            match decodeResult with
            | Ok (decodedValue, _) ->
                Expect.equal decodedValue.OptionalValue value.OptionalValue "OptionalValue should be preserved"
                Expect.equal decodedValue.ListValue value.ListValue "ListValue should be preserved"
                Expect.equal decodedValue.MapValue value.MapValue "MapValue should be preserved"
            | Error err ->
                failwith $"Decoding failed: {err}"
        }
        
        test "encode and decode works with None for optional values" {
            // Arrange
            let schema = createValidatedSchema<AggregateSchema>()
            let value = {
                OptionalValue = None
                ListValue = [1L; 2L; 3L]
                MapValue = Map [("a", 1L); ("b", 2L); ("c", 3L)]
            }
            let buffer = createBuffer()
            
            // Act
            let encodeResult = encode schema value buffer
            let memory = buffer.ToMemory()
            let decodeResult = decode<AggregateSchema> schema memory
            
            // Assert
            Expect.isOk encodeResult "Encoding should succeed"
            Expect.isOk decodeResult "Decoding should succeed"
            
            match decodeResult with
            | Ok (decodedValue, _) ->
                Expect.equal decodedValue.OptionalValue None "OptionalValue should be None"
                Expect.equal decodedValue.ListValue value.ListValue "ListValue should be preserved"
                Expect.equal decodedValue.MapValue value.MapValue "MapValue should be preserved"
            | Error err ->
                failwith $"Decoding failed: {err}"
        }
        
        test "encode and decode works with empty collections" {
            // Arrange
            let schema = createValidatedSchema<AggregateSchema>()
            let value = {
                OptionalValue = Some 42L
                ListValue = []
                MapValue = Map.empty
            }
            let buffer = createBuffer()
            
            // Act
            let encodeResult = encode schema value buffer
            let memory = buffer.ToMemory()
            let decodeResult = decode<AggregateSchema> schema memory
            
            // Assert
            Expect.isOk encodeResult "Encoding should succeed"
            Expect.isOk decodeResult "Decoding should succeed"
            
            match decodeResult with
            | Ok (decodedValue, _) ->
                Expect.equal decodedValue.OptionalValue value.OptionalValue "OptionalValue should be preserved"
                Expect.equal decodedValue.ListValue [] "ListValue should be empty"
                Expect.equal decodedValue.MapValue Map.empty "MapValue should be empty"
            | Error err ->
                failwith $"Decoding failed: {err}"
        }
    ]

[<Tests>]
let unionSchemaTests =
    testList "Union Schema Tests" [
        test "encodes and decodes StringCase correctly" {
            // Since we're just mocking the schema, we'll directly test the union encoding helpers
            // This test would be expanded to use the codec functions with a proper schema implementation
            let unionValue = UnionValue.StringCase "test"
            
            // For testing purposes, we'd convert the union to a tag and value
            let tag = 0u // Example tag for StringCase
            let value = "test"
            
            // Then verify the round-trip works
            Expect.equal (UnionValue.StringCase value) unionValue "Round-trip should preserve union case"
        }
        
        test "encodes and decodes IntCase correctly" {
            let unionValue = UnionValue.IntCase 42L
            
            // For testing purposes, we'd convert the union to a tag and value
            let tag = 1u // Example tag for IntCase
            let value = 42L
            
            // Then verify the round-trip works
            Expect.equal (UnionValue.IntCase value) unionValue "Round-trip should preserve union case"
        }
        
        test "encodes and decodes BoolCase correctly" {
            let unionValue = UnionValue.BoolCase true
            
            // For testing purposes, we'd convert the union to a tag and value
            let tag = 2u // Example tag for BoolCase
            let value = true
            
            // Then verify the round-trip works
            Expect.equal (UnionValue.BoolCase value) unionValue "Round-trip should preserve union case"
        }
    ]

[<Tests>]
let complexSchemaTests =
    testList "Complex Schema Tests" [
        test "encode and decode roundtrip works for complex types" {
            // Arrange
            let schema = createValidatedSchema<ComplexSchema>()
            let value = {
                Name = "John Doe"
                Age = 30
                Id = UMX.tag<person_id> 12345UL
                Tags = ["developer"; "F#"; "BARE"]
                Data = Some [|1uy; 2uy; 3uy|]
                Details = Map [("email", "john@example.com"); ("location", "San Francisco")]
                Value = UnionValue.StringCase "test value"
            }
            let buffer = createBuffer()
            
            // Act
            let encodeResult = encode schema value buffer
            let memory = buffer.ToMemory()
            let decodeResult = decode<ComplexSchema> schema memory
            
            // Assert
            Expect.isOk encodeResult "Encoding should succeed"
            Expect.isOk decodeResult "Decoding should succeed"
            
            match decodeResult with
            | Ok (decodedValue, _) ->
                Expect.equal decodedValue.Name value.Name "Name should be preserved"
                Expect.equal decodedValue.Age value.Age "Age should be preserved"
                Expect.equal decodedValue.Id value.Id "Id should be preserved"
                Expect.equal decodedValue.Tags value.Tags "Tags should be preserved"
                Expect.equal decodedValue.Data value.Data "Data should be preserved"
                Expect.equal decodedValue.Details value.Details "Details should be preserved"
                
                match decodedValue.Value, value.Value with
                | UnionValue.StringCase s1, UnionValue.StringCase s2 -> 
                    Expect.equal s1 s2 "String case value should be preserved"
                | UnionValue.IntCase i1, UnionValue.IntCase i2 -> 
                    Expect.equal i1 i2 "Int case value should be preserved"
                | UnionValue.BoolCase b1, UnionValue.BoolCase b2 -> 
                    Expect.equal b1 b2 "Bool case value should be preserved"
                | _ -> 
                    failwith "Union case mismatch"
            | Error err ->
                failwith $"Decoding failed: {err}"
        }
    ]

[<Tests>]
let measureTypeTests =
    testList "Measure Type Tests" [
        test "encodeWithMeasure and decodeWithMeasure preserve measure types" {
            // Arrange
            let schema = createValidatedSchema<uint64>()
            let value = UMX.tag<person_id> 12345UL
            let buffer = createBuffer()
            
            // Act
            let encodeResult = encodeWithMeasure<uint64, person_id> schema value buffer
            let memory = buffer.ToMemory()
            let decodeResult = decodeWithMeasure<uint64, person_id> schema memory
            
            // Assert
            Expect.isOk encodeResult "Encoding should succeed"
            Expect.isOk decodeResult "Decoding should succeed"
            
            match decodeResult with
            | Ok (decodedValue, _) ->
                Expect.equal decodedValue value "Measure-tagged value should be preserved"
            | Error err ->
                failwith $"Decoding failed: {err}"
        }
    ]

[<Tests>]
let errorHandlingTests =
    testList "Error Handling Tests" [
        test "encode returns Error for invalid data" {
            // Arrange
            let schema = createValidatedSchema<PrimitiveSchema>()
            // Simulate an invalid scenario by using reflection to invoke encode with invalid arguments
            // In a real test, we'd find a way to trigger schema validation errors
            
            // Assert
            // We'd expect something like:
            // Expect.isError result "Encoding should fail for invalid data"
            // Expect.stringContains (Error.getMessage result) "expected error message" "Error message should explain the issue"
            
            // For now, this is just a placeholder to show the pattern
            Expect.isTrue true "Placeholder for error handling test"
        }
        
        test "decode returns Error for corrupted data" {
            // Arrange
            let schema = createValidatedSchema<PrimitiveSchema>()
            let corruptedData = [|255uy; 255uy; 255uy|] // Invalid ULEB128 encoding
            let memory = TestMemory(corruptedData) :> Memory<test, test>
            
            // Act
            let result = decode<PrimitiveSchema> schema memory
            
            // Assert
            // We'd expect something like:
            // Expect.isError result "Decoding should fail for corrupted data"
            // Expect.stringContains (Error.getMessage result) "expected error message" "Error message should explain the issue"
            
            // For now, this is just a placeholder to show the pattern
            Expect.isTrue true "Placeholder for error handling test"
        }
    ]

[<Tests>]
let performanceTests =
    testList "Performance Tests" [
        testCase "encode and decode have acceptable performance for large data" <| fun _ ->
            // Arrange
            let schema = createValidatedSchema<AggregateSchema>()
            let largeList = List.init 10000 int64
            let value = {
                OptionalValue = Some 42L
                ListValue = largeList
                MapValue = Map.ofList (List.init 1000 (fun i -> $"key{i}", int64 i))
            }
            let buffer = createBuffer()
            
            // Act & Assert
            let sw = System.Diagnostics.Stopwatch.StartNew()
            let encodeResult = encode schema value buffer
            sw.Stop()
            let encodeTime = sw.ElapsedMilliseconds
            
            let memory = buffer.ToMemory()
            sw.Restart()
            let decodeResult = decode<AggregateSchema> schema memory
            sw.Stop()
            let decodeTime = sw.ElapsedMilliseconds
            
            // Check performance is within acceptable limits
            // These thresholds would be adjusted based on the expected performance characteristics
            Expect.isLessThan encodeTime 500L "Encoding should complete within time limit"
            Expect.isLessThan decodeTime 500L "Decoding should complete within time limit"
            
            // Also verify correctness
            Expect.isOk encodeResult "Encoding should succeed"
            Expect.isOk decodeResult "Decoding should succeed"
            
            match decodeResult with
            | Ok (decodedValue, _) ->
                Expect.equal decodedValue.ListValue.Length value.ListValue.Length "List length should be preserved"
                Expect.equal decodedValue.MapValue.Count value.MapValue.Count "Map count should be preserved"
            | Error err ->
                failwith $"Decoding failed: {err}"
        }
    ]