namespace BAREWire.Encoding

open BAREWire.Core
open BAREWire.Core.Error

/// <summary>
/// Encoding functions for BARE types
/// </summary>
module Encoder =
    /// <summary>
    /// Writes a uint value using ULEB128 encoding
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The uint64 value to encode</param>
    let writeUInt (buffer: Buffer<'T>) (value: uint64): unit =
        let mutable val' = value
        while val' >= 128UL do
            buffer.Write(byte (val' ||| 128UL))
            val' <- val' >>> 7
        buffer.Write(byte val')
    
    /// <summary>
    /// Writes an int value using zigzag ULEB128 encoding
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The int64 value to encode</param>
    let writeInt (buffer: Buffer<'T>) (value: int64): unit =
        // Zigzag encoding: (n << 1) ^ (n >> 63)
        let zigzag = (value <<< 1) ^^^ (value >>> 63)
        writeUInt buffer (uint64 zigzag)
    
    /// <summary>
    /// Writes a u8 (byte) value
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The byte value to write</param>
    let writeU8 (buffer: Buffer<'T>) (value: byte): unit =
        buffer.Write(value)
    
    /// <summary>
    /// Writes a u16 value in little-endian format
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The uint16 value to write</param>
    let writeU16 (buffer: Buffer<'T>) (value: uint16): unit =
        buffer.Write(byte (value &&& 0xFFus))
        buffer.Write(byte (value >>> 8))
    
    /// <summary>
    /// Writes a u32 value in little-endian format
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The uint32 value to write</param>
    let writeU32 (buffer: Buffer<'T>) (value: uint32): unit =
        buffer.Write(byte (value &&& 0xFFu))
        buffer.Write(byte ((value >>> 8) &&& 0xFFu))
        buffer.Write(byte ((value >>> 16) &&& 0xFFu))
        buffer.Write(byte ((value >>> 24) &&& 0xFFu))
    
    /// <summary>
    /// Writes a u64 value in little-endian format
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The uint64 value to write</param>
    let writeU64 (buffer: Buffer<'T>) (value: uint64): unit =
        buffer.Write(byte (value &&& 0xFFUL))
        buffer.Write(byte ((value >>> 8) &&& 0xFFUL))
        buffer.Write(byte ((value >>> 16) &&& 0xFFUL))
        buffer.Write(byte ((value >>> 24) &&& 0xFFUL))
        buffer.Write(byte ((value >>> 32) &&& 0xFFUL))
        buffer.Write(byte ((value >>> 40) &&& 0xFFUL))
        buffer.Write(byte ((value >>> 48) &&& 0xFFUL))
        buffer.Write(byte ((value >>> 56) &&& 0xFFUL))
    
    /// <summary>
    /// Writes an i8 (sbyte) value
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The sbyte value to write</param>
    let writeI8 (buffer: Buffer<'T>) (value: sbyte): unit =
        buffer.Write(byte value)
    
    /// <summary>
    /// Writes an i16 value in little-endian format
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The int16 value to write</param>
    let writeI16 (buffer: Buffer<'T>) (value: int16): unit =
        buffer.Write(byte (value &&& 0xFFs))
        buffer.Write(byte (value >>> 8))
    
    /// <summary>
    /// Writes an i32 value in little-endian format
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The int32 value to write</param>
    let writeI32 (buffer: Buffer<'T>) (value: int32): unit =
        buffer.Write(byte (value &&& 0xFF))
        buffer.Write(byte ((value >>> 8) &&& 0xFF))
        buffer.Write(byte ((value >>> 16) &&& 0xFF))
        buffer.Write(byte ((value >>> 24) &&& 0xFF))
    
    /// <summary>
    /// Writes an i64 value in little-endian format
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The int64 value to write</param>
    let writeI64 (buffer: Buffer<'T>) (value: int64): unit =
        buffer.Write(byte (value &&& 0xFFL))
        buffer.Write(byte ((value >>> 8) &&& 0xFFL))
        buffer.Write(byte ((value >>> 16) &&& 0xFFL))
        buffer.Write(byte ((value >>> 24) &&& 0xFFL))
        buffer.Write(byte ((value >>> 32) &&& 0xFFL))
        buffer.Write(byte ((value >>> 40) &&& 0xFFL))
        buffer.Write(byte ((value >>> 48) &&& 0xFFL))
        buffer.Write(byte ((value >>> 56) &&& 0xFFL))
    
    /// <summary>
    /// Writes an f32 (float32) value
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The float32 value to write</param>
    let writeF32 (buffer: Buffer<'T>) (value: float32): unit =
        let bits = BitConverter.SingleToInt32Bits(value)
        writeI32 buffer bits
    
    /// <summary>
    /// Writes an f64 (double) value
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The double value to write</param>
    let writeF64 (buffer: Buffer<'T>) (value: float): unit =
        let bits = BitConverter.DoubleToInt64Bits(value)
        writeI64 buffer bits
    
    /// <summary>
    /// Writes a boolean value
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The boolean value to write</param>
    let writeBool (buffer: Buffer<'T>) (value: bool): unit =
        buffer.Write(if value then 1uy else 0uy)
    
    /// <summary>
    /// Writes a string value
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The string to write</param>
    let writeString (buffer: Buffer<'T>) (value: string): unit =
        // Get UTF-8 bytes
        let bytes = Encoding.UTF8.GetBytes(value)
        // Write length as uint
        writeUInt buffer (uint64 bytes.Length)
        // Write bytes
        buffer.WriteSpan(ReadOnlySpan(bytes))
    
    /// <summary>
    /// Writes a variable-length data value
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The byte array to write</param>
    let writeData (buffer: Buffer<'T>) (value: byte[]): unit =
        // Write length as uint
        writeUInt buffer (uint64 value.Length)
        // Write bytes
        buffer.WriteSpan(ReadOnlySpan(value))
    
    /// <summary>
    /// Writes fixed-length data
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The byte array to write</param>
    /// <param name="length">The expected length of the data</param>
    /// <exception cref="System.Exception">Thrown when the data length doesn't match the expected length</exception>
    let writeFixedData (buffer: Buffer<'T>) (value: byte[]) (length: int): unit =
        if value.Length <> length then
            failwith $"Fixed data length mismatch: expected {length}, got {value.Length}"
        
        // Write bytes directly (no length prefix)
        buffer.WriteSpan(ReadOnlySpan(value))
    
    /// <summary>
    /// Writes an optional value
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="valueOpt">The optional value to write</param>
    /// <param name="writeValue">A function to write the value if present</param>
    let writeOptional (buffer: Buffer<'T>) (valueOpt: 'a option) (writeValue: Buffer<'T> -> 'a -> unit): unit =
        match valueOpt with
        | None -> buffer.Write(0uy)  // Not present
        | Some value ->
            buffer.Write(1uy)  // Present
            writeValue buffer value
    
    /// <summary>
    /// Writes a list of values
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="values">The sequence of values to write</param>
    /// <param name="writeValue">A function to write each value</param>
    let writeList (buffer: Buffer<'T>) (values: 'a seq) (writeValue: Buffer<'T> -> 'a -> unit): unit =
        // Write count as uint
        let count = Seq.length values
        writeUInt buffer (uint64 count)
        
        // Write each value
        for value in values do
            writeValue buffer value
    
    /// <summary>
    /// Writes a fixed-length list of values
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="values">The sequence of values to write</param>
    /// <param name="length">The expected number of values</param>
    /// <param name="writeValue">A function to write each value</param>
    /// <exception cref="System.Exception">Thrown when the number of values doesn't match the expected length</exception>
    let writeFixedList (buffer: Buffer<'T>) (values: 'a seq) (length: int) (writeValue: Buffer<'T> -> 'a -> unit): unit =
        let count = Seq.length values
        if count <> length then
            failwith $"Fixed list length mismatch: expected {length}, got {count}"
        
        // Write each value (no length prefix)
        for value in values do
            writeValue buffer value
    
    /// <summary>
    /// Writes a map of key-value pairs
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="entries">The sequence of key-value pairs to write</param>
    /// <param name="writeKey">A function to write each key</param>
    /// <param name="writeValue">A function to write each value</param>
    let writeMap (buffer: Buffer<'T>) 
                (entries: ('k * 'v) seq) 
                (writeKey: Buffer<'T> -> 'k -> unit) 
                (writeValue: Buffer<'T> -> 'v -> unit): unit =
        // Write count as uint
        let count = Seq.length entries
        writeUInt buffer (uint64 count)
        
        // Write each key-value pair
        for key, value in entries do
            writeKey buffer key
            writeValue buffer value
    
    /// <summary>
    /// Writes a union value
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="tag">The union tag</param>
    /// <param name="value">The value to write</param>
    /// <param name="writeValue">A function to write the value</param>
    let writeUnion (buffer: Buffer<'T>) (tag: uint) (value: 'a) (writeValue: Buffer<'T> -> 'a -> unit): unit =
        // Write tag as uint
        writeUInt buffer (uint64 tag)
        
        // Write value
        writeValue buffer value
    
    /// <summary>
    /// Writes an enum value as a uint
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The enum value to write</param>
    let writeEnum (buffer: Buffer<'T>) (value: uint64): unit =
        writeUInt buffer value