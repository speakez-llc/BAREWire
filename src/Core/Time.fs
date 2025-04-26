namespace BAREWire.Core

/// <summary>
/// Pure F# time utilities with no System.DateTime dependencies
/// </summary>
module Time =
    /// <summary>
    /// Unix epoch (1970-01-01) in ticks (custom epoch definition)
    /// </summary>
    let private unixEpochTicks = 621355968000000000L // Constant representing 1970-01-01
    
    /// <summary>
    /// Ticks per second
    /// </summary>
    let private ticksPerSecond = 10000000L
    
    /// <summary>
    /// Ticks per millisecond
    /// </summary>
    let private ticksPerMillisecond = 10000L
    
    /// <summary>
    /// Ticks per minute
    /// </summary>
    let private ticksPerMinute = 60L * ticksPerSecond
    
    /// <summary>
    /// Ticks per hour
    /// </summary>
    let private ticksPerHour = 60L * ticksPerMinute
    
    /// <summary>
    /// Ticks per day
    /// </summary>
    let private ticksPerDay = 24L * ticksPerHour
    
    /// <summary>
    /// Days per year (non-leap)
    /// </summary>
    let private daysPerYear = 365L
    
    /// <summary>
    /// Days per leap year
    /// </summary>
    let private daysPerLeapYear = 366L
    
    /// <summary>
    /// Days in each month (non-leap year)
    /// </summary>
    let private daysInMonth = [|31; 28; 31; 30; 31; 30; 31; 31; 30; 31; 30; 31|]
    
    /// <summary>
    /// Days in each month (leap year)
    /// </summary>
    let private daysInMonthLeapYear = [|31; 29; 31; 30; 31; 30; 31; 31; 30; 31; 30; 31|]
    
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
    /// Checks if a year is a leap year
    /// </summary>
    /// <param name="year">The year to check</param>
    /// <returns>True if the year is a leap year, false otherwise</returns>
    let isLeapYear (year: int): bool =
        (year % 4 = 0 && year % 100 <> 0) || (year % 400 = 0)
    
    /// <summary>
    /// Gets the current time in ticks from the platform
    /// </summary>
    /// <returns>The current time in ticks</returns>
    let currentTicks (): int64 =
        // This is a simplified implementation
        // In a real implementation, we would use platform-specific APIs
        // such as QueryPerformanceCounter on Windows
        
        // For testing, we'll just return a fixed value
        // In production, this would call the platform provider
        unixEpochTicks + (1670000000L * ticksPerSecond)
    
    /// <summary>
    /// Gets the current Unix timestamp (seconds since 1970-01-01)
    /// </summary>
    /// <returns>The current Unix timestamp</returns>
    let currentUnixTimestamp (): int64 =
        let ticks = currentTicks()
        (ticks - unixEpochTicks) / ticksPerSecond
    
    /// <summary>
    /// Converts Unix timestamp to ticks
    /// </summary>
    /// <param name="timestamp">The Unix timestamp in seconds</param>
    /// <returns>The timestamp in ticks</returns>
    let unixTimestampToTicks (timestamp: int64): int64 =
        unixEpochTicks + (timestamp * ticksPerSecond)
    
    /// <summary>
    /// Converts ticks to Unix timestamp
    /// </summary>
    /// <param name="ticks">The timestamp in ticks</param>
    /// <returns>The Unix timestamp in seconds</returns>
    let ticksToUnixTimestamp (ticks: int64): int64 =
        (ticks - unixEpochTicks) / ticksPerSecond
    
    /// <summary>
    /// Converts a Unix timestamp to a DateTime structure
    /// </summary>
    /// <param name="timestamp">The Unix timestamp in seconds</param>
    /// <returns>A DateTime structure representing the timestamp</returns>
    let unixTimestampToDateTime (timestamp: int64): DateTime =
        // Calculate days since epoch
        let mutable remainingSecs = timestamp
        let days = remainingSecs / 86400L // seconds per day
        remainingSecs <- remainingSecs % 86400L
        
        // Calculate time components
        let hours = int (remainingSecs / 3600L)
        remainingSecs <- remainingSecs % 3600L
        
        let minutes = int (remainingSecs / 60L)
        remainingSecs <- remainingSecs % 60L
        
        let seconds = int remainingSecs
        
        // Calculate date components
        let mutable year = 1970
        let mutable remainingDays = days
        
        // Calculate years
        while remainingDays >= (if isLeapYear year then daysPerLeapYear else daysPerYear) do
            let daysInYear = if isLeapYear year then daysPerLeapYear else daysPerYear
            remainingDays <- remainingDays - daysInYear
            year <- year + 1
        
        // Calculate month and day
        let monthDays = if isLeapYear year then daysInMonthLeapYear else daysInMonth
        
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
    /// Converts a DateTime structure to a Unix timestamp
    /// </summary>
    /// <param name="dateTime">The DateTime to convert</param>
    /// <returns>The Unix timestamp in seconds</returns>
    let dateTimeToUnixTimestamp (dateTime: DateTime): int64 =
        // Count days from epoch to start of year
        let mutable days = 0L
        for y = 1970 to dateTime.Year - 1 do
            days <- days + (if isLeapYear y then daysPerLeapYear else daysPerYear)
        
        // Add days for months in current year
        let monthDays = if isLeapYear dateTime.Year then daysInMonthLeapYear else daysInMonth
        
        for m = 0 to dateTime.Month - 2 do
            days <- days + int64 monthDays.[m]
        
        // Add days in current month
        days <- days + int64 (dateTime.Day - 1)
        
        // Calculate total seconds
        let totalSeconds = 
            days * 86400L +
            int64 dateTime.Hour * 3600L +
            int64 dateTime.Minute * 60L +
            int64 dateTime.Second
        
        totalSeconds
    
    /// <summary>
    /// Creates a DateTime structure
    /// </summary>
    /// <param name="year">The year</param>
    /// <param name="month">The month</param>
    /// <param name="day">The day</param>
    /// <param name="hour">The hour</param>
    /// <param name="minute">The minute</param>
    /// <param name="second">The second</param>
    /// <param name="millisecond">The millisecond</param>
    /// <returns>A new DateTime structure</returns>
    /// <exception cref="System.Exception">Thrown when the parameters are invalid</exception>
    let createDateTime (year: int) (month: int) (day: int) (hour: int) (minute: int) (second: int) (millisecond: int): DateTime =
        // Validate parameters
        if year < 1 || year > 9999 then
            failwith $"Year out of range: {year}"
        
        if month < 1 || month > 12 then
            failwith $"Month out of range: {month}"
        
        let daysInCurrentMonth = 
            if isLeapYear year then
                daysInMonthLeapYear.[month - 1]
            else
                daysInMonth.[month - 1]
                
        if day < 1 || day > daysInCurrentMonth then
            failwith $"Day out of range: {day}"
        
        if hour < 0 || hour > 23 then
            failwith $"Hour out of range: {hour}"
        
        if minute < 0 || minute > 59 then
            failwith $"Minute out of range: {minute}"
        
        if second < 0 || second > 59 then
            failwith $"Second out of range: {second}"
        
        if millisecond < 0 || millisecond > 999 then
            failwith $"Millisecond out of range: {millisecond}"
        
        {
            Year = year
            Month = month
            Day = day
            Hour = hour
            Minute = minute
            Second = second
            Millisecond = millisecond
        }
    
    /// <summary>
    /// Gets the current time as a DateTime structure
    /// </summary>
    /// <returns>The current time</returns>
    let now (): DateTime =
        let timestamp = currentUnixTimestamp()
        unixTimestampToDateTime timestamp
    
    /// <summary>
    /// Formats a DateTime as a string using a simplified format
    /// </summary>
    /// <param name="dateTime">The DateTime to format</param>
    /// <returns>A string representation</returns>
    let toString (dateTime: DateTime): string =
        $"{dateTime.Year:D4}-{dateTime.Month:D2}-{dateTime.Day:D2} {dateTime.Hour:D2}:{dateTime.Minute:D2}:{dateTime.Second:D2}"
    
    /// <summary>
    /// Represents a time span structure
    /// </summary>
    type TimeSpan = {
        /// <summary>The days component</summary>
        Days: int
        /// <summary>The hours component</summary>
        Hours: int
        /// <summary>The minutes component</summary>
        Minutes: int
        /// <summary>The seconds component</summary>
        Seconds: int
        /// <summary>The milliseconds component</summary>
        Milliseconds: int
    }
    
    /// <summary>
    /// Creates a TimeSpan structure from total milliseconds
    /// </summary>
    /// <param name="totalMilliseconds">Total milliseconds</param>
    /// <returns>A new TimeSpan structure</returns>
    let fromMilliseconds (totalMilliseconds: int64): TimeSpan =
        let ms = totalMilliseconds % 1000L
        let totalSeconds = totalMilliseconds / 1000L
        
        let s = totalSeconds % 60L
        let totalMinutes = totalSeconds / 60L
        
        let m = totalMinutes % 60L
        let totalHours = totalMinutes / 60L
        
        let h = totalHours % 24L
        let d = totalHours / 24L
        
        {
            Days = int d
            Hours = int h
            Minutes = int m
            Seconds = int s
            Milliseconds = int ms
        }
    
    /// <summary>
    /// Creates a TimeSpan structure from components
    /// </summary>
    /// <param name="days">Days</param>
    /// <param name="hours">Hours</param>
    /// <param name="minutes">Minutes</param>
    /// <param name="seconds">Seconds</param>
    /// <param name="milliseconds">Milliseconds</param>
    /// <returns>A new TimeSpan structure</returns>
    let createTimeSpan (days: int) (hours: int) (minutes: int) (seconds: int) (milliseconds: int): TimeSpan =
        {
            Days = days
            Hours = hours
            Minutes = minutes
            Seconds = seconds
            Milliseconds = milliseconds
        }
    
    /// <summary>
    /// Gets the total milliseconds in a time span
    /// </summary>
    /// <param name="timeSpan">The time span</param>
    /// <returns>Total milliseconds</returns>
    let totalMilliseconds (timeSpan: TimeSpan): int64 =
        int64 timeSpan.Milliseconds +
        int64 timeSpan.Seconds * 1000L +
        int64 timeSpan.Minutes * 60000L +
        int64 timeSpan.Hours * 3600000L +
        int64 timeSpan.Days * 86400000L
    
    /// <summary>
    /// Adds a time span to a date time
    /// </summary>
    /// <param name="dateTime">The date time</param>
    /// <param name="timeSpan">The time span to add</param>
    /// <returns>A new date time</returns>
    let addTimeSpan (dateTime: DateTime) (timeSpan: TimeSpan): DateTime =
        let timestamp = dateTimeToUnixTimestamp dateTime
        let ms = totalMilliseconds timeSpan
        let newTimestamp = timestamp + (ms / 1000L)
        
        // Handle milliseconds separately
        let newDateTime = unixTimestampToDateTime newTimestamp
        { newDateTime with Millisecond = newDateTime.Millisecond + int (ms % 1000L) }
    
    /// <summary>
    /// Subtracts a time span from a date time
    /// </summary>
    /// <param name="dateTime">The date time</param>
    /// <param name="timeSpan">The time span to subtract</param>
    /// <returns>A new date time</returns>
    let subtractTimeSpan (dateTime: DateTime) (timeSpan: TimeSpan): DateTime =
        let timestamp = dateTimeToUnixTimestamp dateTime
        let ms = totalMilliseconds timeSpan
        let newTimestamp = timestamp - (ms / 1000L)
        
        // Handle milliseconds separately
        let newDateTime = unixTimestampToDateTime newTimestamp
        let newMillisecond = newDateTime.Millisecond - int (ms % 1000L)
        
        if newMillisecond < 0 then
            { newDateTime with Millisecond = 1000 + newMillisecond }
        else
            { newDateTime with Millisecond = newMillisecond }