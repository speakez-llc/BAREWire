module BAREWire.Tests.Memory.RegionTests

open System
open Expecto
open FSharp.UMX
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Core.Memory
open BAREWire.Memory.Region

/// Measure type for test memory regions
[<Measure>] type testRegion

[<Tests>]
let regionCreationTests =
    testList "Region Creation Tests" [
        test "create produces a valid memory region" {
            // Arrange
            let testData = [| 1uy; 2uy; 3uy; 4uy |]
            
            // Act
            let region = create<int, testRegion> testData
            
            // Assert
            Expect.equal (int region.Length) testData.Length "Region length should match data length"
            Expect.equal region.Offset 0<offset> "Region offset should be 0"
            Expect.equal region.Data testData "Region data should reference the provided array"
        }
        
        test "createZeroed creates a region filled with zeros" {
            // Arrange
            let size = 10<bytes>
            
            // Act
            let region = createZeroed<int, testRegion> size
            
            // Assert
            Expect.equal (int region.Length) (int size) "Region length should match specified size"
            Expect.equal region.Offset 0<offset> "Region offset should be 0"
            Expect.all region.Data (fun b -> b = 0uy) "All bytes should be zero"
        }
    ]

[<Tests>]
let regionOperationTests =
    testList "Region Operation Tests" [
        test "slice creates a valid subregion" {
            // Arrange
            let testData = [| 1uy; 2uy; 3uy; 4uy; 5uy |]
            let region = create<int, testRegion> testData
            let offset = 1<offset>
            let length = 3<bytes>
            
            // Act
            let result = slice<int, float, testRegion> region offset length
            
            // Assert
            match result with
            | Ok sliced ->
                Expect.equal (int sliced.Length) (int length) "Sliced region length should match specified length"
                Expect.equal sliced.Offset offset "Sliced region offset should match specified offset"
                Expect.equal sliced.Data region.Data "Sliced region should reference the same data array"
            | Error err ->
                failwith $"Slice operation failed: {err.Message}"
        }
        
        test "slice returns error when out of bounds" {
            // Arrange
            let testData = [| 1uy; 2uy; 3uy; 4uy |]
            let region = create<int, testRegion> testData
            let offset = 2<offset>
            let length = 5<bytes> // Too large
            
            // Act
            let result = slice<int, float, testRegion> region offset length
            
            // Assert
            match result with
            | Ok _ -> failwith "Slice should have failed for out-of-bounds request"
            | Error err -> 
                Expect.isTrue (err.Code = ErrorCode.OutOfBounds) "Error should be OutOfBounds"
        }
        
        test "copy correctly transfers data between regions" {
            // Arrange
            let sourceData = [| 1uy; 2uy; 3uy; 4uy |]
            let destData = [| 0uy; 0uy; 0uy; 0uy |]
            let source = create<int, testRegion> sourceData
            let dest = create<float, testRegion> destData
            let count = 3<bytes>
            
            // Act
            let result = copy<int, float, testRegion, testRegion> source dest count
            
            // Assert
            match result with
            | Ok () ->
                Expect.equal destData.[0] sourceData.[0] "First byte should be copied"
                Expect.equal destData.[1] sourceData.[1] "Second byte should be copied"
                Expect.equal destData.[2] sourceData.[2] "Third byte should be copied"
                Expect.equal destData.[3] 0uy "Fourth byte should remain untouched"
            | Error err ->
                failwith $"Copy operation failed: {err.Message}"
        }
        
        test "resize correctly preserves data" {
            // Arrange
            let originalData = [| 1uy; 2uy; 3uy |]
            let region = create<int, testRegion> originalData
            let newSize = 5<bytes>
            
            // Act
            let result = resize<int, testRegion> region newSize
            
            // Assert
            match result with
            | Ok resized ->
                Expect.equal (int resized.Length) (int newSize) "Resized region length should match new size"
                Expect.equal resized.Data.[0] originalData.[0] "First byte should be preserved"
                Expect.equal resized.Data.[1] originalData.[1] "Second byte should be preserved"
                Expect.equal resized.Data.[2] originalData.[2] "Third byte should be preserved"
                Expect.equal resized.Data.[3] 0uy "Extended bytes should be zero"
                Expect.equal resized.Data.[4] 0uy "Extended bytes should be zero"
            | Error err ->
                failwith $"Resize operation failed: {err.Message}"
        }
        
        test "resize to smaller size truncates data" {
            // Arrange
            let originalData = [| 1uy; 2uy; 3uy; 4uy; 5uy |]
            let region = create<int, testRegion> originalData
            let newSize = 3<bytes>
            
            // Act
            let result = resize<int, testRegion> region newSize
            
            // Assert
            match result with
            | Ok resized ->
                Expect.equal (int resized.Length) (int newSize) "Resized region length should match new size"
                Expect.equal resized.Data.[0] originalData.[0] "First byte should be preserved"
                Expect.equal resized.Data.[1] originalData.[1] "Second byte should be preserved"
                Expect.equal resized.Data.[2] originalData.[2] "Third byte should be preserved"
                Expect.equal resized.Data.Length 3 "Array should be truncated"
            | Error err ->
                failwith $"Resize operation failed: {err.Message}"
        }
    ]

[<Tests>]
let regionUtilityTests =
    testList "Region Utility Tests" [
        test "getSize returns correct region size" {
            // Arrange
            let testData = [| 1uy; 2uy; 3uy; 4uy; 5uy |]
            let region = create<int, testRegion> testData
            
            // Act
            let size = getSize<int, testRegion> region
            
            // Assert
            Expect.equal (int size) testData.Length "Size should match the length of the data array"
        }
        
        test "isEmpty returns true for empty region" {
            // Arrange
            let emptyData = [| |]
            let region = create<int, testRegion> emptyData
            
            // Act
            let result = isEmpty<int, testRegion> region
            
            // Assert
            Expect.isTrue result "isEmpty should return true for empty region"
        }
        
        test "isEmpty returns false for non-empty region" {
            // Arrange
            let testData = [| 1uy |]
            let region = create<int, testRegion> testData
            
            // Act
            let result = isEmpty<int, testRegion> region
            
            // Assert
            Expect.isFalse result "isEmpty should return false for non-empty region"
        }
        
        test "merge combines two regions correctly" {
            // Arrange
            let data1 = [| 1uy; 2uy |]
            let data2 = [| 3uy; 4uy |]
            let region1 = create<int, testRegion> data1
            let region2 = create<int, testRegion> data2
            
            // Act
            let result = merge<int, testRegion> region1 region2
            
            // Assert
            match result with
            | Ok merged ->
                Expect.equal (int merged.Length) (data1.Length + data2.Length) "Merged length should be sum of input lengths"
                Expect.equal merged.Data.[0] data1.[0] "First byte should match first region"
                Expect.equal merged.Data.[1] data1.[1] "Second byte should match first region"
                Expect.equal merged.Data.[2] data2.[0] "Third byte should match second region"
                Expect.equal merged.Data.[3] data2.[1] "Fourth byte should match second region"
            | Error err ->
                failwith $"Merge operation failed: {err.Message}"
        }
        
        test "split divides a region correctly" {
            // Arrange
            let testData = [| 1uy; 2uy; 3uy; 4uy |]
            let region = create<int, testRegion> testData
            let offset = 2<offset>
            
            // Act
            let result = split<int, testRegion> region offset
            
            // Assert
            match result with
            | Ok (first, second) ->
                Expect.equal (int first.Length) (int offset) "First region length should match offset"
                Expect.equal (int second.Length) (testData.Length - int offset) "Second region length should be remainder"
                Expect.equal first.Data testData "First region should reference the same data"
                Expect.equal second.Data testData "Second region should reference the same data"
                Expect.equal first.Offset 0<offset> "First region should start at beginning"
                Expect.equal second.Offset offset "Second region should start at split point"
            | Error err ->
                failwith $"Split operation failed: {err.Message}"
        }
        
        test "fill sets all bytes to specified value" {
            // Arrange
            let testData = [| 0uy; 0uy; 0uy; 0uy |]
            let region = create<int, testRegion> testData
            let fillValue = 0xFFuy
            
            // Act
            let result = fill<int, testRegion> region fillValue
            
            // Assert
            match result with
            | Ok () ->
                Expect.all testData (fun b -> b = fillValue) "All bytes should be set to fill value"
            | Error err ->
                failwith $"Fill operation failed: {err.Message}"
        }
        
        test "equal returns true for regions with identical content" {
            // Arrange
            let data1 = [| 1uy; 2uy; 3uy |]
            let data2 = [| 1uy; 2uy; 3uy |]
            let region1 = create<int, testRegion> data1
            let region2 = create<float, testRegion> data2
            
            // Act
            let result = equal<int, float, testRegion, testRegion> region1 region2
            
            // Assert
            Expect.isTrue result "equal should return true for regions with identical content"
        }
        
        test "equal returns false for regions with different content" {
            // Arrange
            let data1 = [| 1uy; 2uy; 3uy |]
            let data2 = [| 1uy; 2uy; 4uy |]
            let region1 = create<int, testRegion> data1
            let region2 = create<float, testRegion> data2
            
            // Act
            let result = equal<int, float, testRegion, testRegion> region1 region2
            
            // Assert
            Expect.isFalse result "equal should return false for regions with different content"
        }
        
        test "equal returns false for regions with different lengths" {
            // Arrange
            let data1 = [| 1uy; 2uy |]
            let data2 = [| 1uy; 2uy; 3uy |]
            let region1 = create<int, testRegion> data1
            let region2 = create<float, testRegion> data2
            
            // Act
            let result = equal<int, float, testRegion, testRegion> region1 region2
            
            // Assert
            Expect.isFalse result "equal should return false for regions with different lengths"
        }
        
        test "find returns correct offset for pattern" {
            // Arrange
            let testData = [| 1uy; 2uy; 3uy; 4uy; 3uy; 4uy |]
            let region = create<int, testRegion> testData
            let pattern = [| 3uy; 4uy |]
            
            // Act
            let result = find<int, testRegion> region pattern
            
            // Assert
            match result with
            | Some offset ->
                Expect.equal (int offset) 2 "Pattern should be found at offset 2"
            | None ->
                failwith "Pattern should be found"
        }
        
        test "find returns None when pattern is not found" {
            // Arrange
            let testData = [| 1uy; 2uy; 3uy; 4uy |]
            let region = create<int, testRegion> testData
            let pattern = [| 5uy; 6uy |]
            
            // Act
            let result = find<int, testRegion> region pattern
            
            // Assert
            Expect.isNone result "find should return None when pattern is not found"
        }
    ]

// Main entry point for running tests
[<EntryPoint>]
let main args =
    runTestsWithCLIArgs [] args regionCreationTests