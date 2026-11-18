namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Linq;
using PennMUSH.Core.Communication.Channels;
using PennMUSH.Core.Database;
using PennMUSH.Core.Types;

public class ComCommandHandler : ICommandHandler
{
    private const int MaxAliasLength = 15;
    private const int MaxTitleLength = 80;
    private readonly ChannelRegistry _channels;

    public string Name => "addcom";

    public ComCommandHandler(ChannelRegistry channels) => _channels = channels;

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null)
            return false;

        string command = context.CommandName.TrimStart('@');
        if (command.Equals("addcom", StringComparison.OrdinalIgnoreCase))
            return Add(context, actor.Id);
        if (command.Equals("delcom", StringComparison.OrdinalIgnoreCase))
            return Remove(context, actor.Id);
        if (command.Equals("comtitle", StringComparison.OrdinalIgnoreCase))
            return Title(context, actor.Id);
        if (command.Equals("comlist", StringComparison.OrdinalIgnoreCase))
            return ListAliases(context, actor.Id);
        return ListChannels(context, actor.Id);
    }

    private bool Add(CommandContext context, Dbref player)
    {
        if (!context.RawCommand.Contains('=') || string.IsNullOrWhiteSpace(context.LeftArg) || string.IsNullOrWhiteSpace(context.RightArg))
        {
            context.Notify(Feedback.UsageAddcomAliasChannel);
            return false;
        }
        string alias = context.LeftArg.Trim();
        if (!OkAlias(alias))
        {
            context.Notify(Feedback.InvalidAlias);
            return false;
        }
        if (_channels.HasAlias(player, alias))
        {
            context.Notify(Feedback.ThatAliasIsAlreadyInUse);
            return false;
        }
        var channel = _channels.MatchChannel(context.RightArg, out bool ambiguous);
        if (ambiguous)
        {
            context.Notify(Feedback.IDontKnowWhichChannelYouMean);
            return false;
        }
        if (channel == null)
        {
            context.Notify(Feedback.IDontRecogniseThatChannel);
            return false;
        }
        if (channel.AddMember(player))
            context.Notify(Feedback.YouJoinChannel, ("channel", $"{channel.Name}"));
        _channels.SetAlias(player, alias, channel.Name);
        context.Notify(Feedback.AliasAddedForChannel, ("alias", $"{alias}"), ("channel", $"{channel.Name}"));
        return true;
    }

    private bool Remove(CommandContext context, Dbref player)
    {
        string alias = string.IsNullOrWhiteSpace(context.LeftArg) ? context.Arguments.Trim() : context.LeftArg.Trim();
        if (!_channels.RemoveAlias(player, alias, out var channelName) || channelName == null)
        {
            context.Notify(Feedback.NoSuchAlias);
            return false;
        }
        if (_channels.CountAliases(player, channelName) > 0)
        {
            context.Notify(Feedback.AliasRemoved);
            return true;
        }
        var channel = _channels.GetChannel(channelName);
        if (channel != null && channel.RemoveMember(player))
        {
            context.Notify(Feedback.YouLeaveChannel, ("channel", $"{channel.Name}"));
            return true;
        }
        context.Notify(Feedback.AliasRemoved);
        return true;
    }

    private bool Title(CommandContext context, Dbref player)
    {
        string alias = context.LeftArg.Trim();
        if (!_channels.TryGetAlias(player, alias, out var channelName) || channelName == null)
        {
            context.Notify(Feedback.NoSuchAlias2, ("alias", $"{alias}"));
            return false;
        }
        var channel = _channels.GetChannel(channelName);
        var member = channel?.GetMember(player);
        if (channel == null || member == null)
        {
            context.Notify(Feedback.YouAreNotOnChannel, ("channel", $"{channelName}"));
            return false;
        }
        if (!context.RawCommand.Contains('='))
        {
            if (string.IsNullOrEmpty(member.Title))
                context.Notify(Feedback.YouHaveNoTitleSetOn, ("value", $"{channel.Name}"));
            else
                context.Notify(Feedback.YourTitleOnIs, ("value", $"{channel.Name}"), ("value2", $"{member.Title}"));
            return true;
        }
        string title = context.RightArg;
        if (string.IsNullOrEmpty(title))
        {
            member.Title = string.Empty;
            context.Notify(Feedback.TitleClearedForChannel, ("channel", $"{channel.Name}"));
            return true;
        }
        if (title.Length > MaxTitleLength)
        {
            context.Notify(Feedback.TitleTooLong);
            return false;
        }
        if (title.Any(ch => ch == '\a' || (char.IsWhiteSpace(ch) && ch != ' ') || char.IsControl(ch)))
        {
            context.Notify(Feedback.InvalidCharacterInTitle);
            return false;
        }
        member.Title = title;
        context.Notify(Feedback.TitleSetForChannel, ("channel", $"{channel.Name}"));
        return true;
    }

    private bool ListAliases(CommandContext context, Dbref player)
    {
        context.Notify($"{"Alias",-18} {"Channel",-30} {"Status",-8} Title");
        foreach (var (alias, channelName) in _channels.ListAliases(player))
        {
            var channel = _channels.GetChannel(channelName);
            var member = channel?.GetMember(player);
            if (channel == null || member == null)
                continue;
            string status = member.IsMuted ? "Off" : "On";
            context.Notify($"{alias,-18} {channel.Name,-30} {status,-8} {member.Title}");
        }
        context.Notify(Feedback.EndOfComlist);
        return true;
    }

    private bool ListChannels(CommandContext context, Dbref player)
    {
        string prefix = context.Arguments.Trim();
        var channels = _channels.GetAllChannels()
            .Where(channel => prefix.Length == 0 || channel.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();
        bool full = context.Switches.Contains("full");
        if (full)
            context.Notify($"{"Name",-30} {"Users",5} {"Msgs",8} {"Flags",-8} Status");
        else
            context.Notify($"{"Name",-30} {"Users",5} Status");
        foreach (var channel in channels)
        {
            var member = channel.GetMember(player);
            string status = member == null ? "Off" : member.IsMuted ? "Gag" : "On";
            if (full)
                context.Notify($"{channel.Name,-30} {channel.Members.Count,5} {channel.MessageCount,8} {Flags(channel),-8} {status}");
            else
                context.Notify($"{channel.Name,-30} {channel.Members.Count,5} {status}");
        }
        return true;
    }

    private static string Flags(Channel channel)
    {
        string flags = "";
        if (channel.Flags.HasFlag(ChannelFlags.Player)) flags += "P";
        if (channel.Flags.HasFlag(ChannelFlags.Quiet)) flags += "Q";
        if (channel.Flags.HasFlag(ChannelFlags.Admin)) flags += "A";
        if (channel.Flags.HasFlag(ChannelFlags.Disabled)) flags += "D";
        return flags.Length == 0 ? "-" : flags;
    }

    private static bool OkAlias(string alias)
    {
        if (alias.Length == 0 || alias.Length > MaxAliasLength || alias.Contains('`'))
            return false;
        return alias.All(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-' or '.');
    }
}
