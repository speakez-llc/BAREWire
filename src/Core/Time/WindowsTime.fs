module BAREWire.Core.Time.WindowsTime

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
/// Windows time functions 
/// </summary>
module Kernel32 =
    let getSystemTimeAsFileTime() =
        // Allocate FILETIME structure
        let fileTime = NativePtr.stackalloc<FILETIME> 1
        
        // Call GetSystemTimeAsFileTime
        invokeFunc1 "kernel32" "GetSystemTimeAsFileTime" (NativePtr.toNativeInt fileTime)
        
        // Read result
        let result = NativePtr.read fileTime
        result.ToInt64()
    
    let getSystemTime() =
        // Allocate SYSTEMTIME structure
        let systemTime = NativePtr.stackalloc<SYSTEMTIME> 1
        
        // Call GetSystemTime
        invokeFunc1 "kernel32" "GetSystemTime" (NativePtr.toNativeInt systemTime)
        
        // Read result
        NativePtr.read systemTime
    
    let queryPerformanceCounter() =
        // Allocate int64 for result
        let counter = NativePtr.stackalloc<int64> 1
        
        // Call QueryPerformanceCounter
        let success = invokeFunc1 "kernel32" "QueryPerformanceCounter" (NativePtr.toNativeInt counter)
        
        // Read result
        if success then NativePtr.read counter
        else 0L
    
    let queryPerformanceFrequency() =
        // Allocate int64 for result
        let frequency = NativePtr.stackalloc<int64> 1
        
        // Call QueryPerformanceFrequency
        let success = invokeFunc1 "kernel32" "QueryPerformanceFrequency" (NativePtr.toNativeInt frequency)
        
        // Read result
        if success then NativePtr.read frequency
        else 0L
    
    let sleep(milliseconds: uint32) =
        // Call Sleep
        invokeFunc1 "kernel32" "Sleep" milliseconds
    
    let fileTimeToSystemTime(fileTime: FILETIME) =
        // Allocate SYSTEMTIME structure
        let systemTime = NativePtr.stackalloc<SYSTEMTIME> 1
        
        // Create FILETIME pointer
        let fileTimePtr = NativePtr.stackalloc<FILETIME> 1
        NativePtr.set fileTimePtr 0 fileTime
        
        // Call FileTimeToSystemTime
        let success = invokeFunc2 "kernel32" "FileTimeToSystemTime" 
                        (NativePtr.toNativeInt fileTimePtr) 
                        (NativePtr.toNativeInt systemTime)
        
        // Read result
        if success then NativePtr.read systemTime
        else Unchecked.defaultof<SYSTEMTIME>

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
    
    // File time to .NET ticks
    let fileTimeToTicks (fileTime: int64) =
        fileTime + windowsToNetTicksOffset
    
    // .NET ticks to file time
    let ticksToFileTime (ticks: int64) =
        ticks - windowsToNetTicksOffset
    
    // Convert SYSTEMTIME to ticks
    let systemTimeToTicks (st: SYSTEMTIME) =
        // This is a simplified implementation
        // A full implementation would handle calendars and time zones
        let days = 
            // Days from year 1 to st.wYear
            let mutable days = 0L
            for y = 1 to int st.wYear - 1 do
                days <- days + (if y % 4 = 0 && (y % 100 <> 0 || y % 400 = 0) then 366L else 365L)
            
            // Add days in months
            let daysInMonth = [|31; 28; 31; 30; 31; 30; 31; 31; 30; 31; 30; 31|]
            let isLeapYear = int st.wYear % 4 = 0 && (int st.wYear % 100 <> 0 || int st.wYear % 400 = 0)
            if isLeapYear then daysInMonth.[1] <- 29
            
            for m = 1 to int st.wMonth - 1 do
                days <- days + int64 daysInMonth.[m - 1]
            
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
/// Windows platform implementation
/// </summary>
type WindowsTimeImplementation() =
    interface IPlatformTime with
        member _.GetCurrentTicks() =
            let fileTime = Kernel32.getSystemTimeAsFileTime()
            TimeConversion.fileTimeToTicks fileTime
        
        member _.GetUtcNow() =
            let fileTime = Kernel32.getSystemTimeAsFileTime()
            TimeConversion.fileTimeToTicks fileTime
        
        member _.GetSystemTimeAsFileTime() =
            Kernel32.getSystemTimeAsFileTime()
        
        member _.GetHighResolutionTicks() =
            Kernel32.queryPerformanceCounter()
        
        member _.GetTickFrequency() =
            Kernel32.queryPerformanceFrequency()
        
        member _.Sleep(milliseconds) =
            Kernel32.sleep(uint32 milliseconds)

/// <summary>
/// Factory function
/// </summary>
let createImplementation() =
    WindowsTimeImplementation() :> IPlatformTime