module BAREWire.Tests.Network.ProtocolTests

open Expecto
open System
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Core.Memory
open BAREWire.Encoding.Codec
open BAREWire.Network.Frame
open BAREWire.Network.Transport
open BAREWire.Network.Protocol
open BAREWire.Schema

/// A mock transport for testing protocol functionality
type MockTransport() =
    let mutable sentFrames = ResizeArray<Frame>()
    let mutable receiveQueue = ResizeArray<Frame option>()
    let mutable closed = false
    
    // Implement ITransport interface
    interface ITransport with
        member _.Send(frame) =
            sentFrames.Add(frame)
            Ok ()
        
        member _.Receive() =
            if receiveQueue.Count > 0 then
                let frame = receiveQueue.[0]
                receiveQueue.RemoveAt(0)
                Ok frame
            else
                Ok None
        
        member _.Close() =
            closed <- true
            Ok ()
    
    // Helper methods for tests
    member _.GetSentFrames() = sentFrames |> Seq.toList
    member _.EnqueueFrameForReceive(frame) = receiveQueue.Add(Some frame)
    member _.EnqueueNoDataForReceive() = receiveQueue.Add(None)
    member _.WasClosed() = closed
    member _.ClearSentFrames() = sentFrames.Clear()

/// A simple test schema
type TestMessage = {
    Id: int
    Name: string
    Data: byte[]
}

/// Create a simple schema for testing
let createTestSchema() =
    let schema = Schema.empty "TestSchema"
    
    let validatedSchema = 
        schema
        |> Schema.withType "TestMessage" (Schema.struct' [
            Schema.field "Id" Schema.int32
            Schema.field "Name" Schema.string
            Schema.field "Data" Schema.data
        ])
        |> Schema.validate
    
    match validatedSchema with
    | Ok schema -> schema
    | Error errors -> failwith (String.concat ", " errors)

/// Helper to create a message
let createTestMessage id name data =
    { Id = id; Name = name; Data = data }

[<Tests>]
let protocolTests =
    testList "Protocol Module Tests" [
        testCase "Protocol.createClient should create a client successfully" <| fun _ ->
            // Arrange
            let transport = MockTransport() :> ITransport
            let schema = createTestSchema()
            
            // Act
            let result = Protocol.createClient transport schema
            
            // Assert
            Expect.isTrue (Result.isOk result) "Client creation should succeed"
            let client = Result.get result
            Expect.isTrue (client.SchemaId <> Guid.Empty) "SchemaId should be generated"
        
        testCase "Protocol.send should encode and send message using transport" <| fun _ ->
            // Arrange
            let mockTransport = MockTransport()
            let transport = mockTransport :> ITransport
            let schema = createTestSchema()
            
            let client = 
                Protocol.createClient transport schema
                |> Result.get
            
            let message = createTestMessage 42 "Test" [| 1uy; 2uy; 3uy |]
            
            // Act
            let result = Protocol.send client message
            
            // Assert
            Expect.isTrue (Result.isOk result) "Send should succeed"
            
            let sentFrames = mockTransport.GetSentFrames()
            Expect.equal sentFrames.Length 1 "Should have sent exactly one frame"
            
            let frame = sentFrames.[0]
            Expect.equal (Frame.hasSchemaId frame) true "Frame should have schema ID"
            
            let schemaId = Frame.getSchemaId frame |> Result.get
            Expect.equal schemaId client.SchemaId "Frame schema ID should match client schema ID"
        
        testCase "Protocol.receive should decode received frame from transport" <| fun _ ->
            // Arrange
            let mockTransport = MockTransport()
            let transport = mockTransport :> ITransport
            let schema = createTestSchema()
            
            let client = 
                Protocol.createClient transport schema
                |> Result.get
            
            // Create a message, encode it, and create a frame
            let originalMessage = createTestMessage 42 "Test" [| 1uy; 2uy; 3uy |]
            let buffer = Buffer.Create(1024)
            
            encode schema originalMessage buffer |> ignore
            
            let payload = Array.init (int buffer.Position) (fun i -> buffer.Data.[i])
            let frame = Frame.createFrame (FrameFlags.HasSchema) (Some client.SchemaId) payload None
            
            // Enqueue the frame for the mock transport to return
            mockTransport.EnqueueFrameForReceive(frame)
            
            // Act
            let result = Protocol.receive<TestMessage> client
            
            // Assert
            Expect.isTrue (Result.isOk result) "Receive should succeed"
            let messageOpt = Result.get result
            Expect.isSome messageOpt "Should have received a message"
            
            let receivedMessage = messageOpt.Value
            Expect.equal receivedMessage.Id originalMessage.Id "Message Id should match"
            Expect.equal receivedMessage.Name originalMessage.Name "Message Name should match"
            Expect.equal receivedMessage.Data originalMessage.Data "Message Data should match"
        
        testCase "Protocol.receive should handle no data correctly" <| fun _ ->
            // Arrange
            let mockTransport = MockTransport()
            let transport = mockTransport :> ITransport
            let schema = createTestSchema()
            
            let client = 
                Protocol.createClient transport schema
                |> Result.get
            
            // Enqueue a 'no data' response
            mockTransport.EnqueueNoDataForReceive()
            
            // Act
            let result = Protocol.receive<TestMessage> client
            
            // Assert
            Expect.isTrue (Result.isOk result) "Receive should succeed even with no data"
            let messageOpt = Result.get result
            Expect.isNone messageOpt "Should not have received a message"
        
        testCase "Protocol.receive should handle schema ID mismatch correctly" <| fun _ ->
            // Arrange
            let mockTransport = MockTransport()
            let transport = mockTransport :> ITransport
            let schema = createTestSchema()
            
            let client = 
                Protocol.createClient transport schema
                |> Result.get
            
            // Create a message, encode it, and create a frame with a different schema ID
            let originalMessage = createTestMessage 42 "Test" [| 1uy; 2uy; 3uy |]
            let buffer = Buffer.Create(1024)
            
            encode schema originalMessage buffer |> ignore
            
            let payload = Array.init (int buffer.Position) (fun i -> buffer.Data.[i])
            let differentSchemaId = Guid.NewGuid()
            let frame = Frame.createFrame (FrameFlags.HasSchema) (Some differentSchemaId) payload None
            
            // Enqueue the frame for the mock transport to return
            mockTransport.EnqueueFrameForReceive(frame)
            
            // Act
            let result = Protocol.receive<TestMessage> client
            
            // Assert
            Expect.isTrue (Result.isError result) "Receive should fail with schema ID mismatch"
            let error = Result.getError result
            Expect.isTrue (error.Message.Contains("Schema ID mismatch")) "Error should mention schema ID mismatch"
        
        testCase "Protocol.close should close the transport" <| fun _ ->
            // Arrange
            let mockTransport = MockTransport()
            let transport = mockTransport :> ITransport
            let schema = createTestSchema()
            
            let client = 
                Protocol.createClient transport schema
                |> Result.get
            
            // Act
            let result = Protocol.close client
            
            // Assert
            Expect.isTrue (Result.isOk result) "Close should succeed"
            Expect.isTrue (mockTransport.WasClosed()) "Transport should be closed"
        
        testCase "Full round-trip test - send and receive" <| fun _ ->
            // Arrange
            let mockTransport = MockTransport()
            let transport = mockTransport :> ITransport
            let schema = createTestSchema()
            
            let client = 
                Protocol.createClient transport schema
                |> Result.get
            
            let originalMessage = createTestMessage 42 "Test" [| 1uy; 2uy; 3uy |]
            
            // Act - Send the message
            let sendResult = Protocol.send client originalMessage
            Expect.isTrue (Result.isOk sendResult) "Send should succeed"
            
            // Get the sent frame and enqueue it for receiving
            let sentFrames = mockTransport.GetSentFrames()
            mockTransport.ClearSentFrames() // Clear sent frames to avoid confusion
            mockTransport.EnqueueFrameForReceive(sentFrames.[0])
            
            // Act - Receive the message
            let receiveResult = Protocol.receive<TestMessage> client
            
            // Assert
            Expect.isTrue (Result.isOk receiveResult) "Receive should succeed"
            let messageOpt = Result.get receiveResult
            Expect.isSome messageOpt "Should have received a message"
            
            let receivedMessage = messageOpt.Value
            Expect.equal receivedMessage.Id originalMessage.Id "Message Id should match"
            Expect.equal receivedMessage.Name originalMessage.Name "Message Name should match"
            Expect.equal receivedMessage.Data originalMessage.Data "Message Data should match"
        
        testList "RPC Tests" [
            testCase "RPC client should be created successfully" <| fun _ ->
                // Arrange
                let mockTransport = MockTransport()
                let transport = mockTransport :> ITransport
                let requestSchema = createTestSchema()
                let responseSchema = createTestSchema()
                
                // Act
                let result = Protocol.createRpcClient transport requestSchema responseSchema
                
                // Assert
                Expect.isTrue (Result.isOk result) "RPC client creation should succeed"
            
            testCase "RPC call should send request and receive response" <| fun _ ->
                // Arrange
                let mockTransport = MockTransport()
                let transport = mockTransport :> ITransport
                let requestSchema = createTestSchema()
                let responseSchema = createTestSchema()
                
                // Create test data
                type TestParams = { Value: int }
                type TestResult = { Success: bool; Message: string }
                
                let client = 
                    Protocol.createRpcClient<TestParams, TestResult> transport requestSchema responseSchema
                    |> Result.get
                
                // Create and enqueue a response to our upcoming request
                let processCall() =
                    // Get the request that was sent
                    let sentFrames = mockTransport.GetSentFrames()
                    Expect.equal sentFrames.Length 1 "Should have sent exactly one frame"
                    
                    let requestFrame = sentFrames.[0]
                    // Decode the request to get the request ID
                    let payload = requestFrame.Payload
                    let memory = { Data = payload; Offset = 0<offset>; Length = payload.Length * 1<bytes> }
                    
                    let requestDeserialized = decode<Request<TestParams>> requestSchema memory |> Result.get
                    let (request, _) = requestDeserialized
                    
                    // Create a response with the same ID
                    let response = {
                        Id = request.Id
                        Result = Some { Success = true; Message = "Success" }
                        Error = None
                    }
                    
                    // Encode the response
                    let buffer = Buffer.Create(1024)
                    encode responseSchema response buffer |> ignore
                    
                    let responsePayload = Array.init (int buffer.Position) (fun i -> buffer.Data.[i])
                    let responseSchemaId = client.ResponseClient.SchemaId
                    let responseFrame = Frame.createFrame (FrameFlags.HasSchema) (Some responseSchemaId) responsePayload None
                    
                    // Enqueue the response
                    mockTransport.EnqueueFrameForReceive(responseFrame)
                
                // Set up the mock to process our request
                // In a real test this would be done with a proper callback mechanism
                // but for simplicity we'll just use a timer
                let timer = new System.Timers.Timer(50.0) // 50ms delay
                timer.AutoReset <- false
                timer.Elapsed.Add(fun _ -> processCall())
                timer.Start()
                
                // Act
                let result = Protocol.call client "test" { Value = 123 }
                
                // Assert
                Expect.isTrue (Result.isOk result) "RPC call should succeed"
                let response = Result.get result
                Expect.equal response.Success true "Response Success should match"
                Expect.equal response.Message "Success" "Response Message should match"
            
            testCase "RPC call should handle error response" <| fun _ ->
                // Arrange
                let mockTransport = MockTransport()
                let transport = mockTransport :> ITransport
                let requestSchema = createTestSchema()
                let responseSchema = createTestSchema()
                
                // Create test data
                type TestParams = { Value: int }
                type TestResult = { Success: bool; Message: string }
                
                let client = 
                    Protocol.createRpcClient<TestParams, TestResult> transport requestSchema responseSchema
                    |> Result.get
                
                // Create and enqueue an error response to our upcoming request
                let processCall() =
                    // Get the request that was sent
                    let sentFrames = mockTransport.GetSentFrames()
                    let requestFrame = sentFrames.[0]
                    // Decode the request to get the request ID
                    let payload = requestFrame.Payload
                    let memory = { Data = payload; Offset = 0<offset>; Length = payload.Length * 1<bytes> }
                    
                    let requestDeserialized = decode<Request<TestParams>> requestSchema memory |> Result.get
                    let (request, _) = requestDeserialized
                    
                    // Create an error response with the same ID
                    let response = {
                        Id = request.Id
                        Result = None
                        Error = Some "An error occurred"
                    }
                    
                    // Encode the response
                    let buffer = Buffer.Create(1024)
                    encode responseSchema response buffer |> ignore
                    
                    let responsePayload = Array.init (int buffer.Position) (fun i -> buffer.Data.[i])
                    let responseSchemaId = client.ResponseClient.SchemaId
                    let responseFrame = Frame.createFrame (FrameFlags.HasSchema) (Some responseSchemaId) responsePayload None
                    
                    // Enqueue the response
                    mockTransport.EnqueueFrameForReceive(responseFrame)
                
                // Set up the mock to process our request
                let timer = new System.Timers.Timer(50.0) // 50ms delay
                timer.AutoReset <- false
                timer.Elapsed.Add(fun _ -> processCall())
                timer.Start()
                
                // Act
                let result = Protocol.call client "test" { Value = 123 }
                
                // Assert
                Expect.isTrue (Result.isError result) "RPC call should fail with error response"
                let error = Result.getError result
                Expect.isTrue (error.Message.Contains("An error occurred")) "Error should contain the error message from the response"
        ]
    ]