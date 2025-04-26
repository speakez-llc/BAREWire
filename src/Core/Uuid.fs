namespace BAREWire.Core

open BAREWire.Platform

/// <summary>
/// Pure F# UUID implementation (RFC 4122)
/// </summary>
module Uuid =
    /// <summary>
    /// A 128-bit UUID
    /// </summary>
    type Uuid = { Data: byte[] }  // 16 bytes
    
    /// <summary>
    /// Creates a new UUID (v4 random)
    /// </summary>
    /// <returns>A new random UUID</returns>
    let newUuid () : Uuid =
        // Get platform-specific random provider
        let provider = PlatformServices.getRandomProvider() |> Result.get
        
        let bytes = Array.zeroCreate 16
        provider.GetBytes(bytes)
        
        // Set version to 4 (random)
        bytes.[7] <- bytes.[7] &&& 0x0Fuy ||| 0x40uy
        // Set variant to RFC 4122
        bytes.[8] <- bytes.[8] &&& 0x3Fuy ||| 0x80uy
        
        { Data = bytes }
    
    /// <summary>
    /// Converts UUID to string representation
    /// </summary>
    /// <param name="uuid">The UUID to convert</param>
    /// <returns>A string representation in the format "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"</returns>
    let toString (uuid: Uuid) : string =
        let byteToHex b = 
            let chars = [|'0';'1';'2';'3';'4';'5';'6';'7';'8';'9';'a';'b';'c';'d';'e';'f'|]
            let hi = int (b >>> 4)
            let lo = int (b &&& 0xFuy)
            [| chars.[hi]; chars.[lo] |]
            
        let hex = Array.collect byteToHex uuid.Data
        let result = Array.zeroCreate 36
        
        // Format: 8-4-4-4-12
        let mutable pos = 0
        let sections = [|0..7; 9..12; 14..17; 19..22; 24..35|]
        
        for section in sections do
            for i in section do
                if i < result.Length then
                    if pos < hex.Length then
                        result.[i] <- hex.[pos]
                        pos <- pos + 1
            
        // Add hyphens
        result.[8] <- '-'
        result.[13] <- '-'
        result.[18] <- '-'
        result.[23] <- '-'
        
        System.String(result)
    
    /// <summary>
    /// Parse a UUID from string
    /// </summary>
    /// <param name="s">The string to parse</param>
    /// <returns>The parsed UUID</returns>
    /// <exception cref="System.Exception">Thrown when the string is not a valid UUID</exception>
    let fromString (s: string) : Uuid =
        if s.Length <> 36 then failwith "Invalid UUID format"
        
        let bytes = Array.zeroCreate 16
        let mutable byteIndex = 0
        let mutable charIndex = 0
        
        // Skip hyphens and parse hex bytes
        while byteIndex < 16 do
            if charIndex = 8 || charIndex = 13 || charIndex = 18 || charIndex = 23 then
                // Skip hyphen
                charIndex <- charIndex + 1
            else
                // Parse hex byte (2 characters)
                let hexChar c =
                    match c with
                    | c when c >= '0' && c <= '9' -> int c - int '0'
                    | c when c >= 'a' && c <= 'f' -> int c - int 'a' + 10
                    | c when c >= 'A' && c <= 'F' -> int c - int 'A' + 10
                    | _ -> failwith "Invalid hex character in UUID"
                
                let hi = hexChar s.[charIndex]
                let lo = hexChar s.[charIndex + 1]
                bytes.[byteIndex] <- byte ((hi <<< 4) ||| lo)
                
                byteIndex <- byteIndex + 1
                charIndex <- charIndex + 2
                
        { Data = bytes }
        
    /// <summary>
    /// Creates a nil UUID (all zeros)
    /// </summary>
    /// <returns>A nil UUID</returns>
    let nil : Uuid = 
        { Data = Array.zeroCreate 16 }
        
    /// <summary>
    /// Compares two UUIDs for equality
    /// </summary>
    /// <param name="uuid1">The first UUID</param>
    /// <param name="uuid2">The second UUID</param>
    /// <returns>True if the UUIDs are equal, false otherwise</returns>
    let equals (uuid1: Uuid) (uuid2: Uuid) : bool =
        let mutable result = true
        let mutable i = 0
        while result && i < 16 do
            if uuid1.Data.[i] <> uuid2.Data.[i] then
                result <- false
            i <- i + 1
        result
        
    /// <summary>
    /// Converts a UUID to a byte array
    /// </summary>
    /// <param name="uuid">The UUID to convert</param>
    /// <returns>A byte array representation</returns>
    let toByteArray (uuid: Uuid) : byte[] =
        Array.copy uuid.Data
        
    /// <summary>
    /// Creates a UUID from a byte array
    /// </summary>
    /// <param name="bytes">The byte array (must be 16 bytes)</param>
    /// <returns>A UUID created from the byte array</returns>
    /// <exception cref="System.Exception">Thrown when the byte array is not 16 bytes</exception>
    let fromByteArray (bytes: byte[]) : Uuid =
        if bytes.Length <> 16 then
            failwith "UUID byte array must be exactly 16 bytes"
        { Data = Array.copy bytes }