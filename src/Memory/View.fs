namespace BAREWire.Memory

open FSharp.UMX
open FSharp.NativeInterop
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Core.Memory
open BAREWire.Memory.SafeMemory

#nowarn "9" // Disable warning about using fixed keyword

/// <summary>
/// Memory view operations for working with typed data in memory regions
/// </summary>
module View =
    /// <summary>
    /// A view over a memory region with a specific schema
    /// </summary>
    /// <typeparam name="'T">The type associated with this memory view</typeparam>
    /// <typeparam name="'region">The memory region measure type</typeparam>
    type MemoryView<'T, [<Measure>] 'region> = {
        /// <summary>The underlying memory region</summary>
        Memory: Memory<'T, 'region>
        
        /// <summary>The schema defining the structure of the data</summary>
        Schema: SchemaDefinition<validated>
        
        /// <summary>Field offsets cache for faster access</summary>
        FieldOffsets: Map<string, FieldOffset>
    }
    
    /// <summary>
    /// A field path in a memory view, represented as a list of field names
    /// </summary>
    and FieldPath = string list
    
    /// <summary>
    /// Field offset information within a memory view
    /// </summary>
    and FieldOffset = {
        /// <summary>The offset of the field in bytes</summary>
        Offset: int<offset>
        
        /// <summary>The type of the field</summary>
        Type: Type
        
        /// <summary>The size of the field in bytes</summary>
        Size: int<bytes>
        
        /// <summary>The alignment of the field in bytes</summary>
        Alignment: int<bytes>
    }
    
    /// <summary>
    /// Creates a view over a memory region with a schema
    /// </summary>
    /// <param name="memory">The memory region to view</param>
    /// <param name="schema">The schema defining the structure of the data</param>
    /// <returns>A memory view for the region</returns>
    let create<'T, [<Measure>] 'region> 
              (memory: Memory<'T, 'region>) 
              (schema: SchemaDefinition<validated>): MemoryView<'T, 'region> =
        // Calculate field offsets for faster access
        let fieldOffsets = calculateFieldOffsets schema
        
        {
            Memory = memory
            Schema = schema
            FieldOffsets = fieldOffsets
        }
    
    /// <summary>
    /// Calculates field offsets for a schema
    /// </summary>
    /// <param name="schema">The schema to calculate offsets for</param>
    /// <returns>A map of field paths to their offset information</returns>
    and calculateFieldOffsets (schema: SchemaDefinition<validated>): Map<string, FieldOffset> =
        let rec calcOffsets typeName parentPath currentOffset acc =
            match Map.tryFind typeName schema.Types with
            | None -> acc  // Type not found
            | Some typ ->
                match typ with
                | Aggregate (Struct fields) ->
                    // Calculate offsets for each field in the struct
                    let mutable fieldOffset = currentOffset
                    let mutable result = acc
                    
                    for field in fields do
                        let fieldPath = 
                            if List.isEmpty parentPath then
                                field.Name
                            else
                                String.concat "." (parentPath @ [field.Name])
                        
                        // Get field type details
                        let fieldType = field.Type
                        let fieldSize, fieldAlign = getSizeAndAlignment schema fieldType
                        
                        // Apply alignment
                        let alignedOffset = 
                            let rem = int fieldOffset % int fieldAlign
                            if rem = 0 then fieldOffset
                            else fieldOffset + ((int fieldAlign - rem) * 1<offset>)
                        
                        // Add field to result
                        result <- Map.add 
                            fieldPath 
                            { 
                                Offset = alignedOffset
                                Type = fieldType
                                Size = fieldSize
                                Alignment = fieldAlign 
                            } 
                            result
                        
                        // Update offset for next field
                        fieldOffset <- alignedOffset + (int fieldSize * 1<offset>)
                        
                        // If this is a nested struct or a user-defined type, recurse
                        match fieldType with
                        | Aggregate (Struct _) ->
                            let newPath = parentPath @ [field.Name]
                            let nestedTypeName = 
                                match Map.tryFind fieldPath result with
                                | Some fo -> getTypeName fo.Type
                                | None -> ""
                            
                            if not (String.IsNullOrEmpty nestedTypeName) then
                                result <- calcOffsets nestedTypeName newPath alignedOffset result
                        | UserDefined nestedTypeName ->
                            let newPath = parentPath @ [field.Name]
                            result <- calcOffsets nestedTypeName newPath alignedOffset result
                        | _ -> ()
                    
                    result
                | _ -> acc  // Only structs have fields
            
        // Start calculating from the root type
        calcOffsets schema.Root [] 0<offset> Map.empty
    
    /// <summary>
    /// Gets the size and alignment for a type
    /// </summary>
    /// <param name="schema">The schema containing type definitions</param>
    /// <param name="typ">The type to get size and alignment for</param>
    /// <returns>A tuple of (size, alignment) in bytes</returns>
    and getSizeAndAlignment (schema: SchemaDefinition<validated>) (typ: Type): int<bytes> * int<bytes> =
        match typ with
        | Primitive primType ->
            match primType with
            | UInt -> 8<bytes>, 8<bytes>  // Variable length, max 10 bytes but align to 8
            | Int -> 8<bytes>, 8<bytes>   // Variable length, max 10 bytes but align to 8
            | U8 -> 1<bytes>, 1<bytes>
            | U16 -> 2<bytes>, 2<bytes>
            | U32 -> 4<bytes>, 4<bytes>
            | U64 -> 8<bytes>, 8<bytes>
            | I8 -> 1<bytes>, 1<bytes>
            | I16 -> 2<bytes>, 2<bytes>
            | I32 -> 4<bytes>, 4<bytes>
            | I64 -> 8<bytes>, 8<bytes>
            | F32 -> 4<bytes>, 4<bytes>
            | F64 -> 8<bytes>, 8<bytes>
            | Bool -> 1<bytes>, 1<bytes>
            | String -> 16<bytes>, 8<bytes>  // Pointer + length
            | Data -> 16<bytes>, 8<bytes>    // Pointer + length
            | FixedData length -> length * 1<bytes>, 1<bytes>
            | Void -> 0<bytes>, 1<bytes>
            | Enum _ -> 8<bytes>, 8<bytes>   // Treat like uint64
        
        | Aggregate aggType ->
            match aggType with
            | Optional innerType ->
                let innerSize, innerAlign = getSizeAndAlignment schema innerType
                innerSize + 1<bytes>, max 1<bytes> innerAlign
                
            | List _ -> 16<bytes>, 8<bytes>  // Pointer + length
            
            | FixedList (innerType, length) ->
                let innerSize, innerAlign = getSizeAndAlignment schema innerType
                innerSize * length, innerAlign
                
            | Map _ -> 16<bytes>, 8<bytes>   // Pointer to map structure
            
            | Union cases ->
                // Union takes the size of the largest case plus 8 bytes for the tag
                let maxSize = 
                    cases 
                    |> Map.values 
                    |> Seq.map (fun t -> fst (getSizeAndAlignment schema t))
                    |> Seq.max
                
                let maxAlign = 
                    cases 
                    |> Map.values 
                    |> Seq.map (fun t -> snd (getSizeAndAlignment schema t))
                    |> Seq.max
                
                maxSize + 8<bytes>, max 8<bytes> maxAlign
                
            | Struct fields ->
                // Calculate total size with proper alignment
                let mutable totalSize = 0<bytes>
                let mutable maxAlign = 1<bytes>
                
                for field in fields do
                    let fieldSize, fieldAlign = getSizeAndAlignment schema field.Type
                    
                    // Align the current offset
                    let rem = int totalSize % int fieldAlign
                    if rem <> 0 then
                        totalSize <- totalSize + ((int fieldAlign - rem) * 1<bytes>)
                    
                    // Add field size
                    totalSize <- totalSize + fieldSize
                    
                    // Track maximum alignment
                    maxAlign <- max maxAlign fieldAlign
                
                // Final size needs to be a multiple of the maximum alignment
                let rem = int totalSize % int maxAlign
                if rem <> 0 then
                    totalSize <- totalSize + ((int maxAlign - rem) * 1<bytes>)
                
                totalSize, maxAlign
        
        | UserDefined typeName ->
            // Look up the user-defined type
            match Map.tryFind typeName schema.Types with
            | Some t -> getSizeAndAlignment schema t
            | None -> 8<bytes>, 8<bytes>  // Default if type not found
    
    /// <summary>
    /// Gets the type name for a type
    /// </summary>
    /// <param name="typ">The type to get the name for</param>
    /// <returns>The type name, or empty string for primitives and aggregates</returns>
    and getTypeName (typ: Type): string =
        match typ with
        | UserDefined name -> name
        | _ -> ""
    
    /// <summary>
    /// Resolves a field path to get its offset in memory
    /// </summary>
    /// <param name="view">The memory view</param>
    /// <param name="fieldPath">The path to the field</param>
    /// <returns>A result containing the field offset information or an error</returns>
    let resolveFieldPath 
        (view: MemoryView<'T, 'region>) 
        (fieldPath: FieldPath): Result<FieldOffset> =
        
        // Convert path to a dot-separated string
        let pathString = String.concat "." fieldPath
        
        // Look up in the offset cache
        match Map.tryFind pathString view.FieldOffsets with
        | Some offset -> Ok offset
        | None ->
            // If not found in cache, try to resolve dynamically
            let rec resolveField currentType remainingPath currentOffset =
                match remainingPath, currentType with
                | [], _ -> 
                    // End of path, return current offset
                    let size, align = getSizeAndAlignment view.Schema currentType
                    Ok { 
                        Offset = currentOffset
                        Type = currentType
                        Size = size
                        Alignment = align
                    }
                    
                | fieldName :: rest, Aggregate (Struct fields) ->
                    // Find the field in the struct
                    match List.tryFind (fun f -> f.Name = fieldName) fields with
                    | Some field ->
                        // Calculate field offset with alignment
                        let fieldSize, fieldAlign = getSizeAndAlignment view.Schema field.Type
                        
                        // Align the current offset
                        let alignedOffset = 
                            let rem = int currentOffset % int fieldAlign
                            if rem = 0 then currentOffset
                            else currentOffset + ((int fieldAlign - rem) * 1<offset>)
                        
                        // Continue with the rest of the path
                        resolveField field.Type rest (alignedOffset + (fieldSize * 1<offset>))
                        
                    | None ->
                        Error (invalidValueError $"Field not found: {fieldName}")
                        
                | fieldName :: rest, UserDefined typeName ->
                    // Look up the type definition
                    match Map.tryFind typeName view.Schema.Types with
                    | Some typ -> resolveField typ remainingPath currentOffset
                    | None -> Error (invalidValueError $"Type not found: {typeName}")
                    
                | _, _ ->
                    Error (invalidValueError $"Cannot access field in non-struct type")
            
            // Start resolving from the root type
            let rootType = Map.find view.Schema.Root view.Schema.Types
            resolveField rootType fieldPath 0<offset>
    
    /// <summary>
    /// Gets a field value from a view
    /// </summary>
    /// <param name="view">The memory view</param>
    /// <param name="fieldPath">The path to the field</param>
    /// <returns>A result containing the field value or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when field resolution or decoding fails</exception>
    let getField<'T, 'Field, [<Measure>] 'region> 
                (view: MemoryView<'T, 'region>) 
                (fieldPath: FieldPath): Result<'Field> =
        resolveFieldPath view fieldPath
        |> Result.bind (fun fieldOffset ->
            try
                // Create a memory slice for the field
                let fieldMemory = {
                    Data = view.Memory.Data
                    Offset = view.Memory.Offset + fieldOffset.Offset
                    Length = min fieldOffset.Size (view.Memory.Length - fieldOffset.Offset)
                }
                
                // Based on the field type, decode the value
                match fieldOffset.Type with
                | Primitive primType ->
                    match primType with
                    | U8 -> 
                        let value = readByte fieldMemory.Data (int fieldMemory.Offset)
                        Ok (box value :?> 'Field)
                        
                    | U16 -> 
                        let value = readUnmanaged<uint16> fieldMemory.Data (int fieldMemory.Offset)
                        Ok (box value :?> 'Field)
                        
                    | U32 -> 
                        let value = readUnmanaged<uint32> fieldMemory.Data (int fieldMemory.Offset)
                        Ok (box value :?> 'Field)
                        
                    | U64 -> 
                        let value = readUnmanaged<uint64> fieldMemory.Data (int fieldMemory.Offset)
                        Ok (box value :?> 'Field)
                        
                    | I8 -> 
                        let value = readUnmanaged<sbyte> fieldMemory.Data (int fieldMemory.Offset)
                        Ok (box value :?> 'Field)
                        
                    | I16 -> 
                        let value = readUnmanaged<int16> fieldMemory.Data (int fieldMemory.Offset)
                        Ok (box value :?> 'Field)
                        
                    | I32 -> 
                        let value = readUnmanaged<int32> fieldMemory.Data (int fieldMemory.Offset)
                        Ok (box value :?> 'Field)
                        
                    | I64 -> 
                        let value = readUnmanaged<int64> fieldMemory.Data (int fieldMemory.Offset)
                        Ok (box value :?> 'Field)
                        
                    | F32 -> 
                        let value = readUnmanaged<float32> fieldMemory.Data (int fieldMemory.Offset)
                        Ok (box value :?> 'Field)
                        
                    | F64 -> 
                        let value = readUnmanaged<float> fieldMemory.Data (int fieldMemory.Offset)
                        Ok (box value :?> 'Field)
                        
                    | Bool -> 
                        let value = readByte fieldMemory.Data (int fieldMemory.Offset) <> 0uy
                        Ok (box value :?> 'Field)
                        
                    | String ->
                        // String is stored as length + bytes
                        let length = readUnmanaged<int32> fieldMemory.Data (int fieldMemory.Offset)
                        
                        if length > 0 then
                            let bytes = Array.init length (fun i -> 
                                readByte fieldMemory.Data (int fieldMemory.Offset + 4 + i))
                            let value = BAREWire.Core.Utf8.getString bytes
                            Ok (box value :?> 'Field)
                        else
                            Ok (box "" :?> 'Field)
                            
                    | _ ->
                        // For other types, we'd need to implement specific decoding logic
                        // This is a simplified version
                        Error (decodingError $"Decoding not implemented for type {fieldOffset.Type}")
                        
                | _ ->
                    // For complex types, we'd need to implement custom decoding logic
                    // based on the schema and field type
                    Error (decodingError $"Decoding not implemented for complex types")
                    
            with ex ->
                Error (decodingError $"Failed to decode field {String.concat "." fieldPath}: {ex.Message}")
        )
    
    /// <summary>
    /// Sets a field value in a view
    /// </summary>
    /// <param name="view">The memory view</param>
    /// <param name="fieldPath">The path to the field</param>
    /// <param name="value">The value to set</param>
    /// <returns>A result indicating success or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when field resolution or encoding fails</exception>
    let setField<'T, 'Field, [<Measure>] 'region> 
                (view: MemoryView<'T, 'region>) 
                (fieldPath: FieldPath) 
                (value: 'Field): Result<unit> =
        resolveFieldPath view fieldPath
        |> Result.bind (fun fieldOffset ->
            try
                let fieldOffset = int (view.Memory.Offset + fieldOffset.Offset)
                
                // Based on the field type, encode the value
                match fieldOffset.Type with
                | Primitive primType ->
                    match primType with
                    | U8 -> 
                        let byteValue = value :?> byte
                        writeByte view.Memory.Data fieldOffset byteValue
                        
                    | U16 -> 
                        let u16Value = value :?> uint16
                        writeUnmanaged<uint16> view.Memory.Data fieldOffset u16Value
                        
                    | U32 -> 
                        let u32Value = value :?> uint32
                        writeUnmanaged<uint32> view.Memory.Data fieldOffset u32Value
                        
                    | U64 -> 
                        let u64Value = value :?> uint64
                        writeUnmanaged<uint64> view.Memory.Data fieldOffset u64Value
                        
                    | I8 -> 
                        let i8Value = value :?> sbyte
                        writeUnmanaged<sbyte> view.Memory.Data fieldOffset i8Value
                        
                    | I16 -> 
                        let i16Value = value :?> int16
                        writeUnmanaged<int16> view.Memory.Data fieldOffset i16Value
                        
                    | I32 -> 
                        let i32Value = value :?> int32
                        writeUnmanaged<int32> view.Memory.Data fieldOffset i32Value
                        
                    | I64 -> 
                        let i64Value = value :?> int64
                        writeUnmanaged<int64> view.Memory.Data fieldOffset i64Value
                        
                    | F32 -> 
                        let f32Value = value :?> float32
                        writeUnmanaged<float32> view.Memory.Data fieldOffset f32Value
                        
                    | F64 -> 
                        let f64Value = value :?> float
                        writeUnmanaged<float> view.Memory.Data fieldOffset f64Value
                        
                    | Bool -> 
                        let boolValue = value :?> bool
                        writeByte view.Memory.Data fieldOffset (if boolValue then 1uy else 0uy)
                        
                    | String ->
                        // String is stored as length + bytes
                        let stringValue = value :?> string
                        let bytes = BAREWire.Core.Utf8.getBytes stringValue
                        let length = bytes.Length
                        
                        // Write length
                        writeUnmanaged<int32> view.Memory.Data fieldOffset length
                        
                        // Write string data
                        if length > 0 then
                            for i = 0 to length - 1 do
                                if fieldOffset + 4 + i < view.Memory.Data.Length then
                                    writeByte view.Memory.Data (fieldOffset + 4 + i) bytes.[i]
                                
                    | _ ->
                        // For other types, we'd need to implement specific encoding logic
                        return Error (encodingError $"Encoding not implemented for type {fieldOffset.Type}")
                        
                | _ ->
                    // For complex types, we'd need to implement custom encoding logic
                    // based on the schema and field type
                    return Error (encodingError $"Encoding not implemented for complex types")
                
                Ok ()
                    
            with ex ->
                Error (encodingError $"Failed to encode field {String.concat "." fieldPath}: {ex.Message}")
        )
    
    /// <summary>
    /// Gets a view for a nested struct field
    /// </summary>
    /// <param name="view">The parent memory view</param>
    /// <param name="fieldPath">The path to the nested struct field</param>
    /// <returns>A result containing the nested view or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when field resolution fails</exception>
    let getNestedView<'T, 'U, [<Measure>] 'region> 
                     (view: MemoryView<'T, 'region>) 
                     (fieldPath: FieldPath): Result<MemoryView<'U, 'region>> =
        resolveFieldPath view fieldPath
        |> Result.bind (fun fieldOffset ->
            // Check if the field is a struct or user-defined type
            match fieldOffset.Type with
            | Aggregate (Struct _) | UserDefined _ ->
                // Create a view for the nested field
                let fieldMemory = {
                    Data = view.Memory.Data
                    Offset = view.Memory.Offset + fieldOffset.Offset
                    Length = min fieldOffset.Size (view.Memory.Length - fieldOffset.Offset)
                }
                
                // Determine the schema for the nested field
                let fieldSchema = 
                    match fieldOffset.Type with
                    | UserDefined typeName ->
                        // Create a new schema with the field's type as root
                        { Types = view.Schema.Types; Root = typeName }
                    | _ ->
                        // For struct, use the same schema
                        view.Schema
                
                // Create and return the nested view
                Ok (create<'U, 'region> fieldMemory fieldSchema)
                
            | _ ->
                Error (invalidValueError $"Field {String.concat "." fieldPath} is not a struct or user-defined type")
        )
    
    /// <summary>
    /// Gets a UMX-typed field value from a view
    /// </summary>
    /// <param name="view">The memory view</param>
    /// <param name="fieldPath">The path to the field</param>
    /// <returns>A result containing the field value with measure type or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when field resolution or decoding fails</exception>
    let getFieldWithMeasure<'T, 'Field, [<Measure>] 'region, [<Measure>] 'measure> 
                           (view: MemoryView<'T, 'region>) 
                           (fieldPath: FieldPath): Result<'Field<'measure>> =
        getField<'T, 'Field, 'region> view fieldPath
        |> Result.map (fun value -> UMX.tag<'measure> value)
    
    /// <summary>
    /// Sets a UMX-typed field value in a view
    /// </summary>
    /// <param name="view">The memory view</param>
    /// <param name="fieldPath">The path to the field</param>
    /// <param name="value">The value with measure type to set</param>
    /// <returns>A result indicating success or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when field resolution or encoding fails</exception>
    let setFieldWithMeasure<'T, 'Field, [<Measure>] 'region, [<Measure>] 'measure> 
                           (view: MemoryView<'T, 'region>) 
                           (fieldPath: FieldPath) 
                           (value: 'Field<'measure>): Result<unit> =
        let rawValue = UMX.untag value
        setField<'T, 'Field, 'region> view fieldPath rawValue
    
    /// <summary>
    /// Applies a function to transform a field value
    /// </summary>
    /// <param name="view">The memory view</param>
    /// <param name="fieldPath">The path to the field</param>
    /// <param name="updateFn">The function to transform the current value</param>
    /// <returns>A result indicating success or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when field resolution, decoding, or encoding fails</exception>
    let updateField<'T, 'Field, [<Measure>] 'region> 
                   (view: MemoryView<'T, 'region>) 
                   (fieldPath: FieldPath) 
                   (updateFn: 'Field -> 'Field): Result<unit> =
        getField<'T, 'Field, 'region> view fieldPath
        |> Result.bind (fun currentValue ->
            let newValue = updateFn currentValue
            setField<'T, 'Field, 'region> view fieldPath newValue
        )
    
    /// <summary>
    /// Checks if a field exists in the view
    /// </summary>
    /// <param name="view">The memory view</param>
    /// <param name="fieldPath">The path to the field</param>
    /// <returns>True if the field exists, false otherwise</returns>
    let fieldExists<'T, [<Measure>] 'region> 
                   (view: MemoryView<'T, 'region>) 
                   (fieldPath: FieldPath): bool =
        // Convert path to a dot-separated string
        let pathString = String.concat "." fieldPath
        
        // Check if the field exists in the offsets cache
        Map.containsKey pathString view.FieldOffsets
    
    /// <summary>
    /// Gets all field names at the root level of a view
    /// </summary>
    /// <param name="view">The memory view</param>
    /// <returns>A list of field names</returns>
    let getRootFieldNames<'T, [<Measure>] 'region> 
                         (view: MemoryView<'T, 'region>): string list =
        // Get the root type
        let rootType = Map.find view.Schema.Root view.Schema.Types
        
        match rootType with
        | Aggregate (Struct fields) ->
            // Extract field names from the struct
            fields |> List.map (fun field -> field.Name)
        | _ ->
            // Non-struct types don't have field names
            []
    
    /// <summary>
    /// Creates a memory view for a specific address within a memory region
    /// </summary>
    /// <param name="address">The address within the region</param>
    /// <param name="memory">The memory region</param>
    /// <param name="schema">The schema for the view</param>
    /// <returns>A memory view for the specified address</returns>
    let fromAddress<'T, [<Measure>] 'region> 
                   (address: Address<'region>) 
                   (memory: Memory<'T, 'region>) 
                   (schema: SchemaDefinition<validated>): MemoryView<'T, 'region> =
        // Calculate the view memory
        let viewMemory = {
            Data = memory.Data
            Offset = address.Offset
            Length = memory.Length - (address.Offset - memory.Offset)
        }
        
        // Create the view
        create<'T, 'region> viewMemory schema