namespace BAREWire.Platform

open BAREWire.Platform.Common
open BAREWire.Platform.Common.Interfaces
open BAREWire.Platform.Providers

/// <summary>
/// Platform services initialization and configuration
/// </summary>
module PlatformServices =
    /// <summary>
    /// Platform initialization status
    /// </summary>
    let mutable private isInitialized = false
    
    /// <summary>
    /// Initializes the platform providers based on the current environment
    /// </summary>
    /// <param name="registryCapacity">The maximum number of providers per registry</param>
    /// <returns>True if initialization was successful, false if already initialized</returns>
    let initialize (registryCapacity: int): bool =
        // Check if already initialized
        if isInitialized then
            false
        else
            // Initialize the registries
            Registry.initializeRegistries registryCapacity
            
            // Get the current platform
            let platform = Registry.getCurrentPlatform()
            
            // Register platform-specific providers
            match platform with
            | Registry.PlatformType.Windows ->
                // Register Windows providers
                Registry.registerMemoryProvider platform (new Windows.WindowsMemoryProvider())
                Registry.registerIpcProvider platform (new Windows.WindowsIpcProvider())
                Registry.registerNetworkProvider platform (new Windows.WindowsNetworkProvider())
                Registry.registerSyncProvider platform (new Windows.WindowsSyncProvider())
                
            | Registry.PlatformType.Linux ->
                // Register Linux providers
                // TODO: Implement Linux providers
                Registry.registerMemoryProvider platform (new InMemory.InMemoryMemoryProvider())
                Registry.registerIpcProvider platform (new InMemory.InMemoryIpcProvider())
                Registry.registerNetworkProvider platform (new InMemory.InMemoryNetworkProvider())
                Registry.registerSyncProvider platform (new InMemory.InMemorySyncProvider())
                
            | Registry.PlatformType.MacOS ->
                // Register macOS providers
                // TODO: Implement macOS providers
                Registry.registerMemoryProvider platform (new InMemory.InMemoryMemoryProvider())
                Registry.registerIpcProvider platform (new InMemory.InMemoryIpcProvider())
                Registry.registerNetworkProvider platform (new InMemory.InMemoryNetworkProvider())
                Registry.registerSyncProvider platform (new InMemory.InMemorySyncProvider())
                
            | Registry.PlatformType.Android ->
                // Register Android providers
                // TODO: Implement Android providers
                Registry.registerMemoryProvider platform (new InMemory.InMemoryMemoryProvider())
                Registry.registerIpcProvider platform (new InMemory.InMemoryIpcProvider())
                Registry.registerNetworkProvider platform (new InMemory.InMemoryNetworkProvider())
                Registry.registerSyncProvider platform (new InMemory.InMemorySyncProvider())
                
            | Registry.PlatformType.iOS ->
                // Register iOS providers
                // TODO: Implement iOS providers
                Registry.registerMemoryProvider platform (new InMemory.InMemoryMemoryProvider())
                Registry.registerIpcProvider platform (new InMemory.InMemoryIpcProvider())
                Registry.registerNetworkProvider platform (new InMemory.InMemoryNetworkProvider())
                Registry.registerSyncProvider platform (new InMemory.InMemorySyncProvider())
                
            | Registry.PlatformType.WebAssembly ->
                // Register WebAssembly providers
                // TODO: Implement WebAssembly providers
                Registry.registerMemoryProvider platform (new InMemory.InMemoryMemoryProvider())
                Registry.registerIpcProvider platform (new InMemory.InMemoryIpcProvider())
                Registry.registerNetworkProvider platform (new InMemory.InMemoryNetworkProvider())
                Registry.registerSyncProvider platform (new InMemory.InMemorySyncProvider())
                
            | _ ->
                // Use in-memory simulation as a fallback
                Registry.registerMemoryProvider platform (new InMemory.InMemoryMemoryProvider())
                Registry.registerIpcProvider platform (new InMemory.InMemoryIpcProvider())
                Registry.registerNetworkProvider platform (new InMemory.InMemoryNetworkProvider())
                Registry.registerSyncProvider platform (new InMemory.InMemorySyncProvider())
            
            // Mark as initialized
            isInitialized <- true
            true
    
    /// <summary>
    /// Explicitly sets the platform type and registers appropriate providers
    /// </summary>
    /// <param name="platform">The platform type to use</param>
    /// <param name="registryCapacity">The maximum number of providers per registry</param>
    /// <returns>True if initialization was successful, false if already initialized</returns>
    let initializeWithPlatform (platform: Registry.PlatformType) (registryCapacity: int): bool =
        // Check if already initialized
        if isInitialized then
            false
        else
            // Set the platform
            Registry.setCurrentPlatform platform
            
            // Initialize with the specified platform
            initialize registryCapacity
    
    /// <summary>
    /// Checks if the platform services have been initialized
    /// </summary>
    /// <returns>True if initialized, false otherwise</returns>
    let isInitialized (): bool =
        isInitialized
    
    /// <summary>
    /// Gets the current platform type
    /// </summary>
    /// <returns>The current platform type</returns>
    let getCurrentPlatform (): Registry.PlatformType =
        Registry.getCurrentPlatform()
    
    /// <summary>
    /// Gets the memory provider for the current platform
    /// </summary>
    /// <returns>A result containing the provider or an error</returns>
    let getMemoryProvider (): Result<IPlatformMemory> =
        if not isInitialized then
            Error (BAREWire.Core.Error.invalidStateError "Platform services not initialized")
        else
            Registry.getCurrentMemoryProvider()
    
    /// <summary>
    /// Gets the IPC provider for the current platform
    /// </summary>
    /// <returns>A result containing the provider or an error</returns>
    let getIpcProvider (): Result<IPlatformIpc> =
        if not isInitialized then
            Error (BAREWire.Core.Error.invalidStateError "Platform services not initialized")
        else
            Registry.getCurrentIpcProvider()
    
    /// <summary>
    /// Gets the network provider for the current platform
    /// </summary>
    /// <returns>A result containing the provider or an error</returns>
    let getNetworkProvider (): Result<IPlatformNetwork> =
        if not isInitialized then
            Error (BAREWire.Core.Error.invalidStateError "Platform services not initialized")
        else
            Registry.getCurrentNetworkProvider()
    
    /// <summary>
    /// Gets the synchronization provider for the current platform
    /// </summary>
    /// <returns>A result containing the provider or an error</returns>
    let getSyncProvider (): Result<IPlatformSync> =
        if not isInitialized then
            Error (BAREWire.Core.Error.invalidStateError "Platform services not initialized")
        else
            Registry.getCurrentSyncProvider()
    
    /// <summary>
    /// Ensures the platform services are initialized, initializing them if necessary
    /// </summary>
    /// <param name="registryCapacity">The maximum number of providers per registry</param>
    /// <returns>True if initialization was successful or already initialized</returns>
    let ensureInitialized (registryCapacity: int): bool =
        if isInitialized then
            true
        else
            initialize registryCapacity