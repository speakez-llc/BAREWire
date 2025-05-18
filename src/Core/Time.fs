namespace BAREWire.Core

open BAREWire.Core.Time.Integration
open Alloy

/// <summary>
/// Pure F# time utilities with no System.DateTime dependencies
/// </summary>
module Time =
    // Private backing fields
    let private unixEpochTicks = 621355968000000000L // Constant representing 1970-01-01
    let private ticksPerSecond = 10000000L
    let private ticksPerMillisecond = 10000L
    let private ticksPerMinute = 60L * ticksPerSecond
    let private ticksPerHour = 60L * ticksPerMinute
    let private ticksPerDay = 24L * ticksPerHour
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
        getCurrentTicks()
    
    /// <summary>
    /// Gets the current Unix timestamp (seconds since 1970-01-01)
    /// </summary>
    /// <returns>The current Unix timestamp</returns>
    let currentUnixTimestamp (): int64 =
        let ticks = currentTicks()
        (ticks - unixEpochTicks) / ticksPerSecond
    
    /// <summary>
    /// Gets high-resolution performance counter ticks
    /// </summary>
    /// <returns>The current high-resolution ticks</returns>
    let highResolutionTicks (): int64 =
        getHighResolutionTicks()
        
    /// <summary>
    /// Gets the frequency of the high-resolution performance counter
    /// </summary>
    /// <returns>The frequency in ticks per second</returns>
    let tickFrequency (): int64 =
        getTickFrequency()
        
    /// <summary>
    /// Calculates elapsed time between two high-resolution tick values
    /// </summary>
    /// <param name="startTicks">The starting tick count</param>
    /// <param name="endTicks">The ending tick count</param>
    /// <returns>A TimeSpan representing the elapsed time</returns>
    let elapsedTime (startTicks: int64) (endTicks: int64): TimeSpan =
        let elapsedTicks = Numerics.subtract endTicks startTicks
        let freq = tickFrequency()
        
        // Convert to milliseconds
        let totalMilliseconds = Numerics.divide (Numerics.multiply elapsedTicks 1000L) freq
        
        // Calculate components
        let totalSeconds = Numerics.divide totalMilliseconds 1000L
        let ms = int (totalMilliseconds % 1000L)
        
        let totalMinutes = Numerics.divide totalSeconds 60L
        let s = int (totalSeconds % 60L)
        
        let totalHours = Numerics.divide totalMinutes 60L
        let m = int (totalMinutes % 60L)
        
        let h = int (totalHours % 24L)
        let d = int (Numerics.divide totalHours 24L)
        
        { Days = d; Hours = h; Minutes = m; Seconds = s; Milliseconds = ms }
    
    /// <summary>
    /// Converts Unix timestamp to ticks
    /// </summary>
    /// <param name="timestamp">The Unix timestamp in seconds</param>
    /// <returns>The timestamp in ticks</returns>
    let unixTimestampToTicks (timestamp: int64): int64 =
        Numerics.add unixEpochTicks (Numerics.multiply timestamp ticksPerSecond)
    
    /// <summary>
    /// Converts ticks to Unix timestamp
    /// </summary>
    /// <param name="ticks">The timestamp in ticks</param>
    /// <returns>The Unix timestamp in seconds</returns>
    let ticksToUnixTimestamp (ticks: int64): int64 =
        Numerics.divide (Numerics.subtract ticks unixEpochTicks) ticksPerSecond
    
    /// <summary>
    /// Converts a Unix timestamp to a DateTime structure
    /// </summary>
    /// <param name="timestamp">The Unix timestamp in seconds</param>
    /// <returns>A DateTime structure representing the timestamp</returns>
    let unixTimestampToDateTime (timestamp: int64): DateTime =
        let mutable remainingSecs = timestamp
        let days = Numerics.divide remainingSecs 86400L // seconds per day
        remainingSecs <- Numerics.modulo remainingSecs 86400L

        let hours = int (Numerics.divide remainingSecs 3600L)
        remainingSecs <- Numerics.modulo remainingSecs 3600L
        
        let minutes = int (Numerics.divide remainingSecs 60L)
        remainingSecs <- Numerics.modulo remainingSecs 60L
        
        let seconds = int remainingSecs

        let mutable year = 1970
        let mutable remainingDays = days

        while Numerics.greaterThanOrEqual remainingDays (if isLeapYear year then daysPerLeapYear else daysPerYear) do
            let daysInYear = if isLeapYear year then daysPerLeapYear else daysPerYear
            remainingDays <- Numerics.subtract remainingDays daysInYear
            year <- Numerics.add year 1

        let monthDays = if isLeapYear year then daysInMonthLeapYear else daysInMonth
        
        let mutable month = 0
        while Numerics.lessThan month 12 && Numerics.greaterThanOrEqual remainingDays (int64 monthDays.[month]) do
            remainingDays <- Numerics.subtract remainingDays (int64 monthDays.[month])
            month <- Numerics.add month 1
        
        {
            Year = year
            Month = Numerics.add month 1
            Day = Numerics.add (int remainingDays) 1
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
            days <- Numerics.add days (if isLeapYear y then daysPerLeapYear else daysPerYear)
        
        // Add days for months in current year
        let monthDays = if isLeapYear dateTime.Year then daysInMonthLeapYear else daysInMonth
        
        for m = 0 to dateTime.Month - 2 do
            days <- Numerics.add days (int64 monthDays.[m])
        
        // Add days in current month
        days <- Numerics.add days (int64 (Numerics.subtract dateTime.Day 1))
        
        // Calculate total seconds
        let totalSeconds = 
            Numerics.add (Numerics.multiply days 86400L)
                (Numerics.add (Numerics.multiply (int64 dateTime.Hour) 3600L)
                     (Numerics.add (Numerics.multiply (int64 dateTime.Minute) 60L)
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
        if Numerics.lessThan year 1 || Numerics.greaterThan year 9999 then
            failwith (String.concat "" ["Year out of range: "; string year])
        
        if Numerics.lessThan month 1 || Numerics.greaterThan month 12 then
            failwith (String.concat "" ["Month out of range: "; string month])
        
        let daysInCurrentMonth = 
            if isLeapYear year then
                daysInMonthLeapYear.[Numerics.subtract month 1]
            else
                daysInMonth.[Numerics.subtract month 1]
                
        if Numerics.lessThan day 1 || Numerics.greaterThan day daysInCurrentMonth then
            failwith (String.concat "" ["Day out of range: "; string day])
        
        if Numerics.lessThan hour 0 || Numerics.greaterThan hour 23 then
            failwith (String.concat "" ["Hour out of range: "; string hour])
        
        if Numerics.lessThan minute 0 || Numerics.greaterThan minute 59 then
            failwith (String.concat "" ["Minute out of range: "; string minute])
        
        if Numerics.lessThan second 0 || Numerics.greaterThan second 59 then
            failwith (String.concat "" ["Second out of range: "; string second])
        
        if Numerics.lessThan millisecond 0 || Numerics.greaterThan millisecond 999 then
            failwith (String.concat "" ["Millisecond out of range: "; string millisecond])
        
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
    /// Gets the current UTC time as a DateTime structure
    /// </summary>
    /// <returns>The current UTC time</returns>
    let utcNow (): DateTime =
        let ticks = getUtcNow()
        let timestamp = (ticks - unixEpochTicks) / ticksPerSecond
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
        
        String.concat "" [
            yearStr; "-"; monthStr; "-"; dayStr; " ";
            hourStr; ":"; minuteStr; ":"; secondStr
        ]
    
    /// <summary>
    /// Creates a TimeSpan structure from total milliseconds
    /// </summary>
    /// <param name="totalMilliseconds">Total milliseconds</param>
    /// <returns>A new TimeSpan structure</returns>
    let fromMilliseconds (totalMilliseconds: int64): TimeSpan =
        let ms = Numerics.modulo totalMilliseconds 1000L
        let totalSeconds = Numerics.divide totalMilliseconds 1000L
        
        let s = Numerics.modulo totalSeconds 60L
        let totalMinutes = Numerics.divide totalSeconds 60L
        
        let m = Numerics.modulo totalMinutes 60L
        let totalHours = Numerics.divide totalMinutes 60L
        
        let h = Numerics.modulo totalHours 24L
        let d = Numerics.divide totalHours 24L
        
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
        Numerics.add (int64 timeSpan.Milliseconds)
            (Numerics.add (Numerics.multiply (int64 timeSpan.Seconds) 1000L)
                 (Numerics.add (Numerics.multiply (int64 timeSpan.Minutes) 60000L)
                      (Numerics.add (Numerics.multiply (int64 timeSpan.Hours) 3600000L)
                           (Numerics.multiply (int64 timeSpan.Days) 86400000L))))
    
    /// <summary>
    /// Adds a time span to a date time
    /// </summary>
    /// <param name="dateTime">The date time</param>
    /// <param name="timeSpan">The time span to add</param>
    /// <returns>A new date time</returns>
    let addTimeSpan (dateTime: DateTime) (timeSpan: TimeSpan): DateTime =
        let timestamp = dateTimeToUnixTimestamp dateTime
        let ms = totalMilliseconds timeSpan
        let newTimestamp = Numerics.add timestamp (Numerics.divide ms 1000L)
        
        // Handle milliseconds separately
        let newDateTime = unixTimestampToDateTime newTimestamp
        { newDateTime with Millisecond = Numerics.add newDateTime.Millisecond (int (Numerics.modulo ms 1000L)) }
    
    /// <summary>
    /// Subtracts a time span from a date time
    /// </summary>
    /// <param name="dateTime">The date time</param>
    /// <param name="timeSpan">The time span to subtract</param>
    /// <returns>A new date time</returns>
    let subtractTimeSpan (dateTime: DateTime) (timeSpan: TimeSpan): DateTime =
        let timestamp = dateTimeToUnixTimestamp dateTime
        let ms = totalMilliseconds timeSpan
        let newTimestamp = Numerics.subtract timestamp (Numerics.divide ms 1000L)
        
        // Handle milliseconds separately
        let newDateTime = unixTimestampToDateTime newTimestamp
        let newMillisecond = Numerics.subtract newDateTime.Millisecond (int (Numerics.modulo ms 1000L))
        
        if Numerics.lessThan newMillisecond 0 then
            { newDateTime with Millisecond = Numerics.add 1000 newMillisecond }
        else
            { newDateTime with Millisecond = newMillisecond }
            
    /// <summary>
    /// Sleeps for the specified number of milliseconds
    /// </summary>
    /// <param name="milliseconds">The number of milliseconds to sleep</param>
    let sleep (milliseconds: int): unit =
        sleep milliseconds
    
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
            let shiftedValue = timestamp >>> (Numerics.multiply i 8)
            let maskedValue = shiftedValue &&& 0xFFL
            buffer.[currentIndex] <- byte maskedValue
            currentIndex <- Numerics.add currentIndex 1
            
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
            let shiftedValue = byteValue <<< (Numerics.multiply i 8)
            timestamp <- timestamp ||| shiftedValue
            currentIndex <- Numerics.add currentIndex 1
        
        unixTimestampToDateTime timestamp, currentIndex
        
    /// <summary>
    /// Gets the days in the month for the specified year and month
    /// </summary>
    /// <param name="year">The year</param>
    /// <param name="month">The month (1-12)</param>
    /// <returns>The number of days in the month</returns>
    let daysInMonth (year: int) (month: int): int =
        if Numerics.lessThan month 1 || Numerics.greaterThan month 12 then
            failwith (String.concat "" ["Month out of range: "; string month])
            
        if isLeapYear year then
            daysInMonthLeapYear.[Numerics.subtract month 1]
        else
            daysInMonth.[Numerics.subtract month 1]
            
    /// <summary>
    /// Compares two DateTimes
    /// </summary>
    /// <param name="dt1">The first DateTime</param>
    /// <param name="dt2">The second DateTime</param>
    /// <returns>
    /// A negative value if dt1 is earlier than dt2; 
    /// zero if dt1 equals dt2;
    /// a positive value if dt1 is later than dt2
    /// </returns>
    let compare (dt1: DateTime) (dt2: DateTime): int =
        let ts1 = dateTimeToUnixTimestamp dt1
        let ts2 = dateTimeToUnixTimestamp dt2
        
        if ts1 < ts2 then -1
        elif ts1 > ts2 then 1
        else
            // Compare milliseconds
            Numerics.compare dt1.Millisecond dt2.Millisecond