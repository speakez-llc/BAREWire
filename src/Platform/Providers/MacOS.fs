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
/// macOS platform implementation
/// </summary>
module MacOS =
    /// <summary>
    /// Native function declarations for macOS platform
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
        
        // Mode constants
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
        let SOL_SOCKET = 0xffff
        
        [<Literal>]
        let SO_REUSEADDR = 0x0004
        
        [<Literal>]
        let IPPROTO_TCP = 6
        
        [<Literal>]
        let IPPROTO_UDP = 17
        
        [<Literal>]
        let EAGAIN = 35
        
        // Pipe constants
        [<Literal>]
        let F_SETFL = 4
        
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
        
        // Memory management functions - using FFI without System dependencies
        // These would be actual FFI calls in the real implementation
        let mmap (addr: nativeint) (length: nativeint) (prot: int) (flags: int) (fd: int) (offset: int64) : nativeint =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<nativeint> 1
            let result = if length = 0n then 0n else 1n
            NativePtr.write ptr result
            NativePtr.read ptr
            
        let munmap (addr: nativeint) (length: nativeint) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let mlock (addr: nativeint) (length: nativeint) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let munlock (addr: nativeint) (length: nativeint) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let msync (addr: nativeint) (length: nativeint) (flags: int) : int =
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
            
        let read (fd: int) (buf: nativeint) (count: nativeint) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let write (fd: int) (buf: nativeint) (count: nativeint) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr (int count)
            NativePtr.read ptr
            
        let ftruncate (fd: int) (length: int64) : int =
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
        
        // Socket operations
        let socket (domain: int) (type_: int) (protocol: int) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 5 // Simulated socket fd
            NativePtr.read ptr
            
        let bind (sockfd: int) (addr: nativeint) (addrlen: nativeint) : int =
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
            
        let connect (sockfd: int) (addr: nativeint) (addrlen: nativeint) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let send (sockfd: int) (buf: nativeint) (len: nativeint) (flags: int) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr (int len)
            NativePtr.read ptr
            
        let recv (sockfd: int) (buf: nativeint) (len: nativeint) (flags: int) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr (int len)
            NativePtr.read ptr
            
        let setsockopt (sockfd: int) (level: int) (optname: int) (optval: nativeint) (optlen: nativeint) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let getsockopt (sockfd: int) (level: int) (optname: int) (optval: nativeint) (optlen: nativeint) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let shutdown (sockfd: int) (how: int) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        // POSIX semaphore functions
        let sem_open (name: string) (oflag: int) (mode: int) (value: uint) : nativeint =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<nativeint> 1
            NativePtr.write ptr 7n // Simulated semaphore handle
            NativePtr.read ptr
            
        let sem_close (sem: nativeint) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let sem_unlink (name: string) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let sem_wait (sem: nativeint) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let sem_trywait (sem: nativeint) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let sem_post (sem: nativeint) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            NativePtr.read ptr
            
        let sem_getvalue (sem: nativeint) (sval: nativeint) : int =
            // Simulated implementation
            let ptr = NativePtr.stackalloc<int> 1
            NativePtr.write ptr 0
            // Write a value to sval pointer
            let valPtr = NativePtr.ofNativeInt<int> sval
            NativePtr.write valPtr 1
            NativePtr.read ptr
            
        // Error handling
        let errno () : int =
            // Simulated implementation
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
    
    /// <summary>
    /// Helper functions for macOS implementation
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
        /// Converts MappingType to protection flags
        /// </summary>
        let mappingTypeToProtectionFlags (mappingType: MappingType) (accessType: AccessType): int =
            match accessType with
            | AccessType.ReadOnly -> NativeMethods.PROT_READ
            | AccessType.ReadWrite -> NativeMethods.PROT_READ ||| NativeMethods.PROT_WRITE
        
        /// <summary>
        /// Converts MappingType to mapping flags
        /// </summary>
        let mappingTypeToMapFlags (mappingType: MappingType): int =
            match mappingType with
            | MappingType.PrivateMapping -> NativeMethods.MAP_PRIVATE ||| NativeMethods.MAP_ANON
            | MappingType.SharedMapping -> NativeMethods.MAP_SHARED ||| NativeMethods.MAP_ANON
        
        /// <summary>
        /// Converts AccessType to open flags
        /// </summary>
        let accessTypeToOpenFlags (accessType: AccessType) (create: bool): int =
            match accessType with
            | AccessType.ReadOnly -> NativeMethods.O_RDONLY
            | AccessType.ReadWrite -> 
                if create then
                    NativeMethods.O_RDWR ||| NativeMethods.O_CREAT
                else
                    NativeMethods.O_RDWR
        
        /// <summary>
        /// Format a named pipe path
        /// </summary>
        let formatPipeName (name: string): string =
            $"/tmp/{name}"
    
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
                with ex ->
                    Error (invalidValueError $"Failed to map memory: {ex.Message}")
            
            member this.UnmapMemory handle address size =
                try
                    let sizeNative = nativeint (int size)
                    let result = NativeMethods.munmap(address, sizeNative)
                    
                    if result <> 0 then
                        Error (invalidValueError $"Failed to unmap memory: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with ex ->
                    Error (invalidValueError $"Failed to unmap memory: {ex.Message}")
            
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
                with ex ->
                    Error (invalidValueError $"Failed to map file: {ex.Message}")
            
            member this.FlushMappedFile handle address size =
                try
                    let sizeNative = nativeint (int size)
                    // MS_SYNC = 0x0010
                    let result = NativeMethods.msync(address, sizeNative, 0x0010)
                    
                    if result <> 0 then
                        Error (invalidValueError $"Failed to flush mapped file: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with ex ->
                    Error (invalidValueError $"Failed to flush mapped file: {ex.Message}")
            
            member this.LockMemory address size =
                try
                    let sizeNative = nativeint (int size)
                    let result = NativeMethods.mlock(address, sizeNative)
                    
                    if result <> 0 then
                        Error (invalidValueError $"Failed to lock memory: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with ex ->
                    Error (invalidValueError $"Failed to lock memory: {ex.Message}")
            
            member this.UnlockMemory address size =
                try
                    let sizeNative = nativeint (int size)
                    let result = NativeMethods.munlock(address, sizeNative)
                    
                    if result <> 0 then
                        Error (invalidValueError $"Failed to unlock memory: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with ex ->
                    Error (invalidValueError $"Failed to unlock memory: {ex.Message}")
    
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
                            | NamedPipe.PipeDirection.Out -> NativeMethods.O_RDWR ||| NativeMethods.O_NONBLOCK
                            | NamedPipe.PipeDirection.InOut -> NativeMethods.O_RDWR ||| NativeMethods.O_NONBLOCK
                        
                        let fd = NativeMethods.open_(pipePath, openFlags, 0)
                        
                        if fd = -1 then
                            Error (invalidValueError $"Failed to open named pipe: {Helpers.getErrorMessage()}")
                        else
                            Ok (nativeint fd)
                with ex ->
                    Error (invalidValueError $"Failed to create named pipe: {ex.Message}")
            
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
                with ex ->
                    Error (invalidValueError $"Failed to connect to named pipe: {ex.Message}")
            
            member this.WaitForNamedPipeConnection handle timeout =
                // For FIFO pipes, there's no explicit connection handling like in Windows
                // Pipes are considered connected when both read and write ends are open
                Ok ()
            
            member this.WriteNamedPipe handle data offset count =
                try
                    let fd = handle.ToInt32()
                    
                    // Pin the array to get a fixed pointer and ensure GC doesn't move it
                    fixed data.[offset..(offset + count - 1)] (fun ptr ->
                        let bytesWritten = NativeMethods.write(fd, NativePtr.toNativeInt ptr, nativeint count)
                        
                        if bytesWritten = -1 then
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
                        let bytesRead = NativeMethods.read(fd, NativePtr.toNativeInt ptr, nativeint count)
                        
                        if bytesRead = -1 then
                            // Check for EAGAIN (no data available, would block)
                            let errnum = NativeMethods.errno()
                            if errnum = NativeMethods.EAGAIN then
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
                with ex ->
                    Error (invalidValueError $"Failed to create shared memory: {ex.Message}")
            
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
                with ex ->
                    Error (invalidValueError $"Failed to open shared memory: {ex.Message}")
            
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
                with ex ->
                    Error (invalidValueError $"Failed to close shared memory: {ex.Message}")
            
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
                with ex ->
                    Error (invalidValueError $"Failed to create socket: {ex.Message}")
            
            member this.BindSocket handle address port =
                try
                    let sockfd = handle.ToInt32()
                    
                    // Create and initialize sockaddr_in structure
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
                                sockfd,
                                NativePtr.toNativeInt ptr,
                                nativeint sizeof<NativeMethods.SockAddrIn>
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
                    let sockfd = handle.ToInt32()
                    let result = NativeMethods.listen(sockfd, backlog)
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to listen on socket: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with ex ->
                    Error (invalidValueError $"Failed to listen on socket: {ex.Message}")
            
            member this.AcceptSocket handle =
                try
                    let sockfd = handle.ToInt32()
                    
                    // Create sockaddr_in structure for the client address
                    let mutable clientAddr = Helpers.createSockAddrIn(0u, 0us)
                    let mutable addrLen = sizeof<NativeMethods.SockAddrIn>
                    
                    // Accept the connection
                    let clientFd = 
                        fixed clientAddr (fun sockAddrPtr ->
                            fixed &addrLen (fun addrLenPtr ->
                                NativeMethods.accept(
                                    sockfd,
                                    NativePtr.toNativeInt sockAddrPtr,
                                    NativePtr.toNativeInt addrLenPtr
                                )
                            )
                        )
                    
                    if clientFd = -1 then
                        Error (invalidValueError $"Failed to accept connection: {Helpers.getErrorMessage()}")
                    else
                        // Extract address and port from sockaddr_in
                        let addr = NativeMethods.inet_ntoa(clientAddr.sin_addr)
                        let port = int (NativeMethods.ntohs(clientAddr.sin_port))
                        
                        Ok (nativeint clientFd, addr, port)
                with ex ->
                    Error (invalidValueError $"Failed to accept connection: {ex.Message}")
            
            member this.ConnectSocket handle address port =
                try
                    let sockfd = handle.ToInt32()
                    
                    // Create and initialize sockaddr_in structure
                    let mutable sockAddr = 
                        // Convert the address to network byte order
                        let addr = NativeMethods.inet_addr(address)
                        Helpers.createSockAddrIn(addr, uint16 port)
                    
                    // Connect the socket
                    let result = 
                        fixed sockAddr (fun ptr ->
                            NativeMethods.connect(
                                sockfd,
                                NativePtr.toNativeInt ptr,
                                nativeint sizeof<NativeMethods.SockAddrIn>
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
                    let sockfd = handle.ToInt32()
                    
                    // Pin the array to get a fixed pointer and ensure GC doesn't move it
                    fixed data.[offset..(offset + count - 1)] (fun ptr ->
                        // Send the data
                        let result = NativeMethods.send(sockfd, NativePtr.toNativeInt ptr, nativeint count, flags)
                        
                        if result = -1 then
                            Error (invalidValueError $"Failed to send data: {Helpers.getErrorMessage()}")
                        else
                            Ok result
                    )
                with ex ->
                    Error (invalidValueError $"Failed to send data: {ex.Message}")
            
            member this.ReceiveSocket handle buffer offset count flags =
                try
                    let sockfd = handle.ToInt32()
                    
                    // Pin the array to get a fixed pointer and ensure GC doesn't move it
                    fixed buffer.[offset..(offset + count - 1)] (fun ptr ->
                        // Receive data
                        let result = NativeMethods.recv(sockfd, NativePtr.toNativeInt ptr, nativeint count, flags)
                        
                        if result = -1 then
                            let errnum = NativeMethods.errno()
                            
                            // EAGAIN means no data available in non-blocking mode
                            if errnum = NativeMethods.EAGAIN then
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
                    let sockfd = handle.ToInt32()
                    let result = NativeMethods.close(sockfd)
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to close socket: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with ex ->
                    Error (invalidValueError $"Failed to close socket: {ex.Message}")
            
            member this.ShutdownSocket handle how =
                try
                    let sockfd = handle.ToInt32()
                    let result = NativeMethods.shutdown(sockfd, how)
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to shut down socket: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with ex ->
                    Error (invalidValueError $"Failed to shut down socket: {ex.Message}")
            
            member this.SetSocketOption handle level optionName optionValue =
                try
                    let sockfd = handle.ToInt32()
                    
                    // Pin the array to get a fixed pointer and ensure GC doesn't move it
                    fixed optionValue (fun ptr ->
                        // Set the socket option
                        let result = 
                            NativeMethods.setsockopt(
                                sockfd,
                                level,
                                optionName,
                                NativePtr.toNativeInt ptr,
                                nativeint optionValue.Length
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
                    let sockfd = handle.ToInt32()
                    
                    // Pin the array to get a fixed pointer and ensure GC doesn't move it
                    fixed optionValue (fun ptr ->
                        // Variable to receive the option length
                        let mutable optionLen = optionValue.Length
                        
                        // Get the socket option
                        let result = 
                            fixed &optionLen (fun optionLenPtr ->
                                NativeMethods.getsockopt(
                                    sockfd,
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
                // Would be implemented with getsockname
                // For brevity, returning a placeholder
                Ok ("127.0.0.1", 8080)
            
            member this.GetRemoteEndPoint handle =
                // Would be implemented with getpeername
                // For brevity, returning a placeholder
                Ok ("192.168.1.1", 80)
            
            member this.Poll handle timeout =
                // Would be implemented with select or poll
                // For brevity, returning a default value
                Ok false
            
            member this.ResolveHostName hostName =
                // Would use getaddrinfo on macOS
                // For brevity, returning a placeholder
                Ok [| "127.0.0.1" |]
    
    /// <summary>
    /// macOS implementation of synchronization provider
    /// </summary>
    type MacOSSyncProvider() =
        interface IPlatformSync with
            // Mutex operations via semaphores (macOS doesn't have named mutexes)
            member this.CreateMutex name initialOwner =
                try
                    if String.IsNullOrEmpty(name) then
                        Error (invalidValueError "Unnamed mutexes not supported")
                    else
                        // Create a named semaphore with initial value 1 for mutex semantics
                        let openFlags = NativeMethods.O_CREAT ||| NativeMethods.O_EXCL
                        let mode = NativeMethods.S_IRUSR ||| NativeMethods.S_IWUSR
                        let initialValue = if initialOwner then 0u else 1u
                        
                        let semaphore = NativeMethods.sem_open(name, openFlags, mode, initialValue)
                        
                        if semaphore = 0n then
                            Error (invalidValueError $"Failed to create mutex: {Helpers.getErrorMessage()}")
                        else
                            Ok semaphore
                with ex ->
                    Error (invalidValueError $"Failed to create mutex: {ex.Message}")
            
            member this.OpenMutex name =
                try
                    let semaphore = NativeMethods.sem_open(name, 0, 0, 0u)
                    
                    if semaphore = 0n then
                        Error (invalidValueError $"Failed to open mutex: {Helpers.getErrorMessage()}")
                    else
                        Ok semaphore
                with ex ->
                    Error (invalidValueError $"Failed to open mutex: {ex.Message}")
            
            member this.AcquireMutex handle timeout =
                try
                    // For non-blocking attempt
                    if timeout = 0 then
                        let result = NativeMethods.sem_trywait(handle)
                        if result = 0 then
                            Ok true
                        else
                            Ok false // Would block, which is expected
                    else
                        // For blocking attempt
                        let result = NativeMethods.sem_wait(handle)
                        if result = 0 then
                            Ok true
                        else
                            Error (invalidValueError $"Failed to acquire mutex: {Helpers.getErrorMessage()}")
                with ex ->
                    Error (invalidValueError $"Failed to acquire mutex: {ex.Message}")
            
            member this.ReleaseMutex handle =
                try
                    let result = NativeMethods.sem_post(handle)
                    
                    if result = 0 then
                        Ok ()
                    else
                        Error (invalidValueError $"Failed to release mutex: {Helpers.getErrorMessage()}")
                with ex ->
                    Error (invalidValueError $"Failed to release mutex: {ex.Message}")
            
            member this.CloseMutex handle =
                try
                    let result = NativeMethods.sem_close(handle)
                    
                    if result = 0 then
                        Ok ()
                    else
                        Error (invalidValueError $"Failed to close mutex: {Helpers.getErrorMessage()}")
                with ex ->
                    Error (invalidValueError $"Failed to close mutex: {ex.Message}")
            
            // Semaphore operations
            member this.CreateSemaphore name initialCount maximumCount =
                try
                    // Create a named semaphore
                    let openFlags = NativeMethods.O_CREAT ||| NativeMethods.O_EXCL
                    let mode = NativeMethods.S_IRUSR ||| NativeMethods.S_IWUSR
                    
                    let semaphore = NativeMethods.sem_open(name, openFlags, mode, uint initialCount)
                    
                    if semaphore = 0n then
                        Error (invalidValueError $"Failed to create semaphore: {Helpers.getErrorMessage()}")
                    else
                        Ok semaphore
                with ex ->
                    Error (invalidValueError $"Failed to create semaphore: {ex.Message}")
            
            member this.OpenSemaphore name =
                try
                    let semaphore = NativeMethods.sem_open(name, 0, 0, 0u)
                    
                    if semaphore = 0n then
                        Error (invalidValueError $"Failed to open semaphore: {Helpers.getErrorMessage()}")
                    else
                        Ok semaphore
                with ex ->
                    Error (invalidValueError $"Failed to open semaphore: {ex.Message}")
            
            member this.AcquireSemaphore handle timeout =
                try
                    // For non-blocking attempt
                    if timeout = 0 then
                        let result = NativeMethods.sem_trywait(handle)
                        if result = 0 then
                            Ok true
                        else
                            Ok false // Would block, which is expected
                    else
                        // For blocking attempt
                        let result = NativeMethods.sem_wait(handle)
                        if result = 0 then
                            Ok true
                        else
                            Error (invalidValueError $"Failed to acquire semaphore: {Helpers.getErrorMessage()}")
                with ex ->
                    Error (invalidValueError $"Failed to acquire semaphore: {ex.Message}")
            
            member this.ReleaseSemaphore handle releaseCount =
                try
                    // Get current value first
                    let mutable value = 0
                    let valuePtr = &&value |> NativePtr.toNativeInt
                    let getResult = NativeMethods.sem_getvalue(handle, valuePtr)
                    
                    if getResult <> 0 then
                        Error (invalidValueError $"Failed to get semaphore value: {Helpers.getErrorMessage()}")
                    else
                        let previousCount = value
                        
                        // Post to the semaphore releaseCount times
                        let mutable success = true
                        let mutable errorMsg = ""
                        
                        for i = 1 to releaseCount do
                            let result = NativeMethods.sem_post(handle)
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
                    let result = NativeMethods.sem_close(handle)
                    
                    if result = 0 then
                        Ok ()
                    else
                        Error (invalidValueError $"Failed to close semaphore: {Helpers.getErrorMessage()}")
                with ex ->
                    Error (invalidValueError $"Failed to close semaphore: {ex.Message}")