module BAREWire.Core.Time.MacOSTime

open FSharp.NativeInterop
open BAREWire.Core.Time.NativeInterop
open BAREWire.Core.Time.Platform

#nowarn "9"  // Disable warning about using NativePtr

/// <summary>
/// macOS time structures and constants
/// </summary>
[<Struct>]
type Timeval =
    val mutable tv_sec: int64  // Seconds
    val mutable tv_usec: int64 // Microseconds
    
    member this.ToTicks() =
        // Convert to 100-nanosecond intervals (ticks)
        (this.tv_sec * 10000000L) + (this.tv_usec * 10L)

[<Struct>]
type Timezone =
    val mutable tz_minuteswest: int // Minutes west of Greenwich
    val mutable tz_dsttime: int     // Type of DST correction

[<Struct>]
type MachTimebaseInfo =
    val mutable numer: uint32 // Numerator
    val mutable denom: uint32 // Denominator

/// <summary>
/// macOS time functions
/// </summary>
module LibC =
    // Get time of day
    let gettimeofday() =
        // Allocate timeval structure
        let timeval = NativePtr.stackalloc<Timeval> 1
        
        // Allocate timezone structure (can be null for gettimeofday)
        let timezone = NativePtr.stackalloc<Timezone> 1
        
        // Call gettimeofday
        let result = invokeFunc2 "libc" "gettimeofday" 
                        (NativePtr.toNativeInt timeval) 
                        (NativePtr.toNativeInt timezone)
        
        // Read result
        if result = 0 then
            let tv = NativePtr.read timeval
            tv.ToTicks()
        else
            0L
    
    // Sleep
    let usleep(microseconds: uint32) =
        // Call usleep
        invokeFunc1 "libc" "usleep" microseconds

/// <summary>
/// macOS Mach time functions
/// </summary>
module MachTime =
    // Get absolute time (monotonic clock)
    let mach_absolute_time() =
        // Call mach_absolute_time
        invokeFunc0<uint64> "libc" "mach_absolute_time"
    
    // Get timebase info (for converting absolute time to nanoseconds)
    let mach_timebase_info() =
        // Allocate timebase info structure
        let timebase = NativePtr.stackalloc<MachTimebaseInfo> 1
        
        // Call mach_timebase_info
        let result = invokeFunc1 "libc" "mach_timebase_info" (NativePtr.toNativeInt timebase)
        
        // Read result
        if result = 0 then
            let tb = NativePtr.read timebase
            (tb.numer, tb.denom)
        else
            (1u, 1u)

/// <summary>
/// Helper constants for time conversion
/// </summary>
module TimeConversion =
    // Unix epoch (January 1, 1970) to .NET epoch (January 1, 0001) offset
    let private unixToNetTicksOffset = 621355968000000000L
    
    // Convert Unix time (seconds since 1970) to .NET ticks
    let unixTimeToTicks (unixTime: int64) =
        unixTime * 10000000L + unixToNetTicksOffset
    
    // Convert .NET ticks to Unix time
    let ticksToUnixTime (ticks: int64) =
        (ticks - unixToNetTicksOffset) / 10000000L
    
    // Convert Mach absolute time to ticks
    let machAbsoluteTimeToTicks (machTime: uint64) =
        // Get timebase info
        let (numer, denom) = MachTime.mach_timebase_info()
        
        // Convert to nanoseconds
        let nanoseconds = (int64 machTime) * (int64 numer) / (int64 denom)
        
        // Convert to ticks (100-nanosecond intervals)
        nanoseconds / 100L

/// <summary>
/// macOS platform implementation
/// </summary>
type MacOSTimeImplementation() =
    // Cache timebase info
    let (numer, denom) = MachTime.mach_timebase_info()
    let ticksPerSecond = 10000000L * (int64 denom) / (int64 numer)
    
    interface IPlatformTime with
        member _.GetCurrentTicks() =
            let unixTime = LibC.gettimeofday()
            TimeConversion.unixTimeToTicks(unixTime / 10000000L) + (unixTime % 10000000L)
        
        member _.GetUtcNow() =
            let unixTime = LibC.gettimeofday()
            TimeConversion.unixTimeToTicks(unixTime / 10000000L) + (unixTime % 10000000L)
        
        member _.GetSystemTimeAsFileTime() =
            let unixTime = LibC.gettimeofday()
            // Convert from Unix epoch to Windows file time (1601-01-01)
            unixTime + 116444736000000000L
        
        member _.GetHighResolutionTicks() =
            let machTime = MachTime.mach_absolute_time()
            TimeConversion.machAbsoluteTimeToTicks machTime
        
        member _.GetTickFrequency() =
            // Return the frequency in 100ns ticks
            ticksPerSecond
        
        member _.Sleep(milliseconds) =
            LibC.usleep(uint32 (milliseconds * 1000))

/// <summary>
/// Factory function
/// </summary>
let createImplementation() =
    MacOSTimeImplementation() :> IPlatformTime