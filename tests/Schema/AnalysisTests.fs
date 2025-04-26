module BAREWire.Tests.Schema.AnalysisTests

open Expecto
open BAREWire.Core
open BAREWire.Schema
open BAREWire.Schema.Definition
open BAREWire.Schema.Validation
open BAREWire.Schema.Analysis

// Helper function to create a validated schema
let createValidatedSchema schema =
    match Validation.validate schema with
    | Ok validated -> validated
    | Error errors -> failwithf "Schema validation failed: %A" errors

let getTypeSizeTests =
    testList "Analysis.getTypeSize tests" [
        test "Primitive types have correct size information" {
            let schema = createValidatedSchema (create "Root")
            
            // Test various primitive types
            let uintSize = getTypeSize schema (Primitive UInt)
            Expect.equal uintSize.Min 1<bytes> "UInt min size should be 1 byte"
            Expect.equal uintSize.Max (Some 10<bytes>) "UInt max size should be 10 bytes"
            Expect.isFalse uintSize.IsFixed "UInt should not be fixed size"
            
            let intSize = getTypeSize schema (Primitive Int)
            Expect.equal intSize.Min 1<bytes> "Int min size should be 1 byte"
            Expect.equal intSize.Max (Some 10<bytes>) "Int max size should be 10 bytes"
            Expect.isFalse intSize.IsFixed "Int should not be fixed size"
            
            let u8Size = getTypeSize schema (Primitive U8)
            Expect.equal u8Size.Min 1<bytes> "U8 min size should be 1 byte"
            Expect.equal u8Size.Max (Some 1<bytes>) "U8 max size should be 1 byte"
            Expect.isTrue u8Size.IsFixed "U8 should be fixed size"
            
            let i32Size = getTypeSize schema (Primitive I32)
            Expect.equal i32Size.Min 4<bytes> "I32 min size should be 4 bytes"
            Expect.equal i32Size.Max (Some 4<bytes>) "I32 max size should be 4 bytes"
            Expect.isTrue i32Size.IsFixed "I32 should be fixed size"
            
            let f64Size = getTypeSize schema (Primitive F64)
            Expect.equal f64Size.Min 8<bytes> "F64 min size should be 8 bytes"
            Expect.equal f64Size.Max (Some 8<bytes>) "F64 max size should be 8 bytes"
            Expect.isTrue f64Size.IsFixed "F64 should be fixed size"
            
            let boolSize = getTypeSize schema (Primitive Bool)
            Expect.equal boolSize.Min 1<bytes> "Bool min size should be 1 byte"
            Expect.equal boolSize.Max (Some 1<bytes>) "Bool max size should be 1 byte"
            Expect.isTrue boolSize.IsFixed "Bool should be fixed size"
            
            let stringSize = getTypeSize schema (Primitive String)
            Expect.equal stringSize.Min 1<bytes> "String min size should be 1 byte"
            Expect.isNone stringSize.Max "String max size should be unlimited"
            Expect.isFalse stringSize.IsFixed "String should not be fixed size"
            
            let dataSize = getTypeSize schema (Primitive Data)
            Expect.equal dataSize.Min 1<bytes> "Data min size should be 1 byte"
            Expect.isNone dataSize.Max "Data max size should be unlimited"
            Expect.isFalse dataSize.IsFixed "Data should not be fixed size"
            
            let fixedDataSize = getTypeSize schema (Primitive (FixedData 10))
            Expect.equal fixedDataSize.Min 10<bytes> "FixedData min size should match length"
            Expect.equal fixedDataSize.Max (Some 10<bytes>) "FixedData max size should match length"
            Expect.isTrue fixedDataSize.IsFixed "FixedData should be fixed size"
            
            let voidSize = getTypeSize schema (Primitive Void)
            Expect.equal voidSize.Min 0<bytes> "Void min size should be 0 bytes"
            Expect.equal voidSize.Max (Some 0<bytes>) "Void max size should be 0 bytes"
            Expect.isTrue voidSize.IsFixed "Void should be fixed size"
            
            let enumSize = getTypeSize schema (Primitive (Enum (Map.ofList [("ONE", 1UL)])))
            Expect.equal enumSize.Min 1<bytes> "Enum min size should be 1 byte"
            Expect.equal enumSize.Max (Some 10<bytes>) "Enum max size should be 10 bytes"
            Expect.isFalse enumSize.IsFixed "Enum should not be fixed size"
        }
        
        test "Optional types have correct size information" {
            let schema = createValidatedSchema (create "Root")
            
            // Optional with fixed-size inner type
            let optFixedSize = getTypeSize schema (Aggregate (Optional (Primitive I32)))
            Expect.equal optFixedSize.Min 1<bytes> "Optional min size should be 1 byte"
            Expect.equal optFixedSize.Max (Some 5<bytes>) "Optional max size should be inner size + 1"
            Expect.isFalse optFixedSize.IsFixed "Optional should not be fixed size"
            
            // Optional with variable-size inner type
            let optVarSize = getTypeSize schema (Aggregate (Optional (Primitive String)))
            Expect.equal optVarSize.Min 1<bytes> "Optional min size should be 1 byte"
            Expect.isNone optVarSize.Max "Optional max size should be unlimited when inner type is unlimited"
            Expect.isFalse optVarSize.IsFixed "Optional should not be fixed size"
        }
        
        test "List types have correct size information" {
            let schema = createValidatedSchema (create "Root")
            
            // List of any type (fixed or variable)
            let listSize = getTypeSize schema (Aggregate (List (Primitive I32)))
            Expect.equal listSize.Min 1<bytes> "List min size should be 1 byte"
            Expect.isNone listSize.Max "List max size should be unlimited"
            Expect.isFalse listSize.IsFixed "List should not be fixed size"
        }
        
        test "FixedList types have correct size information" {
            let schema = createValidatedSchema (create "Root")
            
            // FixedList with fixed-size inner type
            let fixedListFixedSize = getTypeSize schema (Aggregate (FixedList (Primitive I32, 5)))
            Expect.equal fixedListFixedSize.Min (5 * 4<bytes>) "FixedList min size should be length * inner size"
            Expect.equal fixedListFixedSize.Max (Some (5 * 4<bytes>)) "FixedList max size should be length * inner size"
            Expect.isTrue fixedListFixedSize.IsFixed "FixedList with fixed inner type should be fixed size"
            
            // FixedList with variable-size inner type
            let fixedListVarSize = getTypeSize schema (Aggregate (FixedList (Primitive String, 5)))
            Expect.equal fixedListVarSize.Min (5 * 1<bytes>) "FixedList min size should be length * inner min size"
            Expect.isNone fixedListVarSize.Max "FixedList max size should be unlimited when inner type is unlimited"
            Expect.isFalse fixedListVarSize.IsFixed "FixedList with variable inner type should not be fixed size"
        }
        
        test "Map types have correct size information" {
            let schema = createValidatedSchema (create "Root")
            
            // Map of any key/value types
            let mapSize = getTypeSize schema (Aggregate (Map (Primitive String, Primitive I32)))
            Expect.equal mapSize.Min 1<bytes> "Map min size should be 1 byte"
            Expect.isNone mapSize.Max "Map max size should be unlimited"
            Expect.isFalse mapSize.IsFixed "Map should not be fixed size"
        }
        
        test "Union types have correct size information" {
            let schema = createValidatedSchema (create "Root")
            
            // Union with fixed-size cases
            let unionFixedSize = getTypeSize schema (Aggregate (Union (Map.ofList [
                0u, Primitive I8
                1u, Primitive I16
                2u, Primitive I32
            ])))
            
            Expect.equal unionFixedSize.Min (1<bytes> + 1<bytes>) "Union min size should be tag size + smallest case"
            Expect.isSome unionFixedSize.Max "Union with fixed cases should have max size"
            Expect.isFalse unionFixedSize.IsFixed "Union should not be fixed size"
            
            // Union with variable-size cases
            let unionVarSize = getTypeSize schema (Aggregate (Union (Map.ofList [
                0u, Primitive I32
                1u, Primitive String
            ])))
            
            Expect.equal unionVarSize.Min (1<bytes> + 4<bytes>) "Union min size should be tag size + smallest case"
            Expect.isNone unionVarSize.Max "Union with unlimited cases should have unlimited max size"
            Expect.isFalse unionVarSize.IsFixed "Union should not be fixed size"
        }
        
        test "Struct types have correct size information" {
            let schema = createValidatedSchema (create "Root")
            
            // Struct with fixed-size fields
            let structFixedSize = getTypeSize schema (Aggregate (Struct [
                { Name = "field1"; Type = Primitive I8 }
                { Name = "field2"; Type = Primitive I16 }
                { Name = "field3"; Type = Primitive I32 }
            ]))
            
            Expect.equal structFixedSize.Min (1<bytes> + 2<bytes> + 4<bytes>) "Struct min size should be sum of field sizes"
            Expect.equal structFixedSize.Max (Some (1<bytes> + 2<bytes> + 4<bytes>)) "Struct max size should be sum of field sizes"
            Expect.isTrue structFixedSize.IsFixed "Struct with fixed fields should be fixed size"
            
            // Struct with variable-size fields
            let structVarSize = getTypeSize schema (Aggregate (Struct [
                { Name = "field1"; Type = Primitive I32 }
                { Name = "field2"; Type = Primitive String }
            ]))
            
            Expect.equal structVarSize.Min (4<bytes> + 1<bytes>) "Struct min size should be sum of field min sizes"
            Expect.isNone structVarSize.Max "Struct with unlimited fields should have unlimited max size"
            Expect.isFalse structVarSize.IsFixed "Struct with variable fields should not be fixed size"
        }
        
        test "User-defined types have correct size information" {
            // Create a schema with some type definitions
            let schema = createValidatedSchema (
                create "Root"
                |> addPrimitive "IntType" Int
                |> addStruct "Person" [
                    { Name = "name"; Type = Primitive String }
                    { Name = "age"; Type = Primitive I32 }
                ]
            )
            
            // Test lookup of user-defined types
            let intTypeSize = getTypeSize schema (UserDefined "IntType")
            Expect.equal intTypeSize.Min 1<bytes> "UserDefined IntType min size should match Int"
            Expect.equal intTypeSize.Max (Some 10<bytes>) "UserDefined IntType max size should match Int"
            Expect.isFalse intTypeSize.IsFixed "UserDefined IntType should not be fixed size"
            
            let personSize = getTypeSize schema (UserDefined "Person")
            Expect.equal personSize.Min (1<bytes> + 4<bytes>) "UserDefined Person min size should match struct"
            Expect.isNone personSize.Max "UserDefined Person max size should match struct"
            Expect.isFalse personSize.IsFixed "UserDefined Person should not be fixed size"
        }
    ]
    
let getTypeAlignmentTests =
    testList "Analysis.getTypeAlignment tests" [
        test "Primitive types have correct alignment" {
            let schema = createValidatedSchema (create "Root")
            
            // Test various primitive types
            let uintAlign = getTypeAlignment schema (Primitive UInt)
            Expect.equal uintAlign.Value 1<bytes> "UInt alignment should be 1 byte"
            
            let intAlign = getTypeAlignment schema (Primitive Int)
            Expect.equal intAlign.Value 1<bytes> "Int alignment should be 1 byte"
            
            let u8Align = getTypeAlignment schema (Primitive U8)
            Expect.equal u8Align.Value 1<bytes> "U8 alignment should be 1 byte"
            
            let u16Align = getTypeAlignment schema (Primitive U16)
            Expect.equal u16Align.Value 2<bytes> "U16 alignment should be 2 bytes"
            
            let u32Align = getTypeAlignment schema (Primitive U32)
            Expect.equal u32Align.Value 4<bytes> "U32 alignment should be 4 bytes"
            
            let u64Align = getTypeAlignment schema (Primitive U64)
            Expect.equal u64Align.Value 8<bytes> "U64 alignment should be 8 bytes"
            
            let i8Align = getTypeAlignment schema (Primitive I8)
            Expect.equal i8Align.Value 1<bytes> "I8 alignment should be 1 byte"
            
            let i16Align = getTypeAlignment schema (Primitive I16)
            Expect.equal i16Align.Value 2<bytes> "I16 alignment should be 2 bytes"
            
            let i32Align = getTypeAlignment schema (Primitive I32)
            Expect.equal i32Align.Value 4<bytes> "I32 alignment should be 4 bytes"
            
            let i64Align = getTypeAlignment schema (Primitive I64)
            Expect.equal i64Align.Value 8<bytes> "I64 alignment should be 8 bytes"
            
            let f32Align = getTypeAlignment schema (Primitive F32)
            Expect.equal f32Align.Value 4<bytes> "F32 alignment should be 4 bytes"
            
            let f64Align = getTypeAlignment schema (Primitive F64)
            Expect.equal f64Align.Value 8<bytes> "F64 alignment should be 8 bytes"
            
            let boolAlign = getTypeAlignment schema (Primitive Bool)
            Expect.equal boolAlign.Value 1<bytes> "Bool alignment should be 1 byte"
            
            let stringAlign = getTypeAlignment schema (Primitive String)
            Expect.equal stringAlign.Value 1<bytes> "String alignment should be 1 byte"
            
            let dataAlign = getTypeAlignment schema (Primitive Data)
            Expect.equal dataAlign.Value 1<bytes> "Data alignment should be 1 byte"
            
            let fixedDataAlign = getTypeAlignment schema (Primitive (FixedData 10))
            Expect.equal fixedDataAlign.Value 1<bytes> "FixedData alignment should be 1 byte"
            
            let voidAlign = getTypeAlignment schema (Primitive Void)
            Expect.equal voidAlign.Value 1<bytes> "Void alignment should be 1 byte"
            
            let enumAlign = getTypeAlignment schema (Primitive (Enum (Map.ofList [("ONE", 1UL)])))
            Expect.equal enumAlign.Value 1<bytes> "Enum alignment should be 1 byte"
        }
        
        test "Aggregate types have correct alignment" {
            let schema = createValidatedSchema (create "Root")
            
            // Optional type alignment matches inner type alignment
            let optIntAlign = getTypeAlignment schema (Aggregate (Optional (Primitive I32)))
            Expect.equal optIntAlign.Value 4<bytes> "Optional<I32> alignment should match I32 alignment"
            
            let optStringAlign = getTypeAlignment schema (Aggregate (Optional (Primitive String)))
            Expect.equal optStringAlign.Value 1<bytes> "Optional<String> alignment should match String alignment"
            
            // List type alignment matches inner type alignment
            let listIntAlign = getTypeAlignment schema (Aggregate (List (Primitive I32)))
            Expect.equal listIntAlign.Value 4<bytes> "List<I32> alignment should match I32 alignment"
            
            let listStringAlign = getTypeAlignment schema (Aggregate (List (Primitive String)))
            Expect.equal listStringAlign.Value 1<bytes> "List<String> alignment should match String alignment"
            
            // FixedList type alignment matches inner type alignment
            let fixedListIntAlign = getTypeAlignment schema (Aggregate (FixedList (Primitive I32, 5)))
            Expect.equal fixedListIntAlign.Value 4<bytes> "FixedList<I32, 5> alignment should match I32 alignment"
            
            // Map type alignment is the max of key and value alignment
            let mapStringIntAlign = getTypeAlignment schema (Aggregate (Map (Primitive String, Primitive I32)))
            Expect.equal mapStringIntAlign.Value 4<bytes> "Map<String, I32> alignment should be max of String and I32 alignment"
            
            let mapF32I8Align = getTypeAlignment schema (Aggregate (Map (Primitive F32, Primitive I8)))
            Expect.equal mapF32I8Align.Value 4<bytes> "Map<F32, I8> alignment should be max of F32 and I8 alignment"
            
            // Union type alignment is the max of case alignments
            let unionAlign = getTypeAlignment schema (Aggregate (Union (Map.ofList [
                0u, Primitive I8
                1u, Primitive I32
                2u, Primitive F64
            ])))
            Expect.equal unionAlign.Value 8<bytes> "Union alignment should be max of case alignments"
            
            // Struct type alignment is the max of field alignments
            let structAlign = getTypeAlignment schema (Aggregate (Struct [
                { Name = "field1"; Type = Primitive I8 }
                { Name = "field2"; Type = Primitive I32 }
                { Name = "field3"; Type = Primitive F64 }
            ]))
            Expect.equal structAlign.Value 8<bytes> "Struct alignment should be max of field alignments"
        }
        
        test "User-defined types have correct alignment" {
            // Create a schema with some type definitions
            let schema = createValidatedSchema (
                create "Root"
                |> addPrimitive "IntType" Int
                |> addStruct "Person" [
                    { Name = "name"; Type = Primitive String }
                    { Name = "age"; Type = Primitive I32 }
                ]
            )
            
            // Test lookup of user-defined types
            let intTypeAlign = getTypeAlignment schema (UserDefined "IntType")
            Expect.equal intTypeAlign.Value 1<bytes> "UserDefined IntType alignment should match Int alignment"
            
            let personAlign = getTypeAlignment schema (UserDefined "Person")
            Expect.equal personAlign.Value 4<bytes> "UserDefined Person alignment should match struct alignment"
        }
    ]
    
let areTypesCompatibleTests =
    testList "Analysis.areTypesCompatible tests" [
        test "Identical primitive types are compatible" {
            let schema = createValidatedSchema (create "Root")
            
            Expect.isTrue (areTypesCompatible schema schema (Primitive Int) (Primitive Int)) 
                "Int should be compatible with Int"
                
            Expect.isTrue (areTypesCompatible schema schema (Primitive String) (Primitive String)) 
                "String should be compatible with String"
                
            Expect.isTrue (areTypesCompatible schema schema (Primitive (FixedData 10)) (Primitive (FixedData 10))) 
                "FixedData with same length should be compatible"
        }
        
        test "Different primitive types are not compatible" {
            let schema = createValidatedSchema (create "Root")
            
            Expect.isFalse (areTypesCompatible schema schema (Primitive Int) (Primitive UInt)) 
                "Int should not be compatible with UInt"
                
            Expect.isFalse (areTypesCompatible schema schema (Primitive String) (Primitive Data)) 
                "String should not be compatible with Data"
                
            Expect.isFalse (areTypesCompatible schema schema (Primitive (FixedData 10)) (Primitive (FixedData 20))) 
                "FixedData with different lengths should not be compatible"
        }
        
        test "User-defined types with the same name are compatible" {
            let schema = createValidatedSchema (create "Root")
            
            Expect.isTrue (areTypesCompatible schema schema (UserDefined "TypeA") (UserDefined "TypeA")) 
                "UserDefined types with the same name should be compatible"
                
            Expect.isFalse (areTypesCompatible schema schema (UserDefined "TypeA") (UserDefined "TypeB")) 
                "UserDefined types with different names should not be compatible"
        }
        
        test "Optional types are compatible if inner types are compatible" {
            let schema = createValidatedSchema (create "Root")
            
            Expect.isTrue (areTypesCompatible schema schema 
                (Aggregate (Optional (Primitive Int))) 
                (Aggregate (Optional (Primitive Int)))) 
                "Optional<Int> should be compatible with Optional<Int>"
                
            Expect.isFalse (areTypesCompatible schema schema 
                (Aggregate (Optional (Primitive Int))) 
                (Aggregate (Optional (Primitive String)))) 
                "Optional<Int> should not be compatible with Optional<String>"
        }
        
        test "List types are compatible if inner types are compatible" {
            let schema = createValidatedSchema (create "Root")
            
            Expect.isTrue (areTypesCompatible schema schema 
                (Aggregate (List (Primitive Int))) 
                (Aggregate (List (Primitive Int)))) 
                "List<Int> should be compatible with List<Int>"
                
            Expect.isFalse (areTypesCompatible schema schema 
                (Aggregate (List (Primitive Int))) 
                (Aggregate (List (Primitive String)))) 
                "List<Int> should not be compatible with List<String>"
        }
        
        test "FixedList types are compatible if inner types and lengths are compatible" {
            let schema = createValidatedSchema (create "Root")
            
            Expect.isTrue (areTypesCompatible schema schema 
                (Aggregate (FixedList (Primitive Int, 5))) 
                (Aggregate (FixedList (Primitive Int, 5)))) 
                "FixedList<Int, 5> should be compatible with FixedList<Int, 5>"
                
            Expect.isFalse (areTypesCompatible schema schema 
                (Aggregate (FixedList (Primitive Int, 5))) 
                (Aggregate (FixedList (Primitive Int, 10)))) 
                "FixedList<Int, 5> should not be compatible with FixedList<Int, 10>"
                
            Expect.isFalse (areTypesCompatible schema schema 
                (Aggregate (FixedList (Primitive Int, 5))) 
                (Aggregate (FixedList (Primitive String, 5)))) 
                "FixedList<Int, 5> should not be compatible with FixedList<String, 5>"
        }
        
        test "Map types are compatible if key and value types are compatible" {
            let schema = createValidatedSchema (create "Root")
            
            Expect.isTrue (areTypesCompatible schema schema 
                (Aggregate (Map (Primitive String, Primitive Int))) 
                (Aggregate (Map (Primitive String, Primitive Int)))) 
                "Map<String, Int> should be compatible with Map<String, Int>"
                
            Expect.isFalse (areTypesCompatible schema schema 
                (Aggregate (Map (Primitive String, Primitive Int))) 
                (Aggregate (Map (Primitive Int, Primitive Int)))) 
                "Map<String, Int> should not be compatible with Map<Int, Int>"
                
            Expect.isFalse (areTypesCompatible schema schema 
                (Aggregate (Map (Primitive String, Primitive Int))) 
                (Aggregate (Map (Primitive String, Primitive String)))) 
                "Map<String, Int> should not be compatible with Map<String, String>"
        }
        
        test "Union types are compatible if all cases match" {
            let schema = createValidatedSchema (create "Root")
            
            let unionA = Aggregate (Union (Map.ofList [
                0u, Primitive Int
                1u, Primitive String
            ]))
            
            let unionB = Aggregate (Union (Map.ofList [
                0u, Primitive Int
                1u, Primitive String
            ]))
            
            let unionC = Aggregate (Union (Map.ofList [
                0u, Primitive Int
                1u, Primitive UInt
            ]))
            
            let unionD = Aggregate (Union (Map.ofList [
                0u, Primitive Int
                1u, Primitive String
                2u, Primitive Bool
            ]))
            
            Expect.isTrue (areTypesCompatible schema schema unionA unionB) 
                "Unions with the same cases should be compatible"
                
            Expect.isFalse (areTypesCompatible schema schema unionA unionC) 
                "Unions with different case types should not be compatible"
                
            Expect.isFalse (areTypesCompatible schema schema unionA unionD) 
                "Unions with different number of cases should not be compatible"
        }
        
        test "Struct types are compatible if all fields match in name, order, and type" {
            let schema = createValidatedSchema (create "Root")
            
            let structA = Aggregate (Struct [
                { Name = "field1"; Type = Primitive Int }
                { Name = "field2"; Type = Primitive String }
            ])
            
            let structB = Aggregate (Struct [
                { Name = "field1"; Type = Primitive Int }
                { Name = "field2"; Type = Primitive String }
            ])
            
            let structC = Aggregate (Struct [
                { Name = "field1"; Type = Primitive Int }
                { Name = "field2"; Type = Primitive Bool }
            ])
            
            let structD = Aggregate (Struct [
                { Name = "field1"; Type = Primitive Int }
                { Name = "differentName"; Type = Primitive String }
            ])
            
            let structE = Aggregate (Struct [
                { Name = "field2"; Type = Primitive String }
                { Name = "field1"; Type = Primitive Int }
            ])
            
            Expect.isTrue (areTypesCompatible schema schema structA structB) 
                "Structs with the same fields should be compatible"
                
            Expect.isFalse (areTypesCompatible schema schema structA structC) 
                "Structs with different field types should not be compatible"
                
            Expect.isFalse (areTypesCompatible schema schema structA structD) 
                "Structs with different field names should not be compatible"
                
            Expect.isFalse (areTypesCompatible schema schema structA structE) 
                "Structs with different field order should not be compatible"
        }
        
        test "Different aggregate types are not compatible" {
            let schema = createValidatedSchema (create "Root")
            
            Expect.isFalse (areTypesCompatible schema schema 
                (Aggregate (List (Primitive Int))) 
                (Aggregate (Optional (Primitive Int)))) 
                "List<Int> should not be compatible with Optional<Int>"
                
            Expect.isFalse (areTypesCompatible schema schema 
                (Aggregate (Map (Primitive String, Primitive Int))) 
                (Aggregate (Struct [{ Name = "field"; Type = Primitive Int }]))) 
                "Map<String, Int> should not be compatible with Struct"
        }
    ]
    
let checkCompatibilityTests =
    testList "Analysis.checkCompatibility tests" [
        test "Identical schemas are fully compatible" {
            let schema = createValidatedSchema (
                create "Message"
                |> addPrimitive "IntType" Int
                |> addStruct "Person" [
                    { Name = "name"; Type = Primitive String }
                    { Name = "age"; Type = Primitive Int }
                ]
                |> addStruct "Message" [
                    { Name = "sender"; Type = UserDefined "Person" }
                    { Name = "content"; Type = Primitive String }
                ]
            )
            
            let result = checkCompatibility schema schema
            
            Expect.equal result FullyCompatible "Identical schemas should be fully compatible"
        }
        
        test "Union schemas with added cases are backward compatible" {
            let oldSchema = createValidatedSchema (
                create "Result"
                |> addUnion "Result" (Map.ofList [
                    0u, Primitive Int
                    1u, Primitive String
                ])
            )
            
            let newSchema = createValidatedSchema (
                create "Result"
                |> addUnion "Result" (Map.ofList [
                    0u, Primitive Int
                    1u, Primitive String
                    2u, Primitive Bool
                ])
            )
            
            let result = checkCompatibility oldSchema newSchema
            
            Expect.equal result BackwardCompatible "Adding union cases should be backward compatible"
        }
        
        test "Union schemas with removed cases are forward compatible" {
            let oldSchema = createValidatedSchema (
                create "Result"
                |> addUnion "Result" (Map.ofList [
                    0u, Primitive Int
                    1u, Primitive String
                    2u, Primitive Bool
                ])
            )
            
            let newSchema = createValidatedSchema (
                create "Result"
                |> addUnion "Result" (Map.ofList [
                    0u, Primitive Int
                    1u, Primitive String
                ])
            )
            
            let result = checkCompatibility oldSchema newSchema
            
            Expect.equal result ForwardCompatible "Removing union cases should be forward compatible"
        }
        
        test "Union schemas with changed case types are incompatible" {
            let oldSchema = createValidatedSchema (
                create "Result"
                |> addUnion "Result" (Map.ofList [
                    0u, Primitive Int
                    1u, Primitive String
                ])
            )
            
            let newSchema = createValidatedSchema (
                create "Result"
                |> addUnion "Result" (Map.ofList [
                    0u, Primitive Int
                    1u, Primitive Bool
                ])
            )
            
            let result = checkCompatibility oldSchema newSchema
            
            match result with
            | Incompatible reasons -> 
                Expect.isNonEmpty reasons "Should have at least one incompatibility reason"
            | _ -> failtest "Changed case types should be incompatible"
        }
        
        test "Struct schemas with added fields are backward compatible" {
            let oldSchema = createValidatedSchema (
                create "Person"
                |> addStruct "Person" [
                    { Name = "name"; Type = Primitive String }
                    { Name = "age"; Type = Primitive Int }
                ]
            )
            
            let newSchema = createValidatedSchema (
                create "Person"
                |> addStruct "Person" [
                    { Name = "name"; Type = Primitive String }
                    { Name = "age"; Type = Primitive Int }
                    { Name = "email"; Type = Primitive String }
                ]
            )
            
            let result = checkCompatibility oldSchema newSchema
            
            Expect.equal result BackwardCompatible "Adding struct fields should be backward compatible"
        }
        
        test "Struct schemas with changed field types are incompatible" {
            let oldSchema = createValidatedSchema (
                create "Person"
                |> addStruct "Person" [
                    { Name = "name"; Type = Primitive String }
                    { Name = "age"; Type = Primitive Int }
                ]
            )
            
            let newSchema = createValidatedSchema (
                create "Person"
                |> addStruct "Person" [
                    { Name = "name"; Type = Primitive String }
                    { Name = "age"; Type = Primitive F32 }
                ]
            )
            
            let result = checkCompatibility oldSchema newSchema
            
            match result with
            | Incompatible reasons -> 
                Expect.isNonEmpty reasons "Should have at least one incompatibility reason"
            | _ -> failtest "Changed field types should be incompatible"
        }
        
        test "Changing root type makes schemas incompatible" {
            let oldSchema = createValidatedSchema (
                create "IntType"
                |> addPrimitive "IntType" Int
            )
            
            let newSchema = createValidatedSchema (
                create "StringType"
                |> addPrimitive "StringType" String
            )
            
            let result = checkCompatibility oldSchema newSchema
            
            match result with
            | Incompatible reasons -> 
                Expect.isNonEmpty reasons "Should have at least one incompatibility reason"
            | _ -> failtest "Different root types should be incompatible"
        }
    ]

[<Tests>]
let analysisTests =
    testList "Schema Analysis Module Tests" [
        getTypeSizeTests
        getTypeAlignmentTests
        areTypesCompatibleTests
        checkCompatibilityTests
    ]