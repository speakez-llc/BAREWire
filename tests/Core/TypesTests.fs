module BAREWire.Tests.Core

open Expecto
open BAREWire.Core
open FSharp.UMX

[<Tests>]
let typesTests =
    testList "Types Module Tests" [
        testCase "Primitive types are correctly defined" <| fun _ ->
            // Check that all expected primitive types exist
            let primitives = [
                PrimitiveType.UInt
                PrimitiveType.Int
                PrimitiveType.U8
                PrimitiveType.U16
                PrimitiveType.U32
                PrimitiveType.U64
                PrimitiveType.I8
                PrimitiveType.I16
                PrimitiveType.I32
                PrimitiveType.I64
                PrimitiveType.F32
                PrimitiveType.F64
                PrimitiveType.Bool
                PrimitiveType.String
                PrimitiveType.Data
                PrimitiveType.FixedData 10
                PrimitiveType.Void
                PrimitiveType.Enum Map.empty
            ]
            
            // Basic validation that we can construct all primitive types
            Expect.equal primitives.Length 18 "Should have 18 primitive types"
            
        testCase "Aggregate types are correctly defined" <| fun _ ->
            // Define a basic type for testing
            let intType = Type.Primitive PrimitiveType.I32
            
            // Check that all expected aggregate types exist
            let aggregates = [
                AggregateType.Optional intType
                AggregateType.List intType
                AggregateType.FixedList (intType, 5)
                AggregateType.Map (intType, intType)
                AggregateType.Union (Map.ofList [(0u, intType); (1u, intType)])
                AggregateType.Struct [
                    { Name = "field1"; Type = intType }
                    { Name = "field2"; Type = intType }
                ]
            ]
            
            // Basic validation that we can construct all aggregate types
            Expect.equal aggregates.Length 6 "Should have 6 aggregate types"
            
        testCase "Type wraps both primitive and aggregate types" <| fun _ ->
            let primitiveType = Type.Primitive PrimitiveType.I32
            let aggregateType = Type.Aggregate (AggregateType.List primitiveType)
            let userDefinedType = Type.UserDefined "CustomType"
            
            match primitiveType with
            | Type.Primitive pt -> 
                match pt with
                | PrimitiveType.I32 -> () // Expected
                | _ -> failtest "Wrong primitive type"
            | _ -> failtest "Should be a primitive type"
            
            match aggregateType with
            | Type.Aggregate at -> 
                match at with
                | AggregateType.List t -> 
                    match t with
                    | Type.Primitive PrimitiveType.I32 -> () // Expected
                    | _ -> failtest "Wrong nested type"
                | _ -> failtest "Wrong aggregate type"
            | _ -> failtest "Should be an aggregate type"
            
            match userDefinedType with
            | Type.UserDefined name -> Expect.equal name "CustomType" "User defined type name should match"
            | _ -> failtest "Should be a user defined type"
            
        testCase "Size struct represents type sizes correctly" <| fun _ ->
            // Fixed size type
            let fixedSize = { Min = 4<bytes>; Max = Some 4<bytes>; IsFixed = true }
            
            // Variable size type with known bounds
            let boundedSize = { Min = 1<bytes>; Max = Some 10<bytes>; IsFixed = false }
            
            // Variable size type with no upper bound
            let unboundedSize = { Min = 0<bytes>; Max = None; IsFixed = false }
            
            Expect.equal fixedSize.Min 4<bytes> "Fixed size minimum should be 4 bytes"
            Expect.equal fixedSize.Max (Some 4<bytes>) "Fixed size maximum should be 4 bytes"
            Expect.isTrue fixedSize.IsFixed "Fixed size should be marked as fixed"
            
            Expect.equal boundedSize.Min 1<bytes> "Bounded size minimum should be 1 byte"
            Expect.equal boundedSize.Max (Some 10<bytes>) "Bounded size maximum should be 10 bytes"
            Expect.isFalse boundedSize.IsFixed "Bounded size should not be marked as fixed"
            
            Expect.equal unboundedSize.Min 0<bytes> "Unbounded size minimum should be 0 bytes"
            Expect.isNone unboundedSize.Max "Unbounded size should have no maximum"
            Expect.isFalse unboundedSize.IsFixed "Unbounded size should not be marked as fixed"
            
        testCase "Alignment struct represents alignment requirements correctly" <| fun _ ->
            let align1 = { Value = 1<bytes> }
            let align2 = { Value = 2<bytes> }
            let align4 = { Value = 4<bytes> }
            let align8 = { Value = 8<bytes> }
            
            Expect.equal align1.Value 1<bytes> "1-byte alignment value should be 1"
            Expect.equal align2.Value 2<bytes> "2-byte alignment value should be 2"
            Expect.equal align4.Value 4<bytes> "4-byte alignment value should be 4"
            Expect.equal align8.Value 8<bytes> "8-byte alignment value should be 8"
            
        testCase "SchemaDefinition represents schemas with validation state" <| fun _ ->
            // Create a simple schema definition
            let intType = Type.Primitive PrimitiveType.I32
            let stringType = Type.Primitive PrimitiveType.String
            let personType = Type.Aggregate (AggregateType.Struct [
                { Name = "id"; Type = intType }
                { Name = "name"; Type = stringType }
            ])
            
            let types = Map.ofList [
                ("Int", intType)
                ("String", stringType)
                ("Person", personType)
            ]
            
            let schema = { Types = types; Root = "Person" }
            
            // Test schema structure
            Expect.equal schema.Types.Count 3 "Schema should have 3 types"
            Expect.equal schema.Root "Person" "Root type should be 'Person'"
            
            // Test type retrieval
            Expect.equal (schema.Types.["Int"]) intType "Int type should match"
            Expect.equal (schema.Types.["String"]) stringType "String type should match"
            Expect.equal (schema.Types.["Person"]) personType "Person type should match"
            
        testCase "Position represents buffer positions correctly" <| fun _ ->
            let pos = { Offset = 42<offset>; Line = 10; Column = 5 }
            
            Expect.equal pos.Offset 42<offset> "Offset should be 42"
            Expect.equal pos.Line 10 "Line should be 10"
            Expect.equal pos.Column 5 "Column should be 5"
            
        testCase "Address represents memory addresses correctly" <| fun _ ->
            let addr = { Offset = 123<offset> }
            
            Expect.equal addr.Offset 123<offset> "Offset should be 123"
            
        testCase "Units of measure provide type safety" <| fun _ ->
            // This test verifies that units of measure are being correctly applied
            // by checking that operations with mixed units don't compile
            
            // These lines should compile
            let _offset = 10<offset>
            let _bytes = 20<bytes>
            
            // Adding same units works
            let _sum1 = _offset + 5<offset>
            let _sum2 = _bytes + 10<bytes>
            
            // Intentionally disabled: the following would not compile due to unit mismatch
            // let _invalid = _offset + _bytes // Error: type mismatch
            
            // Conversion between units requires explicit conversion
            let _convertedOffset = int _offset * 1<bytes>
            let _convertedBytes = int _bytes * 1<offset>
            
            // Verify we can create schema with validation state
            let _schema1 : SchemaDefinition<draft> = { Types = Map.empty; Root = "" }
            let _schema2 : SchemaDefinition<validated> = { Types = Map.empty; Root = "" }
            
            // Intentionally disabled: the following would not compile due to unit mismatch
            // let _mixed: SchemaDefinition<draft> = _schema2 // Error: type mismatch
            
            // This test passes if it compiles
            Expect.isTrue true "Units of measure should compile correctly"
    ]