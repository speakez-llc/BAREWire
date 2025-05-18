module BAREWire.Core.Time.LinuxTime

open FSharp.NativeInterop
open BAREWire.Core.Time.NativeInterop
open BAREWire.Core.Time.Platform

#nowarn "9"  // Disable warning about using NativePtr

/// <summary>
/// Unix time structures
/// </summary>
[<Struct>]
type Timespec =
    val mutable tv_sec: int64  // Seconds
    val mutable tv_nsec: int64 // Nanoseconds
    
    member this.ToTicks() =
        // Convert to 100-nanosecond intervals (ticks)
        (this.tv_sec * 10000000L) + (this.tv_nsec / 100L)

/// <summary>
/// Unix clock IDs
/// </summary>
[<Struct>]
type ClockID =
    | CLOCK_REALTIME = 0
    | CLOCK_MONOTONIC = 1
    | CLOCK_PROCESS_CPUTIME_ID = 2
    | CLOCK_THREAD_CPUTIME_ID = 3

/// <summary>
/// Unix time functions
/// </summary>
module LibC =
    let clockGettime(clockId: ClockID) =
        // Allocate timespec structure
        let timespec = NativePtr.stackalloc<Timespec> 1
        
        // Call clock_gettime
        let result = invokeFunc2 "libc" "clock_gettime" (int clockId) (NativePtr.toNativeInt timespec)
        
        // Read result
        if result = 0 then
            let ts = NativePtr.read timespec
            ts.ToTicks()
        else
            0L
    
    let nanosleep(seconds: int64, nanoseconds: int64) =
        // Create timespec structure
        let req = NativePtr.stackalloc<Timespec> 1
        NativePtr.set req 0 { tv_sec = seconds; tv_nsec = nanoseconds }
        
        // Allocate for remaining time
        let rem = NativePtr.stackalloc<Timespec> 1
        
        // Call nanosleep
        invokeFunc2 "libc" "nanosleep" (NativePtr.toNativeInt req) (NativePtr.toNativeInt rem)

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

/// <summary>
/// Linux platform implementation
/// </summary>
type LinuxTimeImplementation() =
    interface IPlatformTime with
        member _.GetCurrentTicks() =
            let unixTime = LibC.clockGettime(ClockID.CLOCK_REALTIME)
            TimeConversion.unixTimeToTicks(unixTime / 10000000L) + (unixTime % 10000000L)
        
        member _.GetUtcNow() =
            let unixTime = LibC.clockGettime(ClockID.CLOCK_REALTIME)
            TimeConversion.unixTimeToTicks(unixTime / 10000000L) + (unixTime % 10000000L)
        
        member _.GetSystemTimeAsFileTime() =
            let unixTime = LibC.clockGettime(ClockID.CLOCK_REALTIME)
            // Convert from Unix epoch to Windows file time (1601-01-01)
            unixTime + 116444736000000000L
        
        member _.GetHighResolutionTicks() =
            LibC.clockGettime(ClockID.CLOCK_MONOTONIC)
        
        member _.GetTickFrequency() =
            // CLOCK_MONOTONIC is in nanoseconds, so frequency is 1,000,000,000 (ticks per second)
            10000000L // Convert to 100ns ticks (same scale as .NET ticks)
        
        member _.Sleep(milliseconds) =
            let seconds = int64 (milliseconds / 1000)
            let nanoseconds = int64 ((milliseconds % 1000) * 1000000)
            LibC.nanosleep(seconds, nanoseconds)

/// <summary>
/// Factory function
/// </summary>
let createImplementation() =
    LinuxTimeImplementation() :> IPlatformTime