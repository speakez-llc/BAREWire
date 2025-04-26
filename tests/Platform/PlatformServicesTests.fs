namespace BAREWire.Tests.Platform

open Expecto
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Platform
open BAREWire.Platform.Common
open BAREWire.Platform.Common.Interfaces
open BAREWire.Platform.Common.Registry

/// Mock implementations for testing the platform services
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

/// Helper to reset platform services between tests
module TestHelpers =
    let resetPlatformServices() =
        // Use reflection to reset the isInitialized flag
        // This is not ideal, but necessary for testing initialization
        let fieldInfo = 
            typeof<PlatformServices>
                .GetField("isInitialized", System.Reflection.BindingFlags.Static ||| 
                                           System.Reflection.BindingFlags.NonPublic)
        
        if not (isNull fieldInfo) then
            fieldInfo.SetValue(null, false)

module PlatformServicesTests =
    open MockProviders
    open TestHelpers
    
    // Run before all tests
    let setup() =
        resetPlatformServices()
        
    [<Tests>]
    let initializationTests =
        testList "Platform Services Initialization Tests" [
            testCase "Initialize with default platform" <| fun _ ->
                // Setup
                resetPlatformServices()
                
                // Test
                let result = PlatformServices.initialize 10
                
                // Verify
                Expect.isTrue result "First initialization should return true"
                Expect.isTrue (PlatformServices.isInitialized()) "IsInitialized should return true"
                
                // Second initialization should return false
                let result2 = PlatformServices.initialize 10
                Expect.isFalse result2 "Second initialization should return false"
            
            testCase "Initialize with explicit platform" <| fun _ ->
                // Setup
                resetPlatformServices()
                
                // Test with Windows platform
                let result = PlatformServices.initializeWithPlatform PlatformType.Windows 10
                
                // Verify
                Expect.isTrue result "First initialization should return true"
                Expect.isTrue (PlatformServices.isInitialized()) "IsInitialized should return true"
                Expect.equal (PlatformServices.getCurrentPlatform()) PlatformType.Windows "Platform should be Windows"
            
            testCase "EnsureInitialized initializes if needed" <| fun _ ->
                // Setup
                resetPlatformServices()
                
                // Test
                let result = PlatformServices.ensureInitialized 10
                
                // Verify
                Expect.isTrue result "EnsureInitialized should return true"
                Expect.isTrue (PlatformServices.isInitialized()) "IsInitialized should return true"
                
                // Second call should still return true
                let result2 = PlatformServices.ensureInitialized 10
                Expect.isTrue result2 "Second EnsureInitialized should also return true"
            
            testCase "GetCurrentPlatform returns initialized platform" <| fun _ ->
                // Setup
                resetPlatformServices()
                PlatformServices.initializeWithPlatform PlatformType.MacOS 10
                
                // Test
                let platform = PlatformServices.getCurrentPlatform()
                
                // Verify
                Expect.equal platform PlatformType.MacOS "Platform should be MacOS"
            
            testCase "GetMemoryProvider fails when not initialized" <| fun _ ->
                // Setup
                resetPlatformServices()
                
                // Test
                let provider = PlatformServices.getMemoryProvider()
                
                // Verify
                match provider with
                | Ok _ -> failwith "Should have returned error when not initialized"
                | Error e -> 
                    Expect.stringContains e.Message "not initialized" 
                        "Error should indicate platform services not initialized"
            
            testCase "GetMemoryProvider succeeds when initialized" <| fun _ ->
                // Setup
                resetPlatformServices()
                PlatformServices.initialize 10 |> ignore
                
                // Register a custom provider
                let platform = PlatformServices.getCurrentPlatform()
                Registry.registerMemoryProvider platform (TestMemoryProvider())
                
                // Test
                let provider = PlatformServices.getMemoryProvider()
                
                // Verify
                match provider with
                | Ok _ -> () // Success
                | Error e -> failwith $"Should have returned a provider: {e.Message}"
            
            testCase "GetIpcProvider succeeds when initialized" <| fun _ ->
                // Setup
                resetPlatformServices()
                PlatformServices.initialize 10 |> ignore
                
                // Register a custom provider
                let platform = PlatformServices.getCurrentPlatform()
                Registry.registerIpcProvider platform (TestIpcProvider())
                
                // Test
                let provider = PlatformServices.getIpcProvider()
                
                // Verify
                match provider with
                | Ok _ -> () // Success
                | Error e -> failwith $"Should have returned a provider: {e.Message}"
            
            testCase "GetNetworkProvider succeeds when initialized" <| fun _ ->
                // Setup
                resetPlatformServices()
                PlatformServices.initialize 10 |> ignore
                
                // Register a custom provider
                let platform = PlatformServices.getCurrentPlatform()
                Registry.registerNetworkProvider platform (TestNetworkProvider())
                
                // Test
                let provider = PlatformServices.getNetworkProvider()
                
                // Verify
                match provider with
                | Ok _ -> () // Success
                | Error e -> failwith $"Should have returned a provider: {e.Message}"
            
            testCase "GetSyncProvider succeeds when initialized" <| fun _ ->
                // Setup
                resetPlatformServices()
                PlatformServices.initialize 10 |> ignore
                
                // Register a custom provider
                let platform = PlatformServices.getCurrentPlatform()
                Registry.registerSyncProvider platform (TestSyncProvider())
                
                // Test
                let provider = PlatformServices.getSyncProvider()
                
                // Verify
                match provider with
                | Ok _ -> () // Success
                | Error e -> failwith $"Should have returned a provider: {e.Message}"
            
        ]
        
    let tests = 
        testSequenced <| testList "Platform Services Tests" [
            testFixture setup initializationTests
        ]

module Program =
    [<EntryPoint>]
    let main args =
        runTestsWithArgs defaultConfig args PlatformServicesTests.tests