namespace BAREWire.Platform.Common

open FSharp.UMX
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.IPC
open BAREWire.Memory.Mapping

/// <summary>
/// Interfaces for platform-specific operations
/// </summary>
module Interfaces =
    /// <summary>
    /// Interface for platform-specific memory operations
    /// </summary>
    type IPlatformMemory =
        /// <summary>
        /// Maps memory with specified characteristics
        /// </summary>
        /// <param name="size">The size of the region to map in bytes</param>
        /// <param name="mappingType">The type of mapping (private or shared)</param>
        /// <param name="accessType">The access permissions (read-only or read-write)</param>
        /// <returns>A result containing a tuple of (handle, address) or an error</returns>
        abstract MapMemory: size:int<bytes> -> mappingType:MappingType -> accessType:AccessType -> Result<nativeint * nativeint>
        
        /// <summary>
        /// Unmaps previously mapped memory
        /// </summary>
        /// <param name="handle">The mapping handle</param>
        /// <param name="address">The base address of the mapping</param>
        /// <param name="size">The size of the mapped region in bytes</param>
        /// <returns>A result indicating success or an error</returns>
        abstract UnmapMemory: handle:nativeint -> address:nativeint -> size:int<bytes> -> Result<unit>
        
        /// <summary>
        /// Maps a file into memory
        /// </summary>
        /// <param name="filePath">The path to the file to map</param>
        /// <param name="offset">The offset within the file to start mapping</param>
        /// <param name="size">The size of the region to map in bytes</param>
        /// <param name="accessType">The access permissions (read-only or read-write)</param>
        /// <returns>A result containing a tuple of (handle, address) or an error</returns>
        abstract MapFile: filePath:string -> offset:int64 -> size:int<bytes> -> accessType:AccessType -> Result<nativeint * nativeint>
        
        /// <summary>
        /// Flushes changes to a memory-mapped file
        /// </summary>
        /// <param name="handle">The mapping handle</param>
        /// <param name="address">The base address of the mapping</param>
        /// <param name="size">The size of the mapped region in bytes</param>
        /// <returns>A result indicating success or an error</returns>
        abstract FlushMappedFile: handle:nativeint -> address:nativeint -> size:int<bytes> -> Result<unit>
        
        /// <summary>
        /// Locks a memory region to prevent swapping
        /// </summary>
        /// <param name="address">The base address of the region</param>
        /// <param name="size">The size of the region in bytes</param>
        /// <returns>A result indicating success or an error</returns>
        abstract LockMemory: address:nativeint -> size:int<bytes> -> Result<unit>
        
        /// <summary>
        /// Unlocks a previously locked memory region
        /// </summary>
        /// <param name="address">The base address of the region</param>
        /// <param name="size">The size of the region in bytes</param>
        /// <returns>A result indicating success or an error</returns>
        abstract UnlockMemory: address:nativeint -> size:int<bytes> -> Result<unit>
    
    /// <summary>
    /// Interface for platform-specific IPC operations
    /// </summary>
    type IPlatformIpc =
        /// <summary>
        /// Creates a named pipe with specified characteristics
        /// </summary>
        /// <param name="name">The name of the pipe (must be unique)</param>
        /// <param name="direction">The direction of data flow</param>
        /// <param name="mode">The data transfer mode</param>
        /// <param name="bufferSize">The buffer size in bytes</param>
        /// <returns>A result containing the native handle or an error</returns>
        abstract CreateNamedPipe: name:string -> direction:NamedPipe.PipeDirection -> mode:NamedPipe.PipeMode -> bufferSize:int<bytes> -> Result<nativeint>
        
        /// <summary>
        /// Connects to an existing named pipe
        /// </summary>
        /// <param name="name">The name of the pipe to connect to</param>
        /// <param name="direction">The direction of data flow</param>
        /// <returns>A result containing the native handle or an error</returns>
        abstract ConnectNamedPipe: name:string -> direction:NamedPipe.PipeDirection -> Result<nativeint>
        
        /// <summary>
        /// Waits for a client to connect to a pipe server
        /// </summary>
        /// <param name="handle">The pipe handle</param>
        /// <param name="timeout">The timeout in milliseconds (-1 for infinite)</param>
        /// <returns>A result indicating success or an error</returns>
        abstract WaitForNamedPipeConnection: handle:nativeint -> timeout:int -> Result<unit>
        
        /// <summary>
        /// Sends data through a named pipe
        /// </summary>
        /// <param name="handle">The pipe handle</param>
        /// <param name="data">The data to send</param>
        /// <param name="offset">The offset within the data to start sending</param>
        /// <param name="count">The number of bytes to send</param>
        /// <returns>A result containing the number of bytes sent or an error</returns>
        abstract WriteNamedPipe: handle:nativeint -> data:byte[] -> offset:int -> count:int -> Result<int>
        
        /// <summary>
        /// Receives data from a named pipe
        /// </summary>
        /// <param name="handle">The pipe handle</param>
        /// <param name="buffer">The buffer to receive data into</param>
        /// <param name="offset">The offset within the buffer to start writing</param>
        /// <param name="count">The maximum number of bytes to receive</param>
        /// <returns>A result containing the number of bytes received or an error</returns>
        abstract ReadNamedPipe: handle:nativeint -> buffer:byte[] -> offset:int -> count:int -> Result<int>
        
        /// <summary>
        /// Closes a named pipe
        /// </summary>
        /// <param name="handle">The pipe handle</param>
        /// <returns>A result indicating success or an error</returns>
        abstract CloseNamedPipe: handle:nativeint -> Result<unit>
        
        /// <summary>
        /// Creates a shared memory region
        /// </summary>
        /// <param name="name">The name of the shared region (must be unique)</param>
        /// <param name="size">The size of the shared memory in bytes</param>
        /// <param name="accessType">The access permissions (read-only or read-write)</param>
        /// <returns>A result containing a tuple of (handle, address) or an error</returns>
        abstract CreateSharedMemory: name:string -> size:int<bytes> -> accessType:AccessType -> Result<nativeint * nativeint>
        
        /// <summary>
        /// Opens an existing shared memory region
        /// </summary>
        /// <param name="name">The name of the shared region to open</param>
        /// <param name="accessType">The access permissions (read-only or read-write)</param>
        /// <returns>A result containing a tuple of (handle, address, size) or an error</returns>
        abstract OpenSharedMemory: name:string -> accessType:AccessType -> Result<nativeint * nativeint * int<bytes>>
        
        /// <summary>
        /// Closes a shared memory region
        /// </summary>
        /// <param name="handle">The shared memory handle</param>
        /// <param name="address">The base address of the mapping</param>
        /// <param name="size">The size of the shared memory in bytes</param>
        /// <returns>A result indicating success or an error</returns>
        abstract CloseSharedMemory: handle:nativeint -> address:nativeint -> size:int<bytes> -> Result<unit>
        
        /// <summary>
        /// Checks if a named IPC resource exists
        /// </summary>
        /// <param name="name">The name of the resource to check</param>
        /// <param name="resourceType">The type of resource (pipe or shared memory)</param>
        /// <returns>True if the resource exists, false otherwise</returns>
        abstract ResourceExists: name:string -> resourceType:string -> bool
    
    /// <summary>
    /// Interface for platform-specific network operations
    /// </summary>
    type IPlatformNetwork =
        /// <summary>
        /// Creates a socket with specified characteristics
        /// </summary>
        /// <param name="addressFamily">The address family (IPv4, IPv6, etc.)</param>
        /// <param name="socketType">The socket type (stream, datagram, etc.)</param>
        /// <param name="protocolType">The protocol type (TCP, UDP, etc.)</param>
        /// <returns>A result containing the socket handle or an error</returns>
        abstract CreateSocket: addressFamily:int -> socketType:int -> protocolType:int -> Result<nativeint>
        
        /// <summary>
        /// Binds a socket to a local endpoint
        /// </summary>
        /// <param name="handle">The socket handle</param>
        /// <param name="address">The local address to bind to</param>
        /// <param name="port">The local port to bind to</param>
        /// <returns>A result indicating success or an error</returns>
        abstract BindSocket: handle:nativeint -> address:string -> port:int -> Result<unit>
        
        /// <summary>
        /// Listens for connections on a socket
        /// </summary>
        /// <param name="handle">The socket handle</param>
        /// <param name="backlog">The maximum length of the pending connections queue</param>
        /// <returns>A result indicating success or an error</returns>
        abstract ListenSocket: handle:nativeint -> backlog:int -> Result<unit>
        
        /// <summary>
        /// Accepts a connection on a socket
        /// </summary>
        /// <param name="handle">The socket handle</param>
        /// <returns>A result containing the new connection handle or an error</returns>
        abstract AcceptSocket: handle:nativeint -> Result<nativeint * string * int>
        
        /// <summary>
        /// Connects a socket to a remote endpoint
        /// </summary>
        /// <param name="handle">The socket handle</param>
        /// <param name="address">The remote address to connect to</param>
        /// <param name="port">The remote port to connect to</param>
        /// <returns>A result indicating success or an error</returns>
        abstract ConnectSocket: handle:nativeint -> address:string -> port:int -> Result<unit>
        
        /// <summary>
        /// Sends data through a socket
        /// </summary>
        /// <param name="handle">The socket handle</param>
        /// <param name="data">The data to send</param>
        /// <param name="offset">The offset within the data to start sending</param>
        /// <param name="count">The number of bytes to send</param>
        /// <param name="flags">Socket flags</param>
        /// <returns>A result containing the number of bytes sent or an error</returns>
        abstract SendSocket: handle:nativeint -> data:byte[] -> offset:int -> count:int -> flags:int -> Result<int>
        
        /// <summary>
        /// Receives data from a socket
        /// </summary>
        /// <param name="handle">The socket handle</param>
        /// <param name="buffer">The buffer to receive data into</param>
        /// <param name="offset">The offset within the buffer to start writing</param>
        /// <param name="count">The maximum number of bytes to receive</param>
        /// <param name="flags">Socket flags</param>
        /// <returns>A result containing the number of bytes received or an error</returns>
        abstract ReceiveSocket: handle:nativeint -> buffer:byte[] -> offset:int -> count:int -> flags:int -> Result<int>
        
        /// <summary>
        /// Closes a socket
        /// </summary>
        /// <param name="handle">The socket handle</param>
        /// <returns>A result indicating success or an error</returns>
        abstract CloseSocket: handle:nativeint -> Result<unit>
        
        /// <summary>
        /// Sets a socket option
        /// </summary>
        /// <param name="handle">The socket handle</param>
        /// <param name="level">The option level</param>
        /// <param name="optionName">The option name</param>
        /// <param name="optionValue">The option value</param>
        /// <returns>A result indicating success or an error</returns>
        abstract SetSocketOption: handle:nativeint -> level:int -> optionName:int -> optionValue:byte[] -> Result<unit>
        
        /// <summary>
        /// Gets a socket option
        /// </summary>
        /// <param name="handle">The socket handle</param>
        /// <param name="level">The option level</param>
        /// <param name="optionName">The option name</param>
        /// <param name="optionValue">Buffer to receive the option value</param>
        /// <returns>A result containing the option size or an error</returns>
        abstract GetSocketOption: handle:nativeint -> level:int -> optionName:int -> optionValue:byte[] -> Result<int>
    
    /// <summary>
    /// Interface for platform-specific synchronization operations
    /// </summary>
    type IPlatformSync =
        /// <summary>
        /// Creates a mutex
        /// </summary>
        /// <param name="name">The name of the mutex (or null for unnamed)</param>
        /// <param name="initialOwner">Whether the calling thread should be the initial owner</param>
        /// <returns>A result containing the mutex handle or an error</returns>
        abstract CreateMutex: name:string -> initialOwner:bool -> Result<nativeint>
        
        /// <summary>
        /// Opens an existing mutex
        /// </summary>
        /// <param name="name">The name of the mutex to open</param>
        /// <returns>A result containing the mutex handle or an error</returns>
        abstract OpenMutex: name:string -> Result<nativeint>
        
        /// <summary>
        /// Acquires a mutex
        /// </summary>
        /// <param name="handle">The mutex handle</param>
        /// <param name="timeout">The timeout in milliseconds (-1 for infinite)</param>
        /// <returns>A result indicating success or an error</returns>
        abstract AcquireMutex: handle:nativeint -> timeout:int -> Result<bool>
        
        /// <summary>
        /// Releases a mutex
        /// </summary>
        /// <param name="handle">The mutex handle</param>
        /// <returns>A result indicating success or an error</returns>
        abstract ReleaseMutex: handle:nativeint -> Result<unit>
        
        /// <summary>
        /// Closes a mutex
        /// </summary>
        /// <param name="handle">The mutex handle</param>
        /// <returns>A result indicating success or an error</returns>
        abstract CloseMutex: handle:nativeint -> Result<unit>
        
        /// <summary>
        /// Creates a semaphore
        /// </summary>
        /// <param name="name">The name of the semaphore (or null for unnamed)</param>
        /// <param name="initialCount">The initial count</param>
        /// <param name="maximumCount">The maximum count</param>
        /// <returns>A result containing the semaphore handle or an error</returns>
        abstract CreateSemaphore: name:string -> initialCount:int -> maximumCount:int -> Result<nativeint>
        
        /// <summary>
        /// Opens an existing semaphore
        /// </summary>
        /// <param name="name">The name of the semaphore to open</param>
        /// <returns>A result containing the semaphore handle or an error</returns>
        abstract OpenSemaphore: name:string -> Result<nativeint>
        
        /// <summary>
        /// Acquires a semaphore
        /// </summary>
        /// <param name="handle">The semaphore handle</param>
        /// <param name="timeout">The timeout in milliseconds (-1 for infinite)</param>
        /// <returns>A result indicating success or an error</returns>
        abstract AcquireSemaphore: handle:nativeint -> timeout:int -> Result<bool>
        
        /// <summary>
        /// Releases a semaphore
        /// </summary>
        /// <param name="handle">The semaphore handle</param>
        /// <param name="releaseCount">The number of times to release</param>
        /// <returns>A result containing the previous count or an error</returns>
        abstract ReleaseSemaphore: handle:nativeint -> releaseCount:int -> Result<int>
        
        /// <summary>
        /// Closes a semaphore
        /// </summary>
        /// <param name="handle">The semaphore handle</param>
        /// <returns>A result indicating success or an error</returns>
        abstract CloseSemaphore: handle:nativeint -> Result<unit>