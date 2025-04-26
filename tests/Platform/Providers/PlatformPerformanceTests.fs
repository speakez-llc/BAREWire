namespace BAREWire.Tests.Platform

open System
open System.Diagnostics
open System.Collections.Generic
open Expecto
open BAREWire.Core
open BAREWire.Core.Memory
open BAREWire.Memory.Mapping
open BAREWire.IPC
open BAREWire.Platform
open BAREWire.Platform.Common
open BAREWire.Platform.Common.Registry
open BAREWire.Platform.Providers

/// <summary>
/// Performance tests for platform APIs
/// </summary>
module PlatformPerformanceTests =

    /// <summary>
    /// Performance benchmark tool
    /// </summary>
    module Benchmark =
        /// <summary>
        /// Measures the execution time of a function
        /// </summary>
        let measure<'T> (f: unit -> 'T) =
            // Warm up
            f() |> ignore
            
            // Measure
            let sw = Stopwatch.StartNew()
            let result = f()
            sw.Stop()
            
            (result, sw.ElapsedMilliseconds)
        
        /// <summary>
        /// Runs a function multiple times and returns statistics
        /// </summary>
        let benchmark<'T> (iterations: int) (f: unit -> 'T) =
            // Warm up
            f() |> ignore
            
            // Collect samples
            let times = List<int64>()
            
            for i = 1 to iterations do
                let sw = Stopwatch.StartNew()
                f() |> ignore
                sw.Stop()
                times.Add(sw.ElapsedMilliseconds)
            
            // Calculate statistics
            let avg = times |> Seq.average
            let min = times |> Seq.min
            let max = times |> Seq.max
            let median = 
                let sorted = times |> Seq.sort |> Seq.toArray
                if sorted.Length % 2 = 0 then
                    float (sorted.[sorted.Length / 2 - 1] + sorted.[sorted.Length / 2]) / 2.0
                else
                    float sorted.[sorted.Length / 2]
                    
            (avg, min, max, median)
    
    /// <summary>
    /// Resets platform services for testing
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
    let memoryPerformanceTests =
        testList "Memory API Performance Tests" [
            testCase "MapMemory performance" <| fun _ ->
                setup()
                
                // Get the memory provider
                match PlatformServices.getMemoryProvider() with
                | Ok provider ->
                    // Small allocation
                    let smallSize = 4096<bytes>
                    let (smallAvg, smallMin, smallMax, smallMedian) = 
                        Benchmark.benchmark 1000 (fun () -> 
                            let result = provider.MapMemory smallSize MappingType.PrivateMapping AccessType.ReadWrite
                            match result with
                            | Ok (handle, _) -> provider.UnmapMemory handle 0n smallSize |> ignore
                            | Error _ -> ()
                        )
                    
                    // Medium allocation
                    let mediumSize = 1048576<bytes> // 1MB
                    let (mediumAvg, mediumMin, mediumMax, mediumMedian) = 
                        Benchmark.benchmark 100 (fun () -> 
                            let result = provider.MapMemory mediumSize MappingType.PrivateMapping AccessType.ReadWrite
                            match result with
                            | Ok (handle, _) -> provider.UnmapMemory handle 0n mediumSize |> ignore
                            | Error _ -> ()
                        )
                    
                    // Large allocation
                    let largeSize = 10485760<bytes> // 10MB
                    let (largeAvg, largeMin, largeMax, largeMedian) = 
                        Benchmark.benchmark 10 (fun () -> 
                            let result = provider.MapMemory largeSize MappingType.PrivateMapping AccessType.ReadWrite
                            match result with
                            | Ok (handle, _) -> provider.UnmapMemory handle 0n largeSize |> ignore
                            | Error _ -> ()
                        )
                    
                    // Log results
                    printfn "Small allocation (4KB): Avg=%.2fms, Min=%dms, Max=%dms, Median=%.2fms" 
                        smallAvg smallMin smallMax smallMedian
                    printfn "Medium allocation (1MB): Avg=%.2fms, Min=%dms, Max=%dms, Median=%.2fms" 
                        mediumAvg mediumMin mediumMax mediumMedian
                    printfn "Large allocation (10MB): Avg=%.2fms, Min=%dms, Max=%dms, Median=%.2fms" 
                        largeAvg largeMin largeMax largeMedian
                    
                    // Verify that larger allocations take more time
                    Expect.isGreaterThan mediumAvg smallAvg "Medium allocations should be slower than small allocations"
                    Expect.isGreaterThan largeAvg mediumAvg "Large allocations should be slower than medium allocations"
                | Error e ->
                    failwith $"Failed to get memory provider: {e.Message}"
        ]
        
    [<Tests>]
    let ipcPerformanceTests =
        testList "IPC API Performance Tests" [
            testCase "Named pipe throughput" <| fun _ ->
                setup()
                
                // Get the IPC provider
                match PlatformServices.getIpcProvider() with
                | Ok provider ->
                    // Create a pipe for testing
                    match provider.CreateNamedPipe "perfpipe" NamedPipe.PipeDirection.InOut NamedPipe.PipeMode.Message 65536<bytes> with
                    | Ok serverHandle ->
                        match provider.ConnectNamedPipe "perfpipe" NamedPipe.PipeDirection.InOut with
                        | Ok clientHandle ->
                            provider.WaitForNamedPipeConnection serverHandle 0 |> ignore
                            
                            // Small message
                            let smallMsg = Array.init 100 byte
                            let (smallAvg, _, _, _) = 
                                Benchmark.benchmark 1000 (fun () -> 
                                    provider.WriteNamedPipe serverHandle smallMsg 0 smallMsg.Length |> ignore
                                    let buffer = Array.zeroCreate<byte> smallMsg.Length
                                    provider.ReadNamedPipe clientHandle buffer 0 buffer.Length |> ignore
                                )
                            
                            // Medium message
                            let mediumMsg = Array.init 10000 byte
                            let (mediumAvg, _, _, _) = 
                                Benchmark.benchmark 100 (fun () -> 
                                    provider.WriteNamedPipe serverHandle mediumMsg 0 mediumMsg.Length |> ignore
                                    let buffer = Array.zeroCreate<byte> mediumMsg.Length
                                    provider.ReadNamedPipe clientHandle buffer 0 buffer.Length |> ignore
                                )
                            
                            // Large message
                            let largeMsg = Array.init 1000000 byte
                            let (largeAvg, _, _, _) = 
                                Benchmark.benchmark 10 (fun () -> 
                                    provider.WriteNamedPipe serverHandle largeMsg 0 largeMsg.Length |> ignore
                                    let buffer = Array.zeroCreate<byte> largeMsg.Length
                                    provider.ReadNamedPipe clientHandle buffer 0 buffer.Length |> ignore
                                )
                            
                            // Calculate throughput
                            let smallThroughput = float smallMsg.Length / (smallAvg / 1000.0) / 1024.0 / 1024.0 // MB/s
                            let mediumThroughput = float mediumMsg.Length / (mediumAvg / 1000.0) / 1024.0 / 1024.0 // MB/s
                            let largeThroughput = float largeMsg.Length / (largeAvg / 1000.0) / 1024.0 / 1024.0 // MB/s
                            
                            // Log results
                            printfn "Small message (100B): %.2f MB/s" smallThroughput
                            printfn "Medium message (10KB): %.2f MB/s" mediumThroughput
                            printfn "Large message (1MB): %.2f MB/s" largeThroughput
                            
                            // Clean up
                            provider.CloseNamedPipe clientHandle |> ignore
                            provider.CloseNamedPipe serverHandle |> ignore
                        | Error e ->
                            failwith $"ConnectNamedPipe failed: {e.Message}"
                    | Error e ->
                        failwith $"CreateNamedPipe failed: {e.Message}"
                | Error e ->
                    failwith $"Failed to get IPC provider: {e.Message}"
        ]
        
    [<Tests>]
    let registryPerformanceTests =
        testList "Registry Performance Tests" [
            testCase "Provider registry lookup performance" <| fun _ ->
                setup()
                
                // Measure the time to get a provider
                let iterations = 10000
                let (avg, min, max, median) = 
                    Benchmark.benchmark iterations (fun () -> 
                        PlatformServices.getMemoryProvider() |> ignore
                    )
                    
                printfn "Provider lookup: Avg=%.2fms, Min=%dms, Max=%dms, Median=%.2fms" 
                    avg min max median
                    
                // Verify that lookup is reasonably fast
                Expect.isLessThan avg 1.0 "Provider lookup should be fast (less than 1ms)"
        ]
                
    let tests = 
        testSequenced <| testList "Platform Performance Tests" [
            memoryPerformanceTests
            ipcPerformanceTests
            registryPerformanceTests
        ]

module Program =
    [<EntryPoint>]
    let main args =
        runTestsWithArgs defaultConfig args PlatformPerformanceTests.tests