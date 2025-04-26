module BAREWire.Tests.Memory.SafeMemoryTests

open System
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open Expecto
open FSharp.NativeInterop
open BAREWire.Core.Error
open BAREWire.Memory.SafeMemory

#nowarn "9" // Disable warning about using fixed keyword

[<Tests>]
let pinnedDataTests =
    testList "Pinned Data Tests" [
        test "withPinnedData provides valid memory address" {
            // Arrange
            let testData = [| 1uy; 2uy; 3uy; 4uy |]
            let offset = 0
            
            // Act
            let result = withPinnedData testData offset (fun addr ->
                // Check that the address is non-zero
                addr <> 0n
            )
            
            // Assert
            Expect.isTrue result "Address should be valid (non-zero)"
        }
        
        test "withPinnedData with offset provides correct address" {
            // Arrange
            let testData = [| 1uy; 2uy; 3uy; 4uy |]
            let offset = 2
            
            // Act
            let result = withPinnedData testData offset (fun addr ->
                // Verify the offset address by reading the byte value
                let ptr = NativePtr.ofNativeInt<byte> addr
                let value = NativePtr.read ptr
                value = 3uy
            )
            
            // Assert
            Expect.isTrue result "Address with offset should point to correct value"
        }
    ]

[<Tests>]
let fixedMemoryTests =
    testList "Fixed Memory Tests" [
        test "FixedMemory.GetPointerAtOffset returns correct pointer" {
            // Arrange
            let testData = [| 1uy; 2uy; 3uy; 4uy |]
            
            // Act & Assert
            withFixedMemory<byte, bool> testData (fun mem ->
                // Get pointer to the third byte
                let offset = 2
                let ptr = mem.GetPointerAtOffset(offset)
                
                // Read the value
                let value = NativePtr.read ptr
                Expect.equal value 3uy "Pointer should point to correct value"
                
                true // Required return value from withFixedMemory
            ) |> ignore
        }
        
        test "FixedMemory.Read returns correct values" {
            // Arrange
            let testData = Array.zeroCreate 16
            
            // Setup test data with specific values
            writeUnmanaged<int32> testData 0 0x01020304
            writeUnmanaged<float32> testData 4 42.0f
            writeUnmanaged<int16> testData 8 0x0506s
            
            // Act & Assert
            withFixedMemory<byte, bool> testData (fun mem ->
                // Read int32
                let int32Value = mem.Read<int32>(0)
                Expect.equal int32Value 0x01020304 "Should read correct int32 value"
                
                // Read float32
                let float32Value = mem.Read<float32>(4)
                Expect.equal float32Value 42.0f "Should read correct float32 value"
                
                // Read int16
                let int16Value = mem.Read<int16>(8)
                Expect.equal int16Value 0x0506s "Should read correct int16 value"
                
                true
            ) |> ignore
        }
        
        test "FixedMemory.Write correctly sets values" {
            // Arrange
            let testData = Array.zeroCreate 16
            
            // Act
            withFixedMemory<byte, bool> testData (fun mem ->
                // Write values
                mem.Write<int32>(0, 0x01020304)
                mem.Write<float32>(4, 42.0f)
                mem.Write<int16>(8, 0x0506s)
                true
            ) |> ignore
            
            // Assert - Read back using regular memory access
            let int32Value = readUnmanaged<int32> testData 0
            let float32Value = readUnmanaged<float32> testData 4
            let int16Value = readUnmanaged<int16> testData 8
            
            Expect.equal int32Value 0x01020304 "Should write correct int32 value"
            Expect.equal float32Value 42.0f "Should write correct float32 value"
            Expect.equal int16Value 0x0506s "Should write correct int16 value"
        }
    ]

[<Tests>]
let readWriteUnmanagedTests =
    testList "Read/Write Unmanaged Tests" [
        test "readUnmanaged and writeUnmanaged work for basic types" {
            // Arrange
            let testData = Array.zeroCreate 32
            
            // Act - Write values
            writeUnmanaged<bool> testData 0 true
            writeUnmanaged<byte> testData 1 0xAAuy
            writeUnmanaged<sbyte> testData 2 -42y
            writeUnmanaged<int16> testData 4 0x1234s
            writeUnmanaged<uint16> testData 6 0xABCDus
            writeUnmanaged<int32> testData 8 0x12345678
            writeUnmanaged<uint32> testData 12 0xABCDEF01u
            writeUnmanaged<int64> testData 16 0x0123456789ABCDEFL
            writeUnmanaged<uint64> testData 24 0xFEDCBA9876543210UL
            
            // Read values back
            let boolValue = readUnmanaged<bool> testData 0
            let byteValue = readUnmanaged<byte> testData 1
            let sbyteValue = readUnmanaged<sbyte> testData 2
            let int16Value = readUnmanaged<int16> testData 4
            let uint16Value = readUnmanaged<uint16> testData 6
            let int32Value = readUnmanaged<int32> testData 8
            let uint32Value = readUnmanaged<uint32> testData 12
            let int64Value = readUnmanaged<int64> testData 16
            let uint64Value = readUnmanaged<uint64> testData 24
            
            // Assert
            Expect.equal boolValue true "Bool value should match"
            Expect.equal byteValue 0xAAuy "Byte value should match"
            Expect.equal sbyteValue -42y "SByte value should match"
            Expect.equal int16Value 0x1234s "Int16 value should match"
            Expect.equal uint16Value 0xABCDus "UInt16 value should match"
            Expect.equal int32Value 0x12345678 "Int32 value should match"
            Expect.equal uint32Value 0xABCDEF01u "UInt32 value should match"
            Expect.equal int64Value 0x0123456789ABCDEFL "Int64 value should match"
            Expect.equal uint64Value 0xFEDCBA9876543210UL "UInt64 value should match"
        }
        
        test "readUnmanaged and writeUnmanaged work for floating point types" {
            // Arrange
            let testData = Array.zeroCreate 16
            
            // Act - Write values
            writeUnmanaged<float32> testData 0 3.14159f
            writeUnmanaged<float> testData 4 2.71828
            
            // Read values back
            let float32Value = readUnmanaged<float32> testData 0
            let float64Value = readUnmanaged<float> testData 4
            
            // Assert - Using close comparison for floating point
            Expect.floatClose Accuracy.medium float32Value 3.14159f "Float32 value should be close"
            Expect.floatClose Accuracy.medium float64Value 2.71828 "Float64 value should be close"
        }
        
        test "readUnmanaged and writeUnmanaged work for custom structs" {
            // Define a test struct
            [<Struct; StructLayout(LayoutKind.Sequential)>]
            type TestStruct =
                val mutable Id: int32
                val mutable Value: float32
                val mutable Flag: byte
            
            // Arrange
            let testData = Array.zeroCreate 16
            let testStruct = TestStruct(Id = 42, Value = 3.14f, Flag = 1uy)
            
            // Act - Write struct
            writeUnmanaged<TestStruct> testData 0 testStruct
            
            // Read struct back
            let readStruct = readUnmanaged<TestStruct> testData 0
            
            // Assert
            Expect.equal readStruct.Id 42 "Struct Id should match"
            Expect.floatClose Accuracy.medium readStruct.Value 3.14f "Struct Value should be close"
            Expect.equal readStruct.Flag 1uy "Struct Flag should match"
        }
    ]

[<Tests>]
let byteOperationTests =
    testList "Byte Operation Tests" [
        test "readByte and writeByte work correctly" {
            // Arrange
            let testData = Array.zeroCreate 4
            
            // Act
            writeByte testData 0 0xAAuy
            writeByte testData 1 0xBBuy
            writeByte testData 2 0xCCuy
            writeByte testData 3 0xDDuy
            
            // Read values back
            let byte0 = readByte testData 0
            let byte1 = readByte testData 1
            let byte2 = readByte testData 2
            let byte3 = readByte testData 3
            
            // Assert
            Expect.equal byte0 0xAAuy "First byte should match"
            Expect.equal byte1 0xBBuy "Second byte should match"
            Expect.equal byte2 0xCCuy "Third byte should match"
            Expect.equal byte3 0xDDuy "Fourth byte should match"
        }
        
        test "readByte throws for out of bounds access" {
            // Arrange
            let testData = [| 1uy; 2uy |]
            
            // Act & Assert
            Expect.throws (fun () -> readByte testData 2 |> ignore) "Reading beyond array bounds should throw"
        }
        
        test "writeByte throws for out of bounds access" {
            // Arrange
            let testData = [| 1uy; 2uy |]
            
            // Act & Assert
            Expect.throws (fun () -> writeByte testData 2 3uy) "Writing beyond array bounds should throw"
        }
    ]

[<Tests>]
let memoryOperationTests =
    testList "Memory Operation Tests" [
        test "copyMemory correctly copies data between arrays" {
            // Arrange
            let source = [| 1uy; 2uy; 3uy; 4uy; 5uy |]
            let dest = Array.zeroCreate 10
            
            // Act
            copyMemory source 1 dest 3 3
            
            // Assert
            Expect.equal dest.[0] 0uy "Destination before copy region should be unchanged"
            Expect.equal dest.[1] 0uy "Destination before copy region should be unchanged"
            Expect.equal dest.[2] 0uy "Destination before copy region should be unchanged"
            Expect.equal dest.[3] 2uy "First copied byte should match source"
            Expect.equal dest.[4] 3uy "Second copied byte should match source"
            Expect.equal dest.[5] 4uy "Third copied byte should match source"
            Expect.equal dest.[6] 0uy "Destination after copy region should be unchanged"
        }
        
        test "copyMemory throws for out of bounds access" {
            // Arrange
            let source = [| 1uy; 2uy; 3uy |]
            let dest = Array.zeroCreate 5
            
            // Act & Assert
            Expect.throws (fun () -> copyMemory source 1 dest 2 4) "Copying beyond array bounds should throw"
        }
    ]

[<Tests>]
let typedReadWriteTests =
    testList "Typed Read/Write Tests" [
        test "read/write Int16 functions work correctly" {
            // Arrange
            let testData = Array.zeroCreate 4
            let testValue = 0x1234s
            
            // Act
            let writeResult = writeInt16 testData 0 testValue
            let readResult = readInt16 testData 0
            
            // Assert
            match writeResult, readResult with
            | Ok (), Ok value ->
                Expect.equal value testValue "Read Int16 value should match written value"
            | Error werr, _ ->
                failwith $"Failed to write Int16: {werr.Message}"
            | _, Error rerr ->
                failwith $"Failed to read Int16: {rerr.Message}"
        }
        
        test "read/write Int32 functions work correctly" {
            // Arrange
            let testData = Array.zeroCreate 4
            let testValue = 0x12345678
            
            // Act
            let writeResult = writeInt32 testData 0 testValue
            let readResult = readInt32 testData 0
            
            // Assert
            match writeResult, readResult with
            | Ok (), Ok value ->
                Expect.equal value testValue "Read Int32 value should match written value"
            | Error werr, _ ->
                failwith $"Failed to write Int32: {werr.Message}"
            | _, Error rerr ->
                failwith $"Failed to read Int32: {rerr.Message}"
        }
        
        test "read/write Int64 functions work correctly" {
            // Arrange
            let testData = Array.zeroCreate 8
            let testValue = 0x0123456789ABCDEFL
            
            // Act
            let writeResult = writeInt64 testData 0 testValue
            let readResult = readInt64 testData 0
            
            // Assert
            match writeResult, readResult with
            | Ok (), Ok value ->
                Expect.equal value testValue "Read Int64 value should match written value"
            | Error werr, _ ->
                failwith $"Failed to write Int64: {werr.Message}"
            | _, Error rerr ->
                failwith $"Failed to read Int64: {rerr.Message}"
        }
        
        test "read/write Float32 functions work correctly" {
            // Arrange
            let testData = Array.zeroCreate 4
            let testValue = 3.14159f
            
            // Act
            let writeResult = writeFloat32 testData 0 testValue
            let readResult = readFloat32 testData 0
            
            // Assert
            match writeResult, readResult with
            | Ok (), Ok value ->
                Expect.floatClose Accuracy.medium value testValue "Read Float32 value should be close to written value"
            | Error werr, _ ->
                failwith $"Failed to write Float32: {werr.Message}"
            | _, Error rerr ->
                failwith $"Failed to read Float32: {rerr.Message}"
        }
        
        test "read/write Float64 functions work correctly" {
            // Arrange
            let testData = Array.zeroCreate 8
            let testValue = 2.7182818284590452353602874713527
            
            // Act
            let writeResult = writeFloat64 testData 0 testValue
            let readResult = readFloat64 testData 0
            
            // Assert
            match writeResult, readResult with
            | Ok (), Ok value ->
                Expect.floatClose Accuracy.medium value testValue "Read Float64 value should be close to written value"
            | Error werr, _ ->
                failwith $"Failed to write Float64: {werr.Message}"
            | _, Error rerr ->
                failwith $"Failed to read Float64: {rerr.Message}"
        }
        
        test "read/write functions return errors for out of bounds access" {
            // Arrange
            let testData = Array.zeroCreate 2 // Too small for many operations
            
            // Act & Assert
            let int32WriteResult = writeInt32 testData 0 42
            match int32WriteResult with
            | Ok () -> failwith "Should fail when writing int32 to 2-byte array"
            | Error err -> Expect.equal err.Code ErrorCode.Encoding "Error should be Encoding"
            
            let int64ReadResult = readInt64 testData 0
            match int64ReadResult with
            | Ok _ -> failwith "Should fail when reading int64 from 2-byte array"
            | Error err -> Expect.equal err.Code ErrorCode.Decoding "Error should be Decoding"
        }
    ]

// Main entry point for running tests
[<EntryPoint>]
let main args =
    runTestsWithCLIArgs [] args pinnedDataTests