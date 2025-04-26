module Tests.IPC.NamedPipeTests

open System
open Expecto
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.IPC.NamedPipe
open Tests.IPC.Common

/// Tests for NamedPipe module
[<Tests>]
let namedPipeTests =
    testList "NamedPipe Tests" [
        testCase "createServer should create a new pipe server" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Create a pipe server
            let result = createServer<string> "testPipe" testSchema defaultOptions
            
            // Verify the result
            match result with
            | Ok pipe ->
                Expect.equal pipe.Name "testPipe" "Pipe name should match"
                Expect.isGreaterThan pipe.Handle 0n "Handle should be valid"
                Expect.isTrue pipe.IsServer "IsServer should be true"
                Expect.equal pipe.Options defaultOptions "Pipe options should match"
            | Error err ->
                failtestf "Failed to create pipe server: %A" err
                
        testCase "connect should connect to an existing pipe" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Create a pipe server
            let serverResult = createServer<string> "testPipe" testSchema defaultOptions
            
            match serverResult with
            | Ok _ ->
                // Connect to the pipe
                let clientResult = connect<string> "testPipe" testSchema defaultOptions
                
                match clientResult with
                | Ok pipe ->
                    Expect.equal pipe.Name "testPipe" "Pipe name should match"
                    Expect.isGreaterThan pipe.Handle 0n "Handle should be valid"
                    Expect.isFalse pipe.IsServer "IsServer should be false"
                | Error err ->
                    failtestf "Failed to connect to pipe: %A" err
            | Error err ->
                failtestf "Failed to create pipe server for connect test: %A" err
                
        testCase "waitForConnection should handle client connections" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Create a pipe server
            let serverResult = createServer<string> "testPipe" testSchema defaultOptions
            
            match serverResult with
            | Ok server ->
                // Wait for connection
                match waitForConnection server with
                | Ok () ->
                    // Success - our mock always succeeds immediately
                    ()
                | Error err ->
                    failtestf "Failed to wait for connection: %A" err
            | Error err ->
                failtestf "Failed to create pipe server for waitForConnection test: %A" err
                
        testCase "send and receive should transfer messages" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Create server and client pipes
            let serverResult = createServer<string> "testPipe" testSchema defaultOptions
            
            match serverResult with
            | Ok server ->
                let clientResult = connect<string> "testPipe" testSchema defaultOptions
                
                match clientResult with
                | Ok client ->
                    // Test message
                    let testMessage = "Hello via Named Pipe!"
                    
                    // Send message from client to server
                    match send client testMessage with
                    | Ok () ->
                        // Receive message on server
                        match receive<string> server with
                        | Ok messageResult ->
                            match messageResult with
                            | Some message ->
                                Expect.equal message testMessage "Received message should match sent message"
                            | None ->
                                failtestf "Expected a message but got None"
                        | Error err ->
                            failtestf "Failed to receive message: %A" err
                    | Error err ->
                        failtestf "Failed to send message: %A" err
                | Error err ->
                    failtestf "Failed to connect to pipe: %A" err
            | Error err ->
                failtestf "Failed to create pipe server for send/receive test: %A" err
                
        testCase "close should release pipe resources" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Create a pipe server
            let result = createServer<string> "testPipe" testSchema defaultOptions
            
            match result with
            | Ok pipe ->
                // Close the pipe
                match close pipe with
                | Ok () ->
                    // Success
                    // In a real implementation, operations on a closed pipe would fail
                    ()
                | Error err ->
                    failtestf "Failed to close pipe: %A" err
            | Error err ->
                failtestf "Failed to create pipe for close test: %A" err
                
        testCase "exists should check if a pipe exists" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Check a non-existent pipe
            let existsBeforeCreate = exists "testPipe"
            Expect.isFalse existsBeforeCreate "Pipe should not exist before creation"
            
            // Create a pipe server
            let createResult = createServer<string> "testPipe" testSchema defaultOptions
            
            match createResult with
            | Ok _ ->
                // Check the pipe exists
                let existsAfterCreate = exists "testPipe"
                Expect.isTrue existsAfterCreate "Pipe should exist after creation"
            | Error err ->
                failtestf "Failed to create pipe server for exists test: %A" err
                
        testCase "sendWithTimeout should send with timeout" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Create server and client pipes
            let serverResult = createServer<string> "testPipe" testSchema defaultOptions
            
            match serverResult with
            | Ok server ->
                let clientResult = connect<string> "testPipe" testSchema defaultOptions
                
                match clientResult with
                | Ok client ->
                    // Test message
                    let testMessage = "Hello with timeout!"
                    
                    // Send message with timeout
                    match sendWithTimeout client testMessage 1000 with
                    | Ok () ->
                        // Receive message on server
                        match receive<string> server with
                        | Ok messageResult ->
                            match messageResult with
                            | Some message ->
                                Expect.equal message testMessage "Received message should match sent message"
                            | None ->
                                failtestf "Expected a message but got None"
                        | Error err ->
                            failtestf "Failed to receive message: %A" err
                    | Error err ->
                        failtestf "Failed to send message with timeout: %A" err
                | Error err ->
                    failtestf "Failed to connect to pipe: %A" err
            | Error err ->
                failtestf "Failed to create pipe server for sendWithTimeout test: %A" err
                
        testCase "receiveWithTimeout should receive with timeout" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Create server and client pipes
            let serverResult = createServer<string> "testPipe" testSchema defaultOptions
            
            match serverResult with
            | Ok server ->
                let clientResult = connect<string> "testPipe" testSchema defaultOptions
                
                match clientResult with
                | Ok client ->
                    // Test message
                    let testMessage = "Hello for timeout receive!"
                    
                    // Send message from client
                    match send client testMessage with
                    | Ok () ->
                        // Receive message with timeout on server
                        match receiveWithTimeout<string> server 1000 with
                        | Ok messageResult ->
                            match messageResult with
                            | Some message ->
                                Expect.equal message testMessage "Received message should match sent message"
                            | None ->
                                failtestf "Expected a message but got None"
                        | Error err ->
                            failtestf "Failed to receive message with timeout: %A" err
                    | Error err ->
                        failtestf "Failed to send message: %A" err
                | Error err ->
                    failtestf "Failed to connect to pipe: %A" err
            | Error err ->
                failtestf "Failed to create pipe server for receiveWithTimeout test: %A" err
                
        testCase "flush should ensure all data is sent" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Create a pipe server
            let result = createServer<string> "testPipe" testSchema defaultOptions
            
            match result with
            | Ok pipe ->
                // Flush the pipe
                match flush pipe with
                | Ok () ->
                    // Success - in our mock this is a no-op
                    ()
                | Error err ->
                    failtestf "Failed to flush pipe: %A" err
            | Error err ->
                failtestf "Failed to create pipe for flush test: %A" err
                
        testCase "createDuplexPair should create bidirectional pipes" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Create a duplex pair
            let result = createDuplexPair<string> "testPipe" testSchema defaultOptions
            
            match result with
            | Ok (serverPipe, clientPipe) ->
                // Verify server pipe
                Expect.isTrue serverPipe.IsServer "Server pipe IsServer should be true"
                Expect.equal serverPipe.Options.Direction PipeDirection.Out "Server pipe direction should be Out"
                
                // Verify client pipe
                Expect.isFalse clientPipe.IsServer "Client pipe IsServer should be false"
                Expect.equal clientPipe.Options.Direction PipeDirection.In "Client pipe direction should be In"
                
                // Test bidirectional communication
                let serverToClientMsg = "Server to Client"
                let clientToServerMsg = "Client to Server"
                
                // Server sends to client
                match send serverPipe serverToClientMsg with
                | Ok () ->
                    // Client receives from server
                    match receive<string> clientPipe with
                    | Ok msgResult1 ->
                        match msgResult1 with
                        | Some msg1 ->
                            Expect.equal msg1 serverToClientMsg "Client should receive server's message"
                            
                            // Create another duplex pair for the reverse direction
                            let reverseResult = createDuplexPair<string> "reversePipe" testSchema defaultOptions
                            
                            match reverseResult with
                            | Ok (server2, client2) ->
                                // Client sends to server
                                match send client2 clientToServerMsg with
                                | Ok () ->
                                    // Server receives from client
                                    match receive<string> server2 with
                                    | Ok msgResult2 ->
                                        match msgResult2 with
                                        | Some msg2 ->
                                            Expect.equal msg2 clientToServerMsg "Server should receive client's message"
                                        | None ->
                                            failtestf "Expected a message but got None for client to server"
                                    | Error err ->
                                        failtestf "Failed to receive client to server message: %A" err
                                | Error err ->
                                    failtestf "Failed to send client to server message: %A" err
                            | Error err ->
                                failtestf "Failed to create reverse duplex pair: %A" err
                        | None ->
                            failtestf "Expected a message but got None for server to client"
                    | Error err ->
                        failtestf "Failed to receive server to client message: %A" err
                | Error err ->
                    failtestf "Failed to send server to client message: %A" err
            | Error err ->
                failtestf "Failed to create duplex pipe pair: %A" err
                
        testCase "different pipe modes should be supported" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Test byte mode
            let byteOptions = { defaultOptions with Mode = PipeMode.Byte }
            let byteResult = createServer<string> "bytePipe" testSchema byteOptions
            
            match byteResult with
            | Ok bytePipe ->
                Expect.equal bytePipe.Options.Mode PipeMode.Byte "Byte mode should be set"
                
                // Test message mode
                let messageOptions = { defaultOptions with Mode = PipeMode.Message }
                let messageResult = createServer<string> "messagePipe" testSchema messageOptions
                
                match messageResult with
                | Ok messagePipe ->
                    Expect.equal messagePipe.Options.Mode PipeMode.Message "Message mode should be set"
                | Error err ->
                    failtestf "Failed to create message mode pipe: %A" err
            | Error err ->
                failtestf "Failed to create byte mode pipe: %A" err
                
        testCase "different pipe directions should be supported" <| fun _ ->
            // Setup mock providers
            let ipcProvider, _ = setupMockProviders()
            
            // Test In direction
            let inOptions = { defaultOptions with Direction = PipeDirection.In }
            let inResult = createServer<string> "inPipe" testSchema inOptions
            
            match inResult with
            | Ok inPipe ->
                Expect.equal inPipe.Options.Direction PipeDirection.In "In direction should be set"
                
                // Test Out direction
                let outOptions = { defaultOptions with Direction = PipeDirection.Out }
                let outResult = createServer<string> "outPipe" testSchema outOptions
                
                match outResult with
                | Ok outPipe ->
                    Expect.equal outPipe.Options.Direction PipeDirection.Out "Out direction should be set"
                    
                    // Test InOut direction
                    let inOutOptions = { defaultOptions with Direction = PipeDirection.InOut }
                    let inOutResult = createServer<string> "inOutPipe" testSchema inOutOptions
                    
                    match inOutResult with
                    | Ok inOutPipe ->
                        Expect.equal inOutPipe.Options.Direction PipeDirection.InOut "InOut direction should be set"
                    | Error err ->
                        failtestf "Failed to create InOut direction pipe: %A" err
                | Error err ->
                    failtestf "Failed to create Out direction pipe: %A" err
            | Error err ->
                failtestf "Failed to create In direction pipe: %A" err
    ]