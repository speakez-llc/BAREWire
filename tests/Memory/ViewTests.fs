module BAREWire.Tests.Memory.ViewTests

open System
open Expecto
open FSharp.UMX
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Core.Memory
open BAREWire.Memory.Region
open BAREWire.Memory.View

/// Measure type for test memory regions
[<Measure>] type testRegion

/// Measure type for test fields
[<Measure>] type testFieldMeasure

/// Test schema definition builder
let createTestSchema () =
    let types = Map.ofList [
        "TestStruct", 
            Aggregate (Struct [
                { Name = "intField"; Type = Primitive I32 }
                { Name = "floatField"; Type = Primitive F32 }
                { Name = "stringField"; Type = Primitive String }
                { Name = "boolField"; Type = Primitive Bool }
                { Name = "nestedStruct"; Type = UserDefined "NestedStruct" }
            ])
        "NestedStruct",
            Aggregate (Struct [
                { Name = "nestedInt"; Type = Primitive I32 }
                { Name = "nestedBool"; Type = Primitive Bool }
            ])
    ]
    
    { 
        Types = types
        Root = "TestStruct"
    }

/// Helper to initialize a memory region and view for testing
let createTestMemoryView () =
    let schema = createTestSchema ()
    let regionSize = 256<bytes> // Large enough for our test struct
    let region = Region.createZeroed<obj, testRegion> regionSize
    let view = View.create<obj, testRegion> region schema
    region, view

[<Tests>]
let viewCreationTests =
    testList "View Creation Tests" [
        test "create produces a valid memory view" {
            // Arrange
            let schema = createTestSchema ()
            let regionSize = 128<bytes>
            let region = Region.createZeroed<obj, testRegion> regionSize
            
            // Act
            let view = View.create<obj, testRegion> region schema
            
            // Assert
            Expect.isTrue (view.Memory.Length = regionSize) "View memory length should match region size"
            Expect.isTrue (view.Memory.Offset = 0<offset>) "View memory offset should be 0"
            Expect.isTrue (view.Schema = schema) "View schema should match provided schema"
            Expect.isFalse (Map.isEmpty view.FieldOffsets) "Field offsets should be calculated"
        }
        
        test "field offsets are calculated correctly" {
            // Arrange
            let schema = createTestSchema ()
            let regionSize = 128<bytes>
            let region = Region.createZeroed<obj, testRegion> regionSize
            
            // Act
            let view = View.create<obj, testRegion> region schema
            
            // Assert
            // Check that fields exist in the offsets map
            Expect.isTrue (Map.containsKey "intField" view.FieldOffsets) "intField should be in field offsets"
            Expect.isTrue (Map.containsKey "floatField" view.FieldOffsets) "floatField should be in field offsets"
            Expect.isTrue (Map.containsKey "stringField" view.FieldOffsets) "stringField should be in field offsets"
            Expect.isTrue (Map.containsKey "boolField" view.FieldOffsets) "boolField should be in field offsets"
            Expect.isTrue (Map.containsKey "nestedStruct" view.FieldOffsets) "nestedStruct should be in field offsets"
            
            // Check the offsets are in ascending order
            let intOffset = (Map.find "intField" view.FieldOffsets).Offset
            let floatOffset = (Map.find "floatField" view.FieldOffsets).Offset
            let stringOffset = (Map.find "stringField" view.FieldOffsets).Offset
            let boolOffset = (Map.find "boolField" view.FieldOffsets).Offset
            let nestedOffset = (Map.find "nestedStruct" view.FieldOffsets).Offset
            
            Expect.isTrue (intOffset < floatOffset) "intField offset should be before floatField"
            Expect.isTrue (floatOffset < stringOffset) "floatField offset should be before stringField"
            Expect.isTrue (stringOffset < boolOffset) "stringField offset should be before boolField"
            Expect.isTrue (boolOffset < nestedOffset) "boolField offset should be before nestedStruct"
        }
    ]

[<Tests>]
let fieldAccessTests =
    testList "Field Access Tests" [
        test "resolveFieldPath finds correct field offsets" {
            // Arrange
            let _, view = createTestMemoryView()
            
            // Act
            let intPathResult = resolveFieldPath view ["intField"]
            let nestedIntPathResult = resolveFieldPath view ["nestedStruct"; "nestedInt"]
            
            // Assert
            match intPathResult, nestedIntPathResult with
            | Ok intOffset, Ok nestedIntOffset ->
                Expect.equal intOffset.Type (Primitive I32) "intField should be of type I32"
                Expect.equal nestedIntOffset.Type (Primitive I32) "nestedInt should be of type I32"
                Expect.isTrue (intOffset.Offset < nestedIntOffset.Offset) "nested field should have higher offset"
            | Error err, _ ->
                failwith $"Failed to resolve intField path: {err.Message}"
            | _, Error err ->
                failwith $"Failed to resolve nestedInt path: {err.Message}"
        }
        
        test "resolveFieldPath returns error for invalid path" {
            // Arrange
            let _, view = createTestMemoryView()
            
            // Act
            let result = resolveFieldPath view ["nonExistentField"]
            
            // Assert
            match result with
            | Ok _ -> failwith "Should not resolve non-existent field"
            | Error err -> 
                Expect.isTrue (err.Code = ErrorCode.InvalidValue) "Error should be InvalidValue"
        }
        
        testAsync "setField and getField work correctly for primitive types" {
            // Arrange
            let region, view = createTestMemoryView()
            
            // Act - Write values
            let intResult = setField<obj, int32, testRegion> view ["intField"] 42
            let floatResult = setField<obj, float32, testRegion> view ["floatField"] 3.14f
            let boolResult = setField<obj, bool, testRegion> view ["boolField"] true
            
            // Read values back
            let readIntResult = getField<obj, int32, testRegion> view ["intField"]
            let readFloatResult = getField<obj, float32, testRegion> view ["floatField"]
            let readBoolResult = getField<obj, bool, testRegion> view ["boolField"]
            
            // Assert
            match intResult, floatResult, boolResult, readIntResult, readFloatResult, readBoolResult with
            | Ok (), Ok (), Ok (), Ok readInt, Ok readFloat, Ok readBool ->
                Expect.equal readInt 42 "Read int value should match written value"
                Expect.equal readFloat 3.14f "Read float value should match written value"
                Expect.isTrue readBool "Read bool value should match written value"
            | _ ->
                failwith "Failed to set or get field values"
        }
        
        testAsync "setField and getField work correctly for strings" {
            // Arrange
            let region, view = createTestMemoryView()
            let testString = "Hello, BAREWire!"
            
            // Act
            let setResult = setField<obj, string, testRegion> view ["stringField"] testString
            let getResult = getField<obj, string, testRegion> view ["stringField"]
            
            // Assert
            match setResult, getResult with
            | Ok (), Ok readString ->
                Expect.equal readString testString "Read string should match written string"
            | _ ->
                failwith "Failed to set or get string field"
        }
        
        testAsync "setField and getField work for nested structures" {
            // Arrange
            let region, view = createTestMemoryView()
            
            // Act
            let setNestedIntResult = setField<obj, int32, testRegion> view ["nestedStruct"; "nestedInt"] 99
            let setNestedBoolResult = setField<obj, bool, testRegion> view ["nestedStruct"; "nestedBool"] true
            
            let getNestedIntResult = getField<obj, int32, testRegion> view ["nestedStruct"; "nestedInt"]
            let getNestedBoolResult = getField<obj, bool, testRegion> view ["nestedStruct"; "nestedBool"]
            
            // Assert
            match setNestedIntResult, setNestedBoolResult, getNestedIntResult, getNestedBoolResult with
            | Ok (), Ok (), Ok readInt, Ok readBool ->
                Expect.equal readInt 99 "Read nested int should match written value"
                Expect.isTrue readBool "Read nested bool should match written value"
            | _ ->
                failwith "Failed to set or get nested field values"
        }
    ]

[<Tests>]
let nestedViewTests =
    testList "Nested View Tests" [
        test "getNestedView creates valid view for nested struct" {
            // Arrange
            let region, view = createTestMemoryView()
            
            // Act
            let result = getNestedView<obj, obj, testRegion> view ["nestedStruct"]
            
            // Assert
            match result with
            | Ok nestedView ->
                // Check that the nested view has the correct schema root
                let nestedTypeName = 
                    match Map.tryFind "nestedStruct" view.FieldOffsets with
                    | Some fo -> 
                        match fo.Type with
                        | UserDefined name -> name
                        | _ -> ""
                    | None -> ""
                
                Expect.equal nestedView.Schema.Root "NestedStruct" "Nested view should have NestedStruct as root"
                
                // Check that the nested view's fields are accessible
                let nestedFieldsResult = fieldExists<obj, testRegion> nestedView ["nestedInt"]
                Expect.isTrue nestedFieldsResult "Nested fields should be accessible in nested view"
            | Error err ->
                failwith $"Failed to get nested view: {err.Message}"
        }
        
        test "getNestedView returns error for non-struct field" {
            // Arrange
            let region, view = createTestMemoryView()
            
            // Act
            let result = getNestedView<obj, obj, testRegion> view ["intField"]
            
            // Assert
            match result with
            | Ok _ -> failwith "Should not create nested view for non-struct field"
            | Error err -> 
                Expect.isTrue (err.Code = ErrorCode.InvalidValue) "Error should be InvalidValue"
        }
    ]

[<Tests>]
let fieldMetadataTests =
    testList "Field Metadata Tests" [
        test "fieldExists correctly identifies existing fields" {
            // Arrange
            let region, view = createTestMemoryView()
            
            // Act & Assert
            Expect.isTrue (fieldExists<obj, testRegion> view ["intField"]) "intField should exist"
            Expect.isTrue (fieldExists<obj, testRegion> view ["nestedStruct"; "nestedInt"]) "nested field should exist"
            Expect.isFalse (fieldExists<obj, testRegion> view ["nonExistentField"]) "non-existent field should not exist"
        }
        
        test "getRootFieldNames returns correct field names" {
            // Arrange
            let region, view = createTestMemoryView()
            
            // Act
            let fieldNames = getRootFieldNames<obj, testRegion> view
            
            // Assert
            Expect.contains fieldNames "intField" "Root fields should include intField"
            Expect.contains fieldNames "floatField" "Root fields should include floatField"
            Expect.contains fieldNames "stringField" "Root fields should include stringField"
            Expect.contains fieldNames "boolField" "Root fields should include boolField"
            Expect.contains fieldNames "nestedStruct" "Root fields should include nestedStruct"
            Expect.equal fieldNames.Length 5 "Should have 5 root fields"
        }
    ]

[<Tests>]
let umxIntegrationTests =
    testList "UMX Integration Tests" [
        test "getFieldWithMeasure and setFieldWithMeasure work correctly" {
            // Arrange
            let region, view = createTestMemoryView()
            let measuredValue = UMX.tag<testFieldMeasure> 42
            
            // Act
            let setResult = setFieldWithMeasure<obj, int, testRegion, testFieldMeasure> view ["intField"] measuredValue
            let getResult = getFieldWithMeasure<obj, int, testRegion, testFieldMeasure> view ["intField"]
            
            // Assert
            match setResult, getResult with
            | Ok (), Ok readValue ->
                Expect.equal (UMX.untag readValue) (UMX.untag measuredValue) "Read measured value should match written value"
            | _ ->
                failwith "Failed to set or get measured field value"
        }
    ]

[<Tests>]
let updateFieldTests =
    testList "Update Field Tests" [
        test "updateField correctly transforms field value" {
            // Arrange
            let region, view = createTestMemoryView()
            
            // First set an initial value
            let initialValue = 10
            setField<obj, int32, testRegion> view ["intField"] initialValue |> ignore
            
            // Act - Update the value by doubling it
            let updateResult = updateField<obj, int32, testRegion> view ["intField"] (fun x -> x * 2)
            
            // Read back the updated value
            let getResult = getField<obj, int32, testRegion> view ["intField"]
            
            // Assert
            match updateResult, getResult with
            | Ok (), Ok value ->
                Expect.equal value (initialValue * 2) "Field should be updated with transformed value"
            | _ ->
                failwith "Failed to update field value"
        }
    ]

[<Tests>]
let addressBasedViewTests =
    testList "Address-Based View Tests" [
        test "fromAddress creates valid view for specific address" {
            // Arrange
            let schema = createTestSchema ()
            let regionSize = 256<bytes>
            let region = Region.createZeroed<obj, testRegion> regionSize
            let address = { Offset = 64<offset> }
            
            // Act
            let view = fromAddress<obj, testRegion> address region schema
            
            // Assert
            Expect.equal view.Memory.Offset address.Offset "View offset should match address offset"
            Expect.equal view.Memory.Length (region.Length - (address.Offset - region.Offset)) "View length should be adjusted"
            Expect.equal view.Schema schema "View schema should match provided schema"
        }
    ]

// Main entry point for running tests
[<EntryPoint>]
let main args =
    runTestsWithCLIArgs [] args viewCreationTests