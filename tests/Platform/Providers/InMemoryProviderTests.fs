namespace BAREWire.Tests.Platform.Providers

open Expecto
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Core.Memory
open BAREWire.Memory.Mapping
open BAREWire.IPC
open BAREWire.Platform.Providers
open BAREWire.Platform.Common.Interfaces

module InMemoryProviderTests =
    
    [<Tests>]
    let memoryProviderTests =
        testList "InMemoryMemoryProvider Tests" [
            test "MapMemory allocates memory and returns valid handle and address" {
                let provider = InMemory.InMemoryMemoryProvider() :> IPlatformMemory
                let size = 4096<bytes>
                
                match provider.MapMemory size MappingType.PrivateMapping AccessType.ReadWrite with
                | Ok (handle, address) ->
                    Expect.notEqual handle 0n "Handle should not be zero"
                    Expect.notEqual address 0n "Address should not be zero"
                | Error e ->
                    failwith $"MapMemory failed: {e.Message}"
            }
            
            test "UnmapMemory releases previously mapped memory" {
                let provider = InMemory.InMemoryMemoryProvider() :> IPlatformMemory
                let size = 4096<bytes>
                
                // First map some memory
                match provider.MapMemory size MappingType.PrivateMapping AccessType.ReadWrite with
                | Ok (handle, address) ->
                    // Then unmap it
                    match provider.UnmapMemory handle address size with
                    | Ok () -> () // Success
                    | Error e -> failwith $"UnmapMemory failed: {e.Message}"
                | Error e ->
                    failwith $"MapMemory failed: {e.Message}"
            }
            
            test "LockMemory and UnlockMemory succeed (no-op in in-memory provider)" {
                let provider = InMemory.InMemoryMemoryProvider() :> IPlatformMemory
                let size = 4096<bytes>
                
                // Map some memory
                match provider.MapMemory size MappingType.PrivateMapping AccessType.ReadWrite with
                | Ok (_, address) ->
                    // Lock it
                    match provider.LockMemory address size with
                    | Ok () ->
                        // Unlock it
                        match provider.UnlockMemory address size with
                        | Ok () -> () // Success
                        | Error e -> failwith $"UnlockMemory failed: {e.Message}"
                    | Error e ->
                        failwith $"LockMemory failed: {e.Message}"
                | Error e ->
                    failwith $"MapMemory failed: {e.Message}"
            }
            
            test "MapFile simulates file mapping" {
                let provider = InMemory.InMemoryMemoryProvider() :> IPlatformMemory
                let size = 4096<bytes>
                
                match provider.MapFile "testfile.dat" 0L size AccessType.ReadWrite with
                | Ok (handle, address) ->
                    Expect.notEqual handle 0n "Handle should not be zero"
                    Expect.notEqual address 0n "Address should not be zero"
                    
                    // Try to flush
                    match provider.FlushMappedFile handle address size with
                    | Ok () -> () // Success
                    | Error e -> failwith $"FlushMappedFile failed: {e.Message}"
                | Error e ->
                    failwith $"MapFile failed: {e.Message}"
            }
        ]
        
    [<Tests>]
    let ipcProviderTests =
        testList "InMemoryIpcProvider Tests" [
            test "CreateNamedPipe creates a pipe and returns a handle" {
                let provider = InMemory.InMemoryIpcProvider() :> IPlatformIpc
                
                match provider.CreateNamedPipe "testpipe" NamedPipe.PipeDirection.InOut NamedPipe.PipeMode.Message 4096<bytes> with
                | Ok handle ->
                    Expect.notEqual handle 0n "Handle should not be zero"
                    
                    // Verify the pipe exists
                    Expect.isTrue (provider.ResourceExists "testpipe" "pipe") "Pipe should exist after creation"
                | Error e ->
                    failwith $"CreateNamedPipe failed: {e.Message}"
            }
            
            test "ConnectNamedPipe connects to existing pipe" {
                let provider = InMemory.InMemoryIpcProvider() :> IPlatformIpc
                
                // Create the pipe first
                match provider.CreateNamedPipe "testpipe" NamedPipe.PipeDirection.InOut NamedPipe.PipeMode.Message 4096<bytes> with
                | Ok _ ->
                    // Then connect to it
                    match provider.ConnectNamedPipe "testpipe" NamedPipe.PipeDirection.InOut with
                    | Ok clientHandle ->
                        Expect.notEqual clientHandle 0n "Client handle should not be zero"
                    | Error e ->
                        failwith $"ConnectNamedPipe failed: {e.Message}"
                | Error e ->
                    failwith $"CreateNamedPipe failed: {e.Message}"
            }
            
            test "WriteNamedPipe and ReadNamedPipe transfer data" {
                let provider = InMemory.InMemoryIpcProvider() :> IPlatformIpc
                
                // Create the pipe first
                match provider.CreateNamedPipe "testpipe" NamedPipe.PipeDirection.InOut NamedPipe.PipeMode.Message 4096<bytes> with
                | Ok serverHandle ->
                    // Connect to it
                    match provider.ConnectNamedPipe "testpipe" NamedPipe.PipeDirection.InOut with
                    | Ok clientHandle ->
                        // Mark as connected
                        match provider.WaitForNamedPipeConnection serverHandle 0 with
                        | Ok () ->
                            // Write data to the pipe
                            let data = [| 1uy; 2uy; 3uy; 4uy |]
                            match provider.WriteNamedPipe serverHandle data 0 data.Length with
                            | Ok bytesWritten ->
                                Expect.equal bytesWritten data.Length "Should write all bytes"
                                
                                // Read data from the pipe
                                let buffer = Array.zeroCreate<byte> 10
                                match provider.ReadNamedPipe clientHandle buffer 0 buffer.Length with
                                | Ok bytesRead ->
                                    Expect.equal bytesRead data.Length "Should read same number of bytes"
                                    Expect.sequenceEqual buffer.[0..bytesRead-1] data "Read data should match written data"
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
            }
            
            test "CloseNamedPipe closes a pipe" {
                let provider = InMemory.InMemoryIpcProvider() :> IPlatformIpc
                
                // Create the pipe
                match provider.CreateNamedPipe "testpipe" NamedPipe.PipeDirection.InOut NamedPipe.PipeMode.Message 4096<bytes> with
                | Ok handle ->
                    // Close the pipe
                    match provider.CloseNamedPipe handle with
                    | Ok () ->
                        // Verify the pipe doesn't exist anymore
                        Expect.isFalse (provider.ResourceExists "testpipe" "pipe") "Pipe should not exist after closing"
                    | Error e ->
                        failwith $"CloseNamedPipe failed: {e.Message}"
                | Error e ->
                    failwith $"CreateNamedPipe failed: {e.Message}"
            }
            
            test "CreateSharedMemory creates shared memory and returns handle and address" {
                let provider = InMemory.InMemoryIpcProvider() :> IPlatformIpc
                let size = 4096<bytes>
                
                match provider.CreateSharedMemory "testmem" size AccessType.ReadWrite with
                | Ok (handle, address) ->
                    Expect.notEqual handle 0n "Handle should not be zero"
                    Expect.notEqual address 0n "Address should not be zero"
                    
                    // Verify the shared memory exists
                    Expect.isTrue (provider.ResourceExists "testmem" "sharedmemory") "Shared memory should exist after creation"
                | Error e ->
                    failwith $"CreateSharedMemory failed: {e.Message}"
            }
            
            test "OpenSharedMemory opens existing shared memory" {
                let provider = InMemory.InMemoryIpcProvider() :> IPlatformIpc
                let size = 4096<bytes>
                
                // Create the shared memory first
                match provider.CreateSharedMemory "testmem" size AccessType.ReadWrite with
                | Ok _ ->
                    // Then open it
                    match provider.OpenSharedMemory "testmem" AccessType.ReadWrite with
                    | Ok (handle, address, openedSize) ->
                        Expect.notEqual handle 0n "Handle should not be zero"
                        Expect.notEqual address 0n "Address should not be zero"
                        Expect.equal openedSize size "Opened size should match created size"
                    | Error e ->
                        failwith $"OpenSharedMemory failed: {e.Message}"
                | Error e ->
                    failwith $"CreateSharedMemory failed: {e.Message}"
            }
            
            test "CloseSharedMemory closes shared memory" {
                let provider = InMemory.InMemoryIpcProvider() :> IPlatformIpc
                let size = 4096<bytes>
                
                // Create the shared memory
                match provider.CreateSharedMemory "testmem" size AccessType.ReadWrite with
                | Ok (handle, address) ->
                    // Close the shared memory
                    match provider.CloseSharedMemory handle address size with
                    | Ok () ->
                        // Verify it's gone
                        match provider.OpenSharedMemory "testmem" AccessType.ReadWrite with
                        | Ok _ ->
                            failwith "Shared memory should be closed"
                        | Error _ ->
                            () // Expected
                    | Error e ->
                        failwith $"CloseSharedMemory failed: {e.Message}"
                | Error e ->
                    failwith $"CreateSharedMemory failed: {e.Message}"
            }
            
            test "ResourceExists correctly identifies resources" {
                let provider = InMemory.InMemoryIpcProvider() :> IPlatformIpc
                let size = 4096<bytes>
                
                // Create resources
                match provider.CreateNamedPipe "testpipe" NamedPipe.PipeDirection.InOut NamedPipe.PipeMode.Message 4096<bytes> with
                | Ok _ ->
                    match provider.CreateSharedMemory "testmem" size AccessType.ReadWrite with
                    | Ok _ ->
                        // Test existence
                        Expect.isTrue (provider.ResourceExists "testpipe" "pipe") "Pipe should exist"
                        Expect.isTrue (provider.ResourceExists "testmem" "sharedmemory") "Shared memory should exist"
                        Expect.isFalse (provider.ResourceExists "nonexistent" "pipe") "Non-existent pipe should not exist"
                        Expect.isFalse (provider.ResourceExists "testpipe" "invalidtype") "Invalid resource type should not exist"
                    | Error e ->
                        failwith $"CreateSharedMemory failed: {e.Message}"
                | Error e ->
                    failwith $"CreateNamedPipe failed: {e.Message}"
            }
        ]
        
    [<Tests>]
    let networkProviderTests =
        testList "InMemoryNetworkProvider Tests" [
            test "CreateSocket creates a socket and returns a handle" {
                let provider = InMemory.InMemoryNetworkProvider() :> IPlatformNetwork
                
                match provider.CreateSocket 2 1 6 with // AF_INET, SOCK_STREAM, IPPROTO_TCP
                | Ok handle ->
                    Expect.notEqual handle 0n "Handle should not be zero"
                | Error e ->
                    failwith $"CreateSocket failed: {e.Message}"
            }
            
            test "BindSocket binds socket to address and port" {
                let provider = InMemory.InMemoryNetworkProvider() :> IPlatformNetwork
                
                // Create a socket first
                match provider.CreateSocket 2 1 6 with
                | Ok handle ->
                    // Then bind it
                    match provider.BindSocket handle "127.0.0.1" 8080 with
                    | Ok () -> () // Success
                    | Error e -> failwith $"BindSocket failed: {e.Message}"
                | Error e ->
                    failwith $"CreateSocket failed: {e.Message}"
            }
            
            test "ListenSocket puts socket in listening state" {
                let provider = InMemory.InMemoryNetworkProvider() :> IPlatformNetwork
                
                // Create and bind a socket first
                match provider.CreateSocket 2 1 6 with
                | Ok handle ->
                    match provider.BindSocket handle "127.0.0.1" 8080 with
                    | Ok () ->
                        // Then listen
                        match provider.ListenSocket handle 5 with
                        | Ok () -> () // Success
                        | Error e -> failwith $"ListenSocket failed: {e.Message}"
                    | Error e ->
                        failwith $"BindSocket failed: {e.Message}"
                | Error e ->
                    failwith $"CreateSocket failed: {e.Message}"
            }
            
            test "AcceptSocket accepts a simulated connection" {
                let provider = InMemory.InMemoryNetworkProvider() :> IPlatformNetwork
                
                // Create, bind, and listen
                match provider.CreateSocket 2 1 6 with
                | Ok handle ->
                    match provider.BindSocket handle "127.0.0.1" 8080 with
                    | Ok () ->
                        match provider.ListenSocket handle 5 with
                        | Ok () ->
                            // Accept a connection
                            match provider.AcceptSocket handle with
                            | Ok (clientHandle, clientAddr, clientPort) ->
                                Expect.notEqual clientHandle 0n "Client handle should not be zero"
                                Expect.isNonEmptyString clientAddr "Client address should not be empty"
                                Expect.isGreaterThan clientPort 0 "Client port should be positive"
                            | Error e ->
                                failwith $"AcceptSocket failed: {e.Message}"
                        | Error e ->
                            failwith $"ListenSocket failed: {e.Message}"
                    | Error e ->
                        failwith $"BindSocket failed: {e.Message}"
                | Error e ->
                    failwith $"CreateSocket failed: {e.Message}"
            }
            
            test "ConnectSocket connects to a simulated endpoint" {
                let provider = InMemory.InMemoryNetworkProvider() :> IPlatformNetwork
                
                // Create a socket
                match provider.CreateSocket 2 1 6 with
                | Ok handle ->
                    // Connect to an endpoint
                    match provider.ConnectSocket handle "127.0.0.1" 8080 with
                    | Ok () -> () // Success
                    | Error e -> failwith $"ConnectSocket failed: {e.Message}"
                | Error e ->
                    failwith $"CreateSocket failed: {e.Message}"
            }
            
            test "SendSocket and ReceiveSocket transfer data" {
                let provider = InMemory.InMemoryNetworkProvider() :> IPlatformNetwork
                
                // Create server socket
                match provider.CreateSocket 2 1 6 with
                | Ok serverHandle ->
                    match provider.BindSocket serverHandle "127.0.0.1" 8080 with
                    | Ok () ->
                        match provider.ListenSocket serverHandle 5 with
                        | Ok () ->
                            // Create client socket
                            match provider.CreateSocket 2 1 6 with
                            | Ok clientHandle ->
                                match provider.ConnectSocket clientHandle "127.0.0.1" 8080 with
                                | Ok () ->
                                    // Accept the connection
                                    match provider.AcceptSocket serverHandle with
                                    | Ok (acceptedHandle, _, _) ->
                                        // Send data from client to server
                                        let data = [| 1uy; 2uy; 3uy; 4uy |]
                                        match provider.SendSocket clientHandle data 0 data.Length 0 with
                                        | Ok bytesSent ->
                                            Expect.equal bytesSent data.Length "Should send all bytes"
                                            
                                            // Receive data on the server
                                            let buffer = Array.zeroCreate<byte> 10
                                            match provider.ReceiveSocket acceptedHandle buffer 0 buffer.Length 0 with
                                            | Ok bytesReceived ->
                                                Expect.equal bytesReceived data.Length "Should receive all bytes"
                                                Expect.sequenceEqual buffer.[0..bytesReceived-1] data "Received data should match sent data"
                                            | Error e ->
                                                failwith $"ReceiveSocket failed: {e.Message}"
                                        | Error e ->
                                            failwith $"SendSocket failed: {e.Message}"
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
            }
            
            test "CloseSocket closes a socket" {
                let provider = InMemory.InMemoryNetworkProvider() :> IPlatformNetwork
                
                // Create a socket
                match provider.CreateSocket 2 1 6 with
                | Ok handle ->
                    // Close the socket
                    match provider.CloseSocket handle with
                    | Ok () -> () // Success
                    | Error e -> failwith $"CloseSocket failed: {e.Message}"
                | Error e ->
                    failwith $"CreateSocket failed: {e.Message}"
            }
        ]
        
    [<Tests>]
    let syncProviderTests =
        testList "InMemorySyncProvider Tests" [
            test "CreateMutex creates a mutex and returns a handle" {
                let provider = InMemory.InMemorySyncProvider() :> IPlatformSync
                
                match provider.CreateMutex "testmutex" false with
                | Ok handle ->
                    Expect.notEqual handle 0n "Handle should not be zero"
                | Error e ->
                    failwith $"CreateMutex failed: {e.Message}"
            }
            
            test "OpenMutex opens an existing mutex" {
                let provider = InMemory.InMemorySyncProvider() :> IPlatformSync
                
                // Create the mutex first
                match provider.CreateMutex "testmutex" false with
                | Ok _ ->
                    // Then open it
                    match provider.OpenMutex "testmutex" with
                    | Ok handle ->
                        Expect.notEqual handle 0n "Handle should not be zero"
                    | Error e ->
                        failwith $"OpenMutex failed: {e.Message}"
                | Error e ->
                    failwith $"CreateMutex failed: {e.Message}"
            }
            
            test "AcquireMutex and ReleaseMutex control mutex ownership" {
                let provider = InMemory.InMemorySyncProvider() :> IPlatformSync
                
                // Create the mutex first
                match provider.CreateMutex "testmutex" false with
                | Ok handle ->
                    // Acquire the mutex
                    match provider.AcquireMutex handle 0 with
                    | Ok acquired ->
                        Expect.isTrue acquired "Should acquire the mutex"
                        
                        // Release the mutex
                        match provider.ReleaseMutex handle with
                        | Ok () -> () // Success
                        | Error e -> failwith $"ReleaseMutex failed: {e.Message}"
                    | Error e ->
                        failwith $"AcquireMutex failed: {e.Message}"
                | Error e ->
                    failwith $"CreateMutex failed: {e.Message}"
            }
            
            test "CloseMutex closes a mutex" {
                let provider = InMemory.InMemorySyncProvider() :> IPlatformSync
                
                // Create the mutex
                match provider.CreateMutex "testmutex" false with
                | Ok handle ->
                    // Close the mutex
                    match provider.CloseMutex handle with
                    | Ok () -> () // Success
                    | Error e -> failwith $"CloseMutex failed: {e.Message}"
                | Error e ->
                    failwith $"CreateMutex failed: {e.Message}"
            }
            
            test "CreateSemaphore creates a semaphore and returns a handle" {
                let provider = InMemory.InMemorySyncProvider() :> IPlatformSync
                
                match provider.CreateSemaphore "testsem" 1 5 with
                | Ok handle ->
                    Expect.notEqual handle 0n "Handle should not be zero"
                | Error e ->
                    failwith $"CreateSemaphore failed: {e.Message}"
            }
            
            test "OpenSemaphore opens an existing semaphore" {
                let provider = InMemory.InMemorySyncProvider() :> IPlatformSync
                
                // Create the semaphore first
                match provider.CreateSemaphore "testsem" 1 5 with
                | Ok _ ->
                    // Then open it
                    match provider.OpenSemaphore "testsem" with
                    | Ok handle ->
                        Expect.notEqual handle 0n "Handle should not be zero"
                    | Error e ->
                        failwith $"OpenSemaphore failed: {e.Message}"
                | Error e ->
                    failwith $"CreateSemaphore failed: {e.Message}"
            }
            
            test "AcquireSemaphore and ReleaseSemaphore control semaphore count" {
                let provider = InMemory.InMemorySyncProvider() :> IPlatformSync
                
                // Create the semaphore with initial count 1
                match provider.CreateSemaphore "testsem" 1 5 with
                | Ok handle ->
                    // Acquire the semaphore
                    match provider.AcquireSemaphore handle 0 with
                    | Ok acquired ->
                        Expect.isTrue acquired "Should acquire the semaphore"
                        
                        // Try to acquire again (should fail with timeout 0)
                        match provider.AcquireSemaphore handle 0 with
                        | Ok acquired2 ->
                            Expect.isFalse acquired2 "Should not acquire the semaphore again"
                            
                            // Release the semaphore
                            match provider.ReleaseSemaphore handle 1 with
                            | Ok prevCount ->
                                Expect.equal prevCount 0 "Previous count should be 0"
                                
                                // Now we should be able to acquire again
                                match provider.AcquireSemaphore handle 0 with
                                | Ok acquired3 ->
                                    Expect.isTrue acquired3 "Should acquire the semaphore after release"
                                | Error e ->
                                    failwith $"AcquireSemaphore (after release) failed: {e.Message}"
                            | Error e ->
                                failwith $"ReleaseSemaphore failed: {e.Message}"
                        | Error e ->
                            failwith $"AcquireSemaphore (second) failed: {e.Message}"
                    | Error e ->
                        failwith $"AcquireSemaphore failed: {e.Message}"
                | Error e ->
                    failwith $"CreateSemaphore failed: {e.Message}"
            }
            
            test "CloseSemaphore closes a semaphore" {
                let provider = InMemory.InMemorySyncProvider() :> IPlatformSync
                
                // Create the semaphore
                match provider.CreateSemaphore "testsem" 1 5 with
                | Ok handle ->
                    // Close the semaphore
                    match provider.CloseSemaphore handle with
                    | Ok () -> () // Success
                    | Error e -> failwith $"CloseSemaphore failed: {e.Message}"
                | Error e ->
                    failwith $"CreateSemaphore failed: {e.Message}"
            }
        ]
        
    let tests = 
        testList "InMemory Provider Tests" [
            memoryProviderTests
            ipcProviderTests
            networkProviderTests
            syncProviderTests
        ]

module Program =
    [<EntryPoint>]
    let main args =
        runTestsWithArgs defaultConfig args InMemoryProviderTests.tests