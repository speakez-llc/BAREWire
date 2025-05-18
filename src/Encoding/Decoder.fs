namespace BAREWire.Encoding

open Alloy
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Core.Binary
open BAREWire.Core.Utf8

/// <summary>
/// Decoding functions for BARE types using Alloy's zero-cost abstractions
/// </summary>
module Decoder =
    /// <summary>
    /// Reads a uint value using ULEB128 encoding
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <returns>The decoded uint64 value and the new offset</returns>
    let inline readUInt (memory: Memory<'T, 'region>) (offset: int<offset>): uint64 * int<offset> =
        let mutable result = 0UL
        let mutable shift = 0
        let mutable currentOffset = offset
        let mutable currentByte = 0uy
        
        let mutable shouldContinue = true
        while shouldContinue do
            currentByte <- memory.Data.[int (memory.Offset + currentOffset)]
            currentOffset <- currentOffset + 1<offset>
            
            result <- result ||| ((uint64 (currentByte &&& 0x7Fuy)) <<< shift)
            shift <- shift + 7
            shouldContinue <- (currentByte &&& 0x80uy <> 0uy) && (shift < 64)
        
        result, currentOffset
    
    /// <summary>
    /// Reads a uint value using ULEB128 encoding from a span
    /// </summary>
    /// <param name="span">The span to read from</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <returns>The decoded uint64 value</returns>
    let inline readUIntSpan (span: ReadOnlySpan<byte>) (offset: byref<int>): uint64 =
        let mutable result = 0UL
        let mutable shift = 0
        let mutable currentByte = 0uy
        
        let mutable shouldContinue = true
        while shouldContinue && offset < span.Length do
            currentByte <- span.[offset]
            offset <- offset + 1
            
            result <- result ||| ((uint64 (currentByte &&& 0x7Fuy)) <<< shift)
            shift <- shift + 7
            shouldContinue <- (currentByte &&& 0x80uy <> 0uy) && (shift < 64)
        
        result
    
    /// <summary>
    /// Reads an int value using zigzag ULEB128 encoding
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <returns>The decoded int64 value and the new offset</returns>
    let inline readInt (memory: Memory<'T, 'region>) (offset: int<offset>): int64 * int<offset> =
        let uintVal, newOffset = readUInt memory offset
        
        // Zigzag decoding: (n >>> 1) ^ -(n &&& 1)
        let value = (int64 uintVal >>> 1) ^^^ (-(int64 (uintVal &&& 1UL)))
        value, newOffset
    
    /// <summary>
    /// Reads an int value using zigzag ULEB128 encoding from a span
    /// </summary>
    /// <param name="span">The span to read from</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <returns>The decoded int64 value</returns>
    let inline readIntSpan (span: ReadOnlySpan<byte>) (offset: byref<int>): int64 =
        let uintVal = readUIntSpan span &offset
        
        // Zigzag decoding: (n >>> 1) ^ -(n &&& 1)
        (int64 uintVal >>> 1) ^^^ (-(int64 (uintVal &&& 1UL)))
    
    /// <summary>
    /// Reads a u8 (byte) value
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The offset to read at</param>
    /// <returns>The byte value and the new offset</returns>
    let inline readU8 (memory: Memory<'T, 'region>) (offset: int<offset>): byte * int<offset> =
        let value = memory.Data.[int (memory.Offset + offset)]
        value, offset + 1<offset>
    
    /// <summary>
    /// Reads a u8 (byte) value from a span
    /// </summary>
    /// <param name="span">The span to read from</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <returns>The byte value</returns>
    let inline readU8Span (span: ReadOnlySpan<byte>) (offset: byref<int>): byte =
        let value = span.[offset]
        offset <- offset + 1
        value
    
    /// <summary>
    /// Reads a u16 value in little-endian format
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <returns>The uint16 value and the new offset</returns>
    let inline readU16 (memory: Memory<'T, 'region>) (offset: int<offset>): uint16 * int<offset> =
        let span = memory.AsSpan().Slice(int offset, 2)
        let value = spanToUInt16 span
        value, offset + 2<offset>
    
    /// <summary>
    /// Reads a u16 value in little-endian format from a span
    /// </summary>
    /// <param name="span">The span to read from</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <returns>The uint16 value</returns>
    let inline readU16Span (span: ReadOnlySpan<byte>) (offset: byref<int>): uint16 =
        let value = spanToUInt16 (span.Slice(offset, 2))
        offset <- offset + 2
        value
    
    /// <summary>
    /// Reads a u32 value in little-endian format
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <returns>The uint32 value and the new offset</returns>
    let inline readU32 (memory: Memory<'T, 'region>) (offset: int<offset>): uint32 * int<offset> =
        let span = memory.AsSpan().Slice(int offset, 4)
        let value = spanToUInt32 span
        value, offset + 4<offset>
    
    /// <summary>
    /// Reads a u32 value in little-endian format from a span
    /// </summary>
    /// <param name="span">The span to read from</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <returns>The uint32 value</returns>
    let inline readU32Span (span: ReadOnlySpan<byte>) (offset: byref<int>): uint32 =
        let value = spanToUInt32 (span.Slice(offset, 4))
        offset <- offset + 4
        value
    
    /// <summary>
    /// Reads a u64 value in little-endian format
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <returns>The uint64 value and the new offset</returns>
    let inline readU64 (memory: Memory<'T, 'region>) (offset: int<offset>): uint64 * int<offset> =
        let span = memory.AsSpan().Slice(int offset, 8)
        let value = spanToUInt64 span
        value, offset + 8<offset>
    
    /// <summary>
    /// Reads a u64 value in little-endian format from a span
    /// </summary>
    /// <param name="span">The span to read from</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <returns>The uint64 value</returns>
    let inline readU64Span (span: ReadOnlySpan<byte>) (offset: byref<int>): uint64 =
        let value = spanToUInt64 (span.Slice(offset, 8))
        offset <- offset + 8
        value
    
    /// <summary>
    /// Reads an i8 (sbyte) value
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The offset to read at</param>
    /// <returns>The sbyte value and the new offset</returns>
    let inline readI8 (memory: Memory<'T, 'region>) (offset: int<offset>): sbyte * int<offset> =
        let value = sbyte memory.Data.[int (memory.Offset + offset)]
        value, offset + 1<offset>
    
    /// <summary>
    /// Reads an i8 (sbyte) value from a span
    /// </summary>
    /// <param name="span">The span to read from</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <returns>The sbyte value</returns>
    let inline readI8Span (span: ReadOnlySpan<byte>) (offset: byref<int>): sbyte =
        let value = sbyte span.[offset]
        offset <- offset + 1
        value
    
    /// <summary>
    /// Reads an i16 value in little-endian format
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <returns>The int16 value and the new offset</returns>
    let inline readI16 (memory: Memory<'T, 'region>) (offset: int<offset>): int16 * int<offset> =
        let span = memory.AsSpan().Slice(int offset, 2)
        let value = spanToInt16 span
        value, offset + 2<offset>
    
    /// <summary>
    /// Reads an i16 value in little-endian format from a span
    /// </summary>
    /// <param name="span">The span to read from</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <returns>The int16 value</returns>
    let inline readI16Span (span: ReadOnlySpan<byte>) (offset: byref<int>): int16 =
        let value = spanToInt16 (span.Slice(offset, 2))
        offset <- offset + 2
        value
    
    /// <summary>
    /// Reads an i32 value in little-endian format
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <returns>The int32 value and the new offset</returns>
    let inline readI32 (memory: Memory<'T, 'region>) (offset: int<offset>): int32 * int<offset> =
        let span = memory.AsSpan().Slice(int offset, 4)
        let value = spanToInt32 span
        value, offset + 4<offset>
    
    /// <summary>
    /// Reads an i32 value in little-endian format from a span
    /// </summary>
    /// <param name="span">The span to read from</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <returns>The int32 value</returns>
    let inline readI32Span (span: ReadOnlySpan<byte>) (offset: byref<int>): int32 =
        let value = spanToInt32 (span.Slice(offset, 4))
        offset <- offset + 4
        value
    
    /// <summary>
    /// Reads an i64 value in little-endian format
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <returns>The int64 value and the new offset</returns>
    let inline readI64 (memory: Memory<'T, 'region>) (offset: int<offset>): int64 * int<offset> =
        let span = memory.AsSpan().Slice(int offset, 8)
        let value = spanToInt64 span
        value, offset + 8<offset>
    
    /// <summary>
    /// Reads an i64 value in little-endian format from a span
    /// </summary>
    /// <param name="span">The span to read from</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <returns>The int64 value</returns>
    let inline readI64Span (span: ReadOnlySpan<byte>) (offset: byref<int>): int64 =
        let value = spanToInt64 (span.Slice(offset, 8))
        offset <- offset + 8
        value
    
    /// <summary>
    /// Reads an f32 (float32) value
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <returns>The float32 value and the new offset</returns>
    let inline readF32 (memory: Memory<'T, 'region>) (offset: int<offset>): float32 * int<offset> =
        let bits, newOffset = readI32 memory offset
        let value = int32BitsToSingle bits
        value, newOffset
    
    /// <summary>
    /// Reads an f32 (float32) value from a span
    /// </summary>
    /// <param name="span">The span to read from</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <returns>The float32 value</returns>
    let inline readF32Span (span: ReadOnlySpan<byte>) (offset: byref<int>): float32 =
        let bits = readI32Span span &offset
        int32BitsToSingle bits
    
    /// <summary>
    /// Reads an f64 (double) value
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <returns>The double value and the new offset</returns>
    let inline readF64 (memory: Memory<'T, 'region>) (offset: int<offset>): float * int<offset> =
        let bits, newOffset = readI64 memory offset
        let value = int64BitsToDouble bits
        value, newOffset
    
    /// <summary>
    /// Reads an f64 (double) value from a span
    /// </summary>
    /// <param name="span">The span to read from</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <returns>The double value</returns>
    let inline readF64Span (span: ReadOnlySpan<byte>) (offset: byref<int>): float =
        let bits = readI64Span span &offset
        int64BitsToDouble bits
    
    /// <summary>
    /// Reads a boolean value
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The offset to read at</param>
    /// <returns>The boolean value and the new offset</returns>
    /// <exception cref="System.Exception">Thrown when the byte is not 0 or 1</exception>
    let inline readBool (memory: Memory<'T, 'region>) (offset: int<offset>): bool * int<offset> =
        let b = memory.Data.[int (memory.Offset + offset)]
        match b with
        | 0uy -> false, offset + 1<offset>
        | 1uy -> true, offset + 1<offset>
        | _ -> failwith $"Invalid boolean value: {b}"
    
    /// <summary>
    /// Reads a boolean value from a span
    /// </summary>
    /// <param name="span">The span to read from</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <returns>The boolean value</returns>
    /// <exception cref="System.Exception">Thrown when the byte is not 0 or 1</exception>
    let inline readBoolSpan (span: ReadOnlySpan<byte>) (offset: byref<int>): bool =
        let b = span.[offset]
        offset <- offset + 1
        match b with
        | 0uy -> false
        | 1uy -> true
        | _ -> failwith $"Invalid boolean value: {b}"
    
    /// <summary>
    /// Reads a string value
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <returns>The string value and the new offset</returns>
    let inline readString (memory: Memory<'T, 'region>) (offset: int<offset>): string * int<offset> =
        // Read length
        let length, currentOffset = readUInt memory offset
        if length = 0UL then
            "", currentOffset
        else
            // Read bytes
            let stringSpan = memory.AsSpan().Slice(int currentOffset, int length)
            let str = getStringSpan stringSpan
            
            str, currentOffset + (int length * 1<offset>)
    
    /// <summary>
    /// Reads a string value from a span
    /// </summary>
    /// <param name="span">The span to read from</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <returns>The string value</returns>
    let inline readStringSpan (span: ReadOnlySpan<byte>) (offset: byref<int>): string =
        // Read length
        let length = int (readUIntSpan span &offset)
        if length = 0 then
            ""
        else
            // Read bytes
            let stringSpan = span.Slice(offset, length)
            let str = getStringSpan stringSpan
            offset <- offset + length
            str
    
    /// <summary>
    /// Reads a variable-length data value
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <returns>The byte array and the new offset</returns>
    let inline readData (memory: Memory<'T, 'region>) (offset: int<offset>): byte[] * int<offset> =
        // Read length
        let length, currentOffset = readUInt memory offset
        if length = 0UL then
            Array.empty, currentOffset
        else
            // Extract bytes
            let result = Array.zeroCreate (int length)
            let sourceSpan = memory.AsSpan().Slice(int currentOffset, int length)
            sourceSpan.CopyTo(Span<byte>(result))
            
            result, currentOffset + (int length * 1<offset>)
    
    /// <summary>
    /// Reads a variable-length data value from a span
    /// </summary>
    /// <param name="span">The span to read from</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <returns>The byte array</returns>
    let inline readDataSpan (span: ReadOnlySpan<byte>) (offset: byref<int>): byte[] =
        // Read length
        let length = int (readUIntSpan span &offset)
        if length = 0 then
            Array.empty
        else
            // Extract bytes
            let result = Array.zeroCreate length
            span.Slice(offset, length).CopyTo(Span<byte>(result))
            offset <- offset + length
            result
    
    /// <summary>
    /// Reads fixed-length data
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <param name="length">The length of data to read</param>
    /// <returns>The byte array and the new offset</returns>
    let inline readFixedData (memory: Memory<'T, 'region>) (offset: int<offset>) (length: int): byte[] * int<offset> =
        // Extract bytes
        let result = Array.zeroCreate length
        let sourceSpan = memory.AsSpan().Slice(int offset, length)
        sourceSpan.CopyTo(Span<byte>(result))
        
        result, offset + (length * 1<offset>)
    
    /// <summary>
    /// Reads fixed-length data from a span
    /// </summary>
    /// <param name="span">The span to read from</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <param name="length">The length of data to read</param>
    /// <returns>The byte array</returns>
    let inline readFixedDataSpan (span: ReadOnlySpan<byte>) (offset: byref<int>) (length: int): byte[] =
        // Extract bytes
        let result = Array.zeroCreate length
        span.Slice(offset, length).CopyTo(Span<byte>(result))
        offset <- offset + length
        result
    
    /// <summary>
    /// Reads an optional value using Alloy's ValueOption for zero-allocation
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <param name="readValue">A function to read the value if present</param>
    /// <returns>The optional value and the new offset</returns>
    /// <exception cref="System.Exception">Thrown when the tag is not 0 or 1</exception>
    let inline readOptional (memory: Memory<'T, 'region>) 
                     (offset: int<offset>) 
                     (readValue: Memory<'T, 'region> -> int<offset> -> 'a * int<offset>): 
                     ValueOption<'a> * int<offset> =
        let tag = memory.Data.[int (memory.Offset + offset)]
        let currentOffset = offset + 1<offset>
        
        match tag with
        | 0uy -> ValueOption<'a>.None, currentOffset
        | 1uy -> 
            let value, newOffset = readValue memory currentOffset
            ValueOption.Some value, newOffset
        | _ -> failwith $"Invalid optional tag: {tag}"
    
    /// <summary>
    /// Reads an optional value from a span using Alloy's ValueOption
    /// </summary>
    /// <param name="span">The span to read from</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <param name="readValue">A function to read the value if present</param>
    /// <returns>The optional value</returns>
    /// <exception cref="System.Exception">Thrown when the tag is not 0 or 1</exception>
    let inline readOptionalSpan (span: ReadOnlySpan<byte>) 
                        (offset: byref<int>) 
                        (readValue: ReadOnlySpan<byte> -> byref<int> -> 'a): 
                        ValueOption<'a> =
        let tag = span.[offset]
        offset <- offset + 1
        
        match tag with
        | 0uy -> ValueOption<'a>.None
        | 1uy -> 
            let value = readValue span &offset
            ValueOption.Some value
        | _ -> failwith $"Invalid optional tag: {tag}"
    
    /// <summary>
    /// Reads a list of values
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <param name="readValue">A function to read each value</param>
    /// <returns>The list of values and the new offset</returns>
    let inline readList (memory: Memory<'T, 'region>) 
                 (offset: int<offset>) 
                 (readValue: Memory<'T, 'region> -> int<offset> -> 'a * int<offset>): 
                 'a list * int<offset> =
        // Read count
        let count, currentOffset = readUInt memory offset
        
        // Read each value
        let mutable values = []
        let mutable currentOff = currentOffset
        
        for _ in 1UL..count do
            let value, newOffset = readValue memory currentOff
            values <- value :: values
            currentOff <- newOffset
        
        List.rev values, currentOff
    
    /// <summary>
    /// Reads a list of values from a span
    /// </summary>
    /// <param name="span">The span to read from</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <param name="readValue">A function to read each value</param>
    /// <returns>The list of values</returns>
    let inline readListSpan (span: ReadOnlySpan<byte>) 
                    (offset: byref<int>) 
                    (readValue: ReadOnlySpan<byte> -> byref<int> -> 'a): 
                    'a list =
        // Read count
        let count = int (readUIntSpan span &offset)
        
        // Read each value
        let mutable values = []
        
        for _ in 1..count do
            let value = readValue span &offset
            values <- value :: values
        
        List.rev values
    
    /// <summary>
    /// Reads a fixed-length list of values
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <param name="length">The number of elements to read</param>
    /// <param name="readValue">A function to read each value</param>
    /// <returns>The list of values and the new offset</returns>
    let inline readFixedList (memory: Memory<'T, 'region>) 
                      (offset: int<offset>) 
                      (length: int) 
                      (readValue: Memory<'T, 'region> -> int<offset> -> 'a * int<offset>): 
                      'a list * int<offset> =
        // Read each value (no length prefix)
        let mutable values = []
        let mutable currentOffset = offset
        
        for _ in 1..length do
            let value, newOffset = readValue memory currentOffset
            values <- value :: values
            currentOffset <- newOffset
        
        List.rev values, currentOffset
    
    /// <summary>
    /// Reads a fixed-length list of values from a span
    /// </summary>
    /// <param name="span">The span to read from</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <param name="length">The number of elements to read</param>
    /// <param name="readValue">A function to read each value</param>
    /// <returns>The list of values</returns>
    let inline readFixedListSpan (span: ReadOnlySpan<byte>) 
                         (offset: byref<int>) 
                         (length: int) 
                         (readValue: ReadOnlySpan<byte> -> byref<int> -> 'a): 
                         'a list =
        // Read each value (no length prefix)
        let mutable values = []
        
        for _ in 1..length do
            let value = readValue span &offset
            values <- value :: values
        
        List.rev values
    
    /// <summary>
    /// Reads a map of key-value pairs
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <param name="readKey">A function to read each key</param>
    /// <param name="readValue">A function to read each value</param>
    /// <returns>The map of key-value pairs and the new offset</returns>
    let inline readMap (memory: Memory<'T, 'region>) 
                (offset: int<offset>) 
                (readKey: Memory<'T, 'region> -> int<offset> -> 'k * int<offset>) 
                (readValue: Memory<'T, 'region> -> int<offset> -> 'v * int<offset>): 
                Map<'k, 'v> * int<offset> =
        // Read count
        let count, currentOffset = readUInt memory offset
        
        // Read each key-value pair
        let mutable map = Map.empty
        let mutable currentOff = currentOffset
        
        for _ in 1UL..count do
            let key, keyOffset = readKey memory currentOff
            let value, valueOffset = readValue memory keyOffset
            
            map <- Map.add key value map
            currentOff <- valueOffset
        
        map, currentOff
    
    /// <summary>
    /// Reads a map of key-value pairs from a span
    /// </summary>
    /// <param name="span">The span to read from</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <param name="readKey">A function to read each key</param>
    /// <param name="readValue">A function to read each value</param>
    /// <returns>The map of key-value pairs</returns>
    let inline readMapSpan (span: ReadOnlySpan<byte>) 
                   (offset: byref<int>) 
                   (readKey: ReadOnlySpan<byte> -> byref<int> -> 'k) 
                   (readValue: ReadOnlySpan<byte> -> byref<int> -> 'v): 
                   Map<'k, 'v> =
        // Read count
        let count = int (readUIntSpan span &offset)
        
        // Read each key-value pair
        let mutable map = Map.empty
        
        for _ in 1..count do
            let key = readKey span &offset
            let value = readValue span &offset
            
            map <- Map.add key value map
        
        map
    
    /// <summary>
    /// Reads a union value
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <param name="readValueForTag">A function to read a value based on its tag</param>
    /// <returns>The union tag, value and the new offset</returns>
    let inline readUnion (memory: Memory<'T, 'region>) 
                  (offset: int<offset>) 
                  (readValueForTag: uint -> Memory<'T, 'region> -> int<offset> -> 'a * int<offset>): 
                  uint * 'a * int<offset> =
        // Read tag
        let tagVal, currentOffset = readUInt memory offset
        let tag = uint tagVal
        
        // Read value based on tag
        let value, finalOffset = readValueForTag tag memory currentOffset
        tag, value, finalOffset
    
    /// <summary>
    /// Reads a union value from a span
    /// </summary>
    /// <param name="span">The span to read from</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <param name="readValueForTag">A function to read a value based on its tag</param>
    /// <returns>The union tag and value</returns>
    let inline readUnionSpan (span: ReadOnlySpan<byte>) 
                     (offset: byref<int>) 
                     (readValueForTag: uint -> ReadOnlySpan<byte> -> byref<int> -> 'a): 
                     uint * 'a =
        // Read tag
        let tagVal = readUIntSpan span &offset
        let tag = uint tagVal
        
        // Read value based on tag
        let value = readValueForTag tag span &offset
        tag, value