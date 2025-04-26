namespace BAREWire.Tests.Platform.Providers

open Expecto
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Core.Memory
open BAREWire.Memory.Mapping
open BAREWire.IPC
open BAREWire.Platform
open BAREWire.Platform.Common
open BAREWire.Platform.Common.Interfaces
open BAREWire.Platform.Common.Registry
open BAREWire.Platform.Common.Resource
open BAREWire.Platform.Providers
open System

/// <summary>
/// Tests that verify integration between platform services and various providers
/// </summary>
module ProviderIntegrationTests =
    
    /// <summary>
    /// Helper to reset platform services between tests
    /// </summary>
    let resetPlatformServices() =
        // Use reflection to reset the isInitialized flag
        let fieldInfo = 
            typeof<PlatformServices>
                .GetField("isInitialized", System.Reflection.BindingFlags.Static ||| 
                                           System.Reflection.BindingFlags.NonPublic)
        
        if not (isNull fieldInfo) then
            fieldInfo.SetValue(null, false)
    
    /// <summary>
    /// Setup for all tests: initialize with InMemory providers
    /// </summary>
    let setup() =
        resetPlatformServices()
        PlatformServices.initializeWithPlatform PlatformType.InMemory 10 |> ignore
        
        // Register the InMemory providers
        Registry.registerMemoryProvider PlatformType.InMemory (InMemory.InMemoryMemoryProvider())
        Registry.registerIpcProvider PlatformType.InMemory (InMemory.InMemoryIpcProvider())
        Registry.registerNetworkProvider PlatformType.InMemory (InMemory.InMemoryNetworkProvider())
        Registry.registerSyncProvider PlatformType.InMemory (InMemory.InMemorySyncProvider())
    
    [<Tests>]
    let memoryMappingTests =
        testList "Memory Mapping Integration Tests" [
            test "Map and unmap memory through platform services" {
                let size = 4096<bytes>
                
                // Get the memory provider through platform services
                match PlatformServices.getMemoryProvider() with
                | Ok provider ->
                    // Map some memory
                    match provider.MapMemory size MappingType.PrivateMapping AccessType.ReadWrite with
                    | Ok (handle, address) ->
                        Expect.notEqual handle 0n "Handle should not be zero"
                        Expect.notEqual address 0n "Address should not be zero"
                        
                        // Unmap the memory
                        match provider.UnmapMemory handle address size with
                        | Ok () -> () // Success
                        | Error e -> failwith $"UnmapMemory failed: {e.Message}"
                    | Error e ->
                        failwith $"MapMemory failed: {e.Message}"
                | Error e ->
                    failwith $"Failed to get memory provider: {e.Message}"
            }
            
            test "Memory mapping with resource management" {
                let size = 4096<bytes>
                
                // Get the memory provider through platform services
                match PlatformServices.getMemoryProvider() with
                | Ok provider ->
                    // Track disposals
                    let mutable wasDisposed = false
                    
                    // Create and use a memory resource
                    try
                        match provider.MapMemory size MappingType.PrivateMapping AccessType.ReadWrite with
                        | Ok (handle, address) ->
                            // Create a resource wrapper
                            let resource = create (handle, address) (fun () -> 
                                wasDisposed <- true
                                provider.UnmapMemory handle address size |> ignore
                            )
                            
                            // Use the resource
                            let result = use' resource (fun (h, a) -> 
                                Expect.notEqual h 0n "Handle should not be zero"
                                Expect.notEqual a 0n "Address should not be zero"
                                h.ToString()
                            )
                            
                            // After use', the resource should be disposed
                            Expect.isTrue wasDisposed "Resource should be disposed after use"
                        | Error e ->
                            failwith $"MapMemory failed: {e.Message}"
                    with ex ->
                        failwith $"Unexpected exception: {ex.Message}"
                | Error e ->
                    failwith $"Failed to get memory provider: {e.Message}"
            }
        ]
        
    [<Tests>]
    let namedPipeTests =
        testList "Named Pipe Integration Tests" [
            test "Create and connect to named pipe" {
                // Get the IPC provider through platform services
                match PlatformServices.getIpcProvider() with
                | Ok provider ->
                    // Create a named pipe
                    match provider.CreateNamedPipe "testpipe" NamedPipe.PipeDirection.InOut NamedPipe.PipeMode.Message 4096<bytes> with
                    | Ok serverHandle ->
                        Expect.notEqual serverHandle 0n "Server handle should not be zero"
                        
                        // Connect to the named pipe
                        match provider.ConnectNamedPipe "testpipe" NamedPipe.PipeDirection.InOut with
                        | Ok clientHandle ->
                            Expect.notEqual clientHandle 0n "Client handle should not be zero"
                            
                            // Verify the pipe exists
                            Expect.isTrue (provider.ResourceExists "testpipe" "pipe") "Pipe should exist"
                            
                            // Close the pipes
                            match provider.CloseNamedPipe serverHandle with
                            | Ok () ->
                                // Close client pipe
                                match provider.CloseNamedPipe clientHandle with
                                | Ok () -> () // Success
                                | Error e -> failwith $"CloseNamedPipe (client) failed: {e.Message}"
                            | Error e ->
                                failwith $"CloseNamedPipe (server) failed: {e.Message}"
                        | Error e ->
                            failwith $"ConnectNamedPipe failed: {e.Message}"
                    | Error e ->
                        failwith $"CreateNamedPipe failed: {e.Message}"
                | Error e ->
                    failwith $"Failed to get IPC provider: {e.Message}"
            }
            
            test "Write and read data through named pipe" {
                // Get the IPC provider through platform services
                match PlatformServices.getIpcProvider() with
                | Ok provider ->
                    // Create and connect a named pipe
                    match provider.CreateNamedPipe "testpipe" NamedPipe.PipeDirection.InOut NamedPipe.PipeMode.Message 4096<bytes> with
                    | Ok serverHandle ->
                        match provider.ConnectNamedPipe "testpipe" NamedPipe.PipeDirection.InOut with
                        | Ok clientHandle ->
                            // Mark the server as connected
                            match provider.WaitForNamedPipeConnection serverHandle 0 with
                            | Ok () ->
                                // Write data to the pipe
                                let testData = [| 1uy; 2uy; 3uy; 4uy |]
                                match provider.WriteNamedPipe serverHandle testData 0 testData.Length with
                                | Ok bytesWritten ->
                                    Expect.equal bytesWritten testData.Length "Should write all bytes"
                                    
                                    // Read data from the pipe
                                    let buffer = Array.zeroCreate<byte> 10
                                    match provider.ReadNamedPipe clientHandle buffer 0 buffer.Length with
                                    | Ok bytesRead ->
                                        Expect.equal bytesRead testData.Length "Should read same number of bytes"
                                        Expect.sequenceEqual buffer.[0..bytesRead-1] testData "Read data should match written data"
                                        
                                        // Close the pipes
                                        provider.CloseNamedPipe serverHandle |> ignore
                                        provider.CloseNamedPipe clientHandle |> ignore
                                    | Error e ->
                                        failwith $"ReadNamedPipe failed: {e.Message}"
                                | Error e ->
                                    failwith $"WriteNamedPipe failed: {e.Message}"
                            | Error e ->
                                failwith $"WaitForNamedPipeConnection failed: {e.Message}"
                        | Error e ->
                            failwith $"ConnectNamedPipe failed: {e.Message}"
                    | Error e ->
                        failwith $"CreateNamedPipe failed: {e.Message}"
                | Error e ->
                    failwith $"Failed to get IPC provider: {e.Message}"
            }
            
            test "Named pipe with resource management" {
                // Get the IPC provider through platform services
                match PlatformServices.getIpcProvider() with
                | Ok provider ->
                    // Track disposals
                    let mutable serverDisposed = false
                    let mutable clientDisposed = false
                    
                    // Create and use a pipe with resource management
                    try
                        match provider.CreateNamedPipe "testpipe" NamedPipe.PipeDirection.InOut NamedPipe.PipeMode.Message 4096<bytes> with
                        | Ok serverHandle ->
                            // Create a resource wrapper for server
                            let serverResource = create serverHandle (fun () -> 
                                serverDisposed <- true
                                provider.CloseNamedPipe serverHandle |> ignore
                            )
                            
                            // Connect to the pipe
                            match provider.ConnectNamedPipe "testpipe" NamedPipe.PipeDirection.InOut with
                            | Ok clientHandle ->
                                // Create a resource wrapper for client
                                let clientResource = create clientHandle (fun () -> 
                                    clientDisposed <- true
                                    provider.CloseNamedPipe clientHandle |> ignore
                                )
                                
                                // Use both resources
                                let result = use' serverResource (fun sh -> 
                                    use' clientResource (fun ch -> 
                                        // Simulate pipe usage
                                        provider.WaitForNamedPipeConnection sh 0 |> ignore
                                        
                                        let testData = [| 1uy; 2uy; 3uy; 4uy |]
                                        provider.WriteNamedPipe sh testData 0 testData.Length |> ignore
                                        
                                        let buffer = Array.zeroCreate<byte> 10
                                        provider.ReadNamedPipe ch buffer 0 buffer.Length |> ignore
                                        
                                        true
                                    )
                                )
                                
                                // After use', both resources should be disposed
                                Expect.isTrue serverDisposed "Server resource should be disposed"
                                Expect.isTrue clientDisposed "Client resource should be disposed"
                            | Error e ->
                                failwith $"ConnectNamedPipe failed: {e.Message}"
                        | Error e ->
                            failwith $"CreateNamedPipe failed: {e.Message}"
                    with ex ->
                        failwith $"Unexpected exception: {ex.Message}"
                | Error e ->
                    failwith $"Failed to get IPC provider: {e.Message}"
            }
        ]
        
    [<Tests>]
    let sharedMemoryTests =
        testList "Shared Memory Integration Tests" [
            test "Create and open shared memory" {
                // Get the IPC provider through platform services
                match PlatformServices.getIpcProvider() with
                | Ok provider ->
                    let size = 4096<bytes>
                    
                    // Create shared memory
                    match provider.CreateSharedMemory "testmem" size AccessType.ReadWrite with
                    | Ok (handle, address) ->
                        Expect.notEqual handle 0n "Handle should not be zero"
                        Expect.notEqual address 0n "Address should not be zero"
                        
                        // Verify it exists
                        Expect.isTrue (provider.ResourceExists "testmem" "sharedmemory") "Shared memory should exist"
                        
                        // Open the shared memory
                        match provider.OpenSharedMemory "testmem" AccessType.ReadWrite with
                        | Ok (handle2, address2, size2) ->
                            Expect.notEqual handle2 0n "Second handle should not be zero"
                            Expect.notEqual address2 0n "Second address should not be zero"
                            Expect.equal size2 size "Opened size should match created size"
                            
                            // Close both handles
                            provider.CloseSharedMemory handle address size |> ignore
                            provider.CloseSharedMemory handle2 address2 size |> ignore
                        | Error e ->
                            failwith $"OpenSharedMemory failed: {e.Message}"
                    | Error e ->
                        failwith $"CreateSharedMemory failed: {e.Message}"
                | Error e ->
                    failwith $"Failed to get IPC provider: {e.Message}"
            }
            
            test "Shared memory writing and reading" {
                // Get the IPC provider through platform services
                match PlatformServices.getIpcProvider() with
                | Ok provider ->
                    let size = 4096<bytes>
                    
                    // Create shared memory
                    match provider.CreateSharedMemory "testmem" size AccessType.ReadWrite with
                    | Ok (handle1, address1) ->
                        // Open a second handle to the same memory
                        match provider.OpenSharedMemory "testmem" AccessType.ReadWrite with
                        | Ok (handle2, address2, _) ->
                            try
                                // Write data through the first handle
                                let testPattern = 0xDEADBEEFu
                                
                                // Cast address to uint32 pointer and write a value
                                let ptr1 = NativePtr.ofNativeInt<uint32> address1
                                NativePtr.write ptr1 testPattern
                                
                                // Read through the second handle
                                let ptr2 = NativePtr.ofNativeInt<uint32> address2
                                let readValue = NativePtr.read ptr2
                                
                                // Verify the data is shared
                                Expect.equal readValue testPattern "Value read through second handle should match value written through first handle"
                            finally
                                // Clean up
                                provider.CloseSharedMemory handle1 address1 size |> ignore
                                provider.CloseSharedMemory handle2 address2 size |> ignore
                        | Error e ->
                            failwith $"OpenSharedMemory failed: {e.Message}"
                    | Error e ->
                        failwith $"CreateSharedMemory failed: {e.Message}"
                | Error e ->
                    failwith $"Failed to get IPC provider: {e.Message}"
            }
        ]
        
    [<Tests>]
    let networkTests =
        testList "Network Integration Tests" [
            test "Create and connect sockets" {
                // Get the network provider through platform services
                match PlatformServices.getNetworkProvider() with
                | Ok provider ->
                    // Create server socket
                    match provider.CreateSocket 2 1 6 with // AF_INET, SOCK_STREAM, IPPROTO_TCP
                    | Ok serverHandle ->
                        // Bind and listen
                        match provider.BindSocket serverHandle "127.0.0.1" 8080 with
                        | Ok () ->
                            match provider.ListenSocket serverHandle 5 with
                            | Ok () ->
                                // Create client socket
                                match provider.CreateSocket 2 1 6 with
                                | Ok clientHandle ->
                                    // Connect to server
                                    match provider.ConnectSocket clientHandle "127.0.0.1" 8080 with
                                    | Ok () ->
                                        // Accept the connection
                                        match provider.AcceptSocket serverHandle with
                                        | Ok (acceptedHandle, clientAddr, clientPort) ->
                                            Expect.notEqual acceptedHandle 0n "Accepted handle should not be zero"
                                            Expect.isNonEmptyString clientAddr "Client address should not be empty"
                                            Expect.isGreaterThan clientPort 0 "Client port should be positive"
                                            
                                            // Clean up
                                            provider.CloseSocket acceptedHandle |> ignore
                                            provider.CloseSocket clientHandle |> ignore
                                            provider.CloseSocket serverHandle |> ignore
                                        | Error e ->
                                            failwith $"AcceptSocket failed: {e.Message}"
                                    | Error e ->
                                        failwith $"ConnectSocket failed: {e.Message}"
                                | Error e ->
                                    failwith $"CreateSocket (client) failed: {e.Message}"
                            | Error e ->
                                failwith $"ListenSocket failed: {e.Message}"
                        | Error e ->
                            failwith $"BindSocket failed: {e.Message}"
                    | Error e ->
                        failwith $"CreateSocket (server) failed: {e.Message}"
                | Error e ->
                    failwith $"Failed to get network provider: {e.Message}"
            }
            
            test "Socket data transfer" {
                // Get the network provider through platform services
                match PlatformServices.getNetworkProvider() with
                | Ok provider ->
                    // Set up server and client sockets
                    use' (setupSocketPair provider) (fun (serverHandle, clientHandle, acceptedHandle) ->
                        // Send data from client to server
                        let testData = [| 1uy; 2uy; 3uy; 4uy |]
                        match provider.SendSocket clientHandle testData 0 testData.Length 0 with
                        | Ok bytesSent ->
                            Expect.equal bytesSent testData.Length "Should send all bytes"
                            
                            // Receive data on server
                            let buffer = Array.zeroCreate<byte> 10
                            match provider.ReceiveSocket acceptedHandle buffer 0 buffer.Length 0 with
                            | Ok bytesReceived ->
                                Expect.equal bytesReceived testData.Length "Should receive all bytes"
                                Expect.sequenceEqual buffer.[0..bytesReceived-1] testData "Received data should match sent data"
                            | Error e ->
                                failwith $"ReceiveSocket failed: {e.Message}"
                        | Error e ->
                            failwith $"SendSocket failed: {e.Message}"
                        
                        true
                    )
                | Error e ->
                    failwith $"Failed to get network provider: {e.Message}"
            }
            
            test "Socket option setting and getting" {
                // Get the network provider through platform services
                match PlatformServices.getNetworkProvider() with
                | Ok provider ->
                    // Create a socket
                    match provider.CreateSocket 2 1 6 with // AF_INET, SOCK_STREAM, IPPROTO_TCP
                    | Ok handle ->
                        try
                            // Set a socket option
                            let optionValue = [| 1uy; 0uy; 0uy; 0uy |] // Boolean true in little endian
                            match provider.SetSocketOption handle 0xFFFF 0x0004 optionValue with // SOL_SOCKET, SO_REUSEADDR
                            | Ok () ->
                                // Get the socket option
                                let buffer = Array.zeroCreate<byte> 4
                                match provider.GetSocketOption handle 0xFFFF 0x0004 buffer with
                                | Ok size ->
                                    Expect.isGreaterThan size 0 "Option size should be positive"
                                    Expect.equal buffer.[0] 1uy "Option value should match what was set"
                                | Error e ->
                                    failwith $"GetSocketOption failed: {e.Message}"
                            | Error e ->
                                failwith $"SetSocketOption failed: {e.Message}"
                        finally
                            // Clean up
                            provider.CloseSocket handle |> ignore
                    | Error e ->
                        failwith $"CreateSocket failed: {e.Message}"
                | Error e ->
                    failwith $"Failed to get network provider: {e.Message}"
            }
        ]
        and setupSocketPair provider =
            // Create server socket
            match provider.CreateSocket 2 1 6 with // AF_INET, SOCK_STREAM, IPPROTO_TCP
            | Ok serverHandle ->
                // Create resource for server socket
                let serverResource = create serverHandle (fun () -> provider.CloseSocket serverHandle |> ignore)
                
                // Bind and listen
                provider.BindSocket serverHandle "127.0.0.1" 8080 |> ignore
                provider.ListenSocket serverHandle 5 |> ignore
                
                // Create client socket
                match provider.CreateSocket 2 1 6 with
                | Ok clientHandle ->
                    // Create resource for client socket
                    let clientResource = create clientHandle (fun () -> provider.CloseSocket clientHandle |> ignore)
                    
                    // Connect to server
                    provider.ConnectSocket clientHandle "127.0.0.1" 8080 |> ignore
                    
                    // Accept the connection
                    match provider.AcceptSocket serverHandle with
                    | Ok (acceptedHandle, _, _) ->
                        // Create resource for accepted socket
                        let acceptedResource = create acceptedHandle (fun () -> provider.CloseSocket acceptedHandle |> ignore)
                        
                        // Combine all resources
                        combine (fun _ _ _ -> (serverHandle, clientHandle, acceptedHandle)) 
                                serverResource 
                                (combine (fun c a -> (c, a)) clientResource acceptedResource)
                    | Error e ->
                        // Clean up and propagate error
                        clientResource.Dispose()
                        serverResource.Dispose()
                        failwith $"AcceptSocket failed: {e.Message}"
                | Error e ->
                    // Clean up and propagate error
                    serverResource.Dispose()
                    failwith $"CreateSocket (client) failed: {e.Message}"
            | Error e ->
                failwith $"CreateSocket (server) failed: {e.Message}"
        
    [<Tests>]
    let syncTests =
        testList "Synchronization Integration Tests" [
            test "Mutex creation and operations" {
                // Get the sync provider through platform services
                match PlatformServices.getSyncProvider() with
                | Ok provider ->
                    // Create a mutex
                    match provider.CreateMutex "testmutex" false with
                    | Ok handle ->
                        Expect.notEqual handle 0n "Handle should not be zero"
                        
                        // Acquire the mutex
                        match provider.AcquireMutex handle 0 with
                        | Ok acquired ->
                            Expect.isTrue acquired "Should acquire the mutex"
                            
                            // Release the mutex
                            match provider.ReleaseMutex handle with
                            | Ok () ->
                                // Close the mutex
                                match provider.CloseMutex handle with
                                | Ok () -> () // Success
                                | Error e -> failwith $"CloseMutex failed: {e.Message}"
                            | Error e ->
                                failwith $"ReleaseMutex failed: {e.Message}"
                        | Error e ->
                            failwith $"AcquireMutex failed: {e.Message}"
                    | Error e ->
                        failwith $"CreateMutex failed: {e.Message}"
                | Error e ->
                    failwith $"Failed to get sync provider: {e.Message}"
            }
            
            test "Semaphore creation and operations" {
                // Get the sync provider through platform services
                match PlatformServices.getSyncProvider() with
                | Ok provider ->
                    // Create a semaphore with initial count 2, max count 5
                    match provider.CreateSemaphore "testsem" 2 5 with
                    | Ok handle ->
                        Expect.notEqual handle 0n "Handle should not be zero"
                        
                        // Acquire twice (should succeed)
                        match provider.AcquireSemaphore handle 0 with
                        | Ok acquired1 ->
                            Expect.isTrue acquired1 "Should acquire the semaphore (1)"
                            
                            match provider.AcquireSemaphore handle 0 with
                            | Ok acquired2 ->
                                Expect.isTrue acquired2 "Should acquire the semaphore (2)"
                                
                                // Release with count 1
                                match provider.ReleaseSemaphore handle 1 with
                                | Ok prevCount ->
                                    Expect.equal prevCount 0 "Previous count should be 0"
                                    
                                    // Close the semaphore
                                    match provider.CloseSemaphore handle with
                                    | Ok () -> () // Success
                                    | Error e -> failwith $"CloseSemaphore failed: {e.Message}"
                                | Error e ->
                                    failwith $"ReleaseSemaphore failed: {e.Message}"
                            | Error e ->
                                failwith $"AcquireSemaphore (second) failed: {e.Message}"
                        | Error e ->
                            failwith $"AcquireSemaphore failed: {e.Message}"
                    | Error e ->
                        failwith $"CreateSemaphore failed: {e.Message}"
                | Error e ->
                    failwith $"Failed to get sync provider: {e.Message}"
            }
        ]
        
    let tests = 
        testSequenced <| testList "Provider Integration Tests" [
            testFixture setup memoryMappingTests
            testFixture setup namedPipeTests
            testFixture setup sharedMemoryTests
            testFixture setup networkTests
            testFixture setup syncTests
        ]

module Program =
    [<EntryPoint>]
    let main args =
        runTestsWithArgs defaultConfig args ProviderIntegrationTests.tests