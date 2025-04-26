namespace BAREWire.Network

open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Core.Memory
open BAREWire.Encoding.Codec
open BAREWire.Network.Frame
open BAREWire.Network.Transport

/// <summary>
/// Message passing primitives for network communication
/// </summary>
module Protocol =
    /// <summary>
    /// A protocol message with metadata and payload
    /// </summary>
    /// <typeparam name="'T">The payload type</typeparam>
    type Message<'T> = {
        /// <summary>Unique message identifier</summary>
        Id: Guid
        
        /// <summary>Message type identifier</summary>
        Type: string
        
        /// <summary>Message payload</summary>
        Payload: 'T
        
        /// <summary>Message timestamp</summary>
        Timestamp: int64
    }
    
    /// <summary>
    /// A protocol client with schema-based messaging
    /// </summary>
    /// <typeparam name="'T">The message type</typeparam>
    type Client<'T> = {
        /// <summary>The transport used by the client</summary>
        Transport: ITransport
        
        /// <summary>The schema used for serializing messages</summary>
        Schema: SchemaDefinition<validated>
        
        /// <summary>The unique schema identifier</summary>
        SchemaId: Guid
    }
    
    /// <summary>
    /// Creates a new protocol client
    /// </summary>
    /// <param name="transport">The transport to use</param>
    /// <param name="schema">The schema for serializing messages</param>
    /// <returns>A result containing the client or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when client creation fails</exception>
    let createClient<'T> 
                    (transport: ITransport) 
                    (schema: SchemaDefinition<validated>): Result<Client<'T>> =
        try
            Ok {
                Transport = transport
                Schema = schema
                SchemaId = Guid.NewGuid() // In practice, this would be derived from the schema
            }
        with ex ->
            Error (invalidValueError $"Failed to create client: {ex.Message}")
    
    /// <summary>
    /// Sends a message using the client's transport
    /// </summary>
    /// <param name="client">The client to send with</param>
    /// <param name="message">The message to send</param>
    /// <returns>A result indicating success or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when encoding or sending fails</exception>
    let send<'T> (client: Client<'T>) (message: 'T): Result<unit> =
        try
            // Create a buffer for encoding
            let buffer = Buffer<'T>.Create(8192) // Use a reasonable initial size
            
            // Encode the message
            encode client.Schema message buffer
            |> Result.bind (fun () ->
                // Create the payload
                let payload = Array.init (int buffer.Position) (fun i -> buffer.Data.[i])
                
                // Create the frame
                let frame = Frame.createFrame 
                                (FrameFlags.HasSchema) 
                                (Some client.SchemaId) 
                                payload 
                                None
                
                // Send the frame
                client.Transport.Send(frame)
            )
        with ex ->
            Error (encodingError $"Failed to send message: {ex.Message}")
    
    /// <summary>
    /// Receives a message using the client's transport
    /// </summary>
    /// <param name="client">The client to receive with</param>
    /// <returns>A result containing the received message (or None if no message is available) or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when receiving or decoding fails</exception>
    let receive<'T> (client: Client<'T>): Result<'T option> =
        // Receive a frame
        client.Transport.Receive()
        |> Result.bind (fun frameOpt ->
            match frameOpt with
            | Some frame ->
                // Verify schema ID if present
                if Frame.hasSchemaId frame then
                    Frame.getSchemaId frame
                    |> Result.bind (fun schemaId ->
                        if schemaId <> client.SchemaId then
                            Error (decodingError $"Schema ID mismatch: expected {client.SchemaId}, got {schemaId}")
                        else
                            // Create a memory region from the payload
                            let memory = {
                                Data = frame.Payload
                                Offset = 0<offset>
                                Length = frame.Payload.Length * 1<bytes>
                            }
                            
                            // Decode the message
                            decode<'T> client.Schema memory
                            |> Result.map (fun (message, _) -> Some message)
                    )
                else
                    // No schema ID, just decode the payload
                    let memory = {
                        Data = frame.Payload
                        Offset = 0<offset>
                        Length = frame.Payload.Length * 1<bytes>
                    }
                    
                    // Decode the message
                    decode<'T> client.Schema memory
                    |> Result.map (fun (message, _) -> Some message)
            
            | None ->
                Ok None
        )
    
    /// <summary>
    /// Closes the client's transport
    /// </summary>
    /// <param name="client">The client to close</param>
    /// <returns>A result indicating success or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when closing fails</exception>
    let close<'T> (client: Client<'T>): Result<unit> =
        client.Transport.Close()
    
    /// <summary>
    /// An RPC request message
    /// </summary>
    /// <typeparam name="'P">The parameter type</typeparam>
    type Request<'P> = {
        /// <summary>Unique request identifier</summary>
        Id: Guid
        
        /// <summary>Method name to invoke</summary>
        Method: string
        
        /// <summary>Method parameters</summary>
        Params: 'P
    }
    
    /// <summary>
    /// An RPC response message
    /// </summary>
    /// <typeparam name="'R">The result type</typeparam>
    type Response<'R> = {
        /// <summary>Request identifier that this response is for</summary>
        Id: Guid
        
        /// <summary>Result value if successful</summary>
        Result: 'R option
        
        /// <summary>Error message if failed</summary>
        Error: string option
    }
    
    /// <summary>
    /// An RPC client for request-response communication
    /// </summary>
    /// <typeparam name="'P">The parameter type</typeparam>
    /// <typeparam name="'R">The result type</typeparam>
    type RpcClient<'P, 'R> = {
        /// <summary>The protocol client for sending requests</summary>
        RequestClient: Client<Request<'P>>
        
        /// <summary>The protocol client for receiving responses</summary>
        ResponseClient: Client<Response<'R>>
    }
    
    /// <summary>
    /// Creates a new RPC client
    /// </summary>
    /// <param name="transport">The transport to use</param>
    /// <param name="requestSchema">The schema for serializing requests</param>
    /// <param name="responseSchema">The schema for serializing responses</param>
    /// <returns>A result containing the RPC client or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when client creation fails</exception>
    let createRpcClient<'P, 'R> 
                      (transport: ITransport) 
                      (requestSchema: SchemaDefinition<validated>)
                      (responseSchema: SchemaDefinition<validated>): Result<RpcClient<'P, 'R>> =
        createClient<Request<'P>> transport requestSchema
        |> Result.bind (fun requestClient ->
            createClient<Response<'R>> transport responseSchema
            |> Result.map (fun responseClient ->
                {
                    RequestClient = requestClient
                    ResponseClient = responseClient
                }
            )
        )
    
    /// <summary>
    /// Calls a remote method synchronously
    /// </summary>
    /// <param name="client">The RPC client to use</param>
    /// <param name="method">The method name to call</param>
    /// <param name="params">The method parameters</param>
    /// <returns>A result containing the method result or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when the call fails</exception>
    let call<'P, 'R> 
           (client: RpcClient<'P, 'R>) 
           (method: string) 
           (params: 'P): Result<'R> =
        // Create the request
        let request = {
            Id = Guid.NewGuid()
            Method = method
            Params = params
        }
        
        // Send the request
        send client.RequestClient request
        |> Result.bind (fun () ->
            // Receive the response
            let rec waitForResponse () =
                receive client.ResponseClient
                |> Result.bind (fun responseOpt ->
                    match responseOpt with
                    | Some response ->
                        if response.Id = request.Id then
                            // Check for errors
                            match response.Error with
                            | Some error -> Error (invalidValueError error)
                            | None ->
                                // Return the result
                                match response.Result with
                                | Some result -> Ok result
                                | None -> Error (invalidValueError "No result in response")
                        else
                            // Not our response, wait for another
                            waitForResponse ()
                    | None ->
                        // No response yet, wait and try again
                        // In a real implementation, this would use a timeout
                        waitForResponse ()
                )
            
            waitForResponse ()
        )
    
    /// <summary>
    /// A handler for processing protocol messages
    /// </summary>
    /// <typeparam name="'T">The message payload type</typeparam>
    type MessageHandler<'T> = Message<'T> -> Result<unit>
    
    /// <summary>
    /// A protocol server that processes messages
    /// </summary>
    /// <typeparam name="'T">The message type</typeparam>
    type Server<'T> = {
        /// <summary>The transport used by the server</summary>
        Transport: ITransport
        
        /// <summary>The schema used for serializing messages</summary>
        Schema: SchemaDefinition<validated>
        
        /// <summary>The message handler function</summary>
        Handler: MessageHandler<'T>
    }
    
    /// <summary>
    /// Creates a new protocol server
    /// </summary>
    /// <param name="transport">The transport to use</param>
    /// <param name="schema">The schema for serializing messages</param>
    /// <param name="handler">The message handler function</param>
    /// <returns>A result containing the server or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when server creation fails</exception>
    let createServer<'T> 
                    (transport: ITransport) 
                    (schema: SchemaDefinition<validated>) 
                    (handler: MessageHandler<'T>): Result<Server<'T>> =
        try
            Ok {
                Transport = transport
                Schema = schema
                Handler = handler
            }
        with ex ->
            Error (invalidValueError $"Failed to create server: {ex.Message}")
    
    /// <summary>
    /// Starts the server and begins processing messages
    /// </summary>
    /// <param name="server">The server to start</param>
    /// <returns>A result indicating success or an error</returns>
    /// <remarks>
    /// This is a simplified implementation. In a real implementation, 
    /// this would start a background thread to process messages.
    /// </remarks>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when starting the server fails</exception>
    let start<'T> (server: Server<'T>): Result<unit> =
        // This is a simplified implementation without actual server loop
        // In a real implementation, this would start a background thread
        
        // Create a client for receiving messages
        createClient<'T> server.Transport server.Schema
        |> Result.map (fun client ->
            // Start the server loop
            let rec serverLoop () =
                // Receive a message
                match receive client with
                | Ok (Some message) ->
                    // Handle the message
                    let _ = server.Handler message
                    
                    // Continue the loop
                    serverLoop ()
                    
                | Ok None ->
                    // No message, wait and try again
                    serverLoop ()
                    
                | Error _ ->
                    // Error receiving message, continue the loop
                    serverLoop ()
            
            // In a real implementation, this would be on a background thread
            serverLoop ()
        )
    
    /// <summary>
    /// Stops the server
    /// </summary>
    /// <param name="server">The server to stop</param>
    /// <returns>A result indicating success or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when stopping the server fails</exception>
    let stop<'T> (server: Server<'T>): Result<unit> =
        server.Transport.Close()
    
    /// <summary>
    /// A handler for processing RPC method calls
    /// </summary>
    /// <typeparam name="'P">The parameter type</typeparam>
    /// <typeparam name="'R">The result type</typeparam>
    type MethodHandler<'P, 'R> = string -> 'P -> Result<'R>
    
    /// <summary>
    /// An RPC server that processes method calls
    /// </summary>
    /// <typeparam name="'P">The parameter type</typeparam>
    /// <typeparam name="'R">The result type</typeparam>
    type RpcServer<'P, 'R> = {
        /// <summary>The protocol client for receiving requests</summary>
        RequestClient: Client<Request<'P>>
        
        /// <summary>The protocol client for sending responses</summary>
        ResponseClient: Client<Response<'R>>
        
        /// <summary>The method handler function</summary>
        Handler: MethodHandler<'P, 'R>
    }
    
    /// <summary>
    /// Creates an RPC server
    /// </summary>
    /// <param name="transport">The transport to use</param>
    /// <param name="requestSchema">The schema for serializing requests</param>
    /// <param name="responseSchema">The schema for serializing responses</param>
    /// <param name="handler">The method handler function</param>
    /// <returns>A result containing the RPC server or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when server creation fails</exception>
    let createRpcServer<'P, 'R> 
                      (transport: ITransport)
                      (requestSchema: SchemaDefinition<validated>)
                      (responseSchema: SchemaDefinition<validated>)
                      (handler: MethodHandler<'P, 'R>): Result<RpcServer<'P, 'R>> =
        createClient<Request<'P>> transport requestSchema
        |> Result.bind (fun requestClient ->
            createClient<Response<'R>> transport responseSchema
            |> Result.map (fun responseClient ->
                {
                    RequestClient = requestClient
                    ResponseClient = responseClient
                    Handler = handler
                }
            )
        )
    
    /// <summary>
    /// Starts the RPC server and begins processing method calls
    /// </summary>
    /// <param name="server">The RPC server to start</param>
    /// <returns>A result indicating success or an error</returns>
    /// <remarks>
    /// This is a simplified implementation. In a real implementation, 
    /// this would start a background thread to process method calls.
    /// </remarks>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when starting the server fails</exception>
    let startRpcServer<'P, 'R> (server: RpcServer<'P, 'R>): Result<unit> =
        // This is a simplified implementation without actual server loop
        // In a real implementation, this would start a background thread
        
        // Start the server loop
        let rec serverLoop () =
            // Receive a request
            match receive server.RequestClient with
            | Ok (Some request) ->
                // Handle the request
                let response =
                    match server.Handler request.Method request.Params with
                    | Ok result ->
                        {
                            Id = request.Id
                            Result = Some result
                            Error = None
                        }
                    | Error e ->
                        {
                            Id = request.Id
                            Result = None
                            Error = Some (toString e)
                        }
                
                // Send the response
                let _ = send server.ResponseClient response
                
                // Continue the loop
                serverLoop ()
                
            | Ok None ->
                // No request, wait and try again
                serverLoop ()
                
            | Error _ ->
                // Error receiving request, continue the loop
                serverLoop ()
        
        // In a real implementation, this would be on a background thread
        Ok (serverLoop ())
    
    /// <summary>
    /// Stops the RPC server
    /// </summary>
    /// <param name="server">The RPC server to stop</param>
    /// <returns>A result indicating success or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when stopping the server fails</exception>
    let stopRpcServer<'P, 'R> (server: RpcServer<'P, 'R>): Result<unit> =
        server.RequestClient.Transport.Close()