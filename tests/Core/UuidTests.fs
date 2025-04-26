module BAREWire.Tests.Core

open Expecto
open BAREWire.Core
open BAREWire.Core.Uuid

[<Tests>]
let uuidTests =
    testList "Uuid Module Tests" [
        testCase "nil UUID has all zero bytes" <| fun _ ->
            let nilUuid = nil
            
            Expect.equal nilUuid.Data.Length 16 "Nil UUID should have 16 bytes"
            for b in nilUuid.Data do
                Expect.equal b 0uy "All bytes in nil UUID should be zero"
                
        testCase "nil UUID string representation is all zeros" <| fun _ ->
            let nilString = toString nil
            let expected = "00000000-0000-0000-0000-000000000000"
            
            Expect.equal nilString expected "Nil UUID string should be all zeros with hyphens"
            
        testCase "newUuid generates a version 4 UUID" <| fun _ ->
            let uuid = newUuid()
            
            Expect.equal uuid.Data.Length 16 "UUID should have 16 bytes"
            
            // Check version (should be 4 for random)
            let version = getVersion uuid
            Expect.equal version 4 "UUID should be version 4"
            
            // Check variant (should be 1 for RFC 4122)
            let variant = getVariant uuid
            Expect.equal variant 1 "UUID variant should be RFC 4122"
            
        testCase "newUuidV5 generates a version 5 UUID" <| fun _ ->
            let namespace = newUuid()
            let name = "test"
            let uuid = newUuidV5 namespace name
            
            Expect.equal uuid.Data.Length 16 "UUID should have 16 bytes"
            
            // Check version (should be 5 for name-based with SHA-1)
            let version = getVersion uuid
            Expect.equal version 5 "UUID should be version 5"
            
            // Check variant (should be 1 for RFC 4122)
            let variant = getVariant uuid
            Expect.equal variant 1 "UUID variant should be RFC 4122"
            
        testCase "Consistent UUIDs for same inputs" <| fun _ ->
            let namespace = newUuid()
            let name = "test"
            
            let uuid1 = newUuidV5 namespace name
            let uuid2 = newUuidV5 namespace name
            
            Expect.isTrue (equals uuid1 uuid2) "Same inputs should generate same UUID"
            
        testCase "Different names produce different UUIDs" <| fun _ ->
            let namespace = newUuid()
            let name1 = "test1"
            let name2 = "test2"
            
            let uuid1 = newUuidV5 namespace name1
            let uuid2 = newUuidV5 namespace name2
            
            Expect.isFalse (equals uuid1 uuid2) "Different names should produce different UUIDs"
            
        testCase "Different namespaces produce different UUIDs" <| fun _ ->
            let namespace1 = newUuid()
            let namespace2 = newUuid()
            let name = "test"
            
            let uuid1 = newUuidV5 namespace1 name
            let uuid2 = newUuidV5 namespace2 name
            
            Expect.isFalse (equals uuid1 uuid2) "Different namespaces should produce different UUIDs"
            
        testCase "toString and fromString are inverse operations" <| fun _ ->
            let original = newUuid()
            let str = toString original
            let roundTrip = fromString str
            
            Expect.isTrue (equals original roundTrip) "UUID should round-trip through string representation"
            
        testCase "fromString validates input format" <| fun _ ->
            // Valid UUID
            let validUuid = "123e4567-e89b-12d3-a456-426614174000"
            Expect.doesNotThrow (fun () -> fromString validUuid |> ignore) "Valid UUID format should not throw"
            
            // Invalid formats
            let invalidLength = "123e4567-e89b-12d3-a456-42661417400"  // Too short
            Expect.throws (fun () -> fromString invalidLength |> ignore) "Invalid length should throw"
            
            let invalidHyphens = "123e4567e89b-12d3-a456-426614174000"  // Misplaced hyphens
            Expect.throws (fun () -> fromString invalidHyphens |> ignore) "Invalid hyphen placement should throw"
            
            let invalidChars = "123e4567-e89b-12g3-a456-426614174000"  // Contains 'g' which is not hex
            Expect.throws (fun () -> fromString invalidChars |> ignore) "Invalid characters should throw"
            
        testCase "toByteArray and fromByteArray are inverse operations" <| fun _ ->
            let original = newUuid()
            let bytes = toByteArray original
            let roundTrip = fromByteArray bytes
            
            Expect.isTrue (equals original roundTrip) "UUID should round-trip through byte array"
            Expect.equal bytes.Length 16 "Byte array should be 16 bytes"
            
        testCase "fromByteArray validates input length" <| fun _ ->
            let invalidBytes = Array.zeroCreate 15  // Too short
            Expect.throws (fun () -> fromByteArray invalidBytes |> ignore) "Invalid byte array length should throw"
            
        testCase "getVersion returns correct version" <| fun _ ->
            // Create UUIDs with different versions
            let randomUuid = newUuid()  // Version 4
            let namedUuid = newUuidV5 nil "test"  // Version 5
            
            Expect.equal (getVersion randomUuid) 4 "Random UUID should be version 4"
            Expect.equal (getVersion namedUuid) 5 "Named UUID should be version 5"
            
        testCase "getVariant returns correct variant" <| fun _ ->
            let uuid = newUuid()
            
            Expect.equal (getVariant uuid) 1 "UUID should have RFC 4122 variant"
            
        testCase "equals compares UUIDs correctly" <| fun _ ->
            let uuid1 = fromString "f81d4fae-7dec-11d0-a765-00a0c91e6bf6"
            let uuid2 = fromString "f81d4fae-7dec-11d0-a765-00a0c91e6bf6"  // Same as uuid1
            let uuid3 = fromString "123e4567-e89b-12d3-a456-426614174000"  // Different
            
            Expect.isTrue (equals uuid1 uuid2) "Identical UUIDs should be equal"
            Expect.isFalse (equals uuid1 uuid3) "Different UUIDs should not be equal"
            
        testCase "UUIDs match System.Guid format" <| fun _ ->
            // Create a UUID and convert to string
            let uuid = fromString "f81d4fae-7dec-11d0-a765-00a0c91e6bf6"
            let uuidStr = toString uuid
            
            // Create a System.Guid with the same value
            let guid = System.Guid.Parse("f81d4fae-7dec-11d0-a765-00a0c91e6bf6")
            let guidStr = guid.ToString()
            
            // Compare lowercase to ensure case doesn't affect equality
            Expect.equal (uuidStr.ToLowerInvariant()) (guidStr.ToLowerInvariant()) 
                "UUID string representation should match System.Guid format"
    ]