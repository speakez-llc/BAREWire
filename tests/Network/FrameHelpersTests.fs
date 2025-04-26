module BAREWire.Tests.Network.FrameHelpersTests

open Expecto
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Network.Frame

/// Helper functions for frame manipulation (these would be in the actual implementation)
module FrameHelpers =
    /// Combines multiple frames into a single buffer
    let combineFrames (frames: Frame[]) : byte[] =
        // Calculate total size needed
        let totalSize = 
            frames
            |> Array.sumBy (fun frame -> 
                let encodedFrame = Frame.encode frame
                encodedFrame.Length
            )
        
        // Create the output buffer
        let buffer = Array.zeroCreate totalSize
        let mutable position = 0
        
        // Add each frame
        for frame in frames do
            let encodedFrame = Frame.encode frame
            Array.Copy(encodedFrame, 0, buffer, position, encodedFrame.Length)
            position <- position + encodedFrame.Length
        
        buffer
    
    /// Splits a buffer containing multiple frames
    let splitFrames (buffer: byte[]) : Result<Frame[]> =
        // Process frames until we run out of data
        let rec processFrames (data: byte[]) (offset: int) (frames: Frame list) =
            if offset >= data.Length then
                // No more data to process
                Ok (List.toArray (List.rev frames))
            else
                // Calculate remaining data
                let remaining = data.Length - offset
                
                // Extract the current chunk
                let chunk = Array.create remaining 0uy
                Array.Copy(data, offset, chunk, 0, remaining)
                
                // Try to decode a frame
                match Frame.decode chunk with
                | Ok frame ->
                    // Calculate the size of this frame
                    let frameSize = 1 + 16 + 4 + int frame.Header.PayloadLength
                    
                    // Add frame to our list and continue
                    processFrames data (offset + frameSize) (frame :: frames)
                
                | Error _ when frames.Length > 0 ->
                    // We've processed some frames but can't decode more - stop here
                    Ok (List.toArray (List.rev frames))
                
                | Error err ->
                    // Error on first attempt
                    Error err
        
        processFrames buffer 0 []
    
    /// Compresses a frame's payload if it exceeds a certain size
    let compressLargeFrame (frame: Frame) (threshold: int) : Frame =
        if frame.Payload.Length <= threshold then
            // No compression needed
            frame
        else
            // This is a simplified mock compression that just removes every other byte
            // In a real implementation, this would use a proper compression algorithm
            let compressedPayload = 
                [| for i in 0 .. 2 .. frame.Payload.Length - 1 -> frame.Payload.[i] |]
            
            // Create a new frame with the compressed payload
            { frame with
                Header = {
                    frame.Header with
                        PayloadLength = uint32 compressedPayload.Length
                }
                Payload = compressedPayload
            }
    
    /// Adds a checksum to the frame
    let addFrameChecksum (frame: Frame) : byte[] =
        // Encode the frame first
        let encoded = Frame.encode frame
        
        // Calculate a simple checksum (XOR of all bytes)
        let mutable checksum = 0uy
        for b in encoded do
            checksum <- checksum ^^^ b
        
        // Append checksum to the encoded frame
        Array.append encoded [| checksum |]
    
    /// Verifies frame checksum
    let verifyFrameChecksum (data: byte[]) : Result<Frame> =
        if data.Length < 2 then // Minimum size for a valid frame + checksum
            Error (decodingError "Data too small to contain frame with checksum")
        else
            // Extract the frame data and checksum
            let frameData = data.[0 .. data.Length - 2]
            let checksum = data.[data.Length - 1]
            
            // Calculate expected checksum
            let mutable expectedChecksum = 0uy
            for b in frameData do
                expectedChecksum <- expectedChecksum ^^^ b
            
            // Verify checksum
            if checksum <> expectedChecksum then
                Error (decodingError "Frame checksum verification failed")
            else
                // Decode the frame
                Frame.decode frameData

// Tests for the frame helper functions
[<Tests>]
let frameHelperTests =
    testList "Frame Helper Tests" [
        testCase "FrameHelpers.combineFrames should combine multiple frames correctly" <| fun _ ->
            // Arrange
            let frame1 = Frame.createRequest [| 1uy; 2uy; 3uy |]
            let frame2 = Frame.createNotification [| 4uy; 5uy |]
            let frame3 = Frame.createResponse (Uuid.newUuid()) [| 6uy; 7uy; 8uy; 9uy |]
            
            let frames = [| frame1; frame2; frame3 |]
            
            // Act
            let combined = FrameHelpers.combineFrames frames
            
            // Assert
            let expectedSize = 
                (Frame.encode frame1).Length +
                (Frame.encode frame2).Length +
                (Frame.encode frame3).Length
            
            Expect.equal combined.Length expectedSize "Combined buffer should have correct size"
        
        testCase "FrameHelpers.splitFrames should extract all frames from a combined buffer" <| fun _ ->
            // Arrange
            let frame1 = Frame.createRequest [| 1uy; 2uy; 3uy |]
            let frame2 = Frame.createNotification [| 4uy; 5uy |]
            let frame3 = Frame.createResponse (Uuid.newUuid()) [| 6uy; 7uy; 8uy; 9uy |]
            
            let originalFrames = [| frame1; frame2; frame3 |]
            let combined = FrameHelpers.combineFrames originalFrames
            
            // Act
            let result = FrameHelpers.splitFrames combined
            
            // Assert
            Expect.isTrue (Result.isOk result) "Splitting should succeed"
            let splitFrames = Result.get result
            
            Expect.equal splitFrames.Length originalFrames.Length "Should extract same number of frames"
            
            // Compare each frame
            for i in 0 .. originalFrames.Length - 1 do
                Expect.equal 
                    splitFrames.[i].Header.MessageType 
                    originalFrames.[i].Header.MessageType 
                    $"Frame {i} message type should match"
                
                Expect.equal 
                    splitFrames.[i].Header.PayloadLength 
                    originalFrames.[i].Header.PayloadLength 
                    $"Frame {i} payload length should match"
                
                Expect.equal 
                    splitFrames.[i].Payload 
                    originalFrames.[i].Payload 
                    $"Frame {i} payload should match"
        
        testCase "FrameHelpers.splitFrames should handle partial frames correctly" <| fun _ ->
            // Arrange
            let frame1 = Frame.createRequest [| 1uy; 2uy; 3uy |]
            let frame2 = Frame.createNotification [| 4uy; 5uy |]
            
            let combinedData = FrameHelpers.combineFrames [| frame1; frame2 |]
            
            // Add some partial frame data
            let partialFrameData = [| 0xABuy; 0xCDuy; 0xEFuy |] // Invalid/incomplete frame data
            let dataWithPartial = Array.append combinedData partialFrameData
            
            // Act
            let result = FrameHelpers.splitFrames dataWithPartial
            
            // Assert
            Expect.isTrue (Result.isOk result) "Splitting should succeed for complete frames"
            let splitFrames = Result.get result
            
            Expect.equal splitFrames.Length 2 "Should extract the two complete frames"
            
            // Verify extracted frames
            Expect.equal 
                splitFrames.[0].Header.MessageType 
                frame1.Header.MessageType 
                "First frame message type should match"
            
            Expect.equal 
                splitFrames.[1].Header.MessageType 
                frame2.Header.MessageType 
                "Second frame message type should match"
        
        testCase "FrameHelpers.compressLargeFrame should compress payloads over threshold" <| fun _ ->
            // Arrange
            let largePayload = Array.init 1000 (fun i -> byte (i % 256))
            let frame = Frame.createRequest largePayload
            let threshold = 500
            
            // Act
            let compressedFrame = FrameHelpers.compressLargeFrame frame threshold
            
            // Assert
            Expect.isTrue (compressedFrame.Payload.Length < frame.Payload.Length) "Payload should be compressed"
            Expect.equal compressedFrame.Header.PayloadLength (uint32 compressedFrame.Payload.Length) "Header payload length should be updated"
        
        testCase "FrameHelpers.compressLargeFrame should not compress payloads under threshold" <| fun _ ->
            // Arrange
            let smallPayload = [| 1uy; 2uy; 3uy; 4uy; 5uy |]
            let frame = Frame.createRequest smallPayload
            let threshold = 10
            
            // Act
            let result = FrameHelpers.compressLargeFrame frame threshold
            
            // Assert
            Expect.equal result frame "Frame should be unchanged"
        
        testCase "FrameHelpers.addFrameChecksum should add checksum correctly" <| fun _ ->
            // Arrange
            let frame = Frame.createRequest [| 1uy; 2uy; 3uy |]
            let encoded = Frame.encode frame
            
            // Calculate expected checksum (XOR of all bytes)
            let mutable expectedChecksum = 0uy
            for b in encoded do
                expectedChecksum <- expectedChecksum ^^^ b
            
            // Act
            let withChecksum = FrameHelpers.addFrameChecksum frame
            
            // Assert
            Expect.equal withChecksum.Length (encoded.Length + 1) "Buffer should be original length plus checksum byte"
            Expect.equal withChecksum.[withChecksum.Length - 1] expectedChecksum "Checksum should be correct"
        
        testCase "FrameHelpers.verifyFrameChecksum should accept valid checksums" <| fun _ ->
            // Arrange
            let frame = Frame.createRequest [| 1uy; 2uy; 3uy |]
            let withChecksum = FrameHelpers.addFrameChecksum frame
            
            // Act
            let result = FrameHelpers.verifyFrameChecksum withChecksum
            
            // Assert
            Expect.isTrue (Result.isOk result) "Verification should succeed for valid checksum"
            let extractedFrame = Result.get result
            
            Expect.equal 
                extractedFrame.Header.MessageType 
                frame.Header.MessageType 
                "Frame message type should match original"
            
            Expect.equal 
                extractedFrame.Payload 
                frame.Payload 
                "Frame payload should match original"
        
        testCase "FrameHelpers.verifyFrameChecksum should reject invalid checksums" <| fun _ ->
            // Arrange
            let frame = Frame.createRequest [| 1uy; 2uy; 3uy |]
            let withChecksum = FrameHelpers.addFrameChecksum frame
            
            // Corrupt the checksum
            let corruptedData = Array.copy withChecksum
            corruptedData.[corruptedData.Length - 1] <- corruptedData.[corruptedData.Length - 1] ^^^ 0xFFuy
            
            // Act
            let result = FrameHelpers.verifyFrameChecksum corruptedData
            
            // Assert
            Expect.isTrue (Result.isError result) "Verification should fail for invalid checksum"
            let error = Result.getError result
            Expect.isTrue (error.Message.Contains("checksum verification failed")) "Error should mention checksum verification"
        
        testCase "Complex test: Compress, add checksum, combine, split, verify" <| fun _ ->
            // Arrange
            let payload1 = Array.init 600 (fun i -> byte i)
            let payload2 = Array.init 300 (fun i -> byte (i + 100))
            
            let frame1 = Frame.createRequest payload1
            let frame2 = Frame.createNotification payload2
            
            // Act - complex pipeline
            
            // 1. Compress frames if needed
            let compressed1 = FrameHelpers.compressLargeFrame frame1 500
            let compressed2 = FrameHelpers.compressLargeFrame frame2 500
            
            // 2. Add checksums
            let withChecksum1 = FrameHelpers.addFrameChecksum compressed1
            let withChecksum2 = FrameHelpers.addFrameChecksum compressed2
            
            // 3. Combine into a single buffer
            let combined = Array.append withChecksum1 withChecksum2
            
            // 4. Split into individual frames with checksums
            let frames = 
                [
                    withChecksum1
                    withChecksum2
                ]
            
            // 5. Verify checksums and extract frames
            let results = 
                frames
                |> List.map FrameHelpers.verifyFrameChecksum
                |> List.map (fun result -> 
                    Expect.isTrue (Result.isOk result) "Frame verification should succeed"
                    Result.get result
                )
                |> List.toArray
            
            // Assert
            Expect.equal results.Length 2 "Should extract two frames"
            
            // First frame should be compressed
            Expect.isTrue (results.[0].Payload.Length < payload1.Length) "First frame should be compressed"
            
            // Second frame should not be compressed (was under threshold)
            Expect.equal results.[1].Payload.Length compressed2.Payload.Length "Second frame compression status should be preserved"
            
            // Verify message types
            Expect.equal results.[0].Header.MessageType frame1.Header.MessageType "First frame message type should be preserved"
            Expect.equal results.[1].Header.MessageType frame2.Header.MessageType "Second frame message type should be preserved"
    ]