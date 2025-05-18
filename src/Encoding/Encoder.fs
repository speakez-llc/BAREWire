namespace BAREWire.Encoding

open Alloy
open BAREWire.Core
open BAREWire.Core.Error
open BAREWire.Core.Binary
open BAREWire.Core.Utf8

/// <summary>
/// Encoding functions for BARE types using Alloy's zero-cost abstractions
/// </summary>
module Encoder =
    /// <summary>
    /// Writes a uint value using ULEB128 encoding
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The uint64 value to encode</param>
    let inline writeUInt (buffer: Buffer<'T>) (value: uint64): unit =
        let mutable val' = value
        while val' >= 128UL do
            buffer.Write(byte (val' ||| 128UL))
            val' <- val' >>> 7
        buffer.Write(byte val')
    
    /// <summary>
    /// Writes a uint value using ULEB128 encoding to a span
    /// </summary>
    /// <param name="span">The span to write to</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <param name="value">The uint64 value to encode</param>
    let inline writeUIntSpan (span: Span<byte>) (offset: byref<int>) (value: uint64): unit =
        let mutable val' = value
        while val' >= 128UL do
            span.[offset] <- byte (val' ||| 128UL)
            offset <- offset + 1
            val' <- val' >>> 7
        span.[offset] <- byte val'
        offset <- offset + 1
    
    /// <summary>
    /// Writes an int value using zigzag ULEB128 encoding
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The int64 value to encode</param>
    let inline writeInt (buffer: Buffer<'T>) (value: int64): unit =
        // Zigzag encoding: (n << 1) ^ (n >> 63)
        let zigzag = (value <<< 1) ^^^ (value >>> 63)
        writeUInt buffer (uint64 zigzag)
    
    /// <summary>
    /// Writes an int value using zigzag ULEB128 encoding to a span
    /// </summary>
    /// <param name="span">The span to write to</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <param name="value">The int64 value to encode</param>
    let inline writeIntSpan (span: Span<byte>) (offset: byref<int>) (value: int64): unit =
        // Zigzag encoding: (n << 1) ^ (n >> 63)
        let zigzag = (value <<< 1) ^^^ (value >>> 63)
        writeUIntSpan span &offset (uint64 zigzag)
    
    /// <summary>
    /// Writes a u8 (byte) value
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The byte value to write</param>
    let inline writeU8 (buffer: Buffer<'T>) (value: byte): unit =
        buffer.Write(value)
    
    /// <summary>
    /// Writes a u8 (byte) value to a span
    /// </summary>
    /// <param name="span">The span to write to</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <param name="value">The byte value to write</param>
    let inline writeU8Span (span: Span<byte>) (offset: byref<int>) (value: byte): unit =
        span.[offset] <- value
        offset <- offset + 1
    
    /// <summary>
    /// Writes a u16 value in little-endian format
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The uint16 value to write</param>
    let inline writeU16 (buffer: Buffer<'T>) (value: uint16): unit =
        let span = buffer.RemainingSpan().Slice(0, 2)
        writeUInt16ToSpan value span
        buffer.Position <- buffer.Position + 2<offset>
    
    /// <summary>
    /// Writes a u16 value in little-endian format to a span
    /// </summary>
    /// <param name="span">The span to write to</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <param name="value">The uint16 value to write</param>
    let inline writeU16Span (span: Span<byte>) (offset: byref<int>) (value: uint16): unit =
        writeUInt16ToSpan value (span.Slice(offset, 2))
        offset <- offset + 2
    
    /// <summary>
    /// Writes a u32 value in little-endian format
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The uint32 value to write</param>
    let inline writeU32 (buffer: Buffer<'T>) (value: uint32): unit =
        let span = buffer.RemainingSpan().Slice(0, 4)
        writeUInt32ToSpan value span
        buffer.Position <- buffer.Position + 4<offset>
    
    /// <summary>
    /// Writes a u32 value in little-endian format to a span
    /// </summary>
    /// <param name="span">The span to write to</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <param name="value">The uint32 value to write</param>
    let inline writeU32Span (span: Span<byte>) (offset: byref<int>) (value: uint32): unit =
        writeUInt32ToSpan value (span.Slice(offset, 4))
        offset <- offset + 4
    
    /// <summary>
    /// Writes a u64 value in little-endian format
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The uint64 value to write</param>
    let inline writeU64 (buffer: Buffer<'T>) (value: uint64): unit =
        let span = buffer.RemainingSpan().Slice(0, 8)
        writeUInt64ToSpan value span
        buffer.Position <- buffer.Position + 8<offset>
    
    /// <summary>
    /// Writes a u64 value in little-endian format to a span
    /// </summary>
    /// <param name="span">The span to write to</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <param name="value">The uint64 value to write</param>
    let inline writeU64Span (span: Span<byte>) (offset: byref<int>) (value: uint64): unit =
        writeUInt64ToSpan value (span.Slice(offset, 8))
        offset <- offset + 8
    
    /// <summary>
    /// Writes an i8 (sbyte) value
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The sbyte value to write</param>
    let inline writeI8 (buffer: Buffer<'T>) (value: sbyte): unit =
        buffer.Write(byte value)
    
    /// <summary>
    /// Writes an i8 (sbyte) value to a span
    /// </summary>
    /// <param name="span">The span to write to</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <param name="value">The sbyte value to write</param>
    let inline writeI8Span (span: Span<byte>) (offset: byref<int>) (value: sbyte): unit =
        span.[offset] <- byte value
        offset <- offset + 1
    
    /// <summary>
    /// Writes an i16 value in little-endian format
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The int16 value to write</param>
    let inline writeI16 (buffer: Buffer<'T>) (value: int16): unit =
        let span = buffer.RemainingSpan().Slice(0, 2)
        writeInt16ToSpan value span
        buffer.Position <- buffer.Position + 2<offset>
    
    /// <summary>
    /// Writes an i16 value in little-endian format to a span
    /// </summary>
    /// <param name="span">The span to write to</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <param name="value">The int16 value to write</param>
    let inline writeI16Span (span: Span<byte>) (offset: byref<int>) (value: int16): unit =
        writeInt16ToSpan value (span.Slice(offset, 2))
        offset <- offset + 2
    
    /// <summary>
    /// Writes an i32 value in little-endian format
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The int32 value to write</param>
    let inline writeI32 (buffer: Buffer<'T>) (value: int32): unit =
        let span = buffer.RemainingSpan().Slice(0, 4)
        writeInt32ToSpan value span
        buffer.Position <- buffer.Position + 4<offset>
    
    /// <summary>
    /// Writes an i32 value in little-endian format to a span
    /// </summary>
    /// <param name="span">The span to write to</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <param name="value">The int32 value to write</param>
    let inline writeI32Span (span: Span<byte>) (offset: byref<int>) (value: int32): unit =
        writeInt32ToSpan value (span.Slice(offset, 4))
        offset <- offset + 4
    
    /// <summary>
    /// Writes an i64 value in little-endian format
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The int64 value to write</param>
    let inline writeI64 (buffer: Buffer<'T>) (value: int64): unit =
        let span = buffer.RemainingSpan().Slice(0, 8)
        writeInt64ToSpan value span
        buffer.Position <- buffer.Position + 8<offset>
    
    /// <summary>
    /// Writes an i64 value in little-endian format to a span
    /// </summary>
    /// <param name="span">The span to write to</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <param name="value">The int64 value to write</param>
    let inline writeI64Span (span: Span<byte>) (offset: byref<int>) (value: int64): unit =
        writeInt64ToSpan value (span.Slice(offset, 8))
        offset <- offset + 8
    
    /// <summary>
    /// Writes an f32 (float32) value
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The float32 value to write</param>
    let inline writeF32 (buffer: Buffer<'T>) (value: float32): unit =
        let bits = singleToInt32Bits value
        writeI32 buffer bits
    
    /// <summary>
    /// Writes an f32 (float32) value to a span
    /// </summary>
    /// <param name="span">The span to write to</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <param name="value">The float32 value to write</param>
    let inline writeF32Span (span: Span<byte>) (offset: byref<int>) (value: float32): unit =
        let bits = singleToInt32Bits value
        writeI32Span span &offset bits
    
    /// <summary>
    /// Writes an f64 (double) value
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The double value to write</param>
    let inline writeF64 (buffer: Buffer<'T>) (value: float): unit =
        let bits = doubleToInt64Bits value
        writeI64 buffer bits
    
    /// <summary>
    /// Writes an f64 (double) value to a span
    /// </summary>
    /// <param name="span">The span to write to</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <param name="value">The double value to write</param>
    let inline writeF64Span (span: Span<byte>) (offset: byref<int>) (value: float): unit =
        let bits = doubleToInt64Bits value
        writeI64Span span &offset bits
    
    /// <summary>
    /// Writes a boolean value
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The boolean value to write</param>
    let inline writeBool (buffer: Buffer<'T>) (value: bool): unit =
        buffer.Write(if value then 1uy else 0uy)
    
    /// <summary>
    /// Writes a boolean value to a span
    /// </summary>
    /// <param name="span">The span to write to</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <param name="value">The boolean value to write</param>
    let inline writeBoolSpan (span: Span<byte>) (offset: byref<int>) (value: bool): unit =
        span.[offset] <- if value then 1uy else 0uy
        offset <- offset + 1
    
    /// <summary>
    /// Writes a string value using Alloy's optimized string operations
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The string to write</param>
    let inline writeString (buffer: Buffer<'T>) (value: string): unit =
        if String.isNullOrEmpty value then
            // Write zero length
            writeUInt buffer 0UL
        else
            // Calculate UTF-8 length
            let byteLength = calculateEncodedLength value
            
            // Write length as uint
            writeUInt buffer (uint64 byteLength)
            
            // Write bytes directly to the buffer
            let span = buffer.RemainingSpan().Slice(0, byteLength)
            let bytesWritten = getBytesSpan value span
            buffer.Position <- buffer.Position + (bytesWritten * 1<offset>)
    
    /// <summary>
    /// Writes a string value to a span
    /// </summary>
    /// <param name="span">The span to write to</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <param name="value">The string to write</param>
    let inline writeStringSpan (span: Span<byte>) (offset: byref<int>) (value: string): unit =
        if String.isNullOrEmpty value then
            // Write zero length
            writeUIntSpan span &offset 0UL
        else
            // Calculate UTF-8 length
            let byteLength = calculateEncodedLength value
            
            // Write length
            writeUIntSpan span &offset (uint64 byteLength)
            
            // Write UTF-8 bytes
            let bytesWritten = getBytesSpan value (span.Slice(offset, byteLength))
            offset <- offset + bytesWritten
    
    /// <summary>
    /// Writes a variable-length data value
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The byte array to write</param>
    let inline writeData (buffer: Buffer<'T>) (value: byte[]): unit =
        // Write length as uint
        let length = len value
        writeUInt buffer (uint64 length)
        
        if length > 0 then
            // Write bytes
            buffer.WriteSpan(ReadOnlySpan<byte>(value))
    
    /// <summary>
    /// Writes a variable-length data value to a span
    /// </summary>
    /// <param name="span">The span to write to</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <param name="value">The byte array to write</param>
    let inline writeDataSpan (span: Span<byte>) (offset: byref<int>) (value: ReadOnlySpan<byte>): unit =
        // Write length
        writeUIntSpan span &offset (uint64 value.Length)
        
        if value.Length > 0 then
            // Write bytes
            value.CopyTo(span.Slice(offset))
            offset <- offset + value.Length
    
    /// <summary>
    /// Writes fixed-length data
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The byte array to write</param>
    /// <param name="length">The expected length of the data</param>
    /// <exception cref="System.Exception">Thrown when the data length doesn't match the expected length</exception>
    let inline writeFixedData (buffer: Buffer<'T>) (value: byte[]) (length: int): unit =
        if len value <> length then
            failwith $"Fixed data length mismatch: expected {length}, got {len value}"
        
        // Write bytes directly (no length prefix)
        buffer.WriteSpan(ReadOnlySpan<byte>(value))
    
    /// <summary>
    /// Writes fixed-length data to a span
    /// </summary>
    /// <param name="span">The span to write to</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <param name="value">The byte array to write</param>
    /// <param name="length">The expected length of the data</param>
    /// <exception cref="System.Exception">Thrown when the data length doesn't match the expected length</exception>
    let inline writeFixedDataSpan (span: Span<byte>) (offset: byref<int>) (value: ReadOnlySpan<byte>) (length: int): unit =
        if value.Length <> length then
            failwith $"Fixed data length mismatch: expected {length}, got {value.Length}"
        
        // Write bytes directly (no length prefix)
        value.CopyTo(span.Slice(offset))
        offset <- offset + length
    
    /// <summary>
    /// Writes an optional value using Alloy's ValueOption
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="valueOpt">The optional value to write</param>
    /// <param name="writeValue">A function to write the value if present</param>
    let inline writeOptional (buffer: Buffer<'T>) (valueOpt: ValueOption<'a>) (writeValue: Buffer<'T> -> 'a -> unit): unit =
        if valueOpt.IsSome then
            buffer.Write(1uy)  // Present
            writeValue buffer valueOpt.Value
        else
            buffer.Write(0uy)  // Not present
    
    /// <summary>
    /// Writes an optional value to a span
    /// </summary>
    /// <param name="span">The span to write to</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <param name="valueOpt">The optional value to write</param>
    /// <param name="writeValue">A function to write the value if present</param>
    let inline writeOptionalSpan (span: Span<byte>) (offset: byref<int>) (valueOpt: ValueOption<'a>) (writeValue: Span<byte> -> byref<int> -> 'a -> unit): unit =
        if valueOpt.IsSome then
            span.[offset] <- 1uy  // Present
            offset <- offset + 1
            writeValue span &offset valueOpt.Value
        else
            span.[offset] <- 0uy  // Not present
            offset <- offset + 1
    
    /// <summary>
    /// Writes a list of values
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="values">The sequence of values to write</param>
    /// <param name="writeValue">A function to write each value</param>
    let inline writeList (buffer: Buffer<'T>) (values: 'a seq) (writeValue: Buffer<'T> -> 'a -> unit): unit =
        // Write count as uint
        let count = Seq.length values
        writeUInt buffer (uint64 count)
        
        // Write each value
        for value in values do
            writeValue buffer value
    
    /// <summary>
    /// Writes a list of values to a span
    /// </summary>
    /// <param name="span">The span to write to</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <param name="values">The sequence of values to write</param>
    /// <param name="writeValue">A function to write each value</param>
    let inline writeListSpan (span: Span<byte>) (offset: byref<int>) (values: 'a seq) (writeValue: Span<byte> -> byref<int> -> 'a -> unit): unit =
        // Write count
        let count = Seq.length values
        writeUIntSpan span &offset (uint64 count)
        
        // Write each value
        for value in values do
            writeValue span &offset value
    
    /// <summary>
    /// Writes a fixed-length list of values
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="values">The sequence of values to write</param>
    /// <param name="length">The expected number of values</param>
    /// <param name="writeValue">A function to write each value</param>
    /// <exception cref="System.Exception">Thrown when the number of values doesn't match the expected length</exception>
    let inline writeFixedList (buffer: Buffer<'T>) (values: 'a seq) (length: int) (writeValue: Buffer<'T> -> 'a -> unit): unit =
        let count = Seq.length values
        if count <> length then
            failwith $"Fixed list length mismatch: expected {length}, got {count}"
        
        // Write each value (no length prefix)
        for value in values do
            writeValue buffer value
    
    /// <summary>
    /// Writes a fixed-length list of values to a span
    /// </summary>
    /// <param name="span">The span to write to</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <param name="values">The sequence of values to write</param>
    /// <param name="length">The expected number of values</param>
    /// <param name="writeValue">A function to write each value</param>
    /// <exception cref="System.Exception">Thrown when the number of values doesn't match the expected length</exception>
    let inline writeFixedListSpan (span: Span<byte>) (offset: byref<int>) (values: 'a seq) (length: int) (writeValue: Span<byte> -> byref<int> -> 'a -> unit): unit =
        let count = Seq.length values
        if count <> length then
            failwith $"Fixed list length mismatch: expected {length}, got {count}"
        
        // Write each value (no length prefix)
        for value in values do
            writeValue span &offset value
    
    /// <summary>
    /// Writes a map of key-value pairs
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="entries">The sequence of key-value pairs to write</param>
    /// <param name="writeKey">A function to write each key</param>
    /// <param name="writeValue">A function to write each value</param>
    let inline writeMap (buffer: Buffer<'T>) 
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
    /// Writes a map of key-value pairs to a span
    /// </summary>
    /// <param name="span">The span to write to</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <param name="entries">The sequence of key-value pairs to write</param>
    /// <param name="writeKey">A function to write each key</param>
    /// <param name="writeValue">A function to write each value</param>
    let inline writeMapSpan (span: Span<byte>) 
                   (offset: byref<int>) 
                   (entries: ('k * 'v) seq) 
                   (writeKey: Span<byte> -> byref<int> -> 'k -> unit) 
                   (writeValue: Span<byte> -> byref<int> -> 'v -> unit): unit =
        // Write count
        let count = Seq.length entries
        writeUIntSpan span &offset (uint64 count)
        
        // Write each key-value pair
        for key, value in entries do
            writeKey span &offset key
            writeValue span &offset value
    
    /// <summary>
    /// Writes a union value
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="tag">The union tag</param>
    /// <param name="value">The value to write</param>
    /// <param name="writeValue">A function to write the value</param>
    let inline writeUnion (buffer: Buffer<'T>) (tag: uint) (value: 'a) (writeValue: Buffer<'T> -> 'a -> unit): unit =
        // Write tag as uint
        writeUInt buffer (uint64 tag)
        
        // Write value
        writeValue buffer value
    
    /// <summary>
    /// Writes a union value to a span
    /// </summary>
    /// <param name="span">The span to write to</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <param name="tag">The union tag</param>
    /// <param name="value">The value to write</param>
    /// <param name="writeValue">A function to write the value</param>
    let inline writeUnionSpan (span: Span<byte>) (offset: byref<int>) (tag: uint) (value: 'a) (writeValue: Span<byte> -> byref<int> -> 'a -> unit): unit =
        // Write tag
        writeUIntSpan span &offset (uint64 tag)
        
        // Write value
        writeValue span &offset value
    
    /// <summary>
    /// Writes an enum value as a uint
    /// </summary>
    /// <param name="buffer">The buffer to write to</param>
    /// <param name="value">The enum value to write</param>
    let inline writeEnum (buffer: Buffer<'T>) (value: uint64): unit =
        writeUInt buffer value
        
    /// <summary>
    /// Writes an enum value as a uint to a span
    /// </summary>
    /// <param name="span">The span to write to</param>
    /// <param name="offset">The starting offset (ref parameter updated with new position)</param>
    /// <param name="value">The enum value to write</param>
    let inline writeEnumSpan (span: Span<byte>) (offset: byref<int>) (value: uint64): unit =
        writeUIntSpan span &offset value