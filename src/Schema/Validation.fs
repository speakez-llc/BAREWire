namespace BAREWire.Schema

open BAREWire.Core
open BAREWire.Core.Error

/// <summary>
/// Schema validation module for ensuring schema correctness
/// </summary>
module Validation =
    /// <summary>
    /// Errors that can occur during schema validation
    /// </summary>
    type ValidationError =
        /// <summary>A cyclic reference was detected in the type hierarchy</summary>
        | CyclicTypeReference of typeName:string
        /// <summary>A referenced type was not defined in the schema</summary>
        | UndefinedType of typeName:string
        /// <summary>The void type was used in an invalid context</summary>
        | InvalidVoidUsage of location:string
        /// <summary>An invalid type was used as a map key</summary>
        | InvalidMapKeyType of typeName:string
        /// <summary>An enum was defined with no values</summary>
        | EmptyEnum
        /// <summary>A union was defined with no cases</summary>
        | EmptyUnion
        /// <summary>A struct was defined with no fields</summary>
        | EmptyStruct
        /// <summary>An invalid length was specified for a fixed-length type</summary>
        | InvalidFixedLength of length:int * location:string
    
    /// <summary>
    /// Converts a validation error to a human-readable string
    /// </summary>
    /// <param name="error">The validation error</param>
    /// <returns>A string describing the error</returns>
    let errorToString error =
        match error with
        | CyclicTypeReference typeName -> $"Cyclic type reference detected: {typeName}"
        | UndefinedType typeName -> $"Undefined type: {typeName}"
        | InvalidVoidUsage location -> $"Invalid void usage at {location}"
        | InvalidMapKeyType typeName -> $"Invalid map key type: {typeName}"
        | EmptyEnum -> "Empty enum"
        | EmptyUnion -> "Empty union"
        | EmptyStruct -> "Empty struct"
        | InvalidFixedLength (length, location) -> $"Invalid fixed length {length} at {location}"
    
    /// <summary>
    /// Context for tracking path in the schema during validation
    /// </summary>
    type ValidationContext =
        /// <summary>Root of a type definition</summary>
        | TypeRoot of name:string
        /// <summary>Field within a struct</summary>
        | StructField of name:string
        /// <summary>Case within a union</summary>
        | UnionCase
        /// <summary>Value of an optional type</summary>
        | OptionalValue
        /// <summary>Item within a list</summary>
        | ListItem
        /// <summary>Key within a map</summary>
        | MapKey
        /// <summary>Value within a map</summary>
        | MapValue
    
    /// <summary>
    /// Converts a validation context path to a string for error reporting
    /// </summary>
    /// <param name="path">The context path</param>
    /// <returns>A string representation of the path</returns>
    let typePathToString (path: ValidationContext list) =
        let rec pathToString path =
            match path with
            | [] -> ""
            | [TypeRoot name] -> name
            | TypeRoot name :: rest -> $"{name}.{pathToString rest}"
            | StructField name :: rest -> $"{name}.{pathToString rest}"
            | UnionCase :: rest -> $"case.{pathToString rest}"
            | OptionalValue :: rest -> $"optional.{pathToString rest}"
            | ListItem :: rest -> $"item.{pathToString rest}"
            | MapKey :: rest -> $"key.{pathToString rest}"
            | MapValue :: rest -> $"value.{pathToString rest}"
        
        pathToString (List.rev path)
    
    /// <summary>
    /// Checks if the current context is within a union
    /// </summary>
    /// <param name="path">The context path</param>
    /// <returns>True if the context is within a union, false otherwise</returns>
    let isUnionContext (path: ValidationContext list) =
        List.exists (function | UnionCase -> true | _ -> false) path
    
    /// <summary>
    /// Converts a type to a string representation for error reporting
    /// </summary>
    /// <param name="typ">The type to convert</param>
    /// <returns>A string representation of the type</returns>
    let rec typeToString typ =
        match typ with
        | Primitive UInt -> "UInt"
        | Primitive Int -> "Int"
        | Primitive U8 -> "U8"
        | Primitive U16 -> "U16"
        | Primitive U32 -> "U32"
        | Primitive U64 -> "U64"
        | Primitive I8 -> "I8"
        | Primitive I16 -> "I16"
        | Primitive I32 -> "I32"
        | Primitive I64 -> "I64"
        | Primitive F32 -> "F32"
        | Primitive F64 -> "F64"
        | Primitive Bool -> "Bool"
        | Primitive String -> "String"
        | Primitive Data -> "Data"
        | Primitive (FixedData length) -> $"FixedData({length})"
        | Primitive Void -> "Void"
        | Primitive (Enum _) -> "Enum"
        | Aggregate (Optional innerType) -> $"Optional<{typeToString innerType}>"
        | Aggregate (List innerType) -> $"List<{typeToString innerType}>"
        | Aggregate (FixedList (innerType, length)) -> $"FixedList<{typeToString innerType}, {length}>"
        | Aggregate (Map (keyType, valueType)) -> $"Map<{typeToString keyType}, {typeToString valueType}>"
        | Aggregate (Union _) -> "Union"
        | Aggregate (Struct _) -> "Struct"
        | UserDefined name -> name
    
    /// <summary>
    /// Gets all types referenced by a type
    /// </summary>
    /// <param name="typ">The type to analyze</param>
    /// <returns>A sequence of referenced type names</returns>
    let rec getReferencedTypes (typ: Type): string seq =
        match typ with
        | Primitive _ -> Seq.empty
        | UserDefined name -> seq { yield name }
        | Aggregate agg ->
            match agg with
            | Optional innerType -> getReferencedTypes innerType
            | List innerType -> getReferencedTypes innerType
            | FixedList (innerType, _) -> getReferencedTypes innerType
            | Map (keyType, valueType) -> 
                Seq.append (getReferencedTypes keyType) (getReferencedTypes valueType)
            | Union cases -> 
                cases |> Map.values |> Seq.collect getReferencedTypes
            | Struct fields -> 
                fields |> Seq.collect (fun f -> getReferencedTypes f.Type)
    
    /// <summary>
    /// Validates type-specific invariants
    /// </summary>
    /// <param name="typeName">The name of the type being validated</param>
    /// <param name="typ">The type to validate</param>
    /// <returns>A sequence of validation errors, empty if valid</returns>
    let validateTypeInvariants (typeName: string) (typ: Type): ValidationError seq =
        let rec validateType path t =
            match t with
            | Primitive (Void) ->
                // Void can only be used in a union
                if not (isUnionContext path) then
                    seq { yield InvalidVoidUsage (typePathToString path) }
                else
                    Seq.empty
                    
            | Primitive (Enum values) ->
                // Enum must have at least one value
                if Map.isEmpty values then
                    seq { yield EmptyEnum }
                else
                    Seq.empty
            
            | Aggregate (Union cases) ->
                // Union must have at least one case
                if Map.isEmpty cases then
                    seq { yield EmptyUnion }
                else
                    cases |> Map.values |> Seq.collect (validateType (UnionCase :: path))
            
            | Aggregate (Struct fields) ->
                // Struct must have at least one field
                if List.isEmpty fields then
                    seq { yield EmptyStruct }
                else
                    fields |> Seq.collect (fun f -> validateType (StructField f.Name :: path) f.Type)
            
            | Aggregate (Map (keyType, valueType)) ->
                // Map keys must be primitive and not f32, f64, data, void
                let keyErrors =
                    match keyType with
                    | Primitive (F32) 
                    | Primitive (F64)
                    | Primitive (Data)
                    | Primitive (FixedData _)
                    | Primitive (Void) ->
                        seq { yield InvalidMapKeyType (typeToString keyType) }
                    | Primitive _ -> Seq.empty
                    | _ -> 
                        validateType (MapKey :: path) keyType
                
                let valueErrors = validateType (MapValue :: path) valueType
                Seq.append keyErrors valueErrors
            
            | Aggregate (Optional innerType) ->
                validateType (OptionalValue :: path) innerType
            
            | Aggregate (List innerType) ->
                validateType (ListItem :: path) innerType
            
            | Aggregate (FixedList (innerType, length)) ->
                if length <= 0 then
                    seq { yield InvalidFixedLength (length, typePathToString path) }
                else
                    validateType (ListItem :: path) innerType
            
            | _ -> Seq.empty
        
        validateType [TypeRoot typeName] typ
    
    /// <summary>
    /// Validates a schema definition
    /// </summary>
    /// <param name="schema">The draft schema to validate</param>
    /// <returns>A result containing the validated schema or a list of validation errors</returns>
    let validate (schema: SchemaDefinition<draft>): Result<SchemaDefinition<validated>, ValidationError list> =
        // Step 1: Check that the root type exists
        if not (Map.containsKey schema.Root schema.Types) then
            Error [UndefinedType schema.Root]
        else
            // Step 2: Check for cyclic references
            let detectCycles () =
                let rec visit visited path typeName =
                    if List.contains typeName path then
                        // Cycle detected
                        Some (CyclicTypeReference typeName)
                    else if Set.contains typeName visited then
                        // Already visited, no cycle
                        None
                    else
                        match Map.tryFind typeName schema.Types with
                        | None -> Some (UndefinedType typeName)
                        | Some typ ->
                            // Check referenced types
                            let referencedTypes = getReferencedTypes typ
                            let newPath = typeName :: path
                            let newVisited = Set.add typeName visited
                            
                            Seq.tryPick (fun t -> visit newVisited newPath t) referencedTypes
                
                Map.keys schema.Types
                |> Seq.tryPick (fun typeName -> visit Set.empty [] typeName)
            
            match detectCycles () with
            | Some error -> Error [error]
            | None ->
                // Step 3: Check invariants for each type
                let invariantErrors =
                    schema.Types
                    |> Map.toSeq
                    |> Seq.collect (fun (name, typ) -> validateTypeInvariants name typ)
                    |> Seq.toList
                
                if not (List.isEmpty invariantErrors) then
                    Error invariantErrors
                else
                    // All checks passed
                    Ok ({ Types = schema.Types; Root = schema.Root })