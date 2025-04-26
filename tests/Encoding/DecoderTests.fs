module BAREWire.Tests.Encoding.DecoderTests

open Expecto
open BAREWire.Core
open BAREWire.Encoding.Decoder
open System
open System.Text

[<Measure>] type test
[<Measure>] type offset

/// A simple implementation of Memory for testing
type TestMemory(data: byte[]) =
    let data = data
    
    member _.Data = data
    member _.Offset = 0<offset>
    
    interface Memory<test, test> with
        member _.Data = data
        member _.Offset = 0<offset>
        member _.Length = data.Length

let createMemory (data: byte[]) = TestMemory(data) :> Memory<test, test>

[<Tests>]
let primitiveDecodingTests =
    testList "Primitive Decoding Tests" [
        test "readUInt decodes zero correctly" {
            let memory = createMemory [|0uy|]
            let result, offset = readUInt memory 0<offset>
            
            Expect.equal result 0UL "Decoded value should be 0"
            Expect.equal offset 1<offset> "Offset should be incremented by 1"
        }
        
        test "readUInt decodes small values correctly" {
            let memory = createMemory [|127uy|]
            let result, offset = readUInt memory 0<offset>
            
            Expect.equal result 127UL "Decoded value should be 127"
            Expect.equal offset 1<offset> "Offset should be incremented by 1"
        }
        
        test "readUInt decodes large values correctly" {
            let memory = createMemory [|128uy; 1uy|]
            let result, offset = readUInt memory 0<offset>
            
            Expect.equal result 128UL "Decoded value should be 128"
            Expect.equal offset 2<offset> "Offset should be incremented by 2"
        }
        
        test "readUInt decodes multi-byte values correctly" {
            let memory = createMemory [|0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 0x01uy|]
            let result, offset = readUInt memory 0<offset>
            
            Expect.equal result UInt64.MaxValue "Decoded value should be UInt64.MaxValue"
            Expect.equal offset 9<offset> "Offset should be incremented by 9"
        }
        
        test "readInt decodes zero correctly" {
            let memory = createMemory [|0uy|]
            let result, offset = readInt memory 0<offset>
            
            Expect.equal result 0L "Decoded value should be 0"
            Expect.equal offset 1<offset> "Offset should be incremented by 1"
        }
        
        test "readInt decodes positive values correctly" {
            let memory = createMemory [|126uy|] // Zigzag encoding of 63
            let result, offset = readInt memory 0<offset>
            
            Expect.equal result 63L "Decoded value should be 63"
            Expect.equal offset 1<offset> "Offset should be incremented by 1"
        }
        
        test "readInt decodes negative values correctly" {
            let memory = createMemory [|1uy|] // Zigzag encoding of -1
            let result, offset = readInt memory 0<offset>
            
            Expect.equal result -1L "Decoded value should be -1"
            Expect.equal offset 1<offset> "Offset should be incremented by 1"
        }
        
        test "readBool decodes true correctly" {
            let memory = createMemory [|1uy|]
            let result, offset = readBool memory 0<offset>
            
            Expect.isTrue result "Decoded value should be true"
            Expect.equal offset 1<offset> "Offset should be incremented by 1"
        }
        
        test "readBool decodes false correctly" {
            let memory = createMemory [|0uy|]
            let result, offset = readBool memory 0<offset>
            
            Expect.isFalse result "Decoded value should be false"
            Expect.equal offset 1<offset> "Offset should be incremented by 1"
        }
        
        test "readBool throws exception for invalid value" {
            let memory = createMemory [|2uy|]
            
            Expect.throws (fun () -> readBool memory 0<offset> |> ignore) 
                "Should throw exception for invalid boolean value"
        }
        
        test "readString decodes empty string correctly" {
            let memory = createMemory [|0uy|]
            let result, offset = readString memory 0<offset>
            
            Expect.equal result "" "Decoded value should be empty string"
            Expect.equal offset 1<offset> "Offset should be incremented by 1"
        }
        
        test "readString decodes ASCII string correctly" {
            let str = "Hello"
            let bytes = Encoding.UTF8.GetBytes(str)
            let data = Array.concat [[|byte bytes.Length|]; bytes]
            let memory = createMemory data
            
            let result, offset = readString memory 0<offset>
            
            Expect.equal result str "Decoded value should match original string"
            Expect.equal offset (1 + bytes.Length) * 1<offset> "Offset should be incremented correctly"
        }
        
        test "readString decodes Unicode string correctly" {
            let str = "Hello 世界"
            let bytes = Encoding.UTF8.GetBytes(str)
            let data = Array.concat [[|byte bytes.Length|]; bytes]
            let memory = createMemory data
            
            let result, offset = readString memory 0<offset>
            
            Expect.equal result str "Decoded value should match original string"
            Expect.equal offset (1 + bytes.Length) * 1<offset> "Offset should be incremented correctly"
        }
        
        test "readData decodes empty data correctly" {
            let memory = createMemory [|0uy|]
            let result, offset = readData memory 0<offset>
            
            Expect.equal result [||] "Decoded value should be empty array"
            Expect.equal offset 1<offset> "Offset should be incremented by 1"
        }
        
        test "readData decodes non-empty data correctly" {
            let data = [|1uy; 2uy; 3uy; 4uy; 5uy|]
            let memory = createMemory (Array.concat [[|byte data.Length|]; data])
            
            let result, offset = readData memory 0<offset>
            
            Expect.equal result data "Decoded value should match original data"
            Expect.equal offset (1 + data.Length) * 1<offset> "Offset should be incremented correctly"
        }
        
        test "readFixedData decodes data correctly" {
            let data = [|1uy; 2uy; 3uy|]
            let memory = createMemory data
            
            let result, offset = readFixedData memory 0<offset> 3
            
            Expect.equal result data "Decoded value should match original data"
            Expect.equal offset 3<offset> "Offset should be incremented by 3"
        }
        
        test "readU8 decodes byte correctly" {
            let memory = createMemory [|42uy|]
            let result, offset = readU8 memory 0<offset>
            
            Expect.equal result 42uy "Decoded value should be 42"
            Expect.equal offset 1<offset> "Offset should be incremented by 1"
        }
        
        test "readU16 decodes uint16 correctly" {
            let value = 0x1234us
            let memory = createMemory [|0x34uy; 0x12uy|] // Little-endian
            
            let result, offset = readU16 memory 0<offset>
            
            Expect.equal result value "Decoded value should match original value"
            Expect.equal offset 2<offset> "Offset should be incremented by 2"
        }
        
        test "readU32 decodes uint32 correctly" {
            let value = 0x12345678u
            let memory = createMemory [|0x78uy; 0x56uy; 0x34uy; 0x12uy|] // Little-endian
            
            let result, offset = readU32 memory 0<offset>
            
            Expect.equal result value "Decoded value should match original value"
            Expect.equal offset 4<offset> "Offset should be incremented by 4"
        }
        
        test "readU64 decodes uint64 correctly" {
            let value = 0x1234567890ABCDEFuL
            let memory = createMemory [|
                0xEFuy; 0xCDuy; 0xABuy; 0x90uy;
                0x78uy; 0x56uy; 0x34uy; 0x12uy
            |] // Little-endian
            
            let result, offset = readU64 memory 0<offset>
            
            Expect.equal result value "Decoded value should match original value"
            Expect.equal offset 8<offset> "Offset should be incremented by 8"
        }
        
        test "readI8 decodes sbyte correctly" {
            let value = -42y
            let memory = createMemory [|214uy|] // Two's complement
            
            let result, offset = readI8 memory 0<offset>
            
            Expect.equal result value "Decoded value should match original value"
            Expect.equal offset 1<offset> "Offset should be incremented by 1"
        }
        
        test "readI16 decodes int16 correctly" {
            let value = -12345s
            let memory = createMemory [|0xC7uy; 0xCFuy|] // Little-endian
            
            let result, offset = readI16 memory 0<offset>
            
            Expect.equal result value "Decoded value should match original value"
            Expect.equal offset 2<offset> "Offset should be incremented by 2"
        }
        
        test "readI32 decodes int32 correctly" {
            let value = -12345678
            let bytes = BitConverter.GetBytes(value) // Little-endian
            let memory = createMemory bytes
            
            let result, offset = readI32 memory 0<offset>
            
            Expect.equal result value "Decoded value should match original value"
            Expect.equal offset 4<offset> "Offset should be incremented by 4"
        }
        
        test "readI64 decodes int64 correctly" {
            let value = -1234567890123456789L
            let bytes = BitConverter.GetBytes(value) // Little-endian
            let memory = createMemory bytes
            
            let result, offset = readI64 memory 0<offset>
            
            Expect.equal result value "Decoded value should match original value"
            Expect.equal offset 8<offset> "Offset should be incremented by 8"
        }
        
        test "readF32 decodes float32 correctly" {
            let value = 3.14159f
            let bytes = BitConverter.GetBytes(value) // Little-endian
            let memory = createMemory bytes
            
            let result, offset = readF32 memory 0<offset>
            
            Expect.equal result value "Decoded value should match original value"
            Expect.equal offset 4<offset> "Offset should be incremented by 4"
        }
        
        test "readF64 decodes double correctly" {
            let value = 3.14159265358979
            let bytes = BitConverter.GetBytes(value) // Little-endian
            let memory = createMemory bytes
            
            let result, offset = readF64 memory 0<offset>
            
            Expect.equal result value "Decoded value should match original value"
            Expect.equal offset 8<offset> "Offset should be incremented by 8"
        }
    ]

[<Tests>]
let aggregateDecodingTests =
    testList "Aggregate Decoding Tests" [
        test "readOptional decodes None correctly" {
            let memory = createMemory [|0uy|]
            let result, offset = readOptional memory 0<offset> (fun m o -> 42L, o + 1<offset>)
            
            Expect.equal result None "Decoded value should be None"
            Expect.equal offset 1<offset> "Offset should be incremented by 1"
        }
        
        test "readOptional decodes Some correctly" {
            let memory = createMemory [|1uy; 42uy|]
            let result, offset = readOptional memory 0<offset> (fun m o -> 42L, o + 1<offset>)
            
            Expect.equal result (Some 42L) "Decoded value should be Some 42L"
            Expect.equal offset 2<offset> "Offset should be incremented by 2"
        }
        
        test "readOptional throws exception for invalid tag" {
            let memory = createMemory [|2uy|]
            
            Expect.throws (fun () -> readOptional memory 0<offset> (fun m o -> 42L, o + 1<offset>) |> ignore) 
                "Should throw exception for invalid tag"
        }
        
        test "readList decodes empty list correctly" {
            let memory = createMemory [|0uy|]
            let result, offset = readList memory 0<offset> (fun m o -> 42L, o + 1<offset>)
            
            Expect.equal result [] "Decoded value should be empty list"
            Expect.equal offset 1<offset> "Offset should be incremented by 1"
        }
        
        test "readList decodes non-empty list correctly" {
            // Encode a list with 3 elements, each taking 1 byte
            let memory = createMemory [|3uy; 1uy; 2uy; 3uy|]
            
            let result, offset = readList memory 0<offset> (fun m o -> 
                let value = m.Data.[int o]
                int64 value, o + 1<offset>
            )
            
            Expect.equal result [1L; 2L; 3L] "Decoded value should match original list"
            Expect.equal offset 4<offset> "Offset should be incremented correctly"
        }
        
        test "readFixedList decodes list correctly" {
            // Encode a fixed list with 3 elements, each taking 1 byte (no count prefix)
            let memory = createMemory [|1uy; 2uy; 3uy|]
            
            let result, offset = readFixedList memory 0<offset> 3 (fun m o -> 
                let value = m.Data.[int o]
                int64 value, o + 1<offset>
            )
            
            Expect.equal result [1L; 2L; 3L] "Decoded value should match original list"
            Expect.equal offset 3<offset> "Offset should be incremented correctly"
        }
        
        test "readMap decodes empty map correctly" {
            let memory = createMemory [|0uy|]
            let result, offset = readMap memory 0<offset> 
                (fun m o -> "key", o + 1<offset>) 
                (fun m o -> 42L, o + 1<offset>)
            
            Expect.equal result Map.empty "Decoded value should be empty map"
            Expect.equal offset 1<offset> "Offset should be incremented by 1"
        }
        
        test "readMap decodes non-empty map correctly" {
            // Simplified map encoding for testing
            // [count=2][key1-len=1]["a"][val1=1][key2-len=1]["b"][val2=2]
            let memory = createMemory [|2uy; 1uy; 97uy; 1uy; 1uy; 98uy; 2uy|]
            
            let result, offset = readMap memory 0<offset>
                (fun m o -> 
                    // Read key length and key
                    let len = int m.Data.[int o]
                    let keyStart = int (o + 1<offset>)
                    let keyBytes = Array.sub m.Data keyStart len
                    let key = Encoding.ASCII.GetString(keyBytes)
                    key, o + 1<offset> + (len * 1<offset>)
                )
                (fun m o -> 
                    // Read value
                    let value = int64 m.Data.[int o]
                    value, o + 1<offset>
                )
            
            Expect.equal result.Count 2 "Map should have 2 entries"
            Expect.equal result.["a"] 1L "First entry should be a->1"
            Expect.equal result.["b"] 2L "Second entry should be b->2"
            Expect.equal offset 7<offset> "Offset should be incremented correctly"
        }
        
        test "readUnion decodes tag and value correctly" {
            // Tag 42, followed by a value
            let memory = createMemory [|42uy; 5uy; 72uy; 101uy; 108uy; 108uy; 111uy|]
            
            let result, offset = readUnion memory 0<offset> (fun tag m o ->
                Expect.equal tag 42u "Tag should be 42"
                // Read string
                let len = int m.Data.[int o]
                let strStart = int (o + 1<offset>)
                let strBytes = Array.sub m.Data strStart len
                let str = Encoding.ASCII.GetString(strBytes)
                str, o + 1<offset> + (len * 1<offset>)
            )
            
            Expect.equal result "Hello" "Decoded value should be 'Hello'"
            Expect.equal offset 7<offset> "Offset should be incremented correctly"
        }
    ]