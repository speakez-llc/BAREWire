namespace BAREWire.Schema

open BAREWire.Core
open BAREWire.Core.Error

/// <summary>
/// Schema definition module for creating and manipulating schema definitions
/// </summary>
module Definition =
    /// <summary>
    /// Creates a new draft schema with the specified root type name
    /// </summary>
    /// <param name="rootTypeName">The name of the root type</param>
    /// <returns>A new draft schema definition</returns>
    let create (rootTypeName: string): SchemaDefinition<draft> =
        { Types = Map.empty; Root = rootTypeName }
    
    /// <summary>
    /// Adds a primitive type to a schema
    /// </summary>
    /// <param name="name">The name of the type</param>
    /// <param name="primitiveType">The primitive type definition</param>
    /// <param name="schema">The schema to add to</param>
    /// <returns>A new schema with the type added</returns>
    let addPrimitive 
        (name: string) 
        (primitiveType: PrimitiveType) 
        (schema: SchemaDefinition<draft>): SchemaDefinition<draft> =
        
        let types = Map.add name (Primitive primitiveType) schema.Types
        { schema with Types = types }
    
    /// <summary>
    /// Adds a struct type to a schema
    /// </summary>
    /// <param name="name">The name of the struct type</param>
    /// <param name="fields">The list of struct fields</param>
    /// <param name="schema">The schema to add to</param>
    /// <returns>A new schema with the struct type added</returns>
    let addStruct 
        (name: string) 
        (fields: StructField list) 
        (schema: SchemaDefinition<draft>): SchemaDefinition<draft> =
        
        let types = Map.add name (Aggregate (Struct fields)) schema.Types
        { schema with Types = types }
    
    /// <summary>
    /// Adds a union type to a schema
    /// </summary>
    /// <param name="name">The name of the union type</param>
    /// <param name="cases">The map of union case tags to types</param>
    /// <param name="schema">The schema to add to</param>
    /// <returns>A new schema with the union type added</returns>
    let addUnion 
        (name: string) 
        (cases: Map<uint, Type>) 
        (schema: SchemaDefinition<draft>): SchemaDefinition<draft> =
        
        let types = Map.add name (Aggregate (Union cases)) schema.Types
        { schema with Types = types }
    
    /// <summary>
    /// Adds an optional type to a schema
    /// </summary>
    /// <param name="name">The name of the optional type</param>
    /// <param name="innerType">The type wrapped by the optional</param>
    /// <param name="schema">The schema to add to</param>
    /// <returns>A new schema with the optional type added</returns>
    let addOptional
        (name: string)
        (innerType: Type)
        (schema: SchemaDefinition<draft>): SchemaDefinition<draft> =
        
        let types = Map.add name (Aggregate (Optional innerType)) schema.Types
        { schema with Types = types }
    
    /// <summary>
    /// Adds a list type to a schema
    /// </summary>
    /// <param name="name">The name of the list type</param>
    /// <param name="itemType">The type of items in the list</param>
    /// <param name="schema">The schema to add to</param>
    /// <returns>A new schema with the list type added</returns>
    let addList 
        (name: string) 
        (itemType: Type) 
        (schema: SchemaDefinition<draft>): SchemaDefinition<draft> =
        
        let types = Map.add name (Aggregate (List itemType)) schema.Types
        { schema with Types = types }
    
    /// <summary>
    /// Adds a fixed-length list type to a schema
    /// </summary>
    /// <param name="name">The name of the fixed-length list type</param>
    /// <param name="itemType">The type of items in the list</param>
    /// <param name="length">The fixed length of the list</param>
    /// <param name="schema">The schema to add to</param>
    /// <returns>A new schema with the fixed-length list type added</returns>
    let addFixedList
        (name: string)
        (itemType: Type)
        (length: int)
        (schema: SchemaDefinition<draft>): SchemaDefinition<draft> =
        
        let types = Map.add name (Aggregate (FixedList (itemType, length))) schema.Types
        { schema with Types = types }
    
    /// <summary>
    /// Adds a map type to a schema
    /// </summary>
    /// <param name="name">The name of the map type</param>
    /// <param name="keyType">The type of keys in the map</param>
    /// <param name="valueType">The type of values in the map</param>
    /// <param name="schema">The schema to add to</param>
    /// <returns>A new schema with the map type added</returns>
    let addMap 
        (name: string) 
        (keyType: Type) 
        (valueType: Type) 
        (schema: SchemaDefinition<draft>): SchemaDefinition<draft> =
        
        let types = Map.add name (Aggregate (Map (keyType, valueType))) schema.Types
        { schema with Types = types }
    
    /// <summary>
    /// Adds an enum type to a schema
    /// </summary>
    /// <param name="name">The name of the enum type</param>
    /// <param name="values">The map of enum value names to numeric values</param>
    /// <param name="schema">The schema to add to</param>
    /// <returns>A new schema with the enum type added</returns>
    let addEnum
        (name: string)
        (values: Map<string, uint64>)
        (schema: SchemaDefinition<draft>): SchemaDefinition<draft> =
        
        let types = Map.add name (Primitive (Enum values)) schema.Types
        { schema with Types = types }
    
    /// <summary>
    /// Adds a reference to a user-defined type
    /// </summary>
    /// <param name="name">The name of the reference type</param>
    /// <param name="typeName">The name of the referenced type</param>
    /// <param name="schema">The schema to add to</param>
    /// <returns>A new schema with the type reference added</returns>
    let addTypeRef
        (name: string)
        (typeName: string)
        (schema: SchemaDefinition<draft>): SchemaDefinition<draft> =
        
        let types = Map.add name (UserDefined typeName) schema.Types
        { schema with Types = types }
    
    /// <summary>
    /// Checks if a type exists in the schema
    /// </summary>
    /// <param name="name">The name of the type to check</param>
    /// <param name="schema">The schema to check in</param>
    /// <returns>True if the type exists, false otherwise</returns>
    let typeExists
        (name: string)
        (schema: SchemaDefinition<draft>): bool =
        
        Map.containsKey name schema.Types
    
    /// <summary>
    /// Gets a type from the schema
    /// </summary>
    /// <param name="name">The name of the type to get</param>
    /// <param name="schema">The schema to get from</param>
    /// <returns>The type if found, None otherwise</returns>
    let getType
        (name: string)
        (schema: SchemaDefinition<draft>): Option<Type> =
        
        Map.tryFind name schema.Types
    
    /// <summary>
    /// Removes a type from the schema
    /// </summary>
    /// <param name="name">The name of the type to remove</param>
    /// <param name="schema">The schema to remove from</param>
    /// <returns>A new schema with the type removed</returns>
    let removeType
        (name: string)
        (schema: SchemaDefinition<draft>): SchemaDefinition<draft> =
        
        let types = Map.remove name schema.Types
        { schema with Types = types }
    
    /// <summary>
    /// Sets the root type of the schema
    /// </summary>
    /// <param name="rootTypeName">The name of the root type</param>
    /// <param name="schema">The schema to modify</param>
    /// <returns>A new schema with the updated root type</returns>
    let setRoot
        (rootTypeName: string)
        (schema: SchemaDefinition<draft>): SchemaDefinition<draft> =
        
        { schema with Root = rootTypeName }
    
    /// <summary>
    /// Gets all type names in the schema
    /// </summary>
    /// <param name="schema">The schema to get type names from</param>
    /// <returns>A list of all type names in the schema</returns>
    let getTypeNames
        (schema: SchemaDefinition<'state>): string list =
        
        Map.keys schema.Types |> Seq.toList
    
    /// <summary>
    /// Gets all types in the schema
    /// </summary>
    /// <param name="schema">The schema to get types from</param>
    /// <returns>A list of name-type pairs for all types in the schema</returns>
    let getTypes
        (schema: SchemaDefinition<'state>): (string * Type) list =
        
        Map.toList schema.Types
    
    /// <summary>
    /// Copies a type from one schema to another
    /// </summary>
    /// <param name="typeName">The name of the type to copy</param>
    /// <param name="sourceSchema">The schema to copy from</param>
    /// <param name="targetSchema">The schema to copy to</param>
    /// <returns>A new schema with the copied type</returns>
    let copyType
        (typeName: string)
        (sourceSchema: SchemaDefinition<'state1>)
        (targetSchema: SchemaDefinition<draft>): SchemaDefinition<draft> =
        
        match Map.tryFind typeName sourceSchema.Types with
        | Some typ ->
            let types = Map.add typeName typ targetSchema.Types
            { targetSchema with Types = types }
        | None ->
            targetSchema
    
    /// <summary>
    /// Merges two schemas
    /// </summary>
    /// <param name="schema1">The first schema</param>
    /// <param name="schema2">The second schema</param>
    /// <returns>A new schema containing all types from both schemas, with the root from schema1</returns>
    let merge
        (schema1: SchemaDefinition<'state1>)
        (schema2: SchemaDefinition<'state2>): SchemaDefinition<draft> =
        
        let types = Map.fold (fun acc k v -> Map.add k v acc) schema1.Types schema2.Types
        { Types = types; Root = schema1.Root }
    
    /// <summary>
    /// Creates a clone of a schema
    /// </summary>
    /// <param name="schema">The schema to clone</param>
    /// <returns>A new draft schema with the same content</returns>
    let clone
        (schema: SchemaDefinition<'state>): SchemaDefinition<draft> =
        
        { Types = schema.Types; Root = schema.Root }
    
    /// <summary>
    /// Performs an unsafe cast of schema state
    /// </summary>
    /// <param name="schema">The schema to cast</param>
    /// <returns>The same schema with a different state type</returns>
    /// <remarks>
    /// This function should be used with caution as it bypasses type safety.
    /// Only use when you're certain that the schema meets the requirements of the target state.
    /// </remarks>
    let unsafeCast<[<Measure>] 'stateFrom, [<Measure>] 'stateTo>
        (schema: SchemaDefinition<'stateFrom>): SchemaDefinition<'stateTo> =
        
        { Types = schema.Types; Root = schema.Root }