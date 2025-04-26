module Tests.IPC.MessageQueueTests

open System
open Expecto
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.IPC.MessageQueue
open Tests.IPC.Common

/// Tests for MessageQueue module
[<Tests>]
let messageQueueTests =
    testList "MessageQueue Tests" [
        testCase "create should initialize a new message queue" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Create a message queue
            let capacity = 10
            let result = create<string> "testQueue" capacity testSchema
            
            // Verify the result
            match result with
            | Ok queue ->
                Expect.isNotNull queue.SharedRegion "SharedRegion should not be null"
                Expect.isNotNull queue.Schema "Schema should not be null"
                Expect.equal queue.SharedRegion.Name "testQueue" "Queue name should match"
            | Error err ->
                failtestf "Failed to create message queue: %A" err
                
        testCase "open' should open an existing message queue" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Create a message queue
            let capacity = 10
            let createResult = create<string> "testQueue" capacity testSchema
            
            // Open the queue
            match createResult with
            | Ok _ ->
                let openResult = open'<string> "testQueue" testSchema
                match openResult with
                | Ok queue ->
                    Expect.equal queue.SharedRegion.Name "testQueue" "Queue name should match"
                    Expect.isNotNull queue.Schema "Schema should not be null"
                | Error err ->
                    failtestf "Failed to open message queue: %A" err
            | Error err ->
                failtestf "Failed to create message queue for open test: %A" err
                
        testCase "enqueue should add a message to the queue" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Create a message queue
            let capacity = 10
            let createResult = create<string> "testQueue" capacity testSchema
            
            match createResult with
            | Ok queue ->
                // Enqueue a message
                let msgType = "TestMessage"
                let payload = "Hello, Message Queue!"
                
                match enqueue queue msgType payload with
                | Ok () ->
                    // Success
                    ()
                | Error err ->
                    failtestf "Failed to enqueue message: %A" err
            | Error err ->
                failtestf "Failed to create message queue for enqueue test: %A" err
                
        testCase "dequeue should remove and return a message" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Create a message queue
            let capacity = 10
            let createResult = create<string> "testQueue" capacity testSchema
            
            match createResult with
            | Ok queue ->
                // Enqueue a message
                let msgType = "TestMessage"
                let payload = "Hello, Message Queue!"
                
                match enqueue queue msgType payload with
                | Ok () ->
                    // Dequeue the message
                    match dequeue queue with
                    | Ok messageResult ->
                        match messageResult with
                        | Some message ->
                            Expect.equal message.Type msgType "Message type should match"
                            Expect.equal message.Payload payload "Message payload should match"
                        | None ->
                            failtestf "Expected a message but got None"
                    | Error err ->
                        failtestf "Failed to dequeue message: %A" err
                | Error err ->
                    failtestf "Failed to enqueue message: %A" err
            | Error err ->
                failtestf "Failed to create message queue for dequeue test: %A" err
                
        testCase "peek should return message without removing it" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Create a message queue
            let capacity = 10
            let createResult = create<string> "testQueue" capacity testSchema
            
            match createResult with
            | Ok queue ->
                // Enqueue a message
                let msgType = "TestMessage"
                let payload = "Hello, Message Queue!"
                
                match enqueue queue msgType payload with
                | Ok () ->
                    // Peek at the message
                    match peek queue with
                    | Ok messageResult ->
                        match messageResult with
                        | Some message ->
                            Expect.equal message.Type msgType "Message type should match"
                            Expect.equal message.Payload payload "Message payload should match"
                            
                            // Peek again to ensure message wasn't removed
                            match peek queue with
                            | Ok secondPeekResult ->
                                match secondPeekResult with
                                | Some _ -> () // Success, message still there
                                | None -> failtestf "Message disappeared after first peek"
                            | Error err ->
                                failtestf "Failed to peek second time: %A" err
                        | None ->
                            failtestf "Expected a message but got None"
                    | Error err ->
                        failtestf "Failed to peek at message: %A" err
                | Error err ->
                    failtestf "Failed to enqueue message: %A" err
            | Error err ->
                failtestf "Failed to create message queue for peek test: %A" err
                
        testCase "count should return number of messages in queue" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Create a message queue
            let capacity = 10
            let createResult = create<string> "testQueue" capacity testSchema
            
            match createResult with
            | Ok queue ->
                // Check initial count
                match count queue with
                | Ok countResult ->
                    match countResult with
                    | Ok initialCount ->
                        Expect.equal initialCount 0 "Initial count should be 0"
                        
                        // Enqueue some messages
                        match enqueue queue "Type1" "Message 1" with
                        | Ok () ->
                            match enqueue queue "Type2" "Message 2" with
                            | Ok () ->
                                // Check count after enqueuing
                                match count queue with
                                | Ok countAfterResult ->
                                    match countAfterResult with
                                    | Ok countAfter ->
                                        Expect.equal countAfter 2 "Count after enqueuing should be 2"
                                    | Error err ->
                                        failtestf "Inner count error after enqueue: %A" err
                                | Error err ->
                                    failtestf "Failed to get count after enqueue: %A" err
                            | Error err ->
                                failtestf "Failed to enqueue second message: %A" err
                        | Error err ->
                            failtestf "Failed to enqueue first message: %A" err
                    | Error err ->
                        failtestf "Inner initial count error: %A" err
                | Error err ->
                    failtestf "Failed to get initial count: %A" err
            | Error err ->
                failtestf "Failed to create message queue for count test: %A" err
                
        testCase "isEmpty should check if queue has messages" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Create a message queue
            let capacity = 10
            let createResult = create<string> "testQueue" capacity testSchema
            
            match createResult with
            | Ok queue ->
                // Check if initially empty
                match isEmpty queue with
                | Ok isEmptyResult ->
                    match isEmptyResult with
                    | Ok empty ->
                        Expect.isTrue empty "Queue should be empty initially"
                        
                        // Enqueue a message
                        match enqueue queue "TestType" "Test Message" with
                        | Ok () ->
                            // Check if empty after enqueuing
                            match isEmpty queue with
                            | Ok isEmptyAfterResult ->
                                match isEmptyAfterResult with
                                | Ok emptyAfter ->
                                    Expect.isFalse emptyAfter "Queue should not be empty after enqueuing"
                                | Error err ->
                                    failtestf "Inner isEmpty error after enqueue: %A" err
                            | Error err ->
                                failtestf "Failed to check isEmpty after enqueue: %A" err
                        | Error err ->
                            failtestf "Failed to enqueue message: %A" err
                    | Error err ->
                        failtestf "Inner initial isEmpty error: %A" err
                | Error err ->
                    failtestf "Failed to check initial isEmpty: %A" err
            | Error err ->
                failtestf "Failed to create message queue for isEmpty test: %A" err
                
        testCase "clear should remove all messages" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Create a message queue
            let capacity = 10
            let createResult = create<string> "testQueue" capacity testSchema
            
            match createResult with
            | Ok queue ->
                // Enqueue some messages
                match enqueue queue "Type1" "Message 1" with
                | Ok () ->
                    match enqueue queue "Type2" "Message 2" with
                    | Ok () ->
                        // Clear the queue
                        match clear queue with
                        | Ok () ->
                            // Check if empty after clearing
                            match isEmpty queue with
                            | Ok isEmptyResult ->
                                match isEmptyResult with
                                | Ok empty ->
                                    Expect.isTrue empty "Queue should be empty after clearing"
                                | Error err ->
                                    failtestf "Inner isEmpty error after clear: %A" err
                            | Error err ->
                                failtestf "Failed to check isEmpty after clear: %A" err
                        | Error err ->
                            failtestf "Failed to clear queue: %A" err
                    | Error err ->
                        failtestf "Failed to enqueue second message: %A" err
                | Error err ->
                    failtestf "Failed to enqueue first message: %A" err
            | Error err ->
                failtestf "Failed to create message queue for clear test: %A" err
                
        testCase "close should release queue resources" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Create a message queue
            let capacity = 10
            let createResult = create<string> "testQueue" capacity testSchema
            
            match createResult with
            | Ok queue ->
                // Close the queue
                match close queue with
                | Ok () ->
                    // Success
                    // In a real implementation we would check that queue operations
                    // fail after close, but our mock doesn't enforce this
                    ()
                | Error err ->
                    failtestf "Failed to close message queue: %A" err
            | Error err ->
                failtestf "Failed to create message queue for close test: %A" err
                
        testCase "queue should maintain FIFO order" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Create a message queue
            let capacity = 10
            let createResult = create<string> "testQueue" capacity testSchema
            
            match createResult with
            | Ok queue ->
                // Enqueue messages in specific order
                let messages = [
                    ("TypeA", "First message")
                    ("TypeB", "Second message")
                    ("TypeC", "Third message")
                ]
                
                // Enqueue all messages
                let rec enqueueAll remaining =
                    match remaining with
                    | [] -> Ok ()
                    | (typ, payload)::rest ->
                        match enqueue queue typ payload with
                        | Ok () -> enqueueAll rest
                        | Error err -> Error err
                
                match enqueueAll messages with
                | Ok () ->
                    // Dequeue and verify order
                    let rec dequeueAndVerify expected =
                        match expected with
                        | [] -> Ok ()
                        | (expectedType, expectedPayload)::rest ->
                            match dequeue queue with
                            | Ok messageResult ->
                                match messageResult with
                                | Some message ->
                                    if message.Type <> expectedType || 
                                       message.Payload <> expectedPayload then
                                        Error (invalidValueError 
                                            $"Expected ({expectedType}, {expectedPayload}), got ({message.Type}, {message.Payload})")
                                    else
                                        dequeueAndVerify rest
                                | None ->
                                    Error (invalidValueError "Queue emptied prematurely")
                            | Error err -> Error err
                    
                    match dequeueAndVerify messages with
                    | Ok () -> () // Success
                    | Error err -> failtestf "Message order verification failed: %A" err
                | Error err ->
                    failtestf "Failed to enqueue test messages: %A" err
            | Error err ->
                failtestf "Failed to create message queue for FIFO test: %A" err
    ]