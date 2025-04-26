namespace BAREWire.Core

open FSharp.UMX

/// <summary>
/// Units of measure for BAREWire types
/// </summary>
[<AutoOpen>]
module Measures =
    /// <summary>
    /// Marker for byte offset within a buffer
    /// </summary>
    [<Measure>] type offset

    /// <summary>
    /// Marker for byte count
    /// </summary>
    [<Measure>] type bytes
    
    /// <summary>
    /// Marker for a typed memory region
    /// </summary>
    [<Measure>] type region
    
    /// <summary>
    /// Base marker for schema types
    /// </summary>
    [<Measure>] type schema
    
    /// <summary>
    /// Schema validation state
    /// </summary>
    [<Measure>] type validated
    
    /// <summary>
    /// Schema draft state (not yet validated)
    /// </summary>
    [<Measure>] type draft

/// <summary>
/// BARE primitive types
/// </summary>
type PrimitiveType =
    | UInt
    | Int
    | U8
    | U16
    | U32
    | U64
    | I8
    | I16
    | I32
    | I64
    | F32
    | F64
    | Bool
    | String
    | Data
    | FixedData of length:int
    | Void
    | Enum of values:Map<string, uint64>

/// <summary>
/// BARE aggregate types
/// </summary>
type AggregateType =
    | Optional of Type
    | List of Type
    | FixedList of Type * length:int  
    | Map of keyType:Type * valueType:Type
    | Union of cases:Map<uint, Type>
    | Struct of fields:StructField list

/// <summary>
/// A field in a BARE struct
/// </summary>
and StructField = {
    /// <summary>
    /// The name of the field
    /// </summary>
    Name: string
    
    /// <summary>
    /// The type of the field
    /// </summary>
    Type: Type
}

/// <summary>
/// A BARE type (either primitive or aggregate)
/// </summary>
and Type =
    | Primitive of PrimitiveType
    | Aggregate of AggregateType
    | UserDefined of string

/// <summary>
/// Size information for a BARE type
/// </summary>
[<Struct>]
type Size = {
    /// <summary>
    /// The minimum size in bytes
    /// </summary>
    Min: int<bytes>
    
    /// <summary>
    /// The maximum size in bytes (None if variable-length)
    /// </summary>
    Max: option<int<bytes>>
    
    /// <summary>
    /// Whether the size is fixed (Min = Max)
    /// </summary>
    IsFixed: bool
}

/// <summary>
/// Alignment requirements for a type
/// </summary>
[<Struct>]
type Alignment = {
    /// <summary>
    /// Alignment in bytes (must be a power of 2)
    /// </summary>
    Value: int<bytes>
}

/// <summary>
/// A schema definition with type safety
/// </summary>
/// <typeparam name="'state">The validation state of the schema</typeparam>
type SchemaDefinition<[<Measure>] 'state> = {
    /// <summary>
    /// The types defined in this schema
    /// </summary>
    Types: Map<string, Type>
    
    /// <summary>
    /// The name of the root type
    /// </summary>
    Root: string
}

/// <summary>
/// A position within a binary buffer
/// </summary>
[<Struct>]
type Position = {
    /// <summary>
    /// The byte offset
    /// </summary>
    Offset: int<offset>
    
    /// <summary>
    /// The line number (1-based)
    /// </summary>
    Line: int
    
    /// <summary>
    /// The column number (1-based)
    /// </summary>
    Column: int
}

/// <summary>
/// An address within a memory region
/// </summary>
/// <typeparam name="'region">The memory region measure type</typeparam>
[<Struct>]
type Address<[<Measure>] 'region> = {
    /// <summary>
    /// The absolute offset within the memory region
    /// </summary>
    Offset: int<offset>
}