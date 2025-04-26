module BAREWire.Tests.Core

open Expecto
open BAREWire.Core

[<Measure>] type TestRegion

[<Tests>]
let memoryTests =
    testList "Memory Module Tests" [
        testCase "fromArray creates memory region with correct properties" <| fun _ ->
            let data = [|1uy; 2uy; 3uy; 4uy|]
            let memory = Memory.fromArray<int, TestRegion> data
            
            Expect.equal memory.Data data "Data reference should be preserved"
            Expect.equal memory.Offset 0<offset> "Offset should be 0"
            Expect.equal memory.Length 4<bytes> "Length should match data length"
            
        testCase "fromArraySlice creates memory region with correct properties" <| fun _ ->
            let data = [|1uy; 2uy; 3uy; 4uy; 5uy; 6uy|]
            let memory = Memory.fromArraySlice<int, TestRegion> data 2 3
            
            Expect.equal memory.Data data "Data reference should be preserved"
            Expect.equal memory.Offset 2<offset> "Offset should match specified offset"
            Expect.equal memory.Length 3<bytes> "Length should match specified length"
            
        testCase "copy copies data between memory regions" <| fun _ ->
            let sourceData = [|1uy; 2uy; 3uy; 4uy|]
            let destData = Array.zeroCreate 6
            
            let source = Memory.fromArray<int, TestRegion> sourceData
            let dest = Memory.fromArraySlice<float, int> destData 1 4
            
            Memory.copy source dest 3<bytes>
            
            Expect.equal destData.[0] 0uy "Data before offset should not be modified"
            Expect.equal destData.[1] 1uy "First byte should be copied"
            Expect.equal destData.[2] 2uy "Second byte should be copied"
            Expect.equal destData.[3] 3uy "Third byte should be copied"
            Expect.equal destData.[4] 0uy "Data after length should not be modified"
            Expect.equal destData.[5] 0uy "Data after length should not be modified"
            
        testCase "copy throws when source is too small" <| fun _ ->
            let sourceData = [|1uy; 2uy|]
            let destData = [|0uy; 0uy; 0uy; 0uy|]
            
            let source = Memory.fromArray<int, TestRegion> sourceData
            let dest = Memory.fromArray<float, int> destData
            
            Expect.throws (fun () -> Memory.copy source dest 3<bytes>) 
                "Copy should throw when count exceeds source length"
            
        testCase "copy throws when destination is too small" <| fun _ ->
            let sourceData = [|1uy; 2uy; 3uy; 4uy|]
            let destData = [|0uy; 0uy|]
            
            let source = Memory.fromArray<int, TestRegion> sourceData
            let dest = Memory.fromArray<float, int> destData
            
            Expect.throws (fun () -> Memory.copy source dest 3<bytes>) 
                "Copy should throw when count exceeds destination length"
                
        testCase "slice creates correct sub-region" <| fun _ ->
            let data = [|1uy; 2uy; 3uy; 4uy; 5uy; 6uy|]
            let memory = Memory.fromArray<int, TestRegion> data
            let sliced = Memory.slice<int, float, TestRegion> memory 2<offset> 3<bytes>
            
            Expect.equal sliced.Data data "Data reference should be preserved"
            Expect.equal sliced.Offset 2<offset> "Offset should be updated"
            Expect.equal sliced.Length 3<bytes> "Length should match specified length"
            
        testCase "slice throws on invalid parameters" <| fun _ ->
            let data = [|1uy; 2uy; 3uy; 4uy|]
            let memory = Memory.fromArray<int, TestRegion> data
            
            Expect.throws (fun () -> Memory.slice memory -1<offset> 2<bytes> |> ignore) 
                "Slice should throw with negative offset"
                
            Expect.throws (fun () -> Memory.slice memory 0<offset> -1<bytes> |> ignore) 
                "Slice should throw with negative length"
                
            Expect.throws (fun () -> Memory.slice memory 3<offset> 2<bytes> |> ignore) 
                "Slice should throw when offset + length exceeds memory length"
                
        testCase "readByte reads correct byte" <| fun _ ->
            let data = [|1uy; 2uy; 3uy; 4uy|]
            let memory = Memory.fromArray<int, TestRegion> data
            
            Expect.equal (Memory.readByte memory 0<offset>) 1uy "Should read first byte"
            Expect.equal (Memory.readByte memory 2<offset>) 3uy "Should read third byte"
            
        testCase "readByte throws on invalid offset" <| fun _ ->
            let data = [|1uy; 2uy; 3uy; 4uy|]
            let memory = Memory.fromArray<int, TestRegion> data
            
            Expect.throws (fun () -> Memory.readByte memory -1<offset> |> ignore) 
                "readByte should throw with negative offset"
                
            Expect.throws (fun () -> Memory.readByte memory 4<offset> |> ignore) 
                "readByte should throw with offset outside bounds"
                
        testCase "writeByte writes byte correctly" <| fun _ ->
            let data = [|1uy; 2uy; 3uy; 4uy|]
            let memory = Memory.fromArray<int, TestRegion> data
            
            Memory.writeByte memory 1<offset> 42uy
            
            Expect.equal data.[1] 42uy "Data should be updated"
            
        testCase "writeByte throws on invalid offset" <| fun _ ->
            let data = [|1uy; 2uy; 3uy; 4uy|]
            let memory = Memory.fromArray<int, TestRegion> data
            
            Expect.throws (fun () -> Memory.writeByte memory -1<offset> 42uy) 
                "writeByte should throw with negative offset"
                
            Expect.throws (fun () -> Memory.writeByte memory 4<offset> 42uy) 
                "writeByte should throw with offset outside bounds"
            
        testCase "getAddress returns correct address" <| fun _ ->
            let data = [|1uy; 2uy; 3uy; 4uy|]
            let memory = { Data = data; Offset = 5<offset>; Length = 4<bytes> }
            let address = Memory.getAddress memory 2<offset>
            
            Expect.equal address.Offset 7<offset> "Address offset should be base offset + relative offset"
            
        testCase "getAddress throws when outside bounds" <| fun _ ->
            let data = [|1uy; 2uy; 3uy; 4uy|]
            let memory = Memory.fromArray<int, TestRegion> data
            
            Expect.throws (fun () -> Memory.getAddress memory 4<offset> |> ignore) 
                "getAddress should throw when offset outside bounds"
                
        testCase "Buffer.Create initializes buffer correctly" <| fun _ ->
            let buffer = Buffer<int>.Create 10
            
            Expect.equal buffer.Data.Length 10 "Buffer size should match capacity"
            Expect.equal buffer.Position 0<offset> "Initial position should be 0"
            
        testCase "Buffer.Write writes and advances position" <| fun _ ->
            let buffer = Buffer<int>.Create 3
            
            buffer.Write 1uy
            Expect.equal buffer.Position 1<offset> "Position should advance by 1"
            Expect.equal buffer.Data.[0] 1uy "Data should be written"
            
            buffer.Write 2uy
            Expect.equal buffer.Position 2<offset> "Position should advance by 1"
            Expect.equal buffer.Data.[1] 2uy "Data should be written"
            
        testCase "Buffer.WriteSpan writes span and advances position" <| fun _ ->
            let buffer = Buffer<int>.Create 5
            let data = [|1uy; 2uy; 3uy|] |> ReadOnlySpan
            
            buffer.WriteSpan data
            
            Expect.equal buffer.Position 3<offset> "Position should advance by span length"
            Expect.equal buffer.Data.[0] 1uy "First byte should be written"
            Expect.equal buffer.Data.[1] 2uy "Second byte should be written"
            Expect.equal buffer.Data.[2] 3uy "Third byte should be written"
    ]