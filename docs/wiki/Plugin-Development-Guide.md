# Plugin Development Guide (`PennMUSH.Extensions`)

PennMUSH .NET supports lock-free, hot-reloadable `.NET 10` plugins via `PennMUSH.Extensions` and `PennMUSH.Server/Extensions/`.

---

## Architecture & Hot-Reload Mechanics

- **`ExtensionLoadContext`** (`PennMUSH.Server/Extensions/ExtensionLoadContext.cs`): A collectible `AssemblyLoadContext` (`isCollectible: true`). Assemblies and `.pdb` symbols are read into `MemoryStream`s (`File.ReadAllBytes`) before calling `LoadFromStream`, so **Windows/OS file locks are never held on plugin `.dll` files**.
- **`ExtensionManager`** (`PennMUSH.Server/Extensions/ExtensionManager.cs`):
  - Scans plugin candidate paths at startup (ignoring `PennMUSH.Core*`, `PennMUSH.Server*`, `PennMUSH.Extensions*`).
  - Attaches a `FileSystemWatcher` (`*.dll`, 500 ms debounce) to reload updated plugins live (`ReloadExtension` → `Unload` → `GC.Collect` → `LoadExtension`) or auto-load new ones with zero player disconnects.
- **`ExtensionHostProxy`** (`PennMUSH.Server/Extensions/ExtensionHostProxy.cs`): Tracks every command, function, tick service, protocol handler, custom attribute flag, player input capture, and `ObjectDestroyed` handler registered by a plugin and automatically unregisters/cleans up all of them inside `Cleanup()` on unload/reload.

### Default Plugin Search Directories
`MUSHServer` searches:
- `<GameDirectory>/plugins`
- `<AppContext.BaseDirectory>/plugins`
- `<WorkingDirectory>/plugins`
- Repository root `plugins/` (e.g., where compiled plugin DLLs like `PennMUSH.SpaceEngine.dll` reside)

---

## SDK Interfaces (`PennMUSH.Extensions`)

### `IExtension`
Entry point discovered automatically inside the plugin assembly (`Activator.CreateInstance`):

```csharp
public interface IExtension
{
    string Name { get; }
    string Version { get; }
    string Description { get; }
    string Author { get; }

    void Initialize(IExtensionHost host);
    void Shutdown();
}
```

### `IExtensionHost`
Capabilities exposed to plugins by `ExtensionHostProxy`:

| Member | Purpose |
|---|---|
| `Database`, `Dispatcher`, `Functions`, `Evaluator`, `Queue`, `Cron` | Direct access to core engine subsystems (including softcode `ExpressionEvaluator`) |
| `DataDirectory` | Dedicated per-plugin data directory (`<GameDirectory>/plugins/data/<ExtensionName>`) |
| `RegisterCommand(handler, params aliases)` / `UnregisterCommand(name)` | Add or remove custom player commands |
| `RegisterFunction(name, fn)` / `UnregisterFunction(name)` | Add or remove custom MUSHcode functions |
| `RegisterTickService(service)` / `UnregisterTickService(name)` | Schedule background periodic ticks |
| `RegisterProtocolHandler(prefix, handler)` / `UnregisterProtocolHandler(prefix)` | Intercept MUjs `ProtocolEnvelope` messages matching `type.StartsWith(prefix)` |
| `RegisterAttributeFlag(flagName)` / `UnregisterAttributeFlag(flagName)` | Register custom named attribute flags (`AttributeEntry.CustomFlags`, persisted without consuming the 32-bit `AttributeFlags` bitmask) |
| `CapturePlayerInput(player, onLine)` / `ReleasePlayerInput(player)` | Intercept raw lines from `player` (preserving indentation and blank lines) before command dispatch until `onLine` returns `false`, `ReleasePlayerInput` is called, or the player disconnects |
| `OnObjectDestroyed(handler)` | Subscribe to `ObjectDatabase.ObjectDestroyed` with automatic unsubscription on unload |
| `BroadcastNotification(notification)` | Dispatch room/player/broadcast notifications |
| `NotifyPlayer(player, message)` | Send a line to all connections for `player` |
| `SendProtocolEnvelopeAsync(player, type, payload)` | Send a JSON `ProtocolEnvelope` to `player` |
| `Log(message)` | Timestamped console logging prefixed with `[Name]` |

### `ITickService` & `IProtocolHandler`

```csharp
public interface ITickService
{
    string Name { get; }
    TimeSpan Interval { get; }
    Task ExecuteTickAsync(CancellationToken ct);
}

public interface IProtocolHandler
{
    Task HandleEnvelopeAsync(Dbref player, string type, JsonElement payload, IExtensionHost host);
}
```

---

## Managing Plugins In-Game (`@plugin` / `@extension`)

Wizard-only command handled by `PluginCommandHandler` (`PennMUSH.Server/Commands/PluginCommandHandler.cs`):

| Command | Action |
|---|---|
| `@plugin` or `@plugin/list` | Displays loaded plugin table (`Plugin Name`, `Version`, `Author`, `Description`) |
| `@plugin/load <assembly_path>` | Loads a plugin DLL (resolving relative paths or searching `plugins/`) |
| `@plugin/unload <plugin_name>` | Invokes `Shutdown()`, cleans up all registered hooks, and unloads ALC |
| `@plugin/reload <plugin_name>` | Unloads, collects ALC, and reloads from original path |

---

## Minimal Example Plugin

1. Create a `.NET 10` class library referencing `PennMUSH.Extensions` (and `PennMUSH.Core` with `<Private>false</Private>` / `ExcludeAssets="runtime"` so host assemblies come from `AssemblyLoadContext.Default`).
2. Implement `IExtension`:

```csharp
using PennMUSH.Core.Commands;
using PennMUSH.Core.Database;
using PennMUSH.Extensions;

public sealed class HelloExtension : IExtension
{
    public string Name => "HelloPlugin";
    public string Version => "1.0.0";
    public string Description => "Adds @hello command and hello() function.";
    public string Author => "YourName";

    public void Initialize(IExtensionHost host)
    {
        host.RegisterFunction("HELLO", (ctx, args) => "Hello from C#!");
        host.RegisterCommand(new HelloCommand(), "@hello");
        host.Log("Initialized.");
    }

    public void Shutdown() { }

    private sealed class HelloCommand : ICommandHandler
    {
        public string Name => "@hello";
        public bool Execute(CommandContext context, ObjectDatabase db)
        {
            context.Notify("Hello from plugin!");
            return true;
        }
    }
}
```

3. Build the DLL into `plugins/` (or load it dynamically via `@plugin/load HelloPlugin.dll`).
