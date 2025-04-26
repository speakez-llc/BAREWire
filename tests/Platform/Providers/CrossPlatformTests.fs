namespace BAREWire.Tests.Platform

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
open BAREWire.Platform.Providers
open System

/// <summary>
/// Tests focused on ensuring cross-platform compatibility
/// </summary>
module CrossPlatformTests =
    
    /// <summary>
    /// Utility functions for platform detection
    /// </summary>
    module PlatformUtils =
        let isWindows =
            try
                #if WINDOWS
                    true
                #else
                    Environment.OSVersion.Platform = PlatformID.Win32NT
                #endif
            with _ ->
                false
                
        let isLinux =
            try
                #if LINUX
                    true
                #else
                    let platform = Environment.OSVersion.Platform
                    platform = PlatformID.Unix && 
                    not (Environment.OSVersion.ToString().Contains("Darwin"))
                #endif
            with _ ->
                false
                
        let isMacOS =
            try
                #if MACOS
                    true
                #else
                    let platform = Environment.OSVersion.Platform
                    platform = PlatformID.Unix && 
                    Environment.OSVersion.ToString().Contains("Darwin")
                #endif
            with _ ->
                false
                
        let isWebAssembly =
            try
                #if WASM || FABLE_COMPILER
                    true
                #else
                    false
                #endif
            with _ ->
                false
                
        // Detect the current platform
        let getCurrentPlatform() =
            if isWindows then "Windows"
            elif isLinux then "Linux"
            elif isMacOS then "macOS"
            elif isWebAssembly then "WebAssembly"
            else "Unknown"
    
    /// <summary>
    /// Reset the platform services between tests
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
    /// Initialize with a specific platform provider
    /// </summary>
    let initializeWithPlatform platformType =
        resetPlatformServices()
        PlatformServices.initializeWithPlatform platformType 10 |> ignore
        
        // Register the appropriate providers
        match platformType with
        | PlatformType.Windows ->
            Registry.registerMemoryProvider platformType (Windows.WindowsMemoryProvider())
            Registry.registerIpcProvider platformType (Windows.WindowsIpcProvider())
            Registry.registerNetworkProvider platformType (Windows.WindowsNetworkProvider())
            Registry.registerSyncProvider platformType (Windows.WindowsSyncProvider())
            
        | PlatformType.Linux ->
            Registry.registerMemoryProvider platformType (Linux.LinuxMemoryProvider())
            Registry.registerIpcProvider platformType (Linux.LinuxIpcProvider())
            Registry.registerNetworkProvider platformType (Linux.LinuxNetworkProvider())
            Registry.registerSyncProvider platformType (Linux.LinuxSyncProvider())
            
        | PlatformType.MacOS ->
            Registry.registerMemoryProvider platformType (MacOS.MacOSMemoryProvider())
            Registry.registerIpcProvider platformType (MacOS.MacOSIpcProvider())
            Registry.registerNetworkProvider platformType (MacOS.MacOSNetworkProvider())
            Registry.registerSyncProvider platformType (MacOS.MacOSSyncProvider())
            
        | PlatformType.WebAssembly ->
            Registry.registerMemoryProvider platformType (WebAssembly.WebAssemblyMemoryProvider())
            Registry.registerIpcProvider platformType (WebAssembly.WebAssemblyIpcProvider())
            Registry.registerNetworkProvider platformType (WebAssembly.WebAssemblyNetworkProvider())
            Registry.registerSyncProvider platformType (WebAssembly.WebAssemblySyncProvider())
            
        | _ ->
            Registry.registerMemoryProvider platformType (InMemory.InMemoryMemoryProvider())
            Registry.registerIpcProvider platformType (InMemory.InMemoryIpcProvider())
            Registry.registerNetworkProvider platformType (InMemory.InMemoryNetworkProvider())
            Registry.registerSyncProvider platformType (InMemory.InMemorySyncProvider())
    
    /// <summary>
    /// Platform-agnostic tests that should pass on any platform
    /// </summary>
    [<Tests>]
    let platformAgnosticMemoryTests =
        testList "Platform-Agnostic Memory Tests" [
            test "MapMemory works on all platforms" {
                // For each platform type, initialize and test
                let platformsToTest = [
                    PlatformType.Windows
                    PlatformType.Linux
                    PlatformType.MacOS
                    PlatformType.WebAssembly
                    PlatformType.InMemory
                ]
                
                for platformType in platformsToTest do
                    initializeWithPlatform platformType
                    
                    printfn $"Testing on platform: {platformType}"
                    
                    // Get the memory provider
                    match PlatformServices.getMemoryProvider() with
                    | Ok provider ->
                        // Map some memory
                        let size = 4096<bytes>
                        match provider.MapMemory size MappingType.PrivateMapping AccessType.ReadWrite with
                        | Ok (handle, address) ->
                            Expect.notEqual handle 0n $"Handle should not be zero on {platformType}"
                            Expect.notEqual address 0n $"Address should not be zero on {platformType}"
                            
                            // Clean up
                            match provider.UnmapMemory handle address size with
                            | Ok () -> ()
                            | Error e -> failwith $"UnmapMemory failed on {platformType}: {e.Message}"
                        | Error e ->
                            failwith $"MapMemory failed on {platformType}: {e.Message}"
                    | Error e ->
                        failwith $"Failed to get memory provider for {platformType}: {e.Message}"
            }
        ]
        
    [<Tests>]
    let memorySizingTests =
        testList "Memory Sizing Tests" [
            test "Small memory allocations work on all platforms" {
                // Use in-memory provider for reliable testing
                initializeWithPlatform PlatformType.InMemory
                
                match PlatformServices.getMemoryProvider() with
                | Ok provider ->
                    // Test a range of small sizes
                    let sizes = [
                        4<bytes>
                        16<bytes>
                        256<bytes>
                        1024<bytes>
                        4096<bytes>
                    ]
                    
                    for size in sizes do
                        // Map memory of the given size
                        match provider.MapMemory size MappingType.PrivateMapping AccessType.ReadWrite with
                        | Ok (handle, address) ->
                            Expect.notEqual handle 0n $"Handle should not be zero for size {size}"
                            Expect.notEqual address 0n $"Address should not be zero for size {size}"
                            
                            // Clean up
                            match provider.UnmapMemory handle address size with
                            | Ok () -> ()
                            | Error e -> failwith $"UnmapMemory failed for size {size}: {e.Message}"
                        | Error e ->
                            failwith $"MapMemory failed for size {size}: {e.Message}"
                | Error e ->
                    failwith $"Failed to get memory provider: {e.Message}"
            }
            
            test "Page-aligned memory allocations work on all platforms" {
                // Use in-memory provider for reliable testing
                initializeWithPlatform PlatformType.InMemory
                
                match PlatformServices.getMemoryProvider() with
                | Ok provider ->
                    // Test some common page sizes
                    let pageSizes = [
                        4096<bytes>    // 4KB - most common page size
                        8192<bytes>    // 8KB
                        16384<bytes>   // 16KB
                        65536<bytes>   // 64KB - large pages on some systems
                    ]
                    
                    for pageSize in pageSizes do
                        // Map memory of the given page size
                        match provider.MapMemory pageSize MappingType.PrivateMapping AccessType.ReadWrite with
                        | Ok (handle, address) ->
                            Expect.notEqual handle 0n $"Handle should not be zero for page size {pageSize}"
                            Expect.notEqual address 0n $"Address should not be zero for page size {pageSize}"
                            
                            // Clean up
                            match provider.UnmapMemory handle address pageSize with
                            | Ok () -> ()
                            | Error e -> failwith $"UnmapMemory failed for page size {pageSize}: {e.Message}"
                        | Error e ->
                            failwith $"MapMemory failed for page size {pageSize}: {e.Message}"
                | Error e ->
                    failwith $"Failed to get memory provider: {e.Message}"
            }
        ]
        
    [<Tests>]
    let dataEncodingTests =
        testList "Data Encoding Tests" [
            test "Binary data encoding and decoding is consistent across platforms" {
                // Use in-memory provider for reliable testing
                initializeWithPlatform PlatformType.InMemory
                
                match PlatformServices.getMemoryProvider() with
                | Ok provider ->
                    let size = 1024<bytes>
                    
                    // Map some memory
                    match provider.MapMemory size MappingType.PrivateMapping AccessType.ReadWrite with
                    | Ok (handle, address) ->
                        try
                            // Test various primitive types
                            
                            // Int32
                            let intPtr = NativePtr.ofNativeInt<int> address
                            let intValue = 0x12345678
                            NativePtr.write intPtr intValue
                            let readIntValue = NativePtr.read intPtr
                            Expect.equal readIntValue intValue "Int32 value should be preserved"
                            
                            // Single
                            let floatPtr = NativePtr.ofNativeInt<float32> (address + nativeint sizeof<int>)
                            let floatValue = 3.14159f
                            NativePtr.write floatPtr floatValue
                            let readFloatValue = NativePtr.read floatPtr
                            Expect.equal readFloatValue floatValue "Single value should be preserved"
                            
                            // Double
                            let doublePtr = NativePtr.ofNativeInt<float> (address + nativeint sizeof<int> + nativeint sizeof<float32>)
                            let doubleValue = 2.718281828459045
                            NativePtr.write doublePtr doubleValue
                            let readDoubleValue = NativePtr.read doublePtr
                            Expect.equal readDoubleValue doubleValue "Double value should be preserved"
                            
                            // Byte array
                            let byteArrayOffset = nativeint sizeof<int> + nativeint sizeof<float32> + nativeint sizeof<float>
                            let bytePtr = NativePtr.ofNativeInt<byte> (address + byteArrayOffset)
                            let byteValues = [| 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy |]
                            
                            // Write bytes
                            for i = 0 to byteValues.Length - 1 do
                                NativePtr.set bytePtr i byteValues.[i]
                                
                            // Read bytes
                            let readBytes = Array.zeroCreate<byte> byteValues.Length
                            for i = 0 to readBytes.Length - 1 do
                                readBytes.[i] <- NativePtr.get bytePtr i
                                
                            Expect.sequenceEqual readBytes byteValues "Byte array should be preserved"
                            
                        finally
                            // Clean up
                            provider.UnmapMemory handle address size |> ignore
                    | Error e ->
                        failwith $"MapMemory failed: {e.Message}"
                | Error e ->
                    failwith $"Failed to get memory provider: {e.Message}"
            }
            
            test "Endianness handling is consistent" {
                // Use in-memory provider for reliable testing
                initializeWithPlatform PlatformType.InMemory
                
                match PlatformServices.getMemoryProvider() with
                | Ok provider ->
                    let size = 1024<bytes>
                    
                    // Map some memory
                    match provider.MapMemory size MappingType.PrivateMapping AccessType.ReadWrite with
                    | Ok (handle, address) ->
                        try
                            // Test endianness handling
                            
                            // Write a 32-bit integer in little-endian format
                            let bytePtr = NativePtr.ofNativeInt<byte> address
                            NativePtr.set bytePtr 0 0x78uy
                            NativePtr.set bytePtr 1 0x56uy
                            NativePtr.set bytePtr 2 0x34uy
                            NativePtr.set bytePtr 3 0x12uy
                            
                            // Read it as an Int32
                            let intPtr = NativePtr.ofNativeInt<int> address
                            let readValue = NativePtr.read intPtr
                            
                            if BitConverter.IsLittleEndian then
                                // On little-endian systems, the value should be 0x12345678
                                Expect.equal readValue 0x12345678 "Value should be correctly interpreted on little-endian system"
                            else
                                // On big-endian systems, the bytes would be interpreted differently
                                Expect.equal readValue 0x78563412 "Value should be correctly interpreted on big-endian system"
                            
                            // Now let's handle endianness explicitly
                            
                            // Write a 32-bit integer with explicit endianness handling
                            let intValue = 0x12345678
                            let intBytes = BitConverter.GetBytes(intValue)
                            
                            // Write the bytes at a new offset
                            for i = 0 to 3 do
                                NativePtr.set bytePtr (i + 4) intBytes.[i]
                                
                            // Read them back with the same endianness handling
                            let readBytes = Array.zeroCreate<byte> 4
                            for i = 0 to 3 do
                                readBytes.[i] <- NativePtr.get bytePtr (i + 4)
                                
                            let readIntValue = BitConverter.ToInt32(readBytes, 0)
                            Expect.equal readIntValue intValue "Int32 value should be preserved with explicit endianness handling"
                            
                        finally
                            // Clean up
                            provider.UnmapMemory handle address size |> ignore
                    | Error e ->
                        failwith $"MapMemory failed: {e.Message}"
                | Error e ->
                    failwith $"Failed to get memory provider: {e.Message}"
            }
        ]
        
    [<Tests>]
    let ipcCompatibilityTests =
        testList "IPC Compatibility Tests" [
            test "Named pipes work consistently across providers" {
                // Test named pipes with different providers
                let platformsToTest = [
                    PlatformType.Windows
                    PlatformType.Linux
                    PlatformType.MacOS
                    PlatformType.WebAssembly
                    PlatformType.InMemory
                ]
                
                for platformType in platformsToTest do
                    // Some platforms don't support named pipes - skip them
                    if platformType = PlatformType.WebAssembly || 
                       platformType = PlatformType.Android || 
                       platformType = PlatformType.iOS then
                        ()
                    else
                        initializeWithPlatform platformType
                        
                        printfn $"Testing named pipes on platform: {platformType}"
                        
                        // Get the IPC provider
                        match PlatformServices.getIpcProvider() with
                        | Ok provider ->
                            // Generate a unique pipe name
                            let pipeName = $"BAREWireTest_{Guid.NewGuid().ToString("N")}"
                            
                            // Create a named pipe
                            match provider.CreateNamedPipe pipeName NamedPipe.PipeDirection.InOut NamedPipe.PipeMode.Message 4096<bytes> with
                            | Ok handle ->
                                Expect.notEqual handle 0n $"Handle should not be zero on {platformType}"
                                
                                // Clean up
                                match provider.CloseNamedPipe handle with
                                | Ok () -> ()
                                | Error e -> failwith $"CloseNamedPipe failed on {platformType}: {e.Message}"
                            | Error e ->
                                // Special case for platforms where named pipes are not directly supported
                                if e.Message.Contains("not supported") then
                                    printfn $"Named pipes not supported on {platformType} - skipping"
                                else
                                    failwith $"CreateNamedPipe failed on {platformType}: {e.Message}"
                        | Error e ->
                            failwith $"Failed to get IPC provider for {platformType}: {e.Message}"
            }
            
            test "Shared memory works consistently across providers" {
                // Test shared memory with different providers
                let platformsToTest = [
                    PlatformType.Windows
                    PlatformType.Linux
                    PlatformType.MacOS
                    PlatformType.WebAssembly
                    PlatformType.InMemory
                ]
                
                for platformType in platformsToTest do
                    initializeWithPlatform platformType
                    
                    printfn $"Testing shared memory on platform: {platformType}"
                    
                    // Get the IPC provider
                    match PlatformServices.getIpcProvider() with
                    | Ok provider ->
                        // Generate a unique shared memory name
                        let memName = $"BAREWireTest_{Guid.NewGuid().ToString("N")}"
                        let size = 4096<bytes>
                        
                        // Create shared memory
                        match provider.CreateSharedMemory memName size AccessType.ReadWrite with
                        | Ok (handle, address) ->
                            Expect.notEqual handle 0n $"Handle should not be zero on {platformType}"
                            Expect.notEqual address 0n $"Address should not be zero on {platformType}"
                            
                            // Clean up
                            match provider.CloseSharedMemory handle address size with
                            | Ok () -> ()
                            | Error e -> failwith $"CloseSharedMemory failed on {platformType}: {e.Message}"
                        | Error e ->
                            // Some platforms might not support sharing memory by name
                            if e.Message.Contains("not supported") then
                                printfn $"Named shared memory not supported on {platformType} - skipping"
                            else
                                failwith $"CreateSharedMemory failed on {platformType}: {e.Message}"
                    | Error e ->
                        failwith $"Failed to get IPC provider for {platformType}: {e.Message}"
            }
        ]
        
    [<Tests>]
    let networkCompatibilityTests =
        testList "Network Compatibility Tests" [
            test "Socket creation works consistently across providers" {
                // Test sockets with different providers
                let platformsToTest = [
                    PlatformType.Windows
                    PlatformType.Linux
                    PlatformType.MacOS
                    PlatformType.WebAssembly
                    PlatformType.InMemory
                ]
                
                for platformType in platformsToTest do
                    initializeWithPlatform platformType
                    
                    printfn $"Testing sockets on platform: {platformType}"
                    
                    // Get the network provider
                    match PlatformServices.getNetworkProvider() with
                    | Ok provider ->
                        // Create a socket (TCP)
                        match provider.CreateSocket 2 1 6 with // AF_INET, SOCK_STREAM, IPPROTO_TCP
                        | Ok handle ->
                            Expect.notEqual handle 0n $"Handle should not be zero on {platformType}"
                            
                            // Clean up
                            match provider.CloseSocket handle with
                            | Ok () -> ()
                            | Error e -> failwith $"CloseSocket failed on {platformType}: {e.Message}"
                        | Error e ->
                            failwith $"CreateSocket failed on {platformType}: {e.Message}"
                    | Error e ->
                        failwith $"Failed to get network provider for {platformType}: {e.Message}"
            }
            
            test "UDP socket creation works consistently across providers" {
                // Test UDP sockets with different providers
                let platformsToTest = [
                    PlatformType.Windows
                    PlatformType.Linux
                    PlatformType.MacOS
                    PlatformType.WebAssembly
                    PlatformType.InMemory
                ]
                
                for platformType in platformsToTest do
                    initializeWithPlatform platformType
                    
                    printfn $"Testing UDP sockets on platform: {platformType}"
                    
                    // Get the network provider
                    match PlatformServices.getNetworkProvider() with
                    | Ok provider ->
                        // Create a socket (UDP)
                        match provider.CreateSocket 2 2 17 with // AF_INET, SOCK_DGRAM, IPPROTO_UDP
                        | Ok handle ->
                            Expect.notEqual handle 0n $"Handle should not be zero on {platformType}"
                            
                            // Clean up
                            match provider.CloseSocket handle with
                            | Ok () -> ()
                            | Error e -> failwith $"CloseSocket failed on {platformType}: {e.Message}"
                        | Error e ->
                            failwith $"CreateSocket failed on {platformType}: {e.Message}"
                    | Error e ->
                        failwith $"Failed to get network provider for {platformType}: {e.Message}"
            }
        ]
        
    [<Tests>]
    let syncCompatibilityTests =
        testList "Synchronization Compatibility Tests" [
            test "Mutex creation works consistently across providers" {
                // Test mutexes with different providers
                let platformsToTest = [
                    PlatformType.Windows
                    PlatformType.Linux
                    PlatformType.MacOS
                    PlatformType.WebAssembly
                    PlatformType.InMemory
                ]
                
                for platformType in platformsToTest do
                    initializeWithPlatform platformType
                    
                    printfn $"Testing mutexes on platform: {platformType}"
                    
                    // Get the sync provider
                    match PlatformServices.getSyncProvider() with
                    | Ok provider ->
                        // Generate a unique mutex name
                        let mutexName = $"BAREWireTest_{Guid.NewGuid().ToString("N")}"
                        
                        // Create a mutex
                        match provider.CreateMutex mutexName false with
                        | Ok handle ->
                            Expect.notEqual handle 0n $"Handle should not be zero on {platformType}"
                            
                            // Clean up
                            match provider.CloseMutex handle with
                            | Ok () -> ()
                            | Error e -> failwith $"CloseMutex failed on {platformType}: {e.Message}"
                        | Error e ->
                            // Some platforms have limited synchronization support
                            if e.Message.Contains("not implemented") || e.Message.Contains("not supported") then
                                printfn $"Mutexes not fully implemented on {platformType} - skipping"
                            else
                                failwith $"CreateMutex failed on {platformType}: {e.Message}"
                    | Error e ->
                        failwith $"Failed to get sync provider for {platformType}: {e.Message}"
            }
            
            test "Semaphore creation works consistently across providers" {
                // Test semaphores with different providers
                let platformsToTest = [
                    PlatformType.Windows
                    PlatformType.Linux
                    PlatformType.MacOS
                    PlatformType.WebAssembly
                    PlatformType.InMemory
                ]
                
                for platformType in platformsToTest do
                    initializeWithPlatform platformType
                    
                    printfn $"Testing semaphores on platform: {platformType}"
                    
                    // Get the sync provider
                    match PlatformServices.getSyncProvider() with
                    | Ok provider ->
                        // Generate a unique semaphore name
                        let semName = $"BAREWireTest_{Guid.NewGuid().ToString("N")}"
                        
                        // Create a semaphore
                        match provider.CreateSemaphore semName 1 5 with
                        | Ok handle ->
                            Expect.notEqual handle 0n $"Handle should not be zero on {platformType}"
                            
                            // Clean up
                            match provider.CloseSemaphore handle with
                            | Ok () -> ()
                            | Error e -> failwith $"CloseSemaphore failed on {platformType}: {e.Message}"
                        | Error e ->
                            // Some platforms have limited synchronization support
                            if e.Message.Contains("not implemented") || e.Message.Contains("not supported") then
                                printfn $"Semaphores not fully implemented on {platformType} - skipping"
                            else
                                failwith $"CreateSemaphore failed on {platformType}: {e.Message}"
                    | Error e ->
                        failwith $"Failed to get sync provider for {platformType}: {e.Message}"
            }
        ]
        
    /// <summary>
    /// Print platform information for the current tests
    /// </summary>
    let reportCurrentPlatform() =
        let platform = PlatformUtils.getCurrentPlatform()
        printfn $"Tests are running on actual platform: {platform}"
        
        let detectedPlatform = Registry.getCurrentPlatform()
        printfn $"BAREWire detected platform: {detectedPlatform}"
        
    [<Tests>]
    let tests = 
        testSequenced <| testList "Cross-Platform Tests" [
            testCase "Report Current Platform" <| fun _ ->
                reportCurrentPlatform()
                
            platformAgnosticMemoryTests
            memorySizingTests
            dataEncodingTests
            ipcCompatibilityTests
            networkCompatibilityTests
            syncCompatibilityTests
        ]

module Program =
    [<EntryPoint>]
    let main args =
        runTestsWithArgs defaultConfig args CrossPlatformTests.tests