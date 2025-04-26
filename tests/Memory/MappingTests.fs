module BAREWire.Tests.Memory.MappingTests

open System
open System.IO
open Expecto
open FSharp.UMX
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Core.Memory
open BAREWire.Memory.Region
open BAREWire.Memory.View
open BAREWire.Memory.Mapping
open BAREWire.Platform.Common.Registry
open BAREWire.Platform.Common.Interfaces
open BAREWire.Platform.Providers.InMemory

/// Measure type for test memory regions
[<Measure>] type testRegion

/// Helper functions for setting up tests
module TestSetup =
    /// Initialize platform services for testing
    let setupPlatformForTesting () =
        // Initialize registries with enough capacity for testing
        initializeRegistries 10
        
        // Set the current platform to InMemory
        setCurrentPlatform PlatformType.InMemory
        
        // Register InMemory providers
        registerMemoryProvider PlatformType.InMemory (new InMemoryMemoryProvider())
        registerIpcProvider PlatformType.InMemory (new InMemoryIpcProvider())
        
        // Verify initialization was successful
        let memoryProviderResult = getCurrentMemoryProvider()
        let ipcProviderResult = getCurrentIpcProvider()
        
        match memoryProviderResult, ipcProviderResult with
        | Ok _, Ok _ -> true
        | _ -> false
            
    /// Create a test schema for memory mapping tests
    let createTestSchema () =
        let types = Map.ofList [
            "TestStruct", 
                Aggregate (Struct [
                    { Name = "id"; Type = Primitive I32 }
                    { Name = "value"; Type = Primitive F32 }
                    { Name = "flag"; Type = Primitive Bool }
                ])
        ]
        
        { 
            Types = types
            Root = "TestStruct" 
        }
        
    /// Create a mock file for testing
    let createTestFile () =
        let tempPath = Path.GetTempFileName()
        let content = Array.init 1024 byte
        File.WriteAllBytes(tempPath, content)
        tempPath
        
    /// Delete a test file
    let deleteTestFile path =
        if File.Exists path then
            File.Delete path

/// Tests for memory mapping operations
[<Tests>]
let mappingTests =
    testList "Memory Mapping Tests" [
        testCase "Initialize platform services" <| fun _ ->
            let success = TestSetup.setupPlatformForTesting()
            Expect.isTrue success "Platform services should initialize successfully"
            
        testCase "mapMemory creates valid memory region" <| fun _ ->
            // Ensure platform is setup
            TestSetup.setupPlatformForTesting() |> ignore
            
            // Arrange
            let size = 1024<bytes>
            
            // Act
            let result = mapMemory<obj, testRegion> size PrivateMapping ReadWrite
            
            // Assert
            match result with
            | Ok (region, handle) ->
                Expect.equal (int region.Length) (int size) "Region length should match requested size"
                Expect.equal handle.Size size "Handle size should match requested size"
                Expect.equal handle.Type PrivateMapping "Handle type should match requested type"
                Expect.equal handle.Access ReadWrite "Handle access should match requested access"
                
                // Clean up
                unmapMemory<obj, testRegion> handle |> ignore
            | Error err ->
                failwith $"Mapping operation failed: {err.Message}"
        
        testCase "unmapMemory releases mapping correctly" <| fun _ ->
            // Ensure platform is setup
            TestSetup.setupPlatformForTesting() |> ignore
            
            // Arrange
            let size = 512<bytes>
            let mapResult = mapMemory<obj, testRegion> size PrivateMapping ReadWrite
            
            // Act & Assert
            match mapResult with
            | Ok (_, handle) ->
                let unmapResult = unmapMemory<obj, testRegion> handle
                
                match unmapResult with
                | Ok () -> 
                    // Success is expected
                    ()
                | Error err ->
                    failwith $"Unmapping operation failed: {err.Message}"
            | Error err ->
                failwith $"Setup failed: {err.Message}"
            
        testCase "mapFile creates valid memory mapping" <| fun _ ->
            // Ensure platform is setup
            TestSetup.setupPlatformForTesting() |> ignore
            
            // Arrange
            let filePath = TestSetup.createTestFile()
            
            try
                // Act
                let result = mapFile<obj, testRegion> filePath 0L 1024<bytes> ReadOnly
                
                // Assert
                match result with
                | Ok (region, handle) ->
                    Expect.equal (int region.Length) 1024 "Region length should match requested size"
                    Expect.equal handle.Access ReadOnly "Handle access should match requested access"
                    
                    // Clean up
                    unmapMemory<obj, testRegion> handle |> ignore
                | Error err ->
                    failwith $"File mapping operation failed: {err.Message}"
            finally
                // Clean up
                TestSetup.deleteTestFile filePath
                
        testCase "flushMappedFile succeeds for valid mapping" <| fun _ ->
            // Ensure platform is setup
            TestSetup.setupPlatformForTesting() |> ignore
            
            // Arrange
            let filePath = TestSetup.createTestFile()
            
            try
                // Create a file mapping
                let mapResult = mapFile<obj, testRegion> filePath 0L 1024<bytes> ReadWrite
                
                // Act & Assert
                match mapResult with
                | Ok (region, handle) ->
                    let flushResult = flushMappedFile<obj, testRegion> region handle
                    
                    match flushResult with
                    | Ok () -> 
                        // Success is expected
                        ()
                    | Error err ->
                        failwith $"Flush operation failed: {err.Message}"
                    
                    // Clean up
                    unmapMemory<obj, testRegion> handle |> ignore
                | Error err ->
                    failwith $"Setup failed: {err.Message}"
            finally
                // Clean up
                TestSetup.deleteTestFile filePath
    ]

/// Tests for memory structure mapping
[<Tests>]
let structureMappingTests =
    testList "Structure Mapping Tests" [
        testCase "mapStructure creates valid memory view for a structure" <| fun _ ->
            // Ensure platform is setup
            TestSetup.setupPlatformForTesting() |> ignore
            
            // Arrange
            let schema = TestSetup.createTestSchema()
            let validatedSchema =
                SchemaValidation.validate schema
                |> function
                   | Ok s -> s
                   | Error e -> failwith $"Schema validation failed: {e}"
                
            let size = 256<bytes> // Size large enough for test structure
            
            // First create a memory mapping to get a pointer
            let mapResult = mapMemory<obj, testRegion> size PrivateMapping ReadWrite
            
            // Act & Assert
            match mapResult with
            | Ok (region, handle) ->
                // Get a pointer to the region
                let pointerResult = toPointer<obj, testRegion> region
                
                match pointerResult with
                | Ok pointer ->
                    // Map the structure
                    let structResult = mapStructure<obj, testRegion> pointer size validatedSchema
                    
                    match structResult with
                    | Ok view ->
                        Expect.equal view.Schema validatedSchema "View schema should match provided schema"
                        Expect.isTrue (view.Memory.Length > 0<bytes>) "View should have memory allocated"
                        
                        // Test setting and getting a field value
                        setField<obj, int32, testRegion> view ["id"] 42 |> ignore
                        let idResult = getField<obj, int32, testRegion> view ["id"]
                        
                        match idResult with
                        | Ok id -> Expect.equal id 42 "Field value should be retrievable"
                        | Error e -> failwith $"Failed to get field: {e.Message}"
                    | Error err ->
                        failwith $"Structure mapping failed: {err.Message}"
                | Error err ->
                    failwith $"Failed to get pointer: {err.Message}"
                
                // Clean up
                unmapMemory<obj, testRegion> handle |> ignore
            | Error err ->
                failwith $"Setup failed: {err.Message}"
        
        testCase "copyToPointer and copyFromPointer work correctly" <| fun _ ->
            // Ensure platform is setup
            TestSetup.setupPlatformForTesting() |> ignore
            
            // Arrange
            let schema = TestSetup.createTestSchema()
            let validatedSchema =
                SchemaValidation.validate schema
                |> function
                   | Ok s -> s
                   | Error e -> failwith $"Schema validation failed: {e}"
                
            let size = 256<bytes>
            
            // Create two memory mappings for source and destination
            let srcMapResult = mapMemory<obj, testRegion> size PrivateMapping ReadWrite
            let destMapResult = mapMemory<obj, testRegion> size PrivateMapping ReadWrite
            
            // Act & Assert
            match srcMapResult, destMapResult with
            | Ok (srcRegion, srcHandle), Ok (destRegion, destHandle) ->
                // Get pointers to the regions
                let srcPointerResult = toPointer<obj, testRegion> srcRegion
                let destPointerResult = toPointer<obj, testRegion> destRegion
                
                match srcPointerResult, destPointerResult with
                | Ok srcPointer, Ok destPointer ->
                    // Create views for the regions
                    let srcView = View.create<obj, testRegion> srcRegion validatedSchema
                    
                    // Set a test value in the source view
                    setField<obj, int32, testRegion> srcView ["id"] 42 |> ignore
                    setField<obj, float32, testRegion> srcView ["value"] 3.14f |> ignore
                    
                    // Copy to the destination pointer
                    let copyResult = copyToPointer<obj, testRegion> srcView destPointer
                    
                    match copyResult with
                    | Ok () ->
                        // Map the destination pointer to verify the copy
                        let destViewResult = mapStructure<obj, testRegion> destPointer size validatedSchema
                        
                        match destViewResult with
                        | Ok destView ->
                            // Verify the values were copied
                            let idResult = getField<obj, int32, testRegion> destView ["id"]
                            let valueResult = getField<obj, float32, testRegion> destView ["value"]
                            
                            match idResult, valueResult with
                            | Ok id, Ok value ->
                                Expect.equal id 42 "Integer field should be copied correctly"
                                Expect.equal value 3.14f "Float field should be copied correctly"
                            | _ ->
                                failwith "Failed to get field values from destination"
                        | Error err ->
                            failwith $"Failed to map destination structure: {err.Message}"
                    | Error err ->
                        failwith $"Copy operation failed: {err.Message}"
                | _ ->
                    failwith "Failed to get pointers"
                
                // Clean up
                unmapMemory<obj, testRegion> srcHandle |> ignore
                unmapMemory<obj, testRegion> destHandle |> ignore
            | _ ->
                failwith "Failed to create memory mappings"
    ]

/// Tests for shared memory operations
[<Tests>]
let sharedMemoryTests =
    testList "Shared Memory Tests" [
        testCase "mapSharedMemory creates and opens shared memory correctly" <| fun _ ->
            // Ensure platform is setup
            TestSetup.setupPlatformForTesting() |> ignore
            
            // Arrange
            let schema = TestSetup.createTestSchema()
            let validatedSchema =
                SchemaValidation.validate schema
                |> function
                   | Ok s -> s
                   | Error e -> failwith $"Schema validation failed: {e}"
                   
            let name = $"test-shared-memory-{Guid.NewGuid().ToString()}"
            let size = 1024<bytes>
            
            // Act - Create shared memory
            let createResult = mapSharedMemory<obj, testRegion> name size true validatedSchema
            
            // Assert - Creation
            match createResult with
            | Ok (creatorView, creatorHandle) ->
                // Set some test data in the shared memory
                setField<obj, int32, testRegion> creatorView ["id"] 42 |> ignore
                setField<obj, float32, testRegion> creatorView ["value"] 3.14f |> ignore
                
                // Now open the same shared memory with a different process
                let openResult = mapSharedMemory<obj, testRegion> name size false validatedSchema
                
                // Assert - Opening
                match openResult with
                | Ok (openerView, openerHandle) ->
                    // Verify that the data is shared
                    let idResult = getField<obj, int32, testRegion> openerView ["id"]
                    let valueResult = getField<obj, float32, testRegion> openerView ["value"]
                    
                    match idResult, valueResult with
                    | Ok id, Ok value ->
                        Expect.equal id 42 "Integer field should be accessible from second process"
                        Expect.equal value 3.14f "Float field should be accessible from second process"
                        
                        // Make a change in the opener view
                        setField<obj, int32, testRegion> openerView ["id"] 84 |> ignore
                        
                        // Verify the change is visible in the creator view
                        let creatorIdResult = getField<obj, int32, testRegion> creatorView ["id"]
                        match creatorIdResult with
                        | Ok creatorId -> Expect.equal creatorId 84 "Changes should be visible across processes"
                        | Error e -> failwith $"Failed to get updated field: {e.Message}"
                    | _ ->
                        failwith "Failed to get field values from shared memory"
                    
                    // Clean up opener
                    unmapMemory<obj, testRegion> openerHandle |> ignore
                | Error err ->
                    failwith $"Failed to open shared memory: {err.Message}"
                
                // Clean up creator
                unmapMemory<obj, testRegion> creatorHandle |> ignore
            | Error err ->
                failwith $"Failed to create shared memory: {err.Message}"
    ]

// Main entry point for running tests
[<EntryPoint>]
let main args =
    // Initialize platform services before running tests
    TestSetup.setupPlatformForTesting() |> ignore
    
    // Run all tests
    runTestsWithCLIArgs [] args mappingTests