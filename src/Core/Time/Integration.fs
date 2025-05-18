/// <summary>
/// Integration functions for BAREWire Time module
/// </summary>
module BAREWire.Core.Time.Integration

open BAREWire.Core.Time.Platform

/// <summary>
/// Integration functions for Time module, providing a consistent API
/// across different platform implementations
/// </summary>
[<AutoOpen>]
module TimeIntegration =
    /// <summary>
    /// Platform-specific time implementation that is lazily initialized
    /// </summary>
    let private platformTime : IPlatformTime = 
        try
            getImplementation()
        with 
        | :? PlatformNotSupportedException as ex -> 
            // Provide specific error details
            failwith $"Failed to initialize time implementation: {ex.Message}"
        | ex -> 
            // Handle unexpected errors
            failwith $"Unexpected error initializing time implementation: {ex.Message}"
        
    
    /// <summary>
    /// Gets the current time in ticks from the platform
    /// </summary>
    /// <returns>The current time in 100-nanosecond intervals since January 1, 0001</returns>
    let getCurrentTicks () = 
        platformTime.GetCurrentTicks()
    
    /// <summary>
    /// Gets the current UTC time in ticks
    /// </summary>
    /// <returns>The current UTC time in 100-nanosecond intervals since January 1, 0001</returns>
    let getUtcNow () = 
        platformTime.GetUtcNow()
    
    /// <summary>
    /// Gets the system time as file time
    /// </summary>
    /// <returns>The current time as a file time (100-nanosecond intervals since January 1, 1601)</returns>
    let getSystemTimeAsFileTime () = 
        platformTime.GetSystemTimeAsFileTime()
    
    /// <summary>
    /// Gets high-resolution performance counter ticks
    /// </summary>
    /// <returns>The current value of the high-resolution performance counter</returns>
    let getHighResolutionTicks () = 
        platformTime.GetHighResolutionTicks()
    
    /// <summary>
    /// Gets the frequency of the high-resolution performance counter
    /// </summary>
    /// <returns>The frequency in ticks per second</returns>
    let getTickFrequency () = 
        platformTime.GetTickFrequency()
    
    /// <summary>
    /// Sleeps for the specified number of milliseconds
    /// </summary>
    /// <param name="milliseconds">The number of milliseconds to sleep</param>
    let sleep (milliseconds: int) = 
        if milliseconds < 0 then
            // Validate input
            invalidArg "milliseconds" "Sleep duration must be non-negative"
        
        platformTime.Sleep(milliseconds)