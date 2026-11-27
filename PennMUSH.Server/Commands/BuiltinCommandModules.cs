namespace PennMUSH.Server.Commands;

using PennMUSH.Core.Access;
using PennMUSH.Core.Commands.Handlers;

/// <summary>
/// Built-in commands, grouped so a new command is added in its area.
/// Modules run in this order. <see cref="SpeechAndBuildingModule"/> registers look first.
/// </summary>
public static class BuiltinCommandModules
{
    public static IReadOnlyList<ICommandModule> All { get; } =
    [
        new SpeechAndBuildingModule(),
        new InteractionModule(),
        new FlowModule(),
        new AdministrationModule(),
        new AliasAndRestrictionModule()
    ];
}

public sealed class SpeechAndBuildingModule : ICommandModule
{
    public void Register(CommandServices services)
    {
        var dispatcher = services.Dispatcher;
        dispatcher.Register(new SayCommandHandler(services.Evaluator), "say", "\"", "'");
        dispatcher.Register(new PoseCommandHandler(services.Evaluator), "pose", ":", ";", "semipose");
        dispatcher.Register(services.Look, "look", "l", "read");
        dispatcher.Register(new GoCommandHandler(services.Look, services.Evaluator, dispatcher, services.IsOnline), "go", "goto", "move");
        dispatcher.Register(new DigCommandHandler(services.Look, () => services.Config.UseQuota, () => services.Config.StartingQuota), "@dig");
        dispatcher.Register(new OpenCommandHandler(() => services.Config.UseQuota, () => services.Config.StartingQuota), "@open");
        dispatcher.Register(new LinkCommandHandler(), "@link");
        dispatcher.Register(new UnlinkCommandHandler(), "@unlink");
        dispatcher.Register(new CreateCommandHandler(() => services.Config.UseQuota, () => services.Config.StartingQuota), "@create");
        dispatcher.Register(new SetCommandHandler(), "@set");
        dispatcher.Register(new PowerCommandHandler(), "@power");
        dispatcher.Register(new NameCommandHandler(), "@name");
        dispatcher.Register(new ParentCommandHandler(), "@parent");
        dispatcher.Register(new ZoneCommandHandler(), "@zone");
        dispatcher.Register(new ChzoneCommandHandler(), "@chzone");
        dispatcher.Register(new ChzoneAllCommandHandler(), "@chzoneall");
        dispatcher.Register(new ZemitCommandHandler(), "@zemit", "@nszemit");
        dispatcher.Register(new ScanCommandHandler(), "@scan");
        dispatcher.Register(new SweepCommandHandler(), "@sweep");
        dispatcher.Register(new WithCommandHandler(dispatcher, services.Evaluator), "with");
        dispatcher.Register(new SearchCommandHandler(services.Evaluator), "@search");
        dispatcher.Register(new WaitCommandHandler(services.Queue), "@wait", "@notify", "@drain");
        dispatcher.Register(new HaltCommandHandler(services.Queue), "@halt");
        dispatcher.Register(new AllHaltCommandHandler(services.Queue), "@allhalt");
        dispatcher.Register(new PsCommandHandler(services.Queue), "@ps");
        dispatcher.Register(new KickCommandHandler(services.Queue, dispatcher), "@kick");
        dispatcher.Register(new ThinkCommandHandler(services.Evaluator), "think", "th");
        dispatcher.Register(new TeleportCommandHandler(services.Look), "@teleport", "@tel");
        dispatcher.Register(new GetCommandHandler(), "get", "take", "g");
        dispatcher.Register(new DropCommandHandler(), "drop", "throw", "d");
        dispatcher.Register(new InventoryCommandHandler(services.Evaluator), "inventory", "i", "inv");
        dispatcher.Register(new ChannelCommandHandler(services.Channels), "@channel");
        dispatcher.Register(new ClockCommandHandler(services.Channels), "@clock");
        dispatcher.Register(new ComCommandHandler(services.Channels), "addcom", "delcom", "comtitle", "comlist", "@clist");
        dispatcher.Register(new ChatCommandHandler(services.Channels, services.Evaluator), "@chat", "+c");
        dispatcher.Register(new CemitCommandHandler(services.Channels, services.Evaluator), "@cemit", "@nscemit");
        dispatcher.Register(new MailCommandHandler(services.Mail, services.MailAliases), "@mail");
        dispatcher.Register(new MaliasCommandHandler(services.MailAliases), "@malias");
        dispatcher.Register(new CronCommandHandler(services.Cron, services.Queue), "@cron");
        dispatcher.Register(new HelpCommandHandler(services.Help, services.PlayerReceivesHtml, services.Suggestions), "help", "?");
        dispatcher.Register(new EmitCommandHandler(services.Evaluator), "@emit", "\\", "@lemit", "@nsemit", "@nslemit");
        dispatcher.Register(new PemitCommandHandler(), "@pemit", "@prompt", "@nspemit", "@nsprompt");
        dispatcher.Register(new RemitCommandHandler(), "@remit", "@nsremit");
        dispatcher.Register(new OemitCommandHandler(), "@oemit", "@nsoemit");
        dispatcher.Register(new WallCommandHandler(), "@wall", "@wizwall", "@rwall");
        dispatcher.Register(new WhisperCommandHandler(), "whisper", "w");
        dispatcher.Register(new PageCommandHandler(services.Evaluator), "page", "p");
    }
}

public sealed class InteractionModule : ICommandModule
{
    public void Register(CommandServices services)
    {
        var dispatcher = services.Dispatcher;
        dispatcher.Register(new HomeCommandHandler(services.Look), "home");
        dispatcher.Register(new EnterCommandHandler(services.Look, dispatcher, services.IsOnline), "enter");
        dispatcher.Register(new LeaveCommandHandler(services.Look, dispatcher, services.IsOnline), "leave");
        dispatcher.Register(new FollowCommandHandler(services.IsOnline), "follow");
        dispatcher.Register(new UnfollowCommandHandler(), "unfollow");
        dispatcher.Register(new DismissCommandHandler(), "dismiss");
        dispatcher.Register(new DesertCommandHandler(), "desert");
        dispatcher.Register(new GiveCommandHandler(), "give");
        dispatcher.Register(new UseCommandHandler(services.Queue, services.Evaluator), "use");
        dispatcher.Register(new EmptyCommandHandler(), "empty");
        dispatcher.Register(new TeachCommandHandler(services.Queue), "teach");
        dispatcher.Register(new ExamineCommandHandler(), "examine", "ex", "x", "brief");
    }
}

public sealed class FlowModule : ICommandModule
{
    public void Register(CommandServices services)
    {
        var dispatcher = services.Dispatcher;
        dispatcher.Register(new SwitchCommandHandler(dispatcher, services.Evaluator), "@switch", "@sw", "@select");
        dispatcher.Register(new DolistCommandHandler(dispatcher, services.Evaluator), "@dolist");
        dispatcher.Register(new TriggerCommandHandler(dispatcher, services.Evaluator), "@trigger", "@tr");
        dispatcher.Register(new IncludeCommandHandler(dispatcher, services.Evaluator), "@include");
        dispatcher.Register(new ActionListCommandHandler(dispatcher, "@assert", services.Evaluator, services.Queue), "@assert");
        dispatcher.Register(new ActionListCommandHandler(dispatcher, "@break", services.Evaluator, services.Queue), "@break");
        dispatcher.Register(new ActionListCommandHandler(dispatcher, "@retry", services.Evaluator, services.Queue), "@retry");
        dispatcher.Register(new VerbCommandHandler(dispatcher, services.Evaluator), "@verb");
        dispatcher.Register(new LogCommandHandler(services.Logs), "@log");
        dispatcher.Register(new LogWipeCommandHandler(services.Logs), "@logwipe");
        dispatcher.Register(new RestartCommandHandler(services.Queue), "@restart");
        dispatcher.Register(new SqlCommandHandler(services.Sql), "@sql");
        dispatcher.Register(new MapSqlCommandHandler(services.Sql, services.Queue), "@mapsql");
        dispatcher.Register(new HttpCommandHandler(services.Web, services.Queue), "@http");
        dispatcher.Register(new RespondCommandHandler(services.Http), "@respond");
        dispatcher.Register(new SlaveCommandHandler(), "@slave");
        dispatcher.Register(new FunctionCommandHandler(services.Functions), "@function");
        dispatcher.Register(new EnableCommandHandler(services.Config, false), "@disable");
        dispatcher.Register(new EnableCommandHandler(services.Config, true), "@enable");
        dispatcher.Register(new ConfigCommandHandler(services.Config), "@config");
        dispatcher.Register(new FlagCommandHandler(), "@flag");
        dispatcher.Register(new AttributeCommandHandler(), "@attribute");
        dispatcher.Register(new CommandCommandHandler(dispatcher), "@command");
        dispatcher.Register(new HookCommandHandler(dispatcher), "@hook");
        dispatcher.Register(new WcheckCommandHandler(), "@wcheck");
        dispatcher.Register(new BuyCommandHandler(services.Evaluator, services.Queue), "buy");
        dispatcher.Register(new DbckCommandHandler(), "@dbck");
        dispatcher.Register(new WarningsCommandHandler(), "@warnings");
        dispatcher.Register(new ForceCommandHandler(dispatcher), "@force");
        dispatcher.Register(new IfElseCommandHandler(dispatcher, services.Evaluator), "@ifelse", "@if", "@skip");
        dispatcher.Register(new CommentCommandHandler(), "@@");
    }
}

public sealed class AdministrationModule : ICommandModule
{
    public void Register(CommandServices services)
    {
        var dispatcher = services.Dispatcher;
        var config = services.Config;
        dispatcher.Register(new DestroyCommandHandler(() => config.UseQuota, () => config.StartingQuota), "@destroy", "@recycle", "@nuke");
        dispatcher.Register(new PurgeCommandHandler(() => config.UseQuota, () => config.StartingQuota), "@purge");
        dispatcher.Register(new CopyAttributeCommandHandler(), "@cpattr", "@mvattr");
        dispatcher.Register(new EditAttributeCommandHandler(), "@edit");
        dispatcher.Register(new GrepCommandHandler(), "@grep");
        dispatcher.Register(new AttributeLockCommandHandler(), "@atrlock");
        dispatcher.Register(new AttributeChownCommandHandler(), "@atrchown");
        dispatcher.Register(new LockFlagCommandHandler(), "@lset");
        dispatcher.Register(new DecompileCommandHandler(), "@decompile");
        dispatcher.Register(new MonikerCommandHandler(services.Evaluator), "@moniker");
        dispatcher.Register(new MessageCommandHandler(services.Evaluator), "@message");
        dispatcher.Register(new FindCommandHandler(), "@find");
        dispatcher.Register(new WhereIsCommandHandler(), "@whereis");
        dispatcher.Register(new EntrancesCommandHandler(), "@entrances");
        dispatcher.Register(new FirstExitCommandHandler(), "@firstexit");
        dispatcher.Register(new UptimeCommandHandler(), "@uptime");
        dispatcher.Register(new PoorCommandHandler(), "@poor");
        dispatcher.Register(new QuotaCommandHandler(config.UseQuota, config.StartingQuota, () => config.UseQuota, () => config.StartingQuota), "@quota");
        dispatcher.Register(new SquotaCommandHandler(config.UseQuota, config.StartingQuota, () => config.UseQuota, () => config.StartingQuota), "@squota");
        dispatcher.Register(new AllQuotaCommandHandler(config.StartingQuota, config.UseQuota, () => config.UseQuota, () => config.StartingQuota), "@allquota");
        dispatcher.Register(new ListCommandHandler(dispatcher, services.Functions, services.Motd), "@list");
        dispatcher.Register(new MotdCommandHandler(services.Motd), "@motd", "@listmotd", "@wizmotd", "@rejectmotd");
        dispatcher.Register(new SuggestCommandHandler(services.Suggestions), "@suggest");
        dispatcher.Register(
            new ReadCacheCommandHandler(services.Help, services.ScreenLines, services.TextCache, services.SyncSuggestions),
            "@readcache");
        dispatcher.Register(new DumpCommandHandler(_ => services.SaveAll()), "@dump");
        dispatcher.Register(new UndestroyCommandHandler(), "@undestroy", "@unrecycle");
        dispatcher.Register(new CloneCommandHandler(() => config.UseQuota, () => config.StartingQuota), "@clone");
        dispatcher.Register(new ChownCommandHandler(() => config.UseQuota, () => config.StartingQuota), "@chown");
        dispatcher.Register(new ChownAllCommandHandler(config.StartingQuota, () => config.UseQuota, () => config.StartingQuota), "@chownall");
        dispatcher.Register(new WipeCommandHandler(), "@wipe");
        dispatcher.Register(new LockCommandHandler(), "@lock", "@elock", "@ulock");
        dispatcher.Register(new UnlockCommandHandler(), "@unlock", "@eunlock", "@uunlock");
        dispatcher.Register(new StatsCommandHandler(), "@stats");
        dispatcher.Register(new ScoreCommandHandler(), "score");
        dispatcher.Register(new DoingCommandHandler(), "doing", "@doing");
        dispatcher.Register(new WhoCommandHandler(services.Sessions, evaluator: services.Evaluator), "who", "session");
        dispatcher.Register(new HideCommandHandler(services.Sessions), "@hide");
        dispatcher.Register(new BootCommandHandler(services.Sessions), "@boot");
        dispatcher.Register(new SocksetCommandHandler(services.Sessions), "@sockset");
        dispatcher.Register(new PollCommandHandler(services.Sessions), "@poll");
        dispatcher.Register(new PcreateCommandHandler(services.Functions.Passwords, () => config.DefaultHome, () => config.PlayerStart, () => config.StartingMoney), "@pcreate");
        dispatcher.Register(new PasswordCommandHandler(), "@password");
        dispatcher.Register(new NewPasswordCommandHandler(services.Logs), "@newpassword");
        dispatcher.Register(new VersionCommandHandler(), "@version");
        dispatcher.Register(new ClientCommandHandler(services.EditorSessions), "@client");
        dispatcher.Register(new PluginCommandHandler(services.Extensions), "@plugin", "@extension");
        dispatcher.Register(new RebootCommandHandler(services.Reboot), "@reboot");
        dispatcher.Register(new ShutdownCommandHandler(services.SaveAll, services.RequestShutdown, services.RequestQuietReboot), "@shutdown");
        dispatcher.Register(new SiteLockCommandHandler(services.SiteLocks, services.ConnectionLog, new NameLockFile(Path.Combine(services.GameDirectory, "names.cnf"))), "@sitelock");
    }
}

public sealed class AliasAndRestrictionModule : ICommandModule
{
    public void Register(CommandServices services)
    {
        var dispatcher = services.Dispatcher;
        foreach (var (alias, orig) in services.Config.CommandAliases)
            dispatcher.AddAlias(orig, alias);
        foreach (var reserved in services.Config.ReservedAliases)
            dispatcher.ReserveAlias(reserved);
        foreach (var (alias, orig) in services.Config.FunctionAliases)
            services.Functions.AddAlias(orig, alias);
        foreach (var (oldFlag, newFlag) in services.Config.FlagRenames)
        {
            var flagDef = PennMUSH.Core.Flags.FlagTable.FindFlag(oldFlag);
            if (flagDef != null && PennMUSH.Core.Flags.FlagTable.FindFlag(newFlag) == null)
                PennMUSH.Core.Flags.FlagTable.RenameFlag(flagDef, newFlag);
        }
        foreach (var (fn, rules) in services.Config.FunctionRestrictions)
        {
            foreach (var rule in rules)
                services.Functions.TryRestrict(fn, rule.Restriction);
        }

        dispatcher.AddRestriction("buy", "nogagged", null);
        foreach (var (cmd, rules) in services.Config.CommandRestrictions)
        {
            foreach (var rule in rules)
                dispatcher.AddRestriction(cmd, rule.Restriction, rule.ErrorMessage);
        }

        dispatcher.AddRestriction("@malias", "nogagged", null);
        dispatcher.AddRestriction("@sockset", "nogagged", null);
        dispatcher.AddRestriction("@log", "nogagged", null);
        dispatcher.AddRestriction("@password", "noguest", null);
        dispatcher.AddRestriction("@newpassword", "wizard", null);
        dispatcher.AddRestriction("@logwipe", "nogagged", null);
        dispatcher.AddRestriction("@restart", "nogagged", null);
        dispatcher.AddRestriction("@http", "nogagged", null);
        dispatcher.AddRestriction("@http", "noguest", null);
        dispatcher.AddRestriction("@respond", "nogagged", null);
        dispatcher.AddRestriction("@slave", "wizard", null);
        dispatcher.AddRestriction("@retry", "nogagged", null);
    }
}
