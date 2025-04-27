namespace BAREWire.Network

open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Core.Binary
open BAREWire.Core.Utf8
open BAREWire.Core.Time
open BAREWire.Core.Uuid

/// <summary>
/// Network frame format for binary communication
/// </summary>
module Frame =
    /// <summary>
    /// Frame version identifier
    /// </summary>
    [<Literal>]
    let ProtocolVersion = 1uy
    
    /// <summary>
    /// Message types for network frames
    /// </summary>
    type MessageType =
        | Request = 0uy
        | Response = 1uy
        | Notification = 2uy
        | Error = 3uy
    
    /// <summary>
    /// Frame flags for controlling behavior
    /// </summary>
    [<Flags>]
    type FrameFlags =
        | None = 0uy
        | Compressed = 1uy
        | Encrypted = 2uy
        | HasHeaders = 4uy
    
    /// <summary>
    /// Header entry for a network frame
    /// </summary>
    type HeaderEntry = {
        /// <summary>Key of the header entry</summary>
        Key: string
        
        /// <summary>Value of the header entry</summary>
        Value: string
    }
    
    /// <summary>
    /// Header for a network frame
    /// </summary>
    type FrameHeader = {
        /// <summary>Protocol version</summary>
        Version: byte
        
        /// <summary>Type of message</summary>
        MessageType: MessageType
        
        /// <summary>Frame flags</summary>
        Flags: FrameFlags
        
        /// <summary>ID of the message for correlation</summary>
        MessageId: Uuid
        
        /// <summary>Timestamp of the message (seconds since Unix epoch)</summary>
        Timestamp: int64
        
        /// <summary>Length of the payload in bytes</summary>
        PayloadLength: uint32
        
        /// <summary>Optional headers</summary>
        Headers: HeaderEntry list
    }
    
    /// <summary>
    /// A complete network frame
    /// </summary>
    type Frame = {
        /// <summary>The frame header</summary>
        Header: FrameHeader
        
        /// <summary>The frame payload</summary>
        Payload: byte[]
    }
    
    /// <summary>
    /// Encodes a header entry into a byte array
    /// </summary>
    /// <param name="entry">The header entry to encode</param>
    /// <returns>The encoded byte array</returns>
    let encodeHeaderEntry (entry: HeaderEntry): byte[] =
        // Encode the key
        let keyBytes = getBytes entry.Key
        
        // Encode the value
        let valueBytes = getBytes entry.Value
        
        // Calculate size: 2 (key length) + key bytes + 2 (value length) + value bytes
        let size = 2 + keyBytes.Length + 2 + valueBytes.Length
        let result = Array.zeroCreate size
        let mutable position = 0
        
        // Write key length (uint16)
        let keyLength = uint16 keyBytes.Length
        let keyLengthBytes = getUInt16Bytes keyLength
        Array.Copy(keyLengthBytes, 0, result, position, 2)
        position <- position + 2
        
        // Write key
        Array.Copy(keyBytes, 0, result, position, keyBytes.Length)
        position <- position + keyBytes.Length
        
        // Write value length (uint16)
        let valueLength = uint16 valueBytes.Length
        let valueLengthBytes = getUInt16Bytes valueLength
        Array.Copy(valueLengthBytes, 0, result, position, 2)
        position <- position + 2
        
        // Write value
        Array.Copy(valueBytes, 0, result, position, valueBytes.Length)
        
        result
    
    /// <summary>
    /// Decodes a header entry from a byte array
    /// </summary>
    /// <param name="data">The byte array to decode from</param>
    /// <param name="offset">The starting offset in the array</param>
    /// <returns>The decoded header entry and the new offset</returns>
    let decodeHeaderEntry (data: byte[]) (offset: int): Result<HeaderEntry * int> =
        try
            let mutable position = offset
            
            // Read key length
            if position + 2 > data.Length then
                return Error (decodingError "Buffer too small for header key length")
            
            let keyLength = toUInt16 data position
            position <- position + 2
            
            // Read key
            if position + int keyLength > data.Length then
                return Error (decodingError "Buffer too small for header key")
            
            let keyBytes = Array.init (int keyLength) (fun i -> data.[position + i])
            let key = getString keyBytes
            position <- position + int keyLength
            
            // Read value length
            if position + 2 > data.Length then
                return Error (decodingError "Buffer too small for header value length")
            
            let valueLength = toUInt16 data position
            position <- position + 2
            
            // Read value
            if position + int valueLength > data.Length then
                return Error (decodingError "Buffer too small for header value")
            
            let valueBytes = Array.init (int valueLength) (fun i -> data.[position + i])
            let value = getString valueBytes
            position <- position + int valueLength
            
            // Create header entry
            let entry = {
                Key = key
                Value = value
            }
            
            Ok (entry, position)
        with ex ->
            Error (decodingError $"Failed to decode header entry: {ex.Message}")
    
    /// <summary>
    /// Encodes a frame header into a byte array
    /// </summary>
    /// <param name="header">The frame header to encode</param>
    /// <returns>The encoded byte array and the size of the encoded header</returns>
    let encodeHeader (header: FrameHeader): byte[] * int =
        // Calculate initial size: 1 (version) + 1 (message type) + 1 (flags) + 16 (message id) + 8 (timestamp) + 4 (payload length)
        let mutable size = 1 + 1 + 1 + 16 + 8 + 4
        
        // Encode headers if present
        let headerBytes = 
            if header.Headers.IsEmpty then
                Array.empty
            else
                // Calculate headers size: 2 (count) + sum of header entry sizes
                let headerEntries = header.Headers |> List.map encodeHeaderEntry
                let headersSize = 2 + (headerEntries |> List.sumBy (fun bytes -> bytes.Length))
                size <- size + headersSize
                
                let result = Array.zeroCreate headersSize
                let mutable position = 0
                
                // Write header count
                let headerCount = uint16 header.Headers.Length
                let headerCountBytes = getUInt16Bytes headerCount
                Array.Copy(headerCountBytes, 0, result, position, 2)
                position <- position + 2
                
                // Write header entries
                for bytes in headerEntries do
                    Array.Copy(bytes, 0, result, position, bytes.Length)
                    position <- position + bytes.Length
                
                result
        
        // Create result
        let result = Array.zeroCreate size
        let mutable position = 0
        
        // Write version
        result.[position] <- header.Version
        position <- position + 1
        
        // Write message type
        result.[position] <- byte header.MessageType
        position <- position + 1
        
        // Write flags
        result.[position] <- byte header.Flags
        position <- position + 1
        
        // Write message id
        Array.Copy(header.MessageId.Data, 0, result, position, 16)
        position <- position + 16
        
        // Write timestamp
        let timestampBytes = getInt64Bytes header.Timestamp
        Array.Copy(timestampBytes, 0, result, position, 8)
        position <- position + 8
        
        // Write payload length
        let payloadLengthBytes = getUInt32Bytes header.PayloadLength
        Array.Copy(payloadLengthBytes, 0, result, position, 4)
        position <- position + 4
        
        // Write headers if present
        if not header.Headers.IsEmpty then
            Array.Copy(headerBytes, 0, result, position, headerBytes.Length)
            position <- position + headerBytes.Length
        
        result, position
    
    /// <summary>
    /// Decodes a frame header from a byte array
    /// </summary>
    /// <param name="data">The byte array to decode from</param>
    /// <returns>A result containing the decoded header and the size of the header</returns>
    let decodeHeader (data: byte[]): Result<FrameHeader * int> =
        try
            if data.Length < 31 then // Minimum size for header without custom headers
                return Error (decodingError "Buffer too small for frame header")
            
            let mutable position = 0
            
            // Read version
            let version = data.[position]
            position <- position + 1
            
            // Read message type
            let messageType =
                match data.[position] with
                | 0uy -> MessageType.Request
                | 1uy -> MessageType.Response
                | 2uy -> MessageType.Notification
                | 3uy -> MessageType.Error
                | _ -> MessageType.Request // Default to request for invalid types
            position <- position + 1
            
            // Read flags
            let flags = enum<FrameFlags>(int data.[position])
            position <- position + 1
            
            // Read message id
            let messageId = { Data = Array.zeroCreate 16 }
            Array.Copy(data, position, messageId.Data, 0, 16)
            position <- position + 16
            
            // Read timestamp
            let timestamp = toInt64 data position
            position <- position + 8
            
            // Read payload length
            let payloadLength = toUInt32 data position
            position <- position + 4
            
            // Read headers if present
            let headers =
                if flags.HasFlag(FrameFlags.HasHeaders) then
                    if position + 2 > data.Length then
                        return Error (decodingError "Buffer too small for header count")
                    
                    let headerCount = int (toUInt16 data position)
                    position <- position + 2
                    
                    let mutable result = []
                    let mutable currentPosition = position
                    
                    for _ in 1..headerCount do
                        match decodeHeaderEntry data currentPosition with
                        | Ok (entry, newPosition) ->
                            result <- entry :: result
                            currentPosition <- newPosition
                        | Error err -> return Error err
                    
                    position <- currentPosition
                    List.rev result
                else
                    []
            
            // Create header
            let header = {
                Version = version
                MessageType = messageType
                Flags = flags
                MessageId = messageId
                Timestamp = timestamp
                PayloadLength = payloadLength
                Headers = headers
            }
            
            Ok (header, position)
        with ex ->
            Error (decodingError $"Failed to decode frame header: {ex.Message}")
    
    /// <summary>
    /// Encodes a frame into a byte array
    /// </summary>
    /// <param name="frame">The frame to encode</param>
    /// <returns>The encoded byte array</returns>
    let encode (frame: Frame): byte[] =
        // Encode header
        let headerBytes, headerSize = encodeHeader frame.Header
        
        // Calculate total size
        let totalSize = headerSize + frame.Payload.Length
        
        // Create result
        let result = Array.zeroCreate totalSize
        
        // Copy header
        Array.Copy(headerBytes, result, headerSize)
        
        // Copy payload
        Array.Copy(frame.Payload, 0, result, headerSize, frame.Payload.Length)
        
        result
    
    /// <summary>
    /// Decodes a frame from a byte array
    /// </summary>
    /// <param name="data">The byte array to decode</param>
    /// <returns>A result containing the decoded frame or an error</returns>
    let decode (data: byte[]): Result<Frame> =
        match decodeHeader data with
        | Ok (header, headerSize) ->
            // Validate payload length
            if data.Length < headerSize + int header.PayloadLength then
                Error (decodingError "Buffer too small for payload")
            else
                // Extract payload
                let payload = Array.zeroCreate (int header.PayloadLength)
                Array.Copy(data, headerSize, payload, 0, int header.PayloadLength)
                
                // Create frame
                let frame = {
                    Header = header
                    Payload = payload
                }
                
                Ok frame
        | Error err -> Error err
    
    /// <summary>
    /// Creates a new frame
    /// </summary>
    /// <param name="messageType">The message type</param>
    /// <param name="payload">The payload data</param>
    /// <param name="headers">Optional headers</param>
    /// <returns>A new frame</returns>
    let create (messageType: MessageType) (payload: byte[]) (headers: HeaderEntry list): Frame =
        let flags = 
            if List.isEmpty headers then FrameFlags.None
            else FrameFlags.HasHeaders
        
        let header = {
            Version = ProtocolVersion
            MessageType = messageType
            Flags = flags
            MessageId = newUuid()
            Timestamp = currentUnixTimestamp()
            PayloadLength = uint32 payload.Length
            Headers = headers
        }
        
        {
            Header = header
            Payload = payload
        }
    
    /// <summary>
    /// Creates a request frame
    /// </summary>
    /// <param name="payload">The payload data</param>
    /// <param name="headers">Optional headers</param>
    /// <returns>A new request frame</returns>
    let createRequest (payload: byte[]) (headers: HeaderEntry list): Frame =
        create MessageType.Request payload headers
    
    /// <summary>
    /// Creates a response frame
    /// </summary>
    /// <param name="requestId">The ID of the request to respond to</param>
    /// <param name="payload">The payload data</param>
    /// <param name="headers">Optional headers</param>
    /// <returns>A new response frame</returns>
    let createResponse (requestId: Uuid) (payload: byte[]) (headers: HeaderEntry list): Frame =
        let flags = 
            if List.isEmpty headers then FrameFlags.None
            else FrameFlags.HasHeaders
        
        let header = {
            Version = ProtocolVersion
            MessageType = MessageType.Response
            Flags = flags
            MessageId = requestId
            Timestamp = currentUnixTimestamp()
            PayloadLength = uint32 payload.Length
            Headers = headers
        }
        
        {
            Header = header
            Payload = payload
        }
    
    /// <summary>
    /// Creates a notification frame
    /// </summary>
    /// <param name="payload">The payload data</param>
    /// <param name="headers">Optional headers</param>
    /// <returns>A new notification frame</returns>
    let createNotification (payload: byte[]) (headers: HeaderEntry list): Frame =
        create MessageType.Notification payload headers
    
    /// <summary>
    /// Creates an error frame
    /// </summary>
    /// <param name="requestId">The ID of the request that caused the error</param>
    /// <param name="payload">The error payload data</param>
    /// <param name="headers">Optional headers</param>
    /// <returns>A new error frame</returns>
    let createError (requestId: Uuid) (payload: byte[]) (headers: HeaderEntry list): Frame =
        let flags = 
            if List.isEmpty headers then FrameFlags.None
            else FrameFlags.HasHeaders
        
        let header = {
            Version = ProtocolVersion
            MessageType = MessageType.Error
            Flags = flags
            MessageId = requestId
            Timestamp = currentUnixTimestamp()
            PayloadLength = uint32 payload.Length
            Headers = headers
        }
        
        {
            Header = header
            Payload = payload
        }
    
    /// <summary>
    /// Adds a header to a frame
    /// </summary>
    /// <param name="frame">The frame to modify</param>
    /// <param name="key">The header key</param>
    /// <param name="value">The header value</param>
    /// <returns>A new frame with the added header</returns>
    let addHeader (frame: Frame) (key: string) (value: string): Frame =
        let header = {
            Key = key
            Value = value
        }
        
        let newHeaders = header :: frame.Header.Headers
        let newFlags = 
            if List.isEmpty frame.Header.Headers then
                frame.Header.Flags ||| FrameFlags.HasHeaders
            else
                frame.Header.Flags
        
        let newHeader = {
            frame.Header with
                Headers = newHeaders
                Flags = newFlags
        }
        
        { frame with Header = newHeader }
    
    /// <summary>
    /// Gets a header value from a frame
    /// </summary>
    /// <param name="frame">The frame to get the header from</param>
    /// <param name="key">The header key</param>
    /// <returns>The header value, or None if not found</returns>
    let getHeader (frame: Frame) (key: string): option<string> =
        frame.Header.Headers
        |> List.tryFind (fun h -> h.Key = key)
        |> Option.map (fun h -> h.Value)