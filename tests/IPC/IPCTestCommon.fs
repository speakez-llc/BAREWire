module Tests.IPC.Common

open System
open System.Runtime.InteropServices
open Expecto
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Platform
open BAREWire.Memory.View
open BAREWire.Schema

/// Mock implementation of platform IPC provider for testing
type MockIpcProvider() =
    let mutable sharedMemories = Map.empty<string, byte[] * nativeint>
    let mutable namedPipes = Map.empty<string, System.Collections.Concurrent.ConcurrentQueue<byte[]> * nativeint>
    let mutable nextHandle = 1n
    
    interface IPlatformIpc with
        member this.CreateSharedMemory(name, size, access) =
            // Create a new byte array to represent the shared memory
            let memory = Array.zeroCreate (int size)
            let handle = nextHandle
            nextHandle <- nextHandle + 1n
            
            // Store the memory in our dictionary
            sharedMemories <- Map.add name (memory, handle) sharedMemories
            
            // Create a pinned reference to the memory
            let pinnedMemory = GCHandle.Alloc(memory, GCHandleType.Pinned)
            let address = pinnedMemory.AddrOfPinnedObject()
            
            Ok (handle, address)
            
        member this.OpenSharedMemory(name, access) =
            match Map.tryFind name sharedMemories with
            | Some (memory, handle) ->
                // Get a pinned reference to the memory
                let pinnedMemory = GCHandle.Alloc(memory, GCHandleType.Pinned)
                let address = pinnedMemory.AddrOfPinnedObject()
                
                Ok (handle, address, memory.Length * 1<bytes>)
            | None ->
                Error (invalidValueError $"Shared memory '{name}' not found")
                
        member this.CloseSharedMemory(handle, address, size) =
            // Find the shared memory by handle
            let entry = 
                sharedMemories 
                |> Map.filter (fun _ (_, h) -> h = handle) 
                |> Map.toList
                
            match entry with
            | [(name, _)] ->
                // Remove the shared memory
                sharedMemories <- Map.remove name sharedMemories
                
                // Free the pinned memory if possible
                try
                    let pinnedMemory = GCHandle.FromIntPtr(address)
                    if pinnedMemory.IsAllocated then
                        pinnedMemory.Free()
                with _ -> ()
                
                Ok ()
            | _ ->
                Error (invalidValueError $"Shared memory with handle {handle} not found")
                
        member this.CreateNamedPipe(name, direction, mode, bufferSize) =
            // Create a new queue to represent the pipe
            let queue = new System.Collections.Concurrent.ConcurrentQueue<byte[]>()
            let handle = nextHandle
            nextHandle <- nextHandle + 1n
            
            // Store the queue in our dictionary
            namedPipes <- Map.add name (queue, handle) namedPipes
            
            Ok handle
            
        member this.ConnectNamedPipe(name, direction) =
            match Map.tryFind name namedPipes with
            | Some (_, handle) ->
                Ok handle
            | None ->
                // In a real implementation, we might create the pipe if it doesn't exist
                let queue = new System.Collections.Concurrent.ConcurrentQueue<byte[]>()
                let handle = nextHandle
                nextHandle <- nextHandle + 1n
                
                // Store the queue in our dictionary
                namedPipes <- Map.add name (queue, handle) namedPipes
                
                Ok handle
                
        member this.WaitForNamedPipeConnection(handle, timeout) =
            // In this mock, we assume connections always succeed immediately
            Ok ()
            
        member this.WriteNamedPipe(handle, buffer, offset, count) =
            // Find the pipe by handle
            let entry = 
                namedPipes 
                |> Map.filter (fun _ (_, h) -> h = handle) 
                |> Map.toList
                
            match entry with
            | [(_, (queue, _))] ->
                // Copy the buffer
                let data = Array.zeroCreate count
                Array.Copy(buffer, offset, data, 0, count)
                
                // Add the data to the queue
                queue.Enqueue(data)
                
                Ok count
            | _ ->
                Error (invalidValueError $"Named pipe with handle {handle} not found")
                
        member this.ReadNamedPipe(handle, buffer, offset, count) =
            // Find the pipe by handle
            let entry = 
                namedPipes 
                |> Map.filter (fun _ (_, h) -> h = handle) 
                |> Map.toList
                
            match entry with
            | [(_, (queue, _))] ->
                // Try to get data from the queue
                match queue.TryDequeue() with
                | true, data ->
                    // Copy the data to the buffer
                    let bytesToCopy = min data.Length count
                    Array.Copy(data, 0, buffer, offset, bytesToCopy)
                    
                    Ok bytesToCopy
                | false, _ ->
                    // No data available
                    Ok 0
            | _ ->
                Error (invalidValueError $"Named pipe with handle {handle} not found")
                
        member this.CloseNamedPipe(handle) =
            // Find the pipe by handle
            let entry = 
                namedPipes 
                |> Map.filter (fun _ (_, h) -> h = handle) 
                |> Map.toList
                
            match entry with
            | [(name, _)] ->
                // Remove the pipe
                namedPipes <- Map.remove name namedPipes
                
                Ok ()
            | _ ->
                Error (invalidValueError $"Named pipe with handle {handle} not found")
                
        member this.ResourceExists(name, resourceType) =
            match resourceType with
            | "sharedmemory" -> Map.containsKey name sharedMemories
            | "pipe" -> Map.containsKey name namedPipes
            | _ -> false

/// Mock implementation of platform memory provider for testing
type MockMemoryProvider() =
    let mutable locks = Map.empty<nativeint, bool>
    
    interface IPlatformMemory with
        member this.LockMemory(address, size) =
            // Add the lock
            locks <- Map.add address true locks
            Ok ()
            
        member this.UnlockMemory(address, size) =
            // Remove the lock
            locks <- Map.remove address locks
            Ok ()

/// Sets up the mock platform providers for testing
let setupMockProviders() =
    // Create the mock providers
    let ipcProvider = new MockIpcProvider()
    let memoryProvider = new MockMemoryProvider()
    
    // Register the providers
    PlatformServices.registerProvider<IPlatformIpc>("ipc", ipcProvider :> IPlatformIpc)
    PlatformServices.registerProvider<IPlatformMemory>("memory", memoryProvider :> IPlatformMemory)
    
    // Set the initialized flag
    PlatformServices.setInitialized true
    
    // Return the providers for use in tests
    ipcProvider, memoryProvider

/// Create a simple test schema
let testSchema =
    let draft = Schema.create "TestSchema"
    
    // Add a simple primitive string type
    let withStringType = Schema.addPrimitive "StringType" PrimitiveType.String draft
    
    // Validate the schema
    match SchemaValidation.validate withStringType with
    | Ok schema -> schema
    | Error err -> failwith $"Failed to create test schema: {err}"

/// Helper to create a test message
let createTestMessage id msgType payload =
    {
        Id = id
        Type = msgType
        Payload = payload
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
    }