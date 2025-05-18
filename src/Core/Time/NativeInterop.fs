module BAREWire.Core.Time.NativeInterop

open FSharp.NativeInterop

#nowarn "9"  // Disable warning about using NativePtr

/// Simple key-value pair implementation for library handles
type private LibraryHandle = { 
    LibraryName: string
    Handle: nativeint 
}

/// Native library loading and function pointer retrieval
module NativeLibrary =
    // Simple array-based lookup - no System.Collections dependency
    let private libraryHandles = ref ([| |]: LibraryHandle array)
    
    /// Find library handle by name
    let private findLibrary (libraryName: string) : nativeint option =
        let handles = !libraryHandles
        let rec findInArray idx =
            if idx >= handles.Length then None
            elif handles.[idx].LibraryName = libraryName then Some handles.[idx].Handle
            else findInArray (idx + 1)
        findInArray 0
    
    /// Add library handle to array
    let private addLibrary (libraryName: string) (handle: nativeint) : unit =
        let handles = !libraryHandles
        let newHandles = Array.append handles [| { LibraryName = libraryName; Handle = handle } |]
        libraryHandles := newHandles
    
    /// <summary>
    /// Load a native library by name - pure F# implementation with no System dependencies
    /// </summary>
    let load (libraryPath: string) : nativeint =
        match findLibrary libraryPath with
        | Some handle -> handle
        | None ->
            let handle = 
                #if WINDOWS
                // Windows implementation
                // Directly access Windows API by address
                
                // For Windows, we need to bootload kernel32.dll LoadLibraryW function
                // This requires knowing its address in memory or setting up a custom loader
                
                // We'll use a simpler approach for this example:
                // Initialize with a placeholder address that would be determined at runtime
                // in a real implementation via custom native bootstrapping code
                let loadLibraryAddress = 0x12345678n // Placeholder
                
                // Convert library path to wide string (UTF-16)
                let mutable pathChars = Array.zeroCreate<char> (libraryPath.Length + 1)
                for i = 0 to libraryPath.Length - 1 do
                    pathChars.[i] <- libraryPath.[i]
                
                let pathPtr = NativePtr.stackalloc<char> (libraryPath.Length + 1)
                for i = 0 to libraryPath.Length - 1 do
                    NativePtr.set pathPtr i libraryPath.[i]
                NativePtr.set pathPtr libraryPath.Length '\000'
                
                // Call LoadLibraryW through direct memory access
                let loadLibraryFn = NativePtr.ofNativeInt<nativeint -> nativeint> loadLibraryAddress
                let handle = loadLibraryFn (NativePtr.toNativeInt pathPtr)
                handle
                
                #elif LINUX
                // Linux implementation
                // Initialize with a placeholder address that would be determined at runtime
                let dlopenAddress = 0x12345678n // Placeholder
                
                // Convert library path to ASCII string
                let pathPtr = NativePtr.stackalloc<byte> (libraryPath.Length + 1)
                for i = 0 to libraryPath.Length - 1 do
                    NativePtr.set pathPtr i (byte libraryPath.[i])
                NativePtr.set pathPtr libraryPath.Length 0uy
                
                // Call dlopen through direct memory access
                let dlopenFn = NativePtr.ofNativeInt<nativeint -> int -> nativeint> dlopenAddress
                let RTLD_NOW = 2
                let handle = dlopenFn (NativePtr.toNativeInt pathPtr) RTLD_NOW
                handle
                
                #elif MACOS
                // macOS implementation (similar to Linux)
                // Initialize with a placeholder address that would be determined at runtime
                let dlopenAddress = 0x12345678n // Placeholder
                
                // Convert library path to ASCII string
                let pathPtr = NativePtr.stackalloc<byte> (libraryPath.Length + 1)
                for i = 0 to libraryPath.Length - 1 do
                    NativePtr.set pathPtr i (byte libraryPath.[i])
                NativePtr.set pathPtr libraryPath.Length 0uy
                
                // Call dlopen through direct memory access
                let dlopenFn = NativePtr.ofNativeInt<nativeint -> int -> nativeint> dlopenAddress
                let RTLD_NOW = 2
                let handle = dlopenFn (NativePtr.toNativeInt pathPtr) RTLD_NOW
                handle
                
                #else
                failwith "Unsupported platform"
                #endif
            
            addLibrary libraryPath handle
            handle
    
    /// <summary>
    /// Get a function pointer from a library handle - pure F# implementation with no System dependencies
    /// </summary>
    let getFunctionPointer (handle: nativeint) (functionName: string) : nativeint =
        #if WINDOWS
        // Windows implementation
        // Initialize with a placeholder address that would be determined at runtime
        let getProcAddress = 0x12345678n // Placeholder
        
        // Convert function name to ASCII string
        let namePtr = NativePtr.stackalloc<byte> (functionName.Length + 1)
        for i = 0 to functionName.Length - 1 do
            NativePtr.set namePtr i (byte functionName.[i])
        NativePtr.set namePtr functionName.Length 0uy
        
        // Call GetProcAddress through direct memory access
        let getProcAddressFn = NativePtr.ofNativeInt<nativeint -> nativeint -> nativeint> getProcAddress
        getProcAddressFn handle (NativePtr.toNativeInt namePtr)
        
        #elif LINUX
        // Linux implementation
        // Initialize with a placeholder address that would be determined at runtime
        let dlsymAddress = 0x12345678n // Placeholder
        
        // Convert function name to ASCII string
        let namePtr = NativePtr.stackalloc<byte> (functionName.Length + 1)
        for i = 0 to functionName.Length - 1 do
            NativePtr.set namePtr i (byte functionName.[i])
        NativePtr.set namePtr functionName.Length 0uy
        
        // Call dlsym through direct memory access
        let dlsymFn = NativePtr.ofNativeInt<nativeint -> nativeint -> nativeint> dlsymAddress
        dlsymFn handle (NativePtr.toNativeInt namePtr)
        
        #elif MACOS
        // macOS implementation (similar to Linux)
        // Initialize with a placeholder address that would be determined at runtime
        let dlsymAddress = 0x12345678n // Placeholder
        
        // Convert function name to ASCII string
        let namePtr = NativePtr.stackalloc<byte> (functionName.Length + 1)
        for i = 0 to functionName.Length - 1 do
            NativePtr.set namePtr i (byte functionName.[i])
        NativePtr.set namePtr functionName.Length 0uy
        
        // Call dlsym through direct memory access
        let dlsymFn = NativePtr.ofNativeInt<nativeint -> nativeint -> nativeint> dlsymAddress
        dlsymFn handle (NativePtr.toNativeInt namePtr)
        
        #else
        failwith "Unsupported platform"
        #endif

/// <summary>
/// Invoke function with no arguments - pure F# implementation with no System dependencies
/// </summary>
let invokeFunc0<'TResult> (libName: string) (funcName: string) : 'TResult =
    let handle = NativeLibrary.load libName
    let fnPtr = NativeLibrary.getFunctionPointer handle funcName
    
    // Create function delegate type
    let fnDelegate = NativePtr.ofNativeInt<unit -> 'TResult> fnPtr
    
    // Invoke function
    fnDelegate()

/// <summary>
/// Invoke function with one argument - pure F# implementation with no System dependencies
/// </summary>
let invokeFunc1<'T1, 'TResult> (libName: string) (funcName: string) (arg1: 'T1) : 'TResult =
    let handle = NativeLibrary.load libName
    let fnPtr = NativeLibrary.getFunctionPointer handle funcName
    
    // Create function delegate type
    let fnDelegate = NativePtr.ofNativeInt<'T1 -> 'TResult> fnPtr
    
    // Invoke function
    fnDelegate arg1

/// <summary>
/// Invoke function with two arguments - pure F# implementation with no System dependencies
/// </summary>
let invokeFunc2<'T1, 'T2, 'TResult> (libName: string) (funcName: string) (arg1: 'T1) (arg2: 'T2) : 'TResult =
    let handle = NativeLibrary.load libName
    let fnPtr = NativeLibrary.getFunctionPointer handle funcName
    
    // Create function delegate type
    let fnDelegate = NativePtr.ofNativeInt<'T1 -> 'T2 -> 'TResult> fnPtr
    
    // Invoke function
    fnDelegate arg1 arg2

/// <summary>
/// Invoke function with three arguments - pure F# implementation with no System dependencies
/// </summary>
let invokeFunc3<'T1, 'T2, 'T3, 'TResult> (libName: string) (funcName: string) (arg1: 'T1) (arg2: 'T2) (arg3: 'T3) : 'TResult =
    let handle = NativeLibrary.load libName
    let fnPtr = NativeLibrary.getFunctionPointer handle funcName
    
    // Create function delegate type
    let fnDelegate = NativePtr.ofNativeInt<'T1 -> 'T2 -> 'T3 -> 'TResult> fnPtr
    
    // Invoke function
    fnDelegate arg1 arg2 arg3

/// <summary>
/// Invoke function with four arguments - pure F# implementation with no System dependencies
/// </summary>
let invokeFunc4<'T1, 'T2, 'T3, 'T4, 'TResult> (libName: string) (funcName: string) (arg1: 'T1) (arg2: 'T2) (arg3: 'T3) (arg4: 'T4) : 'TResult =
    let handle = NativeLibrary.load libName
    let fnPtr = NativeLibrary.getFunctionPointer handle funcName
    
    // Create function delegate type
    let fnDelegate = NativePtr.ofNativeInt<'T1 -> 'T2 -> 'T3 -> 'T4 -> 'TResult> fnPtr
    
    // Invoke function
    fnDelegate arg1 arg2 arg3 arg4