namespace BAREWire.Tests.Platform.Common

open Expecto
open FsCheck
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Core.Memory
open BAREWire.Platform.Common.Interfaces
open BAREWire.Memory.Mapping
open BAREWire.IPC
open System

/// Mock implementation of IPlatformMemory for testing interface contracts
type MockMemoryProvider() =
    let mutable mappings = Map.empty<nativeint, nativeint * int<bytes>>
    let mutable nextAddress = 1000n
    
    interface IPlatformMemory with
        member _.MapMemory size mappingType accessType =
            let address = nextAddress
            nextAddress <- nextAddress + nativeint (int size)
            mappings <- mappings.Add(address, (address, size))
            Ok (address, address)
            
        member _.UnmapMemory handle address size =
            mappings <- mappings.Remove handle
            Ok ()
            
        member _.MapFile filePath offset size accessType =
            let address = nextAddress
            nextAddress <- nextAddress + nativeint (int size)
            Ok (address, address)
            
        member _.FlushMappedFile handle address size = Ok ()
        
        member _.LockMemory address size = Ok ()
        
        member _.UnlockMemory address size = Ok ()

/// Mock implementation of IPlatformIpc for testing interface contracts
type MockIpcProvider() =
    let mutable pipes = Map.empty<string, nativeint>
    let mutable sharedMemory = Map.empty<string, nativeint * nativeint * int<bytes>>
    let mutable nextHandle = 1000n
    
    interface IPlatformIpc with
        member _.CreateNamedPipe name direction mode bufferSize =
            let handle = nextHandle
            nextHandle <- nextHandle + 1n
            pipes <- pipes.Add(name, handle)
            Ok handle
            
        member _.ConnectNamedPipe name direction =
            match pipes.TryFind name with
            | Some handle -> Ok handle
            | None -> Error (invalidStateError $"Pipe {name} not found")
            
        member _.WaitForNamedPipeConnection handle timeout = Ok ()
        
        member _.WriteNamedPipe handle data offset count = Ok count
        
        member _.ReadNamedPipe handle buffer offset count = Ok count
        
        member _.CloseNamedPipe handle =
            let pipeName = pipes |> Map.findKey (fun _ h -> h = handle)
            pipes <- pipes.Remove pipeName
            Ok ()
            
        member _.CreateSharedMemory name size accessType =
            let handle = nextHandle
            nextHandle <- nextHandle + 1n
            let address = nextHandle
            nextHandle <- nextHandle + 1n
            sharedMemory <- sharedMemory.Add(name, (handle, address, size))
            Ok (handle, address)
            
        member _.OpenSharedMemory name accessType =
            match sharedMemory.TryFind name with
            | Some (handle, address, size) -> Ok (handle, address, size)
            | None -> Error (invalidStateError $"Shared memory {name} not found")
            
        member _.CloseSharedMemory handle address size =
            let memName = 
                sharedMemory 
                |> Map.findKey (fun _ (h, _, _) -> h = handle)
            sharedMemory <- sharedMemory.Remove memName
            Ok ()
            
        member _.ResourceExists name resourceType =
            match resourceType.ToLowerInvariant() with
            | "pipe" -> pipes.ContainsKey name
            | "sharedmemory" | "sharedmem" -> sharedMemory.ContainsKey name
            | _ -> false

module InterfacesTests =
    [<Tests>]
    let memoryProviderTests =
        testList "IPlatformMemory Interface Tests" [
            test "MapMemory should return valid handle and address" {
                let provider = MockMemoryProvider() :> IPlatformMemory
                let size = 4096<bytes>
                
                match provider.MapMemory size MappingType.PrivateMapping AccessType.ReadWrite with
                | Ok (handle, address) ->
                    Expect.notEqual handle 0n "Handle should not be zero"
                    Expect.notEqual address 0n "Address should not be zero"
                | Error e ->
                    failwith $"MapMemory failed: {e.Message}"
            }
            
            test "UnmapMemory should succeed for mapped memory" {
                let provider = MockMemoryProvider() :> IPlatformMemory
                let size = 4096<bytes>
                
                match provider.MapMemory size MappingType.PrivateMapping AccessType.ReadWrite with
                | Ok (handle, address) ->
                    match provider.UnmapMemory handle address size with
                    | Ok () -> ()
                    | Error e -> failwith $"UnmapMemory failed: {e.Message}"
                | Error e ->
                    failwith $"MapMemory failed: {e.Message}"
            }
            
            test "MapFile should return valid handle and address" {
                let provider = MockMemoryProvider() :> IPlatformMemory
                let size = 4096<bytes>
                
                match provider.MapFile "testfile.txt" 0L size AccessType.ReadOnly with
                | Ok (handle, address) ->
                    Expect.notEqual handle 0n "Handle should not be zero"
                    Expect.notEqual address 0n "Address should not be zero"
                | Error e ->
                    failwith $"MapFile failed: {e.Message}"
            }
        ]
        
    [<Tests>]
    let ipcProviderTests =
        testList "IPlatformIpc Interface Tests" [
            test "CreateNamedPipe should return valid handle" {
                let provider = MockIpcProvider() :> IPlatformIpc
                
                match provider.CreateNamedPipe "testpipe" NamedPipe.PipeDirection.InOut NamedPipe.PipeMode.Message 4096<bytes> with
                | Ok handle ->
                    Expect.notEqual handle 0n "Handle should not be zero"
                | Error e ->
                    failwith $"CreateNamedPipe failed: {e.Message}"
            }
            
            test "ConnectNamedPipe should succeed for existing pipe" {
                let provider = MockIpcProvider() :> IPlatformIpc
                
                // Create pipe first
                match provider.CreateNamedPipe "testpipe" NamedPipe.PipeDirection.InOut NamedPipe.PipeMode.Message 4096<bytes> with
                | Ok _ ->
                    // Then connect
                    match provider.ConnectNamedPipe "testpipe" NamedPipe.PipeDirection.InOut with
                    | Ok handle ->
                        Expect.notEqual handle 0n "Handle should not be zero"
                    | Error e ->
                        failwith $"ConnectNamedPipe failed: {e.Message}"
                | Error e ->
                    failwith $"CreateNamedPipe failed: {e.Message}"
            }
            
            test "CreateSharedMemory should return valid handle and address" {
                let provider = MockIpcProvider() :> IPlatformIpc
                let size = 4096<bytes>
                
                match provider.CreateSharedMemory "testmem" size AccessType.ReadWrite with
                | Ok (handle, address) ->
                    Expect.notEqual handle 0n "Handle should not be zero"
                    Expect.notEqual address 0n "Address should not be zero"
                | Error e ->
                    failwith $"CreateSharedMemory failed: {e.Message}"
            }
            
            test "OpenSharedMemory should succeed for existing memory" {
                let provider = MockIpcProvider() :> IPlatformIpc
                let size = 4096<bytes>
                
                // Create shared memory first
                match provider.CreateSharedMemory "testmem" size AccessType.ReadWrite with
                | Ok _ ->
                    // Then open it
                    match provider.OpenSharedMemory "testmem" AccessType.ReadWrite with
                    | Ok (handle, address, retSize) ->
                        Expect.notEqual handle 0n "Handle should not be zero"
                        Expect.notEqual address 0n "Address should not be zero"
                        Expect.equal retSize size "Size should match original"
                    | Error e ->
                        failwith $"OpenSharedMemory failed: {e.Message}"
                | Error e ->
                    failwith $"CreateSharedMemory failed: {e.Message}"
            }
            
            test "ResourceExists should return true for existing resources" {
                let provider = MockIpcProvider() :> IPlatformIpc
                let size = 4096<bytes>
                
                // Create pipe and shared memory
                match provider.CreateNamedPipe "testpipe" NamedPipe.PipeDirection.InOut NamedPipe.PipeMode.Message 4096<bytes> with
                | Ok _ ->
                    match provider.CreateSharedMemory "testmem" size AccessType.ReadWrite with
                    | Ok _ ->
                        Expect.isTrue (provider.ResourceExists "testpipe" "pipe") "Pipe should exist"
                        Expect.isTrue (provider.ResourceExists "testmem" "sharedmem") "Shared memory should exist"
                        Expect.isFalse (provider.ResourceExists "nonexistent" "pipe") "Non-existent pipe should not exist"
                    | Error e ->
                        failwith $"CreateSharedMemory failed: {e.Message}"
                | Error e ->
                    failwith $"CreateNamedPipe failed: {e.Message}"
            }
        ]
        
    let tests = 
        testList "Interface Tests" [
            memoryProviderTests
            ipcProviderTests
        ]

module Program =
    [<EntryPoint>]
    let main args =
        runTestsWithArgs defaultConfig args InterfacesTests.tests