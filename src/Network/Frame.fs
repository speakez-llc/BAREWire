namespace BAREWire.Network

open BAREWire.Core
open BAREWire.Core.Error

/// <summary>
/// Frame format for binary communication
/// </summary>
module Frame =
    /// <summary>
    /// First magic byte that identifies a BAREWire frame (0xBA)
    /// </summary>
    [<Literal>]
    let private MagicByte1 = 0xBAuy
    
    /// <summary>
    /// Second magic byte that identifies a BAREWire frame (0xRE)
    /// </summary>
    [<Literal>]
    let private MagicByte2 = 0xREuy
    
    /// <summary>
    /// Protocol version (currently 1)
    /// </summary>
    [<Literal>]
    let Version = 1uy
    
    /// <summary>
    /// Flags that can be set on a frame to indicate special properties
    /// </summary>
    [<Flags>]
    type FrameFlags =
        /// <summary>No flags set</summary>
        | None = 0uy
        /// <summary>Payload is compressed</summary>
        | Compressed = 1uy
        /// <summary>Payload is encrypted</summary>
        | Encrypted = 2uy
        /// <summary>Frame has an extended header</summary>
        | ExtendedHeader = 4uy
        /// <summary>Frame includes a schema ID</summary>
        | HasSchema = 8uy
    
    /// <summary>
    /// The header portion of a frame that describes the payload
    /// </summary>
    type FrameHeader = {
        /// <summary>Magic bytes (0xBARE) that identify this as a BAREWire frame</summary>
        Magic: byte[] // 2 bytes
        
        /// <summary>Protocol version</summary>
        Version: byte // 1 byte
        
        /// <summary>Frame flags indicating special properties</summary>
        Flags: FrameFlags // 1 byte
        
        /// <summary>Schema ID, if HasSchema flag is set</summary>
        SchemaId: Guid option // 16 bytes
        
        /// <summary>Length of the payload in bytes</summary>
        PayloadLength: uint32 // 4 bytes
        
        /// <summary>Extended header data, if ExtendedHeader flag is set</summary>
        ExtendedHeader: byte[] option
    }
    
    /// <summary>
    /// A complete frame with header and payload
    /// </summary>
    type Frame = {
        /// <summary>The frame header</summary>
        Header: FrameHeader
        
        /// <summary>The frame payload</summary>
        Payload: byte[]
    }
    
    /// <summary>
    /// Creates a new frame header
    /// </summary>
    /// <param name="flags">Flags to set on the header</param>
    /// <param name="schemaId">Optional schema ID</param>
    /// <param name="payloadLength">Length of the payload in bytes</param>
    /// <param name="extendedHeader">Optional extended header data</param>
    /// <returns>A new frame header</returns>
    let createHeader 
        (flags: FrameFlags) 
        (schemaId: Guid option) 
        (payloadLength: uint32) 
        (extendedHeader: byte[] option): FrameHeader =
        
        {
            Magic = [| MagicByte1; MagicByte2 |]
            Version = Version
            Flags = flags
            SchemaId = schemaId
            PayloadLength = payloadLength
            ExtendedHeader = extendedHeader
        }
    
    /// <summary>
    /// Creates a new frame with the given properties
    /// </summary>
    /// <param name="flags">Flags to set on the frame</param>
    /// <param name="schemaId">Optional schema ID</param>
    /// <param name="payload">Frame payload data</param>
    /// <param name="extendedHeader">Optional extended header data</param>
    /// <returns>A new frame</returns>
    let createFrame 
        (flags: FrameFlags) 
        (schemaId: Guid option) 
        (payload: byte[]) 
        (extendedHeader: byte[] option): Frame =
        
        let header = createHeader flags schemaId (uint32 payload.Length) extendedHeader
        { Header = header; Payload = payload }
    
    /// <summary>
    /// Encodes a frame to a byte array for transmission
    /// </summary>
    /// <param name="frame">The frame to encode</param>
    /// <returns>A result containing the encoded byte array or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when encoding fails</exception>
    let encodeFrame (frame: Frame): Result<byte[]> =
        try
            // Calculate total length
            let headerSize = 
                2 + // Magic
                1 + // Version
                1 + // Flags
                (if frame.Header.SchemaId.IsSome then 16 else 0) + // Schema ID
                4 + // Payload length
                (match frame.Header.ExtendedHeader with 
                 | Some ext -> ext.Length 
                 | None -> 0) // Extended header
            
            let totalSize = headerSize + frame.Payload.Length
            
            // Create the buffer
            let buffer = Array.zeroCreate totalSize
            let position = ref 0
            
            // Write magic bytes
            Array.Copy(frame.Header.Magic, 0, buffer, !position, 2)
            position := !position + 2
            
            // Write version
            buffer.[!position] <- frame.Header.Version
            position := !position + 1
            
            // Write flags
            buffer.[!position] <- byte frame.Header.Flags
            position := !position + 1
            
            // Write schema ID if present
            if frame.Header.SchemaId.IsSome then
                let schemaIdBytes = frame.Header.SchemaId.Value.ToByteArray()
                Array.Copy(schemaIdBytes, 0, buffer, !position, 16)
                position := !position + 16
            
            // Write payload length
            let lengthBytes = BitConverter.GetBytes(frame.Header.PayloadLength)
            Array.Copy(lengthBytes, 0, buffer, !position, 4)
            position := !position + 4
            
            // Write extended header if present
            match frame.Header.ExtendedHeader with
            | Some extHeader ->
                Array.Copy(extHeader, 0, buffer, !position, extHeader.Length)
                position := !position + extHeader.Length
            | None -> ()
            
            // Write payload
            Array.Copy(frame.Payload, 0, buffer, !position, frame.Payload.Length)
            
            Ok buffer
        with ex ->
            Error (encodingError $"Failed to encode frame: {ex.Message}")
    
    /// <summary>
    /// Decodes a frame from a byte array
    /// </summary>
    /// <param name="data">The byte array to decode</param>
    /// <returns>A result containing the decoded frame or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when decoding fails</exception>
    let decodeFrame (data: byte[]): Result<Frame> =
        try
            // Check minimum size
            if data.Length < 8 then
                Error (decodingError "Frame too small")
            else
                // Check magic bytes
                if data.[0] <> MagicByte1 || data.[1] <> MagicByte2 then
                    Error (decodingError "Invalid magic bytes")
                else
                    // Read version
                    let version = data.[2]
                    if version <> Version then
                        Error (decodingError $"Unsupported version: {version}")
                    else
                        // Read flags
                        let flags = enum<FrameFlags>(int data.[3])
                        
                        // Calculate header size
                        let baseHeaderSize = 2 + 1 + 1 + 4 // Magic, Version, Flags, PayloadLength
                        let hasSchemaId = flags.HasFlag(FrameFlags.HasSchema)
                        let hasExtendedHeader = flags.HasFlag(FrameFlags.ExtendedHeader)
                        
                        let position = ref 4 // Start after Magic, Version, Flags
                        
                        // Read schema ID if present
                        let schemaId =
                            if hasSchemaId then
                                if data.Length < !position + 16 then
                                    return Error (decodingError "Frame too small for schema ID")
                                else
                                    let idBytes = data.[!position..(!position + 15)]
                                    position := !position + 16
                                    Some (Guid(idBytes))
                            else
                                None
                        
                        // Read payload length
                        if data.Length < !position + 4 then
                            Error (decodingError "Frame too small for payload length")
                        else
                            let lengthBytes = data.[!position..(!position + 3)]
                            let payloadLength = BitConverter.ToUInt32(lengthBytes, 0)
                            position := !position + 4
                            
                            // Read extended header if present
                            let extendedHeader =
                                if hasExtendedHeader then
                                    // Extended header format would be defined here
                                    // For simplicity, we assume it's not present
                                    None
                                else
                                    None
                            
                            // Read payload
                            if data.Length < !position + int payloadLength then
                                Error (decodingError "Frame too small for payload")
                            else
                                let payload = Array.init (int payloadLength) (fun i -> data.[!position + i])
                                
                                // Create the frame
                                let frame = {
                                    Header = {
                                        Magic = [| MagicByte1; MagicByte2 |]
                                        Version = version
                                        Flags = flags
                                        SchemaId = schemaId
                                        PayloadLength = payloadLength
                                        ExtendedHeader = extendedHeader
                                    }
                                    Payload = payload
                                }
                                
                                Ok frame
        with ex ->
            Error (decodingError $"Failed to decode frame: {ex.Message}")
    
    /// <summary>
    /// Checks if a frame has the Compressed flag set
    /// </summary>
    /// <param name="frame">The frame to check</param>
    /// <returns>True if the frame is compressed, false otherwise</returns>
    let isCompressed (frame: Frame): bool =
        frame.Header.Flags.HasFlag(FrameFlags.Compressed)
    
    /// <summary>
    /// Checks if a frame has the Encrypted flag set
    /// </summary>
    /// <param name="frame">The frame to check</param>
    /// <returns>True if the frame is encrypted, false otherwise</returns>
    let isEncrypted (frame: Frame): bool =
        frame.Header.Flags.HasFlag(FrameFlags.Encrypted)
    
    /// <summary>
    /// Checks if a frame has the HasSchema flag set
    /// </summary>
    /// <param name="frame">The frame to check</param>
    /// <returns>True if the frame has a schema ID, false otherwise</returns>
    let hasSchemaId (frame: Frame): bool =
        frame.Header.Flags.HasFlag(FrameFlags.HasSchema)
    
    /// <summary>
    /// Checks if a frame has the ExtendedHeader flag set
    /// </summary>
    /// <param name="frame">The frame to check</param>
    /// <returns>True if the frame has an extended header, false otherwise</returns>
    let hasExtendedHeader (frame: Frame): bool =
        frame.Header.Flags.HasFlag(FrameFlags.ExtendedHeader)
    
    /// <summary>
    /// Gets the payload length of a frame
    /// </summary>
    /// <param name="frame">The frame to get the payload length from</param>
    /// <returns>The payload length in bytes</returns>
    let getPayloadLength (frame: Frame): uint32 =
        frame.Header.PayloadLength
    
    /// <summary>
    /// Gets the schema ID of a frame
    /// </summary>
    /// <param name="frame">The frame to get the schema ID from</param>
    /// <returns>A result containing the schema ID or an error if the frame doesn't have one</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when the frame doesn't have a schema ID</exception>
    let getSchemaId (frame: Frame): Result<Guid> =
        match frame.Header.SchemaId with
        | Some id -> Ok id
        | None -> Error (invalidValueError "Frame does not have a schema ID")