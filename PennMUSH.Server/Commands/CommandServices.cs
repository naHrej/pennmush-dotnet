namespace PennMUSH.Server.Commands;

using PennMUSH.Core.Access;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Communication;
using PennMUSH.Core.Communication.Channels;
using PennMUSH.Core.Communication.Mail;
using PennMUSH.Core.Configuration;
using PennMUSH.Core.Database;
using PennMUSH.Core.Help;
using PennMUSH.Core.Http;
using PennMUSH.Core.Logging;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Protocol;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Scheduling;
using PennMUSH.Core.Session;
using PennMUSH.Core.Types;
using PennMUSH.Server.Extensions;

/// <summary>
/// The services a command module needs in order to register its handlers.
/// </summary>
public sealed class CommandServices
{
    public required CommandDispatcher Dispatcher { get; init; }
    public required ExpressionEvaluator Evaluator { get; init; }
    public required ActionQueue Queue { get; init; }
    public required MushConfiguration Config { get; init; }
    public required ChannelRegistry Channels { get; init; }
    public required MailStore Mail { get; init; }
    public required MailAliasTable MailAliases { get; init; }
    public required CronScheduler Cron { get; init; }
    public required HelpStore Help { get; init; }
    public required SuggestionVocabulary Suggestions { get; init; }
    public required TextFileCache TextCache { get; init; }
    public required MotdBoard Motd { get; init; }
    public required WizardLog Logs { get; init; }
    public required SqlDatabase Sql { get; init; }
    public required HttpFetch Web { get; init; }
    public required HttpChannel Http { get; init; }
    public required FunctionRegistry Functions { get; init; }
    public required ISessionDirectory Sessions { get; init; }
    public required EditorSessionManager EditorSessions { get; init; }
    public required ExtensionManager Extensions { get; init; }
    public required SiteLockTable SiteLocks { get; init; }
    public required ConnectionLogStore ConnectionLog { get; init; }
    public required string GameDirectory { get; init; }
    public required LookCommandHandler Look { get; init; }
    public required Func<Dbref, bool> IsOnline { get; init; }
    public required Func<Dbref, bool> PlayerReceivesHtml { get; init; }
    public required Action SaveAll { get; init; }
    public required Action SyncSuggestions { get; init; }
    public required Func<IReadOnlyList<string>> ScreenLines { get; init; }
    public required Action Reboot { get; init; }
    public required Action RequestShutdown { get; init; }
    public required Action RequestQuietReboot { get; init; }
}

public interface ICommandModule
{
    void Register(CommandServices services);
}
