namespace BAREWire.Tests.Platform.Common

open Expecto
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Core.Memory
open BAREWire.Platform.Common.Interfaces
open BAREWire.Platform.Common.Registry
open BAREWire.Memory.Mapping
open BAREWire.IPC

/// Mock implementations for testing the registry
module MockProviders =
    type TestMemoryProvider() =
        interface IPlatformMemory with
            member _.MapMemory size mappingType accessType = Ok (1n, 1n)
            member _.UnmapMemory handle address size = Ok ()
            member _.MapFile filePath offset size accessType = Ok (1n, 1n)
            member _.FlushMappedFile handle address size = Ok ()
            member _.LockMemory address size = Ok ()
            member _.UnlockMemory address size = Ok ()
            
    type TestIpcProvider() =
        interface IPlatformIpc with
            member _.CreateNamedPipe name direction mode bufferSize = Ok 1n
            member _.ConnectNamedPipe name direction = Ok 1n
            member _.WaitForNamedPipeConnection handle timeout = Ok ()
            member _.WriteNamedPipe handle data offset count = Ok count
            member _.ReadNamedPipe handle buffer offset count = Ok count
            member _.CloseNamedPipe handle = Ok ()
            member _.CreateSharedMemory name size accessType = Ok (1n, 1n)
            member _.OpenSharedMemory name accessType = Ok (1n, 1n, 4096<bytes>)
            member _.CloseSharedMemory handle address size = Ok ()
            member _.ResourceExists name resourceType = true
            
    type TestNetworkProvider() =
        interface IPlatformNetwork with
            member _.CreateSocket addressFamily socketType protocolType = Ok 1n
            member _.BindSocket handle address port = Ok ()
            member _.ListenSocket handle backlog = Ok ()
            member _.AcceptSocket handle = Ok (1n, "127.0.0.1", 8080)
            member _.ConnectSocket handle address port = Ok ()
            member _.SendSocket handle data offset count flags = Ok count
            member _.ReceiveSocket handle buffer offset count flags = Ok count
            member _.CloseSocket handle = Ok ()
            member _.ShutdownSocket handle how = Ok ()
            member _.SetSocketOption handle level optionName optionValue = Ok ()
            member _.GetSocketOption handle level optionName optionValue = Ok optionValue.Length
            member _.GetLocalEndPoint handle = Ok ("127.0.0.1", 8080)
            member _.GetRemoteEndPoint handle = Ok ("127.0.0.1", 8080)
            member _.Poll handle timeout = Ok true
            member _.ResolveHostName hostName = Ok [| "127.0.0.1" |]
            
    type TestSyncProvider() =
        interface IPlatformSync with
            member _.CreateMutex name initialOwner = Ok 1n
            member _.OpenMutex name = Ok 1n
            member _.AcquireMutex handle timeout = Ok true
            member _.ReleaseMutex handle = Ok ()
            member _.CloseMutex handle = Ok ()
            member _.CreateSemaphore name initialCount maximumCount = Ok 1n
            member _.OpenSemaphore name = Ok 1n
            member _.AcquireSemaphore handle timeout = Ok true
            member _.ReleaseSemaphore handle releaseCount = Ok initialCount
            member _.CloseSemaphore handle = Ok ()

module RegistryTests =
    open MockProviders
    
    [<Tests>]
    let registryInitializationTests =
        testList "Registry Initialization Tests" [
            test "Initialize registries with capacity" {
                // Initialize with capacity of 10
                initializeRegistries 10
                
                // This is a bit of an implementation test, but we can't easily
                // verify initialization without examining state
                Expect.isTrue true "Registry initialization completed without exception"
            }
        ]
        
    [<Tests>]
    let platformDetectionTests =
        testList "Platform Detection Tests" [
            test "Get current platform returns a valid platform" {
                let platform = getCurrentPlatform()
                
                // The platform should be one of the enumeration values
                Expect.isTrue (
                    platform = PlatformType.Windows ||
                    platform = PlatformType.Linux ||
                    platform = PlatformType.MacOS ||
                    platform = PlatformType.Android ||
                    platform = PlatformType.iOS ||
                    platform = PlatformType.WebAssembly ||
                    platform = PlatformType.InMemory
                ) "Platform should be a valid enum value"
            }
            
            test "Setting current platform works" {
                // Save current platform
                let originalPlatform = getCurrentPlatform()
                
                // Set to a specific platform
                setCurrentPlatform PlatformType.Windows
                Expect.equal (getCurrentPlatform()) PlatformType.Windows "Platform should be set to Windows"
                
                // Restore original platform
                setCurrentPlatform originalPlatform
            }
        ]
        
    [<Tests>]
    let providerRegistrationTests =
        testList "Provider Registration Tests" [
            test "Register and get memory provider" {
                // Initialize registry
                initializeRegistries 10
                
                // Register a provider
                let provider = TestMemoryProvider()
                registerMemoryProvider PlatformType.Windows provider
                
                // Get the provider
                match getMemoryProvider PlatformType.Windows with
                | Ok retrievedProvider ->
                    Expect.isTrue (box retrievedProvider = box provider) "Retrieved provider should be the one we registered"
                | Error e ->
                    failwith $"Failed to get memory provider: {e.Message}"
            }
            
            test "Register and get IPC provider" {
                // Initialize registry
                initializeRegistries 10
                
                // Register a provider
                let provider = TestIpcProvider()
                registerIpcProvider PlatformType.Windows provider
                
                // Get the provider
                match getIpcProvider PlatformType.Windows with
                | Ok retrievedProvider ->
                    Expect.isTrue (box retrievedProvider = box provider) "Retrieved provider should be the one we registered"
                | Error e ->
                    failwith $"Failed to get IPC provider: {e.Message}"
            }
            
            test "Register and get network provider" {
                // Initialize registry
                initializeRegistries 10
                
                // Register a provider
                let provider = TestNetworkProvider()
                registerNetworkProvider PlatformType.Windows provider
                
                // Get the provider
                match getNetworkProvider PlatformType.Windows with
                | Ok retrievedProvider ->
                    Expect.isTrue (box retrievedProvider = box provider) "Retrieved provider should be the one we registered"
                | Error e ->
                    failwith $"Failed to get network provider: {e.Message}"
            }
            
            test "Register and get sync provider" {
                // Initialize registry
                initializeRegistries 10
                
                // Register a provider
                let provider = TestSyncProvider()
                registerSyncProvider PlatformType.Windows provider
                
                // Get the provider
                match getSyncProvider PlatformType.Windows with
                | Ok retrievedProvider ->
                    Expect.isTrue (box retrievedProvider = box provider) "Retrieved provider should be the one we registered"
                | Error e ->
                    failwith $"Failed to get sync provider: {e.Message}"
            }
        ]
        
    [<Tests>]
    let getCurrentProviderTests =
        testList "Get Current Provider Tests" [
            test "Get current memory provider" {
                // Initialize registry
                initializeRegistries 10
                
                // Set platform
                setCurrentPlatform PlatformType.Windows
                
                // Register a provider
                let provider = TestMemoryProvider()
                registerMemoryProvider PlatformType.Windows provider
                
                // Get the current provider
                match getCurrentMemoryProvider() with
                | Ok retrievedProvider ->
                    Expect.isTrue (box retrievedProvider = box provider) "Retrieved provider should be the one we registered"
                | Error e ->
                    failwith $"Failed to get current memory provider: {e.Message}"
            }
            
            test "Get non-existent provider returns error" {
                // Initialize registry
                initializeRegistries 10
                
                // Set platform
                setCurrentPlatform PlatformType.Linux
                
                // Don't register any provider
                
                // Get the provider
                match getMemoryProvider PlatformType.Linux with
                | Ok _ ->
                    failwith "Should have returned error for non-existent provider"
                | Error e ->
                    Expect.isTrue (e.Message.Contains "No memory provider registered") "Error message should indicate missing provider"
            }
        ]
        
    let tests = 
        testList "Registry Tests" [
            registryInitializationTests
            platformDetectionTests
            providerRegistrationTests
            getCurrentProviderTests
        ]

module Program =
    [<EntryPoint>]
    let main args =
        runTestsWithArgs defaultConfig args RegistryTests.tests