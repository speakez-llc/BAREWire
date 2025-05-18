namespace BAREWire.Core

open FSharp.UMX
open FSharp.NativeInterop

/// <summary>
/// Core memory types and operations
/// </summary>
[<AutoOpen>]
module Memory =
    /// <summary>
    /// Creates an int with offset measure
    /// </summary>
    let inline private ofInt<[<Measure>] 'u> (i: int) : int<'u> =
        i * 1<_>
    
    /// <summary>
    /// Converts a measured int to raw int
    /// </summary>
    let inline private toInt<[<Measure>] 'u> (i: int<'u>) : int =
        int i
        
    /// <summary>
    /// Adds two measured ints of the same measure
    /// </summary>
    let inline private addMeasured<[<Measure>] 'u> (a: int<'u>) (b: int<'u>) : int<'u> =
        ofInt<'u> (toInt a + toInt b)
        
    /// <summary>
    /// Multiplies an int by a measured int, maintaining the measure
    /// </summary>
    let inline private multiplyMeasured<[<Measure>] 'u> (a: int) (b: int<'u>) : int<'u> =
        ofInt<'u> (a * toInt b)
        
    /// <summary>
    /// Negates a measured int, maintaining the measure
    /// </summary>
    let inline private negativeMeasured<[<Measure>] 'u> (a: int<'u>) : int<'u> =
        ofInt<'u> (-toInt a)
        
    /// <summary>
    /// Compares two measured ints
    /// </summary>
    let inline private lessThanMeasured<[<Measure>] 'u> (a: int<'u>) (b: int<'u>) : bool =
        toInt a < toInt b
        
    /// <summary>
    /// Compares two measured ints
    /// </summary>
    let inline private greaterThanMeasured<[<Measure>] 'u> (a: int<'u>) (b: int<'u>) : bool =
        toInt a > toInt b
        
    /// <summary>
    /// A region of memory with type safety
    /// </summary>
    /// <typeparam name="'T">The type associated with this memory</typeparam>
    /// <typeparam name="'region">The memory region measure type</typeparam>
    [<Struct>]
    type Memory<'T, [<Measure>] 'region> =
        { /// <summary>The underlying data buffer</summary>
          Data: byte[]
          /// <summary>The starting offset within the buffer</summary>
          Offset: int<offset>
          /// <summary>The length in bytes</summary>
          Length: int<bytes> }
    
    /// <summary>
    /// A memory buffer that can be written to
    /// </summary>
    /// <typeparam name="'T">The type associated with this buffer</typeparam>
    [<Struct>]
    type Buffer<'T> =
        { /// <summary>The underlying data buffer</summary>
          Data: byte[]
          /// <summary>The current write position</summary>
          mutable Position: int<offset> }
        
        /// <summary>
        /// Writes a single byte to the buffer and advances the position
        /// </summary>
        /// <param name="value">The byte to write</param>
        member this.Write(value: byte): unit =
            this.Data.[toInt this.Position] <- value
            this.Position <- addMeasured this.Position (ofInt<offset> 1)
            
        /// <summary>
        /// Writes multiple bytes to the buffer and advances the position
        /// </summary>
        /// <param name="values">The bytes to write</param>
        member this.WriteBytes(values: byte[]): unit =
            for i = 0 to values.Length - 1 do
                this.Data.[toInt this.Position + i] <- values.[i]
            this.Position <- addMeasured this.Position (ofInt<offset> values.Length)
            
        /// <summary>
        /// Creates a new buffer with the specified capacity
        /// </summary>
        /// <param name="capacity">The buffer capacity in bytes</param>
        /// <returns>A new buffer instance</returns>
        static member Create(capacity: int): Buffer<'T> =
            { Data = Array.zeroCreate capacity
              Position = ofInt<offset> 0 }
    
    /// <summary>
    /// Creates a memory region from a byte array
    /// </summary>
    /// <param name="data">The source byte array</param>
    /// <returns>A new memory region containing the entire array</returns>
    let fromArray<'T, [<Measure>] 'region> (data: byte[]) : Memory<'T, 'region> =
        { Data = data
          Offset = ofInt<offset> 0
          Length = ofInt<bytes> data.Length }
          
    /// <summary>
    /// Creates a memory region from a slice of an existing byte array
    /// </summary>
    /// <param name="data">The source byte array</param>
    /// <param name="offset">The starting offset in the array</param>
    /// <param name="length">The number of bytes to include</param>
    /// <returns>A new memory region representing the specified slice</returns>
    let fromArraySlice<'T, [<Measure>] 'region> (data: byte[]) (offset: int) (length: int) : Memory<'T, 'region> =
        { Data = data
          Offset = ofInt<offset> offset
          Length = ofInt<bytes> length }
    
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
        
        if greaterThanMeasured count source.Length || greaterThanMeasured count destination.Length then
            failwith "Copy exceeds memory region bounds"
            
        let srcOffset = toInt source.Offset
        let dstOffset = toInt destination.Offset
        let copyCount = toInt count
        
        // Manual copy implementation without System dependencies
        for i = 0 to copyCount - 1 do
            destination.Data.[dstOffset + i] <- source.Data.[srcOffset + i]
    
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
             
        // Convert to raw ints for normal comparisons and calculations
        let rawOffset = toInt offset
        let rawLength = toInt length
        let memoryOffset = toInt memory.Offset
        let memoryLength = toInt memory.Length
         
        if rawOffset < 0 || rawLength < 0 || 
           rawOffset + rawLength > memoryLength then
            failwith "Slice exceeds memory region bounds"
            
        { Data = memory.Data
          Offset = addMeasured memory.Offset offset
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
        
        let rawOffset = toInt offset
        let memoryOffset = toInt memory.Offset
        let memoryLength = toInt memory.Length
                
        if rawOffset < 0 || rawOffset >= memoryLength then
            failwith "Offset out of bounds"
            
        memory.Data.[memoryOffset + rawOffset]
    
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
        
        let rawOffset = toInt offset
        let memoryOffset = toInt memory.Offset
        let memoryLength = toInt memory.Length
                 
        if rawOffset < 0 || rawOffset >= memoryLength then
            failwith "Offset out of bounds"
            
        memory.Data.[memoryOffset + rawOffset] <- value
    
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
        
        let rawOffset = toInt relativeOffset
        let memoryLength = toInt memory.Length
                  
        if rawOffset >= memoryLength then
            failwith "Address outside region bounds"
            
        { Offset = addMeasured memory.Offset relativeOffset }
        
    /// <summary>
    /// Fills a memory region with a specific byte value
    /// </summary>
    /// <param name="memory">The memory region to fill</param>
    /// <param name="value">The byte value to fill with</param>
    let fill<'T, [<Measure>] 'region>
                (memory: Memory<'T, 'region>)
                (value: byte) : unit =
        
        let memoryOffset = toInt memory.Offset
        let memoryLength = toInt memory.Length
        
        for i = 0 to memoryLength - 1 do
            memory.Data.[memoryOffset + i] <- value
        
    /// <summary>
    /// Clears a memory region (fills with zeros)
    /// </summary>
    /// <param name="memory">The memory region to clear</param>
    let clear<'T, [<Measure>] 'region>
                 (memory: Memory<'T, 'region>) : unit =
        fill memory 0uy
        
    /// <summary>
    /// Compares two memory regions for equality
    /// </summary>
    /// <param name="memory1">The first memory region</param>
    /// <param name="memory2">The second memory region</param>
    /// <returns>True if the regions contain the same bytes, false otherwise</returns>
    let equals<'T, 'U, [<Measure>] 'region1, [<Measure>] 'region2>
                  (memory1: Memory<'T, 'region1>)
                  (memory2: Memory<'U, 'region2>) : bool =
        
        let memory1Offset = toInt memory1.Offset
        let memory1Length = toInt memory1.Length
        let memory2Offset = toInt memory2.Offset
        let memory2Length = toInt memory2.Length
        
        if memory1Length <> memory2Length then false
        else
            let mutable result = true
            let mutable i = 0
            
            while result && i < memory1Length do
                if memory1.Data.[memory1Offset + i] <> memory2.Data.[memory2Offset + i] then
                    result <- false
                i <- i + 1
                
            result
            
    /// <summary>
    /// Creates a memory region from a native pointer
    /// </summary>
    /// <param name="ptr">The native pointer</param>
    /// <param name="size">The size in bytes</param>
    /// <returns>A view over the native memory</returns>
    let fromNativePointer<'T, [<Measure>] 'region> (ptr: nativeint) (size: int) : Memory<'T, 'region> =
        // This is a placeholder that would typically use a platform-specific API
        // In a real implementation, we would create a memory-mapped view of the native memory
        // For this example, we'll just create a byte array and copy the data
        let data = Array.zeroCreate size
        // Here we would typically copy memory from the native pointer
        // For now, just return an empty array
        { Data = data; Offset = ofInt<offset> 0; Length = ofInt<bytes> size }