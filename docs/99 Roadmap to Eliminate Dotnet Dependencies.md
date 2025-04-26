# Implementation Roadmap: Eliminating .NET Dependencies in BAREWire

## Phase 1: Core Utility Implementations

### 1.1 Create Utility Modules (New Files)

#### src/Core/Utf8.fs
```fsharp
namespace BAREWire.Core

/// Pure F# UTF-8 encoding/decoding implementation
module Utf8 =
    /// Encodes a string to UTF-8 bytes
    let getBytes (s: string) : byte[] =
        // Implementation from previous memo

    /// Decodes UTF-8 bytes to string
    let getString (bytes: byte[]) : string =
        // Implementation from previous memo
```

#### src/Core/Binary.fs
```fsharp
namespace BAREWire.Core

open FSharp.NativeInterop

/// Pure F# binary conversion utilities
module Binary =
    /// Convert from float32 to Int32 bits
    let singleToInt32Bits (value: float32) : int32 =
        // Implementation from previous memo

    /// Convert from Int32 bits to float32
    let int32BitsToSingle (value: int32) : float32 =
        // Implementation from previous memo

    /// Convert from float to Int64 bits
    let doubleToInt64Bits (value: float) : int64 =
        // Implementation from previous memo

    /// Convert from Int64 bits to float
    let int64BitsToDouble (value: int64) : float =
        // Implementation from previous memo

    /// Get bytes from UInt32
    let getBytes (value: uint32) : byte[] =
        // Implementation from previous memo

    /// Convert bytes to UInt32
    let toUInt32 (bytes: byte[]) (startIndex: int) : uint32 =
        // Implementation from previous memo
```

#### src/Core/Uuid.fs
```fsharp
namespace BAREWire.Core

open BAREWire.Platform

/// Pure F# UUID implementation (RFC 4122)
module Uuid =
    /// A 128-bit UUID
    type Uuid = { Data: byte[] }  // 16 bytes
    
    /// Creates a new UUID (v4 random)
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
    
    /// Converts UUID to string representation
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
    
    /// Parse a UUID from string
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
```

#### src/Core/Time.fs
```fsharp
namespace BAREWire.Core

open BAREWire.Platform

/// Time utilities
module Time =
    /// Gets the current Unix timestamp (seconds since 1970-01-01)
    let currentUnixTimestamp () : int64 =
        // Get platform-specific time provider
        let provider = PlatformServices.getTimeProvider() |> Result.get
        provider.GetUnixTimestamp()
```

### 1.2 Extend Platform Abstractions

#### src/Platform/Common/Interfaces.fs (Add)
```fsharp
/// Random number provider
type IRandomProvider =
    /// Fills the specified buffer with random bytes
    abstract GetBytes: buffer:byte[] -> unit
    
    /// Gets a random integer in the specified range
    abstract GetInt32: minValue:int -> maxValue:int -> int

/// Time provider
type ITimeProvider =
    /// Gets the current Unix timestamp (seconds since 1970-01-01)
    abstract GetUnixTimestamp: unit -> int64
    
    /// Gets the current time in ticks (implementation-defined unit)
    abstract GetCurrentTicks: unit -> int64
```

#### src/Platform/PlatformServices.fs (Add Functions)
```fsharp
/// Gets the random provider for the current platform
let getRandomProvider () : Result<IRandomProvider> =
    ensureInitialized defaultInitTimeout
    |> Result.bind (fun _ ->
        match currentPlatform with
        | Some platform -> 
            match platform.RandomProvider with
            | Some provider -> Ok provider
            | None -> Error (invalidStateError "Random provider not available")
        | None -> Error (invalidStateError "Platform not initialized")
    )

/// Gets the time provider for the current platform
let getTimeProvider () : Result<ITimeProvider> =
    ensureInitialized defaultInitTimeout
    |> Result.bind (fun _ ->
        match currentPlatform with
        | Some platform -> 
            match platform.TimeProvider with
            | Some provider -> Ok provider
            | None -> Error (invalidStateError "Time provider not available")
        | None -> Error (invalidStateError "Platform not initialized")
    )
```

#### src/Platform/Common/PlatformType.fs (Update)
```fsharp
/// Platform type with all providers
type Platform = {
    // Existing providers
    MemoryProvider: IMemoryProvider option
    IpcProvider: IIpcProvider option
    NetworkProvider: INetworkProvider option
    
    // New providers
    RandomProvider: IRandomProvider option
    TimeProvider: ITimeProvider option
}
```

## Phase 2: Update Core Files (Remove Dependencies)

### 2.1 Update Encoder/Decoder

#### src/Encoding/Encoder.fs
```diff
- open System.Text.Encoding
+ open BAREWire.Core.Utf8
+ open BAREWire.Core.Binary

  let writeString (buffer: Buffer<'T>) (value: string): unit =
      // Get UTF-8 bytes
-     let bytes = Encoding.UTF8.GetBytes(value)
+     let bytes = getBytes value
      // Write length as uint
      writeUInt buffer (uint64 bytes.Length)
      // Write bytes
      buffer.WriteSpan(ReadOnlySpan(bytes))

  let writeF32 (buffer: Buffer<'T>) (value: float32): unit =
-     let bits = BitConverter.SingleToInt32Bits(value)
+     let bits = singleToInt32Bits value
      writeI32 buffer bits
      
  let writeF64 (buffer: Buffer<'T>) (value: float): unit =
-     let bits = BitConverter.DoubleToInt64Bits(value)
+     let bits = doubleToInt64Bits value
      writeI64 buffer bits
```

#### src/Encoding/Decoder.fs
```diff
- open System.Text.Encoding
+ open BAREWire.Core.Utf8
+ open BAREWire.Core.Binary

  let readString (memory: Memory<'T, 'region>) (offset: int<offset>): string * int<offset> =
      // Read length
      let length, currentOffset = readUInt memory offset
      
      // Read bytes
      let bytes = Array.init (int length) (fun i -> 
          memory.Data.[int (memory.Offset + currentOffset) + i])
-     let str = Encoding.UTF8.GetString(bytes)
+     let str = getString bytes
      
      str, currentOffset + (int length * 1<offset>)

  let readF32 (memory: Memory<'T, 'region>) (offset: int<offset>): float32 * int<offset> =
      let bits, newOffset = readI32 memory offset
-     let value = BitConverter.Int32BitsToSingle(bits)
+     let value = int32BitsToSingle bits
      value, newOffset
  
  let readF64 (memory: Memory<'T, 'region>) (offset: int<offset>): float * int<offset> =
      let bits, newOffset = readI64 memory offset
-     let value = BitConverter.Int64BitsToDouble(bits)
+     let value = int64BitsToDouble bits
      value, newOffset
```

### 2.2 Update Network/Frame.fs
```diff
- open System.BitConverter
+ open BAREWire.Core.Binary

  // Write payload length
- let lengthBytes = BitConverter.GetBytes(frame.Header.PayloadLength)
+ let lengthBytes = getBytes frame.Header.PayloadLength
  Array.Copy(lengthBytes, 0, buffer, !position, 4)

  // Read payload length
  if data.Length < !position + 4 then
      Error (decodingError "Frame too small for payload length")
  else
      let lengthBytes = data.[!position..(!position + 3)]
-     let payloadLength = BitConverter.ToUInt32(lengthBytes, 0)
+     let payloadLength = toUInt32 lengthBytes 0
      position := !position + 4
```

### 2.3 Update IPC and Message Classes

#### src/IPC/MessageQueue.fs
```diff
- open System
+ open BAREWire.Core.Uuid
+ open BAREWire.Core.Time

  // Create the message
  let message = {
-     Id = Guid.NewGuid()
+     Id = newUuid()
      Type = msgType
      Payload = payload
-     Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
+     Timestamp = currentUnixTimestamp()
  }
```

#### src/Network/Protocol.fs
```diff
- open System
+ open BAREWire.Core.Uuid
+ open BAREWire.Core.Time

  // Create the request
  let request = {
-     Id = Guid.NewGuid()
+     Id = newUuid()
      Method = method
      Params = params
  }
```

## Phase 3: Memory Operations

### 3.1 Create Safe Memory Operations

#### src/Memory/SafeMemory.fs (New File)
```fsharp
namespace BAREWire.Memory

open FSharp.NativeInterop
open BAREWire.Core

/// Safe memory operations using F# native interop
module SafeMemory =
    /// Safely pins memory and executes a function with access to the pinned data
    let withPinnedData<'T> (data: byte[]) (offset: int) (action: nativeint -> 'T) : 'T =
        // Use 'fixed' to pin the array in place during the operation
        use pinHandle = fixed data
        
        // Calculate the address with offset
        let baseAddr = NativePtr.toNativeInt pinHandle
        let offsetAddr = baseAddr + nativeint offset
        
        // Execute the action with the address
        action offsetAddr
    
    /// Safely reads a value of type 'T from memory
    let readUnmanaged<'T when 'T : unmanaged> (data: byte[]) (offset: int) : 'T =
        withPinnedData data offset (fun addr ->
            let ptr = NativePtr.ofNativeInt<'T> addr
            NativePtr.read ptr
        )
    
    /// Safely writes a value of type 'T to memory
    let writeUnmanaged<'T when 'T : unmanaged> (data: byte[]) (offset: int) (value: 'T) : unit =
        withPinnedData data offset (fun addr ->
            let ptr = NativePtr.ofNativeInt<'T> addr
            NativePtr.write ptr value
        )
    
    /// Safely reads a byte from memory
    let readByte (data: byte[]) (offset: int) : byte =
        if offset < 0 || offset >= data.Length then
            failwith "Offset out of bounds"
        
        data.[offset]
    
    /// Safely writes a byte to memory
    let writeByte (data: byte[]) (offset: int) (value: byte) : unit =
        if offset < 0 || offset >= data.Length then
            failwith "Offset out of bounds"
        
        data.[offset] <- value
    
    /// Safely copies memory between arrays
    let copyMemory (source: byte[]) (sourceOffset: int) (dest: byte[]) (destOffset: int) (count: int) : unit =
        if sourceOffset < 0 || sourceOffset + count > source.Length ||
           destOffset < 0 || destOffset + count > dest.Length then
            failwith "Memory copy out of bounds"
        
        for i = 0 to count - 1 do
            dest.[destOffset + i] <- source.[sourceOffset + i]
```

### 3.2 Update Memory View Operations

#### src/Memory/View.fs
```diff
- open System.Runtime.InteropServices
+ open BAREWire.Memory.SafeMemory

  let getField<'T, 'Field, [<Measure>] 'region> 
              (view: MemoryView<'T, 'region>) 
              (fieldPath: FieldPath): Result<'Field> =
      resolveFieldPath view fieldPath
      |> Result.bind (fun fieldOffset ->
          try
              // Create a memory slice for the field
              let fieldMemory = {
                  Data = view.Memory.Data
                  Offset = view.Memory.Offset + fieldOffset.Offset
                  Length = min fieldOffset.Size (view.Memory.Length - fieldOffset.Offset)
              }
              
-             // Pin the array to ensure GC doesn't move it during native operations
-             let handle = GCHandle.Alloc(fieldMemory.Data, GCHandleType.Pinned)
              try
-                 // Get a pointer to the field memory
-                 let basePtr = handle.AddrOfPinnedObject()
-                 let fieldPtr = IntPtr.Add(basePtr, int fieldMemory.Offset)
                  
                  // Based on the field type, marshal or decode the value
                  match fieldOffset.Type with
                  | Primitive primType ->
                      match primType with
                      | U8 -> 
-                         let value = Marshal.ReadByte(fieldPtr)
+                         let value = readByte fieldMemory.Data (int fieldMemory.Offset)
                          Ok (box value :?> 'Field)
                          
                      | U16 -> 
-                         let value = Marshal.ReadInt16(fieldPtr)
+                         let value = readUnmanaged<int16> fieldMemory.Data (int fieldMemory.Offset)
                          Ok (box (uint16 value) :?> 'Field)
                          
                      | U32 -> 
-                         let value = Marshal.ReadInt32(fieldPtr)
+                         let value = readUnmanaged<int32> fieldMemory.Data (int fieldMemory.Offset)
                          Ok (box (uint32 value) :?> 'Field)
                          
                      | U64 -> 
-                         let value = Marshal.ReadInt64(fieldPtr)
+                         let value = readUnmanaged<int64> fieldMemory.Data (int fieldMemory.Offset)
                          Ok (box (uint64 value) :?> 'Field)
                          
                      | I8 -> 
-                         let value = Marshal.ReadByte(fieldPtr)
+                         let value = readByte fieldMemory.Data (int fieldMemory.Offset)
                          Ok (box (sbyte value) :?> 'Field)
                          
                      | I16 -> 
-                         let value = Marshal.ReadInt16(fieldPtr)
+                         let value = readUnmanaged<int16> fieldMemory.Data (int fieldMemory.Offset)
                          Ok (box value :?> 'Field)
                          
                      | I32 -> 
-                         let value = Marshal.ReadInt32(fieldPtr)
+                         let value = readUnmanaged<int32> fieldMemory.Data (int fieldMemory.Offset)
                          Ok (box value :?> 'Field)
                          
                      | I64 -> 
-                         let value = Marshal.ReadInt64(fieldPtr)
+                         let value = readUnmanaged<int64> fieldMemory.Data (int fieldMemory.Offset)
                          Ok (box value :?> 'Field)
                          
                      | F32 -> 
-                         let value = Marshal.PtrToStructure<float32>(fieldPtr)
+                         let value = readUnmanaged<float32> fieldMemory.Data (int fieldMemory.Offset)
                          Ok (box value :?> 'Field)
                          
                      | F64 -> 
-                         let value = Marshal.PtrToStructure<float>(fieldPtr)
+                         let value = readUnmanaged<float> fieldMemory.Data (int fieldMemory.Offset)
                          Ok (box value :?> 'Field)
                          
                      | Bool -> 
-                         let value = Marshal.ReadByte(fieldPtr) <> 0uy
+                         let value = readByte fieldMemory.Data (int fieldMemory.Offset) <> 0uy
                          Ok (box value :?> 'Field)
                          
                      | String ->
                          // String is stored as length + bytes
-                         let lengthPtr = fieldPtr
-                         let length = Marshal.ReadInt32(lengthPtr)
+                         let length = readUnmanaged<int32> fieldMemory.Data (int fieldMemory.Offset)
                          
                          if length > 0 then
-                             let stringPtr = IntPtr.Add(lengthPtr, 4)
-                             let bytes = Array.zeroCreate length
-                             Marshal.Copy(stringPtr, bytes, 0, length)
-                             let value = System.Text.Encoding.UTF8.GetString(bytes)
+                             let bytes = Array.init length (fun i -> 
+                                 readByte fieldMemory.Data (int fieldMemory.Offset + 4 + i))
+                             let value = Utf8.getString bytes
                              Ok (box value :?> 'Field)
                          else
                              Ok (box "" :?> 'Field)
                              
              finally
-                 if handle.IsAllocated then handle.Free()
+                 ()
          with ex ->
              Error (decodingError $"Failed to decode field {String.concat "." fieldPath}: {ex.Message}")
      )
```

Apply similar changes to the `setField` function, replacing Marshal calls with SafeMemory functions.

## Phase 4: Platform Specific Implementations

### 4.1 Update Platform Provider Interfaces

#### src/Platform/Providers/WindowsProvider.fs
Implement `IRandomProvider` and `ITimeProvider` for Windows using P/Invoke to Win32 APIs.

#### src/Platform/Providers/LinuxProvider.fs
Implement `IRandomProvider` and `ITimeProvider` for Linux using P/Invoke to POSIX functions.

#### src/Platform/Providers/MacOSProvider.fs
Implement `IRandomProvider` and `ITimeProvider` for macOS using P/Invoke to Darwin APIs.

#### src/Platform/Providers/WebAssemblyProvider.fs
```fsharp
/// WebAssembly random provider
type WebRandomProvider() =
    interface IRandomProvider with
        member _.GetBytes(buffer: byte[]) =
            // Use JavaScript crypto.getRandomValues API
            JS.Constructors.Crypto.getRandomValues(buffer)
            
        member _.GetInt32(minValue: int, maxValue: int) =
            // Use JavaScript Math.random
            let random = JS.Math.random()
            let range = maxValue - minValue
            minValue + int (random * float range)

/// WebAssembly time provider
type WebTimeProvider() =
    interface ITimeProvider with
        member _.GetUnixTimestamp() =
            // Use JavaScript Date.now() / 1000
            int64 (JS.Date.now() / 1000.0)
            
        member _.GetCurrentTicks() =
            // Use performance.now() * 10000 to match .NET ticks
            int64 (JS.Performance.now() * 10000.0)
```

## Phase 5: Project Structure Updates

### 5.1 Update Project File

#### src/BAREWire.fsproj
```diff
  <ItemGroup>
    <!-- Core -->
    <Compile Include="Core/Types.fs" />
    <Compile Include="Core/Memory.fs" />
    <Compile Include="Core/Error.fs" />
+   <Compile Include="Core/Utf8.fs" />
+   <Compile Include="Core/Binary.fs" />
+   <Compile Include="Core/Uuid.fs" />
+   <Compile Include="Core/Time.fs" />
    
    <!-- Memory -->
+   <Compile Include="Memory/SafeMemory.fs" />
    <Compile Include="Memory/Region.fs" />
    <Compile Include="Memory/View.fs" />
    <Compile Include="Memory/Mapping.fs" />
    
    <!-- Platform Providers -->
    <Compile Include="Platform/Common/Interfaces.fs" />
    <Compile Include="Platform/Common/PlatformType.fs" />
    <Compile Include="Platform/PlatformServices.fs" />
    <Compile Include="Platform/Providers/WindowsProvider.fs" />
    <Compile Include="Platform/Providers/LinuxProvider.fs" />
    <Compile Include="Platform/Providers/MacOSProvider.fs" />
    <Compile Include="Platform/Providers/WebAssemblyProvider.fs" />
  </ItemGroup>
```

## Implementation Priorities and Sequence

1. **Core Utilities (Phase 1)**
   - Create basic utilities with pure F# implementations
   - Extend platform abstractions to support time and random operations

2. **Update Core Encoding/Decoding (Phase 2)**
   - Replace System.Text.Encoding with custom Utf8 module
   - Replace BitConverter with custom Binary module

3. **Safe Memory Operations (Phase 3)**
   - Implement SafeMemory module
   - Update memory access in View.fs to use safe operations

4. **Platform-Specific Implementations (Phase 4)**
   - Implement new provider interfaces for each platform
   - Update platform services to expose new providers

5. **Project Structure (Phase 5)**
   - Update project file to include new modules
   - Ensure correct compilation order

## Testing Strategy

1. Create unit tests for each new pure F# implementation
2. Verify against existing .NET implementations
3. Test platform-specific implementations on each target platform
4. Create integration tests that verify end-to-end functionality

## Conclusion

This roadmap provides a clear path to eliminate .NET dependencies from BAREWire while preserving its current structure. By focusing on discrete phases and leveraging the existing platform abstraction layer, we can safely migrate away from .NET APIs to pure F# implementations that can be compiled through the Fidelity Framework's MLIR/LLVM path.