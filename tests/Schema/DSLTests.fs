module BAREWire.Tests.Schema.DSLTests

open Expecto
open BAREWire.Core
open BAREWire.Schema
open BAREWire.Schema.Definition
open BAREWire.Schema.DSL

let schemaTests =
    testList "DSL.schema tests" [
        test "schema creates a new schema with the specified root" {
            let rootName = "TestRoot"
            let result = schema rootName
            
            Expect.equal result.Root rootName "Root name should match"
            Expect.isEmpty result.Types "Types should be empty"
        }
    ]
    
let primitiveTypeTests =
    testList "DSL primitive type tests" [
        test "Primitive types are defined correctly" {
            Expect.equal uint (Primitive UInt) "uint should be UInt primitive"
            Expect.equal int (Primitive Int) "int should be Int primitive"
            Expect.equal u8 (Primitive U8) "u8 should be U8 primitive"
            Expect.equal u16 (Primitive U16) "u16 should be U16 primitive"
            Expect.equal u32 (Primitive U32) "u32 should be U32 primitive"
            Expect.equal u64 (Primitive U64) "u64 should be U64 primitive"
            Expect.equal i8 (Primitive I8) "i8 should be I8 primitive"
            Expect.equal i16 (Primitive I16) "i16 should be I16 primitive"
            Expect.equal i32 (Primitive I32) "i32 should be I32 primitive"
            Expect.equal i64 (Primitive I64) "i64 should be I64 primitive"
            Expect.equal string (Primitive String) "string should be String primitive"
            Expect.equal data (Primitive Data) "data should be Data primitive"
            Expect.equal bool (Primitive Bool) "bool should be Bool primitive"
            Expect.equal void (Primitive Void) "void should be Void primitive"
            Expect.equal f32 (Primitive F32) "f32 should be F32 primitive"
            Expect.equal f64 (Primitive F64) "f64 should be F64 primitive"
        }
        
        test "fixedData creates correct FixedData primitive with length" {
            let length = 10
            let result = fixedData length
            
            match result with
            | Primitive (FixedData l) -> Expect.equal l length "Length should match"
            | _ -> failtest "Should be a FixedData primitive"
        }
    ]
    
let aggregateTypeTests =
    testList "DSL aggregate type tests" [
        test "optional creates correct Optional aggregate" {
            let innerType = Primitive Int
            let result = optional innerType
            
            match result with
            | Aggregate (Optional t) -> Expect.equal t innerType "Inner type should match"
            | _ -> failtest "Should be an Optional aggregate"
        }
        
        test "list creates correct List aggregate" {
            let itemType = Primitive String
            let result = list itemType
            
            match result with
            | Aggregate (List t) -> Expect.equal t itemType "Item type should match"
            | _ -> failtest "Should be a List aggregate"
        }
        
        test "fixedList creates correct FixedList aggregate with length" {
            let itemType = Primitive Bool
            let length = 5
            let result = fixedList itemType length
            
            match result with
            | Aggregate (FixedList (t, l)) -> 
                Expect.equal t itemType "Item type should match"
                Expect.equal l length "Length should match"
            | _ -> failtest "Should be a FixedList aggregate"
        }
        
        test "map creates correct Map aggregate with key and value types" {
            let keyType = Primitive String
            let valueType = Primitive Int
            let result = map keyType valueType
            
            match result with
            | Aggregate (Map (k, v)) -> 
                Expect.equal k keyType "Key type should match"
                Expect.equal v valueType "Value type should match"
            | _ -> failtest "Should be a Map aggregate"
        }
        
        test "field creates correct struct field" {
            let name = "testField"
            let typ = Primitive Int
            let result = field name typ
            
            Expect.equal result.Name name "Field name should match"
            Expect.equal result.Type typ "Field type should match"
        }
        
        test "struct' creates correct Struct aggregate with fields" {
            let fields = [
                field "field1" (Primitive Int)
                field "field2" (Primitive String)
            ]
            let result = struct' fields
            
            match result with
            | Aggregate (Struct f) -> Expect.equal f fields "Fields should match"
            | _ -> failtest "Should be a Struct aggregate"
        }
        
        test "union creates correct Union aggregate with cases" {
            let cases = Map.ofList [
                0u, Primitive Int
                1u, Primitive String
            ]
            let result = union cases
            
            match result with
            | Aggregate (Union c) -> Expect.equal c cases "Cases should match"
            | _ -> failtest "Should be a Union aggregate"
        }
        
        test "enum creates correct Enum primitive with values" {
            let values = Map.ofList [
                "ONE", 1UL
                "TWO", 2UL
            ]
            let result = enum values
            
            match result with
            | Primitive (Enum v) -> Expect.equal v values "Values should match"
            | _ -> failtest "Should be an Enum primitive"
        }
    ]
    
let userTypeTests =
    testList "DSL.userType tests" [
        test "userType creates correct UserDefined reference" {
            let typeName = "CustomType"
            let result = userType typeName
            
            match result with
            | UserDefined name -> Expect.equal name typeName "Type name should match"
            | _ -> failtest "Should be a UserDefined type"
        }
    ]
    
let withTypeTests =
    testList "DSL.withType tests" [
        test "withType adds type to schema" {
            let s = schema "Root"
            let typeName = "IntType"
            let typ = Primitive Int
            
            let result = withType typeName typ s
            
            Expect.isTrue (Map.containsKey typeName result.Types) "Type should be added to schema"
            Expect.equal result.Types.[typeName] typ "Added type should match"
        }
        
        test "withType replaces existing type with same name" {
            let s = schema "Root" |> withType "ExistingType" (Primitive Int)
            let newType = Primitive String
            
            let result = withType "ExistingType" newType s
            
            Expect.equal result.Types.["ExistingType"] newType "Type should be replaced"
        }
    ]
    
let withRootTests =
    testList "DSL.withRoot tests" [
        test "withRoot changes root type name" {
            let s = schema "OldRoot"
            let newRoot = "NewRoot"
            
            let result = withRoot newRoot s
            
            Expect.equal result.Root newRoot "Root should be changed"
        }
    ]
    
let validateTests =
    testList "DSL.validate tests" [
        test "validate passes valid schema" {
            let s = 
                schema "Person"
                |> withType "Person" (struct' [
                    field "name" string
                    field "age" int
                ])
            
            match validate s with
            | Ok validated -> 
                Expect.equal validated.Root s.Root "Root should be preserved"
                Expect.equal validated.Types s.Types "Types should be preserved"
            | Error errors -> 
                failtest $"Valid schema failed validation with errors: {errors}"
        }
        
        test "validate fails invalid schema" {
            let s = 
                schema "Person"
                |> withType "Person" (struct' []) // Empty struct is invalid
            
            match validate s with
            | Ok _ -> failtest "Invalid schema should fail validation"
            | Error errors -> 
                Expect.isNonEmpty errors "Should have at least one error"
                Expect.exists errors (function | EmptyStruct -> true | _ -> false) "Error should be EmptyStruct"
        }
    ]
    
let complexSchemaTests =
    testList "Complex schema building tests" [
        test "Build and validate a complex schema" {
            // Build a more complex schema for a messaging system
            let messagingSchema =
                schema "MessageList"
                |> withType "UserId" string
                |> withType "MessageId" string
                |> withType "Timestamp" int
                |> withType "MessageContent" string
                |> withType "Attachment" (optional data)
                |> withType "MessageType" (enum (Map.ofList [
                    "TEXT", 0UL
                    "IMAGE", 1UL
                    "VIDEO", 2UL
                    "FILE", 3UL
                ]))
                |> withType "Message" (struct' [
                    field "id" (userType "MessageId")
                    field "sender" (userType "UserId")
                    field "timestamp" (userType "Timestamp")
                    field "content" (userType "MessageContent")
                    field "attachment" (userType "Attachment")
                    field "type" (userType "MessageType")
                ])
                |> withType "MessageList" (list (userType "Message"))
            
            // Validate the schema
            match validate messagingSchema with
            | Ok validated -> 
                // Success, verify the structure
                Expect.equal validated.Root "MessageList" "Root should be MessageList"
                Expect.equal (Map.count validated.Types) 7 "Should have 7 types"
                
                // Check that all expected types exist
                let expectedTypes = [
                    "UserId"; "MessageId"; "Timestamp"; "MessageContent"; 
                    "Attachment"; "MessageType"; "Message"; "MessageList"
                ]
                
                for typeName in expectedTypes do
                    if typeName <> "MessageList" then // MessageList is the root, not in Types
                        Expect.isTrue (Map.containsKey typeName validated.Types) 
                            $"Schema should contain {typeName}"
            
            | Error errors -> 
                failtest $"Valid complex schema failed validation with errors: {errors}"
        }
    ]

[<Tests>]
let dslTests =
    testList "Schema DSL Module Tests" [
        schemaTests
        primitiveTypeTests
        aggregateTypeTests
        userTypeTests
        withTypeTests
        withRootTests
        validateTests
        complexSchemaTests
    ]