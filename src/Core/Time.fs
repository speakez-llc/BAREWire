namespace BAREWire.Core

open BAREWire.Platform

/// <summary>
/// Time utilities for platform-independent time handling
/// </summary>
module Time =
    /// <summary>
    /// Unix epoch (1970-01-01) in .NET ticks
    /// </summary>
    let private unixEpochTicks = 621355968000000000L // .NET ticks for 1970-01-01
    
    /// <summary>
    /// Ticks per second in .NET
    /// </summary>
    let private ticksPerSecond = 10000000L
    
    /// <summary>
    /// Gets the current Unix timestamp (seconds since 1970-01-01)
    /// </summary>
    /// <returns>The current Unix timestamp</returns>
    let currentUnixTimestamp () : int64 =
        // Get platform-specific time provider
        let provider = PlatformServices.getTimeProvider() |> Result.get
        provider.GetUnixTimestamp()
    
    /// <summary>
    /// Gets the current time in ticks
    /// </summary>
    /// <returns>The current time in ticks (implementation-defined unit)</returns>
    let currentTicks () : int64 =
        // Get platform-specific time provider
        let provider = PlatformServices.getTimeProvider() |> Result.get
        provider.GetCurrentTicks()
    
    /// <summary>
    /// Converts Unix timestamp to ticks
    /// </summary>
    /// <param name="timestamp">The Unix timestamp in seconds</param>
    /// <returns>The timestamp in ticks</returns>
    let unixTimestampToTicks (timestamp: int64) : int64 =
        unixEpochTicks + (timestamp * ticksPerSecond)
    
    /// <summary>
    /// Converts ticks to Unix timestamp
    /// </summary>
    /// <param name="ticks">The timestamp in ticks</param>
    /// <returns>The Unix timestamp in seconds</returns>
    let ticksToUnixTimestamp (ticks: int64) : int64 =
        (ticks - unixEpochTicks) / ticksPerSecond
    
    /// <summary>
    /// Represents a date and time structure
    /// </summary>
    type DateTime = {
        /// <summary>The year component (1-9999)</summary>
        Year: int
        /// <summary>The month component (1-12)</summary>
        Month: int
        /// <summary>The day component (1-31)</summary>
        Day: int
        /// <summary>The hour component (0-23)</summary>
        Hour: int
        /// <summary>The minute component (0-59)</summary>
        Minute: int
        /// <summary>The second component (0-59)</summary>
        Second: int
        /// <summary>The millisecond component (0-999)</summary>
        Millisecond: int
    }
    
    /// <summary>
    /// Converts a Unix timestamp to a DateTime structure
    /// </summary>
    /// <param name="timestamp">The Unix timestamp in seconds</param>
    /// <returns>A DateTime structure representing the timestamp</returns>
    /// <remarks>
    /// This is a simplified implementation without timezone handling
    /// </remarks>
    let unixTimestampToDateTime (timestamp: int64) : DateTime =
        // Constants for date calculation
        let secondsPerMinute = 60L
        let secondsPerHour = 60L * secondsPerMinute
        let secondsPerDay = 24L * secondsPerHour
        let daysPerYear = 365L
        let daysPerLeapYear = 366L
        
        // Calculate days since epoch
        let mutable remainingSecs = timestamp
        let mutable days = remainingSecs / secondsPerDay
        remainingSecs <- remainingSecs % secondsPerDay
        
        // Calculate time components
        let hours = int (remainingSecs / secondsPerHour)
        remainingSecs <- remainingSecs % secondsPerHour
        
        let minutes = int (remainingSecs / secondsPerMinute)
        remainingSecs <- remainingSecs % secondsPerMinute
        
        let seconds = int remainingSecs
        
        // Calculate date components
        let mutable year = 1970
        let mutable remainingDays = days
        
        while remainingDays >= (if isLeapYear year then daysPerLeapYear else daysPerYear) do
            remainingDays <- remainingDays - (if isLeapYear year then daysPerLeapYear else daysPerYear)
            year <- year + 1
            
        let monthDays = 
            if isLeapYear year then
                [|31; 29; 31; 30; 31; 30; 31; 31; 30; 31; 30; 31|]
            else
                [|31; 28; 31; 30; 31; 30; 31; 31; 30; 31; 30; 31|]
                
        let mutable month = 0
        while month < 12 && remainingDays >= int64 monthDays.[month] do
            remainingDays <- remainingDays - int64 monthDays.[month]
            month <- month + 1
            
        {
            Year = year
            Month = month + 1
            Day = int remainingDays + 1
            Hour = hours
            Minute = minutes
            Second = seconds
            Millisecond = 0
        }
    
    /// <summary>
    /// Checks if a year is a leap year
    /// </summary>
    /// <param name="year">The year to check</param>
    /// <returns>True if the year is a leap year, false otherwise</returns>
    and isLeapYear (year: int) : bool =
        (year % 4 = 0 && year % 100 <> 0) || (year % 400 = 0)
        
    /// <summary>
    /// Converts a DateTime structure to a Unix timestamp
    /// </summary>
    /// <param name="dateTime">The DateTime to convert</param>
    /// <returns>The Unix timestamp in seconds</returns>
    let dateTimeToUnixTimestamp (dateTime: DateTime) : int64 =
        // Constants for date calculation
        let secondsPerMinute = 60L
        let secondsPerHour = 60L * secondsPerMinute
        let secondsPerDay = 24L * secondsPerHour
        
        // Count days from epoch to start of year
        let mutable days = 0L
        for y = 1970 to dateTime.Year - 1 do
            days <- days + (if isLeapYear y then 366L else 365L)
            
        // Add days for months in current year
        let monthDays = 
            if isLeapYear dateTime.Year then
                [|31; 29; 31; 30; 31; 30; 31; 31; 30; 31; 30; 31|]
            else
                [|31; 28; 31; 30; 31; 30; 31; 31; 30; 31; 30; 31|]
                
        for m = 0 to dateTime.Month - 2 do
            days <- days + int64 monthDays.[m]
            
        // Add days in current month
        days <- days + int64 (dateTime.Day - 1)
        
        // Calculate total seconds
        let totalSeconds = 
            days * secondsPerDay +
            int64 dateTime.Hour * secondsPerHour +
            int64 dateTime.Minute * secondsPerMinute +
            int64 dateTime.Second
            
        totalSeconds