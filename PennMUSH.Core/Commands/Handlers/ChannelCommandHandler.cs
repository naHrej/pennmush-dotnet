namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Linq;
using PennMUSH.Core.Communication.Channels;
using PennMUSH.Core.Database;

public class ChannelCommandHandler : ICommandHandler
{
    private readonly ChannelRegistry _channels;

    public string Name => "@channel";

    public ChannelCommandHandler(ChannelRegistry channels)
    {
        _channels = channels;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var executor = db.GetObject(context.Executor);
        if (executor == null) return false;

        if (context.Switches.Contains("add") || context.Switches.Contains("create"))
        {
            if (!executor.IsWizard && !executor.IsGod)
            {
                context.Notify("Permission denied.");
                return false;
            }

            string chName = context.LeftArg.Trim();
            if (string.IsNullOrWhiteSpace(chName))
            {
                context.Notify("You must specify a channel name.");
                return false;
            }

            if (_channels.CreateChannel(chName, executor.Id))
            {
                context.Notify($"Channel <{chName}> created.");
                return true;
            }
            context.Notify($"Could not create channel <{chName}>. It may already exist.");
            return false;
        }

        if (context.Switches.Contains("delete") || context.Switches.Contains("remove"))
        {
            string chName = context.Arguments.Trim();
            var ch = _channels.GetChannel(chName);
            if (ch == null)
            {
                context.Notify($"Channel <{chName}> not found.");
                return false;
            }

            if (!executor.IsWizard && !executor.IsGod && ch.Owner != executor.Id)
            {
                context.Notify("Permission denied.");
                return false;
            }

            if (_channels.DeleteChannel(chName))
            {
                context.Notify($"Channel <{chName}> deleted.");
                return true;
            }
            return false;
        }

        if (context.Switches.Contains("join") || context.Switches.Contains("on"))
        {
            string chName = context.Arguments.Trim();
            var ch = _channels.GetChannel(chName);
            if (ch == null)
            {
                context.Notify($"Channel <{chName}> not found.");
                return false;
            }

            if (!ch.Visible(executor, db))
            {
                context.Notify("CHAT: I don't recognize that channel.");
                return false;
            }

            if (!ch.Passes("join", executor.Id, db))
            {
                if (executor.IsWizard)
                    context.Notify("CHAT: Warning: You don't meet channel join permissions! (joining anyway)");
                else
                {
                    context.Notify("Permission to join denied.");
                    return false;
                }
            }

            if (ch.AddMember(executor.Id))
            {
                context.Notify($"You join channel <{ch.Name}>.");
                return true;
            }
            context.Notify($"You are already on channel <{ch.Name}>.");
            return false;
        }

        if (context.Switches.Contains("leave") || context.Switches.Contains("off"))
        {
            string chName = context.Arguments.Trim();
            var ch = _channels.GetChannel(chName);
            if (ch == null)
            {
                context.Notify($"Channel <{chName}> not found.");
                return false;
            }

            if (ch.RemoveMember(executor.Id))
            {
                context.Notify($"You leave channel <{ch.Name}>.");
                return true;
            }
            context.Notify($"You are not on channel <{ch.Name}>.");
            return false;
        }

        if (context.Switches.Contains("who"))
        {
            string chName = context.Arguments.Trim();
            var ch = _channels.GetChannel(chName);
            if (ch == null)
            {
                context.Notify($"Channel <{chName}> not found.");
                return false;
            }

            if (!ch.Visible(executor, db))
            {
                context.Notify("CHAT: I don't recognize that channel.");
                return false;
            }

            context.Notify($"Members on <{ch.Name}>:");
            foreach (var member in ch.Members)
            {
                if (member.PlayerId != executor.Id && ch.Owner != executor.Id && !executor.IsWizard && ch.Passes("hide", member.PlayerId, db) && ch.HasLock("hide"))
                    continue;
                var p = db.GetObject(member.PlayerId);
                string pName = p != null ? p.Name : member.PlayerId.ToString();
                string title = !string.IsNullOrEmpty(member.Title) ? $" [{member.Title}]" : "";
                context.Notify($"  {pName}{title}");
            }
            return true;
        }

        if (context.Switches.Contains("title"))
        {
            string chName = context.LeftArg.Trim();
            string title = context.RightArg.Trim();
            var ch = _channels.GetChannel(chName);
            if (ch == null)
            {
                context.Notify($"Channel <{chName}> not found.");
                return false;
            }

            var member = ch.GetMember(executor.Id);
            if (member == null)
            {
                context.Notify($"You are not on channel <{ch.Name}>.");
                return false;
            }

            member.Title = title;
            context.Notify($"Title for channel <{ch.Name}> set to: {title}");
            return true;
        }

        if (context.Switches.Contains("recall"))
        {
            string chName = context.LeftArg.Trim();
            int count = 20;
            if (!string.IsNullOrEmpty(context.RightArg) && int.TryParse(context.RightArg.Trim(), out int n) && n > 0)
                count = n;

            var ch = _channels.GetChannel(chName);
            if (ch == null)
            {
                context.Notify($"Channel <{chName}> not found.");
                return false;
            }

            if (!ch.HasMember(executor.Id) && !executor.IsWizard)
            {
                context.Notify($"You are not on channel <{ch.Name}>.");
                return false;
            }

            var messages = ch.GetRecentMessages(count);
            context.Notify($"--- Recall buffer for <{ch.Name}> ---");
            foreach (var msg in messages)
            {
                context.Notify(msg.Text);
            }
            context.Notify($"--- End recall ---");
            return true;
        }

        // Default or @channel/list: list channels
        var allChannels = _channels.GetAllChannels();
        if (allChannels.Count == 0)
        {
            context.Notify("No channels exist.");
            return true;
        }

        context.Notify("Channel Name          Owner      Members Status");
        context.Notify("--------------------------------------------------");
        foreach (var ch in allChannels)
        {
            if (!ch.Visible(executor, db))
                continue;
            var ownerObj = db.GetObject(ch.Owner);
            string ownerName = ownerObj != null ? ownerObj.Name : ch.Owner.ToString();
            string onStatus = ch.HasMember(executor.Id) ? "[Joined]" : "[Off]";
            context.Notify($"{ch.Name,-20} {ownerName,-10} {ch.Members.Count,-7} {onStatus}");
        }

        return true;
    }
}
