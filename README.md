# BAREWire

BAREWire is a comprehensive F# library for efficient binary data encoding, memory mapping, and inter-process communication. It implements the [BARE (Binary Application Record Encoding)](https://baremessages.org/) protocol with zero-copy operations, strong type safety, and modular components for use in high-performance applications.

[![NuGet](https://img.shields.io/nuget/v/BAREWire.svg)](https://www.nuget.org/packages/BAREWire/)
[![License](https://img.shields.io/badge/license-Apache%202.0%20%2F%20Commercial-blue.svg)](LICENSE)

## Features

- **Zero Dependencies**: Pure F# implementation with no external dependencies (except FSharp.UMX for type safety)
- **Type Safety**: Leverages F#'s type system and units of measure for compile-time safety
- **Zero-Copy Operations**: Minimizes allocations for high-performance applications
- **Modular Design**: Use only what you need for your specific use case
- **Cross-Platform**: Works on all platforms supported by .NET

## Installation

```bash
dotnet add package BAREWire
```

## Core Concepts

BAREWire is built around these core concepts:

1. **Schemas**: Define the structure of your binary data using a type-safe DSL
2. **Encoding/Decoding**: Convert between F# values and their binary representations
3. **Memory Mapping**: Access structured data in memory without copying
4. **IPC**: Communicate between processes with shared memory, message queues, and named pipes
5. **Network Protocol**: Send and receive typed messages over different transports

## Quick Start

### 1. Define a Schema

```fsharp
open BAREWire.Schema.DSL
open BAREWire.Schema.Validation

// Define a schema for a simple message
let messageSchema =
    schema "Message"
    |> withType "UserId" string
    |> withType "Timestamp" int
    |> withType "Content" string
    |> withType "Message" (struct' [
        field "sender" (userType "UserId")
        field "timestamp" (userType "Timestamp")
        field "content" (userType "Content")
    ])
    |> validate // Validate the schema
    |> Result.get // Get the validated schema
```

### 2. Encode and Decode Data

```fsharp
open BAREWire.Core.Memory
open BAREWire.Encoding.Codec

// Define a message type
type Message = {
    Sender: string
    Timestamp: int64
    Content: string
}

// Encode a message
let encodeMessage (message: Message) =
    let buffer = Buffer<Message>.Create(1024)
    
    match encode messageSchema message buffer with
    | Ok () -> 
        let data = Array.init (int buffer.Position) (fun i -> buffer.Data.[i])
        Ok data
    | Error e -> Error e

// Decode a message
let decodeMessage (data: byte[]) =
    let memory = {
        Data = data
        Offset = 0<offset>
        Length = data.Length * 1<bytes>
    }
    
    match decode<Message> messageSchema memory with
    | Ok (message, _) -> Ok message
    | Error e -> Error e

// Usage example
let myMessage = { 
    Sender = "alice@example.com"
    Timestamp = 1645184400L 
    Content = "Hello, world!"
}

let encoded = encodeMessage myMessage |> Result.get
let decoded = decodeMessage encoded |> Result.get

printfn "Original: %A" myMessage
printfn "Decoded: %A" decoded
```

### 3. Memory Mapping

```fsharp
open BAREWire.Memory.Region
open BAREWire.Memory.View

// Create a memory region for a message
let createMessageRegion (message: Message) =
    let data = encodeMessage message |> Result.get
    let region = create<Message, region> data
    Ok region

// Read a message from a memory region
let readMessage (region: Region<Message, region>) =
    let view = View.create<Message, region> region messageSchema
    View.getField<Message, Message, region> view ["Message"]

// Usage example
let region = createMessageRegion myMessage |> Result.get
let readBack = readMessage region |> Result.get

printfn "Read from memory: %A" readBack
```

### 4. IPC with Shared Memory

```fsharp
open BAREWire.IPC.SharedMemory

// Create a shared memory region
let createSharedMessage (name: string) (message: Message) =
    let data = encodeMessage message |> Result.get
    let size = data.Length * 1<bytes>
    
    // Create a shared memory region
    match create<Message> name size messageSchema with
    | Ok shared ->
        // Create a view over the shared region
        let view = getView<Message, region> shared messageSchema
        
        // Set the message in the shared memory
        match View.setField<Message, Message, region> view ["Message"] message with
        | Ok () -> Ok shared
        | Error e -> Error e
    | Error e -> Error e

// Open an existing shared memory region
let openSharedMessage (name: string) =
    match open'<Message> name messageSchema with
    | Ok shared ->
        // Create a view over the shared region
        let view = getView<Message, region> shared messageSchema
        
        // Get the message from the shared memory
        View.getField<Message, Message, region> view ["Message"]
    | Error e -> Error e

// Usage example (in Process 1)
let shared = createSharedMessage "test-message" myMessage |> Result.get

// Usage example (in Process 2)
let received = openSharedMessage "test-message" |> Result.get
printfn "Received via shared memory: %A" received
```

### 5. Network Communication

```fsharp
open BAREWire.Network.Transport
open BAREWire.Network.Protocol

// Create a protocol client
let createClient (host: string) (port: int) =
    let transport = Tcp.createClient host port defaultConfig
    createClient<Message> transport messageSchema

// Send a message
let sendMessage (client: Client<Message>) (message: Message) =
    send client message

// Receive a message
let receiveMessage (client: Client<Message>) =
    receive client

// Usage example (server)
let server = 
    let transport = Tcp.createServer 8080 defaultConfig
    createServer<Message> transport messageSchema (fun msg -> Ok ())
    |> Result.get
    
let _ = start server

// Usage example (client)
let client = createClient "localhost" 8080 |> Result.get
let _ = sendMessage client myMessage
```

## Advanced Features

### Units of Measure for Type Safety

BAREWire leverages F#'s units of measure and FSharp.UMX to provide strong type safety:

```fsharp
open FSharp.UMX
open BAREWire.Core

// Define measure types
[<Measure>] type userId
[<Measure>] type timestamp
[<Measure>] type messageId

// Type-safe message with units of measure
type SafeMessage = {
    Id: string<messageId>
    Sender: string<userId>
    Timestamp: int64<timestamp>
    Content: string
}

// Decode with measures
let decodeSafeMessage (data: byte[]) =
    let memory = {
        Data = data
        Offset = 0<offset>
        Length = data.Length * 1<bytes>
    }
    
    decodeWithMeasure<SafeMessage, messageId> messageSchema memory
```

### Schema Compatibility

BAREWire provides tools to check compatibility between schema versions:

```fsharp
open BAREWire.Schema.Analysis

// Check if schemas are compatible
let oldSchema = // ...
let newSchema = // ...

match checkCompatibility oldSchema newSchema with
| FullyCompatible -> 
    printfn "Schemas are fully compatible"
| BackwardCompatible -> 
    printfn "New schema can read old data"
| ForwardCompatible -> 
    printfn "Old schema can read new data"
| Incompatible reasons -> 
    printfn "Schemas are incompatible: %A" reasons
```

## Architecture

BAREWire is designed as a modular system with several core components that work together:

```
src/
├── Core/
│   ├── Binary.fs           # Binary conversion utilities
│   ├── Error.fs            # Error handling types
│   ├── Memory.fs           # Memory representation and operations
│   ├── Time.fs             # Time utilities and functions
│   ├── Types.fs            # Core type definitions and measures
│   ├── Utf8.fs             # UTF-8 encoding/decoding
│   └── Uuid.fs             # UUID generation and handling
├── Encoding/
│   ├── Codec.fs            # Combined encoding/decoding operations
│   ├── Decoder.fs          # Decoding primitives
│   └── Encoder.fs          # Encoding primitives
├── IPC/
│   ├── MessageQueue.fs     # Message queues for IPC
│   ├── NamedPipe.fs        # Named pipes for IPC
│   └── SharedMemory.fs     # Shared memory regions
├── Memory/
│   ├── Mapping.fs          # Memory mapping functions
│   ├── Region.fs           # Memory region operations
│   ├── SafeMemory.fs       # Safe memory operations
│   └── View.fs             # Memory view operations
├── Network/
│   ├── Frame.fs            # Frame format for binary communication
│   ├── Protocol.fs         # Message passing primitives
│   └── Transport.fs        # Transport abstractions
├── Platform/
│   ├── PlatformServices.fs # Platform service registration
│   ├── Common/
│   │   ├── Interfaces.fs   # Platform abstraction interfaces
│   │   ├── Registry.fs     # Platform provider registry
│   │   └── Resource.fs     # Resource management
│   └── Providers/
│       ├── Android.fs      # Android-specific implementations
│       ├── InMemory.fs     # In-memory simulation implementations
│       ├── iOS.fs          # iOS-specific implementations
│       ├── Linux.fs        # Linux-specific implementations
│       ├── MacOS.fs        # macOS-specific implementations
│       ├── WebAssembly.fs  # WebAssembly implementations
│       └── Windows.fs      # Windows-specific implementations
└── Schema/
    ├── Analysis.fs         # Schema analysis tools
    ├── Definition.fs       # Schema type definitions
    ├── DSL.fs              # Domain-specific language for schema definition
    └── Validation.fs       # Schema validation logic
```

## Cross-Platform Support

BAREWire is designed to work across multiple platforms through its platform abstraction layer:

- **Windows**: Full support for shared memory, named pipes, and TCP/IP networking
- **Linux**: Native implementations for shared memory, FIFOs, and sockets
- **macOS**: Darwin-specific implementations for IPC and networking
- **Android**: Custom implementations for Android's shared memory and IPC mechanisms
- **iOS**: iOS-specific memory management and IPC
- **WebAssembly**: Browser-compatible implementations using SharedArrayBuffer where available

The platform-specific code is isolated in the `Platform/Providers` directory, allowing the core library to remain platform-agnostic while still providing optimized implementations for each target environment.

## Performance

BAREWire is designed for high performance with minimal allocations:

- Zero-copy operations where possible
- Minimal memory footprint
- Efficient binary encoding/decoding
- Type-safe operations without runtime overhead

## License

BAREWire is dual-licensed under both the Apache License 2.0 and a Commercial License.

### Open Source License

For most users, including:
- Open source projects
- Academic and educational use
- Non-commercial applications
- Personal projects and experimentation
- Internal tools that are not part of your commercial product

You can use BAREWire under the **Apache License 2.0** with no additional requirements. The Apache License 2.0 is a permissive open source license that allows you to use, modify, and distribute the software with minimal restrictions.

### Commercial License

A Commercial License is required if you are:
- Incorporating BAREWire into a commercial product or service that you sell to customers
- Using BAREWire as part of your company's commercial offering
- Distributing BAREWire as part of a proprietary software solution
- Requiring professional support, indemnification, or custom development

The Commercial License provides additional rights and benefits not available under the Apache License 2.0, including explicit patent rights, professional support options, and freedom from certain open source requirements.

For information about obtaining a Commercial License, please see [Commercial.md](Commercial.md) or contact us directly.

### Patent Notice

BAREWire includes technology covered by U.S. Patent Application No. 63/786,247 "System and Method for Zero-Copy Inter-Process Communication Using BARE Protocol". For details on patent licensing under both the Apache and Commercial licenses, please see [PATENTS.md](PATENTS.md).

## Contributing

Contributions are welcome! By submitting a pull request, you agree to license your contributions under the same license terms as the project (dual Apache 2.0 and Commercial License).

For major changes or new features, please open an issue first to discuss what you would like to change.