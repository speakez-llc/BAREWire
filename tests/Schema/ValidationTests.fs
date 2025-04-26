module BAREWire.Tests.Schema.ValidationTests

open Expecto
open BAREWire.Core
open BAREWire.Schema
open BAREWire.Schema.Definition
open BAREWire.Schema.Validation

let errorToStringTests =
    testList "ValidationError.errorToString tests" [
        test "CyclicTypeReference produces correct error message" {
            let error = CyclicTypeReference "TestType"
            let message = errorToString error
            
            Expect.stringContains message "Cyclic type reference" "Message should mention cyclic reference"
            Expect.stringContains message "TestType" "Message should contain the type name"
        }
        
        test "UndefinedType produces correct error message" {
            let error = UndefinedType "MissingType"
            let message = errorToString error
            
            Expect.stringContains message "Undefined type" "Message should mention undefined type"
            Expect.stringContains message "MissingType" "Message should contain the type name"
        }
        
        test "InvalidVoidUsage produces correct error message" {
            let error = InvalidVoidUsage "TestStruct.field"
            let message = errorToString error
            
            Expect.stringContains message "Invalid void usage" "Message should mention invalid void usage"
            Expect.stringContains message "TestStruct.field" "Message should contain the location"
        }
        
        test "InvalidMapKeyType produces correct error message" {
            let error = InvalidMapKeyType "F32"
            let message = errorToString error
            
            Expect.stringContains message "Invalid map key type" "Message should mention invalid map key"
            Expect.stringContains message "F32" "Message should contain the type name"
        }
        
        test "EmptyEnum produces correct error message" {
            let error = EmptyEnum
            let message = errorToString error
            
            Expect.stringContains message "Empty enum" "Message should mention empty enum"
        }
        
        test "EmptyUnion produces correct error message" {
            let error = EmptyUnion
            let message = errorToString error
            
            Expect.stringContains message "Empty union" "Message should mention empty union"
        }
        
        test "EmptyStruct produces correct error message" {
            let error = EmptyStruct
            let message = errorToString error
            
            Expect.stringContains message "Empty struct" "Message should mention empty struct"
        }
        
        test "InvalidFixedLength produces correct error message" {
            let error = InvalidFixedLength (0, "TestList")
            let message = errorToString error
            
            Expect.stringContains message "Invalid fixed length" "Message should mention invalid length"
            Expect.stringContains message "0" "Message should contain the length value"
            Expect.stringContains message "TestList" "Message should contain the location"
        }
    ]

let typePathToStringTests =
    testList "Validation.typePathToString tests" [
        test "Empty path returns empty string" {
            let path = []
            let result = typePathToString path
            
            Expect.equal result "" "Empty path should return empty string"
        }
        
        test "TypeRoot only path returns root name" {
            let path = [TypeRoot "RootType"]
            let result = typePathToString path
            
            Expect.equal result "RootType" "Root-only path should return root name"
        }
        
        test "Complex path returns correctly formatted string" {
            let path = [
                MapValue
                MapKey
                StructField "field2"
                StructField "field1"
                TypeRoot "RootType"
            ]
            let result = typePathToString path
            
            Expect.equal result "RootType.field1.field2.key.value" "Complex path should be properly formatted"
        }
        
        test "Union case path is properly formatted" {
            let path = [
                ListItem
                UnionCase
                TypeRoot "UnionType"
            ]
            let result = typePathToString path
            
            Expect.equal result "UnionType.case.item" "Union case path should be properly formatted"
        }
        
        test "Optional value path is properly formatted" {
            let path = [
                OptionalValue
                TypeRoot "OptType"
            ]
            let result = typePathToString path
            
            Expect.equal result "OptType.optional" "Optional value path should be properly formatted"
        }
    ]
    
let isUnionContextTests =
    testList "Validation.isUnionContext tests" [
        test "Returns true when path contains UnionCase" {
            let path = [TypeRoot "Root"; UnionCase; StructField "field"]
            
            Expect.isTrue (isUnionContext path) "Should return true when path contains UnionCase"
        }
        
        test "Returns false when path does not contain UnionCase" {
            let path = [TypeRoot "Root"; StructField "field"; ListItem]
            
            Expect.isFalse (isUnionContext path) "Should return false when path does not contain UnionCase"
        }
        
        test "Returns false for empty path" {
            let path = []
            
            Expect.isFalse (isUnionContext path) "Should return false for empty path"
        }
    ]
    
let typeToStringTests =
    testList "Validation.typeToString tests" [
        test "Primitive types are properly converted to strings" {
            Expect.equal (typeToString (Primitive UInt)) "UInt" "UInt should be correctly converted"
            Expect.equal (typeToString (Primitive Int)) "Int" "Int should be correctly converted"
            Expect.equal (typeToString (Primitive Bool)) "Bool" "Bool should be correctly converted"
            Expect.equal (typeToString (Primitive String)) "String" "String should be correctly converted"
            Expect.equal (typeToString (Primitive Void)) "Void" "Void should be correctly converted"
        }
        
        test "Fixed data type includes length" {
            Expect.equal (typeToString (Primitive (FixedData 10))) "FixedData(10)" "FixedData should include length"
        }
        
        test "Aggregate types are properly converted to strings" {
            Expect.equal (typeToString (Aggregate (Optional (Primitive Int)))) "Optional<Int>" "Optional should be correctly converted"
            Expect.equal (typeToString (Aggregate (List (Primitive String)))) "List<String>" "List should be correctly converted"
            Expect.equal (typeToString (Aggregate (FixedList (Primitive Bool, 5)))) "FixedList<Bool, 5>" "FixedList should be correctly converted"
            
            let mapType = Aggregate (Map (Primitive String, Primitive Int))
            Expect.equal (typeToString mapType) "Map<String, Int>" "Map should be correctly converted"
            
            Expect.equal (typeToString (Aggregate (Union Map.empty))) "Union" "Union should be correctly converted"
            Expect.equal (typeToString (Aggregate (Struct []))) "Struct" "Struct should be correctly converted"
        }
        
        test "UserDefined type returns the type name" {
            Expect.equal (typeToString (UserDefined "CustomType")) "CustomType" "UserDefined should return the type name"
        }
    ]
    
let getReferencedTypesTests =
    testList "Validation.getReferencedTypes tests" [
        test "Primitive types return empty sequence" {
            let referencedTypes = getReferencedTypes (Primitive Int) |> Seq.toList
            
            Expect.isEmpty referencedTypes "Primitive types should not reference other types"
        }
        
        test "UserDefined type returns the referenced type name" {
            let referencedTypes = getReferencedTypes (UserDefined "ReferencedType") |> Seq.toList
            
            Expect.equal referencedTypes ["ReferencedType"] "UserDefined should reference the named type"
        }
        
        test "Optional type returns references from inner type" {
            let optType = Aggregate (Optional (UserDefined "InnerType"))
            let referencedTypes = getReferencedTypes optType |> Seq.toList
            
            Expect.equal referencedTypes ["InnerType"] "Optional should reference its inner type"
        }
        
        test "List type returns references from inner type" {
            let listType = Aggregate (List (UserDefined "ItemType"))
            let referencedTypes = getReferencedTypes listType |> Seq.toList
            
            Expect.equal referencedTypes ["ItemType"] "List should reference its item type"
        }
        
        test "FixedList type returns references from inner type" {
            let fixedListType = Aggregate (FixedList (UserDefined "ItemType", 5))
            let referencedTypes = getReferencedTypes fixedListType |> Seq.toList
            
            Expect.equal referencedTypes ["ItemType"] "FixedList should reference its item type"
        }
        
        test "Map type returns references from key and value types" {
            let mapType = Aggregate (Map (UserDefined "KeyType", UserDefined "ValueType"))
            let referencedTypes = getReferencedTypes mapType |> Seq.toList
            
            Expect.contains referencedTypes "KeyType" "Map should reference its key type"
            Expect.contains referencedTypes "ValueType" "Map should reference its value type"
            Expect.equal referencedTypes.Length 2 "Map should reference exactly two types"
        }
        
        test "Union type returns references from all cases" {
            let unionType = Aggregate (Union (Map.ofList [
                0u, UserDefined "Type1"
                1u, UserDefined "Type2"
            ]))
            let referencedTypes = getReferencedTypes unionType |> Seq.toList
            
            Expect.contains referencedTypes "Type1" "Union should reference its first case type"
            Expect.contains referencedTypes "Type2" "Union should reference its second case type"
            Expect.equal referencedTypes.Length 2 "Union should reference exactly two types"
        }
        
        test "Struct type returns references from all fields" {
            let structType = Aggregate (Struct [
                { Name = "field1"; Type = UserDefined "Type1" }
                { Name = "field2"; Type = UserDefined "Type2" }
            ])
            let referencedTypes = getReferencedTypes structType |> Seq.toList
            
            Expect.contains referencedTypes "Type1" "Struct should reference its first field type"
            Expect.contains referencedTypes "Type2" "Struct should reference its second field type"
            Expect.equal referencedTypes.Length 2 "Struct should reference exactly two types"
        }
    ]
    
let validateTypeInvariantsTests =
    testList "Validation.validateTypeInvariants tests" [
        test "Valid types return empty error sequence" {
            let validStruct = Aggregate (Struct [
                { Name = "field"; Type = Primitive Int }
            ])
            
            let errors = validateTypeInvariants "ValidStruct" validStruct |> Seq.toList
            
            Expect.isEmpty errors "Valid types should not produce errors"
        }
        
        test "Void outside union context produces error" {
            let invalidStruct = Aggregate (Struct [
                { Name = "voidField"; Type = Primitive Void }
            ])
            
            let errors = validateTypeInvariants "InvalidStruct" invalidStruct |> Seq.toList
            
            Expect.isNonEmpty errors "Void in struct should produce an error"
            Expect.exists errors (function | InvalidVoidUsage _ -> true | _ -> false) "Error should be InvalidVoidUsage"
        }
        
        test "Void inside union context is valid" {
            let validUnion = Aggregate (Union (Map.ofList [
                0u, Primitive Int
                1u, Primitive Void
            ]))
            
            let errors = validateTypeInvariants "ValidUnion" validUnion |> Seq.toList
            
            Expect.isEmpty errors "Void in union should not produce errors"
        }
        
        test "Empty enum produces error" {
            let emptyEnum = Primitive (Enum Map.empty)
            
            let errors = validateTypeInvariants "EmptyEnum" emptyEnum |> Seq.toList
            
            Expect.isNonEmpty errors "Empty enum should produce an error"
            Expect.exists errors (function | EmptyEnum -> true | _ -> false) "Error should be EmptyEnum"
        }
        
        test "Empty union produces error" {
            let emptyUnion = Aggregate (Union Map.empty)
            
            let errors = validateTypeInvariants "EmptyUnion" emptyUnion |> Seq.toList
            
            Expect.isNonEmpty errors "Empty union should produce an error"
            Expect.exists errors (function | EmptyUnion -> true | _ -> false) "Error should be EmptyUnion"
        }
        
        test "Empty struct produces error" {
            let emptyStruct = Aggregate (Struct [])
            
            let errors = validateTypeInvariants "EmptyStruct" emptyStruct |> Seq.toList
            
            Expect.isNonEmpty errors "Empty struct should produce an error"
            Expect.exists errors (function | EmptyStruct -> true | _ -> false) "Error should be EmptyStruct"
        }
        
        test "Invalid map key types produce errors" {
            let invalidKeyTypes = [
                Primitive F32
                Primitive F64
                Primitive Data
                Primitive (FixedData 10)
                Primitive Void
            ]
            
            for keyType in invalidKeyTypes do
                let invalidMap = Aggregate (Map (keyType, Primitive Int))
                let typeName = typeToString keyType
                
                let errors = validateTypeInvariants "InvalidMap" invalidMap |> Seq.toList
                
                Expect.isNonEmpty errors $"{typeName} as map key should produce an error"
                Expect.exists errors (function | InvalidMapKeyType _ -> true | _ -> false) "Error should be InvalidMapKeyType"
        }
        
        test "Valid map key types do not produce errors" {
            let validKeyTypes = [
                Primitive UInt
                Primitive Int
                Primitive U8
                Primitive I16
                Primitive Bool
                Primitive String
            ]
            
            for keyType in validKeyTypes do
                let validMap = Aggregate (Map (keyType, Primitive Int))
                let typeName = typeToString keyType
                
                let errors = validateTypeInvariants "ValidMap" validMap |> Seq.toList
                
                Expect.isEmpty errors $"{typeName} as map key should not produce an error"
        }
        
        test "Negative fixed list length produces error" {
            let invalidFixedList = Aggregate (FixedList (Primitive Int, -5))
            
            let errors = validateTypeInvariants "InvalidFixedList" invalidFixedList |> Seq.toList
            
            Expect.isNonEmpty errors "Negative fixed list length should produce an error"
            Expect.exists errors (function | InvalidFixedLength _ -> true | _ -> false) "Error should be InvalidFixedLength"
        }
        
        test "Zero fixed list length produces error" {
            let invalidFixedList = Aggregate (FixedList (Primitive Int, 0))
            
            let errors = validateTypeInvariants "InvalidFixedList" invalidFixedList |> Seq.toList
            
            Expect.isNonEmpty errors "Zero fixed list length should produce an error"
            Expect.exists errors (function | InvalidFixedLength _ -> true | _ -> false) "Error should be InvalidFixedLength"
        }
    ]
    
let validateTests =
    testList "Validation.validate tests" [
        test "Valid schema passes validation" {
            let schema = 
                create "Message"
                |> addPrimitive "IntType" Int
                |> addStruct "Person" [
                    { Name = "name"; Type = Primitive String }
                    { Name = "age"; Type = Primitive Int }
                ]
                |> addList "IntList" (Primitive Int)
                |> addUnion "Result" (Map.ofList [
                    0u, UserDefined "Person"
                    1u, Primitive Void
                ])
            
            match validate schema with
            | Ok validated -> 
                // Success, check that content is preserved
                Expect.equal validated.Root schema.Root "Root should be preserved"
                Expect.equal validated.Types schema.Types "Types should be preserved"
            | Error errors -> 
                failtest $"Valid schema failed validation with errors: {errors}"
        }
        
        test "Schema with non-existent root type fails validation" {
            let schema = 
                create "NonExistentRoot"
                |> addPrimitive "IntType" Int
            
            match validate schema with
            | Ok _ -> failtest "Schema with non-existent root should fail validation"
            | Error errors -> 
                Expect.isNonEmpty errors "Should have at least one error"
                Expect.exists errors (function | UndefinedType "NonExistentRoot" -> true | _ -> false) 
                    "Error should be UndefinedType for root"
        }
        
        test "Schema with undefined referenced type fails validation" {
            let schema = 
                create "Person"
                |> addStruct "Person" [
                    { Name = "name"; Type = Primitive String }
                    { Name = "friend"; Type = UserDefined "NonExistentType" }
                ]
            
            match validate schema with
            | Ok _ -> failtest "Schema with undefined referenced type should fail validation"
            | Error errors -> 
                Expect.isNonEmpty errors "Should have at least one error"
                Expect.exists errors (function | UndefinedType "NonExistentType" -> true | _ -> false) 
                    "Error should be UndefinedType for the referenced type"
        }
        
        test "Schema with cyclic references fails validation" {
            let schema = 
                create "Person"
                |> addStruct "Person" [
                    { Name = "name"; Type = Primitive String }
                    { Name = "friend"; Type = UserDefined "Person" } // Self-reference
                ]
            
            match validate schema with
            | Ok _ -> failtest "Schema with cyclic references should fail validation"
            | Error errors -> 
                Expect.isNonEmpty errors "Should have at least one error"
                Expect.exists errors (function | CyclicTypeReference "Person" -> true | _ -> false) 
                    "Error should be CyclicTypeReference"
        }
        
        test "Schema with multiple cycles fails validation" {
            let schema = 
                create "A"
                |> addStruct "A" [
                    { Name = "b"; Type = UserDefined "B" }
                ]
                |> addStruct "B" [
                    { Name = "c"; Type = UserDefined "C" }
                ]
                |> addStruct "C" [
                    { Name = "a"; Type = UserDefined "A" }
                ]
            
            match validate schema with
            | Ok _ -> failtest "Schema with multiple cycles should fail validation"
            | Error errors -> 
                Expect.isNonEmpty errors "Should have at least one error"
                Expect.exists errors (function | CyclicTypeReference _ -> true | _ -> false) 
                    "Error should be CyclicTypeReference"
        }
        
        test "Schema with invalid type invariants fails validation" {
            let schema = 
                create "Root"
                |> addPrimitive "Root" (Enum Map.empty) // Empty enum
            
            match validate schema with
            | Ok _ -> failtest "Schema with invalid type invariants should fail validation"
            | Error errors -> 
                Expect.isNonEmpty errors "Should have at least one error"
                Expect.exists errors (function | EmptyEnum -> true | _ -> false) 
                    "Error should be EmptyEnum"
        }
        
        test "Schema with invalid nested type invariants fails validation" {
            let schema = 
                create "Root"
                |> addStruct "Root" [
                    { Name = "map"; Type = Aggregate (Map (Primitive F32, Primitive Int)) } // Invalid map key
                ]
            
            match validate schema with
            | Ok _ -> failtest "Schema with invalid nested type invariants should fail validation"
            | Error errors -> 
                Expect.isNonEmpty errors "Should have at least one error"
                Expect.exists errors (function | InvalidMapKeyType _ -> true | _ -> false) 
                    "Error should be InvalidMapKeyType"
        }
    ]

[<Tests>]
let validationTests =
    testList "Schema Validation Module Tests" [
        errorToStringTests
        typePathToStringTests
        isUnionContextTests
        typeToStringTests
        getReferencedTypesTests
        validateTypeInvariantsTests
        validateTests
    ]