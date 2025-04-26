namespace BAREWire.Schema

open BAREWire.Core
open BAREWire.Schema.Definition
open BAREWire.Schema.Validation

/// <summary>
/// Domain-specific language for building schema definitions in a fluent style
/// </summary>
module DSL =
    /// <summary>
    /// Starts building a schema with the specified root type name
    /// </summary>
    /// <param name="rootName">The name of the root type</param>
    /// <returns>A new draft schema</returns>
    let schema rootName = create rootName
    
    /// <summary>
    /// Defines a BARE uint type (variable-length unsigned integer)
    /// </summary>
    let uint = Primitive UInt
    
    /// <summary>
    /// Defines a BARE int type (variable-length signed integer)
    /// </summary>
    let int = Primitive Int
    
    /// <summary>
    /// Defines a BARE u8 type (8-bit unsigned integer)
    /// </summary>
    let u8 = Primitive U8
    
    /// <summary>
    /// Defines a BARE u16 type (16-bit unsigned integer)
    /// </summary>
    let u16 = Primitive U16
    
    /// <summary>
    /// Defines a BARE u32 type (32-bit unsigned integer)
    /// </summary>
    let u32 = Primitive U32
    
    /// <summary>
    /// Defines a BARE u64 type (64-bit unsigned integer)
    /// </summary>
    let u64 = Primitive U64
    
    /// <summary>
    /// Defines a BARE i8 type (8-bit signed integer)
    /// </summary>
    let i8 = Primitive I8
    
    /// <summary>
    /// Defines a BARE i16 type (16-bit signed integer)
    /// </summary>
    let i16 = Primitive I16
    
    /// <summary>
    /// Defines a BARE i32 type (32-bit signed integer)
    /// </summary>
    let i32 = Primitive I32
    
    /// <summary>
    /// Defines a BARE i64 type (64-bit signed integer)
    /// </summary>
    let i64 = Primitive I64
    
    /// <summary>
    /// Defines a BARE string type (UTF-8 encoded string)
    /// </summary>
    let string = Primitive String
    
    /// <summary>
    /// Defines a BARE data type (variable-length byte array)
    /// </summary>
    let data = Primitive Data
    
    /// <summary>
    /// Defines a BARE fixed data type (fixed-length byte array)
    /// </summary>
    /// <param name="length">The fixed length of the array</param>
    /// <returns>A fixed data type</returns>
    let fixedData length = Primitive (FixedData length)
    
    /// <summary>
    /// Defines a BARE bool type (boolean value)
    /// </summary>
    let bool = Primitive Bool
    
    /// <summary>
    /// Defines a BARE void type (no data)
    /// </summary>
    let void = Primitive Void
    
    /// <summary>
    /// Defines a BARE f32 type (32-bit floating point)
    /// </summary>
    let f32 = Primitive F32
    
    /// <summary>
    /// Defines a BARE f64 type (64-bit floating point)
    /// </summary>
    let f64 = Primitive F64
    
    /// <summary>
    /// Defines a BARE optional type (value that may be present or absent)
    /// </summary>
    /// <param name="typ">The type that is optional</param>
    /// <returns>An optional type</returns>
    let optional typ = Aggregate (Optional typ)
    
    /// <summary>
    /// Defines a BARE list type (variable-length array of values)
    /// </summary>
    /// <param name="typ">The type of items in the list</param>
    /// <returns>A list type</returns>
    let list typ = Aggregate (List typ)
    
    /// <summary>
    /// Defines a BARE fixed-length list type (fixed-length array of values)
    /// </summary>
    /// <param name="typ">The type of items in the list</param>
    /// <param name="length">The fixed length of the list</param>
    /// <returns>A fixed-length list type</returns>
    let fixedList typ length = Aggregate (FixedList (typ, length))
    
    /// <summary>
    /// Defines a BARE map type (key-value mapping)
    /// </summary>
    /// <param name="keyType">The type of keys in the map</param>
    /// <param name="valueType">The type of values in the map</param>
    /// <returns>A map type</returns>
    let map keyType valueType = Aggregate (Map (keyType, valueType))
    
    /// <summary>
    /// Creates a field definition for a struct
    /// </summary>
    /// <param name="name">The name of the field</param>
    /// <param name="typ">The type of the field</param>
    /// <returns>A struct field definition</returns>
    let field name typ = { Name = name; Type = typ }
    
    /// <summary>
    /// Defines a BARE struct type (record with named fields)
    /// </summary>
    /// <param name="fields">The list of fields in the struct</param>
    /// <returns>A struct type</returns>
    let struct' fields = Aggregate (Struct fields)
    
    /// <summary>
    /// Defines a BARE union type (tagged variant type)
    /// </summary>
    /// <param name="cases">The map of tags to variant types</param>
    /// <returns>A union type</returns>
    let union cases = Aggregate (Union cases)
    
    /// <summary>
    /// Defines a BARE enum type (named constants with numeric values)
    /// </summary>
    /// <param name="values">The map of enum names to numeric values</param>
    /// <returns>An enum type</returns>
    let enum values = Primitive (Enum values)
    
    /// <summary>
    /// References a user-defined type by name
    /// </summary>
    /// <param name="name">The name of the referenced type</param>
    /// <returns>A reference to a user-defined type</returns>
    let userType name = UserDefined name
    
    /// <summary>
    /// Adds a type to a schema
    /// </summary>
    /// <param name="name">The name to assign to the type</param>
    /// <param name="typ">The type definition</param>
    /// <param name="schema">The schema to add to</param>
    /// <returns>A new schema with the type added</returns>
    let withType name typ schema =
        { schema with Types = Map.add name typ schema.Types }
    
    /// <summary>
    /// Sets the root type of a schema
    /// </summary>
    /// <param name="rootName">The name of the root type</param>
    /// <param name="schema">The schema to modify</param>
    /// <returns>A new schema with the updated root type</returns>
    let withRoot rootName schema =
        { schema with Root = rootName }
    
    /// <summary>
    /// Validates a schema
    /// </summary>
    /// <param name="schema">The schema to validate</param>
    /// <returns>A result containing the validated schema or validation errors</returns>
    let validate schema =
        Validation.validate schema
    
