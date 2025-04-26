module BAREWire.Tests.Core

open Expecto
open BAREWire.Core
open BAREWire.Core.Utf8
open System.Text

[<Tests>]
let utf8Tests =
    testList "Utf8 Module Tests" [
        testCase "getBytes with ASCII characters" <| fun _ ->
            let input = "Hello, world!"
            let expected = Encoding.UTF8.GetBytes(input)
            let actual = getBytes input
            
            Expect.sequenceEqual actual expected "ASCII encoding should match System.Text.Encoding.UTF8"
            Expect.equal actual.Length input.Length "ASCII characters should be 1 byte each"
            
        testCase "getString with ASCII characters" <| fun _ ->
            let original = "Hello, world!"
            let bytes = getBytes original
            let roundTrip = getString bytes
            
            Expect.equal roundTrip original "ASCII decoding should round-trip correctly"
            
        testCase "getBytes with Latin-1 characters" <| fun _ ->
            let input = "Café"  // Contains Latin-1 character 'é'
            let expected = Encoding.UTF8.GetBytes(input)
            let actual = getBytes input
            
            Expect.sequenceEqual actual expected "Latin-1 encoding should match System.Text.Encoding.UTF8"
            Expect.equal actual.Length 5 "Latin-1 non-ASCII character should be 2 bytes"
            
        testCase "getString with Latin-1 characters" <| fun _ ->
            let original = "Café"
            let bytes = getBytes original
            let roundTrip = getString bytes
            
            Expect.equal roundTrip original "Latin-1 decoding should round-trip correctly"
            
        testCase "getBytes with CJK characters" <| fun _ ->
            let input = "你好世界"  // "Hello world" in Chinese
            let expected = Encoding.UTF8.GetBytes(input)
            let actual = getBytes input
            
            Expect.sequenceEqual actual expected "CJK encoding should match System.Text.Encoding.UTF8"
            Expect.equal actual.Length 12 "CJK characters should be 3 bytes each"
            
        testCase "getString with CJK characters" <| fun _ ->
            let original = "你好世界"
            let bytes = getBytes original
            let roundTrip = getString bytes
            
            Expect.equal roundTrip original "CJK decoding should round-trip correctly"
            
        testCase "getBytes with emoji characters" <| fun _ ->
            let input = "😀🌍🚀"  // Emoji: smile, earth, rocket
            let expected = Encoding.UTF8.GetBytes(input)
            let actual = getBytes input
            
            Expect.sequenceEqual actual expected "Emoji encoding should match System.Text.Encoding.UTF8"
            // Most emoji are 4 bytes each in UTF-8
            Expect.isGreaterThanOrEqual actual.Length (input.Length * 3) "Emoji characters should be at least 3 bytes each"
            
        testCase "getString with emoji characters" <| fun _ ->
            let original = "😀🌍🚀"
            let bytes = getBytes original
            let roundTrip = getString bytes
            
            Expect.equal roundTrip original "Emoji decoding should round-trip correctly"
            
        testCase "getBytes with mixed character types" <| fun _ ->
            let input = "Hello, 你好, Café, 😀"
            let expected = Encoding.UTF8.GetBytes(input)
            let actual = getBytes input
            
            Expect.sequenceEqual actual expected "Mixed character encoding should match System.Text.Encoding.UTF8"
            
        testCase "getString with mixed character types" <| fun _ ->
            let original = "Hello, 你好, Café, 😀"
            let bytes = getBytes original
            let roundTrip = getString bytes
            
            Expect.equal roundTrip original "Mixed character decoding should round-trip correctly"
            
        testCase "getString handles invalid UTF-8 sequences" <| fun _ ->
            // Invalid sequences: 
            // 0xC0 without continuation byte
            // 0xE0 with only one continuation byte
            // 0xF0 with only two continuation bytes
            let invalidBytes = [|
                0x48uy; 0x65uy; 0x6Cuy; 0x6Cuy; 0x6Fuy;  // "Hello"
                0xC0uy;  // Invalid: missing continuation
                0xE0uy; 0x80uy;  // Invalid: missing second continuation
                0xF0uy; 0x80uy; 0x80uy;  // Invalid: missing third continuation
                0x21uy  // "!"
            |]
            
            let result = getString invalidBytes
            
            // The implementation should skip invalid sequences or replace them
            Expect.isTrue (result.StartsWith("Hello")) "Valid prefix should be preserved"
            Expect.isTrue (result.EndsWith("!")) "Valid suffix should be preserved"
            
        testCase "getBytes and getString are inverse operations for various inputs" <| fun _ ->
            let inputs = [
                ""  // Empty string
                "a"  // Single ASCII character
                "Hello, world!"  // ASCII string
                "Café"  // Latin-1 characters
                "你好世界"  // CJK characters
                "😀🌍🚀"  // Emoji characters
                "Hello, 你好, Café, 😀"  // Mixed characters
                "The quick brown fox jumps over the lazy dog"  // Longer ASCII text
                "πœƒ∑´®†¥¨ˆøπ"  // Mathematical and special symbols
            ]
            
            for input in inputs do
                let bytes = getBytes input
                let roundTrip = getString bytes
                
                Expect.equal roundTrip input $"String '{input}' should round-trip correctly"
                
        testCase "getBytes output matches System.Text.Encoding.UTF8 for various inputs" <| fun _ ->
            let inputs = [
                ""  // Empty string
                "a"  // Single ASCII character
                "Hello, world!"  // ASCII string
                "Café"  // Latin-1 characters
                "你好世界"  // CJK characters
                "😀🌍🚀"  // Emoji characters
                "Hello, 你好, Café, 😀"  // Mixed characters
                "The quick brown fox jumps over the lazy dog"  // Longer ASCII text
                "πœƒ∑´®†¥¨ˆøπ"  // Mathematical and special symbols
            ]
            
            for input in inputs do
                let expected = Encoding.UTF8.GetBytes(input)
                let actual = getBytes input
                
                Expect.sequenceEqual actual expected $"Encoding of '{input}' should match System.Text.Encoding.UTF8"
                
        testCase "getString output matches System.Text.Encoding.UTF8 for various inputs" <| fun _ ->
            let inputs = [
                ""  // Empty string
                "a"  // Single ASCII character
                "Hello, world!"  // ASCII string
                "Café"  // Latin-1 characters
                "你好世界"  // CJK characters
                "😀🌍🚀"  // Emoji characters
                "Hello, 你好, Café, 😀"  // Mixed characters
                "The quick brown fox jumps over the lazy dog"  // Longer ASCII text
                "πœƒ∑´®†¥¨ˆøπ"  // Mathematical and special symbols
            ]
            
            for input in inputs do
                let bytes = Encoding.UTF8.GetBytes(input)
                let expected = input
                let actual = getString bytes
                
                Expect.equal actual expected $"Decoding of '{input}' bytes should match System.Text.Encoding.UTF8"
    ]