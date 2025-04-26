namespace BAREWire.Platform.Providers

open FSharp.NativeInterop
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Core.Memory
open BAREWire.Core.Binary
open BAREWire.Core.Utf8
open BAREWire.IPC
open BAREWire.Memory.Mapping
open BAREWire.Platform.Common.Interfaces

#nowarn "9" // Disable warning about using NativePtr and fixed expressions

/// <summary>
/// Linux platform implementation
/// </summary>
module Linux =
    /// <summary>
    /// Native function declarations for Linux platform
    /// </summary>
    module private NativeMethods =
        // Linux library names
        [<Literal>]
        let libc = "libc.so.6"
        
        // Memory management constants
        [<Literal>]
        let PROT_READ = 1
        
        [<Literal>]
        let PROT_WRITE = 2
        
        [<Literal>]
        let PROT_EXEC = 4
        
        [<Literal>]
        let MAP_PRIVATE = 2
        
        [<Literal>]
        let MAP_SHARED = 1
        
        [<Literal>]
        let MAP_ANONYMOUS = 0x20
        
        [<Literal>]
        let MAP_FAILED = -1n
        
        // Shared memory constants
        [<Literal>]
        let O_RDONLY = 0
        
        [<Literal>]
        let O_RDWR = 2
        
        [<Literal>]
        let O_CREAT = 0o100
        
        [<Literal>]
        let O_EXCL = 0o200
        
        [<Literal>]
        let S_IRUSR = 0o400
        
        [<Literal>]
        let S_IWUSR = 0o200
        
        // Socket constants
        [<Literal>]
        let AF_INET = 2
        
        [<Literal>]
        let SOCK_STREAM = 1
        
        [<Literal>]
        let SOCK_DGRAM = 2
        
        [<Literal>]
        let SOL_SOCKET = 1
        
        [<Literal>]
        let SO_REUSEADDR = 2
        
        [<Literal>]
        let IPPROTO_TCP = 6
        
        [<Literal>]
        let IPPROTO_UDP = 17
        
        [<Literal>]
        let EAGAIN = 11
        
        [<Literal>]
        let EWOULDBLOCK = 11
        
        // Pipe constants
        [<Literal>]
        let F_SETFL = 4
        
        [<Literal>]
        let O_NONBLOCK = 0o4000
        
        // Socket address structure for IPv4
        [<Struct>]
        type SockAddrIn =
            val mutable sin_family: int16
            val mutable sin_port: uint16
            val mutable sin_addr: uint32
            val mutable sin_zero: byte[]
            
            new(family, port, addr) = {
                sin_family = family
                sin_port = port
                sin_addr = addr
                sin_zero = Array.zeroCreate 8
            }
        
        // Memory management functions
        let mmap (addr: nativeint) (length: unativeint) (prot: int) (flags: int) (fd: int) (offset: int64) : nativeint =
            // In real implementation, this would be a proper FFI call
            // For now, simulating with NativePtr to preserve compilation
            let ptr = NativePtr.stackalloc<nativeint> 1
            let result = if length = 0un then 0n else 1n
            NativePtr.write ptr result
            NativePtr.read ptr
            
        let munmap (addr: nativeint) (length: unativeint) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let mlock (addr: nativeint) (length: unativeint) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let munlock (addr: nativeint) (length: unativeint) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let msync (addr: nativeint) (length: unativeint) (flags: int) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
        
        // File operations
        let open_ (pathname: string) (flags: int) (mode: int) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 3 // Simulated file descriptor
            NativePtr.read ptr
            
        let close (fd: int) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let read (fd: int) (buf: nativeint) (count: unativeint) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let write (fd: int) (buf: nativeint) (count: unativeint) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr (int count)
            NativePtr.read ptr
            
        let ftruncate (fd: int) (length: int64) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        // POSIX shared memory
        let shm_open (name: string) (oflag: int) (mode: int) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 4 // Simulated shared memory fd
            NativePtr.read ptr
            
        let shm_unlink (name: string) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        // POSIX pipe
        let mkfifo (pathname: string) (mode: int) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let fcntl (fd: int) (cmd: int) (arg: int) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        // Socket operations
        let socket (domain: int) (type_: int) (protocol: int) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 5 // Simulated socket fd
            NativePtr.read ptr
            
        let bind (sockfd: int) (addr: nativeint) (addrlen: int) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let listen (sockfd: int) (backlog: int) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let accept (sockfd: int) (addr: nativeint) (addrlen: nativeint) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 6 // Simulated client socket fd
            NativePtr.read ptr
            
        let connect (sockfd: int) (addr: nativeint) (addrlen: int) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let send (sockfd: int) (buf: nativeint) (len: int) (flags: int) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr len
            NativePtr.read ptr
            
        let recv (sockfd: int) (buf: nativeint) (len: int) (flags: int) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr len
            NativePtr.read ptr
            
        let shutdown (sockfd: int) (how: int) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let setsockopt (sockfd: int) (level: int) (optname: int) (optval: nativeint) (optlen: int) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let getsockopt (sockfd: int) (level: int) (optname: int) (optval: nativeint) (optlen: nativeint) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let getsockname (sockfd: int) (addr: nativeint) (addrlen: nativeint) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let getpeername (sockfd: int) (addr: nativeint) (addrlen: nativeint) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        // Error handling
        let errno () : int =
            // Simulated implementation, should access thread-local errno
            0
            
        let strerror (errnum: int) : string =
            // Simulated implementation
            $"Error {errnum}"
            
        // Socket address conversion
        let inet_addr (ipAddress: string) : uint32 =
            // Simulated implementation - parse "a.b.c.d" to uint32
            let parts = ipAddress.Split([|'.'|])
            if parts.Length <> 4 then 0u
            else
                let a = uint32 (int parts.[0])
                let b = uint32 (int parts.[1])
                let c = uint32 (int parts.[2])
                let d = uint32 (int parts.[3])
                (a <<< 24) ||| (b <<< 16) ||| (c <<< 8) ||| d
                
        let inet_ntoa (inAddr: uint32) : string =
            // Simulated implementation - convert uint32 to "a.b.c.d"
            let a = (inAddr >>> 24) &&& 0xFFu
            let b = (inAddr >>> 16) &&& 0xFFu
            let c = (inAddr >>> 8) &&& 0xFFu
            let d = inAddr &&& 0xFFu
            $"{a}.{b}.{c}.{d}"
            
        let htons (value: uint16) : uint16 =
            // Convert from host to network byte order (big endian)
            ((value &&& 0xFFus) <<< 8) ||| ((value &&& 0xFF00us) >>> 8)
            
        let ntohs (value: uint16) : uint16 =
            // Convert from network to host byte order
            htons value
            
        // Semaphore operations
        let sem_init (sem: nativeint) (pshared: int) (value: uint) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let sem_destroy (sem: nativeint) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let sem_open (name: string) (oflag: int) (mode: int) (value: uint) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 7 // Simulated semaphore descriptor
            NativePtr.read ptr
            
        let sem_close (sem: int) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let sem_unlink (name: string) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let sem_wait (sem: int) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let sem_trywait (sem: int) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let sem_post (sem: int) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let sem_getvalue (sem: int) (sval: nativeint) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            // Write a value to sval pointer
            let valPtr = NativePtr.ofNativeInt<int> sval
            NativePtr.write valPtr 1
            NativePtr.read ptr
    
    /// <summary>
    /// Helper functions for working with Linux API
    /// </summary>
    module private Helpers =
        /// <summary>
        /// Gets the last error message
        /// </summary>
        let getErrorMessage (): string =
            let errNum = NativeMethods.errno()
            let errMsg = NativeMethods.strerror(errNum)
            $"Error {errNum}: {errMsg}"
        
        /// <summary>
        /// Creates a sockaddr_in structure
        /// </summary>
        let createSockAddrIn (addr: uint32) (port: uint16): NativeMethods.SockAddrIn =
            NativeMethods.SockAddrIn(
                int16 NativeMethods.AF_INET,
                NativeMethods.htons(port),
                addr
            )
        
        /// <summary>
        /// Converts MappingType to mmap protection flags
        /// </summary>
        let mappingTypeToProtectionFlags (mappingType: MappingType) (accessType: AccessType): int =
            match accessType with
            | AccessType.ReadOnly -> NativeMethods.PROT_READ
            | AccessType.ReadWrite -> NativeMethods.PROT_READ ||| NativeMethods.PROT_WRITE
        
        /// <summary>
        /// Converts MappingType to mmap flags
        /// </summary>
        let mappingTypeToFlags (mappingType: MappingType): int =
            match mappingType with
            | MappingType.PrivateMapping -> NativeMethods.MAP_PRIVATE ||| NativeMethods.MAP_ANONYMOUS
            | MappingType.SharedMapping -> NativeMethods.MAP_SHARED ||| NativeMethods.MAP_ANONYMOUS
        
        /// <summary>
        /// Converts AccessType to open flags
        /// </summary>
        let accessTypeToOpenFlags (accessType: AccessType): int =
            match accessType with
            | AccessType.ReadOnly -> NativeMethods.O_RDONLY
            | AccessType.ReadWrite -> NativeMethods.O_RDWR
        
        /// <summary>
        /// Formats a named pipe path
        /// </summary>
        let formatPipeName (name: string): string =
            $"/tmp/{name}"
    
    /// <summary>
    /// Linux implementation of the memory provider
    /// </summary>
    type LinuxMemoryProvider() =
        interface IPlatformMemory with
            member this.MapMemory size mappingType accessType =
                try
                    let sizeUInt = unativeint (int size)
                    let protectionFlags = Helpers.mappingTypeToProtectionFlags mappingType accessType
                    let mapFlags = Helpers.mappingTypeToFlags mappingType
                    
                    let address = 
                        NativeMethods.mmap(
                            0n,                // Let the system determine where to allocate the region
                            sizeUInt,          // The size of the region
                            protectionFlags,   // The memory protection for the region of pages
                            mapFlags,          // The type of mapping
                            -1,                // Not backed by a file
                            0L                 // Offset (ignored for MAP_ANONYMOUS)
                        )
                    
                    if address = NativeMethods.MAP_FAILED then
                        Error (invalidValueError $"Failed to allocate memory: {Helpers.getErrorMessage()}")
                    else
                        // For Linux, we'll use the address as both the handle and the address
                        Ok (address, address)
                with ex ->
                    Error (invalidValueError $"Failed to map memory: {ex.Message}")
            
            member this.UnmapMemory handle address size =
                try
                    // In our Linux implementation, the handle and address are the same
                    let result = NativeMethods.munmap(address, unativeint (int size))
                    
                    if result <> 0 then
                        Error (invalidValueError $"Failed to free memory: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with ex ->
                    Error (invalidValueError $"Failed to unmap memory: {ex.Message}")
            
            member this.MapFile filePath offset size accessType =
                try
                    let openFlags = Helpers.accessTypeToOpenFlags accessType
                    
                    let fd = NativeMethods.open_(filePath, openFlags, 0o644)
                    
                    if fd = -1 then
                        Error (invalidValueError $"Failed to open file: {Helpers.getErrorMessage()}")
                    else
                        try
                            let protectionFlags = Helpers.mappingTypeToProtectionFlags MappingType.PrivateMapping accessType
                            let mapFlags = 
                                match accessType with
                                | AccessType.ReadOnly -> NativeMethods.MAP_PRIVATE
                                | AccessType.ReadWrite -> NativeMethods.MAP_SHARED
                            
                            let address = 
                                NativeMethods.mmap(
                                    0n,                        // Let the system determine where to allocate the region
                                    unativeint (int size),     // The size of the region
                                    protectionFlags,           // The memory protection for the region of pages
                                    mapFlags,                  // The type of mapping
                                    fd,                        // File descriptor
                                    int64 offset               // Offset within the file
                                )
                            
                            if address = NativeMethods.MAP_FAILED then
                                NativeMethods.close(fd) |> ignore
                                Error (invalidValueError $"Failed to map file: {Helpers.getErrorMessage()}")
                            else
                                // For Linux, we'll use the file descriptor as the handle
                                Ok (nativeint fd, address)
                        with ex ->
                            NativeMethods.close(fd) |> ignore
                            Error (invalidValueError $"Failed to map file: {ex.Message}")
                with ex ->
                    Error (invalidValueError $"Failed to map file: {ex.Message}")
            
            member this.FlushMappedFile handle address size =
                try
                    let result = NativeMethods.msync(address, unativeint (int size), 4) // MS_SYNC
                    
                    if result <> 0 then
                        Error (invalidValueError $"Failed to flush mapped file: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with ex ->
                    Error (invalidValueError $"Failed to flush mapped file: {ex.Message}")
            
            member this.LockMemory address size =
                try
                    let result = NativeMethods.mlock(address, unativeint (int size))
                    
                    if result <> 0 then
                        Error (invalidValueError $"Failed to lock memory: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with ex ->
                    Error (invalidValueError $"Failed to lock memory: {ex.Message}")
            
            member this.UnlockMemory address size =
                try
                    let result = NativeMethods.munlock(address, unativeint (int size))
                    
                    if result <> 0 then
                        Error (invalidValueError $"Failed to unlock memory: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with ex ->
                    Error (invalidValueError $"Failed to unlock memory: {ex.Message}")
    
    /// <summary>
    /// Linux implementation of the IPC provider
    /// </summary>
    type LinuxIpcProvider() =
        interface IPlatformIpc with
            member this.CreateNamedPipe name direction mode bufferSize =
                try
                    let pipePath = Helpers.formatPipeName name
                    
                    // Create the FIFO (named pipe)
                    let result = NativeMethods.mkfifo(pipePath, 0o666)
                    
                    if result <> 0 then
                        Error (invalidValueError $"Failed to create named pipe: {Helpers.getErrorMessage()}")
                    else
                        // Open the pipe
                        let openFlags = 
                            match direction with
                            | NamedPipe.PipeDirection.In -> NativeMethods.O_RDONLY
                            | NamedPipe.PipeDirection.Out -> NativeMethods.O_RDWR
                            | NamedPipe.PipeDirection.InOut -> NativeMethods.O_RDWR
                        
                        let fd = NativeMethods.open_(pipePath, openFlags, 0)
                        
                        if fd = -1 then
                            Error (invalidValueError $"Failed to open named pipe: {Helpers.getErrorMessage()}")
                        else
                            // Set the pipe to non-blocking mode
                            NativeMethods.fcntl(fd, NativeMethods.F_SETFL, NativeMethods.O_NONBLOCK) |> ignore
                            
                            Ok (nativeint fd)
                with ex ->
                    Error (invalidValueError $"Failed to create named pipe: {ex.Message}")
            
            member this.ConnectNamedPipe name direction =
                try
                    let pipePath = Helpers.formatPipeName name
                    
                    // Open the pipe
                    let openFlags = 
                        match direction with
                        | NamedPipe.PipeDirection.In -> NativeMethods.O_RDONLY
                        | NamedPipe.PipeDirection.Out -> NativeMethods.O_RDWR
                        | NamedPipe.PipeDirection.InOut -> NativeMethods.O_RDWR
                    
                    let fd = NativeMethods.open_(pipePath, openFlags, 0)
                    
                    if fd = -1 then
                        Error (invalidValueError $"Failed to connect to named pipe: {Helpers.getErrorMessage()}")
                    else
                        // Set the pipe to non-blocking mode
                        NativeMethods.fcntl(fd, NativeMethods.F_SETFL, NativeMethods.O_NONBLOCK) |> ignore
                        
                        Ok (nativeint fd)
                with ex ->
                    Error (invalidValueError $"Failed to connect to named pipe: {ex.Message}")
            
            member this.WaitForNamedPipeConnection handle timeout =
                // In Linux, creating or opening a named pipe (FIFO) doesn't require an explicit connection
                // The pipe is considered "connected" when both a reader and a writer have opened it
                Ok ()
            
            member this.WriteNamedPipe handle data offset count =
                try
                    let fd = handle.ToInt32()
                    
                    // Pin the array to get a fixed pointer and ensure GC doesn't move it
                    fixed data.[offset..(offset + count - 1)] (fun ptr ->
                        let bytesWritten = NativeMethods.write(fd, NativePtr.toNativeInt ptr, unativeint count)
                        
                        if bytesWritten = -1 then
                            let errorCode = NativeMethods.errno()
                            
                            // EAGAIN or EWOULDBLOCK means the pipe would block (no readers available)
                            if errorCode = NativeMethods.EAGAIN || errorCode = NativeMethods.EWOULDBLOCK then
                                Ok 0
                            else
                                Error (invalidValueError $"Failed to write to pipe: {Helpers.getErrorMessage()}")
                        else
                            Ok bytesWritten
                    )
                with ex ->
                    Error (invalidValueError $"Failed to write to pipe: {ex.Message}")
            
            member this.ReadNamedPipe handle buffer offset count =
                try
                    let fd = handle.ToInt32()
                    
                    // Pin the array to get a fixed pointer and ensure GC doesn't move it
                    fixed buffer.[offset..(offset + count - 1)] (fun ptr ->
                        let bytesRead = NativeMethods.read(fd, NativePtr.toNativeInt ptr, unativeint count)
                        
                        if bytesRead = -1 then
                            let errorCode = NativeMethods.errno()
                            
                            // EAGAIN or EWOULDBLOCK means the pipe would block (no data available)
                            if errorCode = NativeMethods.EAGAIN || errorCode = NativeMethods.EWOULDBLOCK then
                                Ok 0
                            else
                                Error (invalidValueError $"Failed to read from pipe: {Helpers.getErrorMessage()}")
                        else
                            Ok bytesRead
                    )
                with ex ->
                    Error (invalidValueError $"Failed to read from pipe: {ex.Message}")
            
            member this.CloseNamedPipe handle =
                try
                    let fd = handle.ToInt32()
                    
                    let result = NativeMethods.close(fd)
                    
                    if result <> 0 then
                        Error (invalidValueError $"Failed to close pipe: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with ex ->
                    Error (invalidValueError $"Failed to close pipe: {ex.Message}")
            
            member this.CreateSharedMemory name size accessType =
                try
                    let openFlags = Helpers.accessTypeToOpenFlags accessType ||| NativeMethods.O_CREAT ||| NativeMethods.O_EXCL
                    let mode = NativeMethods.S_IRUSR ||| NativeMethods.S_IWUSR
                    
                    let fd = NativeMethods.shm_open(name, openFlags, mode)
                    
                    if fd = -1 then
                        Error (invalidValueError $"Failed to create shared memory: {Helpers.getErrorMessage()}")
                    else
                        try
                            // Set the size of the shared memory object
                            let result = NativeMethods.ftruncate(fd, int64 size)
                            
                            if result <> 0 then
                                NativeMethods.close(fd) |> ignore
                                NativeMethods.shm_unlink(name) |> ignore
                                Error (invalidValueError $"Failed to set shared memory size: {Helpers.getErrorMessage()}")
                            else
                                let protectionFlags = Helpers.mappingTypeToProtectionFlags MappingType.SharedMapping accessType
                                
                                let address = 
                                    NativeMethods.mmap(
                                        0n,                            // Let the system determine where to allocate the region
                                        unativeint (int size),         // The size of the region
                                        protectionFlags,               // The memory protection for the region of pages
                                        NativeMethods.MAP_SHARED,      // The type of mapping
                                        fd,                            // File descriptor
                                        0L                             // Offset
                                    )
                                
                                if address = NativeMethods.MAP_FAILED then
                                    NativeMethods.close(fd) |> ignore
                                    NativeMethods.shm_unlink(name) |> ignore
                                    Error (invalidValueError $"Failed to map shared memory: {Helpers.getErrorMessage()}")
                                else
                                    // For Linux, we'll store the file descriptor and the shm name with the handle
                                    Ok (nativeint fd, address)
                        with ex ->
                            NativeMethods.close(fd) |> ignore
                            NativeMethods.shm_unlink(name) |> ignore
                            Error (invalidValueError $"Failed to create shared memory: {ex.Message}")
                with ex ->
                    Error (invalidValueError $"Failed to create shared memory: {ex.Message}")
            
            member this.OpenSharedMemory name accessType =
                try
                    let openFlags = Helpers.accessTypeToOpenFlags accessType
                    
                    let fd = NativeMethods.shm_open(name, openFlags, 0)
                    
                    if fd = -1 then
                        Error (invalidValueError $"Failed to open shared memory: {Helpers.getErrorMessage()}")
                    else
                        try
                            // Get the size of the shared memory object (we would use fstat in a full implementation)
                            // For simplicity, we use a fixed size as this is just a placeholder
                            let size = 4096<bytes>
                            
                            let protectionFlags = Helpers.mappingTypeToProtectionFlags MappingType.SharedMapping accessType
                            
                            let address = 
                                NativeMethods.mmap(
                                    0n,                            // Let the system determine where to allocate the region
                                    unativeint (int size),         // The size of the region
                                    protectionFlags,               // The memory protection for the region of pages
                                    NativeMethods.MAP_SHARED,      // The type of mapping
                                    fd,                            // File descriptor
                                    0L                             // Offset
                                )
                            
                            if address = NativeMethods.MAP_FAILED then
                                NativeMethods.close(fd) |> ignore
                                Error (invalidValueError $"Failed to map shared memory: {Helpers.getErrorMessage()}")
                            else
                                Ok (nativeint fd, address, size)
                        with ex ->
                            NativeMethods.close(fd) |> ignore
                            Error (invalidValueError $"Failed to open shared memory: {ex.Message}")
                with ex ->
                    Error (invalidValueError $"Failed to open shared memory: {ex.Message}")
            
            member this.CloseSharedMemory handle address size =
                try
                    // First unmap the memory
                    let result = NativeMethods.munmap(address, unativeint (int size))
                    
                    if result <> 0 then
                        Error (invalidValueError $"Failed to unmap shared memory: {Helpers.getErrorMessage()}")
                    else
                        // Then close the file descriptor
                        let fd = handle.ToInt32()
                        let closeResult = NativeMethods.close(fd)
                        
                        if closeResult <> 0 then
                            Error (invalidValueError $"Failed to close shared memory: {Helpers.getErrorMessage()}")
                        else
                            Ok ()
                with ex ->
                    Error (invalidValueError $"Failed to close shared memory: {ex.Message}")
            
            member this.ResourceExists name resourceType =
                try
                    match resourceType.ToLowerInvariant() with
                    | "pipe" ->
                        let pipePath = Helpers.formatPipeName name
                        let fd = NativeMethods.open_(pipePath, NativeMethods.O_RDONLY ||| NativeMethods.O_NONBLOCK, 0)
                        
                        if fd <> -1 then
                            NativeMethods.close(fd) |> ignore
                            true
                        else
                            false
                    
                    | "sharedmemory" | "sharedmem" | "memory" ->
                        let fd = NativeMethods.shm_open(name, NativeMethods.O_RDONLY, 0)
                        
                        if fd <> -1 then
                            NativeMethods.close(fd) |> ignore
                            true
                        else
                            false
                    
                    | _ ->
                        false
                with _ ->
                    false
    
    /// <summary>
    /// Linux implementation of the network provider
    /// </summary>
    type LinuxNetworkProvider() =
        interface IPlatformNetwork with
            member this.CreateSocket addressFamily socketType protocolType =
                try
                    // Create a socket using the Linux socket API
                    let fd = 
                        NativeMethods.socket(
                            int addressFamily,  // Address family
                            int socketType,     // Socket type
                            int protocolType    // Protocol type
                        )
                    
                    if fd = -1 then
                        Error (invalidValueError $"Failed to create socket: {Helpers.getErrorMessage()}")
                    else
                        Ok (nativeint fd)
                with ex ->
                    Error (invalidValueError $"Failed to create socket: {ex.Message}")
            
            member this.BindSocket handle address port =
                try
                    let fd = handle.ToInt32()
                    
                    // Create a sockaddr structure
                    let mutable sockAddr = 
                        if address = "0.0.0.0" || String.IsNullOrEmpty(address) then
                            // INADDR_ANY = 0
                            Helpers.createSockAddrIn(0u, uint16 port)
                        else
                            // Convert the address to network byte order
                            let addr = NativeMethods.inet_addr(address)
                            Helpers.createSockAddrIn(addr, uint16 port)
                    
                    // Bind the socket
                    let result = 
                        fixed sockAddr (fun ptr ->
                            NativeMethods.bind(
                                fd,
                                NativePtr.toNativeInt ptr,
                                sizeof<NativeMethods.SockAddrIn>
                            )
                        )
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to bind socket: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with ex ->
                    Error (invalidValueError $"Failed to bind socket: {ex.Message}")
            
            member this.ListenSocket handle backlog =
                try
                    let fd = handle.ToInt32()
                    
                    // Start listening on the socket
                    let result = NativeMethods.listen(fd, backlog)
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to listen on socket: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with ex ->
                    Error (invalidValueError $"Failed to listen on socket: {ex.Message}")
            
            member this.AcceptSocket handle =
                try
                    let fd = handle.ToInt32()
                    
                    // Create a sockaddr structure to receive the client address
                    let mutable clientAddr = Helpers.createSockAddrIn(0u, 0us)
                    let mutable addrLen = sizeof<NativeMethods.SockAddrIn>
                    
                    // Accept the connection
                    let clientFd = 
                        fixed clientAddr (fun sockAddrPtr ->
                            fixed &addrLen (fun addrLenPtr ->
                                NativeMethods.accept(
                                    fd,
                                    NativePtr.toNativeInt sockAddrPtr,
                                    NativePtr.toNativeInt addrLenPtr
                                )
                            )
                        )
                    
                    if clientFd = -1 then
                        Error (invalidValueError $"Failed to accept connection: {Helpers.getErrorMessage()}")
                    else
                        // Extract the client address and port
                        let addr = NativeMethods.inet_ntoa(clientAddr.sin_addr)
                        let port = int (NativeMethods.ntohs(clientAddr.sin_port))
                        
                        Ok (nativeint clientFd, addr, port)
                with ex ->
                    Error (invalidValueError $"Failed to accept connection: {ex.Message}")
            
            member this.ConnectSocket handle address port =
                try
                    let fd = handle.ToInt32()
                    
                    // Convert the address to network byte order
                    let addr = NativeMethods.inet_addr(address)
                    
                    // Create a sockaddr structure
                    let mutable sockAddr = Helpers.createSockAddrIn(addr, uint16 port)
                    
                    // Connect the socket
                    let result = 
                        fixed sockAddr (fun ptr ->
                            NativeMethods.connect(
                                fd,
                                NativePtr.toNativeInt ptr,
                                sizeof<NativeMethods.SockAddrIn>
                            )
                        )
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to connect socket: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with ex ->
                    Error (invalidValueError $"Failed to connect socket: {ex.Message}")
            
            member this.SendSocket handle data offset count flags =
                try
                    let fd = handle.ToInt32()
                    
                    // Pin the array to get a fixed pointer and ensure GC doesn't move it
                    fixed data.[offset..(offset + count - 1)] (fun ptr ->
                        // Send the data
                        let result = 
                            NativeMethods.send(
                                fd,
                                NativePtr.toNativeInt ptr,
                                count,
                                flags
                            )
                        
                        if result = -1 then
                            Error (invalidValueError $"Failed to send data: {Helpers.getErrorMessage()}")
                        else
                            Ok result
                    )
                with ex ->
                    Error (invalidValueError $"Failed to send data: {ex.Message}")
            
            member this.ReceiveSocket handle buffer offset count flags =
                try
                    let fd = handle.ToInt32()
                    
                    // Pin the array to get a fixed pointer and ensure GC doesn't move it
                    fixed buffer.[offset..(offset + count - 1)] (fun ptr ->
                        // Receive data
                        let result = 
                            NativeMethods.recv(
                                fd,
                                NativePtr.toNativeInt ptr,
                                count,
                                flags
                            )
                        
                        if result = -1 then
                            let errorCode = NativeMethods.errno()
                            
                            // EAGAIN or EWOULDBLOCK means no data available in non-blocking mode
                            if errorCode = NativeMethods.EAGAIN || errorCode = NativeMethods.EWOULDBLOCK then
                                Ok 0
                            else
                                Error (invalidValueError $"Failed to receive data: {Helpers.getErrorMessage()}")
                        else
                            Ok result
                    )
                with ex ->
                    Error (invalidValueError $"Failed to receive data: {ex.Message}")
            
            member this.CloseSocket handle =
                try
                    let fd = handle.ToInt32()
                    
                    // Close the socket
                    let result = NativeMethods.close(fd)
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to close socket: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with ex ->
                    Error (invalidValueError $"Failed to close socket: {ex.Message}")
            
            member this.ShutdownSocket handle how =
                try
                    let fd = handle.ToInt32()
                    
                    // Shut down the socket
                    let result = NativeMethods.shutdown(fd, how)
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to shut down socket: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with ex ->
                    Error (invalidValueError $"Failed to shut down socket: {ex.Message}")
            
            member this.SetSocketOption handle level optionName optionValue =
                try
                    let fd = handle.ToInt32()
                    
                    // Pin the array to get a fixed pointer and ensure GC doesn't move it
                    fixed optionValue (fun ptr ->
                        // Set the socket option
                        let result = 
                            NativeMethods.setsockopt(
                                fd,
                                level,
                                optionName,
                                NativePtr.toNativeInt ptr,
                                optionValue.Length
                            )
                        
                        if result = -1 then
                            Error (invalidValueError $"Failed to set socket option: {Helpers.getErrorMessage()}")
                        else
                            Ok ()
                    )
                with ex ->
                    Error (invalidValueError $"Failed to set socket option: {ex.Message}")
            
            member this.GetSocketOption handle level optionName optionValue =
                try
                    let fd = handle.ToInt32()
                    
                    // Pin the array to get a fixed pointer and ensure GC doesn't move it
                    fixed optionValue (fun ptr ->
                        // Variable to receive the option length
                        let mutable optionLen = optionValue.Length
                        
                        // Get the socket option
                        let result = 
                            fixed &optionLen (fun optionLenPtr ->
                                NativeMethods.getsockopt(
                                    fd,
                                    level,
                                    optionName,
                                    NativePtr.toNativeInt ptr,
                                    NativePtr.toNativeInt optionLenPtr
                                )
                            )
                        
                        if result = -1 then
                            Error (invalidValueError $"Failed to get socket option: {Helpers.getErrorMessage()}")
                        else
                            Ok optionLen
                    )
                with ex ->
                    Error (invalidValueError $"Failed to get socket option: {ex.Message}")
            
            member this.GetLocalEndPoint handle =
                try
                    let fd = handle.ToInt32()
                    
                    // Create a sockaddr structure to receive the local address
                    let mutable localAddr = Helpers.createSockAddrIn(0u, 0us)
                    let mutable addrLen = sizeof<NativeMethods.SockAddrIn>
                    
                    // Get the local address
                    let result = 
                        fixed localAddr (fun sockAddrPtr ->
                            fixed &addrLen (fun addrLenPtr ->
                                NativeMethods.getsockname(
                                    fd,
                                    NativePtr.toNativeInt sockAddrPtr,
                                    NativePtr.toNativeInt addrLenPtr
                                )
                            )
                        )
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to get local endpoint: {Helpers.getErrorMessage()}")
                    else
                        // Extract the local address and port
                        let addr = NativeMethods.inet_ntoa(localAddr.sin_addr)
                        let port = int (NativeMethods.ntohs(localAddr.sin_port))
                        
                        Ok (addr, port)
                with ex ->
                    Error (invalidValueError $"Failed to get local endpoint: {ex.Message}")
            
            member this.GetRemoteEndPoint handle =
                try
                    let fd = handle.ToInt32()
                    
                    // Create a sockaddr structure to receive the remote address
                    let mutable remoteAddr = Helpers.createSockAddrIn(0u, 0us)
                    let mutable addrLen = sizeof<NativeMethods.SockAddrIn>
                    
                    // Get the remote address
                    let result = 
                        fixed remoteAddr (fun sockAddrPtr ->
                            fixed &addrLen (fun addrLenPtr ->
                                NativeMethods.getpeername(
                                    fd,
                                    NativePtr.toNativeInt sockAddrPtr,
                                    NativePtr.toNativeInt addrLenPtr
                                )
                            )
                        )
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to get remote endpoint: {Helpers.getErrorMessage()}")
                    else
                        // Extract the remote address and port
                        let addr = NativeMethods.inet_ntoa(remoteAddr.sin_addr)
                        let port = int (NativeMethods.ntohs(remoteAddr.sin_port))
                        
                        Ok (addr, port)
                with ex ->
                    Error (invalidValueError $"Failed to get remote endpoint: {ex.Message}")
            
            member this.Poll handle timeout =
                // This would be implemented with select() or poll()
                // For simplicity, returning a simulated value
                Ok false
            
            member this.ResolveHostName hostName =
                // This would use the gethostbyname native function
                // For simplicity, returning a placeholder result
                Ok [| "127.0.0.1" |]
    
    /// <summary>
    /// Linux implementation of the synchronization provider
    /// </summary>
    type LinuxSyncProvider() =
        interface IPlatformSync with
            // Mutex operations
            member this.CreateMutex name initialOwner =
                // For Linux, we use POSIX semaphores for both mutexes and semaphores
                // A mutex is just a semaphore with an initial count of 1
                try
                    if String.IsNullOrEmpty(name) then
                        // For unnamed mutexes, we'd allocate memory and initialize a pthread_mutex_t
                        // This is a placeholder implementation
                        Error (invalidValueError "Unnamed mutexes not implemented")
                    else
                        // Create a named semaphore with initial value 1 for mutex semantics
                        let openFlags = NativeMethods.O_CREAT ||| NativeMethods.O_EXCL
                        let mode = NativeMethods.S_IRUSR ||| NativeMethods.S_IWUSR
                        let initialValue = if initialOwner then 0u else 1u
                        
                        let semaphore = NativeMethods.sem_open(name, openFlags, mode, initialValue)
                        
                        if semaphore = -1 then
                            Error (invalidValueError $"Failed to create mutex: {Helpers.getErrorMessage()}")
                        else
                            Ok (nativeint semaphore)
                with ex ->
                    Error (invalidValueError $"Failed to create mutex: {ex.Message}")
            
            member this.OpenMutex name =
                try
                    // Open an existing named semaphore
                    let semaphore = NativeMethods.sem_open(name, 0, 0, 0u)
                    
                    if semaphore = -1 then
                        Error (invalidValueError $"Failed to open mutex: {Helpers.getErrorMessage()}")
                    else
                        Ok (nativeint semaphore)
                with ex ->
                    Error (invalidValueError $"Failed to open mutex: {ex.Message}")
            
            member this.AcquireMutex handle timeout =
                try
                    let semaphore = handle.ToInt32()
                    
                    // For non-blocking attempt
                    if timeout = 0 then
                        let result = NativeMethods.sem_trywait(semaphore)
                        if result = 0 then
                            Ok true
                        else
                            let error = NativeMethods.errno()
                            if error = NativeMethods.EAGAIN then
                                Ok false // Would block, which is expected
                            else
                                Error (invalidValueError $"Failed to acquire mutex: {Helpers.getErrorMessage()}")
                    else
                        // For blocking attempt
                        let result = NativeMethods.sem_wait(semaphore)
                        if result = 0 then
                            Ok true
                        else
                            Error (invalidValueError $"Failed to acquire mutex: {Helpers.getErrorMessage()}")
                with ex ->
                    Error (invalidValueError $"Failed to acquire mutex: {ex.Message}")
            
            member this.ReleaseMutex handle =
                try
                    let semaphore = handle.ToInt32()
                    let result = NativeMethods.sem_post(semaphore)
                    
                    if result = 0 then
                        Ok ()
                    else
                        Error (invalidValueError $"Failed to release mutex: {Helpers.getErrorMessage()}")
                with ex ->
                    Error (invalidValueError $"Failed to release mutex: {ex.Message}")
            
            member this.CloseMutex handle =
                try
                    let semaphore = handle.ToInt32()
                    let result = NativeMethods.sem_close(semaphore)
                    
                    if result = 0 then
                        Ok ()
                    else
                        Error (invalidValueError $"Failed to close mutex: {Helpers.getErrorMessage()}")
                with ex ->
                    Error (invalidValueError $"Failed to close mutex: {ex.Message}")
            
            // Semaphore operations
            member this.CreateSemaphore name initialCount maximumCount =
                try
                    if String.IsNullOrEmpty(name) then
                        // For unnamed semaphores, we'd use sem_init
                        // This is a placeholder implementation
                        Error (invalidValueError "Unnamed semaphores not implemented")
                    else
                        // Create a named semaphore
                        let openFlags = NativeMethods.O_CREAT ||| NativeMethods.O_EXCL
                        let mode = NativeMethods.S_IRUSR ||| NativeMethods.S_IWUSR
                        
                        let semaphore = NativeMethods.sem_open(name, openFlags, mode, uint initialCount)
                        
                        if semaphore = -1 then
                            Error (invalidValueError $"Failed to create semaphore: {Helpers.getErrorMessage()}")
                        else
                            Ok (nativeint semaphore)
                with ex ->
                    Error (invalidValueError $"Failed to create semaphore: {ex.Message}")
            
            member this.OpenSemaphore name =
                try
                    // Open an existing named semaphore
                    let semaphore = NativeMethods.sem_open(name, 0, 0, 0u)
                    
                    if semaphore = -1 then
                        Error (invalidValueError $"Failed to open semaphore: {Helpers.getErrorMessage()}")
                    else
                        Ok (nativeint semaphore)
                with ex ->
                    Error (invalidValueError $"Failed to open semaphore: {ex.Message}")
            
            member this.AcquireSemaphore handle timeout =
                try
                    let semaphore = handle.ToInt32()
                    
                    // For non-blocking attempt
                    if timeout = 0 then
                        let result = NativeMethods.sem_trywait(semaphore)
                        if result = 0 then
                            Ok true
                        else
                            let error = NativeMethods.errno()
                            if error = NativeMethods.EAGAIN then
                                Ok false // Would block, which is expected
                            else
                                Error (invalidValueError $"Failed to acquire semaphore: {Helpers.getErrorMessage()}")
                    else
                        // For blocking attempt
                        let result = NativeMethods.sem_wait(semaphore)
                        if result = 0 then
                            Ok true
                        else
                            Error (invalidValueError $"Failed to acquire semaphore: {Helpers.getErrorMessage()}")
                with ex ->
                    Error (invalidValueError $"Failed to acquire semaphore: {ex.Message}")
            
            member this.ReleaseSemaphore handle releaseCount =
                try
                    let semaphore = handle.ToInt32()
                    
                    // Get current value first
                    let mutable value = 0
                    let valuePtr = &&value |> NativePtr.toNativeInt
                    let getResult = NativeMethods.sem_getvalue(semaphore, valuePtr)
                    
                    if getResult <> 0 then
                        Error (invalidValueError $"Failed to get semaphore value: {Helpers.getErrorMessage()}")
                    else
                        let previousCount = value
                        
                        // Post to the semaphore releaseCount times
                        let mutable success = true
                        let mutable errorMsg = ""
                        
                        for i = 1 to releaseCount do
                            let result = NativeMethods.sem_post(semaphore)
                            if result <> 0 then
                                success <- false
                                errorMsg <- Helpers.getErrorMessage()
                        
                        if success then
                            Ok previousCount
                        else
                            Error (invalidValueError $"Failed to release semaphore: {errorMsg}")
                with ex ->
                    Error (invalidValueError $"Failed to release semaphore: {ex.Message}")
            
            member this.CloseSemaphore handle =
                try
                    let semaphore = handle.ToInt32()
                    let result = NativeMethods.sem_close(semaphore)
                    
                    if result = 0 then
                        Ok ()
                    else
                        Error (invalidValueError $"Failed to close semaphore: {Helpers.getErrorMessage()}")
                with ex ->
                    Error (invalidValueError $"Failed to close semaphore: {ex.Message}")