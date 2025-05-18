module BAREWire.Core.Time.Platform

/// <summary>
/// Interface for platform-specific time operations
/// </summary>
type IPlatformTime =
    /// <summary>
    /// Gets the current time in ticks (100-nanosecond intervals since January 1, 0001)
    /// </summary>
    abstract member GetCurrentTicks: unit -> int64
    
    /// <summary>
    /// Gets the current UTC time in ticks
    /// </summary>
    abstract member GetUtcNow: unit -> int64
    
    /// <summary>
    /// Gets the system time as file time (100-nanosecond intervals since January 1, 1601)
    /// </summary>
    abstract member GetSystemTimeAsFileTime: unit -> int64
    
    /// <summary>
    /// Gets high-resolution performance counter ticks
    /// </summary>
    abstract member GetHighResolutionTicks: unit -> int64
    
    /// <summary>
    /// Gets the frequency of the high-resolution performance counter
    /// </summary>
    abstract member GetTickFrequency: unit -> int64
    
    /// <summary>
    /// Sleeps for the specified number of milliseconds
    /// </summary>
    abstract member Sleep: milliseconds:int -> unit

/// <summary>
/// Function to get the appropriate platform implementation
/// </summary>
let getImplementation() =
    #if WINDOWS
    WindowsTime.createImplementation()
    #elif LINUX
    LinuxTime.createImplementation()
    #elif MACOS
    MacOSTime.createImplementation()
    #elif ANDROID
    AndroidTime.createImplementation()
    #elif IOS
    IOSTime.createImplementation()
    #elif WASM
    WebAssemblyTime.createImplementation()
    #else
    failwith "Unsupported platform"
    #endif