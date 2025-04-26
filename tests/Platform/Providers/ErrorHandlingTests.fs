namespace BAREWire.Tests.Platform.Providers

open Expecto
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Core.Memory
open BAREWire.Memory.Mapping
open BAREWire.IPC
open BAREWire.Platform.Common.Interfaces
open BAREWire.Platform.Common.Resource

/// <summary>
/// Tests focused on error handling in platform providers
/// </summary>
module ErrorHandlingTests =
    
    /// <summary>
    /// Mock provider that generates specific errors for testing
    /// </summary>
    module ErrorGeneratingProviders =
        
        /// <summary>
        /// Memory provider that generates expected errors
        /// </summary>
        type ErrorMemoryProvider() =
            interface IPlatformMemory with
                member _.MapMemory size mappingType accessType =
                    match size with
                    | s when s <= 0<bytes> -> 
                        Error (invalidValueError "Invalid memory size")
                    | s when s > 1024*1024*1024<bytes> -> 
                        Error (resourceError "Requested memory size exceeds available memory")
                    | _ -> 
                        Ok (1n, 1n)
                
                member _.UnmapMemory handle address size =
                    match handle with
                    | h when h = 0n -> 
                        Error (invalidValueError "Invalid handle")
                    | h when h = -1n -> 
                        Error (notFoundError "Memory region not found")
                    | _ -> 
                        Ok ()
                
                member _.MapFile filePath offset size accessType =
                    match filePath with
                    | null | "" -> 
                        Error (invalidValueError "Invalid file path")
                    | path when path.Contains("nonexistent") -> 
                        Error (notFoundError "File not found")
                    | path when path.Contains("noaccess") -> 
                        Error (accessError "Access denied")
                    | _ -> 
                        Ok (1n, 1n)
                
                member _.FlushMappedFile handle address size =
                    match handle with
                    | h when h = 0n -> 
                        Error (invalidValueError "Invalid handle")
                    | h when h = -1n -> 
                        Error (notFoundError "Memory mapping not found")
                    | _ -> 
                        Ok ()
                
                member _.LockMemory address size =
                    match address with
                    | a when a = 0n -> 
                        Error (invalidValueError "Invalid address")
                    | a when a = -1n -> 
                        Error (resourceError "Unable to lock memory (insufficient privileges)")
                    | _ -> 
                        Ok ()
                
                member _.UnlockMemory address size =
                    match address with
                    | a when a = 0n -> 
                        Error (invalidValueError "Invalid address")
                    | a when a = -1n -> 
                        Error (invalidStateError "Memory not locked")
                    | _ -> 
                        Ok ()
        
        /// <summary>
        /// IPC provider that generates expected errors
        /// </summary>
        type ErrorIpcProvider() =
            interface IPlatformIpc with
                member _.CreateNamedPipe name direction mode bufferSize =
                    match name with
                    | null | "" -> 
                        Error (invalidValueError "Invalid pipe name")
                    | n when n.Contains("existing") -> 
                        Error (resourceError "Pipe already exists")
                    | _ -> 
                        Ok 1n
                
                member _.ConnectNamedPipe name direction =
                    match name with
                    | null | "" -> 
                        Error (invalidValueError "Invalid pipe name")
                    | n when n.Contains("nonexistent") -> 
                        Error (notFoundError "Pipe not found")
                    | _ -> 
                        Ok 1n
                
                member _.WaitForNamedPipeConnection handle timeout =
                    match handle, timeout with
                    | h, _ when h = 0n -> 
                        Error (invalidValueError "Invalid handle")
                    | _, t when t < 0 && t <> -1 -> 
                        Error (invalidValueError "Invalid timeout")
                    | h, t when h = -1n && t <> -1 -> 
                        Error (timeoutError "Connection attempt timed out")
                    | _ -> 
                        Ok ()
                
                member _.WriteNamedPipe handle data offset count =
                    match handle, data, offset, count with
                    | h, _, _, _ when h = 0n -> 
                        Error (invalidValueError "Invalid handle")
                    | _, null, _, _ -> 
                        Error (invalidValueError "Data cannot be null")
                    | _, d, o, c when o < 0 || c < 0 || o + c > d.Length -> 
                        Error (invalidValueError "Invalid offset or count")
                    | h, _, _, _ when h = -1n -> 
                        Error (brokenError "Pipe is broken")
                    | h, _, _, _ when h = -2n -> 
                        Error (accessError "Cannot write to read-only pipe")
                    | _ -> 
                        Ok count
                
                member _.ReadNamedPipe handle buffer offset count =
                    match handle, buffer, offset, count with
                    | h, _, _, _ when h = 0n -> 
                        Error (invalidValueError "Invalid handle")
                    | _, null, _, _ -> 
                        Error (invalidValueError "Buffer cannot be null")
                    | _, b, o, c when o < 0 || c < 0 || o + c > b.Length -> 
                        Error (invalidValueError "Invalid offset or count")
                    | h, _, _, _ when h = -1n -> 
                        Error (brokenError "Pipe is broken")
                    | h, _, _, _ when h = -2n -> 
                        Error (accessError "Cannot read from write-only pipe")
                    | _ -> 
                        Ok count
                
                member _.CloseNamedPipe handle =
                    match handle with
                    | h when h = 0n -> 
                        Error (invalidValueError "Invalid handle")
                    | h when h = -1n -> 
                        Error (invalidStateError "Pipe already closed")
                    | _ -> 
                        Ok ()
                
                member _.CreateSharedMemory name size accessType =
                    match name, size with
                    | null | "", _ -> 
                        Error (invalidValueError "Invalid shared memory name")
                    | n, _ when n.Contains("existing") -> 
                        Error (resourceError "Shared memory already exists")
                    | _, s when s <= 0<bytes> -> 
                        Error (invalidValueError "Invalid memory size")
                    | _ -> 
                        Ok (1n, 1n)
                
                member _.OpenSharedMemory name accessType =
                    match name with
                    | null | "" -> 
                        Error (invalidValueError "Invalid shared memory name")
                    | n when n.Contains("nonexistent") -> 
                        Error (notFoundError "Shared memory not found")
                    | n when n.Contains("noaccess") -> 
                        Error (accessError "Access denied")
                    | _ -> 
                        Ok (1n, 1n, 4096<bytes>)
                
                member _.CloseSharedMemory handle address size =
                    match handle with
                    | h when h = 0n -> 
                        Error (invalidValueError "Invalid handle")
                    | h when h = -1n -> 
                        Error (invalidStateError "Shared memory already closed")
                    | _ -> 
                        Ok ()
                
                member _.ResourceExists name resourceType =
                    not (name.Contains("nonexistent"))
    
    [<Tests>]
    let errorHandlingMemoryTests =
        testList "Memory Provider Error Handling Tests" [
            test "MapMemory with invalid size returns appropriate error" {
                let provider = ErrorGeneratingProviders.ErrorMemoryProvider() :> IPlatformMemory
                
                // Test with invalid size
                match provider.MapMemory 0<bytes> MappingType.PrivateMapping AccessType.ReadWrite with
                | Ok _ -> 
                    failwith "Should have returned error for invalid size"
                | Error e -> 
                    Expect.equal e.Kind ErrorKind.InvalidValue "Should return InvalidValue error kind"
                    Expect.stringContains e.Message "Invalid memory size" "Error message should mention invalid size"
            }
            
            test "MapMemory with excessive size returns resource error" {
                let provider = ErrorGeneratingProviders.ErrorMemoryProvider() :> IPlatformMemory
                
                // Test with too large size
                let hugeSize = 2*1024*1024*1024<bytes> // 2 GB
                match provider.MapMemory hugeSize MappingType.PrivateMapping AccessType.ReadWrite with
                | Ok _ -> 
                    failwith "Should have returned error for excessive size"
                | Error e -> 
                    Expect.equal e.Kind ErrorKind.Resource "Should return Resource error kind"
                    Expect.stringContains e.Message "exceeds available" "Error message should mention exceeding available memory"
            }
            
            test "UnmapMemory with invalid handle returns appropriate error" {
                let provider = ErrorGeneratingProviders.ErrorMemoryProvider() :> IPlatformMemory
                
                // Test with invalid handle
                match provider.UnmapMemory 0n 1n 4096<bytes> with
                | Ok _ -> 
                    failwith "Should have returned error for invalid handle"
                | Error e -> 
                    Expect.equal e.Kind ErrorKind.InvalidValue "Should return InvalidValue error kind"
                    Expect.stringContains e.Message "Invalid handle" "Error message should mention invalid handle"
            }
            
            test "MapFile with invalid path returns appropriate error" {
                let provider = ErrorGeneratingProviders.ErrorMemoryProvider() :> IPlatformMemory
                
                // Test with null path
                match provider.MapFile null 0L 4096<bytes> AccessType.ReadOnly with
                | Ok _ -> 
                    failwith "Should have returned error for null path"
                | Error e -> 
                    Expect.equal e.Kind ErrorKind.InvalidValue "Should return InvalidValue error kind"
                    Expect.stringContains e.Message "Invalid file path" "Error message should mention invalid path"
                    
                // Test with non-existent file
                match provider.MapFile "nonexistent.dat" 0L 4096<bytes> AccessType.ReadOnly with
                | Ok _ -> 
                    failwith "Should have returned error for non-existent file"
                | Error e -> 
                    Expect.equal e.Kind ErrorKind.NotFound "Should return NotFound error kind"
                    Expect.stringContains e.Message "File not found" "Error message should mention file not found"
                    
                // Test with access denied
                match provider.MapFile "noaccess.dat" 0L 4096<bytes> AccessType.ReadOnly with
                | Ok _ -> 
                    failwith "Should have returned error for access denied"
                | Error e -> 
                    Expect.equal e.Kind ErrorKind.Access "Should return Access error kind"
                    Expect.stringContains e.Message "Access denied" "Error message should mention access denied"
            }
        ]
        
    [<Tests>]
    let errorHandlingIpcTests =
        testList "IPC Provider Error Handling Tests" [
            test "CreateNamedPipe with invalid name returns appropriate error" {
                let provider = ErrorGeneratingProviders.ErrorIpcProvider() :> IPlatformIpc
                
                // Test with null name
                match provider.CreateNamedPipe null NamedPipe.PipeDirection.InOut NamedPipe.PipeMode.Message 4096<bytes> with
                | Ok _ -> 
                    failwith "Should have returned error for null name"
                | Error e -> 
                    Expect.equal e.Kind ErrorKind.InvalidValue "Should return InvalidValue error kind"
                    Expect.stringContains e.Message "Invalid pipe name" "Error message should mention invalid name"
            }
            
            test "CreateNamedPipe with existing pipe returns resource error" {
                let provider = ErrorGeneratingProviders.ErrorIpcProvider() :> IPlatformIpc
                
                // Test with existing pipe
                match provider.CreateNamedPipe "existing_pipe" NamedPipe.PipeDirection.InOut NamedPipe.PipeMode.Message 4096<bytes> with
                | Ok _ -> 
                    failwith "Should have returned error for existing pipe"
                | Error e -> 
                    Expect.equal e.Kind ErrorKind.Resource "Should return Resource error kind"
                    Expect.stringContains e.Message "already exists" "Error message should mention pipe already exists"
            }
            
            test "ConnectNamedPipe with non-existent pipe returns not found error" {
                let provider = ErrorGeneratingProviders.ErrorIpcProvider() :> IPlatformIpc
                
                // Test with non-existent pipe
                match provider.ConnectNamedPipe "nonexistent_pipe" NamedPipe.PipeDirection.InOut with
                | Ok _ -> 
                    failwith "Should have returned error for non-existent pipe"
                | Error e -> 
                    Expect.equal e.Kind ErrorKind.NotFound "Should return NotFound error kind"
                    Expect.stringContains e.Message "not found" "Error message should mention pipe not found"
            }
            
            test "WaitForNamedPipeConnection with timeout returns timeout error" {
                let provider = ErrorGeneratingProviders.ErrorIpcProvider() :> IPlatformIpc
                
                // Test with timeout
                match provider.WaitForNamedPipeConnection -1n 1000 with
                | Ok _ -> 
                    failwith "Should have returned error for timeout"
                | Error e -> 
                    Expect.equal e.Kind ErrorKind.Timeout "Should return Timeout error kind"
                    Expect.stringContains e.Message "timed out" "Error message should mention connection timed out"
            }
            
            test "WriteNamedPipe with broken pipe returns broken error" {
                let provider = ErrorGeneratingProviders.ErrorIpcProvider() :> IPlatformIpc
                
                // Test with broken pipe
                let data = [| 1uy; 2uy; 3uy; 4uy |]
                match provider.WriteNamedPipe -1n data 0 data.Length with
                | Ok _ -> 
                    failwith "Should have returned error for broken pipe"
                | Error e -> 
                    Expect.equal e.Kind ErrorKind.Broken "Should return Broken error kind"
                    Expect.stringContains e.Message "broken" "Error message should mention pipe is broken"
            }
            
            test "ReadNamedPipe with invalid parameters returns appropriate errors" {
                let provider = ErrorGeneratingProviders.ErrorIpcProvider() :> IPlatformIpc
                
                // Test with null buffer
                match provider.ReadNamedPipe 1n null 0 10 with
                | Ok _ -> 
                    failwith "Should have returned error for null buffer"
                | Error e -> 
                    Expect.equal e.Kind ErrorKind.InvalidValue "Should return InvalidValue error kind"
                    Expect.stringContains e.Message "Buffer cannot be null" "Error message should mention buffer cannot be null"
                
                // Test with invalid offset/count
                let buffer = Array.zeroCreate<byte> 10
                match provider.ReadNamedPipe 1n buffer 5 10 with
                | Ok _ -> 
                    failwith "Should have returned error for invalid offset/count"
                | Error e -> 
                    Expect.equal e.Kind ErrorKind.InvalidValue "Should return InvalidValue error kind"
                    Expect.stringContains e.Message "Invalid offset or count" "Error message should mention invalid offset or count"
            }
            
            test "OpenSharedMemory errors are handled appropriately" {
                let provider = ErrorGeneratingProviders.ErrorIpcProvider() :> IPlatformIpc
                
                // Test with non-existent shared memory
                match provider.OpenSharedMemory "nonexistent_memory" AccessType.ReadWrite with
                | Ok _ -> 
                    failwith "Should have returned error for non-existent shared memory"
                | Error e -> 
                    Expect.equal e.Kind ErrorKind.NotFound "Should return NotFound error kind"
                    Expect.stringContains e.Message "not found" "Error message should mention shared memory not found"
                
                // Test with access denied
                match provider.OpenSharedMemory "noaccess_memory" AccessType.ReadWrite with
                | Ok _ -> 
                    failwith "Should have returned error for access denied"
                | Error e -> 
                    Expect.equal e.Kind ErrorKind.Access "Should return Access error kind"
                    Expect.stringContains e.Message "Access denied" "Error message should mention access denied"
            }
        ]
        
    [<Tests>]
    let resourceErrorRecoveryTests =
        testList "Resource Error Recovery Tests" [
            test "TryCreate handles and recovers from errors" {
                // This test demonstrates how to use Resource.tryCreate to handle errors in resource acquisition
                
                let createErrorResource() =
                    // Simulates a resource creation that might fail
                    if System.Random().Next(2) = 0 then
                        failwith "Simulated random failure in resource creation"
                    else
                        "resource value"
                        
                let disposeResource resource =
                    // Simulates resource disposal
                    ()
                
                // Try to create the resource multiple times until success
                let mutable success = false
                let mutable attempts = 0
                let mutable result = None
                
                while not success && attempts < 5 do
                    attempts <- attempts + 1
                    
                    match tryCreate createErrorResource disposeResource with
                    | Ok resource ->
                        success <- true
                        result <- Some resource
                    | Error e ->
                        // Log the error and retry
                        printfn "Attempt %d failed: %s" attempts e.Message
                
                // Verify we eventually succeeded or ran out of attempts
                if success then
                    match result with
                    | Some resource ->
                        Expect.equal resource.Value "resource value" "Resource should have correct value"
                        resource.Dispose() // Clean up
                    | None ->
                        failwith "Resource should not be None when success is true"
                else
                    Expect.equal attempts 5 "Should have attempted 5 times before giving up"
            }
            
            test "CombineArray with partial failures" {
                // Test creating a collection of resources where some might fail
                
                // Create some resources that will succeed and some that will fail
                let resources = [|
                    tryCreate (fun () -> "resource1") (fun _ -> ())
                    tryCreate (fun () -> "resource2") (fun _ -> ())
                    tryCreate (fun () -> failwith "Simulated failure"; "resource3") (fun _ -> ())
                    tryCreate (fun () -> "resource4") (fun _ -> ())
                |]
                
                // Filter out failures
                let successfulResources = 
                    resources
                    |> Array.choose (function
                        | Ok resource -> Some resource
                        | Error _ -> None)
                
                // Combine the successful resources
                if successfulResources.Length > 0 then
                    let combined = combineArray successfulResources
                    
                    Expect.equal combined.Value.Length 3 "Should have 3 successful resources"
                    Expect.sequenceEqual combined.Value [| "resource1"; "resource2"; "resource4" |] "Combined resources have correct values"
                    
                    combined.Dispose() // Clean up
                else
                    failwith "Should have at least some successful resources"
            }
            
            test "Apply with error recovery" {
                // Test using apply to handle operations that might fail
                
                let provider = ErrorGeneratingProviders.ErrorIpcProvider() :> IPlatformIpc
                
                // Create a resource
                let resourceResult = 
                    tryCreate (fun () -> 
                        // Try to create a pipe
                        match provider.CreateNamedPipe "test_pipe" NamedPipe.PipeDirection.InOut NamedPipe.PipeMode.Message 4096<bytes> with
                        | Ok handle -> handle
                        | Error e -> failwith $"Failed to create pipe: {e.Message}"
                    ) (fun handle ->
                        // Dispose by closing the pipe
                        let _ = provider.CloseNamedPipe handle
                        ()
                    )
                
                match resourceResult with
                | Ok resource ->
                    // Use the resource with an operation that might fail
                    let result = apply (fun handle ->
                        // Try to write to the pipe (success case)
                        let data = [| 1uy; 2uy; 3uy; 4uy |]
                        provider.WriteNamedPipe handle data 0 data.Length
                    ) resource
                    
                    match result with
                    | Ok bytesWritten ->
                        Expect.equal bytesWritten 4 "Should have written all bytes"
                    | Error e ->
                        failwith $"Write operation failed: {e.Message}"
                | Error e ->
                    failwith $"Resource creation failed: {e.Message}"
            }
            
            test "Bind for resource composition with error handling" {
                // Test using bind to chain resource operations with error handling
                
                let provider = ErrorGeneratingProviders.ErrorIpcProvider() :> IPlatformIpc
                
                // Create a named pipe resource
                let pipeResult = 
                    tryCreate (fun () -> 
                        match provider.CreateNamedPipe "test_pipe" NamedPipe.PipeDirection.InOut NamedPipe.PipeMode.Message 4096<bytes> with
                        | Ok handle -> handle
                        | Error e -> failwith $"Failed to create pipe: {e.Message}"
                    ) (fun handle ->
                        let _ = provider.CloseNamedPipe handle
                        ()
                    )
                
                match pipeResult with
                | Ok pipeResource ->
                    // Bind to create a shared memory resource
                    let combinedResult = 
                        bind (fun pipeHandle ->
                            tryCreate (fun () ->
                                match provider.CreateSharedMemory "test_memory" 4096<bytes> AccessType.ReadWrite with
                                | Ok (memHandle, memAddress) -> (pipeHandle, memHandle, memAddress)
                                | Error e -> failwith $"Failed to create shared memory: {e.Message}"
                            ) (fun (_, memHandle, memAddress) ->
                                let _ = provider.CloseSharedMemory memHandle memAddress 4096<bytes>
                                ()
                            )
                        ) pipeResource
                    
                    match combinedResult with
                    | Ok combinedResource ->
                        // Use the combined resources
                        Expect.isTrue true "Successfully created and composed resources"
                        
                        // The resources will be automatically disposed when they go out of scope
                        combinedResource.Dispose()
                    | Error e ->
                        failwith $"Failed to compose resources: {e.Message}"
                | Error e ->
                    failwith $"Failed to create pipe resource: {e.Message}"
            }
        ]
        
    let tests = 
        testList "Error Handling Tests" [
            errorHandlingMemoryTests
            errorHandlingIpcTests
            resourceErrorRecoveryTests
        ]

module Program =
    [<EntryPoint>]
    let main args =
        runTestsWithArgs defaultConfig args ErrorHandlingTests.tests