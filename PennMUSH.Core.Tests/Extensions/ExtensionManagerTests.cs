namespace PennMUSH.Core.Tests.Extensions;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;
using PennMUSH.Extensions;
using PennMUSH.Server.Commands;
using PennMUSH.Server.Engine;
using PennMUSH.Server.Extensions;
using Xunit;

public class ExtensionManagerTests
{
    public class MockExtension : IExtension
    {
        public string Name => "TestPlugin";
        public string Version => "1.0.0";
        public string Description => "Mock Extension for testing";
        public string Author => "TestAuthor";

        public bool Initialized { get; private set; }
        public bool ShutdownCalled { get; private set; }

        public void Initialize(IExtensionHost host)
        {
            Initialized = true;
            host.RegisterCommand(new MockPluginCommand(), "testcmd");
            host.RegisterFunction("plugin_test", (ctx, args) => "PluginFuncOutput");
            host.RegisterProtocolHandler("test.proto", new MockProtocolHandler());
        }

        public void Shutdown()
        {
            ShutdownCalled = true;
        }
    }

    private class MockPluginCommand : ICommandHandler
    {
        public string Name => "testcmd";
        public bool Execute(CommandContext context, ObjectDatabase db)
        {
            context.Notify("Executed MockPluginCommand");
            return true;
        }
    }

    private class MockProtocolHandler : IProtocolHandler
    {
        public bool Handled { get; private set; }
        public Task HandleEnvelopeAsync(Dbref player, string type, JsonElement payload, IExtensionHost host)
        {
            Handled = true;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task ExtensionHostProxy_RegistersAndCleansUpHooks()
    {
        var server = new MUSHServer();
        var manager = server.Extensions;
        var hostProxy = new ExtensionHostProxy(manager, server, "TestPlugin");

        var extension = new MockExtension();
        extension.Initialize(hostProxy);

        // 1. Verify command registered
        var lookup = server.Dispatcher.Table.Lookup("testcmd");
        Assert.Equal(CommandLookupStatus.Found, lookup.Status);

        // 2. Verify function registered
        Assert.True(server.Functions.HasFunction("plugin_test"));
        var evalCtx = new EvaluationContext(new Dbref(1), new Dbref(1));
        string res = server.Evaluator.Evaluate("[plugin_test()]", evalCtx);
        Assert.Equal("PluginFuncOutput", res);

        // 3. Verify protocol handler registered
        var jsonDoc = JsonDocument.Parse("{\"foo\":\"bar\"}");
        bool handled = await manager.TryDispatchProtocolEnvelopeAsync(new Dbref(1), "test.proto.action", jsonDoc.RootElement);
        Assert.True(handled);

        // 4. Cleanup and verify unregistration
        hostProxy.Cleanup();
        extension.Shutdown();

        var lookupAfter = server.Dispatcher.Table.Lookup("testcmd");
        Assert.Equal(CommandLookupStatus.NotFound, lookupAfter.Status);
        Assert.False(server.Functions.HasFunction("plugin_test"));
        Assert.True(extension.ShutdownCalled);
    }

    [Fact]
    public void PluginCommandHandler_ListAndPermissions()
    {
        var server = new MUSHServer();
        var handler = new PluginCommandHandler(server.Extensions);
        var db = server.Database;

        var mortal = db.CreateObject("Mortal", ObjectType.Player, Dbref.Nothing);
        var wizard = db.CreateObject("Wizard", ObjectType.Player, Dbref.Nothing);
        wizard.AddFlag("WIZARD");

        // Mortal denied
        var mortalCtx = new CommandContext
        {
            Executor = mortal.Id,
            CommandName = "@plugin"
        };
        handler.Execute(mortalCtx, db);
        Assert.Contains("Permission denied.", mortalCtx.Notifications);

        // Wizard allowed
        var wizCtx = new CommandContext
        {
            Executor = wizard.Id,
            CommandName = "@plugin"
        };
        handler.Execute(wizCtx, db);
        Assert.Contains(wizCtx.Notifications, n => n.Contains("No external plugins are currently loaded.") || n.Contains("Loaded PennMUSH Extensions"));
    }

    [Fact]
    public void ExtensionManager_CanDynamicallyLoadAndUnloadPlugin()
    {
        string pluginDll = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../plugins/PennMUSH.SpaceEngine.dll"));
        if (!File.Exists(pluginDll))
        {
            pluginDll = @"C:\Repos\pennmush-dotnet\plugins\PennMUSH.SpaceEngine.dll";
        }

        if (File.Exists(pluginDll))
        {
            var server = new MUSHServer();
            var record = server.Extensions.LoadExtension(pluginDll);
            Assert.NotNull(record);
            Assert.True(server.Extensions.IsLoaded("PennMUSH.SpaceEngine"));

            // Verify @space and space registered dynamically
            Assert.Equal(CommandLookupStatus.Found, server.Dispatcher.Table.Lookup("@space").Status);
            Assert.Equal(CommandLookupStatus.Found, server.Dispatcher.Table.Lookup("space").Status);
            Assert.True(server.Functions.HasFunction("space_ships"));

            // Unload dynamically
            bool unloaded = server.Extensions.UnloadExtension("PennMUSH.SpaceEngine");
            Assert.True(unloaded);
            Assert.False(server.Extensions.IsLoaded("PennMUSH.SpaceEngine"));
            Assert.Equal(CommandLookupStatus.NotFound, server.Dispatcher.Table.Lookup("@space").Status);
        }
    }

    [Fact]
    public void ExtensionHostProxy_CapturesAndReleasesPlayerInput()
    {
        var server = new MUSHServer();
        var hostProxy = new ExtensionHostProxy(server.Extensions, server, "ScriptPlugin");
        var player = new Dbref(1);
        var lines = new List<string>();

        hostProxy.CapturePlayerInput(player, line =>
        {
            if (line == ".")
                return false;
            lines.Add(line);
            return true;
        });

        Assert.True(server.TryHandleCapturedInput(player, "  var x = 1;"));
        Assert.True(server.TryHandleCapturedInput(player, ""));
        Assert.True(server.TryHandleCapturedInput(player, "  return x;"));
        Assert.True(server.TryHandleCapturedInput(player, "."));
        Assert.False(server.TryHandleCapturedInput(player, "say normal command"));

        Assert.Equal(new[] { "  var x = 1;", "", "  return x;" }, lines);

        hostProxy.CapturePlayerInput(player, _ => true);
        hostProxy.Cleanup();
        Assert.False(server.TryHandleCapturedInput(player, "after cleanup"));
    }
}
