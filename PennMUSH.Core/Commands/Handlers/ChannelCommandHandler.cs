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
                context.Notify(Feedback.PermissionDenied);
                return false;
            }

            string chName = context.LeftArg.Trim();
            if (string.IsNullOrWhiteSpace(chName))
            {
                context.Notify(Feedback.YouMustSpecifyAChannelName);
                return false;
            }

            if (_channels.CreateChannel(chName, executor.Id))
            {
                context.Notify(Feedback.ChannelCreated, ("channel", $"{chName}"));
                return true;
            }
            context.Notify(Feedback.CouldNotCreateChannelItMayAlreadyExist, ("channel", $"{chName}"));
            return false;
        }

        if (context.Switches.Contains("delete") || context.Switches.Contains("remove"))
        {
            string chName = context.Arguments.Trim();
            var ch = _channels.GetChannel(chName);
            if (ch == null)
            {
                context.Notify(Feedback.ChannelNotFound, ("channel", $"{chName}"));
                return false;
            }

            if (!executor.IsWizard && !executor.IsGod && ch.Owner != executor.Id)
            {
                context.Notify(Feedback.PermissionDenied);
                return false;
            }

            if (_channels.DeleteChannel(chName))
            {
                context.Notify(Feedback.ChannelDeleted, ("channel", $"{chName}"));
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
                context.Notify(Feedback.ChannelNotFound, ("channel", $"{chName}"));
                return false;
            }

            if (!ch.Visible(executor, db))
            {
                context.Notify(Feedback.ChatIDontRecognizeThatChannel);
                return false;
            }

            if (!ch.Passes("join", executor.Id, db))
            {
                if (executor.IsWizard)
                    context.Notify(Feedback.ChatWarningYouDontMeetChannelJoinPermissionsJoiningAnyway);
                else
                {
                    context.Notify(Feedback.PermissionToJoinDenied);
                    return false;
                }
            }

            if (ch.AddMember(executor.Id))
            {
                context.Notify(Feedback.YouJoinChannel, ("channel", $"{ch.Name}"));
                return true;
            }
            context.Notify(Feedback.YouAreAlreadyOnChannel, ("channel", $"{ch.Name}"));
            return false;
        }

        if (context.Switches.Contains("leave") || context.Switches.Contains("off"))
        {
            string chName = context.Arguments.Trim();
            var ch = _channels.GetChannel(chName);
            if (ch == null)
            {
                context.Notify(Feedback.ChannelNotFound, ("channel", $"{chName}"));
                return false;
            }

            if (ch.RemoveMember(executor.Id))
            {
                context.Notify(Feedback.YouLeaveChannel, ("channel", $"{ch.Name}"));
                return true;
            }
            context.Notify(Feedback.YouAreNotOnChannel, ("channel", $"{ch.Name}"));
            return false;
        }

        if (context.Switches.Contains("who"))
        {
            string chName = context.Arguments.Trim();
            var ch = _channels.GetChannel(chName);
            if (ch == null)
            {
                context.Notify(Feedback.ChannelNotFound, ("channel", $"{chName}"));
                return false;
            }

            if (!ch.Visible(executor, db))
            {
                context.Notify(Feedback.ChatIDontRecognizeThatChannel);
                return false;
            }

            context.Notify(Feedback.MembersOn, ("value", $"{ch.Name}"));
            foreach (var member in ch.Members)
            {
                if (member.PlayerId != executor.Id && ch.Owner != executor.Id && !executor.IsWizard && ch.Passes("hide", member.PlayerId, db) && ch.HasLock("hide"))
                    continue;
                var p = db.GetObject(member.PlayerId);
                string pName = p != null ? p.Name : member.PlayerId.ToString();
                string title = !string.IsNullOrEmpty(member.Title) ? $" [{member.Title}]" : "";
                context.Notify(Feedback.ValueValue, ("value", $"{pName}"), ("value2", $"{title}"));
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
                context.Notify(Feedback.ChannelNotFound, ("channel", $"{chName}"));
                return false;
            }

            var member = ch.GetMember(executor.Id);
            if (member == null)
            {
                context.Notify(Feedback.YouAreNotOnChannel, ("channel", $"{ch.Name}"));
                return false;
            }

            member.Title = title;
            context.Notify(Feedback.TitleForChannelSetTo, ("channel", $"{ch.Name}"), ("value", $"{title}"));
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
                context.Notify(Feedback.ChannelNotFound, ("channel", $"{chName}"));
                return false;
            }

            if (!ch.HasMember(executor.Id) && !executor.IsWizard)
            {
                context.Notify(Feedback.YouAreNotOnChannel, ("channel", $"{ch.Name}"));
                return false;
            }

            var messages = ch.GetRecentMessages(count);
            context.Notify(Feedback.RecallBufferFor, ("value", $"{ch.Name}"));
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
            context.Notify(Feedback.NoChannelsExist);
            return true;
        }

        context.Notify(Feedback.ChannelNameOwnerMembersStatus);
        context.Notify(Feedback.Dashes50);
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
