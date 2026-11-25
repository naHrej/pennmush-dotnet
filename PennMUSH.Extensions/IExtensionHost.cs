namespace PennMUSH.Extensions;

using System;
using System.Text.Json;
using System.Threading.Tasks;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Database;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Scheduling;
using PennMUSH.Core.Types;

public interface IExtensionHost
{
    ObjectDatabase Database { get; }
    CommandDispatcher Dispatcher { get; }
    FunctionRegistry Functions { get; }
    ExpressionEvaluator Evaluator { get; }
    ActionQueue Queue { get; }
    CronScheduler Cron { get; }
    string DataDirectory { get; }

    void RegisterCommand(ICommandHandler handler, params string[] aliases);
    void UnregisterCommand(string commandName);

    void RegisterFunction(string name, MUSHFunction fn);
    void UnregisterFunction(string name);

    void RegisterTickService(ITickService tickService);
    void UnregisterTickService(string name);

    void RegisterProtocolHandler(string typePrefix, IProtocolHandler handler);
    void UnregisterProtocolHandler(string typePrefix);

    bool RegisterAttributeFlag(string flagName);
    bool UnregisterAttributeFlag(string flagName);

    void CapturePlayerInput(Dbref player, Func<string, bool> onLine);
    void ReleasePlayerInput(Dbref player);

    void OnObjectDestroyed(Action<Dbref> handler);

    void BroadcastNotification(Notification notification);
    void NotifyPlayer(Dbref player, string message);
    Task SendProtocolEnvelopeAsync(Dbref player, string type, object payload);
    void Log(string message);
}
