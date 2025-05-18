module BAREWire.Core.Time.Integration

open BAREWire.Core.Time.Platform

/// <summary>
/// Integration functions for Time module
/// </summary>
[<AutoOpen>]
module TimeIntegration =
    /// <summary>
    /// Platform-specific time implementation
    /// </summary>
    let private platformTime = getImplementation()
    
    /// <summary>
    /// Gets the current time in ticks from the platform
    /// </summary>
    let getCurrentTicks () = platformTime.GetCurrentTicks()
    
    /// <summary>
    /// Gets the current UTC time in ticks
    /// </summary>
    let getUtcNow () = platformTime.GetUtcNow()
    
    /// <summary>
    /// Gets the system time as file time
    /// </summary>
    let getSystemTimeAsFileTime () = platformTime.GetSystemTimeAsFileTime()
    
    /// <summary>
    /// Gets high-resolution performance counter ticks
    /// </summary>
    let getHighResolutionTicks () = platformTime.GetHighResolutionTicks()
    
    /// <summary>
    /// Gets the frequency of the high-resolution performance counter
    /// </summary>
    let getTickFrequency () = platformTime.GetTickFrequency()
    
    /// <summary>
    /// Sleeps for the specified number of milliseconds
    /// </summary>
    let sleep (milliseconds: int) = platformTime.Sleep(milliseconds)