module BAREWire.Tests.Core

open Expecto
open BAREWire.Core
open FSharp.NativeInterop

[<Tests>]
let binaryTests =
    testList "Binary Module Tests" [
        testCase "singleToInt32Bits and int32BitsToSingle are inverse operations" <| fun _ ->
            let values = [0.0f; 1.0f; -1.0f; System.Single.MaxValue; System.Single.MinValue; 
                          System.Single.Epsilon; System.Single.NaN; System.Single.PositiveInfinity; 
                          System.Single.NegativeInfinity]
                          
            for original in values do
                let bits = Binary.singleToInt32Bits original
                let roundTrip = Binary.int32BitsToSingle bits
                
                // For NaN, we can't use equality comparison
                if System.Single.IsNaN(original) then
                    Expect.isTrue (System.Single.IsNaN(roundTrip)) "NaN should round-trip correctly"
                else
                    Expect.equal roundTrip original "Float value should round-trip correctly"
        
        testCase "doubleToInt64Bits and int64BitsToDouble are inverse operations" <| fun _ ->
            let values = [0.0; 1.0; -1.0; System.Double.MaxValue; System.Double.MinValue; 
                          System.Double.Epsilon; System.Double.NaN; System.Double.PositiveInfinity; 
                          System.Double.NegativeInfinity]
                          
            for original in values do
                let bits = Binary.doubleToInt64Bits original
                let roundTrip = Binary.int64BitsToDouble bits
                
                // For NaN, we can't use equality comparison
                if System.Double.IsNaN(original) then
                    Expect.isTrue (System.Double.IsNaN(roundTrip)) "NaN should round-trip correctly"
                else
                    Expect.equal roundTrip original "Double value should round-trip correctly"
        
        testCase "getInt16Bytes and toInt16 are inverse operations" <| fun _ ->
            let values = [0s; 1s; -1s; 32767s; -32768s]
            
            for original in values do
                let bytes = Binary.getInt16Bytes original
                let roundTrip = Binary.toInt16 bytes 0
                
                Expect.equal roundTrip original "Int16 value should round-trip correctly"
                Expect.equal bytes.Length 2 "Int16 should be 2 bytes"
        
        testCase "getUInt16Bytes and toUInt16 are inverse operations" <| fun _ ->
            let values = [0us; 1us; 32767us; 65535us]
            
            for original in values do
                let bytes = Binary.getUInt16Bytes original
                let roundTrip = Binary.toUInt16 bytes 0
                
                Expect.equal roundTrip original "UInt16 value should round-trip correctly"
                Expect.equal bytes.Length 2 "UInt16 should be 2 bytes"
        
        testCase "getInt32Bytes and toInt32 are inverse operations" <| fun _ ->
            let values = [0; 1; -1; 2147483647; -2147483648]
            
            for original in values do
                let bytes = Binary.getInt32Bytes original
                let roundTrip = Binary.toInt32 bytes 0
                
                Expect.equal roundTrip original "Int32 value should round-trip correctly"
                Expect.equal bytes.Length 4 "Int32 should be 4 bytes"
        
        testCase "getUInt32Bytes and toUInt32 are inverse operations" <| fun _ ->
            let values = [0u; 1u; 2147483647u; 4294967295u]
            
            for original in values do
                let bytes = Binary.getUInt32Bytes original
                let roundTrip = Binary.toUInt32 bytes 0
                
                Expect.equal roundTrip original "UInt32 value should round-trip correctly"
                Expect.equal bytes.Length 4 "UInt32 should be 4 bytes"
        
        testCase "getInt64Bytes and toInt64 are inverse operations" <| fun _ ->
            let values = [0L; 1L; -1L; 9223372036854775807L; -9223372036854775808L]
            
            for original in values do
                let bytes = Binary.getInt64Bytes original
                let roundTrip = Binary.toInt64 bytes 0
                
                Expect.equal roundTrip original "Int64 value should round-trip correctly"
                Expect.equal bytes.Length 8 "Int64 should be 8 bytes"
        
        testCase "getUInt64Bytes and toUInt64 are inverse operations" <| fun _ ->
            let values = [0UL; 1UL; 9223372036854775807UL; 18446744073709551615UL]
            
            for original in values do
                let bytes = Binary.getUInt64Bytes original
                let roundTrip = Binary.toUInt64 bytes 0
                
                Expect.equal roundTrip original "UInt64 value should round-trip correctly"
                Expect.equal bytes.Length 8 "UInt64 should be 8 bytes"
        
        testCase "toInt16 should throw when out of bounds" <| fun _ ->
            let bytes = [|0uy; 1uy|]
            Expect.throws (fun () -> Binary.toInt16 bytes 1 |> ignore) "Should throw when start index + size exceeds array length"
        
        testCase "toUInt16 should throw when out of bounds" <| fun _ ->
            let bytes = [|0uy; 1uy|]
            Expect.throws (fun () -> Binary.toUInt16 bytes 1 |> ignore) "Should throw when start index + size exceeds array length"
        
        testCase "toInt32 should throw when out of bounds" <| fun _ ->
            let bytes = [|0uy; 1uy; 2uy; 3uy|]
            Expect.throws (fun () -> Binary.toInt32 bytes 1 |> ignore) "Should throw when start index + size exceeds array length"
        
        testCase "toUInt32 should throw when out of bounds" <| fun _ ->
            let bytes = [|0uy; 1uy; 2uy; 3uy|]
            Expect.throws (fun () -> Binary.toUInt32 bytes 1 |> ignore) "Should throw when start index + size exceeds array length"
        
        testCase "toInt64 should throw when out of bounds" <| fun _ ->
            let bytes = [|0uy; 1uy; 2uy; 3uy; 4uy; 5uy; 6uy; 7uy|]
            Expect.throws (fun () -> Binary.toInt64 bytes 1 |> ignore) "Should throw when start index + size exceeds array length"
        
        testCase "toUInt64 should throw when out of bounds" <| fun _ ->
            let bytes = [|0uy; 1uy; 2uy; 3uy; 4uy; 5uy; 6uy; 7uy|]
            Expect.throws (fun () -> Binary.toUInt64 bytes 1 |> ignore) "Should throw when start index + size exceeds array length"
            
        testCase "Binary conversions should match System.BitConverter" <| fun _ ->
            // Int16
            let value16 = 12345s
            let systemBytes16 = System.BitConverter.GetBytes(value16)
            let customBytes16 = Binary.getInt16Bytes value16
            Expect.sequenceEqual customBytes16 systemBytes16 "Int16 bytes should match System.BitConverter"
            
            // UInt16
            let valueU16 = 54321us
            let systemBytesU16 = System.BitConverter.GetBytes(valueU16)
            let customBytesU16 = Binary.getUInt16Bytes valueU16
            Expect.sequenceEqual customBytesU16 systemBytesU16 "UInt16 bytes should match System.BitConverter"
            
            // Int32
            let value32 = 1234567890
            let systemBytes32 = System.BitConverter.GetBytes(value32)
            let customBytes32 = Binary.getInt32Bytes value32
            Expect.sequenceEqual customBytes32 systemBytes32 "Int32 bytes should match System.BitConverter"
            
            // UInt32
            let valueU32 = 3456789012u
            let systemBytesU32 = System.BitConverter.GetBytes(valueU32)
            let customBytesU32 = Binary.getUInt32Bytes valueU32
            Expect.sequenceEqual customBytesU32 systemBytesU32 "UInt32 bytes should match System.BitConverter"
            
            // Int64
            let value64 = 1234567890123456789L
            let systemBytes64 = System.BitConverter.GetBytes(value64)
            let customBytes64 = Binary.getInt64Bytes value64
            Expect.sequenceEqual customBytes64 systemBytes64 "Int64 bytes should match System.BitConverter"
            
            // UInt64
            let valueU64 = 9876543210987654321UL
            let systemBytesU64 = System.BitConverter.GetBytes(valueU64)
            let customBytesU64 = Binary.getUInt64Bytes valueU64
            Expect.sequenceEqual customBytesU64 systemBytesU64 "UInt64 bytes should match System.BitConverter"
            
            // Float32
            let valueF32 = 123.456f
            let systemBytesF32 = System.BitConverter.GetBytes(valueF32)
            let bitsF32 = Binary.singleToInt32Bits valueF32
            let customBytesF32 = Binary.getInt32Bytes bitsF32
            Expect.sequenceEqual customBytesF32 systemBytesF32 "Float32 bytes should match System.BitConverter"
            
            // Float64
            let valueF64 = 123.45678901234
            let systemBytesF64 = System.BitConverter.GetBytes(valueF64)
            let bitsF64 = Binary.doubleToInt64Bits valueF64
            let customBytesF64 = Binary.getInt64Bytes bitsF64
            Expect.sequenceEqual customBytesF64 systemBytesF64 "Float64 bytes should match System.BitConverter"
    ]