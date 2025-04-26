namespace BAREWire.Platform.Common

open BAREWire.Core.Error
open BAREWire.Platform.Common.Interfaces
open BAREWire.Core.Memory

/// <summary>
/// Platform detection and provider registry
/// </summary>
module Registry =
    /// <summary>
    /// Platform type enumeration
    /// </summary>
    type PlatformType =
        /// <summary>Microsoft Windows platform</summary>
        | Windows = 0
        /// <summary>Linux platform</summary>
        | Linux = 1
        /// <summary>macOS platform</summary>
        | MacOS = 2
        /// <summary>Android platform</summary>
        | Android = 3
        /// <summary>iOS platform</summary>
        | iOS = 4
        /// <summary>WebAssembly platform</summary>
        | WebAssembly = 5
        /// <summary>In-memory simulation (no actual platform)</summary>
        | InMemory = 6
    
    /// <summary>
    /// Provider type enumeration
    /// </summary>
    type ProviderType =
        /// <summary>Memory operations provider</summary>
        | Memory = 0
        /// <summary>IPC operations provider</summary>
        | IPC = 1
        /// <summary>Network operations provider</summary>
        | Network = 2
        /// <summary>Synchronization operations provider</summary>
        | Sync = 3
    
    /// <summary>
    /// Provider registry entry
    /// </summary>
    type ProviderEntry<'TProvider> = {
        /// <summary>Provider type</summary>
        ProviderType: ProviderType
        
        /// <summary>Platform type</summary>
        PlatformType: PlatformType
        
        /// <summary>The provider implementation</summary>
        Provider: 'TProvider
    }
    
    /// <summary>
    /// Provider registry for mapping platform-specific provider implementations
    /// </summary>
    [<RequireQualifiedAccess>]
    module private Providers =
        /// <summary>Memory providers registry</summary>
        let mutable memoryProviders: ProviderEntry<IPlatformMemory>[] = [||]
        
        /// <summary>IPC providers registry</summary>
        let mutable ipcProviders: ProviderEntry<IPlatformIpc>[] = [||]
        
        /// <summary>Network providers registry</summary>
        let mutable networkProviders: ProviderEntry<IPlatformNetwork>[] = [||]
        
        /// <summary>Synchronization providers registry</summary>
        let mutable syncProviders: ProviderEntry<IPlatformSync>[] = [||]
        
        /// <summary>Current platform type</summary>
        let mutable currentPlatform = PlatformType.InMemory
    
    /// <summary>
    /// Initializes the provider registries with the given maximum capacity
    /// </summary>
    /// <param name="capacity">Maximum number of providers per registry</param>
    let initializeRegistries (capacity: int): unit =
        Providers.memoryProviders <- Array.zeroCreate capacity
        Providers.ipcProviders <- Array.zeroCreate capacity
        Providers.networkProviders <- Array.zeroCreate capacity
        Providers.syncProviders <- Array.zeroCreate capacity
    
    /// <summary>
    /// Gets the current platform type based on compilation symbols and runtime detection
    /// </summary>
    /// <returns>The detected platform type</returns>
    let getCurrentPlatform (): PlatformType =
        // Return cached platform if already detected
        if Providers.currentPlatform <> PlatformType.InMemory then
            Providers.currentPlatform
        else
            // Detect platform based on compile-time symbols and runtime checks
            let platform =
                #if WINDOWS
                    PlatformType.Windows
                #elif LINUX
                    PlatformType.Linux
                #elif MACOS
                    PlatformType.MacOS
                #elif ANDROID
                    PlatformType.Android
                #elif IOS
                    PlatformType.iOS
                #elif WASM || FABLE_COMPILER
                    PlatformType.WebAssembly
                #else
                    // Runtime detection as fallback
                    // Note: This implementation is simplified and may not be accurate in all environments
                    // especially in environments without traditional runtime identification
                    let osNameLower = "unknown"
                    
                    if osNameLower.Contains("win") then
                        PlatformType.Windows
                    elif osNameLower.Contains("linux") then
                        PlatformType.Linux
                    elif osNameLower.Contains("mac") || osNameLower.Contains("darwin") then
                        PlatformType.MacOS
                    elif osNameLower.Contains("android") then
                        PlatformType.Android
                    elif osNameLower.Contains("ios") then
                        PlatformType.iOS
                    else
                        PlatformType.InMemory
                #endif
            
            // Cache the detected platform
            Providers.currentPlatform <- platform
            platform
    
    /// <summary>
    /// Sets the current platform type explicitly (for testing or specialized environments)
    /// </summary>
    /// <param name="platform">The platform type to set</param>
    let setCurrentPlatform (platform: PlatformType): unit =
        Providers.currentPlatform <- platform
    
    /// <summary>
    /// Registers a memory provider for a specific platform
    /// </summary>
    /// <param name="platform">The platform for this provider</param>
    /// <param name="provider">The provider implementation</param>
    let registerMemoryProvider (platform: PlatformType) (provider: IPlatformMemory): unit =
        let index = 
            Providers.memoryProviders 
            |> Array.tryFindIndex (fun p -> isNull (box p))
            |> function
               | Some i -> i
               | None -> failwith "Memory provider registry is full"
        
        Providers.memoryProviders.[index] <- {
            ProviderType = ProviderType.Memory
            PlatformType = platform
            Provider = provider
        }
    
    /// <summary>
    /// Registers an IPC provider for a specific platform
    /// </summary>
    /// <param name="platform">The platform for this provider</param>
    /// <param name="provider">The provider implementation</param>
    let registerIpcProvider (platform: PlatformType) (provider: IPlatformIpc): unit =
        let index = 
            Providers.ipcProviders 
            |> Array.tryFindIndex (fun p -> isNull (box p))
            |> function
               | Some i -> i
               | None -> failwith "IPC provider registry is full"
        
        Providers.ipcProviders.[index] <- {
            ProviderType = ProviderType.IPC
            PlatformType = platform
            Provider = provider
        }
    
    /// <summary>
    /// Registers a network provider for a specific platform
    /// </summary>
    /// <param name="platform">The platform for this provider</param>
    /// <param name="provider">The provider implementation</param>
    let registerNetworkProvider (platform: PlatformType) (provider: IPlatformNetwork): unit =
        let index = 
            Providers.networkProviders 
            |> Array.tryFindIndex (fun p -> isNull (box p))
            |> function
               | Some i -> i
               | None -> failwith "Network provider registry is full"
        
        Providers.networkProviders.[index] <- {
            ProviderType = ProviderType.Network
            PlatformType = platform
            Provider = provider
        }
    
    /// <summary>
    /// Registers a synchronization provider for a specific platform
    /// </summary>
    /// <param name="platform">The platform for this provider</param>
    /// <param name="provider">The provider implementation</param>
    let registerSyncProvider (platform: PlatformType) (provider: IPlatformSync): unit =
        let index = 
            Providers.syncProviders 
            |> Array.tryFindIndex (fun p -> isNull (box p))
            |> function
               | Some i -> i
               | None -> failwith "Sync provider registry is full"
        
        Providers.syncProviders.[index] <- {
            ProviderType = ProviderType.Sync
            PlatformType = platform
            Provider = provider
        }
    
    /// <summary>
    /// Gets a memory provider for a specific platform
    /// </summary>
    /// <param name="platform">The platform to get the provider for</param>
    /// <returns>A result containing the provider or an error</returns>
    let getMemoryProvider (platform: PlatformType): Result<IPlatformMemory> =
        Providers.memoryProviders
        |> Array.tryFind (fun p -> not (isNull (box p)) && p.PlatformType = platform)
        |> function
           | Some entry -> Ok entry.Provider
           | None -> Error (invalidValueError $"No memory provider registered for platform: {platform}")
    
    /// <summary>
    /// Gets an IPC provider for a specific platform
    /// </summary>
    /// <param name="platform">The platform to get the provider for</param>
    /// <returns>A result containing the provider or an error</returns>
    let getIpcProvider (platform: PlatformType): Result<IPlatformIpc> =
        Providers.ipcProviders
        |> Array.tryFind (fun p -> not (isNull (box p)) && p.PlatformType = platform)
        |> function
           | Some entry -> Ok entry.Provider
           | None -> Error (invalidValueError $"No IPC provider registered for platform: {platform}")
    
    /// <summary>
    /// Gets a network provider for a specific platform
    /// </summary>
    /// <param name="platform">The platform to get the provider for</param>
    /// <returns>A result containing the provider or an error</returns>
    let getNetworkProvider (platform: PlatformType): Result<IPlatformNetwork> =
        Providers.networkProviders
        |> Array.tryFind (fun p -> not (isNull (box p)) && p.PlatformType = platform)
        |> function
           | Some entry -> Ok entry.Provider
           | None -> Error (invalidValueError $"No network provider registered for platform: {platform}")
    
    /// <summary>
    /// Gets a synchronization provider for a specific platform
    /// </summary>
    /// <param name="platform">The platform to get the provider for</param>
    /// <returns>A result containing the provider or an error</returns>
    let getSyncProvider (platform: PlatformType): Result<IPlatformSync> =
        Providers.syncProviders
        |> Array.tryFind (fun p -> not (isNull (box p)) && p.PlatformType = platform)
        |> function
           | Some entry -> Ok entry.Provider
           | None -> Error (invalidValueError $"No sync provider registered for platform: {platform}")
    
    /// <summary>
    /// Gets the memory provider for the current platform
    /// </summary>
    /// <returns>A result containing the provider or an error</returns>
    let getCurrentMemoryProvider (): Result<IPlatformMemory> =
        let platform = getCurrentPlatform()
        getMemoryProvider platform
    
    /// <summary>
    /// Gets the IPC provider for the current platform
    /// </summary>
    /// <returns>A result containing the provider or an error</returns>
    let getCurrentIpcProvider (): Result<IPlatformIpc> =
        let platform = getCurrentPlatform()
        getIpcProvider platform
    
    /// <summary>
    /// Gets the network provider for the current platform
    /// </summary>
    /// <returns>A result containing the provider or an error</returns>
    let getCurrentNetworkProvider (): Result<IPlatformNetwork> =
        let platform = getCurrentPlatform()
        getNetworkProvider platform
    
    /// <summary>
    /// Gets the synchronization provider for the current platform
    /// </summary>
    /// <returns>A result containing the provider or an error</returns>
    let getCurrentSyncProvider (): Result<IPlatformSync> =
        let platform = getCurrentPlatform()
        getSyncProvider platform