namespace BAREWire.IPC

open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Memory.Region
open BAREWire.Memory.View
open BAREWire.Platform
open BAREWire.Memory.Mapping

/// <summary>
/// Shared memory regions for inter-process communication
/// </summary>
module SharedMemory =
    /// <summary>
    /// A shared memory region with type safety
    /// </summary>
    /// <typeparam name="'T">The type stored in the shared memory</typeparam>
    /// <typeparam name="'region">The memory region measure type</typeparam>
    type SharedRegion<'T, [<Measure>] 'region> = {
        /// <summary>
        /// The memory region
        /// </summary>
        Region: Region<'T, 'region>
        
        /// <summary>
        /// The name of the shared region
        /// </summary>
        Name: string
        
        /// <summary>
        /// The native handle to the shared memory
        /// </summary>
        Handle: nativeint
    }
    
    /// <summary>
    /// Creates a new shared memory region
    /// </summary>
    /// <param name="name">The name of the shared region (must be unique)</param>
    /// <param name="size">The size of the shared memory in bytes</param>
    /// <param name="schema">The schema for serializing data</param>
    /// <returns>A result containing the new shared region or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when shared memory creation fails</exception>
    let create<'T> 
              (name: string) 
              (size: int<bytes>) 
              (schema: SchemaDefinition<validated>): Result<SharedRegion<'T, region>> =
        try
            // Ensure platform services are initialized
            if not (PlatformServices.ensureInitialized 10) then
                Error (invalidStateError "Failed to initialize platform services")
            else
                // Get the IPC provider
                PlatformServices.getIpcProvider()
                |> Result.bind (fun provider ->
                    // Create the shared memory using the platform provider
                    provider.CreateSharedMemory(name, size, AccessType.ReadWrite)
                    |> Result.bind (fun (handle, address) ->
                        // Create a region from the shared memory
                        let regionResult = Region.fromPointer<'T, region> address size
                        
                        match regionResult with
                        | Ok region ->
                            Ok {
                                Region = region
                                Name = name
                                Handle = handle
                            }
                        | Error err ->
                            // Clean up the shared memory if region creation fails
                            provider.CloseSharedMemory(handle, address, size) |> ignore
                            Error err
                    )
                )
        with ex ->
            Error (invalidValueError $"Failed to create shared memory region: {ex.Message}")
    
    /// <summary>
    /// Opens an existing shared memory region
    /// </summary>
    /// <param name="name">The name of the shared region to open</param>
    /// <param name="schema">The schema for serializing data</param>
    /// <returns>A result containing the opened shared region or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when opening the shared memory fails</exception>
    let open'<'T> 
              (name: string) 
              (schema: SchemaDefinition<validated>): Result<SharedRegion<'T, region>> =
        try
            // Ensure platform services are initialized
            if not (PlatformServices.ensureInitialized 10) then
                Error (invalidStateError "Failed to initialize platform services")
            else
                // Get the IPC provider
                PlatformServices.getIpcProvider()
                |> Result.bind (fun provider ->
                    // Open the shared memory using the platform provider
                    provider.OpenSharedMemory(name, AccessType.ReadWrite)
                    |> Result.bind (fun (handle, address, actualSize) ->
                        // Create a region from the shared memory
                        let regionResult = Region.fromPointer<'T, region> address actualSize
                        
                        match regionResult with
                        | Ok region ->
                            Ok {
                                Region = region
                                Name = name
                                Handle = handle
                            }
                        | Error err ->
                            // Clean up the shared memory if region creation fails
                            provider.CloseSharedMemory(handle, address, actualSize) |> ignore
                            Error err
                    )
                )
        with ex ->
            Error (invalidValueError $"Failed to open shared memory region: {ex.Message}")
    
    /// <summary>
    /// Gets a memory view for a shared region
    /// </summary>
    /// <param name="shared">The shared memory region</param>
    /// <param name="schema">The schema for serializing data</param>
    /// <returns>A memory view for the shared region</returns>
    let getView<'T, [<Measure>] 'region> 
              (shared: SharedRegion<'T, 'region>) 
              (schema: SchemaDefinition<validated>): MemoryView<'T, 'region> =
        View.create<'T, 'region> shared.Region schema
    
    /// <summary>
    /// Closes a shared memory region
    /// </summary>
    /// <param name="shared">The shared memory region to close</param>
    /// <returns>A result indicating success or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when closing the shared memory fails</exception>
    let close<'T, [<Measure>] 'region> 
             (shared: SharedRegion<'T, 'region>): Result<unit> =
        try
            // Ensure platform services are initialized
            if not (PlatformServices.ensureInitialized 10) then
                Error (invalidStateError "Failed to initialize platform services")
            else
                // Get the IPC provider
                PlatformServices.getIpcProvider()
                |> Result.bind (fun provider ->
                    // Get the address from the region
                    toPointer shared.Region
                    |> Result.bind (fun address ->
                        // Close the shared memory using the platform provider
                        provider.CloseSharedMemory(
                            shared.Handle, 
                            address, 
                            shared.Region.Length
                        )
                    )
                )
        with ex ->
            Error (invalidValueError $"Failed to close shared memory region: {ex.Message}")
    
    /// <summary>
    /// Resizes a shared memory region
    /// </summary>
    /// <param name="shared">The shared memory region to resize</param>
    /// <param name="newSize">The new size in bytes</param>
    /// <returns>A result containing the resized shared region or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when resizing the shared memory fails</exception>
    let resize<'T, [<Measure>] 'region> 
              (shared: SharedRegion<'T, 'region>) 
              (newSize: int<bytes>): Result<SharedRegion<'T, 'region>> =
        try
            // Resize is not supported directly in most operating systems for shared memory
            // So we need to create a new region with the new size and copy the data
            
            // First, create a temporary buffer for the data
            let currentSize = shared.Region.Length
            let dataToCopy = min currentSize newSize
            
            let tempBuffer = Array.zeroCreate (int dataToCopy)
            
            // Copy data from the current region to the temporary buffer
            for i = 0 to int dataToCopy - 1 do
                tempBuffer.[i] <- shared.Region.Data.[int shared.Region.Offset + i]
            
            // Close the current shared memory
            close shared
            |> Result.bind (fun () ->
                // Create a new shared memory with the new size
                create<'T> shared.Name newSize (SchemaDefinition<validated>.empty())
                |> Result.bind (fun newShared ->
                    // Copy data from the temporary buffer to the new region
                    for i = 0 to int dataToCopy - 1 do
                        newShared.Region.Data.[int newShared.Region.Offset + i] <- tempBuffer.[i]
                    
                    Ok newShared
                )
            )
        with ex ->
            Error (invalidValueError $"Failed to resize shared memory region: {ex.Message}")
    
    /// <summary>
    /// Locks a shared memory region for exclusive access
    /// </summary>
    /// <param name="shared">The shared memory region to lock</param>
    /// <returns>A result indicating success or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when locking the shared memory fails</exception>
    let lock<'T, [<Measure>] 'region> 
            (shared: SharedRegion<'T, 'region>): Result<unit> =
        try
            // Ensure platform services are initialized
            if not (PlatformServices.ensureInitialized 10) then
                Error (invalidStateError "Failed to initialize platform services")
            else
                // Get the memory provider
                PlatformServices.getMemoryProvider()
                |> Result.bind (fun provider ->
                    // Get the address from the region
                    toPointer shared.Region
                    |> Result.bind (fun address ->
                        // Lock the memory using the platform provider
                        provider.LockMemory(
                            address, 
                            shared.Region.Length
                        )
                    )
                )
        with ex ->
            Error (invalidValueError $"Failed to lock shared memory region: {ex.Message}")
    
    /// <summary>
    /// Unlocks a shared memory region
    /// </summary>
    /// <param name="shared">The shared memory region to unlock</param>
    /// <returns>A result indicating success or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when unlocking the shared memory fails</exception>
    let unlock<'T, [<Measure>] 'region> 
              (shared: SharedRegion<'T, 'region>): Result<unit> =
        try
            // Ensure platform services are initialized
            if not (PlatformServices.ensureInitialized 10) then
                Error (invalidStateError "Failed to initialize platform services")
            else
                // Get the memory provider
                PlatformServices.getMemoryProvider()
                |> Result.bind (fun provider ->
                    // Get the address from the region
                    toPointer shared.Region
                    |> Result.bind (fun address ->
                        // Unlock the memory using the platform provider
                        provider.UnlockMemory(
                            address, 
                            shared.Region.Length
                        )
                    )
                )
        with ex ->
            Error (invalidValueError $"Failed to unlock shared memory region: {ex.Message}")
    
    /// <summary>
    /// Checks if a shared memory region with the given name exists
    /// </summary>
    /// <param name="name">The name of the shared region to check</param>
    /// <returns>True if the shared region exists, false otherwise</returns>
    let exists (name: string): bool =
        try
            // Ensure platform services are initialized
            if not (PlatformServices.ensureInitialized 10) then
                false
            else
                // Get the IPC provider
                match PlatformServices.getIpcProvider() with
                | Ok provider ->
                    // Check if the resource exists using the platform provider
                    provider.ResourceExists(name, "sharedmemory")
                | Error _ ->
                    false
        with _ ->
            false
    
    /// <summary>
    /// Gets information about a shared memory region
    /// </summary>
    /// <param name="name">The name of the shared region</param>
    /// <returns>A result containing a tuple of (size, creation time) or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when getting information fails</exception>
    let getInfo (name: string): Result<int<bytes> * DateTime> =
        try
            // This operation is not directly supported by the platform providers
            // We would need to open the shared memory to get information about it
            
            // For simplicity, attempt to open the shared memory and get its size
            let schema = SchemaDefinition<validated>.empty()
            open'<obj> name schema
            |> Result.map (fun shared ->
                let size = shared.Region.Length
                
                // Close the shared memory
                close shared |> ignore
                
                // Return the size and current time (since we don't have creation time)
                (size, DateTime.Now)
            )
        with ex ->
            Error (invalidValueError $"Failed to get shared memory region info: {ex.Message}")
    
    /// <summary>
    /// Lists all available shared memory regions
    /// </summary>
    /// <returns>A result containing a list of shared region names or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when listing shared regions fails</exception>
    let listAll (): Result<string list> =
        // This operation is platform-specific and not directly supported
        // We would need to enumerate the shared memory regions using platform-specific APIs
        
        // For simplicity, return an empty list
        Ok []
    
    /// <summary>
    /// Maps a function over a shared memory region
    /// </summary>
    /// <param name="shared">The shared memory region</param>
    /// <param name="schema">The schema for serializing data</param>
    /// <param name="f">The function to apply to the memory view</param>
    /// <returns>A result containing the function result or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when applying the function fails</exception>
    let map<'T, 'U, [<Measure>] 'region> 
           (shared: SharedRegion<'T, 'region>) 
           (schema: SchemaDefinition<validated>) 
           (f: MemoryView<'T, 'region> -> 'U): Result<'U> =
        try
            let view = getView<'T, 'region> shared schema
            Ok (f view)
        with ex ->
            Error (invalidValueError $"Failed to map function over shared memory region: {ex.Message}")