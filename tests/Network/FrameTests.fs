module BAREWire.Tests.Network.FrameTests

open Expecto
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Network.Frame

/// Helper function to create a test frame
let createTestFrame messageType payload =
    {
        Header = {
            MessageType = messageType
            MessageId = Uuid.newUuid()
            PayloadLength = uint32 payload.Length
        }
        Payload = payload
    }

[<Tests>]
let frameTests =
    testList "Frame Module Tests" [
        testCase "Frame.encode should correctly encode a frame" <| fun _ ->
            // Arrange
            let messageType = MessageType.Request
            let payload = [| 1uy; 2uy; 3uy; 4uy |]
            let frame = createTestFrame messageType payload
            let expectedSize = 1 + 16 + 4 + payload.Length // 1 (type) + 16 (uuid) + 4 (length) + payload
            
            // Act
            let result = Frame.encode frame
            
            // Assert
            Expect.equal result.Length expectedSize "Encoded frame should have the expected size"
            Expect.equal result.[0] (byte messageType) "First byte should be the message type"
            
            // Check payload
            for i in 0 .. payload.Length - 1 do
                Expect.equal result.[21 + i] payload.[i] $"Payload byte at position {i} should match"
        
        testCase "Frame.decode should correctly decode a valid frame" <| fun _ ->
            // Arrange
            let messageType = MessageType.Request
            let payload = [| 1uy; 2uy; 3uy; 4uy |]
            let originalFrame = createTestFrame messageType payload
            let encoded = Frame.encode originalFrame
            
            // Act
            let result = Frame.decode encoded
            
            // Assert
            Expect.isTrue (Result.isOk result) "Decoding should succeed"
            let decodedFrame = Result.get result
            Expect.equal decodedFrame.Header.MessageType originalFrame.Header.MessageType "Message type should match"
            Expect.equal decodedFrame.Header.PayloadLength originalFrame.Header.PayloadLength "Payload length should match"
            Expect.equal decodedFrame.Payload originalFrame.Payload "Payload should match"
            
            // UUID comparison requires byte-by-byte comparison since it's a custom type
            for i in 0 .. 15 do
                Expect.equal 
                    decodedFrame.Header.MessageId.Data.[i] 
                    originalFrame.Header.MessageId.Data.[i] 
                    $"UUID byte at position {i} should match"
        
        testCase "Frame.decode should return error for too small data" <| fun _ ->
            // Arrange
            let tooSmallData = [| 1uy; 2uy; 3uy |] // Less than minimum header size
            
            // Act
            let result = Frame.decode tooSmallData
            
            // Assert
            Expect.isTrue (Result.isError result) "Decoding should fail for too small data"
            let error = Result.getError result
            Expect.isTrue (error.Message.Contains("too small")) "Error message should mention that the frame is too small"
        
        testCase "Frame.decode should return error for invalid payload length" <| fun _ ->
            // Arrange
            let messageType = MessageType.Request
            let uuid = Uuid.newUuid()
            let invalidData = Array.concat [
                [| byte messageType |]
                uuid.Data
                [| 100uy; 0uy; 0uy; 0uy |] // Length of 100, but not enough data
                [| 1uy; 2uy; 3uy; 4uy |] // Only 4 bytes of payload
            ]
            
            // Act
            let result = Frame.decode invalidData
            
            // Assert
            Expect.isTrue (Result.isError result) "Decoding should fail for invalid payload length"
        
        testCase "Frame.createRequest should create a frame with Request message type" <| fun _ ->
            // Arrange
            let payload = [| 1uy; 2uy; 3uy; 4uy |]
            
            // Act
            let frame = Frame.createRequest payload
            
            // Assert
            Expect.equal frame.Header.MessageType MessageType.Request "Message type should be Request"
            Expect.equal frame.Header.PayloadLength (uint32 payload.Length) "Payload length should match"
            Expect.equal frame.Payload payload "Payload should match"
        
        testCase "Frame.createResponse should create a frame with Response message type" <| fun _ ->
            // Arrange
            let requestId = Uuid.newUuid()
            let payload = [| 1uy; 2uy; 3uy; 4uy |]
            
            // Act
            let frame = Frame.createResponse requestId payload
            
            // Assert
            Expect.equal frame.Header.MessageType MessageType.Response "Message type should be Response"
            Expect.equal frame.Header.PayloadLength (uint32 payload.Length) "Payload length should match"
            Expect.equal frame.Payload payload "Payload should match"
            
            // Verify the ID was preserved
            for i in 0 .. 15 do
                Expect.equal 
                    frame.Header.MessageId.Data.[i] 
                    requestId.Data.[i] 
                    $"UUID byte at position {i} should match request ID"
        
        testCase "Frame.createNotification should create a frame with Notification message type" <| fun _ ->
            // Arrange
            let payload = [| 1uy; 2uy; 3uy; 4uy |]
            
            // Act
            let frame = Frame.createNotification payload
            
            // Assert
            Expect.equal frame.Header.MessageType MessageType.Notification "Message type should be Notification"
            Expect.equal frame.Header.PayloadLength (uint32 payload.Length) "Payload length should match"
            Expect.equal frame.Payload payload "Payload should match"
        
        testCase "Frame.createError should create a frame with Error message type" <| fun _ ->
            // Arrange
            let requestId = Uuid.newUuid()
            let payload = [| 1uy; 2uy; 3uy; 4uy |]
            
            // Act
            let frame = Frame.createError requestId payload
            
            // Assert
            Expect.equal frame.Header.MessageType MessageType.Error "Message type should be Error"
            Expect.equal frame.Header.PayloadLength (uint32 payload.Length) "Payload length should match"
            Expect.equal frame.Payload payload "Payload should match"
            
            // Verify the ID was preserved
            for i in 0 .. 15 do
                Expect.equal 
                    frame.Header.MessageId.Data.[i] 
                    requestId.Data.[i] 
                    $"UUID byte at position {i} should match request ID"
        
        testCase "Roundtrip encode/decode should preserve all data" <| fun _ ->
            // Test all message types
            let messageTypes = [
                MessageType.Request
                MessageType.Response
                MessageType.Notification
                MessageType.Error
            ]
            
            // Test various payload sizes
            let payloadSizes = [0; 1; 10; 100; 1000]
            
            for msgType in messageTypes do
                for size in payloadSizes do
                    // Arrange
                    let payload = Array.init size (fun i -> byte (i % 256))
                    let originalFrame = createTestFrame msgType payload
                    
                    // Act
                    let encoded = Frame.encode originalFrame
                    let decoded = Frame.decode encoded |> Result.get
                    
                    // Assert
                    Expect.equal decoded.Header.MessageType originalFrame.Header.MessageType 
                        $"Message type should be preserved for {msgType} with payload size {size}"
                    Expect.equal decoded.Header.PayloadLength originalFrame.Header.PayloadLength 
                        $"Payload length should be preserved for {msgType} with payload size {size}"
                    Expect.equal decoded.Payload originalFrame.Payload 
                        $"Payload should be preserved for {msgType} with payload size {size}"
    ]