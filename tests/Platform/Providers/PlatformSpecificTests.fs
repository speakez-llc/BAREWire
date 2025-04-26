namespace BAREWire.Tests.Platform.Providers

open Expecto
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Core.Memory
open BAREWire.Platform
open BAREWire.Platform.Common
open BAREWire.Platform.Common.Registry
open System

/// <summary>
/// Tests for specific platform implementations that only run on their target platforms
/// </summary>
module PlatformSpecificTests =

    /// <summary>
    /// Tests for Windows provider that only run on Windows
    /// </summary>
    [<Tests>]
    let windowsProviderTests =
        // These tests will only run on Windows
        testList "Windows Provider Tests" [
            testCase "Windows-specific test" <| fun _ ->
                if Environment.OSVersion.Platform = PlatformID.Win32NT then
                    // Reset to ensure we can initialize with Windows provider
                    let fieldInfo = 
                        typeof<PlatformServices>
                            .GetField("isInitialized", System.Reflection.BindingFlags.Static ||| 
                                                      System.Reflection.BindingFlags.NonPublic)
                    if not (isNull fieldInfo) then
                        fieldInfo.SetValue(null, false)
                    
                    // Initialize with Windows provider
                    PlatformServices.initializeWithPlatform PlatformType.Windows 10 |> ignore
                    
                    // Get the current platform
                    let platform = PlatformServices.getCurrentPlatform()
                    Expect.equal platform PlatformType.Windows "Platform should be Windows"
                else
                    skiptest "Test only runs on Windows"
        ]
        
    /// <summary>
    /// Tests for Linux provider that only run on Linux
    /// </summary>
    [<Tests>]
    let linuxProviderTests =
        // These tests will only run on Linux
        testList "Linux Provider Tests" [
            testCase "Linux-specific test" <| fun _ ->
                if Environment.OSVersion.Platform = PlatformID.Unix then
                    // Reset to ensure we can initialize with Linux provider
                    let fieldInfo = 
                        typeof<PlatformServices>
                            .GetField("isInitialized", System.Reflection.BindingFlags.Static ||| 
                                                      System.Reflection.BindingFlags.NonPublic)
                    if not (isNull fieldInfo) then
                        fieldInfo.SetValue(null, false)
                    
                    // Initialize with Linux provider
                    PlatformServices.initializeWithPlatform PlatformType.Linux 10 |> ignore
                    
                    // Get the current platform
                    let platform = PlatformServices.getCurrentPlatform()
                    Expect.equal platform PlatformType.Linux "Platform should be Linux"
                else
                    skiptest "Test only runs on Linux"
        ]
        
    /// <summary>
    /// Tests for macOS provider that only run on macOS
    /// </summary>
    [<Tests>]
    let macOSProviderTests =
        // These tests will only run on macOS
        testList "macOS Provider Tests" [
            testCase "macOS-specific test" <| fun _ ->
                if Environment.OSVersion.Platform = PlatformID.MacOSX then
                    // Reset to ensure we can initialize with macOS provider
                    let fieldInfo = 
                        typeof<PlatformServices>
                            .GetField("isInitialized", System.Reflection.BindingFlags.Static ||| 
                                                      System.Reflection.BindingFlags.NonPublic)
                    if not (isNull fieldInfo) then
                        fieldInfo.SetValue(null, false)
                    
                    // Initialize with macOS provider
                    PlatformServices.initializeWithPlatform PlatformType.MacOS 10 |> ignore
                    
                    // Get the current platform
                    let platform = PlatformServices.getCurrentPlatform()
                    Expect.equal platform PlatformType.MacOS "Platform should be macOS"
                else
                    skiptest "Test only runs on macOS"
        ]
        
    /// <summary>
    /// Tests that run on all platforms with appropriate conditional logic
    /// </summary>
    [<Tests>]
    let platformAdaptiveTests =
        testList "Platform Adaptive Tests" [
            testCase "Platform detection test" <| fun _ ->
                // This test should run on all platforms
                let platform = 
                    match Environment.OSVersion.Platform with
                    | PlatformID.Win32NT -> PlatformType.Windows
                    | PlatformID.Unix -> 
                        // Here we would need to distinguish between Linux, macOS, Android, etc.
                        // For simplicity, we're just using Unix as Linux
                        PlatformType.Linux
                    | PlatformID.MacOSX -> PlatformType.MacOS
                    | _ -> PlatformType.InMemory
                
                // Reset to ensure we can initialize
                let fieldInfo = 
                    typeof<PlatformServices>
                        .GetField("isInitialized", System.Reflection.BindingFlags.Static ||| 
                                                   System.Reflection.BindingFlags.NonPublic)
                if not (isNull fieldInfo) then
                    fieldInfo.SetValue(null, false)
                
                // Initialize with current platform
                PlatformServices.initialize 10 |> ignore
                
                // Get the detected platform
                let detectedPlatform = PlatformServices.getCurrentPlatform()
                
                // On each platform, check that the correct platform was detected
                match Environment.OSVersion.Platform with
                | PlatformID.Win32NT ->
                    Expect.equal detectedPlatform PlatformType.Windows "Should detect Windows"
                | PlatformID.Unix ->
                    // This is simplified; in reality we'd need more complex detection
                    Expect.isTrue (detectedPlatform = PlatformType.Linux || 
                                  detectedPlatform = PlatformType.Android) 
                                 "Should detect a Unix-based platform"
                | PlatformID.MacOSX ->
                    Expect.equal detectedPlatform PlatformType.MacOS "Should detect macOS"
                | _ ->
                    // For any other platform, we should fall back to InMemory
                    Expect.equal detectedPlatform PlatformType.InMemory "Should fall back to InMemory"
        ]
        
    let tests = 
        testList "Platform Specific Tests" [
            windowsProviderTests
            linuxProviderTests
            macOSProviderTests
            platformAdaptiveTests
        ]

module Program =
    [<EntryPoint>]
    let main args =
        runTestsWithArgs defaultConfig args PlatformSpecificTests.tests