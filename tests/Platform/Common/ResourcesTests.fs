namespace BAREWire.Tests.Platform.Common

open Expecto
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Platform.Common.Resource
open System

module ResourceTests =
    /// Simple resource type for testing
    type TestResource = {
        Id: int
        Value: string
    }
    
    /// Track disposals for testing
    let mutable disposalCount = 0
    
    let resetDisposalCount() =
        disposalCount <- 0
    
    /// Create a test resource with disposal tracking
    let createTestResource id value =
        create { Id = id; Value = value } (fun () -> disposalCount <- disposalCount + 1)
    
    [<Tests>]
    let resourceCreationTests =
        testList "Resource Creation Tests" [
            test "Create resource with value and disposal function" {
                resetDisposalCount()
                
                let resource = createTestResource 1 "test"
                
                Expect.equal resource.Value.Id 1 "Resource ID should be 1"
                Expect.equal resource.Value.Value "test" "Resource value should be 'test'"
                
                // Explicit disposal
                resource.Dispose()
                
                Expect.equal disposalCount 1 "Disposal function should have been called once"
            }
            
            test "Empty resource doesn't call disposal" {
                resetDisposalCount()
                
                let resource = empty { Id = 2; Value = "empty" }
                
                Expect.equal resource.Value.Id 2 "Resource ID should be 2"
                
                // Explicit disposal of empty resource
                resource.Dispose()
                
                Expect.equal disposalCount 0 "Empty resource should not call disposal function"
            }
        ]
        
    [<Tests>]
    let resourceUsageTests =
        testList "Resource Usage Tests" [
            test "Using a resource calls disposal afterwards" {
                resetDisposalCount()
                
                let resource = createTestResource 3 "use"
                
                // Use the resource
                let result = use' resource (fun r -> r.Id * 2)
                
                Expect.equal result 6 "Result should be 3 * 2 = 6"
                Expect.equal disposalCount 1 "Resource should be disposed after use"
            }
            
            test "Using a resource disposes even when exception occurs" {
                resetDisposalCount()
                
                let resource = createTestResource 4 "exception"
                
                // Use the resource with a function that throws
                try
                    use' resource (fun _ -> failwith "Deliberate exception") |> ignore
                with _ ->
                    ()
                    
                Expect.equal disposalCount 1 "Resource should be disposed even after exception"
            }
        ]
        
    [<Tests>]
    let resourceTransformationTests =
        testList "Resource Transformation Tests" [
            test "Map transforms resource value but preserves disposal" {
                resetDisposalCount()
                
                let resource = createTestResource 5 "map"
                
                // Map the resource
                let mapped = map (fun r -> r.Id.ToString()) resource
                
                Expect.equal mapped.Value "5" "Mapped value should be '5'"
                
                // Disposal should propagate
                mapped.Dispose()
                
                Expect.equal disposalCount 1 "Original disposal function should be called"
            }
            
            test "Combine merges two resources with a function" {
                resetDisposalCount()
                
                let resource1 = createTestResource 6 "first"
                let resource2 = createTestResource 7 "second"
                
                // Combine the resources
                let combined = combine (fun r1 r2 -> $"{r1.Value}-{r2.Value}") resource1 resource2
                
                Expect.equal combined.Value "first-second" "Combined value should be 'first-second'"
                
                // Disposal should handle both resources
                combined.Dispose()
                
                Expect.equal disposalCount 2 "Both resources should be disposed"
            }
            
            test "Bind chains resource-producing functions" {
                resetDisposalCount()
                
                let resource = createTestResource 8 "bind"
                
                // Bind to a function that produces another resource
                let bindFn r = 
                    Ok (createTestResource (r.Id * 2) (r.Value + "-bound"))
                
                match bind bindFn resource with
                | Ok boundResource ->
                    Expect.equal boundResource.Value.Id 16 "Bound resource ID should be 8 * 2 = 16"
                    Expect.equal boundResource.Value.Value "bind-bound" "Bound resource value should be 'bind-bound'"
                    
                    // Disposing bound resource should dispose both resources
                    boundResource.Dispose()
                    
                    Expect.equal disposalCount 2 "Both resources should be disposed"
                | Error e ->
                    failwith $"Bind failed: {e.Message}"
            }
        ]
        
    [<Tests>]
    let resourceErrorHandlingTests =
        testList "Resource Error Handling Tests" [
            test "TryCreate handles success case" {
                resetDisposalCount()
                
                match tryCreate (fun () -> { Id = 9; Value = "tryCreate" }) (fun _ -> disposalCount <- disposalCount + 1) with
                | Ok resource ->
                    Expect.equal resource.Value.Id 9 "Resource ID should be 9"
                    
                    resource.Dispose()
                    
                    Expect.equal disposalCount 1 "Resource should be disposed"
                | Error e ->
                    failwith $"TryCreate failed: {e.Message}"
            }
            
            test "TryCreate handles exception case" {
                resetDisposalCount()
                
                match tryCreate (fun () -> failwith "Deliberate exception") (fun _ -> disposalCount <- disposalCount + 1) with
                | Ok _ ->
                    failwith "Should have returned error"
                | Error e ->
                    Expect.isTrue (e.Message.Contains "Failed to create resource") "Error should indicate creation failure"
                    Expect.equal disposalCount 0 "No resource should be created or disposed"
            }
            
            test "MapResult maps success case and disposes resource" {
                resetDisposalCount()
                
                let resource = createTestResource 10 "mapResult"
                
                let result = mapResult (fun r -> Ok (r.Id * 3)) resource
                
                match result with
                | Ok value ->
                    Expect.equal value 30 "Result should be 10 * 3 = 30"
                | Error e ->
                    failwith $"MapResult failed: {e.Message}"
                    
                Expect.equal disposalCount 1 "Resource should be disposed after mapping"
            }
            
            test "MapResult handles error case and still disposes" {
                resetDisposalCount()
                
                let resource = createTestResource 11 "mapResultError"
                
                let result = mapResult (fun _ -> Error (invalidValueError "Deliberate error")) resource
                
                match result with
                | Ok _ ->
                    failwith "Should have returned error"
                | Error e ->
                    Expect.isTrue (e.Message.Contains "Deliberate error") "Error should be propagated"
                    
                Expect.equal disposalCount 1 "Resource should be disposed even after error"
            }
        ]
        
    [<Tests>]
    let resourceCollectionTests =
        testList "Resource Collection Tests" [
            test "CombineArray combines multiple resources" {
                resetDisposalCount()
                
                let resources = [|
                    createTestResource 12 "array1"
                    createTestResource 13 "array2"
                    createTestResource 14 "array3"
                |]
                
                let combined = combineArray resources
                
                Expect.equal combined.Value.Length 3 "Combined array should have 3 elements"
                Expect.equal combined.Value.[0].Id 12 "First element ID should be 12"
                Expect.equal combined.Value.[1].Value "array2" "Second element value should be 'array2'"
                
                // Dispose the combined resource
                combined.Dispose()
                
                Expect.equal disposalCount 3 "All three resources should be disposed"
            }
        ]
        
    [<Tests>]
    let nativeHandleTests =
        testList "Native Handle Tests" [
            test "FromHandle wraps native handle with disposal" {
                resetDisposalCount()
                
                let handle = 0x1234n
                
                let resource = fromHandle handle (fun h -> 
                    Expect.equal h 0x1234n "Handle passed to disposal function should match original"
                    disposalCount <- disposalCount + 1
                )
                
                Expect.equal resource.Value 0x1234n "Resource value should be the handle"
                
                resource.Dispose()
                
                Expect.equal disposalCount 1 "Handle disposal function should be called"
            }
        ]
        
    let tests = 
        testList "Resource Tests" [
            resourceCreationTests
            resourceUsageTests
            resourceTransformationTests
            resourceErrorHandlingTests
            resourceCollectionTests
            nativeHandleTests
        ]

module Program =
    [<EntryPoint>]
    let main args =
        runTestsWithArgs defaultConfig args ResourceTests.tests