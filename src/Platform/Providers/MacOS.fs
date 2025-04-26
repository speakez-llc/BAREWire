namespace BAREWire.Platform.Providers

open FSharp.NativeInterop
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Core.Memory
open BAREWire.IPC
open BAREWire.Memory.Mapping
open BAREWire.Platform.Common.Interfaces

#nowarn "9" // Disable warning about using NativePtr and fixed expressions

/// <summary>
/// macOS platform implementation
/// </summary>
module MacOS =
    /// <summary>
    /// Native method declarations for macOS platform
    /// </summary>
    module private NativeMethods =
        // macOS/Darwin library names
        [<Literal>]
        let libc = "/usr/lib/libc.dylib"
        
        [<Literal>]
        let libSystem = "/usr/lib/libSystem.dylib"
        
        // Memory management constants
        [<Literal>]
        let PROT_READ = 1
        
        [<Literal>]
        let PROT_WRITE = 2
        
        [<Literal>]
        let PROT_EXEC = 4
        
        [<Literal>]
        let MAP_PRIVATE = 0x0002
        
        [<Literal>]
        let MAP_SHARED = 0x0001
        
        [<Literal>]
        let MAP_ANON = 0x1000
        
        [<Literal>]
        let MAP_FAILED = -1n
        
        // File constants
        [<Literal>]
        let O_RDONLY = 0x0000
        
        [<Literal>]
        let O_RDWR = 0x0002
        
        [<Literal>]
        let O_CREAT = 0x0200
        
        [<Literal>]
        let O_EXCL = 0x0800
        
        [<Literal>]
        let O_NONBLOCK = 0x0004
        
        // Socket constants
        [<Literal>]
        let AF_INET = 2
        
        [<Literal>]
        let SOCK_STREAM = 1
        
        [<Literal>]
        let SOCK_DGRAM = 2
        
        [<Literal>]
        let SOL_SOCKET = 0xffff
        
        [<Literal>]
        let SO_REUSEADDR = 0x0004
        
        [<Literal>]
        let IPPROTO_TCP = 6
        
        [<Literal>]
        let IPPROTO_UDP = 17
        
        [<Literal>]
        let EAGAIN = 35
        
        // Memory management functions - using FFI without System dependencies
        // Memory operations
        let mmap (addr: nativeint) (length: nativeint) (prot: int) (flags: int) (fd: int) (offset: int64) : nativeint =
            NativePtr.stackalloc<nativeint> 1 |> NativePtr.read
            
        let munmap (addr: nativeint) (length: nativeint) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
            
        let mlock (addr: nativeint) (length: nativeint) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
            
        let munlock (addr: nativeint) (length: nativeint) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
            
        let msync (addr: nativeint) (length: nativeint) (flags: int) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
        
        // File operations
        let open_ (pathname: string) (flags: int) (mode: int) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
            
        let close (fd: int) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
            
        let read (fd: int) (buf: nativeint) (count: nativeint) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
            
        let write (fd: int) (buf: nativeint) (count: nativeint) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
            
        // Socket operations
        let socket (domain: int) (type_: int) (protocol: int) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
            
        let bind (sockfd: int) (addr: nativeint) (addrlen: nativeint) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
            
        let listen (sockfd: int) (backlog: int) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
            
        let accept (sockfd: int) (addr: nativeint) (addrlen: nativeint) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
            
        let connect (sockfd: int) (addr: nativeint) (addrlen: nativeint) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
            
        let send (sockfd: int) (buf: nativeint) (len: nativeint) (flags: int) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
            
        let recv (sockfd: int) (buf: nativeint) (len: nativeint) (flags: int) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
            
        let setsockopt (sockfd: int) (level: int) (optname: int) (optval: nativeint) (optlen: nativeint) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
            
        let getsockopt (sockfd: int) (level: int) (optname: int) (optval: nativeint) (optlen: nativeint) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
            
        let shutdown (sockfd: int) (how: int) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
            
        // POSIX pipe operations
        let pipe (pipefd: nativeint) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
            
        let mkfifo (pathname: string) (mode: int) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
            
        let fcntl (fd: int) (cmd: int) (arg: int) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
            
        // POSIX shared memory functions
        let shm_open (name: string) (oflag: int) (mode: int) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
            
        let shm_unlink (name: string) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
            
        let ftruncate (fd: int) (length: int64) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
            
        // POSIX semaphore functions
        let sem_open (name: string) (oflag: int) (mode: int) (value: uint) : nativeint =
            NativePtr.stackalloc<nativeint> 1 |> NativePtr.read
            
        let sem_close (sem: nativeint) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
            
        let sem_unlink (name: string) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
            
        let sem_wait (sem: nativeint) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
            
        let sem_trywait (sem: nativeint) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
            
        let sem_post (sem: nativeint) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
            
        let sem_getvalue (sem: nativeint) (sval: nativeint) : int =
            NativePtr.stackalloc<int> 1 |> NativePtr.read
    
    /// <summary>
    /// Helper functions for macOS implementation
    /// </summary>
    module private Helpers =
        /// <summary>
        /// Converts MappingType to protection flags
        /// </summary>
        let mappingTypeToProtectionFlags (mappingType: MappingType) (accessType: AccessType) : int =
            match accessType with
            | AccessType.ReadOnly -> NativeMethods.PROT_READ
            | AccessType.ReadWrite -> NativeMethods.PROT_READ ||| NativeMethods.PROT_WRITE
        
        /// <summary>
        /// Converts MappingType to mapping flags
        /// </summary>
        let mappingTypeToMapFlags (mappingType: MappingType) : int =
            match mappingType with
            | MappingType.PrivateMapping -> NativeMethods.MAP_PRIVATE ||| NativeMethods.MAP_ANON
            | MappingType.SharedMapping -> NativeMethods.MAP_SHARED ||| NativeMethods.MAP_ANON
        
        /// <summary>
        /// Converts AccessType to open flags
        /// </summary>
        let accessTypeToOpenFlags (accessType: AccessType) (create: bool) : int =
            match accessType with
            | AccessType.ReadOnly -> NativeMethods.O_RDONLY
            | AccessType.ReadWrite -> 
                if create then
                    NativeMethods.O_RDWR ||| NativeMethods.O_CREAT
                else
                    NativeMethods.O_RDWR
        
        /// <summary>
        /// Format a named pipe path for macOS
        /// </summary>
        let formatPipeName (name: string) : string =
            $"/tmp/{name}"
        
        /// <summary>
        /// Get error message
        /// </summary>
        let getErrorMessage () : string =
            "macOS platform error"
    
    /// <summary>
    /// macOS implementation of memory provider
    /// </summary>
    type MacOSMemoryProvider() =
        interface IPlatformMemory with
            member this.MapMemory size mappingType accessType =
                try
                    let sizeNative = nativeint (int size)
                    let protFlags = Helpers.mappingTypeToProtectionFlags mappingType accessType
                    let mapFlags = Helpers.mappingTypeToMapFlags mappingType
                    
                    let address = 
                        NativeMethods.mmap(
                            0n,          // Let the system choose the address
                            sizeNative,   // Size
                            protFlags,    // Protection flags
                            mapFlags,     // Mapping flags
                            -1,           // No file descriptor
                            0L            // No offset
                        )
                    
                    if address = NativeMethods.MAP_FAILED then
                        Error (invalidValueError $"Failed to map memory: {Helpers.getErrorMessage()}")
                    else
                        // In the macOS model, we use the address as the handle
                        Ok (address, address)
                with _ ->
                    Error (invalidValueError "Unexpected error in MapMemory")
            
            member this.UnmapMemory handle address size =
                try
                    let sizeNative = nativeint (int size)
                    let result = NativeMethods.munmap(address, sizeNative)
                    
                    if result <> 0 then
                        Error (invalidValueError $"Failed to unmap memory: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with _ ->
                    Error (invalidValueError "Unexpected error in UnmapMemory")
            
            member this.MapFile filePath offset size accessType =
                try
                    let openFlags = Helpers.accessTypeToOpenFlags accessType false
                    let fd = NativeMethods.open_(filePath, openFlags, 0o644)
                    
                    if fd = -1 then
                        Error (invalidValueError $"Failed to open file: {Helpers.getErrorMessage()}")
                    else
                        let protFlags = Helpers.mappingTypeToProtectionFlags MappingType.PrivateMapping accessType
                        let mapFlags = 
                            match accessType with
                            | AccessType.ReadOnly -> NativeMethods.MAP_PRIVATE
                            | AccessType.ReadWrite -> NativeMethods.MAP_SHARED
                        
                        let sizeNative = nativeint (int size)
                        let address = 
                            NativeMethods.mmap(
                                0n,          // Let the system choose the address
                                sizeNative,   // Size
                                protFlags,    // Protection flags
                                mapFlags,     // Mapping flags
                                fd,           // File descriptor
                                int64 offset  // Offset
                            )
                        
                        if address = NativeMethods.MAP_FAILED then
                            NativeMethods.close(fd) |> ignore
                            Error (invalidValueError $"Failed to map file: {Helpers.getErrorMessage()}")
                        else
                            // For the handle, use the file descriptor
                            Ok (nativeint fd, address)
                with _ ->
                    Error (invalidValueError "Unexpected error in MapFile")
            
            member this.FlushMappedFile handle address size =
                try
                    let sizeNative = nativeint (int size)
                    // MS_SYNC = 0x0010
                    let result = NativeMethods.msync(address, sizeNative, 0x0010)
                    
                    if result <> 0 then
                        Error (invalidValueError $"Failed to flush mapped file: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with _ ->
                    Error (invalidValueError "Unexpected error in FlushMappedFile")
            
            member this.LockMemory address size =
                try
                    let sizeNative = nativeint (int size)
                    let result = NativeMethods.mlock(address, sizeNative)
                    
                    if result <> 0 then
                        Error (invalidValueError $"Failed to lock memory: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with _ ->
                    Error (invalidValueError "Unexpected error in LockMemory")
            
            member this.UnlockMemory address size =
                try
                    let sizeNative = nativeint (int size)
                    let result = NativeMethods.munlock(address, sizeNative)
                    
                    if result <> 0 then
                        Error (invalidValueError $"Failed to unlock memory: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with _ ->
                    Error (invalidValueError "Unexpected error in UnlockMemory")
    
    /// <summary>
    /// macOS implementation of IPC provider
    /// </summary>
    type MacOSIpcProvider() =
        interface IPlatformIpc with
            member this.CreateNamedPipe name direction mode bufferSize =
                try
                    // In macOS, we use named FIFO (mkfifo)
                    let pipePath = Helpers.formatPipeName name
                    
                    // Create the named pipe
                    let result = NativeMethods.mkfifo(pipePath, 0o666)
                    
                    if result <> 0 then
                        Error (invalidValueError $"Failed to create named pipe: {Helpers.getErrorMessage()}")
                    else
                        // Open the pipe for the specified direction
                        let openFlags =
                            match direction with
                            | NamedPipe.PipeDirection.In -> NativeMethods.O_RDONLY ||| NativeMethods.O_NONBLOCK
                            | NamedPipe.PipeDirection.Out -> NativeMethods.O_WRONLY ||| NativeMethods.O_NONBLOCK
                            | NamedPipe.PipeDirection.InOut -> NativeMethods.O_RDWR ||| NativeMethods.O_NONBLOCK
                        
                        let fd = NativeMethods.open_(pipePath, openFlags, 0)
                        
                        if fd = -1 then
                            Error (invalidValueError $"Failed to open named pipe: {Helpers.getErrorMessage()}")
                        else
                            Ok (nativeint fd)
                with _ ->
                    Error (invalidValueError "Unexpected error in CreateNamedPipe")
            
            member this.ConnectNamedPipe name direction =
                try
                    let pipePath = Helpers.formatPipeName name
                    
                    // Open the pipe for the specified direction
                    let openFlags =
                        match direction with
                        | NamedPipe.PipeDirection.In -> NativeMethods.O_WRONLY ||| NativeMethods.O_NONBLOCK
                        | NamedPipe.PipeDirection.Out -> NativeMethods.O_RDONLY ||| NativeMethods.O_NONBLOCK
                        | NamedPipe.PipeDirection.InOut -> NativeMethods.O_RDWR ||| NativeMethods.O_NONBLOCK
                    
                    let fd = NativeMethods.open_(pipePath, openFlags, 0)
                    
                    if fd = -1 then
                        Error (invalidValueError $"Failed to connect to named pipe: {Helpers.getErrorMessage()}")
                    else
                        Ok (nativeint fd)
                with _ ->
                    Error (invalidValueError "Unexpected error in ConnectNamedPipe")
            
            member this.WaitForNamedPipeConnection handle timeout =
                // For FIFO pipes, there's no explicit connection handling like in Windows
                // Pipes are considered connected when both read and write ends are open
                Ok ()
            
            member this.WriteNamedPipe handle data offset count =
                try
                    let fd = handle.ToInt32()
                    
                    // Get pointer to data
                    let dataPtr = &&data.[offset] |> NativePtr.toNativeInt
                    
                    // Write to pipe
                    let bytesWritten = NativeMethods.write(fd, dataPtr, nativeint count)
                    
                    if bytesWritten = -1 then
                        Error (invalidValueError $"Failed to write to pipe: {Helpers.getErrorMessage()}")
                    else
                        Ok bytesWritten
                with _ ->
                    Error (invalidValueError "Unexpected error in WriteNamedPipe")
            
            member this.ReadNamedPipe handle buffer offset count =
                try
                    let fd = handle.ToInt32()
                    
                    // Get pointer to buffer
                    let bufferPtr = &&buffer.[offset] |> NativePtr.toNativeInt
                    
                    // Read from pipe
                    let bytesRead = NativeMethods.read(fd, bufferPtr, nativeint count)
                    
                    if bytesRead = -1 then
                        // Check for EAGAIN (no data available, would block)
                        let errnum = 0 // In real implementation, get the error code
                        if errnum = NativeMethods.EAGAIN then
                            Ok 0
                        else
                            Error (invalidValueError $"Failed to read from pipe: {Helpers.getErrorMessage()}")
                    else
                        Ok bytesRead
                with _ ->
                    Error (invalidValueError "Unexpected error in ReadNamedPipe")
            
            member this.CloseNamedPipe handle =
                try
                    let fd = handle.ToInt32()
                    let result = NativeMethods.close(fd)
                    
                    if result <> 0 then
                        Error (invalidValueError $"Failed to close pipe: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with _ ->
                    Error (invalidValueError "Unexpected error in CloseNamedPipe")
            
            member this.CreateSharedMemory name size accessType =
                try
                    // macOS supports POSIX shared memory
                    let openFlags = Helpers.accessTypeToOpenFlags accessType true
                    let fd = NativeMethods.shm_open(name, openFlags, 0o644)
                    
                    if fd = -1 then
                        Error (invalidValueError $"Failed to create shared memory: {Helpers.getErrorMessage()}")
                    else
                        // Set the size of the shared memory
                        let sizeResult = NativeMethods.ftruncate(fd, int64 size)
                        
                        if sizeResult <> 0 then
                            NativeMethods.close(fd) |> ignore
                            Error (invalidValueError $"Failed to set shared memory size: {Helpers.getErrorMessage()}")
                        else
                            // Map the shared memory
                            let protFlags = Helpers.mappingTypeToProtectionFlags MappingType.SharedMapping accessType
                            let sizeNative = nativeint (int size)
                            
                            let address = 
                                NativeMethods.mmap(
                                    0n,                    // Let the system choose the address
                                    sizeNative,            // Size
                                    protFlags,             // Protection flags
                                    NativeMethods.MAP_SHARED, // Shared mapping
                                    fd,                    // File descriptor
                                    0L                     // Offset
                                )
                            
                            if address = NativeMethods.MAP_FAILED then
                                NativeMethods.close(fd) |> ignore
                                Error (invalidValueError $"Failed to map shared memory: {Helpers.getErrorMessage()}")
                            else
                                // Return fd as handle and mapped address
                                Ok (nativeint fd, address)
                with _ ->
                    Error (invalidValueError "Unexpected error in CreateSharedMemory")
            
            member this.OpenSharedMemory name accessType =
                try
                    // Open existing shared memory
                    let openFlags = Helpers.accessTypeToOpenFlags accessType false
                    let fd = NativeMethods.shm_open(name, openFlags, 0)
                    
                    if fd = -1 then
                        Error (invalidValueError $"Failed to open shared memory: {Helpers.getErrorMessage()}")
                    else
                        // Get the size (this is simplified; would need fstat in real implementation)
                        let size = 4096<bytes> // Default size for example
                        
                        // Map the shared memory
                        let protFlags = Helpers.mappingTypeToProtectionFlags MappingType.SharedMapping accessType
                        let sizeNative = nativeint (int size)
                        
                        let address = 
                            NativeMethods.mmap(
                                0n,                    // Let the system choose the address
                                sizeNative,            // Size
                                protFlags,             // Protection flags
                                NativeMethods.MAP_SHARED, // Shared mapping
                                fd,                    // File descriptor
                                0L                     // Offset
                            )
                        
                        if address = NativeMethods.MAP_FAILED then
                            NativeMethods.close(fd) |> ignore
                            Error (invalidValueError $"Failed to map shared memory: {Helpers.getErrorMessage()}")
                        else
                            // Return fd, address, and size
                            Ok (nativeint fd, address, size)
                with _ ->
                    Error (invalidValueError "Unexpected error in OpenSharedMemory")
            
            member this.CloseSharedMemory handle address size =
                try
                    let sizeNative = nativeint (int size)
                    
                    // First unmap the memory
                    let unmapResult = NativeMethods.munmap(address, sizeNative)
                    
                    if unmapResult <> 0 then
                        Error (invalidValueError $"Failed to unmap shared memory: {Helpers.getErrorMessage()}")
                    else
                        // Close the file descriptor
                        let fd = handle.ToInt32()
                        let closeResult = NativeMethods.close(fd)
                        
                        if closeResult <> 0 then
                            Error (invalidValueError $"Failed to close shared memory: {Helpers.getErrorMessage()}")
                        else
                            Ok ()
                with _ ->
                    Error (invalidValueError "Unexpected error in CloseSharedMemory")
            
            member this.ResourceExists name resourceType =
                match resourceType.ToLowerInvariant() with
                | "pipe" -> 
                    // Check if named pipe exists
                    let pipePath = Helpers.formatPipeName name
                    let fd = NativeMethods.open_(pipePath, NativeMethods.O_RDONLY ||| NativeMethods.O_NONBLOCK, 0)
                    if fd <> -1 then
                        NativeMethods.close(fd) |> ignore
                        true
                    else
                        false
                | "sharedmemory" | "sharedmem" | "memory" ->
                    // Check if shared memory exists
                    let fd = NativeMethods.shm_open(name, NativeMethods.O_RDONLY, 0)
                    if fd <> -1 then
                        NativeMethods.close(fd) |> ignore
                        true
                    else
                        false
                | _ -> 
                    false
    
    /// <summary>
    /// macOS implementation of network provider
    /// </summary>
    type MacOSNetworkProvider() =
        interface IPlatformNetwork with
            member this.CreateSocket addressFamily socketType protocolType =
                try
                    let sockfd = NativeMethods.socket(int addressFamily, int socketType, int protocolType)
                    
                    if sockfd = -1 then
                        Error (invalidValueError $"Failed to create socket: {Helpers.getErrorMessage()}")
                    else
                        Ok (nativeint sockfd)
                with _ ->
                    Error (invalidValueError "Unexpected error in CreateSocket")
            
            member this.BindSocket handle address port =
                try
                    let sockfd = handle.ToInt32()
                    
                    // Create and initialize sockaddr_in structure
                    // This would need proper implementation with byte manipulation
                    // Placeholder for actual implementation
                    let sockaddrSize = 16  // Size of sockaddr_in
                    let sockaddrPtr = NativePtr.stackalloc<byte> sockaddrSize |> NativePtr.toNativeInt
                    
                    // Fill in sockaddr_in structure
                    // sin_family = AF_INET
                    NativePtr.write (NativePtr.ofNativeInt<int16> sockaddrPtr) (int16 NativeMethods.AF_INET)
                    
                    // sin_port = htons(port)
                    // We'd convert port to network byte order
                    
                    // sin_addr = inet_addr(address)
                    // We'd convert address to network byte order
                    
                    // Bind
                    let result = NativeMethods.bind(sockfd, sockaddrPtr, nativeint sockaddrSize)
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to bind socket: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with _ ->
                    Error (invalidValueError "Unexpected error in BindSocket")
            
            member this.ListenSocket handle backlog =
                try
                    let sockfd = handle.ToInt32()
                    let result = NativeMethods.listen(sockfd, backlog)
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to listen on socket: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with _ ->
                    Error (invalidValueError "Unexpected error in ListenSocket")
            
            member this.AcceptSocket handle =
                try
                    let sockfd = handle.ToInt32()
                    
                    // Create sockaddr_in structure for the client address
                    let sockaddrSize = 16  // Size of sockaddr_in
                    let sockaddrPtr = NativePtr.stackalloc<byte> sockaddrSize |> NativePtr.toNativeInt
                    let sockaddrLenPtr = NativePtr.stackalloc<int> 1 |> NativePtr.toNativeInt
                    NativePtr.write (NativePtr.ofNativeInt<int> sockaddrLenPtr) sockaddrSize
                    
                    // Accept the connection
                    let clientfd = NativeMethods.accept(sockfd, sockaddrPtr, sockaddrLenPtr)
                    
                    if clientfd = -1 then
                        Error (invalidValueError $"Failed to accept connection: {Helpers.getErrorMessage()}")
                    else
                        // Extract address and port from sockaddr_in
                        // This would require proper implementation
                        let clientAddress = "0.0.0.0"  // Placeholder
                        let clientPort = 0  // Placeholder
                        
                        Ok (nativeint clientfd, clientAddress, clientPort)
                with _ ->
                    Error (invalidValueError "Unexpected error in AcceptSocket")
            
            member this.ConnectSocket handle address port =
                try
                    let sockfd = handle.ToInt32()
                    
                    // Create and initialize sockaddr_in structure
                    // This would need proper implementation
                    let sockaddrSize = 16  // Size of sockaddr_in
                    let sockaddrPtr = NativePtr.stackalloc<byte> sockaddrSize |> NativePtr.toNativeInt
                    
                    // Fill in sockaddr_in structure
                    // sin_family = AF_INET
                    NativePtr.write (NativePtr.ofNativeInt<int16> sockaddrPtr) (int16 NativeMethods.AF_INET)
                    
                    // sin_port = htons(port)
                    // We'd convert port to network byte order
                    
                    // sin_addr = inet_addr(address)
                    // We'd convert address to network byte order
                    
                    // Connect
                    let result = NativeMethods.connect(sockfd, sockaddrPtr, nativeint sockaddrSize)
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to connect socket: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with _ ->
                    Error (invalidValueError "Unexpected error in ConnectSocket")
            
            member this.SendSocket handle data offset count flags =
                try
                    let sockfd = handle.ToInt32()
                    
                    // Get pointer to data
                    let dataPtr = &&data.[offset] |> NativePtr.toNativeInt
                    
                    // Send data
                    let result = NativeMethods.send(sockfd, dataPtr, nativeint count, flags)
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to send data: {Helpers.getErrorMessage()}")
                    else
                        Ok result
                with _ ->
                    Error (invalidValueError "Unexpected error in SendSocket")
            
            member this.ReceiveSocket handle buffer offset count flags =
                try
                    let sockfd = handle.ToInt32()
                    
                    // Get pointer to buffer
                    let bufferPtr = &&buffer.[offset] |> NativePtr.toNativeInt
                    
                    // Receive data
                    let result = NativeMethods.recv(sockfd, bufferPtr, nativeint count, flags)
                    
                    if result = -1 then
                        let errnum = 0  // We'd get the error number properly
                        
                        // Check for EAGAIN/EWOULDBLOCK (would block on non-blocking socket)
                        if errnum = NativeMethods.EAGAIN then
                            Ok 0
                        else
                            Error (invalidValueError $"Failed to receive data: {Helpers.getErrorMessage()}")
                    else
                        Ok result
                with _ ->
                    Error (invalidValueError "Unexpected error in ReceiveSocket")
            
            member this.CloseSocket handle =
                try
                    let sockfd = handle.ToInt32()
                    let result = NativeMethods.close(sockfd)
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to close socket: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with _ ->
                    Error (invalidValueError "Unexpected error in CloseSocket")
            
            member this.ShutdownSocket handle how =
                try
                    let sockfd = handle.ToInt32()
                    let result = NativeMethods.shutdown(sockfd, how)
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to shutdown socket: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with _ ->
                    Error (invalidValueError "Unexpected error in ShutdownSocket")
            
            member this.SetSocketOption handle level optionName optionValue =
                try
                    let sockfd = handle.ToInt32()
                    
                    // Get pointer to option value
                    let optionValuePtr = &&optionValue.[0] |> NativePtr.toNativeInt
                    
                    // Set socket option
                    let result = NativeMethods.setsockopt(
                        sockfd,
                        level,
                        optionName,
                        optionValuePtr,
                        nativeint optionValue.Length
                    )
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to set socket option: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with _ ->
                    Error (invalidValueError "Unexpected error in SetSocketOption")
            
            member this.GetSocketOption handle level optionName optionValue =
                try
                    let sockfd = handle.ToInt32()
                    
                    // Get pointer to option value
                    let optionValuePtr = &&optionValue.[0] |> NativePtr.toNativeInt
                    
                    // Variable to receive the option length
                    let optionLenPtr = NativePtr.stackalloc<int> 1 |> NativePtr.toNativeInt
                    NativePtr.write (NativePtr.ofNativeInt<int> optionLenPtr) optionValue.Length
                    
                    // Get socket option
                    let result = NativeMethods.getsockopt(
                        sockfd,
                        level,
                        optionName,
                        optionValuePtr,
                        optionLenPtr
                    )
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to get socket option: {Helpers.getErrorMessage()}")
                    else
                        let optionLen = NativePtr.read (NativePtr.ofNativeInt<int> optionLenPtr)
                        Ok optionLen
                with _ ->
                    Error (invalidValueError "Unexpected error in GetSocketOption")
            
            member this.GetLocalEndPoint handle =
                // Not implemented for this example
                Error (invalidValueError "Not implemented")
            
            member this.GetRemoteEndPoint handle =
                // Not implemented for this example
                Error (invalidValueError "Not implemented")
            
            member this.Poll handle timeout =
                // Not implemented for this example
                Error (invalidValueError "Not implemented")
            
            member this.ResolveHostName hostName =
                // Not implemented for this example
                Error (invalidValueError "Not implemented")
    
    /// <summary>
    /// macOS implementation of synchronization provider
    /// </summary>
    type MacOSSyncProvider() =
        interface IPlatformSync with
            // Mutex operations
            member this.CreateMutex name initialOwner =
                // macOS doesn't have named mutexes
                // Can be implemented using semaphores
                Error (invalidValueError "Named mutexes not directly supported on macOS; use semaphores instead")
            
            member this.OpenMutex name =
                Error (invalidValueError "Named mutexes not directly supported on macOS; use semaphores instead")
            
            member this.AcquireMutex handle timeout =
                Error (invalidValueError "Named mutexes not directly supported on macOS; use semaphores instead")
            
            member this.ReleaseMutex handle =
                Error (invalidValueError "Named mutexes not directly supported on macOS; use semaphores instead")
            
            member this.CloseMutex handle =
                Error (invalidValueError "Named mutexes not directly supported on macOS; use semaphores instead")
            
            // Semaphore operations
            member this.CreateSemaphore name initialCount maximumCount =
                try
                    // macOS supports POSIX semaphores
                    let sem = NativeMethods.sem_open(name, NativeMethods.O_CREAT ||| NativeMethods.O_EXCL, 0o644, uint initialCount)
                    
                    if sem = 0n then
                        Error (invalidValueError $"Failed to create semaphore: {Helpers.getErrorMessage()}")
                    else
                        Ok sem
                with _ ->
                    Error (invalidValueError "Unexpected error in CreateSemaphore")
            
            member this.OpenSemaphore name =
                try
                    let sem = NativeMethods.sem_open(name, 0, 0, 0u)
                    
                    if sem = 0n then
                        Error (invalidValueError $"Failed to open semaphore: {Helpers.getErrorMessage()}")
                    else
                        Ok sem
                with _ ->
                    Error (invalidValueError "Unexpected error in OpenSemaphore")
            
            member this.AcquireSemaphore handle timeout =
                try
                    if timeout = 0 then
                        // Try to acquire without waiting
                        let result = NativeMethods.sem_trywait(handle)
                        
                        if result = 0 then
                            Ok true
                        else
                            Ok false
                    else
                        // Wait for semaphore
                        let result = NativeMethods.sem_wait(handle)
                        
                        if result = 0 then
                            Ok true
                        else
                            Error (invalidValueError $"Failed to acquire semaphore: {Helpers.getErrorMessage()}")
                with _ ->
                    Error (invalidValueError "Unexpected error in AcquireSemaphore")
            
            member this.ReleaseSemaphore handle releaseCount =
                try
                    // Get current value
                    let valuePtr = NativePtr.stackalloc<int> 1 |> NativePtr.toNativeInt
                    let getResult = NativeMethods.sem_getvalue(handle, valuePtr)
                    
                    if getResult <> 0 then
                        Error (invalidValueError $"Failed to get semaphore value: {Helpers.getErrorMessage()}")
                    else
                        let prevValue = NativePtr.read (NativePtr.ofNativeInt<int> valuePtr)
                        
                        // Post to semaphore releaseCount times
                        let mutable success = true
                        for i = 1 to releaseCount do
                            let postResult = NativeMethods.sem_post(handle)
                            if postResult <> 0 then
                                success <- false
                        
                        if success then
                            Ok prevValue
                        else
                            Error (invalidValueError $"Failed to release semaphore: {Helpers.getErrorMessage()}")
                with _ ->
                    Error (invalidValueError "Unexpected error in ReleaseSemaphore")
            
            member this.CloseSemaphore handle =
                try
                    let result = NativeMethods.sem_close(handle)
                    
                    if result <> 0 then
                        Error (invalidValueError $"Failed to close semaphore: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with _ ->
                    Error (invalidValueError "Unexpected error in CloseSemaphore")