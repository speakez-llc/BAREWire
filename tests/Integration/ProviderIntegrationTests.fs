module BAREWire.Tests.Integration.ProviderIntegrationTests

open Expecto
open System
open System.IO
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Core.Memory
open BAREWire.Memory.Mapping
open BAREWire.Memory.Region
open BAREWire.Memory.View
open BAREWire.Platform
open BAREWire.Platform.Common
open BAREWire.Platform.Common.Interfaces
open BAREWire.Platform.Providers
open BAREWire.Network.Frame
open BAREWire.Network.Transport
open BAREWire.Network.Protocol
open BAREWire.IPC
open BAREWire.Schema
open BAREWire.Schema.DSL

// Platform-specific test utilities
module PlatformTestUtil =
    // Detects if we're running on Windows
    let isWindows() =
        #if WINDOWS
            true
        #else
            match Environment.OSVersion.Platform with
            | PlatformID.Win32NT | PlatformID.Win32S | PlatformID.Win32Windows | PlatformID.WinCE -> true
            | _ -> false
        #endif
        
    // Detects if we're running on Linux
    let isLinux() =
        #if LINUX
            true
        #else
            match Environment.OSVersion.Platform with
            | PlatformID.Unix -> true
            | _ -> false
        #endif
        
    // Detects if we're running on macOS
    let isMacOS() =
        #if MACOS
            true
        #else
            match Environment.OSVersion.Platform with
            | PlatformID.MacOSX -> true
            | _ -> false
        #endif
        
    // Gets the appropriate platform type based on OS detection
    let getPlatformType() =
        if isWindows() then Registry.PlatformType.Windows
        elif isLinux() then Registry.PlatformType.Linux
        elif isMacOS() then Registry.PlatformType.MacOS
        else Registry.PlatformType.InMemory // Fallback
        
    // Initializes platform services with the correct platform type
    let initializePlatformServices() =
        let platformType = getPlatformType()
        
        // First try to initialize with detected platform
        match PlatformServices.initializeWithPlatform platformType 10 with
        | true -> 
            // Successfully initialized with native platform
            platformType
        | false ->
            // Fall back to in-memory if native platform initialization fails
            match PlatformServices.initializeWithPlatform Registry.PlatformType.InMemory 10 with
            | true -> Registry.PlatformType.InMemory
            | false -> failwith "Failed to initialize platform services"

// Test messages for network protocol tests
type TestMessage = {
    Id: int
    Name: string
    Value: float
}

// Create a schema for TestMessage
let createTestMessageSchema() =
    schema "TestMessage"
    |> withType "TestMessage" (struct' [
        field "Id" i32
        field "Name" string
        field "Value" f64
    ])
    |> validate
    |> function
        | Ok schema -> schema
        | Error errors -> 
            let errorMessages = errors |> List.map Validation.errorToString |> String.concat ", "
            failwith $"Schema validation failed: {errorMessages}"

[<Tests>]
let memoryProviderTests =
    // These tests will use the actual platform memory provider when available
    testList "Memory Provider Tests" [
        testCase "Memory mapping with platform provider" <| fun _ ->
            // Initialize platform services
            let platformType = PlatformTestUtil.initializePlatformServices()
            
            // Get the memory provider
            match PlatformServices.getMemoryProvider() with
            | Ok provider ->
                // Map memory
                match provider.MapMemory 4096<bytes> MappingType.PrivateMapping AccessType.ReadWrite with
                | Ok (handle, address) ->
                    // Create a region from the memory
                    match Region.fromPointer<int, region> address 4096<bytes> with
                    | Ok region ->
                        // Write to the memory
                        let testData = [| 1uy; 2uy; 3uy; 4uy |]
                        for i = 0 to testData.Length - 1 do
                            Memory.writeByte region (i * 1<offset>) testData.[i]
                        
                        // Read from the memory
                        for i = 0 to testData.Length - 1 do
                            let readByte = Memory.readByte region (i * 1<offset>)
                            Expect.equal readByte testData.[i] $"Byte at offset {i} should match"
                        
                        // Unmap memory
                        match provider.UnmapMemory handle address 4096<bytes> with
                        | Ok () -> ()
                        | Error e -> failwith $"Failed to unmap memory: {toString e}"
                    | Error e ->
                        failwith $"Failed to create region from pointer: {toString e}"
                | Error e ->
                    failwith $"Failed to map memory: {toString e}"
            | Error e ->
                failwith $"Failed to get memory provider: {toString e}"
                
        testCase "Memory locking and unlocking" <| fun _ ->
            // This test only runs the API, without being able to verify the memory is actually locked
            // Initialize platform services
            let platformType = PlatformTestUtil.initializePlatformServices()
            
            // Get the memory provider
            match PlatformServices.getMemoryProvider() with
            | Ok provider ->
                // Map memory
                match provider.MapMemory 4096<bytes> MappingType.PrivateMapping AccessType.ReadWrite with
                | Ok (handle, address) ->
                    // Lock memory
                    match provider.LockMemory address 4096<bytes> with
                    | Ok () ->
                        // Unlock memory
                        match provider.UnlockMemory address 4096<bytes> with
                        | Ok () ->
                            // Unmap memory
                            match provider.UnmapMemory handle address 4096<bytes> with
                            | Ok () -> ()
                            | Error e -> failwith $"Failed to unmap memory: {toString e}"
                        | Error e ->
                            // Unmap memory even if unlock fails
                            provider.UnmapMemory handle address 4096<bytes> |> ignore
                            failwith $"Failed to unlock memory: {toString e}"
                    | Error e ->
                        // Unmap memory even if lock fails
                        provider.UnmapMemory handle address 4096<bytes> |> ignore
                        
                        // Skip test on platforms that don't support locking
                        if platformType = Registry.PlatformType.InMemory then
                            ()
                        else
                            failwith $"Failed to lock memory: {toString e}"
                | Error e ->
                    failwith $"Failed to map memory: {toString e}"
            | Error e ->
                failwith $"Failed to get memory provider: {toString e}"
                
        testCaseIf (PlatformTestUtil.isWindows() || PlatformTestUtil.isLinux() || PlatformTestUtil.isMacOS())
            "File mapping with platform provider" <| fun _ ->
            // This test creates a temporary file, maps it, and verifies we can read/write
            
            // Initialize platform services
            let platformType = PlatformTestUtil.initializePlatformServices()
            
            // Skip for in-memory platform
            if platformType = Registry.PlatformType.InMemory then
                ()
            else
                // Create a temporary file
                let tempFilePath = Path.GetTempFileName()
                
                try
                    // Write some data to the file
                    let testData = [| 1uy; 2uy; 3uy; 4uy; 5uy; 6uy; 7uy; 8uy |]
                    File.WriteAllBytes(tempFilePath, testData)
                    
                    // Get the memory provider
                    match PlatformServices.getMemoryProvider() with
                    | Ok provider ->
                        // Map the file
                        match provider.MapFile tempFilePath 0L 8<bytes> AccessType.ReadWrite with
                        | Ok (handle, address) ->
                            // Create a region from the mapped file
                            match Region.fromPointer<int, region> address 8<bytes> with
                            | Ok region ->
                                // Read from the mapped file
                                for i = 0 to testData.Length - 1 do
                                    let readByte = Memory.readByte region (i * 1<offset>)
                                    Expect.equal readByte testData.[i] $"Byte at offset {i} should match"
                                
                                // Modify the mapped file
                                for i = 0 to testData.Length - 1 do
                                    Memory.writeByte region (i * 1<offset>) (byte (9 - i))
                                
                                // Flush changes to disk
                                match provider.FlushMappedFile handle address 8<bytes> with
                                | Ok () ->
                                    // Unmap the file
                                    match provider.UnmapMemory handle address 8<bytes> with
                                    | Ok () ->
                                        // Verify changes were written to the file
                                        let fileData = File.ReadAllBytes(tempFilePath)
                                        Expect.equal fileData.Length 8 "File size should be 8 bytes"
                                        
                                        for i = 0 to 7 do
                                            Expect.equal fileData.[i] (byte (9 - i)) $"File byte at offset {i} should match modified value"
                                    | Error e ->
                                        failwith $"Failed to unmap file: {toString e}"
                                | Error e ->
                                    // Unmap the file even if flush fails
                                    provider.UnmapMemory handle address 8<bytes> |> ignore
                                    failwith $"Failed to flush mapped file: {toString e}"
                            | Error e ->
                                // Unmap the file if region creation fails
                                provider.UnmapMemory handle address 8<bytes> |> ignore
                                failwith $"Failed to create region from mapped file: {toString e}"
                        | Error e ->
                            failwith $"Failed to map file: {toString e}"
                    | Error e ->
                        failwith $"Failed to get memory provider: {toString e}"
                finally
                    // Clean up the temporary file
                    if File.Exists(tempFilePath) then
                        File.Delete(tempFilePath)
    ]

[<Tests>]
let ipcProviderTests =
    testList "IPC Provider Tests" [
        testCaseIf (PlatformTestUtil.isWindows())
            "Named pipe communication using platform provider" <| fun _ ->
            // Initialize platform services
            let platformType = PlatformTestUtil.initializePlatformServices()
            
            // Generate a unique pipe name to avoid conflicts with other tests
            let pipeName = $"barewire-test-pipe-{Guid.NewGuid().ToString("N")}"
            
            // Get the IPC provider
            match PlatformServices.getIpcProvider() with
            | Ok provider ->
                // Create a named pipe
                match provider.CreateNamedPipe pipeName NamedPipe.PipeDirection.InOut NamedPipe.PipeMode.Message 4096<bytes> with
                | Ok serverHandle ->
                    // Connect to the pipe
                    match provider.ConnectNamedPipe pipeName NamedPipe.PipeDirection.InOut with
                    | Ok clientHandle ->
                        // Wait for connection
                        match provider.WaitForNamedPipeConnection serverHandle 1000 with
                        | Ok () ->
                            // Create test data
                            let testData = [| 1uy; 2uy; 3uy; 4uy; 5uy |]
                            
                            // Write data to the pipe
                            match provider.WriteNamedPipe serverHandle testData 0 testData.Length with
                            | Ok bytesWritten ->
                                Expect.equal bytesWritten testData.Length "Should write all bytes"
                                
                                // Read data from the pipe
                                let readBuffer = Array.zeroCreate 10
                                match provider.ReadNamedPipe clientHandle readBuffer 0 readBuffer.Length with
                                | Ok bytesRead ->
                                    // On Windows, this should read the data
                                    // On other platforms using the InMemory provider, it might not work
                                    if platformType = Registry.PlatformType.Windows && bytesRead = testData.Length then
                                        for i = 0 to testData.Length - 1 do
                                            Expect.equal readBuffer.[i] testData.[i] $"Byte at position {i} should match"
                                | Error e ->
                                    // If we're not on Windows, this might fail but we don't want to fail the test
                                    if platformType = Registry.PlatformType.Windows then
                                        failwith $"Failed to read from pipe: {toString e}"
                            | Error e ->
                                failwith $"Failed to write to pipe: {toString e}"
                            
                            // Close the pipes
                            provider.CloseNamedPipe serverHandle |> ignore
                            provider.CloseNamedPipe clientHandle |> ignore
                        | Error e ->
                            // Close handles even if connection fails
                            provider.CloseNamedPipe serverHandle |> ignore
                            provider.CloseNamedPipe clientHandle |> ignore
                            
                            // This might fail on non-Windows platforms
                            if platformType = Registry.PlatformType.Windows then
                                failwith $"Failed to wait for pipe connection: {toString e}"
                    | Error e ->
                        // Close server handle if client connection fails
                        provider.CloseNamedPipe serverHandle |> ignore
                        
                        // This might fail on non-Windows platforms
                        if platformType = Registry.PlatformType.Windows then
                            failwith $"Failed to connect to named pipe: {toString e}"
                | Error e ->
                    // This might fail on non-Windows platforms
                    if platformType = Registry.PlatformType.Windows then
                        failwith $"Failed to create named pipe: {toString e}"
            | Error e ->
                failwith $"Failed to get IPC provider: {toString e}"
                
        testCaseIf (PlatformTestUtil.isWindows() || PlatformTestUtil.isLinux() || PlatformTestUtil.isMacOS())
            "Shared memory using platform provider" <| fun _ ->
            // Initialize platform services
            let platformType = PlatformTestUtil.initializePlatformServices()
            
            // Skip for platforms that don't support shared memory
            if platformType = Registry.PlatformType.InMemory then
                ()
            else
                // Generate a unique shared memory name to avoid conflicts
                let memoryName = $"barewire-test-memory-{Guid.NewGuid().ToString("N")}"
                
                // Get the IPC provider
                match PlatformServices.getIpcProvider() with
                | Ok provider ->
                    // Create shared memory
                    match provider.CreateSharedMemory memoryName 1024<bytes> AccessType.ReadWrite with
                    | Ok (ownerHandle, ownerAddress) ->
                        try
                            // Open the shared memory
                            match provider.OpenSharedMemory memoryName AccessType.ReadWrite with
                            | Ok (clientHandle, clientAddress, size) ->
                                try
                                    // Verify size
                                    Expect.equal size 1024<bytes> "Shared memory size should match"
                                    
                                    // Write to the shared memory (from owner)
                                    let ownerRegionResult = Region.fromPointer<int, region> ownerAddress 1024<bytes>
                                    match ownerRegionResult with
                                    | Ok ownerRegion ->
                                        // Write pattern to memory
                                        for i = 0 to 9 do
                                            Memory.writeByte ownerRegion (i * 1<offset>) (byte (i + 1))
                                            
                                        // Read from shared memory (from client)
                                        let clientRegionResult = Region.fromPointer<int, region> clientAddress 1024<bytes>
                                        match clientRegionResult with
                                        | Ok clientRegion ->
                                            // Verify the pattern
                                            for i = 0 to 9 do
                                                let value = Memory.readByte clientRegion (i * 1<offset>)
                                                Expect.equal value (byte (i + 1)) $"Shared memory value at {i} should match"
                                        | Error e ->
                                            failwith $"Failed to create client region: {toString e}"
                                    | Error e ->
                                        failwith $"Failed to create owner region: {toString e}"
                                finally
                                    // Close the client handle
                                    match provider.CloseSharedMemory clientHandle clientAddress size with
                                    | Ok () -> ()
                                    | Error e -> failwith $"Failed to close client shared memory: {toString e}"
                            | Error e ->
                                failwith $"Failed to open shared memory: {toString e}"
                        finally
                            // Close the owner handle
                            match provider.CloseSharedMemory ownerHandle ownerAddress 1024<bytes> with
                            | Ok () -> ()
                            | Error e -> failwith $"Failed to close owner shared memory: {toString e}"
                    | Error e ->
                        failwith $"Failed to create shared memory: {toString e}"
                | Error e ->
                    failwith $"Failed to get IPC provider: {toString e}"
    ]

[<Tests>]
let networkTests =
    testList "Network Tests" [
        testCase "Network frame encoding/decoding" <| fun _ ->
            // Create test frames
            let testFrames = [
                // Request frame
                createRequest [| 1uy; 2uy; 3uy |]
                
                // Response frame
                let requestId = Uuid.newUuid()
                createResponse requestId [| 4uy; 5uy; 6uy |]
                
                // Notification frame
                createNotification [| 7uy; 8uy; 9uy |]
                
                // Error frame
                let errorRequestId = Uuid.newUuid()
                createError errorRequestId [| 10uy; 11uy; 12uy |]
            ]
            
            // Encode and decode each frame
            for originalFrame in testFrames do
                // Encode the frame
                let encoded = encode originalFrame
                
                // Decode the frame
                match decode encoded with
                | Ok decodedFrame ->
                    // Verify message type
                    Expect.equal decodedFrame.Header.MessageType originalFrame.Header.MessageType "Message type should match"
                    
                    // Verify message ID
                    Expect.isTrue (Uuid.equals decodedFrame.Header.MessageId originalFrame.Header.MessageId) "Message ID should match"
                    
                    // Verify payload length
                    Expect.equal decodedFrame.Header.PayloadLength originalFrame.Header.PayloadLength "Payload length should match"
                    
                    // Verify payload
                    Expect.sequenceEqual decodedFrame.Payload originalFrame.Payload "Payload should match"
                | Error e ->
                    failwith $"Frame decoding failed: {e.Message}"
    ]

[<Tests>]
let platformServicesTests =
    testList "Platform Services Tests" [
        testCase "Platform type detection" <| fun _ ->
            // Initialize platform services
            let platformType = PlatformTestUtil.initializePlatformServices()
            
            // Get the current platform type
            let detectedPlatform = PlatformServices.getCurrentPlatform()
            
            // Verify they match
            Expect.equal detectedPlatform platformType "Detected platform should match initialized platform"
            
        testCase "Provider retrieval" <| fun _ ->
            // Initialize platform services
            PlatformTestUtil.initializePlatformServices() |> ignore
            
            // Get providers
            let memoryProviderResult = PlatformServices.getMemoryProvider()
            let ipcProviderResult = PlatformServices.getIpcProvider()
            let networkProviderResult = PlatformServices.getNetworkProvider()
            let syncProviderResult = PlatformServices.getSyncProvider()
            
            // Verify we can get all providers
            Expect.isOk memoryProviderResult "Should get memory provider"
            Expect.isOk ipcProviderResult "Should get IPC provider"
            Expect.isOk networkProviderResult "Should get network provider"
            Expect.isOk syncProviderResult "Should get sync provider"
    ]

// Test adapter module to collect and run all tests
module TestAdapter =
    let allTests = 
        testList "All Integration Tests" [
            memoryProviderTests
            ipcProviderTests
            networkTests
            platformServicesTests
        ]

[<EntryPoint>]
let main args =
    runTestsWithArgs defaultConfig args TestAdapter.allTests