/// <summary>
/// macOS platform-specific time implementation
/// </summary>
module BAREWire.Core.Time.MacOSTime

open System
open FSharp.NativeInterop
open BAREWire.Core.Time.NativeInterop
open BAREWire.Core.Time.Platform

#nowarn "9"  // Disable warning about using NativePtr

/// <summary>
/// macOS time structures
/// </summary>
[<Struct>]
type Timeval =
    val mutable tv_sec: int64  // Seconds
    val mutable tv_usec: int64 // Microseconds
    
    /// <summary>
    /// Converts Timeval to ticks (100-nanosecond intervals)
    /// </summary>
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
/// macOS time functions using enhanced P/Invoke-like API
/// </summary>
module LibC =
    // Helper function to ensure correct type inference for function delegates
    let inline importFunc2<'T1, 'T2, 'TResult> libraryName functionName =
        // Force the correct type through explicit construction
        {
            LibraryName = libraryName
            FunctionName = functionName
            CallingConvention = CallingConvention.Cdecl
            CharSet = CharSet.Ansi
            SupressErrorHandling = false
        } : NativeImport<'T1 -> 'T2 -> 'TResult>

    let gettimeofdayImport = 
        {
            LibraryName = "libc"
            FunctionName = "gettimeofday"
            CallingConvention = CallingConvention.Cdecl
            CharSet = CharSet.Ansi
            SupressErrorHandling = false
        } : NativeImport<nativeint -> nativeint -> int>
        
    let usleepImport = 
        {
            LibraryName = "libc"
            FunctionName = "usleep"
            CallingConvention = CallingConvention.Cdecl
            CharSet = CharSet.Ansi
            SupressErrorHandling = false
        } : NativeImport<uint32 -> int>
        
    let inline importFunc1<'T1, 'TResult> libraryName functionName =
        dllImport<'T1 -> 'TResult> libraryName functionName
        
    let inline importFunc0<'TResult> libraryName functionName =
        dllImport<unit -> 'TResult> libraryName functionName
    
   
    /// <summary>
    /// Gets the current time of day
    /// </summary>
    let gettimeofday() =
        // Allocate timeval structure
        let timeval = NativePtr.stackalloc<Timeval> 1
        
        // Allocate timezone structure (can be null for gettimeofday)
        let timezone = NativePtr.stackalloc<Timezone> 1
        
        // Call gettimeofday
        let result = invokeFunc2 
                        gettimeofdayImport
                        (NativePtr.toNativeInt timeval) 
                        (NativePtr.toNativeInt timezone)
        
        // Read result
        if result = 0 then
            let tv = NativePtr.read timeval
            tv.ToTicks()
        else
            failwith $"gettimeofday failed with error code {result}"
    
    /// <summary>
    /// Suspends execution for the specified number of microseconds
    /// </summary>
    let usleep(microseconds: uint32) =
        // Call usleep
        let result = invokeFunc1 usleepImport microseconds
        
        if result < 0 then
            // In a real implementation, we would check errno here
            failwith "usleep failed"

/// <summary>
/// macOS Mach time functions using enhanced P/Invoke-like API
/// </summary>
module MachTime =
    // Reuse the helper functions from LibC for consistent imports
    open LibC

    let inline createImport<'TDelegate> libraryName functionName =
        {
            LibraryName = libraryName
            FunctionName = functionName
            CallingConvention = CallingConvention.Cdecl
            CharSet = CharSet.Ansi
            SupressErrorHandling = false
        } : NativeImport<'TDelegate>
    
    // Define native imports
    let mach_absolute_timeImport : NativeImport<unit -> uint64> = 
        {
            LibraryName = "libc"
            FunctionName = "mach_absolute_time"
            CallingConvention = CallingConvention.Cdecl
            CharSet = CharSet.Ansi
            SupressErrorHandling = false
        }
        
    let mach_timebase_infoImport : NativeImport<nativeint -> int> = 
        {
            LibraryName = "libc"
            FunctionName = "mach_timebase_info"
            CallingConvention = CallingConvention.Cdecl
            CharSet = CharSet.Ansi
            SupressErrorHandling = false
        }
    
    /// <summary>
    /// Gets the current value of the high-resolution clock
    /// </summary>
    let mach_absolute_time() =
        // Call mach_absolute_time
        invokeFunc0 mach_absolute_timeImport
    
    /// <summary>
    /// Gets the timebase info for converting absolute time to nanoseconds
    /// </summary>
    let mach_timebase_info() =
        // Allocate timebase info structure
        let timebase = NativePtr.stackalloc<MachTimebaseInfo> 1
        
        // Call mach_timebase_info
        let result = invokeFunc1 mach_timebase_infoImport (NativePtr.toNativeInt timebase)
        
        // Read result
        if result = 0 then
            let tb = NativePtr.read timebase
            (tb.numer, tb.denom)
        else
            failwith $"mach_timebase_info failed with error code {result}"

/// <summary>
/// Helper constants for time conversion
/// </summary>
module TimeConversion =
    // Unix epoch (January 1, 1970) to .NET epoch (January 1, 0001) offset
    let private unixToNetTicksOffset = 621355968000000000L
    
    /// <summary>
    /// Convert Unix time (seconds since 1970) to .NET ticks
    /// </summary>
    let unixTimeToTicks (unixTime: int64) =
        unixTime * 10000000L + unixToNetTicksOffset
    
    /// <summary>
    /// Convert .NET ticks to Unix time
    /// </summary>
    let ticksToUnixTime (ticks: int64) =
        (ticks - unixToNetTicksOffset) / 10000000L
    
    /// <summary>
    /// Convert Mach absolute time to ticks
    /// </summary>
    let machAbsoluteTimeToTicks (machTime: uint64) =
        // Get timebase info
        let (numer, denom) = MachTime.mach_timebase_info()
        
        // Convert to nanoseconds
        let nanoseconds = (int64 machTime) * (int64 numer) / (int64 denom)
        
        // Convert to ticks (100-nanosecond intervals)
        nanoseconds / 100L

/// <summary>
/// macOS platform implementation of IPlatformTime
/// </summary>
type MacOSTimeImplementation() =
    // Cache timebase info
    let (numer, denom) = MachTime.mach_timebase_info()
    let ticksPerSecond = 10000000L * (int64 denom) / (int64 numer)
    
    interface IPlatformTime with
        /// <summary>
        /// Gets the current time in ticks (100-nanosecond intervals since January 1, 0001)
        /// </summary>
        member _.GetCurrentTicks() =
            let unixTime = LibC.gettimeofday()
            TimeConversion.unixTimeToTicks(unixTime / 10000000L) + (unixTime % 10000000L)
        
        /// <summary>
        /// Gets the current UTC time in ticks
        /// </summary>
        member _.GetUtcNow() =
            let unixTime = LibC.gettimeofday()
            TimeConversion.unixTimeToTicks(unixTime / 10000000L) + (unixTime % 10000000L)
        
        /// <summary>
        /// Gets the system time as file time (100-nanosecond intervals since January 1, 1601)
        /// </summary>
        member _.GetSystemTimeAsFileTime() =
            let unixTime = LibC.gettimeofday()
            // Convert from Unix epoch to Windows file time (1601-01-01)
            unixTime + 116444736000000000L
        
        /// <summary>
        /// Gets high-resolution performance counter ticks
        /// </summary>
        member _.GetHighResolutionTicks() =
            let machTime = MachTime.mach_absolute_time()
            TimeConversion.machAbsoluteTimeToTicks machTime
        
        /// <summary>
        /// Gets the frequency of the high-resolution performance counter
        /// </summary>
        member _.GetTickFrequency() =
            // Return the frequency in 100ns ticks
            ticksPerSecond
        
        /// <summary>
        /// Sleeps for the specified number of milliseconds
        /// </summary>
        member _.Sleep(milliseconds) =
            LibC.usleep(uint32 (milliseconds * 1000))

/// <summary>
/// Factory function to create a macOS time implementation
/// </summary>
let createImplementation() =
    MacOSTimeImplementation() :> IPlatformTime