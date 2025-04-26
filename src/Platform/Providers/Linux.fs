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
/// Linux platform implementation
/// </summary>
module Linux =
    /// <summary>
    /// P/Invoke declarations for Linux API
    /// </summary>
    module private NativeMethods =
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
        [<Struct; StructLayout(LayoutKind.Sequential)>]
        type SockAddrIn =
            val mutable sin_family: int16
            val mutable sin_port: uint16
            val mutable sin_addr: uint32
            [<MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)>]
            val mutable sin_zero: byte[]
        
        // Host entry structure
        [<Struct; StructLayout(LayoutKind.Sequential)>]
        type HostEnt =
            val mutable h_name: nativeint          // Official name
            val mutable h_aliases: nativeint       // Alias list
            val mutable h_addrtype: int16          // Address type
            val mutable h_length: int16            // Address length
            val mutable h_addr_list: nativeint     // Address list
        
        // Timeval structure for socket operations
        [<Struct; StructLayout(LayoutKind.Sequential)>]
        type Timeval =
            val mutable tv_sec: int32       // Seconds
            val mutable tv_usec: int32      // Microseconds
        
        // FD_SET structure for socket operations
        [<Struct; StructLayout(LayoutKind.Sequential)>]
        type FdSet =
            [<MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)>]
            val mutable fds_bits: int32[]
        
        // Memory management functions
        [<DllImport(libc, SetLastError = true)>]
        extern nativeint mmap(nativeint addr, unativeint length, int prot, int flags, int fd, int64 offset)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int munmap(nativeint addr, unativeint length)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int mlock(nativeint addr, unativeint length)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int munlock(nativeint addr, unativeint length)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int msync(nativeint addr, unativeint length, int flags)
        
        // File and shared memory functions
        [<DllImport(libc, SetLastError = true)>]
        extern int open([<MarshalAs(UnmanagedType.LPStr)>] string pathname, int flags, int mode)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int close(int fd)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int ftruncate(int fd, int64 length)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int shm_open([<MarshalAs(UnmanagedType.LPStr)>] string name, int oflag, int mode)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int shm_unlink([<MarshalAs(UnmanagedType.LPStr)>] string name)
        
        // Pipe functions
        [<DllImport(libc, SetLastError = true)>]
        extern int pipe(nativeint pipefd)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int mkfifo([<MarshalAs(UnmanagedType.LPStr)>] string pathname, int mode)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int fcntl(int fd, int cmd, int arg)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int read(int fd, nativeint buf, unativeint count)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int write(int fd, nativeint buf, unativeint count)
        
        // Socket functions
        [<DllImport(libc, SetLastError = true)>]
        extern int socket(int domain, int type, int protocol)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int bind(int sockfd, nativeint addr, uint32 addrlen)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int listen(int sockfd, int backlog)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int accept(int sockfd, nativeint addr, nativeint addrlen)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int connect(int sockfd, nativeint addr, uint32 addrlen)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int send(int sockfd, nativeint buf, unativeint len, int flags)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int recv(int sockfd, nativeint buf, unativeint len, int flags)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int shutdown(int sockfd, int how)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int setsockopt(int sockfd, int level, int optname, nativeint optval, uint32 optlen)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int getsockopt(int sockfd, int level, int optname, nativeint optval, nativeint optlen)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int getsockname(int sockfd, nativeint addr, nativeint addrlen)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int getpeername(int sockfd, nativeint addr, nativeint addrlen)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int select(int nfds, nativeint readfds, nativeint writefds, nativeint exceptfds, nativeint timeout)
        
        [<DllImport(libc, SetLastError = false)>]
        extern uint32 inet_addr([<MarshalAs(UnmanagedType.LPStr)>] string cp)
        
        [<DllImport(libc, SetLastError = false)>]
        extern nativeint inet_ntoa(uint32 in_addr)
        
        [<DllImport(libc, SetLastError = false)>]
        extern uint16 htons(uint16 hostshort)
        
        [<DllImport(libc, SetLastError = false)>]
        extern uint16 ntohs(uint16 netshort)
        
        [<DllImport(libc, SetLastError = true)>]
        extern nativeint gethostbyname([<MarshalAs(UnmanagedType.LPStr)>] string name)
        
        [<DllImport(libc, SetLastError = true)>]
        extern void FD_ZERO(nativeint set)
        
        [<DllImport(libc, SetLastError = true)>]
        extern void FD_SET(int fd, nativeint set)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int FD_ISSET(int fd, nativeint set)
        
        // Error functions
        [<DllImport(libc, SetLastError = true)>]
        extern int errno
        
        [<DllImport(libc, SetLastError = false)>]
        extern nativeint strerror(int errnum)
        
        // Mutex and semaphore functions (using POSIX)
        [<DllImport(libc, SetLastError = true)>]
        extern int sem_init(nativeint sem, int pshared, uint32 value)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int sem_destroy(nativeint sem)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int sem_open([<MarshalAs(UnmanagedType.LPStr)>] string name, int oflag, int mode, uint32 value)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int sem_close(int sem)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int sem_unlink([<MarshalAs(UnmanagedType.LPStr)>] string name)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int sem_wait(int sem)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int sem_trywait(int sem)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int sem_timedwait(int sem, nativeint abs_timeout)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int sem_post(int sem)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int sem_getvalue(int sem, nativeint sval)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int pthread_mutex_init(nativeint mutex, nativeint attr)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int pthread_mutex_destroy(nativeint mutex)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int pthread_mutex_lock(nativeint mutex)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int pthread_mutex_trylock(nativeint mutex)
        
        [<DllImport(libc, SetLastError = true)>]
        extern int pthread_mutex_unlock(nativeint mutex)
    
    /// <summary>
    /// Helper functions for working with Linux API
    /// </summary>
    module private Helpers =
        /// <summary>
        /// Gets the last error message
        /// </summary>
        let getErrorMessage (): string =
            let errNum = Marshal.GetLastWin32Error()
            let errPtr = NativeMethods.strerror(errNum)
            let errMsg = Marshal.PtrToStringAnsi(errPtr)
            $"Error {errNum}: {errMsg}"
        
        /// <summary>
        /// Creates a sockaddr_in structure
        /// </summary>
        let createSockAddrIn (addr: uint32) (port: uint16): NativeMethods.SockAddrIn =
            let mutable result = NativeMethods.SockAddrIn()
            result.sin_family <- int16 NativeMethods.AF_INET
            result.sin_port <- NativeMethods.htons(port)
            result.sin_addr <- addr
            result.sin_zero <- Array.zeroCreate 8
            result
        
        /// <summary>
        /// Creates a timeval structure
        /// </summary>
        let createTimeval (milliseconds: int): NativeMethods.Timeval =
            let mutable result = NativeMethods.Timeval()
            result.tv_sec <- milliseconds / 1000
            result.tv_usec <- (milliseconds % 1000) * 1000
            result
        
        /// <summary>
        /// Creates an empty fd_set structure
        /// </summary>
        let createFdSet (): NativeMethods.FdSet =
            let mutable result = NativeMethods.FdSet()
            result.fds_bits <- Array.zeroCreate 16
            result
        
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
                            sizeUInt,         // The size of the region
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
                    
                    let fd = NativeMethods.open(filePath, openFlags, 0o644)
                    
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
                        
                        let fd = NativeMethods.open(pipePath, openFlags, 0)
                        
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
                    
                    let fd = NativeMethods.open(pipePath, openFlags, 0)
                    
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
                    let dataPtr = &&data.[offset] |> NativePtr.toNativeInt
                    
                    let bytesWritten = NativeMethods.write(fd, dataPtr, unativeint count)
                    
                    if bytesWritten = -1 then
                        let errorCode = Marshal.GetLastWin32Error()
                        
                        // EAGAIN or EWOULDBLOCK means the pipe would block (no readers available)
                        if errorCode = NativeMethods.EAGAIN || errorCode = NativeMethods.EWOULDBLOCK then
                            Ok 0
                        else
                            Error (invalidValueError $"Failed to write to pipe: {Helpers.getErrorMessage()}")
                    else
                        Ok bytesWritten
                with ex ->
                    Error (invalidValueError $"Failed to write to pipe: {ex.Message}")
            
            member this.ReadNamedPipe handle buffer offset count =
                try
                    let fd = handle.ToInt32()
                    
                    // Pin the array to get a fixed pointer and ensure GC doesn't move it
                    let bufferPtr = &&buffer.[offset] |> NativePtr.toNativeInt
                    
                    let bytesRead = NativeMethods.read(fd, bufferPtr, unativeint count)
                    
                    if bytesRead = -1 then
                        let errorCode = Marshal.GetLastWin32Error()
                        
                        // EAGAIN or EWOULDBLOCK means the pipe would block (no data available)
                        if errorCode = NativeMethods.EAGAIN || errorCode = NativeMethods.EWOULDBLOCK then
                            Ok 0
                        else
                            Error (invalidValueError $"Failed to read from pipe: {Helpers.getErrorMessage()}")
                    else
                        Ok bytesRead
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
                        let fd = NativeMethods.open(pipePath, NativeMethods.O_RDONLY ||| NativeMethods.O_NONBLOCK, 0)
                        
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
                        NativeMethods.bind(
                            fd,
                            &&sockAddr |> NativePtr.toNativeInt,
                            uint32 (sizeof<NativeMethods.SockAddrIn>)
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
                    let mutable addrLen = uint32 (sizeof<NativeMethods.SockAddrIn>)
                    
                    // Accept the connection
                    let clientFd = 
                        NativeMethods.accept(
                            fd,
                            &&clientAddr |> NativePtr.toNativeInt,
                            &&addrLen |> NativePtr.toNativeInt
                        )
                    
                    if clientFd = -1 then
                        Error (invalidValueError $"Failed to accept connection: {Helpers.getErrorMessage()}")
                    else
                        // Extract the client address and port
                        let addrPtr = NativeMethods.inet_ntoa(clientAddr.sin_addr)
                        let addr = Marshal.PtrToStringAnsi(addrPtr)
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
                        NativeMethods.connect(
                            fd,
                            &&sockAddr |> NativePtr.toNativeInt,
                            uint32 (sizeof<NativeMethods.SockAddrIn>)
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
                    let dataPtr = &&data.[offset] |> NativePtr.toNativeInt
                    
                    // Send the data
                    let result = 
                        NativeMethods.send(
                            fd,
                            dataPtr,
                            unativeint count,
                            flags
                        )
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to send data: {Helpers.getErrorMessage()}")
                    else
                        Ok result
                with ex ->
                    Error (invalidValueError $"Failed to send data: {ex.Message}")
            
            member this.ReceiveSocket handle buffer offset count flags =
                try
                    let fd = handle.ToInt32()
                    
                    // Pin the array to get a fixed pointer and ensure GC doesn't move it
                    let bufferPtr = &&buffer.[offset] |> NativePtr.toNativeInt
                    
                    // Receive data
                    let result = 
                        NativeMethods.recv(
                            fd,
                            bufferPtr,
                            unativeint count,
                            flags
                        )
                    
                    if result = -1 then
                        let errorCode = Marshal.GetLastWin32Error()
                        
                        // EAGAIN or EWOULDBLOCK means no data available in non-blocking mode
                        if errorCode = NativeMethods.EAGAIN || errorCode = NativeMethods.EWOULDBLOCK then
                            Ok 0
                        else
                            Error (invalidValueError $"Failed to receive data: {Helpers.getErrorMessage()}")
                    else
                        Ok result
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
                    let optionValuePtr = &&optionValue.[0] |> NativePtr.toNativeInt
                    
                    // Set the socket option
                    let result = 
                        NativeMethods.setsockopt(
                            fd,
                            level,
                            optionName,
                            optionValuePtr,
                            uint32 optionValue.Length
                        )
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to set socket option: {Helpers.getErrorMessage()}")
                    else
                        Ok ()
                with ex ->
                    Error (invalidValueError $"Failed to set socket option: {ex.Message}")
            
            member this.GetSocketOption handle level optionName optionValue =
                try
                    let fd = handle.ToInt32()
                    
                    // Pin the array to get a fixed pointer and ensure GC doesn't move it
                    let optionValuePtr = &&optionValue.[0] |> NativePtr.toNativeInt
                    
                    // Variable to receive the option length
                    let mutable optionLen = uint32 optionValue.Length
                    let optionLenPtr = &&optionLen |> NativePtr.toNativeInt
                    
                    // Get the socket option
                    let result = 
                        NativeMethods.getsockopt(
                            fd,
                            level,
                            optionName,
                            optionValuePtr,
                            optionLenPtr
                        )
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to get socket option: {Helpers.getErrorMessage()}")
                    else
                        Ok (int optionLen)
                with ex ->
                    Error (invalidValueError $"Failed to get socket option: {ex.Message}")
            
            member this.GetLocalEndPoint handle =
                try
                    let fd = handle.ToInt32()
                    
                    // Create a sockaddr structure to receive the local address
                    let mutable localAddr = Helpers.createSockAddrIn(0u, 0us)
                    let mutable addrLen = uint32 (sizeof<NativeMethods.SockAddrIn>)
                    
                    // Get the local address
                    let result = 
                        NativeMethods.getsockname(
                            fd,
                            &&localAddr |> NativePtr.toNativeInt,
                            &&addrLen |> NativePtr.toNativeInt
                        )
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to get local endpoint: {Helpers.getErrorMessage()}")
                    else
                        // Extract the local address and port
                        let addrPtr = NativeMethods.inet_ntoa(localAddr.sin_addr)
                        let addr = Marshal.PtrToStringAnsi(addrPtr)
                        let port = int (NativeMethods.ntohs(localAddr.sin_port))
                        
                        Ok (addr, port)
                with ex ->
                    Error (invalidValueError $"Failed to get local endpoint: {ex.Message}")
            
            member this.GetRemoteEndPoint handle =
                try
                    let fd = handle.ToInt32()
                    
                    // Create a sockaddr structure to receive the remote address
                    let mutable remoteAddr = Helpers.createSockAddrIn(0u, 0us)
                    let mutable addrLen = uint32 (sizeof<NativeMethods.SockAddrIn>)
                    
                    // Get the remote address
                    let result = 
                        NativeMethods.getpeername(
                            fd,
                            &&remoteAddr |> NativePtr.toNativeInt,
                            &&addrLen |> NativePtr.toNativeInt
                        )
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to get remote endpoint: {Helpers.getErrorMessage()}")
                    else
                        // Extract the remote address and port
                        let addrPtr = NativeMethods.inet_ntoa(remoteAddr.sin_addr)
                        let addr = Marshal.PtrToStringAnsi(addrPtr)
                        let port = int (NativeMethods.ntohs(remoteAddr.sin_port))
                        
                        Ok (addr, port)
                with ex ->
                    Error (invalidValueError $"Failed to get remote endpoint: {ex.Message}")
            
            member this.Poll handle timeout =
                try
                    let fd = handle.ToInt32()
                    
                    // Create a fd_set structure
                    let mutable readFds = Helpers.createFdSet()
                    let readFdsPtr = &&readFds |> NativePtr.toNativeInt
                    
                    // Zero the fd_set
                    NativeMethods.FD_ZERO(readFdsPtr)
                    
                    // Add the socket to the set
                    NativeMethods.FD_SET(fd, readFdsPtr)
                    
                    // Create a timeout structure
                    let mutable tv = Helpers.createTimeval(timeout)
                    let tvPtr = &&tv |> NativePtr.toNativeInt
                    
                    // Select on the socket
                    let result = 
                        NativeMethods.select(
                            fd + 1,           // nfds should be the highest fd + 1
                            readFdsPtr,       // readfds
                            0n,               // writefds
                            0n,               // exceptfds
                            tvPtr             // timeout
                        )
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to poll socket: {Helpers.getErrorMessage()}")
                    else
                        // Check if the socket is in the set
                        let isSet = NativeMethods.FD_ISSET(fd, readFdsPtr)
                        
                        Ok (isSet <> 0)
                with ex ->
                    Error (invalidValueError $"Failed to poll socket: {ex.Message}")
            
            member this.ResolveHostName hostName =
                try
                    // Get the host by name
                    let hostEntPtr = NativeMethods.gethostbyname(hostName)
                    
                    if hostEntPtr = 0n then
                        Error (invalidValueError $"Failed to resolve host name: {Helpers.getErrorMessage()}")
                    else
                        // Access the host entry structure
                        let hostEnt = NativePtr.ofNativeInt hostEntPtr |> NativePtr.read
                        
                        // Get the address list
                        let addrListPtr = hostEnt.h_addr_list
                        
                        // Collect all addresses
                        let mutable addresses = []
                        let mutable i = 0
                        let addrList = NativePtr.ofNativeInt addrListPtr
                        
                        while (NativePtr.get addrList i) <> 0n do
                            let inAddrPtr = NativePtr.get addrList i
                            let inAddr = NativePtr.ofNativeInt inAddrPtr |> NativePtr.read<uint32>
                            let addrStrPtr = NativeMethods.inet_ntoa(inAddr)
                            let addrStr = Marshal.PtrToStringAnsi(addrStrPtr)
                            addresses <- addrStr :: addresses
                            i <- i + 1
                        
                        Ok (List.rev addresses |> Array.ofList)
                with ex ->
                    Error (invalidValueError $"Failed to resolve host name: {ex.Message}")
    
    /// <summary>
    /// Linux implementation of the synchronization provider
    /// </summary>
    type LinuxSyncProvider() =
        interface IPlatformSync with
            member this.CreateMutex name initialOwner =
                // For Linux, we use POSIX semaphores for both mutexes and semaphores
                // A mutex is just a semaphore with an initial count of 1
                try
                    if String.IsNullOrEmpty(name) then
                        // For unnamed mutexes, we allocate memory and initialize a pthread_mutex_t
                        let mutexSize = 40 // Size of pthread_mutex_t (platform dependent)
                        let handle = Marshal.AllocHGlobal(mutexSize)
                        
                        let result = NativeMethods.pthread_mutex_init(handle, 0n)
                        
                        if result <> 0 then
                            Marshal.FreeHGlobal(handle)
                            Error (invalidValueError $"Failed to create mutex: {Helpers.getErrorMessage()}")