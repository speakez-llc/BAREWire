namespace BAREWire.Platform.Common

open BAREWire.Core.Error

/// <summary>
/// Resource management for platform-specific resources
/// </summary>
module Resource =
    /// <summary>
    /// A disposable resource wrapper
    /// </summary>
    /// <typeparam name="'T">The type of resource</typeparam>
    type Resource<'T> = {
        /// <summary>
        /// The resource value
        /// </summary>
        Value: 'T
        
        /// <summary>
        /// Function to dispose the resource
        /// </summary>
        Dispose: unit -> unit
    }
    
    /// <summary>
    /// Creates a resource with a disposal function
    /// </summary>
    /// <param name="value">The resource value</param>
    /// <param name="dispose">Function to dispose the resource</param>
    /// <returns>A wrapped resource</returns>
    let create<'T> (value: 'T) (dispose: unit -> unit): Resource<'T> =
        { Value = value; Dispose = dispose }
    
    /// <summary>
    /// Uses a resource and ensures it is properly disposed
    /// </summary>
    /// <param name="resource">The resource to use</param>
    /// <param name="f">Function that uses the resource</param>
    /// <returns>The result of the function</returns>
    let use'<'T, 'U> (resource: Resource<'T>) (f: 'T -> 'U): 'U =
        try
            f resource.Value
        finally
            resource.Dispose()
    
    /// <summary>
    /// Maps a resource to a new type while preserving the disposal behavior
    /// </summary>
    /// <param name="f">Function to map the resource value</param>
    /// <param name="resource">The resource to map</param>
    /// <returns>A new resource with the mapped value</returns>
    let map<'T, 'U> (f: 'T -> 'U) (resource: Resource<'T>): Resource<'U> =
        { Value = f resource.Value; Dispose = resource.Dispose }
    
    /// <summary>
    /// Combines two resources into one, with a combining function for the values
    /// and a disposal function that disposes both resources
    /// </summary>
    /// <param name="f">Function to combine resource values</param>
    /// <param name="resource1">The first resource</param>
    /// <param name="resource2">The second resource</param>
    /// <returns>A combined resource</returns>
    let combine<'T1, 'T2, 'U> 
               (f: 'T1 -> 'T2 -> 'U) 
               (resource1: Resource<'T1>) 
               (resource2: Resource<'T2>): Resource<'U> =
        {
            Value = f resource1.Value resource2.Value
            Dispose = fun () -> 
                try
                    resource1.Dispose()
                finally
                    resource2.Dispose()
        }
    
    /// <summary>
    /// Applies a resource-producing function to create a new resource
    /// </summary>
    /// <param name="f">Function to create a new resource from the value of the first</param>
    /// <param name="resource">The source resource</param>
    /// <returns>A result containing the new resource or an error</returns>
    let bind<'T, 'U> 
            (f: 'T -> Result<Resource<'U>>) 
            (resource: Resource<'T>): Result<Resource<'U>> =
        try
            f resource.Value
            |> Result.map (fun innerResource ->
                {
                    Value = innerResource.Value
                    Dispose = fun () ->
                        try
                            innerResource.Dispose()
                        finally
                            resource.Dispose()
                }
            )
        with ex ->
            resource.Dispose()
            Error (invalidValueError $"Failed to bind resource: {ex.Message}")
    
    /// <summary>
    /// Creates an array of resources with a single disposal function
    /// </summary>
    /// <param name="resources">Array of resources</param>
    /// <returns>A resource containing the array of values</returns>
    let combineArray<'T> (resources: Resource<'T>[]): Resource<'T[]> =
        {
            Value = resources |> Array.map (fun r -> r.Value)
            Dispose = fun () ->
                // Dispose all resources, ensuring all are attempted even if some fail
                let mutable exceptions = []
                for resource in resources do
                    try
                        resource.Dispose()
                    with ex ->
                        exceptions <- ex :: exceptions
                
                // If any disposals failed, raise an aggregate exception
                match exceptions with
                | [] -> ()
                | [ex] -> raise ex
                | _ -> 
                    let message = 
                        exceptions 
                        |> List.map (fun ex -> ex.Message) 
                        |> String.concat ", "
                    failwith $"Multiple exceptions during resource disposal: {message}"
        }
    
    /// <summary>
    /// Converts a native handle to a resource with proper disposal
    /// </summary>
    /// <param name="handle">The native handle</param>
    /// <param name="dispose">Function to dispose the handle</param>
    /// <returns>A resource wrapping the handle</returns>
    let fromHandle (handle: nativeint) (dispose: nativeint -> unit): Resource<nativeint> =
        {
            Value = handle
            Dispose = fun () -> dispose handle
        }
    
    /// <summary>
    /// Creates a resource that does nothing when disposed
    /// </summary>
    /// <param name="value">The resource value</param>
    /// <returns>A resource with no-op disposal</returns>
    let empty<'T> (value: 'T): Resource<'T> =
        { Value = value; Dispose = ignore }
    
    /// <summary>
    /// Tries to create a resource, handling exceptions by returning a Result
    /// </summary>
    /// <param name="createFn">Function to create the resource value</param>
    /// <param name="disposeFn">Function to dispose the resource</param>
    /// <returns>A result containing the resource or an error</returns>
    let tryCreate<'T> (createFn: unit -> 'T) (disposeFn: 'T -> unit): Result<Resource<'T>> =
        try
            let value = createFn()
            Ok { 
                Value = value
                Dispose = fun () -> disposeFn value
            }
        with ex ->
            Error (invalidValueError $"Failed to create resource: {ex.Message}")
    
    /// <summary>
    /// Safely maps a resource to a result
    /// </summary>
    /// <param name="f">Function that may fail</param>
    /// <param name="resource">The resource to map</param>
    /// <returns>A result containing the mapped value or an error</returns>
    let mapResult<'T, 'U> (f: 'T -> Result<'U>) (resource: Resource<'T>): Result<'U> =
        try
            f resource.Value
        finally
            resource.Dispose()
    
    /// <summary>
    /// Applies an operation to a resource and returns a result
    /// </summary>
    /// <param name="operation">Function that uses the resource and returns a result</param>
    /// <param name="resource">The resource to use</param>
    /// <returns>The result of the operation</returns>
    let apply<'T, 'U> (operation: 'T -> Result<'U>) (resource: Resource<'T>): Result<'U> =
        use'<'T, Result<'U>> resource operation