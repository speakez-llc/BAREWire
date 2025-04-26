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
/// Windows platform implementation
/// </summary>
module Windows =
    /// <summary>
    /// P/Invoke declarations for Windows API
    /// </summary>
    module private NativeMethods =
        [<Literal>]
        let kernel32 = "kernel32.dll"
        [<Literal>]
        let ws2_32 = "ws2_32.dll"
        
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
            val mutable fd_count: uint32
            [<MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)>]
            val mutable fd_array: nativeint[]
        
        // Memory management constants
        [<Literal>]
        let PAGE_READWRITE = 0x04u
        
        [<Literal>]
        let PAGE_READONLY = 0x02u
        
        [<Literal>]
        let MEM_COMMIT = 0x1000u
        
        [<Literal>]
        let MEM_RESERVE = 0x2000u
        
        [<Literal>]
        let MEM_RELEASE = 0x8000u
        
        [<Literal>]
        let FILE_MAP_ALL_ACCESS = 0xF001Fu
        
        [<Literal>]
        let FILE_MAP_READ = 0x0004u
        
        [<Literal>]
        let FILE_MAP_WRITE = 0x0002u
        
        // Pipe constants
        [<Literal>]
        let PIPE_ACCESS_DUPLEX = 0x00000003u
        
        [<Literal>]
        let PIPE_ACCESS_INBOUND = 0x00000001u
        
        [<Literal>]
        let PIPE_ACCESS_OUTBOUND = 0x00000002u
        
        [<Literal>]
        let PIPE_TYPE_MESSAGE = 0x00000004u
        
        [<Literal>]
        let PIPE_READMODE_MESSAGE = 0x00000002u
        
        [<Literal>]
        let PIPE_WAIT = 0x00000000u
        
        [<Literal>]
        let PIPE_UNLIMITED_INSTANCES = 255u
        
        [<Literal>]
        let ERROR_PIPE_CONNECTED = 535u
        
        [<Literal>]
        let INVALID_HANDLE_VALUE = -1n
        
        // Memory management functions
        [<DllImport(kernel32, SetLastError = true)>]
        extern nativeint VirtualAlloc(nativeint lpAddress, unativeint dwSize, uint32 flAllocationType, uint32 flProtect)
        
        [<DllImport(kernel32, SetLastError = true)>]
        extern bool VirtualFree(nativeint lpAddress, unativeint dwSize, uint32 dwFreeType)
        
        [<DllImport(kernel32, SetLastError = true)>]
        extern bool VirtualLock(nativeint lpAddress, unativeint dwSize)
        
        [<DllImport(kernel32, SetLastError = true)>]
        extern bool VirtualUnlock(nativeint lpAddress, unativeint dwSize)
        
        [<DllImport(kernel32, SetLastError = true)>]
        extern nativeint CreateFileMappingW(nativeint hFile, nativeint lpFileMappingAttributes, uint32 flProtect, uint32 dwMaximumSizeHigh, uint32 dwMaximumSizeLow, string lpName)
        
        [<DllImport(kernel32, SetLastError = true)>]
        extern nativeint MapViewOfFile(nativeint hFileMappingObject, uint32 dwDesiredAccess, uint32 dwFileOffsetHigh, uint32 dwFileOffsetLow, unativeint dwNumberOfBytesToMap)
        
        [<DllImport(kernel32, SetLastError = true)>]
        extern bool UnmapViewOfFile(nativeint lpBaseAddress)
        
        [<DllImport(kernel32, SetLastError = true)>]
        extern bool FlushViewOfFile(nativeint lpBaseAddress, unativeint dwNumberOfBytesToFlush)
        
        [<DllImport(kernel32, SetLastError = true)>]
        extern nativeint OpenFileMappingW(uint32 dwDesiredAccess, bool bInheritHandle, string lpName)
        
        // File functions
        [<DllImport(kernel32, SetLastError = true, CharSet = CharSet.Unicode)>]
        extern nativeint CreateFileW(string lpFileName, uint32 dwDesiredAccess, uint32 dwShareMode, nativeint lpSecurityAttributes, uint32 dwCreationDisposition, uint32 dwFlagsAndAttributes, nativeint hTemplateFile)
        
        // Pipe functions
        [<DllImport(kernel32, SetLastError = true, CharSet = CharSet.Unicode)>]
        extern nativeint CreateNamedPipeW(string lpName, uint32 dwOpenMode, uint32 dwPipeMode, uint32 nMaxInstances, uint32 nOutBufferSize, uint32 nInBufferSize, uint32 nDefaultTimeOut, nativeint lpSecurityAttributes)
        
        [<DllImport(kernel32, SetLastError = true, CharSet = CharSet.Unicode)>]
        extern nativeint CreateFileW(string lpFileName, uint32 dwDesiredAccess, uint32 dwShareMode, nativeint lpSecurityAttributes, uint32 dwCreationDisposition, uint32 dwFlagsAndAttributes, nativeint hTemplateFile)
        
        [<DllImport(kernel32, SetLastError = true)>]
        extern bool ConnectNamedPipe(nativeint hNamedPipe, nativeint lpOverlapped)
        
        [<DllImport(kernel32, SetLastError = true)>]
        extern bool DisconnectNamedPipe(nativeint hNamedPipe)
        
        [<DllImport(kernel32, SetLastError = true)>]
        extern bool ReadFile(nativeint hFile, nativeint lpBuffer, uint32 nNumberOfBytesToRead, nativeint lpNumberOfBytesRead, nativeint lpOverlapped)
        
        [<DllImport(kernel32, SetLastError = true)>]
        extern bool WriteFile(nativeint hFile, nativeint lpBuffer, uint32 nNumberOfBytesToWrite, nativeint lpNumberOfBytesWritten, nativeint lpOverlapped)
        
        [<DllImport(kernel32, SetLastError = true)>]
        extern bool PeekNamedPipe(nativeint hNamedPipe, nativeint lpBuffer, uint32 nBufferSize, nativeint lpBytesRead, nativeint lpTotalBytesAvail, nativeint lpBytesLeftThisMessage)
        
        [<DllImport(kernel32, SetLastError = true)>]
        extern bool WaitNamedPipeW(string lpNamedPipeName, uint32 nTimeOut)
        
        // Common functions
        [<DllImport(kernel32, SetLastError = true)>]
        extern bool CloseHandle(nativeint hObject)
        
        [<DllImport(kernel32, SetLastError = true)>]
        extern uint32 GetLastError()
        
        // Winsock functions
        [<DllImport(ws2_32, SetLastError = true)>]
        extern nativeint socket(int af, int type, int protocol)
        
        [<DllImport(ws2_32, SetLastError = true)>]
        extern int bind(nativeint s, nativeint name, int namelen)
        
        [<DllImport(ws2_32, SetLastError = true)>]
        extern int listen(nativeint s, int backlog)
        
        [<DllImport(ws2_32, SetLastError = true)>]
        extern nativeint accept(nativeint s, nativeint addr, nativeint addrlen)
        
        [<DllImport(ws2_32, SetLastError = true)>]
        extern int connect(nativeint s, nativeint name, int namelen)
        
        [<DllImport(ws2_32, SetLastError = true)>]
        extern int send(nativeint s, nativeint buf, int len, int flags)
        
        [<DllImport(ws2_32, SetLastError = true)>]
        extern int recv(nativeint s, nativeint buf, int len, int flags)
        
        [<DllImport(ws2_32, SetLastError = true)>]
        extern int closesocket(nativeint s)
        
        [<DllImport(ws2_32, SetLastError = true)>]
        extern int shutdown(nativeint s, int how)
        
        [<DllImport(ws2_32, SetLastError = true)>]
        extern int setsockopt(nativeint s, int level, int optname, nativeint optval, int optlen)
        
        [<DllImport(ws2_32, SetLastError = true)>]
        extern int getsockopt(nativeint s, int level, int optname, nativeint optval, nativeint optlen)
        
        [<DllImport(ws2_32, SetLastError = true)>]
        extern int getsockname(nativeint s, nativeint name, nativeint namelen)
        
        [<DllImport(ws2_32, SetLastError = true)>]
        extern int getpeername(nativeint s, nativeint name, nativeint namelen)
        
        [<DllImport(ws2_32, SetLastError = true)>]
        extern int select(int nfds, nativeint readfds, nativeint writefds, nativeint exceptfds, nativeint timeout)
        
        [<DllImport(ws2_32, SetLastError = false)>]
        extern uint32 inet_addr(string cp)
        
        [<DllImport(ws2_32, SetLastError = false)>]
        extern string inet_ntoa(uint32 in_addr)
        
        [<DllImport(ws2_32, SetLastError = false)>]
        extern uint16 htons(uint16 hostshort)
        
        [<DllImport(ws2_32, SetLastError = false)>]
        extern uint16 ntohs(uint16 netshort)
        
        [<DllImport(ws2_32, SetLastError = true)>]
        extern nativeint gethostbyname(string name)
        
        [<DllImport(ws2_32, SetLastError = true)>]
        extern uint32 WSAGetLastError()
        
        [<DllImport(ws2_32, SetLastError = true)>]
        extern int FD_SET(nativeint s, nativeint set)
        
        [<DllImport(ws2_32, SetLastError = true)>]
        extern int FD_ISSET(nativeint s, nativeint set)
        
        // Mutex functions
        [<DllImport(kernel32, SetLastError = true, CharSet = CharSet.Unicode)>]
        extern nativeint CreateMutexW(nativeint lpMutexAttributes, bool bInitialOwner, string lpName)
        
        [<DllImport(kernel32, SetLastError = true, CharSet = CharSet.Unicode)>]
        extern nativeint OpenMutexW(uint32 dwDesiredAccess, bool bInheritHandle, string lpName)
        
        [<DllImport(kernel32, SetLastError = true)>]
        extern uint32 WaitForSingleObject(nativeint hHandle, uint32 dwMilliseconds)
        
        [<DllImport(kernel32, SetLastError = true)>]
        extern bool ReleaseMutex(nativeint hMutex)
        
        // Semaphore functions
        [<DllImport(kernel32, SetLastError = true, CharSet = CharSet.Unicode)>]
        extern nativeint CreateSemaphoreW(nativeint lpSemaphoreAttributes, int lInitialCount, int lMaximumCount, string lpName)
        
        [<DllImport(kernel32, SetLastError = true, CharSet = CharSet.Unicode)>]
        extern nativeint OpenSemaphoreW(uint32 dwDesiredAccess, bool bInheritHandle, string lpName)
        
        [<DllImport(kernel32, SetLastError = true)>]
        extern bool ReleaseSemaphore(nativeint hSemaphore, int lReleaseCount, nativeint lpPreviousCount)
    
    /// <summary>
    /// Helper functions for working with Windows API
    /// </summary>
    module private Helpers =
        /// <summary>
        /// Converts a managed string to a null-terminated wide string for P/Invoke
        /// </summary>
        let toWideNullTerminated (s: string): string =
            if String.IsNullOrEmpty(s) then
                null
            else
                s
        
        /// <summary>
        /// Gets the last Win32 error as a string
        /// </summary>
        let getLastErrorString (): string =
            let errorCode = NativeMethods.GetLastError()
            $"Win32 error code: {errorCode}"
        
        /// <summary>
        /// Gets the last Winsock error as a string
        /// </summary>
        let getWinsockErrorString (): string =
            let errorCode = NativeMethods.WSAGetLastError()
            $"Winsock error code: {errorCode}"
        
        /// <summary>
        /// Creates a sockaddr_in structure
        /// </summary>
        let createSockAddrIn (addr: uint32) (port: uint16): NativeMethods.SockAddrIn =
            let mutable result = NativeMethods.SockAddrIn()
            result.sin_family <- 2s // AF_INET
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
            result.fd_count <- 0u
            result.fd_array <- Array.zeroCreate 64
            result
        
        /// <summary>
        /// Converts MappingType to Windows protection flags
        /// </summary>
        let mappingTypeToProtectionFlags (mappingType: MappingType) (accessType: AccessType): uint32 =
            match accessType with
            | AccessType.ReadOnly -> NativeMethods.PAGE_READONLY
            | AccessType.ReadWrite -> NativeMethods.PAGE_READWRITE
        
        /// <summary>
        /// Converts AccessType to Windows file mapping access flags
        /// </summary>
        let accessTypeToFileMappingAccess (accessType: AccessType): uint32 =
            match accessType with
            | AccessType.ReadOnly -> NativeMethods.FILE_MAP_READ
            | AccessType.ReadWrite -> NativeMethods.FILE_MAP_ALL_ACCESS
        
        /// <summary>
        /// Converts PipeDirection to Windows pipe access flags
        /// </summary>
        let pipeDirectionToFlags (direction: NamedPipe.PipeDirection): uint32 =
            match direction with
            | NamedPipe.PipeDirection.In -> NativeMethods.PIPE_ACCESS_INBOUND
            | NamedPipe.PipeDirection.Out -> NativeMethods.PIPE_ACCESS_OUTBOUND
            | NamedPipe.PipeDirection.InOut -> NativeMethods.PIPE_ACCESS_DUPLEX
        
        /// <summary>
        /// Converts PipeMode to Windows pipe mode flags
        /// </summary>
        let pipeModeToFlags (mode: NamedPipe.PipeMode): uint32 =
            match mode with
            | NamedPipe.PipeMode.Byte -> NativeMethods.PIPE_WAIT
            | NamedPipe.PipeMode.Message -> NativeMethods.PIPE_TYPE_MESSAGE ||| NativeMethods.PIPE_READMODE_MESSAGE ||| NativeMethods.PIPE_WAIT
        
        /// <summary>
        /// Formats a named pipe path
        /// </summary>
        let formatPipeName (name: string): string =
            $"\\\\.\\pipe\\{name}"
        
        /// <summary>
        /// Converts a 64-bit size to high and low 32-bit components
        /// </summary>
        let sizeToHighLow (size: int64): uint32 * uint32 =
            let high = uint32 (size >>> 32)
            let low = uint32 size
            (high, low)
    
    /// <summary>
    /// Windows implementation of the memory provider
    /// </summary>
    type WindowsMemoryProvider() =
        interface IPlatformMemory with
            member this.MapMemory size mappingType accessType =
                try
                    let sizeUInt = unativeint (int size)
                    let protectionFlags = Helpers.mappingTypeToProtectionFlags mappingType accessType
                    
                    let address = 
                        NativeMethods.VirtualAlloc(
                            0n,                           // Let the system determine where to allocate the region
                            sizeUInt,                     // The size of the region
                            NativeMethods.MEM_COMMIT ||| NativeMethods.MEM_RESERVE, // The type of memory allocation
                            protectionFlags               // The memory protection for the region of pages
                        )
                    
                    if address = 0n then
                        Error (invalidValueError $"Failed to allocate memory: {Helpers.getLastErrorString()}")
                    else
                        // For Windows, we use the address as both the handle and the address
                        Ok (address, address)
                with ex ->
                    Error (invalidValueError $"Failed to map memory: {ex.Message}")
            
            member this.UnmapMemory handle address size =
                try
                    // In Windows VirtualAlloc implementation, the handle and address are the same
                    if NativeMethods.VirtualFree(address, unativeint 0, NativeMethods.MEM_RELEASE) then
                        Ok ()
                    else
                        Error (invalidValueError $"Failed to free memory: {Helpers.getLastErrorString()}")
                with ex ->
                    Error (invalidValueError $"Failed to unmap memory: {ex.Message}")
            
            member this.MapFile filePath offset size accessType =
                try
                    let fileHandle =
                        NativeMethods.CreateFileW(
                            filePath,
                            match accessType with
                            | AccessType.ReadOnly -> 0x80000000u     // GENERIC_READ
                            | AccessType.ReadWrite -> 0xC0000000u,   // GENERIC_READ | GENERIC_WRITE
                            0x00000003u,                             // FILE_SHARE_READ | FILE_SHARE_WRITE
                            0n,                                      // Default security attributes
                            3u,                                      // OPEN_EXISTING
                            0x80u,                                   // FILE_ATTRIBUTE_NORMAL
                            0n                                       // No template file
                        )
                    
                    if fileHandle = NativeMethods.INVALID_HANDLE_VALUE then
                        Error (invalidValueError $"Failed to open file: {Helpers.getLastErrorString()}")
                    else
                        try
                            let protectionFlags = Helpers.mappingTypeToProtectionFlags MappingType.PrivateMapping accessType
                            
                            let mappingHandle =
                                NativeMethods.CreateFileMappingW(
                                    fileHandle,                     // File handle
                                    0n,                             // Default security attributes
                                    protectionFlags,                // Page protection
                                    0u,                             // Maximum size high dword
                                    0u,                             // Maximum size low dword - 0 means use the file size
                                    null                            // Mapping name - null for no name
                                )
                            
                            if mappingHandle = 0n then
                                NativeMethods.CloseHandle(fileHandle) |> ignore
                                Error (invalidValueError $"Failed to create file mapping: {Helpers.getLastErrorString()}")
                            else
                                try
                                    let (offsetHigh, offsetLow) = Helpers.sizeToHighLow offset
                                    let accessFlags = Helpers.accessTypeToFileMappingAccess accessType
                                    
                                    let baseAddress =
                                        NativeMethods.MapViewOfFile(
                                            mappingHandle,               // File mapping handle
                                            accessFlags,                 // Access mode
                                            offsetHigh,                  // Offset high dword
                                            offsetLow,                   // Offset low dword
                                            unativeint (int size)        // Number of bytes to map
                                        )
                                    
                                    if baseAddress = 0n then
                                        Error (invalidValueError $"Failed to map view of file: {Helpers.getLastErrorString()}")
                                    else
                                        Ok (mappingHandle, baseAddress)
                                with ex ->
                                    NativeMethods.CloseHandle(mappingHandle) |> ignore
                                    NativeMethods.CloseHandle(fileHandle) |> ignore
                                    Error (invalidValueError $"Failed to map view of file: {ex.Message}")
                        with ex ->
                            NativeMethods.CloseHandle(fileHandle) |> ignore
                            Error (invalidValueError $"Failed to create file mapping: {ex.Message}")
                with ex ->
                    Error (invalidValueError $"Failed to map file: {ex.Message}")
            
            member this.FlushMappedFile handle address size =
                try
                    if NativeMethods.FlushViewOfFile(address, unativeint (int size)) then
                        Ok ()
                    else
                        Error (invalidValueError $"Failed to flush mapped file: {Helpers.getLastErrorString()}")
                with ex ->
                    Error (invalidValueError $"Failed to flush mapped file: {ex.Message}")
            
            member this.LockMemory address size =
                try
                    if NativeMethods.VirtualLock(address, unativeint (int size)) then
                        Ok ()
                    else
                        Error (invalidValueError $"Failed to lock memory: {Helpers.getLastErrorString()}")
                with ex ->
                    Error (invalidValueError $"Failed to lock memory: {ex.Message}")
            
            member this.UnlockMemory address size =
                try
                    if NativeMethods.VirtualUnlock(address, unativeint (int size)) then
                        Ok ()
                    else
                        Error (invalidValueError $"Failed to unlock memory: {Helpers.getLastErrorString()}")
                with ex ->
                    Error (invalidValueError $"Failed to unlock memory: {ex.Message}")
    
    /// <summary>
    /// Windows implementation of the IPC provider
    /// </summary>
    type WindowsIpcProvider() =
        interface IPlatformIpc with
            member this.CreateNamedPipe name direction mode bufferSize =
                try
                    let pipeName = Helpers.formatPipeName name
                    let pipeDirection = Helpers.pipeDirectionToFlags direction
                    let pipeMode = Helpers.pipeModeToFlags mode
                    
                    let handle =
                        NativeMethods.CreateNamedPipeW(
                            pipeName,                      // The name of the pipe
                            pipeDirection,                 // The pipe access mode
                            pipeMode,                      // The pipe type, read mode, and blocking mode
                            NativeMethods.PIPE_UNLIMITED_INSTANCES, // Maximum number of instances
                            uint32 (int bufferSize),       // Output buffer size
                            uint32 (int bufferSize),       // Input buffer size
                            0u,                            // Default timeout
                            0n                             // Default security attributes
                        )
                    
                    if handle = NativeMethods.INVALID_HANDLE_VALUE then
                        Error (invalidValueError $"Failed to create named pipe: {Helpers.getLastErrorString()}")
                    else
                        Ok handle
                with ex ->
                    Error (invalidValueError $"Failed to create named pipe: {ex.Message}")
            
            member this.ConnectNamedPipe name direction =
                try
                    let pipeName = Helpers.formatPipeName name
                    
                    // For client connections, we use CreateFile to connect to the pipe
                    let accessMode =
                        match direction with
                        | NamedPipe.PipeDirection.In -> 0x80000000u   // GENERIC_READ
                        | NamedPipe.PipeDirection.Out -> 0x40000000u  // GENERIC_WRITE
                        | NamedPipe.PipeDirection.InOut -> 0xC0000000u // GENERIC_READ | GENERIC_WRITE
                    
                    let handle =
                        NativeMethods.CreateFileW(
                            pipeName,                // The name of the pipe
                            accessMode,              // The desired access
                            0u,                      // No sharing
                            0n,                      // Default security attributes
                            3u,                      // OPEN_EXISTING
                            0x80u,                   // FILE_ATTRIBUTE_NORMAL
                            0n                       // No template file
                        )
                    
                    if handle = NativeMethods.INVALID_HANDLE_VALUE then
                        Error (invalidValueError $"Failed to connect to named pipe: {Helpers.getLastErrorString()}")
                    else
                        Ok handle
                with ex ->
                    Error (invalidValueError $"Failed to connect to named pipe: {ex.Message}")
            
            member this.WaitForNamedPipeConnection handle timeout =
                try
                    if NativeMethods.ConnectNamedPipe(handle, 0n) then
                        Ok ()
                    else
                        let error = NativeMethods.GetLastError()
                        if error = NativeMethods.ERROR_PIPE_CONNECTED then
                            // This is not an error - the client is already connected
                            Ok ()
                        else
                            Error (invalidValueError $"Failed to wait for pipe connection: {Helpers.getLastErrorString()}")
                with ex ->
                    Error (invalidValueError $"Failed to wait for pipe connection: {ex.Message}")
            
            member this.WriteNamedPipe handle data offset count =
                try
                    // Pin the array to get a fixed pointer and ensure GC doesn't move it
                    let bytesWritten = 0u
                    let bytesWrittenPtr = &&bytesWritten |> NativePtr.toNativeInt
                    
                    let dataPtr = &&data.[offset] |> NativePtr.toNativeInt
                    
                    if NativeMethods.WriteFile(handle, dataPtr, uint32 count, bytesWrittenPtr, 0n) then
                        Ok (int bytesWritten)
                    else
                        Error (invalidValueError $"Failed to write to pipe: {Helpers.getLastErrorString()}")
                with ex ->
                    Error (invalidValueError $"Failed to write to pipe: {ex.Message}")
            
            member this.ReadNamedPipe handle buffer offset count =
                try
                    // Pin the array to get a fixed pointer and ensure GC doesn't move it
                    let bytesRead = 0u
                    let bytesReadPtr = &&bytesRead |> NativePtr.toNativeInt
                    
                    let bufferPtr = &&buffer.[offset] |> NativePtr.toNativeInt
                    
                    if NativeMethods.ReadFile(handle, bufferPtr, uint32 count, bytesReadPtr, 0n) then
                        Ok (int bytesRead)
                    else
                        Error (invalidValueError $"Failed to read from pipe: {Helpers.getLastErrorString()}")
                with ex ->
                    Error (invalidValueError $"Failed to read from pipe: {ex.Message}")
            
            member this.CloseNamedPipe handle =
                try
                    if NativeMethods.CloseHandle(handle) then
                        Ok ()
                    else
                        Error (invalidValueError $"Failed to close pipe: {Helpers.getLastErrorString()}")
                with ex ->
                    Error (invalidValueError $"Failed to close pipe: {ex.Message}")
            
            member this.CreateSharedMemory name size accessType =
                try
                    let protectionFlags = Helpers.mappingTypeToProtectionFlags MappingType.SharedMapping accessType
                    
                    // Convert size to high and low 32-bit values
                    let (sizeHigh, sizeLow) = Helpers.sizeToHighLow (int64 size)
                    
                    let mappingHandle =
                        NativeMethods.CreateFileMappingW(
                            NativeMethods.INVALID_HANDLE_VALUE, // Use paging file
                            0n,                                 // Default security attributes
                            protectionFlags,                    // Page protection
                            sizeHigh,                           // Maximum size high dword
                            sizeLow,                            // Maximum size low dword
                            Helpers.toWideNullTerminated name   // Mapping name
                        )
                    
                    if mappingHandle = 0n then
                        Error (invalidValueError $"Failed to create shared memory: {Helpers.getLastErrorString()}")
                    else
                        let accessFlags = Helpers.accessTypeToFileMappingAccess accessType
                        
                        let baseAddress =
                            NativeMethods.MapViewOfFile(
                                mappingHandle,               // File mapping handle
                                accessFlags,                 // Access mode
                                0u,                          // Offset high dword
                                0u,                          // Offset low dword
                                unativeint (int size)        // Number of bytes to map
                            )
                        
                        if baseAddress = 0n then
                            NativeMethods.CloseHandle(mappingHandle) |> ignore
                            Error (invalidValueError $"Failed to map view of shared memory: {Helpers.getLastErrorString()}")
                        else
                            Ok (mappingHandle, baseAddress)
                with ex ->
                    Error (invalidValueError $"Failed to create shared memory: {ex.Message}")
            
            member this.OpenSharedMemory name accessType =
                try
                    let accessFlags = Helpers.accessTypeToFileMappingAccess accessType
                    
                    let mappingHandle =
                        NativeMethods.OpenFileMappingW(
                            accessFlags,                      // Access mode
                            false,                            // Don't inherit handle
                            Helpers.toWideNullTerminated name // Mapping name
                        )
                    
                    if mappingHandle = 0n then
                        Error (invalidValueError $"Failed to open shared memory: {Helpers.getLastErrorString()}")
                    else
                        // Map the entire shared memory region
                        let baseAddress =
                            NativeMethods.MapViewOfFile(
                                mappingHandle,               // File mapping handle
                                accessFlags,                 // Access mode
                                0u,                          // Offset high dword
                                0u,                          // Offset low dword
                                unativeint 0                 // 0 means map the entire file
                            )
                        
                        if baseAddress = 0n then
                            NativeMethods.CloseHandle(mappingHandle) |> ignore
                            Error (invalidValueError $"Failed to map view of shared memory: {Helpers.getLastErrorString()}")
                        else
                            // Determine the size of the mapping
                            // In a real implementation, we would use VirtualQuery to get the region size
                            // For simplicity, we use a fixed size as this is just a placeholder
                            let size = 4096<bytes>
                            
                            Ok (mappingHandle, baseAddress, size)
                with ex ->
                    Error (invalidValueError $"Failed to open shared memory: {ex.Message}")
            
            member this.CloseSharedMemory handle address size =
                try
                    // First unmap the view
                    if not (NativeMethods.UnmapViewOfFile(address)) then
                        Error (invalidValueError $"Failed to unmap view of shared memory: {Helpers.getLastErrorString()}")
                    else
                        // Then close the mapping handle
                        if NativeMethods.CloseHandle(handle) then
                            Ok ()
                        else
                            Error (invalidValueError $"Failed to close shared memory handle: {Helpers.getLastErrorString()}")
                with ex ->
                    Error (invalidValueError $"Failed to close shared memory: {ex.Message}")
            
            member this.ResourceExists name resourceType =
                try
                    match resourceType.ToLowerInvariant() with
                    | "pipe" ->
                        let pipeName = Helpers.formatPipeName name
                        NativeMethods.WaitNamedPipeW(pipeName, 0u) // 0 means don't wait
                    
                    | "sharedmemory" | "sharedmem" | "memory" ->
                        let handle = 
                            NativeMethods.OpenFileMappingW(
                                NativeMethods.FILE_MAP_READ, // Read-only access is sufficient for checking existence
                                false,                       // Don't inherit handle
                                Helpers.toWideNullTerminated name
                            )
                        
                        if handle <> 0n then
                            NativeMethods.CloseHandle(handle) |> ignore
                            true
                        else
                            false
                    
                    | _ ->
                        false
                with _ ->
                    false
    
    /// <summary>
    /// Windows implementation of the network provider
    /// </summary>
    type WindowsNetworkProvider() =
        interface IPlatformNetwork with
            member this.CreateSocket addressFamily socketType protocolType =
                try
                    // Create a socket using the WinSock API
                    let handle =
                        NativeMethods.socket(
                            int addressFamily,  // Address family
                            int socketType,     // Socket type
                            int protocolType    // Protocol type
                        )
                    
                    if handle = -1n then
                        Error (invalidValueError $"Failed to create socket: {Helpers.getLastErrorString()}")
                    else
                        Ok handle
                with ex ->
                    Error (invalidValueError $"Failed to create socket: {ex.Message}")
            
            member this.BindSocket handle address port =
                try
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
                            handle,
                            &&sockAddr |> NativePtr.toNativeInt,
                            sizeof<NativeMethods.SockAddrIn> |> int
                        )
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to bind socket: {Helpers.getLastErrorString()}")
                    else
                        Ok ()
                with ex ->
                    Error (invalidValueError $"Failed to bind socket: {ex.Message}")
            
            member this.ListenSocket handle backlog =
                try
                    // Start listening on the socket
                    let result = NativeMethods.listen(handle, backlog)
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to listen on socket: {Helpers.getLastErrorString()}")
                    else
                        Ok ()
                with ex ->
                    Error (invalidValueError $"Failed to listen on socket: {ex.Message}")
            
            member this.AcceptSocket handle =
                try
                    // Create a sockaddr structure to receive the client address
                    let mutable clientAddr = Helpers.createSockAddrIn(0u, 0us)
                    let mutable addrLen = sizeof<NativeMethods.SockAddrIn> |> int
                    
                    // Accept the connection
                    let clientHandle = 
                        NativeMethods.accept(
                            handle,
                            &&clientAddr |> NativePtr.toNativeInt,
                            &&addrLen |> NativePtr.toNativeInt
                        )
                    
                    if clientHandle = -1n then
                        Error (invalidValueError $"Failed to accept connection: {Helpers.getLastErrorString()}")
                    else
                        // Extract the client address and port
                        let addr = NativeMethods.inet_ntoa(clientAddr.sin_addr)
                        let port = NativeMethods.ntohs(clientAddr.sin_port) |> int
                        
                        Ok (clientHandle, addr, port)
                with ex ->
                    Error (invalidValueError $"Failed to accept connection: {ex.Message}")
            
            member this.ConnectSocket handle address port =
                try
                    // Convert the address to network byte order
                    let addr = NativeMethods.inet_addr(address)
                    
                    // Create a sockaddr structure
                    let mutable sockAddr = Helpers.createSockAddrIn(addr, uint16 port)
                    
                    // Connect the socket
                    let result = 
                        NativeMethods.connect(
                            handle,
                            &&sockAddr |> NativePtr.toNativeInt,
                            sizeof<NativeMethods.SockAddrIn> |> int
                        )
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to connect socket: {Helpers.getLastErrorString()}")
                    else
                        Ok ()
                with ex ->
                    Error (invalidValueError $"Failed to connect socket: {ex.Message}")
            
            member this.SendSocket handle data offset count flags =
                try
                    // Pin the array to get a fixed pointer and ensure GC doesn't move it
                    let bytesSent = 0
                    let bytesSentPtr = &&bytesSent |> NativePtr.toNativeInt
                    
                    let dataPtr = &&data.[offset] |> NativePtr.toNativeInt
                    
                    // Send the data
                    let result = 
                        NativeMethods.send(
                            handle,
                            dataPtr,
                            count,
                            flags
                        )
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to send data: {Helpers.getLastErrorString()}")
                    else
                        Ok result
                with ex ->
                    Error (invalidValueError $"Failed to send data: {ex.Message}")
            
            member this.ReceiveSocket handle buffer offset count flags =
                try
                    // Pin the array to get a fixed pointer and ensure GC doesn't move it
                    let bytesRead = 0
                    let bytesReadPtr = &&bytesRead |> NativePtr.toNativeInt
                    
                    let bufferPtr = &&buffer.[offset] |> NativePtr.toNativeInt
                    
                    // Receive data
                    let result = 
                        NativeMethods.recv(
                            handle,
                            bufferPtr,
                            count,
                            flags
                        )
                    
                    if result = -1 then
                        let errorCode = int (NativeMethods.WSAGetLastError())
                        
                        // WSAEWOULDBLOCK (10035) means no data available in non-blocking mode
                        if errorCode = 10035 then
                            Ok 0
                        else
                            Error (invalidValueError $"Failed to receive data: {Helpers.getLastErrorString()}")
                    else
                        Ok result
                with ex ->
                    Error (invalidValueError $"Failed to receive data: {ex.Message}")
            
            member this.CloseSocket handle =
                try
                    // Close the socket
                    let result = NativeMethods.closesocket(handle)
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to close socket: {Helpers.getLastErrorString()}")
                    else
                        Ok ()
                with ex ->
                    Error (invalidValueError $"Failed to close socket: {ex.Message}")
            
            member this.ShutdownSocket handle how =
                try
                    // Shut down the socket
                    let result = NativeMethods.shutdown(handle, int how)
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to shut down socket: {Helpers.getLastErrorString()}")
                    else
                        Ok ()
                with ex ->
                    Error (invalidValueError $"Failed to shut down socket: {ex.Message}")
            
            member this.SetSocketOption handle level optionName optionValue =
                try
                    // Pin the array to get a fixed pointer and ensure GC doesn't move it
                    let optionValuePtr = &&optionValue.[0] |> NativePtr.toNativeInt
                    
                    // Set the socket option
                    let result = 
                        NativeMethods.setsockopt(
                            handle,
                            level,
                            optionName,
                            optionValuePtr,
                            optionValue.Length
                        )
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to set socket option: {Helpers.getLastErrorString()}")
                    else
                        Ok ()
                with ex ->
                    Error (invalidValueError $"Failed to set socket option: {ex.Message}")
            
            member this.GetSocketOption handle level optionName optionValue =
                try
                    // Pin the array to get a fixed pointer and ensure GC doesn't move it
                    let optionValuePtr = &&optionValue.[0] |> NativePtr.toNativeInt
                    
                    // Variable to receive the option length
                    let mutable optionLen = optionValue.Length
                    let optionLenPtr = &&optionLen |> NativePtr.toNativeInt
                    
                    // Get the socket option
                    let result = 
                        NativeMethods.getsockopt(
                            handle,
                            level,
                            optionName,
                            optionValuePtr,
                            optionLenPtr
                        )
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to get socket option: {Helpers.getLastErrorString()}")
                    else
                        Ok optionLen
                with ex ->
                    Error (invalidValueError $"Failed to get socket option: {ex.Message}")
            
            member this.GetLocalEndPoint handle =
                try
                    // Create a sockaddr structure to receive the local address
                    let mutable localAddr = Helpers.createSockAddrIn(0u, 0us)
                    let mutable addrLen = sizeof<NativeMethods.SockAddrIn> |> int
                    
                    // Get the local address
                    let result = 
                        NativeMethods.getsockname(
                            handle,
                            &&localAddr |> NativePtr.toNativeInt,
                            &&addrLen |> NativePtr.toNativeInt
                        )
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to get local endpoint: {Helpers.getLastErrorString()}")
                    else
                        // Extract the local address and port
                        let addr = NativeMethods.inet_ntoa(localAddr.sin_addr)
                        let port = NativeMethods.ntohs(localAddr.sin_port) |> int
                        
                        Ok (addr, port)
                with ex ->
                    Error (invalidValueError $"Failed to get local endpoint: {ex.Message}")
            
            member this.GetRemoteEndPoint handle =
                try
                    // Create a sockaddr structure to receive the remote address
                    let mutable remoteAddr = Helpers.createSockAddrIn(0u, 0us)
                    let mutable addrLen = sizeof<NativeMethods.SockAddrIn> |> int
                    
                    // Get the remote address
                    let result = 
                        NativeMethods.getpeername(
                            handle,
                            &&remoteAddr |> NativePtr.toNativeInt,
                            &&addrLen |> NativePtr.toNativeInt
                        )
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to get remote endpoint: {Helpers.getLastErrorString()}")
                    else
                        // Extract the remote address and port
                        let addr = NativeMethods.inet_ntoa(remoteAddr.sin_addr)
                        let port = NativeMethods.ntohs(remoteAddr.sin_port) |> int
                        
                        Ok (addr, port)
                with ex ->
                    Error (invalidValueError $"Failed to get remote endpoint: {ex.Message}")
            
            member this.Poll handle timeout =
                try
                    // Create a fd_set structure
                    let mutable readFds = Helpers.createFdSet()
                    
                    // Add the socket to the set
                    NativeMethods.FD_SET(handle, &&readFds |> NativePtr.toNativeInt)
                    
                    // Create a timeout structure
                    let mutable tv = Helpers.createTimeval(timeout)
                    
                    // Select on the socket
                    let result = 
                        NativeMethods.select(
                            1,                                  // nfds (ignored on Windows)
                            &&readFds |> NativePtr.toNativeInt, // readfds
                            0n,                                 // writefds
                            0n,                                 // exceptfds
                            &&tv |> NativePtr.toNativeInt       // timeout
                        )
                    
                    if result = -1 then
                        Error (invalidValueError $"Failed to poll socket: {Helpers.getLastErrorString()}")
                    else
                        // Check if the socket is in the set
                        let isSet = NativeMethods.FD_ISSET(handle, &&readFds |> NativePtr.toNativeInt)
                        
                        Ok (isSet <> 0)
                with ex ->
                    Error (invalidValueError $"Failed to poll socket: {ex.Message}")
            
            member this.ResolveHostName hostName =
                try
                    // Get the host by name
                    let hostEnt = NativeMethods.gethostbyname(hostName)
                    
                    if hostEnt = 0n then
                        Error (invalidValueError $"Failed to resolve host name: {Helpers.getLastErrorString()}")
                    else
                        // Access the host entry structure
                        let hostEntry = NativePtr.ofNativeInt hostEnt |> NativePtr.read
                        
                        // Get the address list
                        let addrList = NativePtr.ofNativeInt hostEntry.h_addr_list
                        
                        // Collect all addresses
                        let mutable addresses = []
                        let mutable i = 0
                        
                        while NativePtr.get addrList i <> 0n do
                            let inAddr = NativePtr.ofNativeInt (NativePtr.get addrList i) |> NativePtr.read
                            let addr = NativeMethods.inet_ntoa(inAddr)
                            addresses <- addr :: addresses
                            i <- i + 1
                        
                        Ok (List.rev addresses |> Array.ofList)
                with ex ->
                    Error (invalidValueError $"Failed to resolve host name: {ex.Message}")

    /// <summary>
    /// Windows implementation of the synchronization provider
    /// </summary>
    type WindowsSyncProvider() =
        interface IPlatformSync with
            member this.CreateMutex name initialOwner =
                try
                    let handle =
                        NativeMethods.CreateMutexW(
                            0n,                                 // Default security attributes
                            initialOwner,                       // Initial owner flag
                            Helpers.toWideNullTerminated name   // Mutex name
                        )
                    
                    if handle = 0n then
                        Error (invalidValueError $"Failed to create mutex: {Helpers.getLastErrorString()}")
                    else
                        Ok handle
                with ex ->
                    Error (invalidValueError $"Failed to create mutex: {ex.Message}")
            
            member this.OpenMutex name =
                try
                    let handle =
                        NativeMethods.OpenMutexW(
                            0x001F0001u,                       // MUTEX_ALL_ACCESS
                            false,                             // Don't inherit handle
                            Helpers.toWideNullTerminated name  // Mutex name
                        )
                    
                    if handle = 0n then
                        Error (invalidValueError $"Failed to open mutex: {Helpers.getLastErrorString()}")
                    else
                        Ok handle
                with ex ->
                    Error (invalidValueError $"Failed to open mutex: {ex.Message}")
            
            member this.AcquireMutex handle timeout =
                try
                    let waitResult = 
                        NativeMethods.WaitForSingleObject(
                            handle,
                            if timeout = -1 then 0xFFFFFFFFu else uint32 timeout
                        )
                    
                    match waitResult with
                    | 0u -> Ok true                // WAIT_OBJECT_0 - The mutex was acquired
                    | 0x80u -> Ok false            // WAIT_ABANDONED - The mutex was acquired, but it was abandoned
                    | 0x102u -> Ok false           // WAIT_TIMEOUT - The timeout elapsed
                    | _ -> Error (invalidValueError $"Failed to acquire mutex: {Helpers.getLastErrorString()}")
                with ex ->
                    Error (invalidValueError $"Failed to acquire mutex: {ex.Message}")
            
            member this.ReleaseMutex handle =
                try
                    if NativeMethods.ReleaseMutex(handle) then
                        Ok ()
                    else
                        Error (invalidValueError $"Failed to release mutex: {Helpers.getLastErrorString()}")
                with ex ->
                    Error (invalidValueError $"Failed to release mutex: {ex.Message}")
            
            member this.CloseMutex handle =
                try
                    if NativeMethods.CloseHandle(handle) then
                        Ok ()
                    else
                        Error (invalidValueError $"Failed to close mutex: {Helpers.getLastErrorString()}")
                with ex ->
                    Error (invalidValueError $"Failed to close mutex: {ex.Message}")
            
            member this.CreateSemaphore name initialCount maximumCount =
                try
                    let handle =
                        NativeMethods.CreateSemaphoreW(
                            0n,                                   // Default security attributes
                            initialCount,                         // Initial count
                            maximumCount,                         // Maximum count
                            Helpers.toWideNullTerminated name     // Semaphore name
                        )
                    
                    if handle = 0n then
                        Error (invalidValueError $"Failed to create semaphore: {Helpers.getLastErrorString()}")
                    else
                        Ok handle
                with ex ->
                    Error (invalidValueError $"Failed to create semaphore: {ex.Message}")
            
            member this.OpenSemaphore name =
                try
                    let handle =
                        NativeMethods.OpenSemaphoreW(
                            0x001F0003u,                          // SEMAPHORE_ALL_ACCESS
                            false,                                // Don't inherit handle
                            Helpers.toWideNullTerminated name     // Semaphore name
                        )
                    
                    if handle = 0n then
                        Error (invalidValueError $"Failed to open semaphore: {Helpers.getLastErrorString()}")
                    else
                        Ok handle
                with ex ->
                    Error (invalidValueError $"Failed to open semaphore: {ex.Message}")
            
            member this.AcquireSemaphore handle timeout =
                try
                    let waitResult = 
                        NativeMethods.WaitForSingleObject(
                            handle,
                            if timeout = -1 then 0xFFFFFFFFu else uint32 timeout
                        )
                    
                    match waitResult with
                    | 0u -> Ok true                // WAIT_OBJECT_0 - The semaphore was acquired
                    | 0x102u -> Ok false           // WAIT_TIMEOUT - The timeout elapsed
                    | _ -> Error (invalidValueError $"Failed to acquire semaphore: {Helpers.getLastErrorString()}")
                with ex ->
                    Error (invalidValueError $"Failed to acquire semaphore: {ex.Message}")
            
            member this.ReleaseSemaphore handle releaseCount =
                try
                    let previousCount = 0
                    let previousCountPtr = &&previousCount |> NativePtr.toNativeInt
                    
                    if NativeMethods.ReleaseSemaphore(handle, releaseCount, previousCountPtr) then
                        Ok previousCount
                    else
                        Error (invalidValueError $"Failed to release semaphore: {Helpers.getLastErrorString()}")
                with ex ->
                    Error (invalidValueError $"Failed to release semaphore: {ex.Message}")
            
            member this.CloseSemaphore handle =
                try
                    if NativeMethods.CloseHandle(handle) then
                        Ok ()
                    else
                        Error (invalidValueError $"Failed to close semaphore: {Helpers.getLastErrorString()}")
                with ex ->
                    Error (invalidValueError $"Failed to close semaphore: {ex.Message}")