namespace PennMUSH.Server.Extensions;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PennMUSH.Core.Attributes;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Database;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Scheduling;
using PennMUSH.Core.Types;
using PennMUSH.Extensions;
using PennMUSH.Server.Engine;

public class ExtensionHostProxy : IExtensionHost
{
    private readonly ExtensionManager _manager;
    private readonly MUSHServer _server;
    private readonly string _extensionName;

    private readonly List<string> _registeredCommands = new();
    private readonly List<string> _registeredFunctions = new();
    private readonly List<string> _registeredTickServices = new();
    private readonly List<string> _registeredProtocolHandlers = new();
    private readonly List<string> _registeredAttributeFlags = new();
    private readonly HashSet<Dbref> _capturedInputPlayers = new();
    private readonly List<Action<Dbref>> _objectDestroyedHandlers = new();

    public ObjectDatabase Database => _server.Database;
    public CommandDispatcher Dispatcher => _server.Dispatcher;
    public FunctionRegistry Functions => _server.Functions;
    public ExpressionEvaluator Evaluator => _server.Evaluator;
    public ActionQueue Queue => _server.Queue;
    public CronScheduler Cron => _server.Cron;
    public string DataDirectory
    {
        get
        {
            string safeName = string.Concat(_extensionName.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
            if (string.IsNullOrWhiteSpace(safeName))
                safeName = "extension";
            return Path.Combine(_server.GameDirectory, "plugins", "data", safeName);
        }
    }

    public ExtensionHostProxy(ExtensionManager manager, MUSHServer server, string extensionName)
    {
        _manager = manager;
        _server = server;
        _extensionName = extensionName;
    }

    public void RegisterCommand(ICommandHandler handler, params string[] aliases)
    {
        _server.Dispatcher.Register(handler, aliases);
        _registeredCommands.Add(handler.Name);
        foreach (var alias in aliases)
        {
            _registeredCommands.Add(alias);
        }
    }

    public void UnregisterCommand(string commandName)
    {
        _server.Dispatcher.Unregister(commandName);
        _registeredCommands.Remove(commandName);
    }

    public void RegisterFunction(string name, MUSHFunction fn)
    {
        _server.Functions.Register(name, fn);
        _registeredFunctions.Add(name);
    }

    public void UnregisterFunction(string name)
    {
        _server.Functions.Unregister(name);
        _registeredFunctions.Remove(name);
    }

    public void RegisterTickService(ITickService tickService)
    {
        _manager.RegisterTickServiceInternal(tickService);
        _registeredTickServices.Add(tickService.Name);
    }

    public void UnregisterTickService(string name)
    {
        _manager.UnregisterTickServiceInternal(name);
        _registeredTickServices.Remove(name);
    }

    public void RegisterProtocolHandler(string typePrefix, IProtocolHandler handler)
    {
        _manager.RegisterProtocolHandlerInternal(typePrefix, handler);
        _registeredProtocolHandlers.Add(typePrefix);
    }

    public void UnregisterProtocolHandler(string typePrefix)
    {
        _manager.UnregisterProtocolHandlerInternal(typePrefix);
        _registeredProtocolHandlers.Remove(typePrefix);
    }

    public bool RegisterAttributeFlag(string flagName, string? shortName = null)
    {
        if (!StandardAttributes.RegisterCustomFlag(flagName, shortName))
            return false;
        _registeredAttributeFlags.Add(flagName);
        return true;
    }

    public bool UnregisterAttributeFlag(string flagName)
    {
        _registeredAttributeFlags.RemoveAll(f => f.Equals(flagName, StringComparison.OrdinalIgnoreCase));
        return StandardAttributes.UnregisterCustomFlag(flagName);
    }

    public void CapturePlayerInput(Dbref player, Func<string, bool> onLine)
    {
        ArgumentNullException.ThrowIfNull(onLine);
        _capturedInputPlayers.Add(player);
        _server.CapturePlayerInput(player, line =>
        {
            bool keep = onLine(line);
            if (!keep)
                _capturedInputPlayers.Remove(player);
            return keep;
        });
    }

    public void ReleasePlayerInput(Dbref player)
    {
        _capturedInputPlayers.Remove(player);
        _server.ReleasePlayerInput(player);
    }

    public void OnObjectDestroyed(Action<Dbref> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _objectDestroyedHandlers.Add(handler);
        _server.Database.ObjectDestroyed += handler;
    }

    public void BroadcastNotification(Notification notification)
    {
        _server.DispatchNotification(notification);
    }

    public void NotifyPlayer(Dbref player, string message)
    {
        foreach (var conn in _server.Connections)
        {
            if (conn.PlayerId == player)
            {
                _ = conn.SendLineAsync(message);
            }
        }
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task SendProtocolEnvelopeAsync(Dbref player, string type, object payload)
    {
        var envelope = new
        {
            type = type,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            payload = payload
        };

        string json = JsonSerializer.Serialize(envelope, SerializerOptions);

        foreach (var conn in _server.Connections)
        {
            if (conn.PlayerId == player)
            {
                await conn.SendLineAsync(json);
            }
        }
    }

    public void Log(string message)
    {
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] [{_extensionName}] {message}");
    }

    public void Cleanup()
    {
        foreach (var cmd in _registeredCommands)
        {
            try { _server.Dispatcher.Unregister(cmd); } catch { }
        }
        _registeredCommands.Clear();

        foreach (var func in _registeredFunctions)
        {
            try { _server.Functions.Unregister(func); } catch { }
        }
        _registeredFunctions.Clear();

        foreach (var tick in _registeredTickServices)
        {
            try { _manager.UnregisterTickServiceInternal(tick); } catch { }
        }
        _registeredTickServices.Clear();

        foreach (var proto in _registeredProtocolHandlers)
        {
            try { _manager.UnregisterProtocolHandlerInternal(proto); } catch { }
        }
        _registeredProtocolHandlers.Clear();

        foreach (var attrFlag in _registeredAttributeFlags)
        {
            try { StandardAttributes.UnregisterCustomFlag(attrFlag); } catch { }
        }
        _registeredAttributeFlags.Clear();

        foreach (var captured in _capturedInputPlayers.ToArray())
        {
            try { _server.ReleasePlayerInput(captured); } catch { }
        }
        _capturedInputPlayers.Clear();

        foreach (var handler in _objectDestroyedHandlers)
        {
            try { _server.Database.ObjectDestroyed -= handler; } catch { }
        }
        _objectDestroyedHandlers.Clear();
    }
}
