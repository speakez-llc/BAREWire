namespace BAREWire.Encoding

open FSharp.UMX
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Encoding.Encoder
open BAREWire.Encoding.Decoder

/// <summary>
/// Combined encoding and decoding operations for BARE types
/// </summary>
module Codec =
    /// <summary>
    /// Encodes a value based on its schema
    /// </summary>
    /// <param name="schema">The validated schema definition</param>
    /// <param name="value">The value to encode</param>
    /// <param name="buffer">The buffer to write to</param>
    /// <returns>A result indicating success or an encoding error</returns>
    let encode (schema: SchemaDefinition<validated>) (value: 'T) (buffer: Buffer<'a>): Result<unit> =
        try
            let rec encodeWithType (typ: Type) (value: obj) (buffer: Buffer<'a>): unit =
                match typ with
                | Primitive primType ->
                    match primType with
                    | UInt -> writeUInt buffer (unbox<uint64> value)
                    | Int -> writeInt buffer (unbox<int64> value)
                    | U8 -> writeU8 buffer (unbox<byte> value)
                    | U16 -> writeU16 buffer (unbox<uint16> value)
                    | U32 -> writeU32 buffer (unbox<uint32> value)
                    | U64 -> writeU64 buffer (unbox<uint64> value)
                    | I8 -> writeI8 buffer (unbox<sbyte> value)
                    | I16 -> writeI16 buffer (unbox<int16> value)
                    | I32 -> writeI32 buffer (unbox<int32> value)
                    | I64 -> writeI64 buffer (unbox<int64> value)
                    | F32 -> writeF32 buffer (unbox<float32> value)
                    | F64 -> writeF64 buffer (unbox<float> value)
                    | Bool -> writeBool buffer (unbox<bool> value)
                    | String -> writeString buffer (unbox<string> value)
                    | Data -> writeData buffer (unbox<byte[]> value)
                    | FixedData length -> writeFixedData buffer (unbox<byte[]> value) length
                    | Void -> ()
                    | Enum _ -> writeEnum buffer (unbox<uint64> value)
                    
                | Aggregate aggType ->
                    match aggType with
                    | Optional innerType ->
                        let opt = unbox<option<obj>> value
                        writeOptional buffer opt (fun buf v -> encodeWithType innerType v buf)
                    
                    | List innerType ->
                        let list = unbox<obj seq> value
                        writeList buffer list (fun buf v -> encodeWithType innerType v buf)
                    
                    | FixedList (innerType, length) ->
                        let list = unbox<obj seq> value
                        writeFixedList buffer list length (fun buf v -> encodeWithType innerType v buf)
                    
                    | Map (keyType, valueType) ->
                        let map = unbox<(obj * obj) seq> value
                        writeMap buffer map 
                            (fun buf k -> encodeWithType keyType k buf)
                            (fun buf v -> encodeWithType valueType v buf)
                    
                    | Union cases ->
                        let (tag, innerValue) = unbox<uint * obj> value
                        let innerType = Map.find tag cases
                        writeUnion buffer tag innerValue (fun buf v -> encodeWithType innerType v buf)
                    
                    | Struct fields ->
                        let record = value
                        for field in fields do
                            let fieldValue = getRecordField record field.Name
                            encodeWithType field.Type fieldValue buffer
                
                | UserDefined typeName ->
                    // Look up the type in the schema
                    let actualType = Map.find typeName schema.Types
                    encodeWithType actualType value buffer
            
            // Start encoding with the root type
            let rootType = Map.find schema.Root schema.Types
            encodeWithType rootType (box value) buffer
            Ok ()
        with ex ->
            Error (encodingError ex.Message)
    
    /// <summary>
    /// Gets the value of a field from a record using reflection
    /// </summary>
    /// <param name="record">The record object</param>
    /// <param name="fieldName">The name of the field to retrieve</param>
    /// <returns>The field value as an object</returns>
    /// <exception cref="System.Exception">Thrown when the field is not found</exception>
    and getRecordField (record: obj) (fieldName: string): obj =
        let recordType = record.GetType()
        let property = recordType.GetProperty(fieldName)
        if property <> null then
            property.GetValue(record)
        else
            let field = recordType.GetField(fieldName)
            if field <> null then
                field.GetValue(record)
            else
                failwith $"Field or property '{fieldName}' not found on type '{recordType.FullName}'"
    
    /// <summary>
    /// Decodes a value based on its schema
    /// </summary>
    /// <param name="schema">The validated schema definition</param>
    /// <param name="memory">The memory region containing the encoded data</param>
    /// <returns>A result containing the decoded value and final offset, or a decoding error</returns>
    let decode<'T> (schema: SchemaDefinition<validated>) (memory: Memory<'a, 'region>): Result<'T * int<offset>> =
        try
            let rec decodeWithType (typ: Type) (memory: Memory<'a, 'region>) (offset: int<offset>): obj * int<offset> =
                match typ with
                | Primitive primType ->
                    match primType with
                    | UInt -> 
                        let value, newOffset = readUInt memory offset
                        box value, newOffset
                    | Int -> 
                        let value, newOffset = readInt memory offset
                        box value, newOffset
                    | U8 -> 
                        let value, newOffset = readU8 memory offset
                        box value, newOffset
                    | U16 -> 
                        let value, newOffset = readU16 memory offset
                        box value, newOffset
                    | U32 -> 
                        let value, newOffset = readU32 memory offset
                        box value, newOffset
                    | U64 -> 
                        let value, newOffset = readU64 memory offset
                        box value, newOffset
                    | I8 -> 
                        let value, newOffset = readI8 memory offset
                        box value, newOffset
                    | I16 -> 
                        let value, newOffset = readI16 memory offset
                        box value, newOffset
                    | I32 -> 
                        let value, newOffset = readI32 memory offset
                        box value, newOffset
                    | I64 -> 
                        let value, newOffset = readI64 memory offset
                        box value, newOffset
                    | F32 -> 
                        let value, newOffset = readF32 memory offset
                        box value, newOffset
                    | F64 -> 
                        let value, newOffset = readF64 memory offset
                        box value, newOffset
                    | Bool -> 
                        let value, newOffset = readBool memory offset
                        box value, newOffset
                    | String -> 
                        let value, newOffset = readString memory offset
                        box value, newOffset
                    | Data -> 
                        let value, newOffset = readData memory offset
                        box value, newOffset
                    | FixedData length -> 
                        let value, newOffset = readFixedData memory offset length
                        box value, newOffset
                    | Void -> box (), offset
                    | Enum _ ->
                        let value, newOffset = readUInt memory offset
                        box value, newOffset
                    
                | Aggregate aggType ->
                    match aggType with
                    | Optional innerType ->
                        let optValue, newOffset = 
                            readOptional memory offset 
                                (fun mem off -> decodeWithType innerType mem off)
                        box optValue, newOffset
                    
                    | List innerType ->
                        let list, newOffset = 
                            readList memory offset 
                                (fun mem off -> decodeWithType innerType mem off)
                        box list, newOffset
                    
                    | FixedList (innerType, length) ->
                        let list, newOffset = 
                            readFixedList memory offset length
                                (fun mem off -> decodeWithType innerType mem off)
                        box list, newOffset
                    
                    | Map (keyType, valueType) ->
                        let map, newOffset = 
                            readMap memory offset
                                (fun mem off -> decodeWithType keyType mem off)
                                (fun mem off -> decodeWithType valueType mem off)
                        box map, newOffset
                    
                    | Union cases ->
                        let tagVal, tagOffset = readUInt memory offset
                        let tag = uint tagVal
                        
                        if not (Map.containsKey tag cases) then
                            failwith $"Unknown union tag: {tag}"
                            
                        let innerType = Map.find tag cases
                        let value, newOffset = decodeWithType innerType memory tagOffset
                        box (tag, value), newOffset
                    
                    | Struct fields ->
                        // Create a record with the fields
                        let mutable fieldValues = Map.empty
                        let mutable currentOffset = offset
                        
                        for field in fields do
                            let value, newOffset = decodeWithType field.Type memory currentOffset
                            fieldValues <- Map.add field.Name value fieldValues
                            currentOffset <- newOffset
                        
                        let record = createRecord fieldValues
                        box record, currentOffset
                
                | UserDefined typeName ->
                    // Look up the type in the schema
                    let actualType = Map.find typeName schema.Types
                    decodeWithType actualType memory offset
            
            // Start decoding with the root type
            let rootType = Map.find schema.Root schema.Types
            let value, finalOffset = decodeWithType rootType memory 0<offset>
            Ok (unbox<'T> value, finalOffset)
        with ex ->
            Error (decodingError ex.Message)
    
    /// <summary>
    /// Creates a record from field values
    /// </summary>
    /// <param name="fieldValues">A map of field names to values</param>
    /// <returns>A record instance with the specified fields</returns>
    /// <remarks>
    /// This is a placeholder implementation. In a real implementation, 
    /// this would use reflection to create a record instance with the specified field values.
    /// </remarks>
    and createRecord (fieldValues: Map<string, obj>): obj =
        box fieldValues
    
    /// <summary>
    /// Encodes a value with UMX type safety
    /// </summary>
    /// <param name="schema">The validated schema definition</param>
    /// <param name="value">The value to encode, wrapped with a measure type</param>
    /// <param name="buffer">The buffer to write to</param>
    /// <returns>A result indicating success or an encoding error</returns>
    let encodeWithMeasure<'T, [<Measure>] 'm> 
                        (schema: SchemaDefinition<validated>) 
                        (value: 'T<'m>) 
                        (buffer: Buffer<'a>): Result<unit> =
        // Unwrap the measure
        let rawValue = UMX.untag value
        // Encode the raw value
        encode schema rawValue buffer

    /// <summary>
    /// Decodes a value with UMX type safety
    /// </summary>
    /// <param name="schema">The validated schema definition</param>
    /// <param name="memory">The memory region containing the encoded data</param>
    /// <returns>A result containing the decoded value (with measure) and final offset, or a decoding error</returns>
    let decodeWithMeasure<'T, [<Measure>] 'm> 
                        (schema: SchemaDefinition<validated>) 
                        (memory: Memory<'a, 'region>): Result<'T<'m> * int<offset>> =
        // Decode the raw value
        let result = decode<'T> schema memory
        match result with
        | Ok (rawValue, offset) ->
            // Wrap with the measure
            Ok (UMX.tag<'m> rawValue, offset)
        | Error err -> Error err