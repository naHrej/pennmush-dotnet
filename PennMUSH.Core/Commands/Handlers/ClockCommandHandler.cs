namespace PennMUSH.Core.Commands.Handlers;

using System.Text;
using PennMUSH.Core.Communication.Channels;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Locks;

public class ClockCommandHandler : ICommandHandler
{
    private readonly ChannelRegistry _channels;

    public ClockCommandHandler(ChannelRegistry channels) => _channels = channels;

    public string Name => "@clock";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null)
            return false;

        string? kind = Kind(context);
        if (kind == null)
        {
            context.Notify("You must specify a type of lock!");
            return false;
        }

        string name = context.LeftArg.Trim();
        var channel = _channels.GetChannel(name);
        if (channel == null)
        {
            context.Notify("CHAT: I don't recognize that channel.");
            return false;
        }
        if (!channel.CanModify(actor, db))
        {
            context.Notify($"CHAT: Channel <{channel.Name}> resists.");
            return false;
        }

        string key = context.RightArg.Trim();
        if (key.Length == 0)
        {
            channel.ClearLock(kind);
            context.Notify($"CHAT: {Label(kind)} on <{channel.Name}> reset.");
            return true;
        }
        if (!Understands(key))
        {
            context.Notify("CHAT: I don't understand that key.");
            return false;
        }

        channel.SetLock(kind, key, LockParser.Parse(key));
        context.Notify($"CHAT: {Label(kind)} on <{channel.Name}> set.");
        return true;
    }

    private static string? Kind(CommandContext context)
    {
        if (context.Switches.Contains("join"))
            return "join";
        if (context.Switches.Contains("speak"))
            return "speak";
        if (context.Switches.Contains("mod"))
            return "mod";
        if (context.Switches.Contains("see"))
            return "see";
        if (context.Switches.Contains("hide"))
            return "hide";
        return null;
    }

    private static string Label(string kind) => kind switch
    {
        "join" => "Joinlock",
        "speak" => "Speaklock",
        "mod" => "Modlock",
        "see" => "Seelock",
        _ => "Hidelock"
    };

    private static bool Understands(string key)
    {
        foreach (char ch in key)
        {
            if (char.IsLetter(ch))
                return false;
            if (char.IsDigit(ch) || "#&|!() \t".Contains(ch))
                continue;
            return false;
        }
        return true;
    }
}

public static class ChannelAccess
{
    public static bool CanModify(this Channel channel, MUSHObject actor, ObjectDatabase db) =>
        actor.IsWizard || channel.Owner == actor.Id || (channel.HasLock("mod") && channel.Passes("mod", actor.Id, db));

    public static bool Visible(this Channel channel, MUSHObject actor, ObjectDatabase db) =>
        actor.IsWizard || channel.Owner == actor.Id || channel.Passes("see", actor.Id, db);
}
