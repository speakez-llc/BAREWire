namespace BAREWire.Core

open Alloy

/// <summary>
/// Pure F# time utilities with no System.DateTime dependencies, built on Alloy's zero-cost abstractions
/// </summary>
module Time =
    let private unixEpochTicks = 621355968000000000L // Constant representing 1970-01-01
    
    let private ticksPerSecond = 10000000L

    let private ticksPerMillisecond = 10000L

    let private ticksPerMinute = Numerics.multiply 60L ticksPerSecond

    let private ticksPerHour = multiply 60L ticksPerMinute

    let private ticksPerDay = multiply 24L ticksPerHour

    let private daysPerYear = 365L

    let private daysPerLeapYear = 366L

    let private daysInMonth = [|31; 28; 31; 30; 31; 30; 31; 31; 30; 31; 30; 31|]

    let private daysInMonthLeapYear = [|31; 29; 31; 30; 31; 30; 31; 31; 30; 31; 30; 31|]
    
    /// <summary>
    /// Represents a date and time structure
    /// </summary>
    [<Struct>]
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
        Alloy.Numerics.add unixEpochTicks (multiply 1670000000L ticksPerSecond)
    
    /// <summary>
    /// Gets the current Unix timestamp (seconds since 1970-01-01)
    /// </summary>
    /// <returns>The current Unix timestamp</returns>
    let currentUnixTimestamp (): int64 =
        let ticks = currentTicks()
        divide (subtract ticks unixEpochTicks) ticksPerSecond
    
    /// <summary>
    /// Converts Unix timestamp to ticks
    /// </summary>
    /// <param name="timestamp">The Unix timestamp in seconds</param>
    /// <returns>The timestamp in ticks</returns>
    let unixTimestampToTicks (timestamp: int64): int64 =
        add unixEpochTicks (multiply timestamp ticksPerSecond)
    
    /// <summary>
    /// Converts ticks to Unix timestamp
    /// </summary>
    /// <param name="ticks">The timestamp in ticks</param>
    /// <returns>The Unix timestamp in seconds</returns>
    let ticksToUnixTimestamp (ticks: int64): int64 =
        divide (subtract ticks unixEpochTicks) ticksPerSecond
    
    /// <summary>
    /// Converts a Unix timestamp to a DateTime structure
    /// </summary>
    /// <param name="timestamp">The Unix timestamp in seconds</param>
    /// <returns>A DateTime structure representing the timestamp</returns>
    let unixTimestampToDateTime (timestamp: int64): DateTime =
        let mutable remainingSecs = timestamp
        let days = divide remainingSecs 86400L // seconds per day
        remainingSecs <- remainingSecs % 86400L

        let hours = int (divide remainingSecs 3600L)
        remainingSecs <- remainingSecs % 3600L
        
        let minutes = int (divide remainingSecs 60L)
        remainingSecs <- remainingSecs % 60L
        
        let seconds = int remainingSecs

        let mutable year = 1970
        let mutable remainingDays = days

        while greaterThanOrEqual remainingDays (if isLeapYear year then daysPerLeapYear else daysPerYear) do
            let daysInYear = if isLeapYear year then daysPerLeapYear else daysPerYear
            remainingDays <- subtract remainingDays daysInYear
            year <- add year 1

        let monthDays = if isLeapYear year then daysInMonthLeapYear else daysInMonth
        
        let mutable month = 0
        while lessThan month 12 && greaterThanOrEqual remainingDays (int64 monthDays.[month]) do
            remainingDays <- subtract remainingDays (int64 monthDays.[month])
            month <- add month 1
        
        {
            Year = year
            Month = add month 1
            Day = add (int remainingDays) 1
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
            days <- add days (if isLeapYear y then daysPerLeapYear else daysPerYear)
        
        // Add days for months in current year
        let monthDays = if isLeapYear dateTime.Year then daysInMonthLeapYear else daysInMonth
        
        for m = 0 to dateTime.Month - 2 do
            days <- add days (int64 monthDays.[m])
        
        // Add days in current month
        days <- add days (int64 (subtract dateTime.Day 1))
        
        // Calculate total seconds
        let totalSeconds = 
            add (multiply days 86400L)
                (add (multiply (int64 dateTime.Hour) 3600L)
                     (add (multiply (int64 dateTime.Minute) 60L)
                          (int64 dateTime.Second)))
        
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
        if lessThan year 1 || greaterThan year 9999 then
            failwith (concat "Year out of range: " (string year))
        
        if lessThan month 1 || greaterThan month 12 then
            failwith (concat "Month out of range: " (string month))
        
        let daysInCurrentMonth = 
            if isLeapYear year then
                daysInMonthLeapYear.[subtract month 1]
            else
                daysInMonth.[subtract month 1]
                
        if lessThan day 1 || greaterThan day daysInCurrentMonth then
            failwith (concat "Day out of range: " (string day))
        
        if lessThan hour 0 || greaterThan hour 23 then
            failwith (concat "Hour out of range: " (string hour))
        
        if lessThan minute 0 || greaterThan minute 59 then
            failwith (concat "Minute out of range: " (string minute))
        
        if lessThan second 0 || greaterThan second 59 then
            failwith (concat "Second out of range: " (string second))
        
        if lessThan millisecond 0 || greaterThan millisecond 999 then
            failwith (concat "Millisecond out of range: " (string millisecond))
        
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
        let yearStr = if dateTime.Year < 1000 then 
                        String.padLeft '0' 4 (string dateTime.Year)
                      else string dateTime.Year
        let monthStr = String.padLeft '0' 2 (string dateTime.Month)
        let dayStr = String.padLeft '0' 2 (string dateTime.Day)
        let hourStr = String.padLeft '0' 2 (string dateTime.Hour)
        let minuteStr = String.padLeft '0' 2 (string dateTime.Minute)
        let secondStr = String.padLeft '0' 2 (string dateTime.Second)
        
        concat (concat (concat (concat (concat (concat yearStr "-") monthStr) "-") dayStr) " ")
               (concat (concat (concat hourStr ":") minuteStr) (concat ":" secondStr))
    
    /// <summary>
    /// Represents a time span structure
    /// </summary>
    [<Struct>]
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
        let totalSeconds = divide totalMilliseconds 1000L
        
        let s = totalSeconds % 60L
        let totalMinutes = divide totalSeconds 60L
        
        let m = totalMinutes % 60L
        let totalHours = divide totalMinutes 60L
        
        let h = totalHours % 24L
        let d = divide totalHours 24L
        
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
        add (int64 timeSpan.Milliseconds)
            (add (multiply (int64 timeSpan.Seconds) 1000L)
                 (add (multiply (int64 timeSpan.Minutes) 60000L)
                      (add (multiply (int64 timeSpan.Hours) 3600000L)
                           (multiply (int64 timeSpan.Days) 86400000L))))
    
    /// <summary>
    /// Adds a time span to a date time
    /// </summary>
    /// <param name="dateTime">The date time</param>
    /// <param name="timeSpan">The time span to add</param>
    /// <returns>A new date time</returns>
    let addTimeSpan (dateTime: DateTime) (timeSpan: TimeSpan): DateTime =
        let timestamp = dateTimeToUnixTimestamp dateTime
        let ms = totalMilliseconds timeSpan
        let newTimestamp = add timestamp (divide ms 1000L)
        
        // Handle milliseconds separately
        let newDateTime = unixTimestampToDateTime newTimestamp
        { newDateTime with Millisecond = add newDateTime.Millisecond (int (ms % 1000L)) }
    
    /// <summary>
    /// Subtracts a time span from a date time
    /// </summary>
    /// <param name="dateTime">The date time</param>
    /// <param name="timeSpan">The time span to subtract</param>
    /// <returns>A new date time</returns>
    let subtractTimeSpan (dateTime: DateTime) (timeSpan: TimeSpan): DateTime =
        let timestamp = dateTimeToUnixTimestamp dateTime
        let ms = totalMilliseconds timeSpan
        let newTimestamp = subtract timestamp (divide ms 1000L)
        
        // Handle milliseconds separately
        let newDateTime = unixTimestampToDateTime newTimestamp
        let newMillisecond = subtract newDateTime.Millisecond (int (ms % 1000L))
        
        if lessThan newMillisecond 0 then
            { newDateTime with Millisecond = add 1000 newMillisecond }
        else
            { newDateTime with Millisecond = newMillisecond }
            
    /// <summary>
    /// Writes a DateTime to a byte array
    /// </summary>
    /// <param name="dateTime">The DateTime to write</param>
    /// <param name="buffer">The destination buffer</param>
    /// <param name="startIndex">The starting index</param>
    /// <returns>The new index after writing</returns>
    let writeDateTimeToBytes (dateTime: DateTime) (buffer: byte[]) (startIndex: int): int =
        let timestamp = dateTimeToUnixTimestamp dateTime
        let mutable currentIndex = startIndex
        
        // Write 8-byte timestamp
        for i = 0 to 7 do
            let shiftedValue = timestamp >>> (multiply i 8)
            let maskedValue = shiftedValue &&& 0xFFL
            buffer.[currentIndex] <- byte maskedValue
            currentIndex <- add currentIndex 1
            
        currentIndex
    
    /// <summary>
    /// Reads a DateTime from a byte array
    /// </summary>
    /// <param name="buffer">The source buffer</param>
    /// <param name="startIndex">The starting index</param>
    /// <returns>The DateTime and the new index</returns>
    let readDateTimeFromBytes (buffer: byte[]) (startIndex: int): DateTime * int =
        let mutable timestamp = 0L
        let mutable currentIndex = startIndex
        
        for i = 0 to 7 do
            let byteValue = int64 buffer.[currentIndex]
            let shiftedValue = byteValue <<< (multiply i 8)
            timestamp <- timestamp ||| shiftedValue
            currentIndex <- add currentIndex 1
        
        unixTimestampToDateTime timestamp, currentIndex