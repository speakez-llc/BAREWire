module BAREWire.Tests.Core

open Expecto
open BAREWire.Core
open BAREWire.Core.Time

[<Tests>]
let timeTests =
    testList "Time Module Tests" [
        testCase "isLeapYear correctly identifies leap years" <| fun _ ->
            // Non-leap years
            Expect.isFalse (isLeapYear 1700) "1700 is not a leap year (divisible by 100 but not 400)"
            Expect.isFalse (isLeapYear 1800) "1800 is not a leap year (divisible by 100 but not 400)"
            Expect.isFalse (isLeapYear 1900) "1900 is not a leap year (divisible by 100 but not 400)"
            Expect.isFalse (isLeapYear 2001) "2001 is not a leap year (not divisible by 4)"
            Expect.isFalse (isLeapYear 2002) "2002 is not a leap year (not divisible by 4)"
            Expect.isFalse (isLeapYear 2003) "2003 is not a leap year (not divisible by 4)"
            
            // Leap years
            Expect.isTrue (isLeapYear 1600) "1600 is a leap year (divisible by 400)"
            Expect.isTrue (isLeapYear 2000) "2000 is a leap year (divisible by 400)"
            Expect.isTrue (isLeapYear 2004) "2004 is a leap year (divisible by 4 but not 100)"
            Expect.isTrue (isLeapYear 2008) "2008 is a leap year (divisible by 4 but not 100)"
            Expect.isTrue (isLeapYear 2012) "2012 is a leap year (divisible by 4 but not 100)"
            Expect.isTrue (isLeapYear 2016) "2016 is a leap year (divisible by 4 but not 100)"
            Expect.isTrue (isLeapYear 2020) "2020 is a leap year (divisible by 4 but not 100)"
            Expect.isTrue (isLeapYear 2024) "2024 is a leap year (divisible by 4 but not 100)"
            
        testCase "unixTimestampToDateTime and dateTimeToUnixTimestamp are inverse operations" <| fun _ ->
            // Test with various timestamps
            let testTimestamps = [
                0L                  // 1970-01-01 00:00:00 (Unix epoch)
                1000000000L         // 2001-09-09 01:46:40
                1577836800L         // 2020-01-01 00:00:00
                1609459199L         // 2020-12-31 23:59:59
                1735689600L         // 2025-01-01 00:00:00
            ]
            
            for timestamp in testTimestamps do
                let dateTime = unixTimestampToDateTime timestamp
                let roundTrip = dateTimeToUnixTimestamp dateTime
                
                Expect.equal roundTrip timestamp "DateTime conversion should round-trip correctly"
                
        testCase "createDateTime validates input parameters" <| fun _ ->
            // Valid cases
            Expect.doesNotThrow (fun () -> createDateTime 2020 1 1 0 0 0 0 |> ignore) 
                "Valid date should not throw"
                
            Expect.doesNotThrow (fun () -> createDateTime 2020 2 29 23 59 59 999 |> ignore) 
                "Valid leap day should not throw"
                
            // Invalid cases
            Expect.throws (fun () -> createDateTime 0 1 1 0 0 0 0 |> ignore) 
                "Year 0 should throw"
                
            Expect.throws (fun () -> createDateTime 2020 0 1 0 0 0 0 |> ignore) 
                "Month 0 should throw"
                
            Expect.throws (fun () -> createDateTime 2020 13 1 0 0 0 0 |> ignore) 
                "Month 13 should throw"
                
            Expect.throws (fun () -> createDateTime 2020 1 0 0 0 0 0 |> ignore) 
                "Day 0 should throw"
                
            Expect.throws (fun () -> createDateTime 2020 1 32 0 0 0 0 |> ignore) 
                "Day 32 in January should throw"
                
            Expect.throws (fun () -> createDateTime 2020 4 31 0 0 0 0 |> ignore) 
                "Day 31 in April should throw"
                
            Expect.throws (fun () -> createDateTime 2020 2 30 0 0 0 0 |> ignore) 
                "Day 30 in February 2020 (leap year) should throw"
                
            Expect.throws (fun () -> createDateTime 2019 2 29 0 0 0 0 |> ignore) 
                "Day 29 in February 2019 (non-leap year) should throw"
                
            Expect.throws (fun () -> createDateTime 2020 1 1 24 0 0 0 |> ignore) 
                "Hour 24 should throw"
                
            Expect.throws (fun () -> createDateTime 2020 1 1 0 60 0 0 |> ignore) 
                "Minute 60 should throw"
                
            Expect.throws (fun () -> createDateTime 2020 1 1 0 0 60 0 |> ignore) 
                "Second 60 should throw"
                
            Expect.throws (fun () -> createDateTime 2020 1 1 0 0 0 1000 |> ignore) 
                "Millisecond 1000 should throw"
                
        testCase "now returns a valid DateTime" <| fun _ ->
            let dateTime = now()
            
            // Basic validation
            Expect.isGreaterThan dateTime.Year 1970 "Year should be after 1970"
            Expect.isGreaterThan dateTime.Year 2000 "Year should be after 2000"
            Expect.isLessThan dateTime.Year 3000 "Year should be before 3000"
            
            Expect.isTrue (dateTime.Month >= 1 && dateTime.Month <= 12) "Month should be valid"
            Expect.isTrue (dateTime.Day >= 1 && dateTime.Day <= 31) "Day should be valid"
            Expect.isTrue (dateTime.Hour >= 0 && dateTime.Hour <= 23) "Hour should be valid"
            Expect.isTrue (dateTime.Minute >= 0 && dateTime.Minute <= 59) "Minute should be valid"
            Expect.isTrue (dateTime.Second >= 0 && dateTime.Second <= 59) "Second should be valid"
            
        testCase "toString formats DateTime correctly" <| fun _ ->
            let dateTime = {
                Year = 2020
                Month = 5
                Day = 15
                Hour = 8
                Minute = 30
                Second = 45
                Millisecond = 123
            }
            
            let formatted = toString dateTime
            Expect.equal formatted "2020-05-15 08:30:45" "DateTime should be formatted correctly"
            
        testCase "fromMilliseconds creates correct TimeSpan" <| fun _ ->
            let inputs = [
                0L
                1L
                1000L
                60000L
                3600000L
                86400000L
                90061001L  // 1 day, 1 hour, 1 minute, 1 second, 1 millisecond
            ]
            
            for ms in inputs do
                let timeSpan = fromMilliseconds ms
                let roundTrip = totalMilliseconds timeSpan
                
                Expect.equal roundTrip ms "TimeSpan conversion should round-trip correctly"
                
        testCase "createTimeSpan creates TimeSpan with correct properties" <| fun _ ->
            let timeSpan = createTimeSpan 1 2 3 4 5
            
            Expect.equal timeSpan.Days 1 "Days should match"
            Expect.equal timeSpan.Hours 2 "Hours should match"
            Expect.equal timeSpan.Minutes 3 "Minutes should match"
            Expect.equal timeSpan.Seconds 4 "Seconds should match"
            Expect.equal timeSpan.Milliseconds 5 "Milliseconds should match"
            
            let totalMs = totalMilliseconds timeSpan
            let expected = 
                (1L * 86400000L) +    // Days
                (2L * 3600000L) +     // Hours
                (3L * 60000L) +       // Minutes
                (4L * 1000L) +        // Seconds
                5L                    // Milliseconds
                
            Expect.equal totalMs expected "Total milliseconds should be calculated correctly"
            
        testCase "addTimeSpan correctly adds time" <| fun _ ->
            let dateTime = {
                Year = 2020
                Month = 12
                Day = 31
                Hour = 23
                Minute = 59
                Second = 59
                Millisecond = 0
            }
            
            let timeSpan = createTimeSpan 1 0 0 1 0  // 1 day, 1 second
            
            let result = addTimeSpan dateTime timeSpan
            
            Expect.equal result.Year 2021 "Year should roll over"
            Expect.equal result.Month 1 "Month should roll over"
            Expect.equal result.Day 1 "Day should roll over"
            Expect.equal result.Hour 0 "Hour should roll over"
            Expect.equal result.Minute 0 "Minute should roll over"
            Expect.equal result.Second 0 "Second should roll over"
            
        testCase "subtractTimeSpan correctly subtracts time" <| fun _ ->
            let dateTime = {
                Year = 2020
                Month = 1
                Day = 1
                Hour = 0
                Minute = 0
                Second = 0
                Millisecond = 0
            }
            
            let timeSpan = createTimeSpan 1 0 0 0 0  // 1 day
            
            let result = subtractTimeSpan dateTime timeSpan
            
            Expect.equal result.Year 2019 "Year should roll back"
            Expect.equal result.Month 12 "Month should roll back"
            Expect.equal result.Day 31 "Day should roll back"
            
        testCase "unixTimestampToDateTime handles specific dates correctly" <| fun _ ->
            // Test specific dates of interest
            let tests = [
                // timestamp, expected year, month, day, hour, minute, second
                (0L, 1970, 1, 1, 0, 0, 0)  // Unix epoch
                (1L, 1970, 1, 1, 0, 0, 1)  // 1 second after epoch
                (60L, 1970, 1, 1, 0, 1, 0)  // 1 minute after epoch
                (3600L, 1970, 1, 1, 1, 0, 0)  // 1 hour after epoch
                (86400L, 1970, 1, 2, 0, 0, 0)  // 1 day after epoch
                (946684800L, 2000, 1, 1, 0, 0, 0)  // Y2K
                (1577836800L, 2020, 1, 1, 0, 0, 0)  // 2020-01-01
                (1609459199L, 2020, 12, 31, 23, 59, 59)  // Last second of 2020
            ]
            
            for (timestamp, expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, expectedSecond) in tests do
                let dateTime = unixTimestampToDateTime timestamp
                
                Expect.equal dateTime.Year expectedYear $"Year should be {expectedYear}"
                Expect.equal dateTime.Month expectedMonth $"Month should be {expectedMonth}"
                Expect.equal dateTime.Day expectedDay $"Day should be {expectedDay}"
                Expect.equal dateTime.Hour expectedHour $"Hour should be {expectedHour}"
                Expect.equal dateTime.Minute expectedMinute $"Minute should be {expectedMinute}"
                Expect.equal dateTime.Second expectedSecond $"Second should be {expectedSecond}"
    ]