/// <summary>
/// Native interoperability layer for BAREWire, providing P/Invoke-like functionality 
/// without System.Runtime.InteropServices dependencies
/// </summary>
module BAREWire.Core.Time.NativeInterop

open FSharp.NativeInterop

#nowarn "9"  // Disable warning about using NativePtr

/// <summary>
/// Calling convention for native functions
/// </summary>
type CallingConvention =
    | Cdecl = 0
    | StdCall = 1
    | ThisCall = 2
    | FastCall = 3
    | WinApi = 4  // StdCall on Windows, Cdecl elsewhere

/// <summary>
/// Character set to use for string marshalling
/// </summary>
type CharSet =
    | Ansi = 0
    | Unicode = 1
    | Auto = 2

/// <summary>
/// Native function import definition
/// </summary>
type NativeImport<'TDelegate> = {
    /// <summary>The name of the library containing the function</summary>
    LibraryName: string
    /// <summary>The name of the function to import</summary>
    FunctionName: string
    /// <summary>Optional calling convention (defaults to Cdecl)</summary>
    CallingConvention: CallingConvention
    /// <summary>Optional character set for string marshalling (defaults to Ansi)</summary>
    CharSet: CharSet
    /// <summary>Set to true to suppress the standard error handling</summary>
    SupressErrorHandling: bool
}

/// Simple key-value pair implementation for library handles
type private LibraryHandle = { 
    LibraryName: string
    Handle: nativeint 
}

/// <summary>
/// Exception thrown when a native library cannot be loaded
/// </summary>
exception NativeLibraryNotFoundException of libraryName: string * errorMessage: string

/// <summary>
/// Exception thrown when a native function cannot be found
/// </summary>
exception NativeFunctionNotFoundException of libraryName: string * functionName: string * errorMessage: string

/// Native library loading and function pointer retrieval
module NativeLibrary =
    // Simple array-based lookup - no System.Collections dependency
    let private libraryHandles = ref ([| |]: LibraryHandle array)
    
    /// <summary>
    /// Find library handle by name
    /// </summary>
    let private findLibrary (libraryName: string) : nativeint option =
        let handles = !libraryHandles
        let rec findInArray idx =
            if idx >= handles.Length then None
            elif handles.[idx].LibraryName = libraryName then Some handles.[idx].Handle
            else findInArray (idx + 1)
        findInArray 0
    
    /// <summary>
    /// Add library handle to array
    /// </summary>
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
            // Platform-specific library loading implementation
            let handle = 
                #if WINDOWS
                // Windows implementation
                try
                    // On Windows, kernel32.dll is always loaded, so we can get LoadLibraryW directly
                    let kernel32Handle = 
                        // This would be implemented at bootstrap time with an initial loader
                        // For the purposes of this implementation, we use a placeholder
                        // In a real implementation, this would use platform-specific mechanisms to get kernel32
                        0x12345678n // Placeholder - replaced at bootstrap time
                    
                    // Convert library path to wide string (UTF-16)
                    let pathPtr = NativePtr.stackalloc<char> (libraryPath.Length + 1)
                    for i = 0 to libraryPath.Length - 1 do
                        NativePtr.set pathPtr i libraryPath.[i]
                    NativePtr.set pathPtr libraryPath.Length '\000'
                    
                    // Get LoadLibraryW function pointer
                    let loadLibraryFuncPtr = 0x87654321n // Placeholder - resolved at bootstrap
                    let loadLibraryFn = NativePtr.ofNativeInt<nativeint -> nativeint> loadLibraryFuncPtr
                    
                    // Call LoadLibraryW
                    let handle = loadLibraryFn (NativePtr.toNativeInt pathPtr)
                    
                    if handle = 0n then
                        // Get error code
                        let getLastErrorFuncPtr = 0x87654322n // Placeholder - resolved at bootstrap
                        let getLastErrorFn = NativePtr.ofNativeInt<unit -> uint32> getLastErrorFuncPtr
                        let errorCode = getLastErrorFn()
                        raise (NativeLibraryNotFoundException(libraryPath, $"LoadLibrary failed with error code {errorCode}"))
                    
                    handle
                with ex ->
                    raise (NativeLibraryNotFoundException(libraryPath, $"Failed to load library: {ex.Message}"))
                
                #elif LINUX
                // Linux implementation
                try
                    // Get handle to libc which is always loaded
                    let libcHandle = 
                        // This would be implemented at bootstrap time with an initial loader
                        // For the purposes of this implementation, we use a placeholder
                        0x12345678n // Placeholder - replaced at bootstrap time
                    
                    // Convert library path to ASCII string
                    let pathPtr = NativePtr.stackalloc<byte> (libraryPath.Length + 1)
                    for i = 0 to libraryPath.Length - 1 do
                        NativePtr.set pathPtr i (byte libraryPath.[i])
                    NativePtr.set pathPtr libraryPath.Length 0uy
                    
                    // Get dlopen function pointer
                    let dlopenFuncPtr = 0x87654321n // Placeholder - resolved at bootstrap
                    let dlopenFn = NativePtr.ofNativeInt<nativeint -> int -> nativeint> dlopenFuncPtr
                    
                    // Call dlopen with RTLD_NOW | RTLD_GLOBAL
                    let RTLD_NOW = 2
                    let RTLD_GLOBAL = 256 // 0x100
                    let handle = dlopenFn (NativePtr.toNativeInt pathPtr) (RTLD_NOW ||| RTLD_GLOBAL)
                    
                    if handle = 0n then
                        // Get error string
                        let dlerrorFuncPtr = 0x87654322n // Placeholder - resolved at bootstrap
                        let dlerrorFn = NativePtr.ofNativeInt<unit -> nativeint> dlerrorFuncPtr
                        let errorPtr = dlerrorFn()
                        
                        let rec readCString (ptr: nativeint) (index: int) (chars: ResizeArray<char>) =
                            let bytePtr = NativePtr.ofNativeInt<byte> (ptr + nativeint index)
                            let b = NativePtr.read bytePtr
                            if b = 0uy then chars.ToArray() |> System.String
                            else
                                chars.Add(char b)
                                readCString ptr (index + 1) chars
                                
                        let errorMessage = readCString errorPtr 0 (ResizeArray<char>())
                        raise (NativeLibraryNotFoundException(libraryPath, errorMessage))
                    
                    handle
                with ex ->
                    raise (NativeLibraryNotFoundException(libraryPath, $"Failed to load library: {ex.Message}"))
                
                #elif MACOS
                // macOS implementation (similar to Linux)
                try
                    // Get handle to libSystem.dylib which is always loaded
                    let libSystemHandle = 
                        // This would be implemented at bootstrap time with an initial loader
                        // For the purposes of this implementation, we use a placeholder
                        0x12345678n // Placeholder - replaced at bootstrap time
                    
                    // Convert library path to ASCII string
                    let pathPtr = NativePtr.stackalloc<byte> (libraryPath.Length + 1)
                    for i = 0 to libraryPath.Length - 1 do
                        NativePtr.set pathPtr i (byte libraryPath.[i])
                    NativePtr.set pathPtr libraryPath.Length 0uy
                    
                    // Get dlopen function pointer
                    let dlopenFuncPtr = 0x87654321n // Placeholder - resolved at bootstrap
                    let dlopenFn = NativePtr.ofNativeInt<nativeint -> int -> nativeint> dlopenFuncPtr
                    
                    // Call dlopen with RTLD_NOW | RTLD_GLOBAL
                    let RTLD_NOW = 2
                    let RTLD_GLOBAL = 8 // 0x8
                    let handle = dlopenFn (NativePtr.toNativeInt pathPtr) (RTLD_NOW ||| RTLD_GLOBAL)
                    
                    if handle = 0n then
                        // Get error string
                        let dlerrorFuncPtr = 0x87654322n // Placeholder - resolved at bootstrap
                        let dlerrorFn = NativePtr.ofNativeInt<unit -> nativeint> dlerrorFuncPtr
                        let errorPtr = dlerrorFn()
                        
                        let rec readCString (ptr: nativeint) (index: int) (chars: ResizeArray<char>) =
                            let bytePtr = NativePtr.ofNativeInt<byte> (ptr + nativeint index)
                            let b = NativePtr.read bytePtr
                            if b = 0uy then chars.ToArray() |> System.String
                            else
                                chars.Add(char b)
                                readCString ptr (index + 1) chars
                                
                        let errorMessage = readCString errorPtr 0 (ResizeArray<char>())
                        raise (NativeLibraryNotFoundException(libraryPath, errorMessage))
                    
                    handle
                with ex ->
                    raise (NativeLibraryNotFoundException(libraryPath, $"Failed to load library: {ex.Message}"))
                
                #else
                failwith "Unsupported platform"
                #endif
            
            addLibrary libraryPath handle
            handle
    
    /// <summary>
    /// Get a function pointer from a library handle - pure F# implementation with no System dependencies
    /// </summary>
    let getFunctionPointer (handle: nativeint) (functionName: string) : nativeint =
        try
            #if WINDOWS
            // Windows implementation
            // Get handle to kernel32.dll which is always loaded
            let kernel32Handle = 
                // This would be implemented at bootstrap time with an initial loader
                // For the purposes of this implementation, we use a placeholder
                0x12345678n // Placeholder - replaced at bootstrap time
            
            // Convert function name to ASCII string
            let namePtr = NativePtr.stackalloc<byte> (functionName.Length + 1)
            for i = 0 to functionName.Length - 1 do
                NativePtr.set namePtr i (byte functionName.[i])
            NativePtr.set namePtr functionName.Length 0uy
            
            // Get GetProcAddress function pointer
            let getProcAddressFuncPtr = 0x87654321n // Placeholder - resolved at bootstrap
            let getProcAddressFn = NativePtr.ofNativeInt<nativeint -> nativeint -> nativeint> getProcAddressFuncPtr
            
            // Call GetProcAddress
            let funcPtr = getProcAddressFn handle (NativePtr.toNativeInt namePtr)
            
            if funcPtr = 0n then
                // Get error code
                let getLastErrorFuncPtr = 0x87654322n // Placeholder - resolved at bootstrap
                let getLastErrorFn = NativePtr.ofNativeInt<unit -> uint32> getLastErrorFuncPtr
                let errorCode = getLastErrorFn()
                
                // Get library name for error message
                let libraryName = 
                    let handles = !libraryHandles
                    let rec findInArray idx =
                        if idx >= handles.Length then "unknown"
                        elif handles.[idx].Handle = handle then handles.[idx].LibraryName
                        else findInArray (idx + 1)
                    findInArray 0
                
                raise (NativeFunctionNotFoundException(libraryName, functionName, $"GetProcAddress failed with error code {errorCode}"))
            
            funcPtr
            
            #elif LINUX || MACOS
            // Linux/macOS implementation
            // Get handle to libc/libSystem which is always loaded
            let libcHandle = 
                // This would be implemented at bootstrap time with an initial loader
                // For the purposes of this implementation, we use a placeholder
                0x12345678n // Placeholder - replaced at bootstrap time
            
            // Convert function name to ASCII string
            let namePtr = NativePtr.stackalloc<byte> (functionName.Length + 1)
            for i = 0 to functionName.Length - 1 do
                NativePtr.set namePtr i (byte functionName.[i])
            NativePtr.set namePtr functionName.Length 0uy
            
            // Get dlsym function pointer
            let dlsymFuncPtr = 0x87654321n // Placeholder - resolved at bootstrap
            let dlsymFn = NativePtr.ofNativeInt<nativeint -> nativeint -> nativeint> dlsymFuncPtr
            
            // Call dlsym
            let funcPtr = dlsymFn handle (NativePtr.toNativeInt namePtr)
            
            if funcPtr = 0n then
                // Get error string
                let dlerrorFuncPtr = 0x87654322n // Placeholder - resolved at bootstrap
                let dlerrorFn = NativePtr.ofNativeInt<unit -> nativeint> dlerrorFuncPtr
                let errorPtr = dlerrorFn()
                
                let rec readCString (ptr: nativeint) (index: int) (chars: ResizeArray<char>) =
                    let bytePtr = NativePtr.ofNativeInt<byte> (ptr + nativeint index)
                    let b = NativePtr.read bytePtr
                    if b = 0uy then chars.ToArray() |> System.String
                    else
                        chars.Add(char b)
                        readCString ptr (index + 1) chars
                        
                let errorMessage = readCString errorPtr 0 (ResizeArray<char>())
                
                // Get library name for error message
                let libraryName = 
                    let handles = !libraryHandles
                    let rec findInArray idx =
                        if idx >= handles.Length then "unknown"
                        elif handles.[idx].Handle = handle then handles.[idx].LibraryName
                        else findInArray (idx + 1)
                    findInArray 0
                
                raise (NativeFunctionNotFoundException(libraryName, functionName, errorMessage))
            
            funcPtr
            
            #else
            failwith "Unsupported platform"
            #endif
        with
        | :? NativeFunctionNotFoundException as ex -> raise ex
        | ex -> 
            // Get library name for error message
            let libraryName = 
                let handles = !libraryHandles
                let rec findInArray idx =
                    if idx >= handles.Length then "unknown"
                    elif handles.[idx].Handle = handle then handles.[idx].LibraryName
                    else findInArray (idx + 1)
                findInArray 0
            
            raise (NativeFunctionNotFoundException(libraryName, functionName, $"Failed to get function pointer: {ex.Message}"))

/// <summary>
/// Creates a native function import definition
/// </summary>
let inline dllImport<'TDelegate> libraryName functionName =
    {
        LibraryName = libraryName
        FunctionName = functionName
        CallingConvention = CallingConvention.Cdecl
        CharSet = CharSet.Ansi
        SupressErrorHandling = false
    }

/// <summary>
/// Gets a callable delegate for the specified native function import
/// </summary>
let inline private getDelegate<'TDelegate> (import: NativeImport<'TDelegate>) : 'TDelegate =
    // Load the library
    let handle = NativeLibrary.load import.LibraryName
    
    // Get the function pointer
    let funcPtr = NativeLibrary.getFunctionPointer handle import.FunctionName
    
    // Create the delegate
    let fn = NativePtr.ofNativeInt<'TDelegate> funcPtr
    NativePtr.read fn

/// <summary>
/// Invokes a native function with no arguments
/// </summary>
let inline invokeFunc0<'TResult> (import: NativeImport<unit -> 'TResult>) : 'TResult =
    let fn = getDelegate import
    fn()

/// <summary>
/// Invokes a native function with one argument
/// </summary>
let inline invokeFunc1<'T1, 'TResult> (import: NativeImport<'T1 -> 'TResult>) (arg1: 'T1) : 'TResult =
    let fn = getDelegate import
    fn arg1

/// <summary>
/// Invokes a native function with two arguments
/// </summary>
let inline invokeFunc2<'T1, 'T2, 'TResult> 
    (import: NativeImport<'T1 -> 'T2 -> 'TResult>) (arg1: 'T1) (arg2: 'T2) : 'TResult =
    let fn = getDelegate import
    fn arg1 arg2

/// <summary>
/// Invokes a native function with three arguments
/// </summary>
let inline invokeFunc3<'T1, 'T2, 'T3, 'TResult> 
    (import: NativeImport<'T1 -> 'T2 -> 'T3 -> 'TResult>) (arg1: 'T1) (arg2: 'T2) (arg3: 'T3) : 'TResult =
    let fn = getDelegate import
    fn arg1 arg2 arg3

/// <summary>
/// Invokes a native function with four arguments
/// </summary>
let inline invokeFunc4<'T1, 'T2, 'T3, 'T4, 'TResult> 
    (import: NativeImport<'T1 -> 'T2 -> 'T3 -> 'T4 -> 'TResult>) 
    (arg1: 'T1) (arg2: 'T2) (arg3: 'T3) (arg4: 'T4) : 'TResult =
    let fn = getDelegate import
    fn arg1 arg2 arg3 arg4

/// <summary>
/// Simplified legacy invocation (for backward compatibility)
/// </summary>
module Legacy =
    /// <summary>
    /// Invoke function with no arguments - legacy API for backward compatibility
    /// </summary>
    let invokeFunc0<'TResult> (libName: string) (funcName: string) : 'TResult =
        let import = dllImport<unit -> 'TResult> libName funcName
        invokeFunc0 import

    /// <summary>
    /// Invoke function with one argument - legacy API for backward compatibility
    /// </summary>
    let invokeFunc1<'T1, 'TResult> (libName: string) (funcName: string) (arg1: 'T1) : 'TResult =
        let import = dllImport<'T1 -> 'TResult> libName funcName
        invokeFunc1 import arg1

    /// <summary>
    /// Invoke function with two arguments - legacy API for backward compatibility
    /// </summary>
    let invokeFunc2<'T1, 'T2, 'TResult> (libName: string) (funcName: string) (arg1: 'T1) (arg2: 'T2) : 'TResult =
        let import = dllImport<'T1 -> 'T2 -> 'TResult> libName funcName
        invokeFunc2 import arg1 arg2

    /// <summary>
    /// Invoke function with three arguments - legacy API for backward compatibility
    /// </summary>
    let invokeFunc3<'T1, 'T2, 'T3, 'TResult> 
        (libName: string) (funcName: string) (arg1: 'T1) (arg2: 'T2) (arg3: 'T3) : 'TResult =
        let import = dllImport<'T1 -> 'T2 -> 'T3 -> 'TResult> libName funcName
        invokeFunc3 import arg1 arg2 arg3

    /// <summary>
    /// Invoke function with four arguments - legacy API for backward compatibility
    /// </summary>
    let invokeFunc4<'T1, 'T2, 'T3, 'T4, 'TResult> 
        (libName: string) (funcName: string) (arg1: 'T1) (arg2: 'T2) (arg3: 'T3) (arg4: 'T4) : 'TResult =
        let import = dllImport<'T1 -> 'T2 -> 'T3 -> 'T4 -> 'TResult> libName funcName
        invokeFunc4 import arg1 arg2 arg3 arg4