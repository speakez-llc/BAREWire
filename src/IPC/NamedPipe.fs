namespace BAREWire.IPC

open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Core.Memory
open BAREWire.Encoding.Codec
open BAREWire.Platform

/// <summary>
/// Named pipes for inter-process communication
/// </summary>
module NamedPipe =
    /// <summary>
    /// Specifies the direction of data flow in a named pipe
    /// </summary>
    type PipeDirection =
        /// <summary>Read only pipe</summary>
        | In
        /// <summary>Write only pipe</summary>
        | Out
        /// <summary>Bidirectional pipe</summary>
        | InOut
    
    /// <summary>
    /// Specifies the data transfer mode of a named pipe
    /// </summary>
    type PipeMode =
        /// <summary>Transfers data as a stream of bytes</summary>
        | Byte
        /// <summary>Transfers data as discrete messages</summary>
        | Message
    
    /// <summary>
    /// Specifies the security context of a named pipe
    /// </summary>
    type PipeSecurity =
        /// <summary>Accessible only by the current user</summary>
        | CurrentUser
        /// <summary>Accessible by the local system account</summary>
        | LocalSystem
        /// <summary>Accessible by any user on the system</summary>
        | Everyone
    
    /// <summary>
    /// Configuration options for a named pipe
    /// </summary>
    type PipeOptions = {
        /// <summary>
        /// The buffer size in bytes
        /// </summary>
        BufferSize: int<bytes>
        
        /// <summary>
        /// The direction of data flow
        /// </summary>
        Direction: PipeDirection
        
        /// <summary>
        /// The data transfer mode
        /// </summary>
        Mode: PipeMode
        
        /// <summary>
        /// The security context
        /// </summary>
        Security: PipeSecurity
    }
    
    /// <summary>
    /// Default pipe configuration options
    /// </summary>
    let defaultOptions = {
        BufferSize = 4096<bytes>
        Direction = InOut
        Mode = Message
        Security = CurrentUser
    }
    
    /// <summary>
    /// A named pipe for inter-process communication
    /// </summary>
    /// <typeparam name="'T">The message type</typeparam>
    type Pipe<'T> = {
        /// <summary>
        /// The name of the pipe
        /// </summary>
        Name: string
        
        /// <summary>
        /// The schema for serializing messages
        /// </summary>
        Schema: SchemaDefinition<validated>
        
        /// <summary>
        /// The native handle to the pipe
        /// </summary>
        Handle: nativeint
        
        /// <summary>
        /// Whether this is a server or client pipe
        /// </summary>
        IsServer: bool
        
        /// <summary>
        /// The pipe configuration options
        /// </summary>
        Options: PipeOptions
    }
    
    /// <summary>
    /// Creates a new named pipe server
    /// </summary>
    /// <param name="name">The name of the pipe (must be unique)</param>
    /// <param name="schema">The schema for serializing messages</param>
    /// <param name="options">The pipe configuration options</param>
    /// <returns>A result containing the new pipe server or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when pipe creation fails</exception>
    let createServer<'T> 
                    (name: string) 
                    (schema: SchemaDefinition<validated>)
                    (options: PipeOptions): Result<Pipe<'T>> =
        try
            // Ensure platform services are initialized
            if not (PlatformServices.ensureInitialized 10) then
                Error (invalidStateError "Failed to initialize platform services")
            else
                // Get the IPC provider
                PlatformServices.getIpcProvider()
                |> Result.bind (fun provider ->
                    // Create the named pipe using the platform provider
                    provider.CreateNamedPipe(
                        name, 
                        options.Direction, 
                        options.Mode, 
                        options.BufferSize
                    )
                    |> Result.map (fun handle ->
                        {
                            Name = name
                            Schema = schema
                            Handle = handle
                            IsServer = true
                            Options = options
                        }
                    )
                )
        with ex ->
            Error (invalidValueError $"Failed to create named pipe server: {ex.Message}")
    
    /// <summary>
    /// Connects to an existing named pipe
    /// </summary>
    /// <param name="name">The name of the pipe to connect to</param>
    /// <param name="schema">The schema for serializing messages</param>
    /// <param name="options">The pipe configuration options</param>
    /// <returns>A result containing the pipe client or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when connection fails</exception>
    let connect<'T> 
               (name: string) 
               (schema: SchemaDefinition<validated>)
               (options: PipeOptions): Result<Pipe<'T>> =
        try
            // Ensure platform services are initialized
            if not (PlatformServices.ensureInitialized 10) then
                Error (invalidStateError "Failed to initialize platform services")
            else
                // Get the IPC provider
                PlatformServices.getIpcProvider()
                |> Result.bind (fun provider ->
                    // Connect to the named pipe using the platform provider
                    provider.ConnectNamedPipe(
                        name, 
                        options.Direction
                    )
                    |> Result.map (fun handle ->
                        {
                            Name = name
                            Schema = schema
                            Handle = handle
                            IsServer = false
                            Options = options
                        }
                    )
                )
        with ex ->
            Error (invalidValueError $"Failed to connect to named pipe: {ex.Message}")
    
    /// <summary>
    /// Waits for a client to connect to a pipe server
    /// </summary>
    /// <param name="pipe">The pipe server</param>
    /// <returns>A result indicating success or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when the pipe is not a server or when waiting fails</exception>
    let waitForConnection<'T> 
                        (pipe: Pipe<'T>): Result<unit> =
        if not pipe.IsServer then
            Error (invalidValueError "Only a server pipe can wait for connections")
        else
            try
                // Ensure platform services are initialized
                if not (PlatformServices.ensureInitialized 10) then
                    Error (invalidStateError "Failed to initialize platform services")
                else
                    // Get the IPC provider
                    PlatformServices.getIpcProvider()
                    |> Result.bind (fun provider ->
                        // Wait for connection using the platform provider
                        provider.WaitForNamedPipeConnection(
                            pipe.Handle, 
                            -1 // Infinite timeout
                        )
                    )
            with ex ->
                Error (invalidValueError $"Failed to wait for connection: {ex.Message}")
    
    /// <summary>
    /// Sends a message through the pipe
    /// </summary>
    /// <param name="pipe">The pipe to send through</param>
    /// <param name="message">The message to send</param>
    /// <returns>A result indicating success or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when sending fails</exception>
    let send<'T> (pipe: Pipe<'T>) (message: 'T): Result<unit> =
        try
            // Create a buffer for encoding
            let buffer = Buffer<'T>.Create(int pipe.Options.BufferSize)
            
            // Encode the message
            encode pipe.Schema message buffer
            |> Result.bind (fun () ->
                // Ensure platform services are initialized
                if not (PlatformServices.ensureInitialized 10) then
                    Error (invalidStateError "Failed to initialize platform services")
                else
                    // Get the IPC provider
                    PlatformServices.getIpcProvider()
                    |> Result.bind (fun provider ->
                        // Write the encoded message to the pipe
                        provider.WriteNamedPipe(
                            pipe.Handle, 
                            buffer.Data, 
                            0, 
                            buffer.Position |> int
                        )
                        |> Result.map (fun _ -> ())
                    )
            )
        with ex ->
            Error (encodingError $"Failed to send message: {ex.Message}")
    
    /// <summary>
    /// Receives a message from the pipe
    /// </summary>
    /// <param name="pipe">The pipe to receive from</param>
    /// <returns>A result containing the received message (or None if no message is available) or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when receiving fails</exception>
    let receive<'T> (pipe: Pipe<'T>): Result<'T option> =
        try
            // Create a buffer for reading
            let bufferSize = int pipe.Options.BufferSize
            let readBuffer = Array.zeroCreate bufferSize
            
            // Ensure platform services are initialized
            if not (PlatformServices.ensureInitialized 10) then
                Error (invalidStateError "Failed to initialize platform services")
            else
                // Get the IPC provider
                PlatformServices.getIpcProvider()
                |> Result.bind (fun provider ->
                    // Read data from the pipe
                    provider.ReadNamedPipe(
                        pipe.Handle, 
                        readBuffer, 
                        0, 
                        bufferSize
                    )
                    |> Result.bind (fun bytesRead ->
                        if bytesRead = 0 then
                            // No data available
                            Ok None
                        else
                            // Create a buffer from the read data
                            let buffer = {
                                Data = readBuffer
                                Position = bytesRead * 1<offset>
                            }
                            
                            // Decode the message
                            decode<'T> pipe.Schema buffer
                            |> Result.map Some
                    )
                )
        with ex ->
            Error (decodingError $"Failed to receive message: {ex.Message}")
    
    /// <summary>
    /// Closes a pipe
    /// </summary>
    /// <param name="pipe">The pipe to close</param>
    /// <returns>A result indicating success or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when closing fails</exception>
    let close<'T> (pipe: Pipe<'T>): Result<unit> =
        try
            // Ensure platform services are initialized
            if not (PlatformServices.ensureInitialized 10) then
                Error (invalidStateError "Failed to initialize platform services")
            else
                // Get the IPC provider
                PlatformServices.getIpcProvider()
                |> Result.bind (fun provider ->
                    // Close the pipe using the platform provider
                    provider.CloseNamedPipe(pipe.Handle)
                )
        with ex ->
            Error (invalidValueError $"Failed to close pipe: {ex.Message}")
    
    /// <summary>
    /// Flushes the pipe, ensuring all buffered data is sent
    /// </summary>
    /// <param name="pipe">The pipe to flush</param>
    /// <returns>A result indicating success or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when flushing fails</exception>
    let flush<'T> (pipe: Pipe<'T>): Result<unit> =
        // Flushing is not explicitly supported in the platform provider interface
        // Most implementations automatically flush data when writing
        Ok ()
    
    /// <summary>
    /// Checks if a named pipe with the given name exists
    /// </summary>
    /// <param name="name">The name of the pipe to check</param>
    /// <returns>True if the pipe exists, false otherwise</returns>
    let exists (name: string): bool =
        try
            // Ensure platform services are initialized
            if not (PlatformServices.ensureInitialized 10) then
                false
            else
                // Get the IPC provider
                match PlatformServices.getIpcProvider() with
                | Ok provider ->
                    // Check if the resource exists using the platform provider
                    provider.ResourceExists(name, "pipe")
                | Error _ ->
                    false
        with _ ->
            false
    
    /// <summary>
    /// Sends a message through the pipe with a timeout
    /// </summary>
    /// <param name="pipe">The pipe to send through</param>
    /// <param name="message">The message to send</param>
    /// <param name="timeout">The timeout in milliseconds</param>
    /// <returns>A result indicating success or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when sending fails or times out</exception>
    let sendWithTimeout<'T> 
                       (pipe: Pipe<'T>) 
                       (message: 'T) 
                       (timeout: int): Result<unit> =
        // Timeout is not explicitly supported in the platform provider interface
        // We'll just call the regular send function
        send pipe message
    
    /// <summary>
    /// Receives a message from the pipe with a timeout
    /// </summary>
    /// <param name="pipe">The pipe to receive from</param>
    /// <param name="timeout">The timeout in milliseconds</param>
    /// <returns>A result containing the received message (or None if no message is available or timeout occurs) or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when receiving fails</exception>
    let receiveWithTimeout<'T> 
                          (pipe: Pipe<'T>) 
                          (timeout: int): Result<'T option> =
        // Timeout is not explicitly supported in the platform provider interface
        // We'll just call the regular receive function
        receive pipe
    
    /// <summary>
    /// Creates a pair of pipes for bidirectional communication
    /// </summary>
    /// <param name="name">The base name for the pipes</param>
    /// <param name="schema">The schema for serializing messages</param>
    /// <param name="options">The pipe configuration options</param>
    /// <returns>A result containing a tuple of (server pipe, client pipe) or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when pipe creation fails</exception>
    let createDuplexPair<'T> 
                       (name: string) 
                       (schema: SchemaDefinition<validated>)
                       (options: PipeOptions): Result<Pipe<'T> * Pipe<'T>> =
        try
            // Create server-side pipe options
            let serverOptions = { options with Direction = Out }
            
            // Create client-side pipe options
            let clientOptions = { options with Direction = In }
            
            // Create the server pipe
            createServer<'T> $"{name}_server" schema serverOptions
            |> Result.bind (fun serverPipe ->
                // Create the client pipe
                connect<'T> $"{name}_client" schema clientOptions
                |> Result.map (fun clientPipe ->
                    serverPipe, clientPipe
                )
            )
        with ex ->
            Error (invalidValueError $"Failed to create duplex pipe pair: {ex.Message}")