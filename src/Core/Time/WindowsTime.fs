/// <summary>
/// Windows platform-specific time implementation
/// </summary>
module BAREWire.Core.Time.WindowsTime

open System
open FSharp.NativeInterop
open BAREWire.Core.Time.NativeInterop
open BAREWire.Core.Time.Platform

#nowarn "9"  // Disable warning about using NativePtr

/// <summary>
/// Windows time structures
/// </summary>
[<Struct>]
type FILETIME =
    val mutable dwLowDateTime: uint32
    val mutable dwHighDateTime: uint32
    
    /// <summary>
    /// Converts FILETIME to 64-bit integer
    /// </summary>
    member this.ToInt64() =
        let low = uint64 this.dwLowDateTime
        let high = uint64 this.dwHighDateTime
        int64 ((high <<< 32) ||| low)

[<Struct>]
type SYSTEMTIME =
    val mutable wYear: uint16
    val mutable wMonth: uint16
    val mutable wDayOfWeek: uint16
    val mutable wDay: uint16
    val mutable wHour: uint16
    val mutable wMinute: uint16
    val mutable wSecond: uint16
    val mutable wMilliseconds: uint16

/// <summary>
/// Windows time functions using the enhanced P/Invoke-like API
/// </summary>
module Kernel32 =
    // Define native imports with explicit type annotations
    let getSystemTimeAsFileTimeImport : NativeImport<nativeint -> unit> = 
        {
            LibraryName = "kernel32"
            FunctionName = "GetSystemTimeAsFileTime"
            CallingConvention = CallingConvention.Cdecl
            CharSet = CharSet.Ansi
            SupressErrorHandling = false
        }
        
    let getSystemTimeImport : NativeImport<nativeint -> unit> = 
        {
            LibraryName = "kernel32"
            FunctionName = "GetSystemTime"
            CallingConvention = CallingConvention.Cdecl
            CharSet = CharSet.Ansi
            SupressErrorHandling = false
        }
        
    let queryPerformanceCounterImport : NativeImport<nativeint -> bool> = 
        {
            LibraryName = "kernel32"
            FunctionName = "QueryPerformanceCounter"
            CallingConvention = CallingConvention.Cdecl
            CharSet = CharSet.Ansi
            SupressErrorHandling = false
        }
        
    let queryPerformanceFrequencyImport : NativeImport<nativeint -> bool> = 
        {
            LibraryName = "kernel32"
            FunctionName = "QueryPerformanceFrequency"
            CallingConvention = CallingConvention.Cdecl
            CharSet = CharSet.Ansi
            SupressErrorHandling = false
        }
        
    let sleepImport : NativeImport<uint32 -> unit> = 
        {
            LibraryName = "kernel32"
            FunctionName = "Sleep"
            CallingConvention = CallingConvention.Cdecl
            CharSet = CharSet.Ansi
            SupressErrorHandling = false
        }
        
    let fileTimeToSystemTimeImport : NativeImport<nativeint -> nativeint -> bool> = 
        {
            LibraryName = "kernel32"
            FunctionName = "FileTimeToSystemTime"
            CallingConvention = CallingConvention.Cdecl
            CharSet = CharSet.Ansi
            SupressErrorHandling = false
        }
    
    /// <summary>
    /// Gets the current system time as a FILETIME
    /// </summary>
    let getSystemTimeAsFileTime() =
        // Allocate FILETIME structure
        let fileTime = NativePtr.stackalloc<FILETIME> 1
        
        // Call GetSystemTimeAsFileTime
        invokeFunc1 getSystemTimeAsFileTimeImport (NativePtr.toNativeInt fileTime)
        
        // Read result
        let result = NativePtr.read fileTime
        result.ToInt64()
    
    /// <summary>
    /// Gets the current system time as a SYSTEMTIME
    /// </summary>
    let getSystemTime() =
        // Allocate SYSTEMTIME structure
        let systemTime = NativePtr.stackalloc<SYSTEMTIME> 1
        
        // Call GetSystemTime
        invokeFunc1 getSystemTimeImport (NativePtr.toNativeInt systemTime)
        
        // Read result
        NativePtr.read systemTime
    
    /// <summary>
    /// Gets the current value of the high-resolution performance counter
    /// </summary>
    let queryPerformanceCounter() =
        // Allocate int64 for result
        let counter = NativePtr.stackalloc<int64> 1
        
        // Call QueryPerformanceCounter
        let success = invokeFunc1 queryPerformanceCounterImport (NativePtr.toNativeInt counter)
        
        // Read result
        if success then NativePtr.read counter
        else failwith "QueryPerformanceCounter failed"
    
    /// <summary>
    /// Gets the frequency of the high-resolution performance counter
    /// </summary>
    let queryPerformanceFrequency() =
        // Allocate int64 for result
        let frequency = NativePtr.stackalloc<int64> 1
        
        // Call QueryPerformanceFrequency
        let success = invokeFunc1 queryPerformanceFrequencyImport (NativePtr.toNativeInt frequency)
        
        // Read result
        if success then NativePtr.read frequency
        else failwith "QueryPerformanceFrequency failed"
    
    /// <summary>
    /// Suspends the execution of the current thread for the specified time
    /// </summary>
    let sleep(milliseconds: uint32) =
        // Call Sleep
        invokeFunc1 sleepImport milliseconds
    
    /// <summary>
    /// Converts a FILETIME to a SYSTEMTIME
    /// </summary>
    let fileTimeToSystemTime(fileTime: FILETIME) =
        // Allocate SYSTEMTIME structure
        let systemTime = NativePtr.stackalloc<SYSTEMTIME> 1
        
        // Create FILETIME pointer
        let fileTimePtr = NativePtr.stackalloc<FILETIME> 1
        NativePtr.set fileTimePtr 0 fileTime
        
        // Call FileTimeToSystemTime
        let success = invokeFunc2
                        fileTimeToSystemTimeImport
                        (NativePtr.toNativeInt fileTimePtr) 
                        (NativePtr.toNativeInt systemTime)
        
        // Read result
        if success then NativePtr.read systemTime
        else failwith "FileTimeToSystemTime failed"

/// <summary>
/// Helper functions to convert between time formats
/// </summary>
module TimeConversion =
    // Constants for date conversion
    let private ticksPerMillisecond = 10000L
    let private ticksPerSecond = 10000000L
    let private ticksPerMinute = 600000000L
    let private ticksPerHour = 36000000000L
    let private ticksPerDay = 864000000000L
    
    // .NET epoch (January 1, 0001) to Windows epoch (January 1, 1601) offset
    let private windowsToNetTicksOffset = 504911232000000000L
    
    // Days from year 1 to year 10000 (includes leap years)
    let private daysTo10000 = 3652059L
    
    /// <summary>
    /// Converts a Windows file time to .NET ticks
    /// </summary>
    let fileTimeToTicks (fileTime: int64) =
        fileTime + windowsToNetTicksOffset
    
    /// <summary>
    /// Converts .NET ticks to a Windows file time
    /// </summary>
    let ticksToFileTime (ticks: int64) =
        ticks - windowsToNetTicksOffset
    
    /// <summary>
    /// Checks if the specified year is a leap year
    /// </summary>
    let isLeapYear (year: int) =
        (year % 4 = 0 && year % 100 <> 0) || (year % 400 = 0)
    
    /// <summary>
    /// Gets the number of days in the specified month of the specified year
    /// </summary>
    let daysInMonth (year: int) (month: int) =
        match month with
        | 2 -> if isLeapYear year then 29 else 28
        | 4 | 6 | 9 | 11 -> 30
        | _ -> 31
    
    /// <summary>
    /// Converts a SYSTEMTIME to ticks
    /// </summary>
    let systemTimeToTicks (st: SYSTEMTIME) =
        // This is a simplified implementation
        // A full implementation would handle calendars and time zones
        let days = 
            // Days from year 1 to st.wYear
            let mutable days = 0L
            for y = 1 to int st.wYear - 1 do
                days <- days + (if isLeapYear y then 366L else 365L)
            
            // Add days in months
            for m = 1 to int st.wMonth - 1 do
                days <- days + int64 (daysInMonth (int st.wYear) m)
            
            // Add days in current month
            days + int64 st.wDay - 1L
        
        // Calculate ticks
        let ticks = 
            days * ticksPerDay +
            int64 st.wHour * ticksPerHour +
            int64 st.wMinute * ticksPerMinute +
            int64 st.wSecond * ticksPerSecond +
            int64 st.wMilliseconds * ticksPerMillisecond
        
        ticks

/// <summary>
/// Windows platform implementation of IPlatformTime
/// </summary>
type WindowsTimeImplementation() =
    interface IPlatformTime with
        /// <summary>
        /// Gets the current time in ticks (100-nanosecond intervals since January 1, 0001)
        /// </summary>
        member _.GetCurrentTicks() =
            let fileTime = Kernel32.getSystemTimeAsFileTime()
            TimeConversion.fileTimeToTicks fileTime
        
        /// <summary>
        /// Gets the current UTC time in ticks
        /// </summary>
        member _.GetUtcNow() =
            let fileTime = Kernel32.getSystemTimeAsFileTime()
            TimeConversion.fileTimeToTicks fileTime
        
        /// <summary>
        /// Gets the system time as file time (100-nanosecond intervals since January 1, 1601)
        /// </summary>
        member _.GetSystemTimeAsFileTime() =
            Kernel32.getSystemTimeAsFileTime()
        
        /// <summary>
        /// Gets high-resolution performance counter ticks
        /// </summary>
        member _.GetHighResolutionTicks() =
            Kernel32.queryPerformanceCounter()
        
        /// <summary>
        /// Gets the frequency of the high-resolution performance counter
        /// </summary>
        member _.GetTickFrequency() =
            Kernel32.queryPerformanceFrequency()
        
        /// <summary>
        /// Sleeps for the specified number of milliseconds
        /// </summary>
        member _.Sleep(milliseconds) =
            Kernel32.sleep(uint32 milliseconds)

/// <summary>
/// Factory function to create a Windows time implementation
/// </summary>
let createImplementation() =
    WindowsTimeImplementation() :> IPlatformTime