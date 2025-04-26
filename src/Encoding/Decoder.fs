namespace BAREWire.Encoding

open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Core.Binary
open BAREWire.Core.Utf8

/// <summary>
/// Decoding functions for BARE types
/// </summary>
module Decoder =
    /// <summary>
    /// Reads a uint value using ULEB128 encoding
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <returns>The decoded uint64 value and the new offset</returns>
    let readUInt (memory: Memory<'T, 'region>) (offset: int<offset>): uint64 * int<offset> =
        let mutable result = 0UL
        let mutable shift = 0
        let mutable currentOffset = offset
        let mutable currentByte = 0uy
        
        let mutable continue = true
        while continue do
            currentByte <- memory.Data.[int (memory.Offset + currentOffset)]
            currentOffset <- currentOffset + 1<offset>
            
            result <- result ||| ((uint64 (currentByte &&& 0x7Fuy)) <<< shift)
            shift <- shift + 7
            continue <- (currentByte &&& 0x80uy <> 0uy) && (shift < 64)
        
        result, currentOffset
    
    /// <summary>
    /// Reads an int value using zigzag ULEB128 encoding
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <returns>The decoded int64 value and the new offset</returns>
    let readInt (memory: Memory<'T, 'region>) (offset: int<offset>): int64 * int<offset> =
        let uintVal, newOffset = readUInt memory offset
        
        // Zigzag decoding: (n >>> 1) ^ -(n &&& 1)
        let value = (int64 uintVal >>> 1) ^^^ (-(int64 (uintVal &&& 1UL)))
        value, newOffset
    
    /// <summary>
    /// Reads a u8 (byte) value
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The offset to read at</param>
    /// <returns>The byte value and the new offset</returns>
    let readU8 (memory: Memory<'T, 'region>) (offset: int<offset>): byte * int<offset> =
        let value = memory.Data.[int (memory.Offset + offset)]
        value, offset + 1<offset>
    
    /// <summary>
    /// Reads a u16 value in little-endian format
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <returns>The uint16 value and the new offset</returns>
    let readU16 (memory: Memory<'T, 'region>) (offset: int<offset>): uint16 * int<offset> =
        let b0 = uint16 memory.Data.[int (memory.Offset + offset)]
        let b1 = uint16 memory.Data.[int (memory.Offset + offset + 1<offset>)]
        
        let value = b0 ||| (b1 <<< 8)
        value, offset + 2<offset>
    
    /// <summary>
    /// Reads a u32 value in little-endian format
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <returns>The uint32 value and the new offset</returns>
    let readU32 (memory: Memory<'T, 'region>) (offset: int<offset>): uint32 * int<offset> =
        let b0 = uint32 memory.Data.[int (memory.Offset + offset)]
        let b1 = uint32 memory.Data.[int (memory.Offset + offset + 1<offset>)]
        let b2 = uint32 memory.Data.[int (memory.Offset + offset + 2<offset>)]
        let b3 = uint32 memory.Data.[int (memory.Offset + offset + 3<offset>)]
        
        let value = b0 ||| (b1 <<< 8) ||| (b2 <<< 16) ||| (b3 <<< 24)
        value, offset + 4<offset>
    
    /// <summary>
    /// Reads a u64 value in little-endian format
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <returns>The uint64 value and the new offset</returns>
    let readU64 (memory: Memory<'T, 'region>) (offset: int<offset>): uint64 * int<offset> =
        let b0 = uint64 memory.Data.[int (memory.Offset + offset)]
        let b1 = uint64 memory.Data.[int (memory.Offset + offset + 1<offset>)]
        let b2 = uint64 memory.Data.[int (memory.Offset + offset + 2<offset>)]
        let b3 = uint64 memory.Data.[int (memory.Offset + offset + 3<offset>)]
        let b4 = uint64 memory.Data.[int (memory.Offset + offset + 4<offset>)]
        let b5 = uint64 memory.Data.[int (memory.Offset + offset + 5<offset>)]
        let b6 = uint64 memory.Data.[int (memory.Offset + offset + 6<offset>)]
        let b7 = uint64 memory.Data.[int (memory.Offset + offset + 7<offset>)]
        
        let value = 
            b0 ||| (b1 <<< 8) ||| (b2 <<< 16) ||| (b3 <<< 24) |||
            (b4 <<< 32) ||| (b5 <<< 40) ||| (b6 <<< 48) ||| (b7 <<< 56)
        value, offset + 8<offset>
    
    /// <summary>
    /// Reads an i8 (sbyte) value
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The offset to read at</param>
    /// <returns>The sbyte value and the new offset</returns>
    let readI8 (memory: Memory<'T, 'region>) (offset: int<offset>): sbyte * int<offset> =
        let value = sbyte memory.Data.[int (memory.Offset + offset)]
        value, offset + 1<offset>
    
    /// <summary>
    /// Reads an i16 value in little-endian format
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <returns>The int16 value and the new offset</returns>
    let readI16 (memory: Memory<'T, 'region>) (offset: int<offset>): int16 * int<offset> =
        let b0 = int16 memory.Data.[int (memory.Offset + offset)]
        let b1 = int16 memory.Data.[int (memory.Offset + offset + 1<offset>)]
        
        let value = b0 ||| (b1 <<< 8)
        value, offset + 2<offset>
    
    /// <summary>
    /// Reads an i32 value in little-endian format
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <returns>The int32 value and the new offset</returns>
    let readI32 (memory: Memory<'T, 'region>) (offset: int<offset>): int32 * int<offset> =
        let b0 = int32 memory.Data.[int (memory.Offset + offset)]
        let b1 = int32 memory.Data.[int (memory.Offset + offset + 1<offset>)]
        let b2 = int32 memory.Data.[int (memory.Offset + offset + 2<offset>)]
        let b3 = int32 memory.Data.[int (memory.Offset + offset + 3<offset>)]
        
        let value = b0 ||| (b1 <<< 8) ||| (b2 <<< 16) ||| (b3 <<< 24)
        value, offset + 4<offset>
    
    /// <summary>
    /// Reads an i64 value in little-endian format
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <returns>The int64 value and the new offset</returns>
    let readI64 (memory: Memory<'T, 'region>) (offset: int<offset>): int64 * int<offset> =
        let b0 = int64 memory.Data.[int (memory.Offset + offset)]
        let b1 = int64 memory.Data.[int (memory.Offset + offset + 1<offset>)]
        let b2 = int64 memory.Data.[int (memory.Offset + offset + 2<offset>)]
        let b3 = int64 memory.Data.[int (memory.Offset + offset + 3<offset>)]
        let b4 = int64 memory.Data.[int (memory.Offset + offset + 4<offset>)]
        let b5 = int64 memory.Data.[int (memory.Offset + offset + 5<offset>)]
        let b6 = int64 memory.Data.[int (memory.Offset + offset + 6<offset>)]
        let b7 = int64 memory.Data.[int (memory.Offset + offset + 7<offset>)]
        
        let value = 
            b0 ||| (b1 <<< 8) ||| (b2 <<< 16) ||| (b3 <<< 24) |||
            (b4 <<< 32) ||| (b5 <<< 40) ||| (b6 <<< 48) ||| (b7 <<< 56)
        value, offset + 8<offset>
    
    /// <summary>
    /// Reads an f32 (float32) value
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <returns>The float32 value and the new offset</returns>
    let readF32 (memory: Memory<'T, 'region>) (offset: int<offset>): float32 * int<offset> =
        let bits, newOffset = readI32 memory offset
        let value = int32BitsToSingle bits
        value, newOffset
    
    /// <summary>
    /// Reads an f64 (double) value
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <returns>The double value and the new offset</returns>
    let readF64 (memory: Memory<'T, 'region>) (offset: int<offset>): float * int<offset> =
        let bits, newOffset = readI64 memory offset
        let value = int64BitsToDouble bits
        value, newOffset
    
    /// <summary>
    /// Reads a boolean value
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The offset to read at</param>
    /// <returns>The boolean value and the new offset</returns>
    /// <exception cref="System.Exception">Thrown when the byte is not 0 or 1</exception>
    let readBool (memory: Memory<'T, 'region>) (offset: int<offset>): bool * int<offset> =
        let b = memory.Data.[int (memory.Offset + offset)]
        match b with
        | 0uy -> false, offset + 1<offset>
        | 1uy -> true, offset + 1<offset>
        | _ -> failwith $"Invalid boolean value: {b}"
    
    /// <summary>
    /// Reads a string value
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <returns>The string value and the new offset</returns>
    let readString (memory: Memory<'T, 'region>) (offset: int<offset>): string * int<offset> =
        // Read length
        let length, currentOffset = readUInt memory offset
        
        // Read bytes
        let bytes = Array.init (int length) (fun i -> 
            memory.Data.[int (memory.Offset + currentOffset) + i])
        let str = getString bytes
        
        str, currentOffset + (int length * 1<offset>)
    
    /// <summary>
    /// Reads a variable-length data value
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <returns>The byte array and the new offset</returns>
    let readData (memory: Memory<'T, 'region>) (offset: int<offset>): byte[] * int<offset> =
        // Read length
        let length, currentOffset = readUInt memory offset
        
        // Extract bytes
        let bytes = Array.init (int length) (fun i -> 
            memory.Data.[int (memory.Offset + currentOffset) + i])
        
        bytes, currentOffset + (int length * 1<offset>)
    
    /// <summary>
    /// Reads fixed-length data
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <param name="length">The length of data to read</param>
    /// <returns>The byte array and the new offset</returns>
    let readFixedData (memory: Memory<'T, 'region>) (offset: int<offset>) (length: int): byte[] * int<offset> =
        // Extract bytes
        let bytes = Array.init length (fun i -> 
            memory.Data.[int (memory.Offset + offset) + i])
        
        bytes, offset + (length * 1<offset>)
    
    /// <summary>
    /// Reads an optional value
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <param name="readValue">A function to read the value if present</param>
    /// <returns>The optional value and the new offset</returns>
    /// <exception cref="System.Exception">Thrown when the tag is not 0 or 1</exception>
    let readOptional (memory: Memory<'T, 'region>) 
                     (offset: int<offset>) 
                     (readValue: Memory<'T, 'region> -> int<offset> -> 'a * int<offset>): 
                     'a option * int<offset> =
        let tag = memory.Data.[int (memory.Offset + offset)]
        let currentOffset = offset + 1<offset>
        
        match tag with
        | 0uy -> None, currentOffset
        | 1uy -> 
            let value, newOffset = readValue memory currentOffset
            Some value, newOffset
        | _ -> failwith $"Invalid optional tag: {tag}"
    
    /// <summary>
    /// Reads a list of values
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <param name="readValue">A function to read each value</param>
    /// <returns>The list of values and the new offset</returns>
    let readList (memory: Memory<'T, 'region>) 
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
    /// Reads a fixed-length list of values
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <param name="length">The number of elements to read</param>
    /// <param name="readValue">A function to read each value</param>
    /// <returns>The list of values and the new offset</returns>
    let readFixedList (memory: Memory<'T, 'region>) 
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
    /// Reads a map of key-value pairs
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <param name="readKey">A function to read each key</param>
    /// <param name="readValue">A function to read each value</param>
    /// <returns>The map of key-value pairs and the new offset</returns>
    let readMap (memory: Memory<'T, 'region>) 
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
    /// Reads a union value
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The starting offset</param>
    /// <param name="readValueForTag">A function to read a value based on its tag</param>
    /// <returns>The value and the new offset</returns>
    let readUnion (memory: Memory<'T, 'region>) 
                  (offset: int<offset>) 
                  (readValueForTag: uint -> Memory<'T, 'region> -> int<offset> -> 'a * int<offset>): 
                  'a * int<offset> =
        // Read tag
        let tagVal, currentOffset = readUInt memory offset
        let tag = uint tagVal
        
        // Read value based on tag
        readValueForTag tag memory currentOffset