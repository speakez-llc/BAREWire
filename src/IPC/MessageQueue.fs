namespace BAREWire.IPC

open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.IPC.SharedMemory

/// <summary>
/// Message queues for inter-process communication
/// </summary>
module MessageQueue =
    /// <summary>
    /// A message with a header and payload
    /// </summary>
    /// <typeparam name="'T">The payload type</typeparam>
    type Message<'T> = {
        /// <summary>
        /// Unique message identifier
        /// </summary>
        Id: Guid
        
        /// <summary>
        /// Message type identifier
        /// </summary>
        Type: string
        
        /// <summary>
        /// Message payload
        /// </summary>
        Payload: 'T
        
        /// <summary>
        /// Message timestamp (Unix time)
        /// </summary>
        Timestamp: int64
    }
    
    /// <summary>
    /// Message queue internal state
    /// </summary>
    /// <typeparam name="'T">The message payload type</typeparam>
    type QueueState<'T> = {
        /// <summary>
        /// Messages stored in the queue
        /// </summary>
        Messages: Message<'T> list
        
        /// <summary>
        /// Index of the first message to read
        /// </summary>
        Head: int
        
        /// <summary>
        /// Index of the next position to write
        /// </summary>
        Tail: int
        
        /// <summary>
        /// Maximum number of messages the queue can store
        /// </summary>
        Capacity: int
    }
    
    /// <summary>
    /// A message queue for inter-process communication
    /// </summary>
    /// <typeparam name="'T">The message payload type</typeparam>
    /// <typeparam name="'region">The memory region measure type</typeparam>
    type Queue<'T, [<Measure>] 'region> = {
        /// <summary>
        /// The shared memory region containing the queue state
        /// </summary>
        SharedRegion: SharedRegion<QueueState<'T>, 'region>
        
        /// <summary>
        /// The schema for serializing messages
        /// </summary>
        Schema: SchemaDefinition<validated>
    }
    
    /// <summary>
    /// Creates a new message queue
    /// </summary>
    /// <param name="name">The name of the queue (must be unique)</param>
    /// <param name="capacity">The maximum number of messages the queue can hold</param>
    /// <param name="schema">The schema for serializing messages</param>
    /// <returns>A result containing the new queue or an error</returns>
    let create<'T> 
              (name: string) 
              (capacity: int) 
              (schema: SchemaDefinition<validated>): Result<Queue<'T, region>> =
        let stateSize = 4096<bytes> // Fixed size for the queue state
        
        SharedMemory.create<QueueState<'T>> name stateSize schema
        |> Result.map (fun sharedRegion ->
            // Initialize the queue
            let view = SharedMemory.getView<QueueState<'T>, region> sharedRegion schema
            
            let initialState = {
                Messages = []
                Head = 0
                Tail = 0
                Capacity = capacity
            }
            
            let setResult = View.setField<QueueState<'T>, QueueState<'T>, region> view ["QueueState"] initialState
            
            match setResult with
            | Ok () -> 
                {
                    SharedRegion = sharedRegion
                    Schema = schema
                }
            | Error e -> 
                // Log error and return empty queue
                {
                    SharedRegion = sharedRegion
                    Schema = schema
                }
        )
    
    /// <summary>
    /// Opens an existing message queue
    /// </summary>
    /// <param name="name">The name of the queue to open</param>
    /// <param name="schema">The schema for serializing messages</param>
    /// <returns>A result containing the opened queue or an error</returns>
    let open'<'T> 
              (name: string) 
              (schema: SchemaDefinition<validated>): Result<Queue<'T, region>> =
        SharedMemory.open'<QueueState<'T>> name schema
        |> Result.map (fun sharedRegion ->
            {
                SharedRegion = sharedRegion
                Schema = schema
            }
        )
    
    /// <summary>
    /// Enqueues a message to the queue
    /// </summary>
    /// <param name="queue">The message queue</param>
    /// <param name="msgType">The type of the message</param>
    /// <param name="payload">The message payload</param>
    /// <returns>A result indicating success or an error</returns>
    /// <exception cref="BAREWire.Core.Error.Error">Thrown when the queue is full or an error occurs during enqueuing</exception>
    let enqueue<'T, [<Measure>] 'region> 
               (queue: Queue<'T, 'region>) 
               (msgType: string) 
               (payload: 'T): Result<unit> =
        // Lock the queue
        SharedMemory.lock queue.SharedRegion
        |> Result.bind (fun () ->
            // Get the current state
            let view = SharedMemory.getView<QueueState<'T>, 'region> queue.SharedRegion queue.Schema
            
            View.getField<QueueState<'T>, QueueState<'T>, 'region> view ["QueueState"]
            |> Result.bind (fun state ->
                // Check if the queue is full
                if state.Tail - state.Head >= state.Capacity then
                    SharedMemory.unlock queue.SharedRegion
                    |> Result.map (fun () -> Error (invalidValueError "Queue is full"))
                else
                    // Create the message
                    let message = {
                        Id = Guid.NewGuid()
                        Type = msgType
                        Payload = payload
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    }
                    
                    // Add the message to the queue
                    let newMessages = message :: state.Messages
                    
                    // Update the state
                    let newState = {
                        state with
                            Messages = newMessages
                            Tail = state.Tail + 1
                    }
                    
                    // Update the shared memory
                    View.setField<QueueState<'T>, QueueState<'T>, 'region> view ["QueueState"] newState
                    |> Result.bind (fun () ->
                        // Unlock the queue
                        SharedMemory.unlock queue.SharedRegion
                    )
            )
        )
    
    /// <summary>
    /// Dequeues a message from the queue
    /// </summary>
    /// <param name="queue">The message queue</param>
    /// <returns>A result containing the dequeued message (or None if the queue is empty) or an error</returns>
    let dequeue<'T, [<Measure>] 'region> 
               (queue: Queue<'T, 'region>): Result<Message<'T> option> =
        // Lock the queue
        SharedMemory.lock queue.SharedRegion
        |> Result.bind (fun () ->
            // Get the current state
            let view = SharedMemory.getView<QueueState<'T>, 'region> queue.SharedRegion queue.Schema
            
            View.getField<QueueState<'T>, QueueState<'T>, 'region> view ["QueueState"]
            |> Result.bind (fun state ->
                // Check if the queue is empty
                if state.Head >= state.Tail then
                    SharedMemory.unlock queue.SharedRegion
                    |> Result.map (fun () -> Ok None)
                else
                    // Get the oldest message
                    match List.tryLast state.Messages with
                    | None -> 
                        SharedMemory.unlock queue.SharedRegion
                        |> Result.map (fun () -> Ok None)
                    | Some message ->
                        // Remove the message from the queue
                        let newMessages = 
                            state.Messages 
                            |> List.filter (fun m -> m.Id <> message.Id)
                        
                        // Update the state
                        let newState = {
                            state with
                                Messages = newMessages
                                Head = state.Head + 1
                        }
                        
                        // Update the shared memory
                        View.setField<QueueState<'T>, QueueState<'T>, 'region> view ["QueueState"] newState
                        |> Result.bind (fun () ->
                            // Unlock the queue
                            SharedMemory.unlock queue.SharedRegion
                            |> Result.map (fun () -> Ok (Some message))
                        )
            )
        )
    
    /// <summary>
    /// Peeks at the next message without removing it from the queue
    /// </summary>
    /// <param name="queue">The message queue</param>
    /// <returns>A result containing the next message (or None if the queue is empty) or an error</returns>
    let peek<'T, [<Measure>] 'region> 
            (queue: Queue<'T, 'region>): Result<Message<'T> option> =
        // Lock the queue
        SharedMemory.lock queue.SharedRegion
        |> Result.bind (fun () ->
            // Get the current state
            let view = SharedMemory.getView<QueueState<'T>, 'region> queue.SharedRegion queue.Schema
            
            View.getField<QueueState<'T>, QueueState<'T>, 'region> view ["QueueState"]
            |> Result.bind (fun state ->
                // Check if the queue is empty
                if state.Head >= state.Tail then
                    SharedMemory.unlock queue.SharedRegion
                    |> Result.map (fun () -> Ok None)
                else
                    // Get the oldest message
                    let message = List.tryLast state.Messages
                    
                    // Unlock the queue
                    SharedMemory.unlock queue.SharedRegion
                    |> Result.map (fun () -> Ok message)
            )
        )
    
    /// <summary>
    /// Gets the number of messages in the queue
    /// </summary>
    /// <param name="queue">The message queue</param>
    /// <returns>A result containing the message count or an error</returns>
    let count<'T, [<Measure>] 'region> 
             (queue: Queue<'T, 'region>): Result<int> =
        // Lock the queue
        SharedMemory.lock queue.SharedRegion
        |> Result.bind (fun () ->
            // Get the current state
            let view = SharedMemory.getView<QueueState<'T>, 'region> queue.SharedRegion queue.Schema
            
            View.getField<QueueState<'T>, QueueState<'T>, 'region> view ["QueueState"]
            |> Result.bind (fun state ->
                // Calculate the count
                let count = state.Tail - state.Head
                
                // Unlock the queue
                SharedMemory.unlock queue.SharedRegion
                |> Result.map (fun () -> Ok count)
            )
        )
    
    /// <summary>
    /// Checks if the queue is empty
    /// </summary>
    /// <param name="queue">The message queue</param>
    /// <returns>A result containing a boolean indicating whether the queue is empty or an error</returns>
    let isEmpty<'T, [<Measure>] 'region> 
               (queue: Queue<'T, 'region>): Result<bool> =
        count queue
        |> Result.map (fun countResult ->
            match countResult with
            | Ok count -> Ok (count = 0)
            | Error e -> Error e
        )
    
    /// <summary>
    /// Clears all messages from the queue
    /// </summary>
    /// <param name="queue">The message queue</param>
    /// <returns>A result indicating success or an error</returns>
    let clear<'T, [<Measure>] 'region> 
             (queue: Queue<'T, 'region>): Result<unit> =
        // Lock the queue
        SharedMemory.lock queue.SharedRegion
        |> Result.bind (fun () ->
            // Get the current state
            let view = SharedMemory.getView<QueueState<'T>, 'region> queue.SharedRegion queue.Schema
            
            View.getField<QueueState<'T>, QueueState<'T>, 'region> view ["QueueState"]
            |> Result.bind (fun state ->
                // Create a new empty state
                let newState = {
                    state with
                        Messages = []
                        Head = 0
                        Tail = 0
                }
                
                // Update the shared memory
                View.setField<QueueState<'T>, QueueState<'T>, 'region> view ["QueueState"] newState
                |> Result.bind (fun () ->
                    // Unlock the queue
                    SharedMemory.unlock queue.SharedRegion
                )
            )
        )
    
    /// <summary>
    /// Closes a message queue
    /// </summary>
    /// <param name="queue">The message queue to close</param>
    /// <returns>A result indicating success or an error</returns>
    let close<'T, [<Measure>] 'region> 
             (queue: Queue<'T, 'region>): Result<unit> =
        SharedMemory.close queue.SharedRegion