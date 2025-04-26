module Tests.IPC.SharedMemoryTests

open System
open Expecto
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Memory.View
open BAREWire.IPC.SharedMemory
open Tests.IPC.Common

/// Tests for SharedMemory module
[<Tests>]
let sharedMemoryTests =
    testList "SharedMemory Tests" [
        testCase "create should create a new shared memory region" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Create a shared memory region
            let result = create<string> "testRegion" 1024<bytes> testSchema
            
            // Verify the result
            match result with
            | Ok region ->
                Expect.equal region.Name "testRegion" "Region name should match"
                Expect.isGreaterThan region.Handle 0n "Handle should be valid"
                Expect.isGreaterThan region.Region.Length 0<bytes> "Region length should be positive"
            | Error err ->
                failtestf "Failed to create shared memory region: %A" err
                
        testCase "open' should open an existing shared memory region" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Create a shared memory region
            let createResult = create<string> "testRegion" 1024<bytes> testSchema
            
            // Open the region
            match createResult with
            | Ok _ ->
                let openResult = open'<string> "testRegion" testSchema
                match openResult with
                | Ok region ->
                    Expect.equal region.Name "testRegion" "Region name should match"
                    Expect.isGreaterThan region.Handle 0n "Handle should be valid"
                | Error err ->
                    failtestf "Failed to open shared memory region: %A" err
            | Error err ->
                failtestf "Failed to create shared memory region for open test: %A" err
                
        testCase "getView should return a valid memory view" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Create a shared memory region
            let createResult = create<string> "testRegion" 1024<bytes> testSchema
            
            match createResult with
            | Ok region ->
                // Get a view of the region
                let view = getView<string, region> region testSchema
                
                // Verify the view
                Expect.isNotNull view "View should not be null"
                
                // Try to write and read from the view to verify it works
                let path = ["StringValue"]
                let testValue = "Hello, Shared Memory!"
                
                match View.setField<string, string, region> view path testValue with
                | Ok () ->
                    match View.getField<string, string, region> view path with
                    | Ok retrievedValue ->
                        Expect.equal retrievedValue testValue "Retrieved value should match set value"
                    | Error err ->
                        failtestf "Failed to get field from view: %A" err
                | Error err ->
                    failtestf "Failed to set field in view: %A" err
            | Error err ->
                failtestf "Failed to create shared memory region for view test: %A" err
                
        testCase "close should release shared memory resources" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Create a shared memory region
            let createResult = create<string> "testRegion" 1024<bytes> testSchema
            
            match createResult with
            | Ok region ->
                // Close the region
                match close region with
                | Ok () ->
                    // Success - in a real implementation, trying to use the region after
                    // closing would fail, but in our mock it's hard to verify
                    ()
                | Error err ->
                    failtestf "Failed to close shared memory region: %A" err
            | Error err ->
                failtestf "Failed to create shared memory region for close test: %A" err
                
        testCase "lock and unlock should control concurrent access" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Create a shared memory region
            let createResult = create<string> "testRegion" 1024<bytes> testSchema
            
            match createResult with
            | Ok region ->
                // Lock the region
                match lock region with
                | Ok () ->
                    // Region is locked, now unlock it
                    match unlock region with
                    | Ok () ->
                        // Successfully locked and unlocked
                        ()
                    | Error err ->
                        failtestf "Failed to unlock shared memory region: %A" err
                | Error err ->
                    failtestf "Failed to lock shared memory region: %A" err
            | Error err ->
                failtestf "Failed to create shared memory region for lock test: %A" err
                
        testCase "exists should correctly detect region existence" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Check if non-existent region exists
            let existsBeforeCreate = exists "testRegion"
            Expect.isFalse existsBeforeCreate "Region should not exist before creation"
            
            // Create the region
            let createResult = create<string> "testRegion" 1024<bytes> testSchema
            
            match createResult with
            | Ok _ ->
                // Check if region exists after creation
                let existsAfterCreate = exists "testRegion"
                Expect.isTrue existsAfterCreate "Region should exist after creation"
            | Error err ->
                failtestf "Failed to create shared memory region for exists test: %A" err
                
        testCase "map should apply function over shared memory view" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Create a shared memory region
            let createResult = create<string> "testRegion" 1024<bytes> testSchema
            
            match createResult with
            | Ok region ->
                // Initialize some data
                let view = getView<string, region> region testSchema
                let path = ["StringValue"]
                let testValue = "Test Value"
                
                match View.setField<string, string, region> view path testValue with
                | Ok () ->
                    // Map a function over the region
                    let mapResult = map region testSchema (fun v -> 
                        match View.getField<string, string, region> v path with
                        | Ok value -> value.ToUpper()
                        | Error _ -> "ERROR"
                    )
                    
                    match mapResult with
                    | Ok result ->
                        Expect.equal result (testValue.ToUpper()) "Map result should be uppercase value"
                    | Error err ->
                        failtestf "Failed to map function over shared memory: %A" err
                | Error err ->
                    failtestf "Failed to set initial value in view: %A" err
            | Error err ->
                failtestf "Failed to create shared memory region for map test: %A" err
                
        testCase "resize should create new region with updated size" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Create a shared memory region
            let createResult = create<string> "testRegion" 1024<bytes> testSchema
            
            match createResult with
            | Ok region ->
                // Initialize some data
                let view = getView<string, region> region testSchema
                let path = ["StringValue"]
                let testValue = "Test Data For Resize"
                
                match View.setField<string, string, region> view path testValue with
                | Ok () ->
                    // Resize the region to larger size
                    let newSize = 2048<bytes>
                    match resize region newSize with
                    | Ok newRegion ->
                        Expect.equal newRegion.Name region.Name "Region name should be preserved"
                        Expect.equal newRegion.Region.Length newSize "Region size should be updated"
                        
                        // Verify data was preserved
                        let newView = getView<string, region> newRegion testSchema
                        match View.getField<string, string, region> newView path with
                        | Ok value ->
                            Expect.equal value testValue "Data should be preserved after resize"
                        | Error err ->
                            failtestf "Failed to get field after resize: %A" err
                    | Error err ->
                        failtestf "Failed to resize shared memory region: %A" err
                | Error err ->
                    failtestf "Failed to set initial value in view: %A" err
            | Error err ->
                failtestf "Failed to create shared memory region for resize test: %A" err
                
        testCase "getInfo should return region information" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Create a shared memory region with specific size
            let size = 4096<bytes>
            let createResult = create<string> "testRegion" size testSchema
            
            match createResult with
            | Ok _ ->
                // Get info about the region
                match getInfo "testRegion" with
                | Ok (regionSize, creationTime) ->
                    Expect.equal regionSize size "Region size should match"
                    // Creation time is mocked in our implementation and will be current time
                    Expect.isTrue (DateTime.Now.Subtract(creationTime).TotalMinutes < 1.0) 
                        "Creation time should be recent"
                | Error err ->
                    failtestf "Failed to get shared memory region info: %A" err
            | Error err ->
                failtestf "Failed to create shared memory region for getInfo test: %A" err
    ]