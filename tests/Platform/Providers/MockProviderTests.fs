namespace BAREWire.Tests.Platform.Providers

open Expecto
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Core.Memory
open BAREWire.Memory.Mapping
open BAREWire.IPC
open BAREWire.Platform.Common.Interfaces

/// <summary>
/// Mock providers with configurable behavior for testing
/// </summary>
module MockProviders =
    
    /// <summary>
    /// Mock memory provider with configurable behavior
    /// </summary>
    type MockMemoryProvider() =
        let mutable mapMemoryImpl = fun (size: int<bytes>) (mappingType: MappingType) (accessType: AccessType) -> 
            Ok (1n, 1n)
            
        let mutable unmapMemoryImpl = fun (handle: nativeint) (address: nativeint) (size: int<bytes>) -> 
            Ok ()
            
        let mutable mapFileImpl = fun (filePath: string) (offset: int64) (size: int<bytes>) (accessType: AccessType) -> 
            Ok (1n, 1n)
            
        let mutable flushMappedFileImpl = fun (handle: nativeint) (address: nativeint) (size: int<bytes>) -> 
            Ok ()
            
        let mutable lockMemoryImpl = fun (address: nativeint) (size: int<bytes>) -> 
            Ok ()
            
        let mutable unlockMemoryImpl = fun (address: nativeint) (size: int<bytes>) -> 
            Ok ()
        
        /// <summary>Configure the MapMemory implementation</summary>
        member this.WithMapMemory(impl) =
            mapMemoryImpl <- impl
            this
            
        /// <summary>Configure the UnmapMemory implementation</summary>
        member this.WithUnmapMemory(impl) =
            unmapMemoryImpl <- impl
            this
            
        /// <summary>Configure the MapFile implementation</summary>
        member this.WithMapFile(impl) =
            mapFileImpl <- impl
            this
            
        /// <summary>Configure the FlushMappedFile implementation</summary>
        member this.WithFlushMappedFile(impl) =
            flushMappedFileImpl <- impl
            this
            
        /// <summary>Configure the LockMemory implementation</summary>
        member this.WithLockMemory(impl) =
            lockMemoryImpl <- impl
            this
            
        /// <summary>Configure the UnlockMemory implementation</summary>
        member this.WithUnlockMemory(impl) =
            unlockMemoryImpl <- impl
            this
            
        /// <summary>Track invocations for verification</summary>
        member val Invocations = System.Collections.Generic.List<string>() with get
        
        interface IPlatformMemory with
            member this.MapMemory size mappingType accessType =
                this.Invocations.Add("MapMemory")
                mapMemoryImpl size mappingType accessType
                
            member this.UnmapMemory handle address size =
                this.Invocations.Add("UnmapMemory")
                unmapMemoryImpl handle address size
                
            member this.MapFile filePath offset size accessType =
                this.Invocations.Add("MapFile")
                mapFileImpl filePath offset size accessType
                
            member this.FlushMappedFile handle address size =
                this.Invocations.Add("FlushMappedFile")
                flushMappedFileImpl handle address size
                
            member this.LockMemory address size =
                this.Invocations.Add("LockMemory")
                lockMemoryImpl address size
                
            member this.UnlockMemory address size =
                this.Invocations.Add("UnlockMemory")
                unlockMemoryImpl address size
    
    /// <summary>
    /// Mock IPC provider with configurable behavior
    /// </summary>
    type MockIpcProvider() =
        let mutable createNamedPipeImpl = fun (name: string) (direction: NamedPipe.PipeDirection) (mode: NamedPipe.PipeMode) (bufferSize: int<bytes>) -> 
            Ok 1n
            
        let mutable connectNamedPipeImpl = fun (name: string) (direction: NamedPipe.PipeDirection) -> 
            Ok 1n
            
        let mutable waitForNamedPipeConnectionImpl = fun (handle: nativeint) (timeout: int) -> 
            Ok ()
            
        let mutable writeNamedPipeImpl = fun (handle: nativeint) (data: byte[]) (offset: int) (count: int) -> 
            Ok count
            
        let mutable readNamedPipeImpl = fun (handle: nativeint) (buffer: byte[]) (offset: int) (count: int) -> 
            Ok count
            
        let mutable closeNamedPipeImpl = fun (handle: nativeint) -> 
            Ok ()
            
        let mutable createSharedMemoryImpl = fun (name: string) (size: int<bytes>) (accessType: AccessType) -> 
            Ok (1n, 1n)
            
        let mutable openSharedMemoryImpl = fun (name: string) (accessType: AccessType) -> 
            Ok (1n, 1n, 4096<bytes>)
            
        let mutable closeSharedMemoryImpl = fun (handle: nativeint) (address: nativeint) (size: int<bytes>) -> 
            Ok ()
            
        let mutable resourceExistsImpl = fun (name: string) (resourceType: string) -> 
            true
        
        /// <summary>Configure the CreateNamedPipe implementation</summary>
        member this.WithCreateNamedPipe(impl) =
            createNamedPipeImpl <- impl
            this
            
        /// <summary>Configure the ConnectNamedPipe implementation</summary>
        member this.WithConnectNamedPipe(impl) =
            connectNamedPipeImpl <- impl
            this
            
        /// <summary>Configure the WaitForNamedPipeConnection implementation</summary>
        member this.WithWaitForNamedPipeConnection(impl) =
            waitForNamedPipeConnectionImpl <- impl
            this
            
        /// <summary>Configure the WriteNamedPipe implementation</summary>
        member this.WithWriteNamedPipe(impl) =
            writeNamedPipeImpl <- impl
            this
            
        /// <summary>Configure the ReadNamedPipe implementation</summary>
        member this.WithReadNamedPipe(impl) =
            readNamedPipeImpl <- impl
            this
            
        /// <summary>Configure the CloseNamedPipe implementation</summary>
        member this.WithCloseNamedPipe(impl) =
            closeNamedPipeImpl <- impl
            this
            
        /// <summary>Configure the CreateSharedMemory implementation</summary>
        member this.WithCreateSharedMemory(impl) =
            createSharedMemoryImpl <- impl
            this
            
        /// <summary>Configure the OpenSharedMemory implementation</summary>
        member this.WithOpenSharedMemory(impl) =
            openSharedMemoryImpl <- impl
            this
            
        /// <summary>Configure the CloseSharedMemory implementation</summary>
        member this.WithCloseSharedMemory(impl) =
            closeSharedMemoryImpl <- impl
            this
            
        /// <summary>Configure the ResourceExists implementation</summary>
        member this.WithResourceExists(impl) =
            resourceExistsImpl <- impl
            this
            
        /// <summary>Track invocations for verification</summary>
        member val Invocations = System.Collections.Generic.List<string>() with get
        
        interface IPlatformIpc with
            member this.CreateNamedPipe name direction mode bufferSize =
                this.Invocations.Add("CreateNamedPipe")
                createNamedPipeImpl name direction mode bufferSize
                
            member this.ConnectNamedPipe name direction =
                this.Invocations.Add("ConnectNamedPipe")
                connectNamedPipeImpl name direction
                
            member this.WaitForNamedPipeConnection handle timeout =
                this.Invocations.Add("WaitForNamedPipeConnection")
                waitForNamedPipeConnectionImpl handle timeout
                
            member this.WriteNamedPipe handle data offset count =
                this.Invocations.Add("WriteNamedPipe")
                writeNamedPipeImpl handle data offset count
                
            member this.ReadNamedPipe handle buffer offset count =
                this.Invocations.Add("ReadNamedPipe")
                readNamedPipeImpl handle buffer offset count
                
            member this.CloseNamedPipe handle =
                this.Invocations.Add("CloseNamedPipe")
                closeNamedPipeImpl handle
                
            member this.CreateSharedMemory name size accessType =
                this.Invocations.Add("CreateSharedMemory")
                createSharedMemoryImpl name size accessType
                
            member this.OpenSharedMemory name accessType =
                this.Invocations.Add("OpenSharedMemory")
                openSharedMemoryImpl name accessType
                
            member this.CloseSharedMemory handle address size =
                this.Invocations.Add("CloseSharedMemory")
                closeSharedMemoryImpl handle address size
                
            member this.ResourceExists name resourceType =
                this.Invocations.Add("ResourceExists")
                resourceExistsImpl name resourceType

    /// <summary>
    /// Mock network provider with configurable behavior
    /// </summary>
    type MockNetworkProvider() =
        let mutable createSocketImpl = fun (addressFamily: int) (socketType: int) (protocolType: int) -> 
            Ok 1n
            
        let mutable bindSocketImpl = fun (handle: nativeint) (address: string) (port: int) -> 
            Ok ()
            
        let mutable listenSocketImpl = fun (handle: nativeint) (backlog: int) -> 
            Ok ()
            
        let mutable acceptSocketImpl = fun (handle: nativeint) -> 
            Ok (1n, "127.0.0.1", 8080)
            
        let mutable connectSocketImpl = fun (handle: nativeint) (address: string) (port: int) -> 
            Ok ()
            
        let mutable sendSocketImpl = fun (handle: nativeint) (data: byte[]) (offset: int) (count: int) (flags: int) -> 
            Ok count
            
        let mutable receiveSocketImpl = fun (handle: nativeint) (buffer: byte[]) (offset: int) (count: int) (flags: int) -> 
            Ok count
            
        let mutable closeSocketImpl = fun (handle: nativeint) -> 
            Ok ()
            
        let mutable shutdownSocketImpl = fun (handle: nativeint) (how: int) -> 
            Ok ()
            
        let mutable setSocketOptionImpl = fun (handle: nativeint) (level: int) (optionName: int) (optionValue: byte[]) -> 
            Ok ()
            
        let mutable getSocketOptionImpl = fun (handle: nativeint) (level: int) (optionName: int) (optionValue: byte[]) -> 
            Ok optionValue.Length
            
        let mutable getLocalEndPointImpl = fun (handle: nativeint) -> 
            Ok ("127.0.0.1", 8080)
            
        let mutable getRemoteEndPointImpl = fun (handle: nativeint) -> 
            Ok ("127.0.0.1", 8080)
            
        let mutable pollImpl = fun (handle: nativeint) (timeout: int) -> 
            Ok true
            
        let mutable resolveHostNameImpl = fun (hostName: string) -> 
            Ok [| "127.0.0.1" |]
        
        /// <summary>Configure implementations and track invocations</summary>
        member val Invocations = System.Collections.Generic.List<string>() with get
        
        interface IPlatformNetwork with
            member this.CreateSocket addressFamily socketType protocolType =
                this.Invocations.Add("CreateSocket")
                createSocketImpl addressFamily socketType protocolType
                
            member this.BindSocket handle address port =
                this.Invocations.Add("BindSocket")
                bindSocketImpl handle address port
                
            member this.ListenSocket handle backlog =
                this.Invocations.Add("ListenSocket")
                listenSocketImpl handle backlog
                
            member this.AcceptSocket handle =
                this.Invocations.Add("AcceptSocket")
                acceptSocketImpl handle
                
            member this.ConnectSocket handle address port =
                this.Invocations.Add("ConnectSocket")
                connectSocketImpl handle address port
                
            member this.SendSocket handle data offset count flags =
                this.Invocations.Add("SendSocket")
                sendSocketImpl handle data offset count flags
                
            member this.ReceiveSocket handle buffer offset count flags =
                this.Invocations.Add("ReceiveSocket")
                receiveSocketImpl handle buffer offset count flags
                
            member this.CloseSocket handle =
                this.Invocations.Add("CloseSocket")
                closeSocketImpl handle
                
            member this.ShutdownSocket handle how =
                this.Invocations.Add("ShutdownSocket")
                shutdownSocketImpl handle how
                
            member this.SetSocketOption handle level optionName optionValue =
                this.Invocations.Add("SetSocketOption")
                setSocketOptionImpl handle level optionName optionValue
                
            member this.GetSocketOption handle level optionName optionValue =
                this.Invocations.Add("GetSocketOption")
                getSocketOptionImpl handle level optionName optionValue
                
            member this.GetLocalEndPoint handle =
                this.Invocations.Add("GetLocalEndPoint")
                getLocalEndPointImpl handle
                
            member this.GetRemoteEndPoint handle =
                this.Invocations.Add("GetRemoteEndPoint")
                getRemoteEndPointImpl handle
                
            member this.Poll handle timeout =
                this.Invocations.Add("Poll")
                pollImpl handle timeout
                
            member this.ResolveHostName hostName =
                this.Invocations.Add("ResolveHostName")
                resolveHostNameImpl hostName

    /// <summary>
    /// Mock synchronization provider with configurable behavior
    /// </summary>
    type MockSyncProvider() =
        let mutable createMutexImpl = fun (name: string) (initialOwner: bool) -> 
            Ok 1n
            
        let mutable openMutexImpl = fun (name: string) -> 
            Ok 1n
            
        let mutable acquireMutexImpl = fun (handle: nativeint) (timeout: int) -> 
            Ok true
            
        let mutable releaseMutexImpl = fun (handle: nativeint) -> 
            Ok ()
            
        let mutable closeMutexImpl = fun (handle: nativeint) -> 
            Ok ()
            
        let mutable createSemaphoreImpl = fun (name: string) (initialCount: int) (maximumCount: int) -> 
            Ok 1n
            
        let mutable openSemaphoreImpl = fun (name: string) -> 
            Ok 1n
            
        let mutable acquireSemaphoreImpl = fun (handle: nativeint) (timeout: int) -> 
            Ok true
            
        let mutable releaseSemaphoreImpl = fun (handle: nativeint) (releaseCount: int) -> 
            Ok 0
            
        let mutable closeSemaphoreImpl = fun (handle: nativeint) -> 
            Ok ()
            
        /// <summary>Track invocations for verification</summary>
        member val Invocations = System.Collections.Generic.List<string>() with get
        
        interface IPlatformSync with
            member this.CreateMutex name initialOwner =
                this.Invocations.Add("CreateMutex")
                createMutexImpl name initialOwner
                
            member this.OpenMutex name =
                this.Invocations.Add("OpenMutex")
                openMutexImpl name
                
            member this.AcquireMutex handle timeout =
                this.Invocations.Add("AcquireMutex")
                acquireMutexImpl handle timeout
                
            member this.ReleaseMutex handle =
                this.Invocations.Add("ReleaseMutex")
                releaseMutexImpl handle
                
            member this.CloseMutex handle =
                this.Invocations.Add("CloseMutex")
                closeMutexImpl handle
                
            member this.CreateSemaphore name initialCount maximumCount =
                this.Invocations.Add("CreateSemaphore")
                createSemaphoreImpl name initialCount maximumCount
                
            member this.OpenSemaphore name =
                this.Invocations.Add("OpenSemaphore")
                openSemaphoreImpl name
                
            member this.AcquireSemaphore handle timeout =
                this.Invocations.Add("AcquireSemaphore")
                acquireSemaphoreImpl handle timeout
                
            member this.ReleaseSemaphore handle releaseCount =
                this.Invocations.Add("ReleaseSemaphore")
                releaseSemaphoreImpl handle releaseCount
                
            member this.CloseSemaphore handle =
                this.Invocations.Add("CloseSemaphore")
                closeSemaphoreImpl handle

module MockProviderTests =
    open MockProviders
    
    [<Tests>]
    let mockMemoryProviderTests =
        testList "MockMemoryProvider Tests" [
            test "Default implementations return expected values" {
                let provider = MockMemoryProvider() :> IPlatformMemory
                
                // Default implementations should succeed
                match provider.MapMemory 4096<bytes> MappingType.PrivateMapping AccessType.ReadWrite with
                | Ok (handle, address) ->
                    Expect.equal handle 1n "Default handle should be 1n"
                    Expect.equal address 1n "Default address should be 1n"
                | Error e ->
                    failwith $"MapMemory failed: {e.Message}"
                    
                match provider.UnmapMemory 1n 1n 4096<bytes> with
                | Ok () -> () // Success
                | Error e -> failwith $"UnmapMemory failed: {e.Message}"
            }
            
            test "Custom implementations are used" {
                let mutable wasCalled = false
                
                let provider = 
                    MockMemoryProvider()
                        .WithMapMemory(fun size mappingType accessType -> 
                            wasCalled <- true
                            Ok (2n, 3n))
                        :> IPlatformMemory
                
                match provider.MapMemory 4096<bytes> MappingType.PrivateMapping AccessType.ReadWrite with
                | Ok (handle, address) ->
                    Expect.isTrue wasCalled "Custom implementation should be called"
                    Expect.equal handle 2n "Custom handle should be 2n"
                    Expect.equal address 3n "Custom address should be 3n"
                | Error e ->
                    failwith $"MapMemory failed: {e.Message}"
            }
            
            test "Error responses can be simulated" {
                let provider = 
                    MockMemoryProvider()
                        .WithUnmapMemory(fun handle address size -> 
                            Error (invalidValueError "Simulated error"))
                        :> IPlatformMemory
                
                match provider.UnmapMemory 1n 1n 4096<bytes> with
                | Ok () -> 
                    failwith "Should have returned error"
                | Error e -> 
                    Expect.stringContains e.Message "Simulated error" "Error message should match simulation"
            }
            
            test "Invocations are tracked" {
                let mockProvider = MockMemoryProvider()
                let provider = mockProvider :> IPlatformMemory
                
                // Make several calls
                provider.MapMemory 4096<bytes> MappingType.PrivateMapping AccessType.ReadWrite |> ignore
                provider.UnmapMemory 1n 1n 4096<bytes> |> ignore
                provider.LockMemory 1n 4096<bytes> |> ignore
                
                // Check the invocation record
                Expect.equal mockProvider.Invocations.Count 3 "Should have recorded 3 invocations"
                Expect.equal mockProvider.Invocations.[0] "MapMemory" "First invocation should be MapMemory"
                Expect.equal mockProvider.Invocations.[1] "UnmapMemory" "Second invocation should be UnmapMemory"
                Expect.equal mockProvider.Invocations.[2] "LockMemory" "Third invocation should be LockMemory"
            }
        ]
        
    [<Tests>]
    let mockIpcProviderTests =
        testList "MockIpcProvider Tests" [
            test "Default implementations return expected values" {
                let provider = MockIpcProvider() :> IPlatformIpc
                
                // Default implementations should succeed
                match provider.CreateNamedPipe "testpipe" NamedPipe.PipeDirection.InOut NamedPipe.PipeMode.Message 4096<bytes> with
                | Ok handle ->
                    Expect.equal handle 1n "Default handle should be 1n"
                | Error e ->
                    failwith $"CreateNamedPipe failed: {e.Message}"
                    
                // ResourceExists should return true by default
                Expect.isTrue (provider.ResourceExists "testpipe" "pipe") "ResourceExists should return true by default"
            }
            
            test "Custom implementations are used" {
                let mockProvider = MockIpcProvider()
                
                // Custom pipe data simulation
                let pipeData = System.Collections.Generic.Dictionary<nativeint, byte[]>()
                
                mockProvider.WithWriteNamedPipe(fun handle data offset count ->
                    let actualData = Array.sub data offset count
                    pipeData.[handle] <- actualData
                    Ok count
                ) |> ignore
                
                mockProvider.WithReadNamedPipe(fun handle buffer offset count ->
                    match pipeData.TryGetValue handle with
                    | true, data ->
                        let bytesToCopy = min count data.Length
                        Array.Copy(data, 0, buffer, offset, bytesToCopy)
                        Ok bytesToCopy
                    | false, _ ->
                        Ok 0
                ) |> ignore
                
                let provider = mockProvider :> IPlatformIpc
                
                // Get a pipe handle
                match provider.CreateNamedPipe "testpipe" NamedPipe.PipeDirection.InOut NamedPipe.PipeMode.Message 4096<bytes> with
                | Ok handle ->
                    // Write data to the pipe
                    let writeData = [| 1uy; 2uy; 3uy; 4uy |]
                    match provider.WriteNamedPipe handle writeData 0 writeData.Length with
                    | Ok bytesWritten ->
                        Expect.equal bytesWritten writeData.Length "Should write all bytes"
                        
                        // Read data from the pipe
                        let readBuffer = Array.zeroCreate<byte> 10
                        match provider.ReadNamedPipe handle readBuffer 0 readBuffer.Length with
                        | Ok bytesRead ->
                            Expect.equal bytesRead writeData.Length "Should read same number of bytes"
                            Expect.sequenceEqual readBuffer.[0..bytesRead-1] writeData "Read data should match written data"
                        | Error e ->
                            failwith $"ReadNamedPipe failed: {e.Message}"
                    | Error e ->
                        failwith $"WriteNamedPipe failed: {e.Message}"
                | Error e ->
                    failwith $"CreateNamedPipe failed: {e.Message}"
            }
            
            test "Error responses can be simulated" {
                let provider = 
                    MockIpcProvider()
                        .WithResourceExists(fun name resourceType -> 
                            name = "exists" && resourceType = "pipe")
                        :> IPlatformIpc
                
                // Test with both existing and non-existing resources
                Expect.isTrue (provider.ResourceExists "exists" "pipe") "Should return true for existing pipe"
                Expect.isFalse (provider.ResourceExists "nonexistent" "pipe") "Should return false for non-existent pipe"
                Expect.isFalse (provider.ResourceExists "exists" "sharedmem") "Should return false for wrong resource type"
            }
        ]
        
    [<Tests>]
    let mockNetworkProviderTests =
        testList "MockNetworkProvider Tests" [
            test "Simulated server-client communication" {
                let mockProvider = MockNetworkProvider()
                let provider = mockProvider :> IPlatformNetwork
                
                // Create server socket
                match provider.CreateSocket 2 1 6 with // AF_INET, SOCK_STREAM, IPPROTO_TCP
                | Ok serverHandle ->
                    // Create client socket
                    match provider.CreateSocket 2 1 6 with
                    | Ok clientHandle ->
                        // Bind and listen on server
                        provider.BindSocket serverHandle "127.0.0.1" 8080 |> ignore
                        provider.ListenSocket serverHandle 5 |> ignore
                        
                        // Connect client
                        provider.ConnectSocket clientHandle "127.0.0.1" 8080 |> ignore
                        
                        // Accept connection on server
                        match provider.AcceptSocket serverHandle with
                        | Ok (acceptedHandle, clientAddr, clientPort) ->
                            // Check invocation order
                            let expectedOrder = [
                                "CreateSocket"; // Server
                                "CreateSocket"; // Client
                                "BindSocket"; 
                                "ListenSocket"; 
                                "ConnectSocket"; 
                                "AcceptSocket"
                            ]
                            
                            let actualOrder = mockProvider.Invocations |> Seq.take expectedOrder.Length |> Seq.toList
                            Expect.sequenceEqual actualOrder expectedOrder "Operations should be called in expected order"
                            
                            // Check we can get endpoint information
                            match provider.GetLocalEndPoint acceptedHandle with
                            | Ok (localAddr, localPort) ->
                                Expect.isNonEmptyString localAddr "Local address should not be empty"
                                Expect.isGreaterThan localPort 0 "Local port should be positive"
                            | Error e ->
                                failwith $"GetLocalEndPoint failed: {e.Message}"
                                
                            match provider.GetRemoteEndPoint clientHandle with
                            | Ok (remoteAddr, remotePort) ->
                                Expect.isNonEmptyString remoteAddr "Remote address should not be empty"
                                Expect.isGreaterThan remotePort 0 "Remote port should be positive"
                            | Error e ->
                                failwith $"GetRemoteEndPoint failed: {e.Message}"
                                
                        | Error e ->
                            failwith $"AcceptSocket failed: {e.Message}"
                    | Error e ->
                        failwith $"CreateSocket (client) failed: {e.Message}"
                | Error e ->
                    failwith $"CreateSocket (server) failed: {e.Message}"
            }
        ]
        
    [<Tests>]
    let mockSyncProviderTests =
        testList "MockSyncProvider Tests" [
            test "Mutex acquire/release simulation" {
                let mockProvider = MockSyncProvider()
                
                // Track mutex state
                let mutexOwnership = System.Collections.Generic.Dictionary<nativeint, bool>()
                
                mockProvider.WithCreateMutex(fun name initialOwner -> 
                    let handle = 100n // Fixed test handle
                    mutexOwnership.[handle] <- initialOwner
                    Ok handle
                ) |> ignore
                
                mockProvider.WithAcquireMutex(fun handle timeout -> 
                    match mutexOwnership.TryGetValue handle with
                    | true, isOwned ->
                        if isOwned then
                            Ok false // Already owned
                        else
                            mutexOwnership.[handle] <- true
                            Ok true // Acquired
                    | false, _ ->
                        Error (invalidValueError "Invalid mutex handle")
                ) |> ignore
                
                mockProvider.WithReleaseMutex(fun handle -> 
                    match mutexOwnership.TryGetValue handle with
                    | true, isOwned ->
                        if isOwned then
                            mutexOwnership.[handle] <- false
                            Ok () // Released
                        else
                            Error (invalidValueError "Mutex not owned")
                    | false, _ ->
                        Error (invalidValueError "Invalid mutex handle")
                ) |> ignore
                
                let provider = mockProvider :> IPlatformSync
                
                // Create a mutex (not initially owned)
                match provider.CreateMutex "testmutex" false with
                | Ok handle ->
                    // Mutex should not be owned initially
                    Expect.isFalse mutexOwnership.[handle] "Mutex should not be owned initially"
                    
                    // Acquire the mutex
                    match provider.AcquireMutex handle 0 with
                    | Ok acquired ->
                        Expect.isTrue acquired "Should acquire the mutex"
                        Expect.isTrue mutexOwnership.[handle] "Mutex should be owned after acquire"
                        
                        // Try to acquire again (should fail)
                        match provider.AcquireMutex handle 0 with
                        | Ok acquired2 ->
                            Expect.isFalse acquired2 "Should not acquire the mutex again"
                            
                            // Release the mutex
                            match provider.ReleaseMutex handle with
                            | Ok () ->
                                Expect.isFalse mutexOwnership.[handle] "Mutex should not be owned after release"
                                
                                // Should be able to acquire again
                                match provider.AcquireMutex handle 0 with
                                | Ok acquired3 ->
                                    Expect.isTrue acquired3 "Should acquire the mutex after release"
                                | Error e ->
                                    failwith $"AcquireMutex (after release) failed: {e.Message}"
                            | Error e ->
                                failwith $"ReleaseMutex failed: {e.Message}"
                        | Error e ->
                            failwith $"AcquireMutex (second) failed: {e.Message}"
                    | Error e ->
                        failwith $"AcquireMutex failed: {e.Message}"
                | Error e ->
                    failwith $"CreateMutex failed: {e.Message}"
            }
            
            test "Semaphore count simulation" {
                let mockProvider = MockSyncProvider()
                
                // Track semaphore state
                let semaphoreState = System.Collections.Generic.Dictionary<nativeint, struct(int * int)>()
                
                mockProvider.WithCreateSemaphore(fun name initialCount maximumCount -> 
                    let handle = 200n // Fixed test handle
                    semaphoreState.[handle] <- struct(initialCount, maximumCount)
                    Ok handle
                ) |> ignore
                
                mockProvider.WithAcquireSemaphore(fun handle timeout -> 
                    match semaphoreState.TryGetValue handle with
                    | true, struct(currentCount, _) ->
                        if currentCount > 0 then
                            let struct(_, maxCount) = semaphoreState.[handle]
                            semaphoreState.[handle] <- struct(currentCount - 1, maxCount)
                            Ok true // Acquired
                        else if timeout = 0 then
                            Ok false // Would block, but timeout is 0
                        else
                            // In a real implementation, this would wait until timeout
                            Ok false // Simplified: just fail for non-zero timeout
                    | false, _ ->
                        Error (invalidValueError "Invalid semaphore handle")
                ) |> ignore
                
                mockProvider.WithReleaseSemaphore(fun handle releaseCount -> 
                    match semaphoreState.TryGetValue handle with
                    | true, struct(currentCount, maxCount) ->
                        if currentCount + releaseCount <= maxCount then
                            let oldCount = currentCount
                            semaphoreState.[handle] <- struct(currentCount + releaseCount, maxCount)
                            Ok oldCount
                        else
                            Error (invalidValueError "Would exceed maximum count")
                    | false, _ ->
                        Error (invalidValueError "Invalid semaphore handle")
                ) |> ignore
                
                let provider = mockProvider :> IPlatformSync
                
                // Create a semaphore with initial count 2, max count 5
                match provider.CreateSemaphore "testsem" 2 5 with
                | Ok handle ->
                    // Verify initial state
                    let struct(count, maxCount) = semaphoreState.[handle]
                    Expect.equal count 2 "Initial count should be 2"
                    Expect.equal maxCount 5 "Maximum count should be 5"
                    
                    // Acquire twice
                    match provider.AcquireSemaphore handle 0 with
                    | Ok acquired1 ->
                        Expect.isTrue acquired1 "Should acquire the semaphore (1)"
                        
                        match provider.AcquireSemaphore handle 0 with
                        | Ok acquired2 ->
                            Expect.isTrue acquired2 "Should acquire the semaphore (2)"
                            
                            // Count should now be 0
                            let struct(count2, _) = semaphoreState.[handle]
                            Expect.equal count2 0 "Count should be 0 after two acquires"
                            
                            // Try to acquire again (should fail with timeout 0)
                            match provider.AcquireSemaphore handle 0 with
                            | Ok acquired3 ->
                                Expect.isFalse acquired3 "Should not acquire the semaphore when count is 0"
                                
                                // Release with count 2
                                match provider.ReleaseSemaphore handle 2 with
                                | Ok prevCount ->
                                    Expect.equal prevCount 0 "Previous count should be 0"
                                    
                                    // Count should now be 2 again
                                    let struct(count3, _) = semaphoreState.[handle]
                                    Expect.equal count3 2 "Count should be 2 after release with count 2"
                                    
                                    // Try to release with count 4 (should exceed maximum)
                                    match provider.ReleaseSemaphore handle 4 with
                                    | Ok _ ->
                                        failwith "Should have failed when exceeding maximum count"
                                    | Error e ->
                                        Expect.stringContains e.Message "exceed maximum" "Error should mention exceeding maximum count"
                                | Error e ->
                                    failwith $"ReleaseSemaphore failed: {e.Message}"
                            | Error e ->
                                failwith $"AcquireSemaphore (third) failed: {e.Message}"
                        | Error e ->
                            failwith $"AcquireSemaphore (second) failed: {e.Message}"
                    | Error e ->
                        failwith $"AcquireSemaphore failed: {e.Message}"
                | Error e ->
                    failwith $"CreateSemaphore failed: {e.Message}"
            }
        ]
        
    let tests = 
        testList "Mock Provider Tests" [
            mockMemoryProviderTests
            mockIpcProviderTests
            mockNetworkProviderTests
            mockSyncProviderTests
        ]

module Program =
    [<EntryPoint>]
    let main args =
        runTestsWithArgs defaultConfig args MockProviderTests.tests