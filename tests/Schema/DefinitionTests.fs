module BAREWire.Tests.Schema.DefinitionTests

open Expecto
open BAREWire.Core
open BAREWire.Schema
open BAREWire.Schema.Definition

let createTests =
    testList "Definition.create tests" [
        test "create returns a schema with empty types and specified root" {
            let rootName = "TestRoot"
            let schema = create rootName
            
            Expect.equal schema.Types Map.empty "Types should be empty"
            Expect.equal schema.Root rootName "Root should be set to the provided name"
        }
    ]
    
let addPrimitiveTests =
    testList "Definition.addPrimitive tests" [
        test "addPrimitive adds a primitive type to the schema" {
            let schema = create "Root"
            let typeName = "MyInt"
            let primitiveType = Int
            
            let updatedSchema = addPrimitive typeName primitiveType schema
            
            Expect.isTrue (Map.containsKey typeName updatedSchema.Types) "Type should be added to the schema"
            Expect.equal updatedSchema.Types.[typeName] (Primitive primitiveType) "Type should be a primitive"
        }
        
        test "addPrimitive replaces existing type with the same name" {
            let schema = create "Root" |> addPrimitive "ExistingType" Int
            let updatedSchema = addPrimitive "ExistingType" UInt schema
            
            Expect.equal updatedSchema.Types.["ExistingType"] (Primitive UInt) "Type should be replaced"
        }
    ]

let addStructTests =
    testList "Definition.addStruct tests" [
        test "addStruct adds a struct type to the schema" {
            let schema = create "Root"
            let typeName = "MyStruct"
            let fields = [
                { Name = "field1"; Type = Primitive Int }
                { Name = "field2"; Type = Primitive String }
            ]
            
            let updatedSchema = addStruct typeName fields schema
            
            Expect.isTrue (Map.containsKey typeName updatedSchema.Types) "Type should be added to the schema"
            match updatedSchema.Types.[typeName] with
            | Aggregate (Struct structFields) ->
                Expect.equal structFields fields "Struct fields should match"
            | _ -> failtest "Type should be a struct"
        }
    ]
    
let addUnionTests =
    testList "Definition.addUnion tests" [
        test "addUnion adds a union type to the schema" {
            let schema = create "Root"
            let typeName = "MyUnion"
            let cases = Map.ofList [
                0u, Primitive Int
                1u, Primitive String
            ]
            
            let updatedSchema = addUnion typeName cases schema
            
            Expect.isTrue (Map.containsKey typeName updatedSchema.Types) "Type should be added to the schema"
            match updatedSchema.Types.[typeName] with
            | Aggregate (Union unionCases) ->
                Expect.equal unionCases cases "Union cases should match"
            | _ -> failtest "Type should be a union"
        }
    ]
    
let addOptionalTests =
    testList "Definition.addOptional tests" [
        test "addOptional adds an optional type to the schema" {
            let schema = create "Root"
            let typeName = "MyOptional"
            let innerType = Primitive String
            
            let updatedSchema = addOptional typeName innerType schema
            
            Expect.isTrue (Map.containsKey typeName updatedSchema.Types) "Type should be added to the schema"
            match updatedSchema.Types.[typeName] with
            | Aggregate (Optional optType) ->
                Expect.equal optType innerType "Optional inner type should match"
            | _ -> failtest "Type should be an optional"
        }
    ]
    
let addListTests =
    testList "Definition.addList tests" [
        test "addList adds a list type to the schema" {
            let schema = create "Root"
            let typeName = "MyList"
            let itemType = Primitive Int
            
            let updatedSchema = addList typeName itemType schema
            
            Expect.isTrue (Map.containsKey typeName updatedSchema.Types) "Type should be added to the schema"
            match updatedSchema.Types.[typeName] with
            | Aggregate (List listType) ->
                Expect.equal listType itemType "List item type should match"
            | _ -> failtest "Type should be a list"
        }
    ]
    
let addFixedListTests =
    testList "Definition.addFixedList tests" [
        test "addFixedList adds a fixed-length list type to the schema" {
            let schema = create "Root"
            let typeName = "MyFixedList"
            let itemType = Primitive Int
            let length = 10
            
            let updatedSchema = addFixedList typeName itemType length schema
            
            Expect.isTrue (Map.containsKey typeName updatedSchema.Types) "Type should be added to the schema"
            match updatedSchema.Types.[typeName] with
            | Aggregate (FixedList (fixedListType, fixedLength)) ->
                Expect.equal fixedListType itemType "Fixed list item type should match"
                Expect.equal fixedLength length "Fixed list length should match"
            | _ -> failtest "Type should be a fixed list"
        }
    ]
    
let addMapTests =
    testList "Definition.addMap tests" [
        test "addMap adds a map type to the schema" {
            let schema = create "Root"
            let typeName = "MyMap"
            let keyType = Primitive String
            let valueType = Primitive Int
            
            let updatedSchema = addMap typeName keyType valueType schema
            
            Expect.isTrue (Map.containsKey typeName updatedSchema.Types) "Type should be added to the schema"
            match updatedSchema.Types.[typeName] with
            | Aggregate (Map (mapKeyType, mapValueType)) ->
                Expect.equal mapKeyType keyType "Map key type should match"
                Expect.equal mapValueType valueType "Map value type should match"
            | _ -> failtest "Type should be a map"
        }
    ]
    
let addEnumTests =
    testList "Definition.addEnum tests" [
        test "addEnum adds an enum type to the schema" {
            let schema = create "Root"
            let typeName = "MyEnum"
            let values = Map.ofList [
                "ONE", 1UL
                "TWO", 2UL
            ]
            
            let updatedSchema = addEnum typeName values schema
            
            Expect.isTrue (Map.containsKey typeName updatedSchema.Types) "Type should be added to the schema"
            match updatedSchema.Types.[typeName] with
            | Primitive (Enum enumValues) ->
                Expect.equal enumValues values "Enum values should match"
            | _ -> failtest "Type should be an enum"
        }
    ]
    
let addTypeRefTests =
    testList "Definition.addTypeRef tests" [
        test "addTypeRef adds a reference to a user-defined type" {
            let schema = create "Root"
            let typeName = "MyTypeRef"
            let referencedTypeName = "ReferencedType"
            
            let updatedSchema = addTypeRef typeName referencedTypeName schema
            
            Expect.isTrue (Map.containsKey typeName updatedSchema.Types) "Type should be added to the schema"
            match updatedSchema.Types.[typeName] with
            | UserDefined refTypeName ->
                Expect.equal refTypeName referencedTypeName "Referenced type name should match"
            | _ -> failtest "Type should be a user-defined type reference"
        }
    ]
    
let typeExistsTests =
    testList "Definition.typeExists tests" [
        test "typeExists returns true for existing types" {
            let schema = create "Root" |> addPrimitive "ExistingType" Int
            
            Expect.isTrue (typeExists "ExistingType" schema) "Should return true for existing type"
        }
        
        test "typeExists returns false for non-existing types" {
            let schema = create "Root"
            
            Expect.isFalse (typeExists "NonExistingType" schema) "Should return false for non-existing type"
        }
    ]
    
let getTypeTests =
    testList "Definition.getType tests" [
        test "getType returns Some for existing types" {
            let schema = create "Root" |> addPrimitive "ExistingType" Int
            
            match getType "ExistingType" schema with
            | Some typ -> 
                Expect.equal typ (Primitive Int) "Should return the correct type"
            | None -> failtest "Should return Some for existing type"
        }
        
        test "getType returns None for non-existing types" {
            let schema = create "Root"
            
            Expect.isNone (getType "NonExistingType" schema) "Should return None for non-existing type"
        }
    ]
    
let removeTypeTests =
    testList "Definition.removeType tests" [
        test "removeType removes existing type from the schema" {
            let schema = create "Root" |> addPrimitive "TypeToRemove" Int
            
            let updatedSchema = removeType "TypeToRemove" schema
            
            Expect.isFalse (Map.containsKey "TypeToRemove" updatedSchema.Types) "Type should be removed"
        }
        
        test "removeType does nothing for non-existing types" {
            let schema = create "Root" |> addPrimitive "ExistingType" Int
            
            let updatedSchema = removeType "NonExistingType" schema
            
            Expect.equal updatedSchema.Types schema.Types "Types should remain unchanged"
        }
    ]
    
let setRootTests =
    testList "Definition.setRoot tests" [
        test "setRoot changes the root type of the schema" {
            let schema = create "OldRoot"
            
            let updatedSchema = setRoot "NewRoot" schema
            
            Expect.equal updatedSchema.Root "NewRoot" "Root should be updated"
        }
    ]
    
let getTypeNamesTests =
    testList "Definition.getTypeNames tests" [
        test "getTypeNames returns all type names in the schema" {
            let schema = create "Root"
                         |> addPrimitive "Type1" Int
                         |> addPrimitive "Type2" String
            
            let typeNames = getTypeNames schema
            
            Expect.contains typeNames "Type1" "Should contain the first type"
            Expect.contains typeNames "Type2" "Should contain the second type"
            Expect.equal typeNames.Length 2 "Should contain exactly two types"
        }
        
        test "getTypeNames returns empty list for empty schema" {
            let schema = create "Root"
            
            let typeNames = getTypeNames schema
            
            Expect.isEmpty typeNames "Should return empty list"
        }
    ]
    
let getTypesTests =
    testList "Definition.getTypes tests" [
        test "getTypes returns all types in the schema" {
            let schema = create "Root"
                         |> addPrimitive "Type1" Int
                         |> addPrimitive "Type2" String
            
            let types = getTypes schema
            
            Expect.contains types ("Type1", Primitive Int) "Should contain the first type"
            Expect.contains types ("Type2", Primitive String) "Should contain the second type"
            Expect.equal types.Length 2 "Should contain exactly two types"
        }
        
        test "getTypes returns empty list for empty schema" {
            let schema = create "Root"
            
            let types = getTypes schema
            
            Expect.isEmpty types "Should return empty list"
        }
    ]
    
let copyTypeTests =
    testList "Definition.copyType tests" [
        test "copyType copies a type from source schema to target schema" {
            let sourceSchema = create "SourceRoot" |> addPrimitive "TypeToCopy" Int
            let targetSchema = create "TargetRoot"
            
            let updatedTargetSchema = copyType "TypeToCopy" sourceSchema targetSchema
            
            Expect.isTrue (Map.containsKey "TypeToCopy" updatedTargetSchema.Types) "Type should be copied to target schema"
            Expect.equal updatedTargetSchema.Types.["TypeToCopy"] (Primitive Int) "Copied type should match source"
        }
        
        test "copyType does nothing if type doesn't exist in source schema" {
            let sourceSchema = create "SourceRoot"
            let targetSchema = create "TargetRoot"
            
            let updatedTargetSchema = copyType "NonExistingType" sourceSchema targetSchema
            
            Expect.equal updatedTargetSchema targetSchema "Target schema should remain unchanged"
        }
    ]
    
let mergeTests =
    testList "Definition.merge tests" [
        test "merge combines types from both schemas" {
            let schema1 = create "Root1" |> addPrimitive "Type1" Int
            let schema2 = create "Root2" |> addPrimitive "Type2" String
            
            let mergedSchema = merge schema1 schema2
            
            Expect.equal mergedSchema.Root "Root1" "Root should be from schema1"
            Expect.isTrue (Map.containsKey "Type1" mergedSchema.Types) "Should contain types from schema1"
            Expect.isTrue (Map.containsKey "Type2" mergedSchema.Types) "Should contain types from schema2"
        }
        
        test "merge gives priority to schema2 on type conflicts" {
            let schema1 = create "Root1" |> addPrimitive "Conflicting" Int
            let schema2 = create "Root2" |> addPrimitive "Conflicting" String
            
            let mergedSchema = merge schema1 schema2
            
            Expect.equal mergedSchema.Types.["Conflicting"] (Primitive String) "Type from schema2 should take precedence"
        }
    ]
    
let cloneTests =
    testList "Definition.clone tests" [
        test "clone creates a new schema with the same content" {
            let original = create "Root" 
                           |> addPrimitive "Type1" Int
                           |> addStruct "Type2" [{ Name = "field"; Type = Primitive String }]
            
            let cloned = clone original
            
            Expect.equal cloned.Root original.Root "Root should be the same"
            Expect.equal cloned.Types original.Types "Types should be the same"
            // Ensure it's a new object
            Expect.isTrue (obj.ReferenceEquals(cloned, original) |> not) "Should be a new object"
        }
    ]
    
let unsafeCastTests =
    testList "Definition.unsafeCast tests" [
        test "unsafeCast changes the state type of the schema" {
            let draftSchema = create "Root" |> addPrimitive "Type" Int
            
            // This is just a type-level test, no runtime behavior to verify
            let castedSchema = unsafeCast<draft, validated> draftSchema
            
            // Check that the content is the same
            Expect.equal castedSchema.Root draftSchema.Root "Root should be the same"
            Expect.equal castedSchema.Types draftSchema.Types "Types should be the same"
        }
    ]

[<Tests>]
let definitionTests =
    testList "Schema Definition Module Tests" [
        createTests
        addPrimitiveTests
        addStructTests
        addUnionTests
        addOptionalTests
        addListTests
        addFixedListTests
        addMapTests
        addEnumTests
        addTypeRefTests
        typeExistsTests
        getTypeTests
        removeTypeTests
        setRootTests
        getTypeNamesTests
        getTypesTests
        copyTypeTests
        mergeTests
        cloneTests
        unsafeCastTests
    ]