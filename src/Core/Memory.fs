namespace BAREWire.Core

open BAREWire.Core

/// <summary>
/// Core memory types and operations
/// </summary>
[<AutoOpen>]
module Memory =
    /// <summary>
    /// A region of memory with type safety
    /// </summary>
    /// <typeparam name="'T">The type associated with this memory</typeparam>
    /// <typeparam name="'region">The memory region measure type</typeparam>
    type Memory<'T, [<Measure>] 'region> =
        { Data: byte[]
          Offset: int<offset>
          Length: int<bytes> }
        
    /// <summary>
    /// A memory buffer that can be written to
    /// </summary>
    /// <typeparam name="'T">The type associated with this buffer</typeparam>
    type Buffer<'T> =
        { Data: byte[]
          mutable Position: int<offset> }
        
        /// <summary>
        /// Writes a single byte to the buffer and advances the position
        /// </summary>
        /// <param name="value">The byte to write</param>
        member inline this.Write(value: byte): unit =
            this.Data.[int this.Position] <- value
            this.Position <- this.Position + 1<offset>
            
        /// <summary>
        /// Writes a span of bytes to the buffer and advances the position
        /// </summary>
        /// <param name="span">The span of bytes to write</param>
        member inline this.WriteSpan(span: ReadOnlySpan<byte>): unit =
            for i = 0 to span.Length - 1 do
                this.Data.[int this.Position + i] <- span.[i]
            this.Position <- this.Position + (span.Length * 1<offset>)
            
        /// <summary>
        /// Creates a new buffer with the specified capacity
        /// </summary>
        /// <param name="capacity">The buffer capacity in bytes</param>
        /// <returns>A new buffer instance</returns>
        static member Create(capacity: int): Buffer<'T> =
            { Data = Array.zeroCreate capacity
              Position = 0<offset> }
    
    /// <summary>
    /// Creates a memory region from a byte array
    /// </summary>
    /// <param name="data">The source byte array</param>
    /// <returns>A new memory region containing the entire array</returns>
    let fromArray<'T, [<Measure>] 'region> (data: byte[]) : Memory<'T, 'region> =
        { Data = data
          Offset = 0<offset>
          Length = data.Length * 1<bytes> }
          
    /// <summary>
    /// Creates a memory region from a slice of an existing byte array
    /// </summary>
    /// <param name="data">The source byte array</param>
    /// <param name="offset">The starting offset in the array</param>
    /// <param name="length">The number of bytes to include</param>
    /// <returns>A new memory region representing the specified slice</returns>
    let fromArraySlice<'T, [<Measure>] 'region> (data: byte[]) (offset: int) (length: int) : Memory<'T, 'region> =
        { Data = data
          Offset = offset * 1<offset>
          Length = length * 1<bytes> }
    
    /// <summary>
    /// Copies data from one memory region to another
    /// </summary>
    /// <param name="source">The source memory region</param>
    /// <param name="destination">The destination memory region</param>
    /// <param name="count">The number of bytes to copy</param>
    /// <exception cref="System.Exception">Thrown when copy exceeds memory region bounds</exception>
    let copy<'T, 'U, [<Measure>] 'region1, [<Measure>] 'region2> 
            (source: Memory<'T, 'region1>) 
            (destination: Memory<'U, 'region2>) 
            (count: int<bytes>) : unit =
        
        if count > source.Length || count > destination.Length then
            failwith "Copy exceeds memory region bounds"
            
        Array.Copy(
            source.Data, int source.Offset, 
            destination.Data, int destination.Offset, 
            int count)
    
    /// <summary>
    /// Creates a slice of a memory region
    /// </summary>
    /// <param name="memory">The source memory region</param>
    /// <param name="offset">The starting offset within the region</param>
    /// <param name="length">The number of bytes to include in the slice</param>
    /// <returns>A new memory region representing the specified slice</returns>
    /// <exception cref="System.Exception">Thrown when slice exceeds memory region bounds</exception>
    let slice<'T, 'U, [<Measure>] 'region> 
             (memory: Memory<'T, 'region>) 
             (offset: int<offset>) 
             (length: int<bytes>) : Memory<'U, 'region> =
             
        if offset < 0<offset> || length < 0<bytes> || 
           offset + length > memory.Length then
            failwith "Slice exceeds memory region bounds"
            
        { Data = memory.Data
          Offset = memory.Offset + offset
          Length = length }
    
    /// <summary>
    /// Reads a byte from memory at the specified offset
    /// </summary>
    /// <param name="memory">The memory region to read from</param>
    /// <param name="offset">The offset within the region</param>
    /// <returns>The byte value at the specified offset</returns>
    /// <exception cref="System.Exception">Thrown when offset is out of bounds</exception>
    let readByte<'T, [<Measure>] 'region> 
                (memory: Memory<'T, 'region>) 
                (offset: int<offset>) : byte =
                
        if offset < 0<offset> || offset >= memory.Length then
            failwith "Offset out of bounds"
            
        memory.Data.[int memory.Offset + int offset]
    
    /// <summary>
    /// Writes a byte to memory at the specified offset
    /// </summary>
    /// <param name="memory">The memory region to write to</param>
    /// <param name="offset">The offset within the region</param>
    /// <param name="value">The byte value to write</param>
    /// <exception cref="System.Exception">Thrown when offset is out of bounds</exception>
    let writeByte<'T, [<Measure>] 'region> 
                 (memory: Memory<'T, 'region>) 
                 (offset: int<offset>) 
                 (value: byte) : unit =
                 
        if offset < 0<offset> || offset >= memory.Length then
            failwith "Offset out of bounds"
            
        memory.Data.[int memory.Offset + int offset] <- value
    
    /// <summary>
    /// Gets the absolute address of a location within a region
    /// </summary>
    /// <param name="memory">The memory region</param>
    /// <param name="relativeOffset">The relative offset within the region</param>
    /// <returns>An address representing the absolute location</returns>
    /// <exception cref="System.Exception">Thrown when address is outside region bounds</exception>
    let getAddress<'T, [<Measure>] 'region> 
                  (memory: Memory<'T, 'region>) 
                  (relativeOffset: int<offset>) : Address<'region> =
                  
        if int relativeOffset >= int memory.Length then
            failwith "Address outside region bounds"
            
        { Offset = memory.Offset + relativeOffset }