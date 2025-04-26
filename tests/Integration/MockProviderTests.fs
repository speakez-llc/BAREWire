module BAREWire.Tests.Integration.MockProviderTests

open Expecto
open System
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Core.Memory
open BAREWire.Memory.Mapping
open BAREWire.Platform
open BAREWire.Platform.Common
open BAREWire.Platform.Common.Interfaces
open BAREWire.Platform.Providers
open BAREWire.Platform.Providers.InMemory
open BAREWire.IPC

// Mock implementations for testing
type MockMemoryProvider() =
    let mutable memoryMap = Map.empty<nativeint, byte[]>
    let mutable nextHandle = 1n
    
    // Create a fake handle
    let getNextHandle() =
        let handle = nextHandle
        nextHandle <- nextHandle + 1n
        handle
    
    interface IPlatformMemory with
        member this.MapMemory size mappingType accessType =
            let handle = getNextHandle()
            let address = getNextHandle()
            
            // Allocate the memory
            let memory = Array.zeroCreate (int size)
            memoryMap <- memoryMap.Add(address, memory)
            
            Ok (handle, address)
            
        member this.UnmapMemory handle address size =
            // Remove from the map
            memoryMap <- memoryMap.Remove(address)
            Ok ()
            
        member this.MapFile filePath offset size accessType =
            let handle = getNextHandle()
            let address = getNextHandle()
            
            // Create empty memory for the "file"
            let memory = Array.zeroCreate (int size)
            memoryMap <- memoryMap.Add(address, memory)
            
            Ok (handle, address)
            
        member this.FlushMappedFile handle address size =
            Ok ()
            
        member this.LockMemory address size =
            Ok ()
            
        member this.UnlockMemory address size =
            Ok ()
            
type MockIpcProvider() =
    let mutable namedPipes = Map.empty<string, byte[]>
    let mutable sharedMemory = Map.empty<string, byte[]>
    let mutable nextHandle = 1n
    
    // Create a fake handle
    let getNextHandle() =
        let handle = nextHandle
        nextHandle <- nextHandle + 1n
        handle
    
    interface IPlatformIpc with
        member this.CreateNamedPipe name direction mode bufferSize =
            let handle = getNextHandle()
            namedPipes <- namedPipes.Add(name, Array.zeroCreate (int bufferSize))
            Ok handle
            
        member this.ConnectNamedPipe name direction =
            match namedPipes.TryFind(name) with
            | Some _ -> Ok (getNextHandle())
            | None -> Error (invalidValueError $"Named pipe not found: {name}")
            
        member this.WaitForNamedPipeConnection handle timeout =
            Ok ()
            
        member this.WriteNamedPipe handle data offset count =
            // In a real implementation, we would find the pipe by handle
            // For simplicity, we just return success
            Ok count
            
        member this.ReadNamedPipe handle buffer offset count =
            // In a real implementation, we would find the pipe by handle
            // For simplicity, we just return 0 (no data)
            Ok 0
            
        member this.CloseNamedPipe handle =
            // In a real implementation, we would find and remove the pipe
            Ok ()
            
        member this.CreateSharedMemory name size accessType =
            let handle = getNextHandle()
            let address = getNextHandle()
            sharedMemory <- sharedMemory.Add(name, Array.zeroCreate (int size))
            Ok (handle, address)
            
        member this.OpenSharedMemory name accessType =
            match sharedMemory.TryFind(name) with
            | Some mem -> 
                let handle = getNextHandle()
                let address = getNextHandle()
                Ok (handle, address, mem.Length * 1<bytes>)
            | None -> 
                Error (invalidValueError $"Shared memory not found: {name}")
            
        member this.CloseSharedMemory handle address size =
            // In a real implementation, we would find and remove the memory
            Ok ()
            
        member this.ResourceExists name resourceType =
            match resourceType.ToLowerInvariant() with
            | "pipe" -> namedPipes.ContainsKey(name)
            | "sharedmemory" | "sharedmem" -> sharedMemory.ContainsKey(name)
            | _ -> false

// A simple network packet for testing
type TestPacket = {
    Id: int
    Data: string
}

// Mock network provider
type MockNetworkProvider() =
    let mutable sockets = Map.empty<nativeint, byte[] list>
    let mutable socketConnections = Map.empty<nativeint, string * int>
    let mutable nextHandle = 1n
    
    // Create a fake handle
    let getNextHandle() =
        let handle = nextHandle
        nextHandle <- nextHandle + 1n
        handle
    
    interface IPlatformNetwork with
        member this.CreateSocket addressFamily socketType protocolType =
            let handle = getNextHandle()
            sockets <- sockets.Add(handle, [])
            Ok handle
            
        member this.BindSocket handle address port =
            Ok ()
            
        member this.ListenSocket handle backlog =
            Ok ()
            
        member this.AcceptSocket handle =
            let clientHandle = getNextHandle()
            sockets <- sockets.Add(clientHandle, [])
            Ok (clientHandle, "127.0.0.1", 12345)
            
        member this.ConnectSocket handle address port =
            socketConnections <- socketConnections.Add(handle, (address, port))
            Ok ()
            
        member this.SendSocket handle data offset count flags =
            // In a real implementation, we would find the socket by handle
            // and send to the connected peer
            // For simplicity, we just return success
            Ok count
            
        member this.ReceiveSocket handle buffer offset count flags =
            // In a real implementation, we would find the socket by handle
            // and read from its receive buffer
            // For simplicity, we just return 0 (no data)
            Ok 0
            
        member this.CloseSocket handle =
            sockets <- sockets.Remove(handle)
            socketConnections <- socketConnections.Remove(handle)
            Ok ()
            
        member this.Poll handle timeout =
            // For testing, always return true (data available)
            Ok true
            
        member this.SetSocketOption handle level optionName optionValue =
            Ok ()
            
        member this.GetSocketOption handle level optionName optionValue =
            Ok 0
            
        member this.ResolveHostName hostName =
            Ok [| "127.0.0.1" |]

// Setup function to register mock providers
let setupMockProviders() =
    Registry.initializeRegistries 10
    Registry.setCurrentPlatform Registry.PlatformType.InMemory
    Registry.registerMemoryProvider Registry.PlatformType.InMemory (new MockMemoryProvider())
    Registry.registerIpcProvider Registry.PlatformType.InMemory (new MockIpcProvider())
    Registry.registerNetworkProvider Registry.PlatformType.InMemory (new MockNetworkProvider())
    PlatformServices.isInitialized <- true

[<Tests>]
let memoryMappingTests =
    testList "Memory Mapping Tests" [
        testCase "Map and unmap memory" <| fun _ ->
            // Setup mock providers
            setupMockProviders()
            
            // Map memory
            match Mapping.mapMemory<int, region> 1024<bytes> MappingType.PrivateMapping AccessType.ReadWrite with
            | Ok (region, handle) ->
                // Check region size
                Expect.equal (Region.getSize region) 1024<bytes> "Region size should match requested size"
                
                // Unmap memory
                match Mapping.unmapMemory handle with
                | Ok () -> ()
                | Error e -> failwith $"Failed to unmap memory: {toString e}"
            | Error e ->
                failwith $"Failed to map memory: {toString e}"
                
        testCase "Map file" <| fun _ ->
            // Setup mock providers
            setupMockProviders()
            
            // Map file
            match Mapping.mapFile<int, region> "test.dat" 0L 2048<bytes> AccessType.ReadOnly with
            | Ok (region, handle) ->
                // Check region size
                Expect.equal (Region.getSize region) 2048<bytes> "Region size should match requested size"
                
                // Unmap memory
                match Mapping.unmapMemory handle with
                | Ok () -> ()
                | Error e -> failwith $"Failed to unmap memory: {toString e}"
            | Error e ->
                failwith $"Failed to map file: {toString e}"
    ]

[<Tests>]
let ipcTests =
    testList "IPC Tests" [
        testCase "Named pipe creation and connection" <| fun _ ->
            // Setup mock providers
            setupMockProviders()
            
            // Create a named pipe
            match NamedPipe.createServer<byte[]> 
                      "test-pipe" 
                      SchemaDefinition<validated>.empty() 
                      NamedPipe.defaultOptions with
            | Ok server ->
                // Connect to the pipe
                match NamedPipe.connect<byte[]> 
                          "test-pipe" 
                          SchemaDefinition<validated>.empty() 
                          NamedPipe.defaultOptions with
                | Ok client ->
                    // Close the connections
                    NamedPipe.close server |> ignore
                    NamedPipe.close client |> ignore
                | Error e ->
                    failwith $"Failed to connect to named pipe: {toString e}"
            | Error e ->
                failwith $"Failed to create named pipe: {toString e}"
                
        testCase "Shared memory creation and opening" <| fun _ ->
            // Setup mock providers
            setupMockProviders()
            
            // Create shared memory
            match SharedMemory.create<int> 
                      "test-memory" 
                      1024<bytes> 
                      SchemaDefinition<validated>.empty() with
            | Ok sharedRegion ->
                // Open the existing shared memory
                match SharedMemory.open'<int> 
                          "test-memory" 
                          SchemaDefinition<validated>.empty() with
                | Ok openedRegion ->
                    // Close the regions
                    SharedMemory.close sharedRegion |> ignore
                    SharedMemory.close openedRegion |> ignore
                | Error e ->
                    failwith $"Failed to open shared memory: {toString e}"
            | Error e ->
                failwith $"Failed to create shared memory: {toString e}"
    ]

[<Tests>]
let inMemoryProviderTests =
    testList "InMemory Provider Tests" [
        testCase "InMemoryMemoryProvider" <| fun _ ->
            let provider = new InMemory.InMemoryMemoryProvider()
            
            // Test MapMemory
            match provider.MapMemory 1024<bytes> MappingType.PrivateMapping AccessType.ReadWrite with
            | Ok (handle, address) ->
                // Test UnmapMemory
                match provider.UnmapMemory handle address 1024<bytes> with
                | Ok () -> ()
                | Error e -> failwith $"Failed to unmap memory: {toString e}"
            | Error e ->
                failwith $"Failed to map memory: {toString e}"
                
        testCase "InMemoryIpcProvider" <| fun _ ->
            let provider = new InMemory.InMemoryIpcProvider()
            
            // Test CreateNamedPipe
            match provider.CreateNamedPipe 
                      "test-pipe" 
                      NamedPipe.PipeDirection.InOut 
                      NamedPipe.PipeMode.Message 
                      1024<bytes> with
            | Ok handle ->
                // Test ConnectNamedPipe
                match provider.ConnectNamedPipe "test-pipe" NamedPipe.PipeDirection.InOut with
                | Ok clientHandle ->
                    // Test ResourceExists
                    Expect.isTrue (provider.ResourceExists "test-pipe" "pipe") "Named pipe should exist"
                    
                    // Test CloseNamedPipe
                    match provider.CloseNamedPipe handle with
                    | Ok () -> ()
                    | Error e -> failwith $"Failed to close server pipe: {toString e}"
                    
                    match provider.CloseNamedPipe clientHandle with
                    | Ok () -> ()
                    | Error e -> failwith $"Failed to close client pipe: {toString e}"
                | Error e ->
                    failwith $"Failed to connect to named pipe: {toString e}"
            | Error e ->
                failwith $"Failed to create named pipe: {toString e}"
                
            // Test CreateSharedMemory
            match provider.CreateSharedMemory "test-memory" 1024<bytes> AccessType.ReadWrite with
            | Ok (handle, address) ->
                // Test OpenSharedMemory
                match provider.OpenSharedMemory "test-memory" AccessType.ReadWrite with
                | Ok (clientHandle, clientAddress, size) ->
                    // Test ResourceExists
                    Expect.isTrue (provider.ResourceExists "test-memory" "sharedmemory") "Shared memory should exist"
                    
                    // Test CloseSharedMemory
                    match provider.CloseSharedMemory handle address 1024<bytes> with
                    | Ok () -> ()
                    | Error e -> failwith $"Failed to close server shared memory: {toString e}"
                    
                    match provider.CloseSharedMemory clientHandle clientAddress size with
                    | Ok () -> ()
                    | Error e -> failwith $"Failed to close client shared memory: {toString e}"
                | Error e ->
                    failwith $"Failed to open shared memory: {toString e}"
            | Error e ->
                failwith $"Failed to create shared memory: {toString e}"
    ]

[<EntryPoint>]
let main args =
    runTestsWithArgs defaultConfig args inMemoryProviderTests