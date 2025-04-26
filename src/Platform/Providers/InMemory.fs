namespace BAREWire.Platform.Providers

open FSharp.NativeInterop
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Core.Memory
open BAREWire.Core.Time
open BAREWire.Core.Uuid
open BAREWire.IPC
open BAREWire.Memory.Mapping
open BAREWire.Platform.Common.Interfaces

#nowarn "9" // Disable warning about using NativePtr and fixed expressions

/// <summary>
/// In-memory simulation provider for platforms without native support
/// </summary>
module InMemory =
    /// <summary>
    /// Custom handle type to replace System.IntPtr
    /// </summary>
    type Handle = int64
    
    /// <summary>
    /// Null handle (equivalent to IntPtr.Zero)
    /// </summary>
    let NULL_HANDLE = 0L
    
    /// <summary>
    /// Invalid handle (equivalent to INVALID_HANDLE_VALUE)
    /// </summary>
    let INVALID_HANDLE = -1L
    
    /// <summary>
    /// Simple buffer implementation to replace ResizeArray
    /// </summary>
    type Buffer private (initialCapacity: int) =
        let mutable data = Array.zeroCreate<byte> initialCapacity
        let mutable count = 0
        
        /// <summary>Gets the current count of elements</summary>
        member _.Count = count
        
        /// <summary>Gets the underlying data array</summary>
        member _.Data = data
        
        /// <summary>Gets an element at the specified index</summary>
        member _.Item
            with get(index: int): byte =
                if index < 0 || index >= count then
                    failwith "Index out of range"
                data.[index]
                
        /// <summary>Adds an element to the buffer</summary>
        member _.Add(item: byte): unit =
            // Resize if needed
            if count = data.Length then
                let newSize = max 4 (data.Length * 2)
                let newData = Array.zeroCreate<byte> newSize
                Array.Copy(data, newData, count)
                data <- newData
                
            data.[count] <- item
            count <- count + 1
            
        /// <summary>Removes elements from the buffer</summary>
        member _.RemoveRange(index: int, count': int): unit =
            if index < 0 || count' < 0 || index + count' > count then
                failwith "Invalid range"
                
            // Shift elements
            for i = index to count - count' - 1 do
                data.[i] <- data.[i + count']
                
            count <- count - count'
            
        /// <summary>Creates a new buffer</summary>
        static member Create(capacity: int): Buffer =
            Buffer(capacity)
    
    /// <summary>
    /// In-memory storage for simulating platform-specific resources
    /// </summary>
    module private Storage =
        /// <summary>Memory mapping information</summary>
        type MemoryMapping = {
            /// <summary>The simulated handle</summary>
            Handle: Handle
            
            /// <summary>The memory data</summary>
            Data: byte[]
            
            /// <summary>The base address</summary>
            Address: Handle
            
            /// <summary>The mapping type</summary>
            MappingType: MappingType
            
            /// <summary>The access type</summary>
            AccessType: AccessType
        }
        
        /// <summary>Named pipe information</summary>
        type NamedPipe = {
            /// <summary>The simulated handle</summary>
            Handle: Handle
            
            /// <summary>The pipe name</summary>
            Name: string
            
            /// <summary>The buffer for pipe data</summary>
            Buffer: Buffer
            
            /// <summary>The direction of data flow</summary>
            Direction: NamedPipe.PipeDirection
            
            /// <summary>The data transfer mode</summary>
            Mode: NamedPipe.PipeMode
            
            /// <summary>Whether the pipe is connected</summary>
            mutable IsConnected: bool
        }
        
        /// <summary>Shared memory information</summary>
        type SharedMemory = {
            /// <summary>The simulated handle</summary>
            Handle: Handle
            
            /// <summary>The shared memory name</summary>
            Name: string
            
            /// <summary>The memory data</summary>
            Data: byte[]
            
            /// <summary>The base address</summary>
            Address: Handle
            
            /// <summary>The access type</summary>
            AccessType: AccessType
        }
        
        /// <summary>Mutex information</summary>
        type Mutex = {
            /// <summary>The simulated handle</summary>
            Handle: Handle
            
            /// <summary>The mutex name</summary>
            Name: string
            
            /// <summary>Whether the mutex is owned</summary>
            mutable IsOwned: bool
            
            /// <summary>The owner thread ID, if owned</summary>
            mutable OwnerThreadId: int option
        }
        
        /// <summary>Semaphore information</summary>
        type Semaphore = {
            /// <summary>The simulated handle</summary>
            Handle: Handle
            
            /// <summary>The semaphore name</summary>
            Name: string
            
            /// <summary>The current count</summary>
            mutable CurrentCount: int
            
            /// <summary>The maximum count</summary>
            MaximumCount: int
        }

        /// <summary>Socket information</summary>
        type Socket = {
            /// <summary>The simulated handle</summary>
            Handle: Handle
            
            /// <summary>The address family</summary>
            AddressFamily: int
            
            /// <summary>The socket type</summary>
            SocketType: int
            
            /// <summary>The protocol type</summary>
            ProtocolType: int
            
            /// <summary>Whether the socket is bound</summary>
            mutable IsBound: bool
            
            /// <summary>Whether the socket is listening</summary>
            mutable IsListening: bool
            
            /// <summary>Whether the socket is connected</summary>
            mutable IsConnected: bool
            
            /// <summary>The local address</summary>
            mutable LocalAddress: string
            
            /// <summary>The local port</summary>
            mutable LocalPort: int
            
            /// <summary>The remote address</summary>
            mutable RemoteAddress: string
            
            /// <summary>The remote port</summary>
            mutable RemotePort: int
            
            /// <summary>The data buffer</summary>
            Buffer: Buffer
        }
        
        /// <summary>Active memory mappings</summary>
        let mutable memoryMappings: Map<Handle, MemoryMapping> = Map.empty
        
        /// <summary>Active file mappings</summary>
        let mutable fileMappings: Map<Handle, MemoryMapping> = Map.empty
        
        /// <summary>Active named pipes</summary>
        let mutable namedPipes: Map<string, NamedPipe> = Map.empty
        
        /// <summary>Active named pipe handles</summary>
        let mutable pipeHandles: Map<Handle, string> = Map.empty
        
        /// <summary>Active shared memory regions</summary>
        let mutable sharedMemory: Map<string, SharedMemory> = Map.empty
        
        /// <summary>Active shared memory handles</summary>
        let mutable sharedMemoryHandles: Map<Handle, string> = Map.empty
        
        /// <summary>Active mutexes</summary>
        let mutable mutexes: Map<string, Mutex> = Map.empty
        
        /// <summary>Active mutex handles</summary>
        let mutable mutexHandles: Map<Handle, string> = Map.empty
        
        /// <summary>Active semaphores</summary>
        let mutable semaphores: Map<string, Semaphore> = Map.empty
        
        /// <summary>Active semaphore handles</summary>
        let mutable semaphoreHandles: Map<Handle, string> = Map.empty
        
        /// <summary>Active sockets</summary>
        let mutable sockets: Map<Handle, Socket> = Map.empty
        
        /// <summary>Next handle value (incremented for each new handle)</summary>
        let mutable nextHandle = 1L
        
        /// <summary>Gets the next unique handle</summary>
        let getNextHandle () =
            let handle = nextHandle
            nextHandle <- nextHandle + 1L
            handle
        
        /// <summary>Lock object for thread synchronization</summary>
        let syncLock = obj()
        
        /// <summary>Performs a thread-safe operation on storage</summary>
        let withLock f =
            lock syncLock f
    
    /// <summary>
    /// In-memory implementation of the memory provider
    /// </summary>
    type InMemoryMemoryProvider() =
        interface IPlatformMemory with
            member this.MapMemory size mappingType accessType =
                Storage.withLock (fun () ->
                    try
                        // Allocate memory
                        let data = Array.zeroCreate (int size)
                        let handle = Storage.getNextHandle()
                        
                        // In a real implementation, we would pin the array to get a fixed address
                        // Here we just use the handle as the address for simplicity
                        let address = handle
                        
                        // Store mapping information
                        let mapping = {
                            Handle = handle
                            Data = data
                            Address = address
                            MappingType = mappingType
                            AccessType = accessType
                        }
                        
                        Storage.memoryMappings <- Storage.memoryMappings.Add(handle, mapping)
                        
                        Ok (handle, address)
                    with ex ->
                        Error (invalidValueError $"Failed to map memory: {ex.Message}")
                )
            
            member this.UnmapMemory handle address size =
                Storage.withLock (fun () ->
                    try
                        // Find and remove the mapping
                        match Storage.memoryMappings.TryFind(handle) with
                        | Some mapping ->
                            // Remove the mapping
                            Storage.memoryMappings <- Storage.memoryMappings.Remove(handle)
                            Ok ()
                        | None ->
                            Error (invalidValueError $"Invalid memory mapping handle: {handle}")
                    with ex ->
                        Error (invalidValueError $"Failed to unmap memory: {ex.Message}")
                )
            
            member this.MapFile filePath offset size accessType =
                Storage.withLock (fun () ->
                    try
                        // Simulate file mapping by reading the file into memory
                        // In a real implementation, this would use memory-mapped files
                        let data = Array.zeroCreate (int size)
                        let handle = Storage.getNextHandle()
                        let address = handle
                        
                        // Store mapping information
                        let mapping = {
                            Handle = handle
                            Data = data
                            Address = address
                            MappingType = MappingType.PrivateMapping
                            AccessType = accessType
                        }
                        
                        Storage.fileMappings <- Storage.fileMappings.Add(handle, mapping)
                        
                        Ok (handle, address)
                    with ex ->
                        Error (invalidValueError $"Failed to map file: {ex.Message}")
                )
            
            member this.FlushMappedFile handle address size =
                Storage.withLock (fun () ->
                    try
                        // Find the file mapping
                        match Storage.fileMappings.TryFind(handle) with
                        | Some mapping ->
                            // In a real implementation, this would flush changes to disk
                            // For in-memory simulation, there's nothing to do
                            Ok ()
                        | None ->
                            Error (invalidValueError $"Invalid file mapping handle: {handle}")
                    with ex ->
                        Error (invalidValueError $"Failed to flush mapped file: {ex.Message}")
                )
            
            member this.LockMemory address size =
                // In-memory simulation doesn't need to lock memory since there's no swapping
                Ok ()
            
            member this.UnlockMemory address size =
                // In-memory simulation doesn't need to unlock memory
                Ok ()
    
    /// <summary>
    /// In-memory implementation of the IPC provider
    /// </summary>
    type InMemoryIpcProvider() =
        interface IPlatformIpc with
            member this.CreateNamedPipe name direction mode bufferSize =
                Storage.withLock (fun () ->
                    try
                        // Check if the pipe already exists
                        if Storage.namedPipes.ContainsKey(name) then
                            Error (invalidValueError $"Named pipe already exists: {name}")
                        else
                            // Create a new pipe
                            let handle = Storage.getNextHandle()
                            
                            let pipe = {
                                Handle = handle
                                Name = name
                                Buffer = Buffer.Create(int bufferSize)
                                Direction = direction
                                Mode = mode
                                IsConnected = false
                            }
                            
                            // Store the pipe information
                            Storage.namedPipes <- Storage.namedPipes.Add(name, pipe)
                            Storage.pipeHandles <- Storage.pipeHandles.Add(handle, name)
                            
                            Ok handle
                    with ex ->
                        Error (invalidValueError $"Failed to create named pipe: {ex.Message}")
                )
            
            member this.ConnectNamedPipe name direction =
                Storage.withLock (fun () ->
                    try
                        // Find the pipe
                        match Storage.namedPipes.TryFind(name) with
                        | Some pipe ->
                            // Create a client handle
                            let handle = Storage.getNextHandle()
                            
                            // Mark the pipe as connected
                            let updatedPipe = { pipe with IsConnected = true }
                            Storage.namedPipes <- Storage.namedPipes.Add(name, updatedPipe)
                            
                            // Store the client handle
                            Storage.pipeHandles <- Storage.pipeHandles.Add(handle, name)
                            
                            Ok handle
                        | None ->
                            Error (invalidValueError $"Named pipe not found: {name}")
                    with ex ->
                        Error (invalidValueError $"Failed to connect to named pipe: {ex.Message}")
                )
            
            member this.WaitForNamedPipeConnection handle timeout =
                Storage.withLock (fun () ->
                    try
                        // Find the pipe name from the handle
                        match Storage.pipeHandles.TryFind(handle) with
                        | Some pipeName ->
                            // Find the pipe
                            match Storage.namedPipes.TryFind(pipeName) with
                            | Some pipe ->
                                // Mark the pipe as connected
                                let updatedPipe = { pipe with IsConnected = true }
                                Storage.namedPipes <- Storage.namedPipes.Add(pipeName, updatedPipe)
                                
                                Ok ()
                            | None ->
                                Error (invalidValueError $"Named pipe not found: {pipeName}")
                        | None ->
                            Error (invalidValueError $"Invalid pipe handle: {handle}")
                    with ex ->
                        Error (invalidValueError $"Failed to wait for pipe connection: {ex.Message}")
                )
            
            member this.WriteNamedPipe handle data offset count =
                Storage.withLock (fun () ->
                    try
                        // Find the pipe name from the handle
                        match Storage.pipeHandles.TryFind(handle) with
                        | Some pipeName ->
                            // Find the pipe
                            match Storage.namedPipes.TryFind(pipeName) with
                            | Some pipe ->
                                // Check if the pipe is connected
                                if not pipe.IsConnected then
                                    Error (invalidValueError "Pipe is not connected")
                                else
                                    // Add data to the buffer
                                    for i = offset to offset + count - 1 do
                                        pipe.Buffer.Add(data.[i])
                                    
                                    Ok count
                            | None ->
                                Error (invalidValueError $"Named pipe not found: {pipeName}")
                        | None ->
                            Error (invalidValueError $"Invalid pipe handle: {handle}")
                    with ex ->
                        Error (invalidValueError $"Failed to write to pipe: {ex.Message}")
                )
            
            member this.ReadNamedPipe handle buffer offset count =
                Storage.withLock (fun () ->
                    try
                        // Find the pipe name from the handle
                        match Storage.pipeHandles.TryFind(handle) with
                        | Some pipeName ->
                            // Find the pipe
                            match Storage.namedPipes.TryFind(pipeName) with
                            | Some pipe ->
                                // Check if the pipe is connected
                                if not pipe.IsConnected then
                                    Error (invalidValueError "Pipe is not connected")
                                // Check if there's data available
                                elif pipe.Buffer.Count = 0 then
                                    Ok 0 // No data available
                                else
                                    // Read data from the buffer
                                    let bytesToRead = min pipe.Buffer.Count count
                                    
                                    for i = 0 to bytesToRead - 1 do
                                        buffer.[offset + i] <- pipe.Buffer.[i]
                                    
                                    // Remove read data from the buffer
                                    pipe.Buffer.RemoveRange(0, bytesToRead)
                                    
                                    Ok bytesToRead
                            | None ->
                                Error (invalidValueError $"Named pipe not found: {pipeName}")
                        | None ->
                            Error (invalidValueError $"Invalid pipe handle: {handle}")
                    with ex ->
                        Error (invalidValueError $"Failed to read from pipe: {ex.Message}")
                )
            
            member this.CloseNamedPipe handle =
                Storage.withLock (fun () ->
                    try
                        // Find the pipe name from the handle
                        match Storage.pipeHandles.TryFind(handle) with
                        | Some pipeName ->
                            // Remove the handle
                            Storage.pipeHandles <- Storage.pipeHandles.Remove(handle)
                            
                            // If this was the server handle, remove the pipe entirely
                            match Storage.namedPipes.TryFind(pipeName) with
                            | Some pipe when pipe.Handle = handle ->
                                Storage.namedPipes <- Storage.namedPipes.Remove(pipeName)
                            | _ -> ()
                            
                            Ok ()
                        | None ->
                            Error (invalidValueError $"Invalid pipe handle: {handle}")
                    with ex ->
                        Error (invalidValueError $"Failed to close pipe: {ex.Message}")
                )
            
            member this.CreateSharedMemory name size accessType =
                Storage.withLock (fun () ->
                    try
                        // Check if shared memory with this name already exists
                        match Storage.sharedMemory.TryFind(name) with
                        | Some _ ->
                            Error (invalidValueError $"Shared memory already exists: {name}")
                        | None ->
                            // Create a new shared memory region
                            let data = Array.zeroCreate (int size)
                            let handle = Storage.getNextHandle()
                            let address = Storage.getNextHandle() // Use a different value for the address
                            
                            let sharedMem = {
                                Handle = handle
                                Name = name
                                Data = data
                                Address = address
                                AccessType = accessType
                            }
                            
                            // Store the shared memory information
                            Storage.sharedMemory <- Storage.sharedMemory.Add(name, sharedMem)
                            Storage.sharedMemoryHandles <- Storage.sharedMemoryHandles.Add(handle, name)
                            
                            Ok (handle, address)
                    with ex ->
                        Error (invalidValueError $"Failed to create shared memory: {ex.Message}")
                )
            
            member this.OpenSharedMemory name accessType =
                Storage.withLock (fun () ->
                    try
                        // Find the shared memory
                        match Storage.sharedMemory.TryFind(name) with
                        | Some sharedMem ->
                            // Create a new handle to the existing shared memory
                            let handle = Storage.getNextHandle()
                            
                            // Store the handle
                            Storage.sharedMemoryHandles <- Storage.sharedMemoryHandles.Add(handle, name)
                            
                            // Return the handle, address, and size
                            let size = sharedMem.Data.Length * 1<bytes>
                            Ok (handle, sharedMem.Address, size)
                        | None ->
                            Error (invalidValueError $"Shared memory not found: {name}")
                    with ex ->
                        Error (invalidValueError $"Failed to open shared memory: {ex.Message}")
                )
            
            member this.CloseSharedMemory handle address size =
                Storage.withLock (fun () ->
                    try
                        // Find the shared memory name from the handle
                        match Storage.sharedMemoryHandles.TryFind(handle) with
                        | Some name ->
                            // Remove the handle
                            Storage.sharedMemoryHandles <- Storage.sharedMemoryHandles.Remove(handle)
                            
                            // If no more handles to this shared memory, remove it
                            let remainingHandles = 
                                Storage.sharedMemoryHandles
                                |> Map.filter (fun _ n -> n = name)
                            
                            if Map.isEmpty remainingHandles then
                                Storage.sharedMemory <- Storage.sharedMemory.Remove(name)
                            
                            Ok ()
                        | None ->
                            Error (invalidValueError $"Invalid shared memory handle: {handle}")
                    with ex ->
                        Error (invalidValueError $"Failed to close shared memory: {ex.Message}")
                )
            
            member this.ResourceExists name resourceType =
                Storage.withLock (fun () ->
                    try
                        match resourceType.ToLowerInvariant() with
                        | "pipe" ->
                            Storage.namedPipes.ContainsKey(name)
                        | "sharedmemory" | "sharedmem" | "memory" ->
                            Storage.sharedMemory.ContainsKey(name)
                        | _ ->
                            false
                    with _ ->
                        false
                )
    
    /// <summary>
    /// In-memory implementation of the network provider
    /// </summary>
    type InMemoryNetworkProvider() =
        interface IPlatformNetwork with
            member this.CreateSocket addressFamily socketType protocolType =
                Storage.withLock (fun () ->
                    try
                        // Create a new socket
                        let handle = Storage.getNextHandle()
                        
                        let socket = {
                            Handle = handle
                            AddressFamily = int addressFamily
                            SocketType = int socketType
                            ProtocolType = int protocolType
                            IsBound = false
                            IsListening = false
                            IsConnected = false
                            LocalAddress = ""
                            LocalPort = 0
                            RemoteAddress = ""
                            RemotePort = 0
                            Buffer = Buffer.Create(1024) // Default buffer size
                        }
                        
                        // Store the socket
                        Storage.sockets <- Storage.sockets.Add(handle, socket)
                        
                        Ok handle
                    with ex ->
                        Error (invalidValueError $"Failed to create socket: {ex.Message}")
                )
            
            member this.BindSocket handle address port =
                Storage.withLock (fun () ->
                    try
                        // Find the socket
                        match Storage.sockets.TryFind(handle) with
                        | Some socket ->
                            // Check if the socket is already bound
                            if socket.IsBound then
                                Error (invalidValueError "Socket is already bound")
                            else
                                // Bind the socket
                                let updatedSocket = { 
                                    socket with 
                                        IsBound = true
                                        LocalAddress = address
                                        LocalPort = port
                                }
                                
                                Storage.sockets <- Storage.sockets.Add(handle, updatedSocket)
                                
                                Ok ()
                        | None ->
                            Error (invalidValueError $"Invalid socket handle: {handle}")
                    with ex ->
                        Error (invalidValueError $"Failed to bind socket: {ex.Message}")
                )
            
            member this.ListenSocket handle backlog =
                Storage.withLock (fun () ->
                    try
                        // Find the socket
                        match Storage.sockets.TryFind(handle) with
                        | Some socket ->
                            // Check if the socket is bound
                            if not socket.IsBound then
                                Error (invalidValueError "Socket is not bound")
                            else
                                // Start listening
                                let updatedSocket = { 
                                    socket with 
                                        IsListening = true
                                }
                                
                                Storage.sockets <- Storage.sockets.Add(handle, updatedSocket)
                                
                                Ok ()
                        | None ->
                            Error (invalidValueError $"Invalid socket handle: {handle}")
                    with ex ->
                        Error (invalidValueError $"Failed to listen on socket: {ex.Message}")
                )
            
            member this.AcceptSocket handle =
                Storage.withLock (fun () ->
                    try
                        // Find the socket
                        match Storage.sockets.TryFind(handle) with
                        | Some socket ->
                            // Check if the socket is listening
                            if not socket.IsListening then
                                Error (invalidValueError "Socket is not listening")
                            else
                                // Create a new socket for the client
                                let clientHandle = Storage.getNextHandle()
                                
                                // In a real implementation, we would wait for a client connection
                                // For simplicity, we'll create a dummy client
                                let clientAddress = "127.0.0.1"
                                let clientPort = 12345
                                
                                let clientSocket = {
                                    Handle = clientHandle
                                    AddressFamily = socket.AddressFamily
                                    SocketType = socket.SocketType
                                    ProtocolType = socket.ProtocolType
                                    IsBound = true
                                    IsListening = false
                                    IsConnected = true
                                    LocalAddress = socket.LocalAddress
                                    LocalPort = socket.LocalPort
                                    RemoteAddress = clientAddress
                                    RemotePort = clientPort
                                    Buffer = Buffer.Create(1024) // Default buffer size
                                }
                                
                                // Store the client socket
                                Storage.sockets <- Storage.sockets.Add(clientHandle, clientSocket)
                                
                                Ok (clientHandle, clientAddress, clientPort)
                        | None ->
                            Error (invalidValueError $"Invalid socket handle: {handle}")
                    with ex ->
                        Error (invalidValueError $"Failed to accept connection: {ex.Message}")
                )
            
            member this.ConnectSocket handle address port =
                Storage.withLock (fun () ->
                    try
                        // Find the socket
                        match Storage.sockets.TryFind(handle) with
                        | Some socket ->
                            // Check if the socket is already connected
                            if socket.IsConnected then
                                Error (invalidValueError "Socket is already connected")
                            else
                                // Connect the socket
                                let updatedSocket = { 
                                    socket with 
                                        IsConnected = true
                                        RemoteAddress = address
                                        RemotePort = port
                                }
                                
                                Storage.sockets <- Storage.sockets.Add(handle, updatedSocket)
                                
                                Ok ()
                        | None ->
                            Error (invalidValueError $"Invalid socket handle: {handle}")
                    with ex ->
                        Error (invalidValueError $"Failed to connect socket: {ex.Message}")
                )
            
            member this.SendSocket handle data offset count flags =
                Storage.withLock (fun () ->
                    try
                        // Find the socket
                        match Storage.sockets.TryFind(handle) with
                        | Some socket ->
                            // Check if the socket is connected
                            if not socket.IsConnected then
                                Error (invalidValueError "Socket is not connected")
                            else
                                // In a real implementation, we would send data to the remote endpoint
                                // For this simulation, we'll just store it in the socket buffer
                                for i = offset to offset + count - 1 do
                                    socket.Buffer.Add(data.[i])
                                
                                Ok count
                        | None ->
                            Error (invalidValueError $"Invalid socket handle: {handle}")
                    with ex ->
                        Error (invalidValueError $"Failed to send data: {ex.Message}")
                )
            
            member this.ReceiveSocket handle buffer offset count flags =
                Storage.withLock (fun () ->
                    try
                        // Find the socket
                        match Storage.sockets.TryFind(handle) with
                        | Some socket ->
                            // Check if the socket is connected
                            if not socket.IsConnected then
                                Error (invalidValueError "Socket is not connected")
                            // Check if there's data available
                            elif socket.Buffer.Count = 0 then
                                Ok 0 // No data available
                            else
                                // Read data from the buffer
                                let bytesToRead = min socket.Buffer.Count count
                                
                                for i = 0 to bytesToRead - 1 do
                                    buffer.[offset + i] <- socket.Buffer.[i]
                                
                                // Remove read data from the buffer
                                socket.Buffer.RemoveRange(0, bytesToRead)
                                
                                Ok bytesToRead
                        | None ->
                            Error (invalidValueError $"Invalid socket handle: {handle}")
                    with ex ->
                        Error (invalidValueError $"Failed to receive data: {ex.Message}")
                )
            
            member this.CloseSocket handle =
                Storage.withLock (fun () ->
                    try
                        // Find the socket
                        match Storage.sockets.TryFind(handle) with
                        | Some _ ->
                            // Remove the socket
                            Storage.sockets <- Storage.sockets.Remove(handle)
                            
                            Ok ()
                        | None ->
                            Error (invalidValueError $"Invalid socket handle: {handle}")
                    with ex ->
                        Error (invalidValueError $"Failed to close socket: {ex.Message}")
                )
            
            member this.Poll handle timeout =
                Storage.withLock (fun () ->
                    try
                        // Find the socket
                        match Storage.sockets.TryFind(handle) with
                        | Some socket ->
                            // Check if there's data available
                            Ok (socket.Buffer.Count > 0)
                        | None ->
                            Error (invalidValueError $"Invalid socket handle: {handle}")
                    with ex ->
                        Error (invalidValueError $"Failed to poll socket: {ex.Message}")
                )
            
            member this.ResolveHostName hostName =
                // For in-memory simulation, we'll return a dummy IP address
                try
                    let addresses = [| "127.0.0.1" |]
                    Ok addresses
                with ex ->
                    Error (invalidValueError $"Failed to resolve host name: {ex.Message}")
    
    /// <summary>
    /// In-memory implementation of the synchronization provider
    /// </summary>
    type InMemorySyncProvider() =
        interface IPlatformSync with
            member this.CreateMutex name initialOwner =
                Storage.withLock (fun () ->
                    try
                        // Check if the mutex already exists
                        if not (isNull name) && Storage.mutexes.ContainsKey(name) then
                            // Return a handle to the existing mutex
                            let existingMutex = Storage.mutexes.[name]
                            let handle = Storage.getNextHandle()
                            
                            // Store the handle
                            Storage.mutexHandles <- Storage.mutexHandles.Add(handle, name)
                            
                            // If initialOwner is true, try to acquire it
                            if initialOwner && not existingMutex.IsOwned then
                                let updatedMutex = { 
                                    existingMutex with 
                                        IsOwned = true 
                                        OwnerThreadId = Some 1 // Simplified thread ID
                                }
                                Storage.mutexes <- Storage.mutexes.Add(name, updatedMutex)
                            
                            Ok handle
                        else
                            // Create a new mutex
                            let handle = Storage.getNextHandle()
                            
                            let mutex = {
                                Handle = handle
                                Name = name
                                IsOwned = initialOwner
                                OwnerThreadId = if initialOwner then Some 1 else None // Simplified thread ID
                            }
                            
                            // Store the mutex information
                            if not (isNull name) then
                                Storage.mutexes <- Storage.mutexes.Add(name, mutex)
                            
                            Storage.mutexHandles <- Storage.mutexHandles.Add(handle, name)
                            
                            Ok handle
                    with ex ->
                        Error (invalidValueError $"Failed to create mutex: {ex.Message}")
                )
            
            member this.OpenMutex name =
                Storage.withLock (fun () ->
                    try
                        // Find the mutex
                        match Storage.mutexes.TryFind(name) with
                        | Some mutex ->
                            // Create a new handle to the existing mutex
                            let handle = Storage.getNextHandle()
                            
                            // Store the handle
                            Storage.mutexHandles <- Storage.mutexHandles.Add(handle, name)
                            
                            Ok handle
                        | None ->
                            Error (invalidValueError $"Mutex not found: {name}")
                    with ex ->
                        Error (invalidValueError $"Failed to open mutex: {ex.Message}")
                )
            
            member this.AcquireMutex handle timeout =
                Storage.withLock (fun () ->
                    try
                        // Find the mutex name from the handle
                        match Storage.mutexHandles.TryFind(handle) with
                        | Some name ->
                            // Find the mutex
                            let mutex = 
                                if isNull name then
                                    // This is an unnamed mutex, find it by handle
                                    None
                                else
                                    Storage.mutexes.TryFind(name)
                            
                            match mutex with
                            | Some mutex ->
                                // Check if the mutex is already owned
                                if mutex.IsOwned then
                                    // Check if the current thread is the owner
                                    if mutex.OwnerThreadId = Some 1 then // Simplified thread ID
                                        // Mutex is already owned by this thread
                                        Ok true
                                    else
                                        // Mutex is owned by another thread
                                        if timeout = 0 then
                                            // No wait
                                            Ok false
                                        else
                                            // In a real implementation, this would wait until timeout
                                            // For simplicity, we just return false
                                            Ok false
                                else
                                    // Acquire the mutex
                                    let updatedMutex = { 
                                        mutex with 
                                            IsOwned = true 
                                            OwnerThreadId = Some 1 // Simplified thread ID
                                    }
                                    
                                    Storage.mutexes <- Storage.mutexes.Add(name, updatedMutex)
                                    
                                    Ok true
                            | None ->
                                Error (invalidValueError $"Mutex not found: {name}")
                        | None ->
                            Error (invalidValueError $"Invalid mutex handle: {handle}")
                    with ex ->
                        Error (invalidValueError $"Failed to acquire mutex: {ex.Message}")
                )
            
            member this.ReleaseMutex handle =
                Storage.withLock (fun () ->
                    try
                        // Find the mutex name from the handle
                        match Storage.mutexHandles.TryFind(handle) with
                        | Some name ->
                            // Find the mutex
                            let mutex = 
                                if isNull name then
                                    // This is an unnamed mutex, find it by handle
                                    None
                                else
                                    Storage.mutexes.TryFind(name)
                            
                            match mutex with
                            | Some mutex ->
                                // Check if the mutex is owned by the current thread
                                if mutex.OwnerThreadId <> Some 1 then // Simplified thread ID
                                    Error (invalidValueError "Mutex not owned by the current thread")
                                else
                                    // Release the mutex
                                    let updatedMutex = { 
                                        mutex with 
                                            IsOwned = false 
                                            OwnerThreadId = None
                                    }
                                    
                                    Storage.mutexes <- Storage.mutexes.Add(name, updatedMutex)
                                    
                                    Ok ()
                            | None ->
                                Error (invalidValueError $"Mutex not found: {name}")
                        | None ->
                            Error (invalidValueError $"Invalid mutex handle: {handle}")
                    with ex ->
                        Error (invalidValueError $"Failed to release mutex: {ex.Message}")
                )
            
            member this.CloseMutex handle =
                Storage.withLock (fun () ->
                    try
                        // Find the mutex name from the handle
                        match Storage.mutexHandles.TryFind(handle) with
                        | Some name ->
                            // Remove the handle
                            Storage.mutexHandles <- Storage.mutexHandles.Remove(handle)
                            
                            // If no more handles to this mutex, remove it
                            if not (isNull name) then
                                let remainingHandles = 
                                    Storage.mutexHandles
                                    |> Map.filter (fun _ n -> n = name)
                                
                                if Map.isEmpty remainingHandles then
                                    Storage.mutexes <- Storage.mutexes.Remove(name)
                            
                            Ok ()
                        | None ->
                            Error (invalidValueError $"Invalid mutex handle: {handle}")
                    with ex ->
                        Error (invalidValueError $"Failed to close mutex: {ex.Message}")
                )
            
            member this.CreateSemaphore name initialCount maximumCount =
                Storage.withLock (fun () ->
                    try
                        // Check if the semaphore already exists
                        if not (isNull name) && Storage.semaphores.ContainsKey(name) then
                            // Return a handle to the existing semaphore
                            let existingSemaphore = Storage.semaphores.[name]
                            let handle = Storage.getNextHandle()
                            
                            // Store the handle
                            Storage.semaphoreHandles <- Storage.semaphoreHandles.Add(handle, name)
                            
                            Ok handle
                        else
                            // Create a new semaphore
                            let handle = Storage.getNextHandle()
                            
                            let semaphore = {
                                Handle = handle
                                Name = name
                                CurrentCount = initialCount
                                MaximumCount = maximumCount
                            }
                            
                            // Store the semaphore information
                            if not (isNull name) then
                                Storage.semaphores <- Storage.semaphores.Add(name, semaphore)
                            
                            Storage.semaphoreHandles <- Storage.semaphoreHandles.Add(handle, name)
                            
                            Ok handle
                    with ex ->
                        Error (invalidValueError $"Failed to create semaphore: {ex.Message}")
                )
            
            member this.OpenSemaphore name =
                Storage.withLock (fun () ->
                    try
                        // Find the semaphore
                        match Storage.semaphores.TryFind(name) with
                        | Some semaphore ->
                            // Create a new handle to the existing semaphore
                            let handle = Storage.getNextHandle()
                            
                            // Store the handle
                            Storage.semaphoreHandles <- Storage.semaphoreHandles.Add(handle, name)
                            
                            Ok handle
                        | None ->
                            Error (invalidValueError $"Semaphore not found: {name}")
                    with ex ->
                        Error (invalidValueError $"Failed to open semaphore: {ex.Message}")
                )
            
            member this.AcquireSemaphore handle timeout =
                Storage.withLock (fun () ->
                    try
                        // Find the semaphore name from the handle
                        match Storage.semaphoreHandles.TryFind(handle) with
                        | Some name ->
                            // Find the semaphore
                            let semaphore = 
                                if isNull name then
                                    // This is an unnamed semaphore, find it by handle
                                    None
                                else
                                    Storage.semaphores.TryFind(name)
                            
                            match semaphore with
                            | Some semaphore ->
                                // Check if the semaphore has available count
                                if semaphore.CurrentCount > 0 then
                                    // Acquire the semaphore
                                    let updatedSemaphore = { 
                                        semaphore with 
                                            CurrentCount = semaphore.CurrentCount - 1
                                    }
                                    
                                    Storage.semaphores <- Storage.semaphores.Add(name, updatedSemaphore)
                                    
                                    Ok true
                                else
                                    // No count available
                                    if timeout = 0 then
                                        // No wait
                                        Ok false
                                    else
                                        // In a real implementation, this would wait until timeout
                                        // For simplicity, we just return false
                                        Ok false
                            | None ->
                                Error (invalidValueError $"Semaphore not found: {name}")
                        | None ->
                            Error (invalidValueError $"Invalid semaphore handle: {handle}")
                    with ex ->
                        Error (invalidValueError $"Failed to acquire semaphore: {ex.Message}")
                )
            
            member this.ReleaseSemaphore handle releaseCount =
                Storage.withLock (fun () ->
                    try
                        // Find the semaphore name from the handle
                        match Storage.semaphoreHandles.TryFind(handle) with
                        | Some name ->
                            // Find the semaphore
                            let semaphore = 
                                if isNull name then
                                    // This is an unnamed semaphore, find it by handle
                                    None
                                else
                                    Storage.semaphores.TryFind(name)
                            
                            match semaphore with
                            | Some semaphore ->
                                // Check if the new count would exceed the maximum
                                let newCount = semaphore.CurrentCount + releaseCount
                                
                                if newCount > semaphore.MaximumCount then
                                    Error (invalidValueError "Semaphore count would exceed maximum")
                                else
                                    // Release the semaphore
                                    let previousCount = semaphore.CurrentCount
                                    let updatedSemaphore = { 
                                        semaphore with 
                                            CurrentCount = newCount
                                    }
                                    
                                    Storage.semaphores <- Storage.semaphores.Add(name, updatedSemaphore)
                                    
                                    Ok previousCount
                            | None ->
                                Error (invalidValueError $"Semaphore not found: {name}")
                        | None ->
                            Error (invalidValueError $"Invalid semaphore handle: {handle}")
                    with ex ->
                        Error (invalidValueError $"Failed to release semaphore: {ex.Message}")
                )
            
            member this.CloseSemaphore handle =
                Storage.withLock (fun () ->
                    try
                        // Find the semaphore name from the handle
                        match Storage.semaphoreHandles.TryFind(handle) with
                        | Some name ->
                            // Remove the handle
                            Storage.semaphoreHandles <- Storage.semaphoreHandles.Remove(handle)
                            
                            // If no more handles to this semaphore, remove it
                            if not (isNull name) then
                                let remainingHandles = 
                                    Storage.semaphoreHandles
                                    |> Map.filter (fun _ n -> n = name)
                                
                                if Map.isEmpty remainingHandles then
                                    Storage.semaphores <- Storage.semaphores.Remove(name)
                            
                            Ok ()
                        | None ->
                            Error (invalidValueError $"Invalid semaphore handle: {handle}")
                    with ex ->
                        Error (invalidValueError $"Failed to close semaphore: {ex.Message}")
                )