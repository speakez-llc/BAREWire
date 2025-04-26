module BAREWire.Tests.Network.TransportTests

open Expecto
open System
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Network.Frame
open BAREWire.Network.Transport
open BAREWire.Platform
open BAREWire.Platform.Common.Interfaces

/// A mock network provider for testing transport functionality
type MockNetworkProvider() =
    let mutable socketCounter = 0n
    let mutable sockets = Map.empty<nativeint, MemoryBuffer>
    let mutable connectionStatus = Map.empty<nativeint, bool>
    
    // Buffers to simulate network communication
    and MemoryBuffer() =
        let mutable sendBuffer = ResizeArray<byte>()
        let mutable receiveBuffer = ResizeArray<byte>()
        
        member _.WriteToSend(data: byte[], offset: int, length: int) =
            for i in offset .. (offset + length - 1) do
                sendBuffer.Add(data.[i])
            length
        
        member _.ReadFromReceive(data: byte[], offset: int, length: int) =
            let available = receiveBuffer.Count
            if available = 0 then 0
            else
                let actualRead = min length available
                for i in 0 .. (actualRead - 1) do
                    data.[offset + i] <- receiveBuffer.[i]
                
                // Remove read data
                receiveBuffer.RemoveRange(0, actualRead)
                actualRead
        
        member _.HasData = receiveBuffer.Count > 0
        
        // For testing: simulate data arriving from remote endpoint
        member _.SimulateDataReceived(data: byte[]) =
            for b in data do
                receiveBuffer.Add(b)
        
        // For testing: get data that was "sent" to check correctness
        member _.GetSentData() =
            let result = sendBuffer.ToArray()
            sendBuffer.Clear()
            result
    
    interface INetworkProvider with
        member _.CreateSocket(family: AddressFamily, socketType: SocketType, protocolType: ProtocolType) =
            socketCounter <- socketCounter + 1n
            sockets <- Map.add socketCounter (MemoryBuffer())
            connectionStatus <- Map.add socketCounter false
            Ok socketCounter
        
        member _.CloseSocket(handle: nativeint) =
            if Map.containsKey handle sockets then
                sockets <- Map.remove handle sockets
                connectionStatus <- Map.remove handle connectionStatus
                Ok ()
            else
                Error (invalidValueError $"Invalid socket handle: {handle}")
        
        member _.ConnectSocket(handle: nativeint, address: string, port: int) =
            if Map.containsKey handle sockets then
                connectionStatus <- Map.add handle true
                Ok ()
            else
                Error (invalidValueError $"Invalid socket handle: {handle}")
        
        member _.BindSocket(handle: nativeint, address: string, port: int) =
            if Map.containsKey handle sockets then
                Ok ()
            else
                Error (invalidValueError $"Invalid socket handle: {handle}")
        
        member _.SendSocket(handle: nativeint, data: byte[], offset: int, length: int, flags: int) =
            if Map.containsKey handle sockets then
                let socket = Map.find handle sockets
                let bytesSent = socket.WriteToSend(data, offset, length)
                Ok bytesSent
            else
                Error (invalidValueError $"Invalid socket handle: {handle}")
        
        member _.ReceiveSocket(handle: nativeint, data: byte[], offset: int, length: int, flags: int) =
            if Map.containsKey handle sockets then
                let socket = Map.find handle sockets
                let bytesRead = socket.ReadFromReceive(data, offset, length)
                Ok bytesRead
            else
                Error (invalidValueError $"Invalid socket handle: {handle}")
        
        member _.Poll(handle: nativeint, timeoutMs: int) =
            if Map.containsKey handle sockets then
                let socket = Map.find handle sockets
                Ok socket.HasData
            else
                Error (invalidValueError $"Invalid socket handle: {handle}")
        
        member _.ResolveHostName(host: string) =
            // Always resolve to a fixed test address
            Ok [| "127.0.0.1" |]
    
    // Test helper methods
    member this.SimulateDataReceived(handle: nativeint, data: byte[]) =
        if Map.containsKey handle sockets then
            let socket = Map.find handle sockets
            socket.SimulateDataReceived(data)
            true
        else
            false
    
    member this.GetSentData(handle: nativeint) =
        if Map.containsKey handle sockets then
            let socket = Map.find handle sockets
            Some (socket.GetSentData())
        else
            None
    
    member this.GetSocketHandle() = socketCounter

/// Helper to set up a platform services mock for testing
let setupPlatformServicesMock() =
    let mockNetworkProvider = MockNetworkProvider()
    
    // Replace the actual platform services with our mock
    PlatformServices.registerNetworkProvider(mockNetworkProvider :> INetworkProvider)
    
    // Return the mock for test assertions
    mockNetworkProvider

/// Helper for creating test frames
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
let transportTests =
    testList "Transport Module Tests" [
        testList "TCP Transport Tests" [
            testCase "TCP transport should connect on first send" <| fun _ ->
                // Arrange
                let mockProvider = setupPlatformServicesMock()
                let transport = Tcp.createClient "localhost" 8080 defaultConfig
                let testFrame = createTestFrame MessageType.Request [| 1uy; 2uy; 3uy; 4uy |]
                
                // Act
                let result = transport.Send testFrame
                
                // Assert
                Expect.isTrue (Result.isOk result) "Send should succeed"
                
                // Get the sent data to verify it was encoded correctly
                let socketHandle = mockProvider.GetSocketHandle()
                let sentData = mockProvider.GetSentData(socketHandle)
                Expect.isSome sentData "Should have sent data"
                
                // Decode the sent frame to verify it matches
                match sentData with
                | Some data ->
                    let decodedFrame = Frame.decode data
                    Expect.isTrue (Result.isOk decodedFrame) "Should decode valid frame"
                    let frame = Result.get decodedFrame
                    Expect.equal frame.Header.MessageType testFrame.Header.MessageType "Message type should match"
                    Expect.equal frame.Header.PayloadLength testFrame.Header.PayloadLength "Payload length should match"
                    Expect.equal frame.Payload testFrame.Payload "Payload should match"
                | None ->
                    failwith "No sent data found"
            
            testCase "TCP transport should receive data correctly" <| fun _ ->
                // Arrange
                let mockProvider = setupPlatformServicesMock()
                let transport = Tcp.createClient "localhost" 8080 defaultConfig
                
                // Create a test frame and encode it
                let testFrame = createTestFrame MessageType.Response [| 1uy; 2uy; 3uy; 4uy |]
                let encodedFrame = Frame.encode testFrame
                
                // Send a dummy frame to establish connection
                transport.Send (createTestFrame MessageType.Request [| 0uy |]) |> ignore
                
                // Simulate receiving data
                let socketHandle = mockProvider.GetSocketHandle()
                mockProvider.SimulateDataReceived(socketHandle, encodedFrame)
                
                // Act
                let result = transport.Receive()
                
                // Assert
                Expect.isTrue (Result.isOk result) "Receive should succeed"
                let receivedFrameOpt = Result.get result
                Expect.isSome receivedFrameOpt "Should have received a frame"
                
                match receivedFrameOpt with
                | Some receivedFrame ->
                    Expect.equal receivedFrame.Header.MessageType testFrame.Header.MessageType "Message type should match"
                    Expect.equal receivedFrame.Header.PayloadLength testFrame.Header.PayloadLength "Payload length should match"
                    Expect.equal receivedFrame.Payload testFrame.Payload "Payload should match"
                | None ->
                    failwith "No received frame"
            
            testCase "TCP transport should handle no data correctly" <| fun _ ->
                // Arrange
                let _ = setupPlatformServicesMock()
                let transport = Tcp.createClient "localhost" 8080 defaultConfig
                
                // Send a dummy frame to establish connection
                transport.Send (createTestFrame MessageType.Request [| 0uy |]) |> ignore
                
                // Act
                let result = transport.Receive()
                
                // Assert
                Expect.isTrue (Result.isOk result) "Receive should succeed even with no data"
                let receivedFrameOpt = Result.get result
                Expect.isNone receivedFrameOpt "Should not have received a frame"
            
            testCase "TCP transport should close connection successfully" <| fun _ ->
                // Arrange
                let mockProvider = setupPlatformServicesMock()
                let transport = Tcp.createClient "localhost" 8080 defaultConfig
                
                // Establish connection
                transport.Send (createTestFrame MessageType.Request [| 0uy |]) |> ignore
                
                // Act
                let result = transport.Close()
                
                // Assert
                Expect.isTrue (Result.isOk result) "Close should succeed"
            
            testCase "TCP transport should handle large frames" <| fun _ ->
                // Arrange
                let mockProvider = setupPlatformServicesMock()
                let transport = Tcp.createClient "localhost" 8080 defaultConfig
                
                // Create a large payload
                let largePayload = Array.init 10000 (fun i -> byte (i % 256))
                let testFrame = createTestFrame MessageType.Request largePayload
                
                // Act
                let sendResult = transport.Send testFrame
                
                // Assert
                Expect.isTrue (Result.isOk sendResult) "Send should succeed for large frame"
                
                // Get the sent data to verify it was encoded correctly
                let socketHandle = mockProvider.GetSocketHandle()
                let sentData = mockProvider.GetSentData(socketHandle)
                Expect.isSome sentData "Should have sent data"
                
                // Simulate receiving the large frame back
                let encodedFrame = Option.get sentData
                mockProvider.SimulateDataReceived(socketHandle, encodedFrame)
                
                // Act - receive the large frame
                let receiveResult = transport.Receive()
                
                // Assert
                Expect.isTrue (Result.isOk receiveResult) "Receive should succeed for large frame"
                let receivedFrameOpt = Result.get receiveResult
                Expect.isSome receivedFrameOpt "Should have received a frame"
                
                match receivedFrameOpt with
                | Some receivedFrame ->
                    Expect.equal receivedFrame.Header.MessageType testFrame.Header.MessageType "Message type should match"
                    Expect.equal receivedFrame.Header.PayloadLength testFrame.Header.PayloadLength "Payload length should match"
                    Expect.equal receivedFrame.Payload testFrame.Payload "Payload should match"
                | None ->
                    failwith "No received frame"
        ]
        
        testList "UDP Transport Tests" [
            testCase "UDP transport should initialize on first send" <| fun _ ->
                // Arrange
                let mockProvider = setupPlatformServicesMock()
                let transport = Udp.create "localhost" 8080 defaultConfig
                let testFrame = createTestFrame MessageType.Request [| 1uy; 2uy; 3uy; 4uy |]
                
                // Act
                let result = transport.Send testFrame
                
                // Assert
                Expect.isTrue (Result.isOk result) "Send should succeed"
                
                // Get the sent data to verify it was encoded correctly
                let socketHandle = mockProvider.GetSocketHandle()
                let sentData = mockProvider.GetSentData(socketHandle)
                Expect.isSome sentData "Should have sent data"
                
                // Decode the sent frame to verify it matches
                match sentData with
                | Some data ->
                    let decodedFrame = Frame.decode data
                    Expect.isTrue (Result.isOk decodedFrame) "Should decode valid frame"
                    let frame = Result.get decodedFrame
                    Expect.equal frame.Header.MessageType testFrame.Header.MessageType "Message type should match"
                    Expect.equal frame.Header.PayloadLength testFrame.Header.PayloadLength "Payload length should match"
                    Expect.equal frame.Payload testFrame.Payload "Payload should match"
                | None ->
                    failwith "No sent data found"
            
            testCase "UDP transport should receive data correctly" <| fun _ ->
                // Arrange
                let mockProvider = setupPlatformServicesMock()
                let transport = Udp.create "localhost" 8080 defaultConfig
                
                // Initialize the transport
                transport.Send (createTestFrame MessageType.Request [| 0uy |]) |> ignore
                
                // Create a test frame and encode it
                let testFrame = createTestFrame MessageType.Response [| 1uy; 2uy; 3uy; 4uy |]
                let encodedFrame = Frame.encode testFrame
                
                // Simulate receiving data
                let socketHandle = mockProvider.GetSocketHandle()
                mockProvider.SimulateDataReceived(socketHandle, encodedFrame)
                
                // Act
                let result = transport.Receive()
                
                // Assert
                Expect.isTrue (Result.isOk result) "Receive should succeed"
                let receivedFrameOpt = Result.get result
                Expect.isSome receivedFrameOpt "Should have received a frame"
                
                match receivedFrameOpt with
                | Some receivedFrame ->
                    Expect.equal receivedFrame.Header.MessageType testFrame.Header.MessageType "Message type should match"
                    Expect.equal receivedFrame.Header.PayloadLength testFrame.Header.PayloadLength "Payload length should match"
                    Expect.equal receivedFrame.Payload testFrame.Payload "Payload should match"
                | None ->
                    failwith "No received frame"
            
            testCase "UDP transport should close socket successfully" <| fun _ ->
                // Arrange
                let mockProvider = setupPlatformServicesMock()
                let transport = Udp.create "localhost" 8080 defaultConfig
                
                // Initialize the transport
                transport.Send (createTestFrame MessageType.Request [| 0uy |]) |> ignore
                
                // Act
                let result = transport.Close()
                
                // Assert
                Expect.isTrue (Result.isOk result) "Close should succeed"
            
            testCase "UDP transport should handle packet boundaries" <| fun _ ->
                // Arrange
                let mockProvider = setupPlatformServicesMock()
                let transport = Udp.create "localhost" 8080 defaultConfig
                
                // Initialize the transport
                transport.Send (createTestFrame MessageType.Request [| 0uy |]) |> ignore
                
                // Create multiple test frames and encode them
                let frame1 = createTestFrame MessageType.Notification [| 1uy; 2uy; 3uy |]
                let frame2 = createTestFrame MessageType.Notification [| 4uy; 5uy; 6uy |]
                let encoded1 = Frame.encode frame1
                let encoded2 = Frame.encode frame2
                
                // Simulate receiving data - with UDP we simulate separate packets
                let socketHandle = mockProvider.GetSocketHandle()
                mockProvider.SimulateDataReceived(socketHandle, encoded1)
                
                // Act - receive first frame
                let result1 = transport.Receive()
                
                // Assert
                Expect.isTrue (Result.isOk result1) "Receive should succeed for first frame"
                let receivedFrame1Opt = Result.get result1
                Expect.isSome receivedFrame1Opt "Should have received first frame"
                
                // Now simulate receiving the second frame
                mockProvider.SimulateDataReceived(socketHandle, encoded2)
                
                // Act - receive second frame
                let result2 = transport.Receive()
                
                // Assert
                Expect.isTrue (Result.isOk result2) "Receive should succeed for second frame"
                let receivedFrame2Opt = Result.get result2
                Expect.isSome receivedFrame2Opt "Should have received second frame"
                
                // Verify frames were received correctly and in order
                match receivedFrame1Opt, receivedFrame2Opt with
                | Some receivedFrame1, Some receivedFrame2 ->
                    Expect.equal receivedFrame1.Payload frame1.Payload "First frame payload should match"
                    Expect.equal receivedFrame2.Payload frame2.Payload "Second frame payload should match"
                | _ ->
                    failwith "Did not receive both frames correctly"
        ]
        
        testList "WebSocket Transport Tests" [
            testCase "WebSocket transport should connect on first send" <| fun _ ->
                // Arrange
                let mockProvider = setupPlatformServicesMock()
                let transport = WebSocket.create "ws://localhost:8080" defaultConfig
                let testFrame = createTestFrame MessageType.Request [| 1uy; 2uy; 3uy; 4uy |]
                
                // Act
                let result = transport.Send testFrame
                
                // Assert
                Expect.isTrue (Result.isOk result) "Send should succeed"
                
                // Get the sent data to verify it was encoded correctly
                let socketHandle = mockProvider.GetSocketHandle()
                let sentData = mockProvider.GetSentData(socketHandle)
                Expect.isSome sentData "Should have sent data"
            
            testCase "WebSocket transport should receive data correctly" <| fun _ ->
                // Arrange
                let mockProvider = setupPlatformServicesMock()
                let transport = WebSocket.create "ws://localhost:8080" defaultConfig
                
                // Create a test frame and encode it
                let testFrame = createTestFrame MessageType.Response [| 1uy; 2uy; 3uy; 4uy |]
                let encodedFrame = Frame.encode testFrame
                
                // Send a dummy frame to establish connection
                transport.Send (createTestFrame MessageType.Request [| 0uy |]) |> ignore
                
                // Simulate receiving data
                let socketHandle = mockProvider.GetSocketHandle()
                mockProvider.SimulateDataReceived(socketHandle, encodedFrame)
                
                // Act
                let result = transport.Receive()
                
                // Assert
                Expect.isTrue (Result.isOk result) "Receive should succeed"
                let receivedFrameOpt = Result.get result
                Expect.isSome receivedFrameOpt "Should have received a frame"
                
                match receivedFrameOpt with
                | Some receivedFrame ->
                    Expect.equal receivedFrame.Header.MessageType testFrame.Header.MessageType "Message type should match"
                    Expect.equal receivedFrame.Header.PayloadLength testFrame.Header.PayloadLength "Payload length should match"
                    Expect.equal receivedFrame.Payload testFrame.Payload "Payload should match"
                | None ->
                    failwith "No received frame"
            
            testCase "WebSocket transport should parse URL correctly" <| fun _ ->
                // Arrange
                let urls = [
                    "ws://localhost:8080/chat"
                    "wss://secure.example.com:9000"
                    "ws://192.168.1.1"
                ]
                
                // Act & Assert
                for url in urls do
                    let mockProvider = setupPlatformServicesMock()
                    let transport = WebSocket.create url defaultConfig
                    
                    // Send something to trigger connection
                    let result = transport.Send (createTestFrame MessageType.Request [| 0uy |])
                    
                    Expect.isTrue (Result.isOk result) $"Should successfully connect to {url}"
                    
                    // Clean up
                    transport.Close() |> ignore
        ]
    ]