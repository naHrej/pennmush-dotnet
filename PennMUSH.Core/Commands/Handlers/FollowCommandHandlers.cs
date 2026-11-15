namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Database;
using PennMUSH.Core.Types;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Locks;

public class FollowCommandHandler : ICommandHandler
{
    private readonly Func<Dbref, bool>? _connected;

    public string Name => "follow";

    public FollowCommandHandler(Func<Dbref, bool>? connected = null) => _connected = connected;

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null || (actor.Type != ObjectType.Player && actor.Type != ObjectType.Thing))
            return false;

        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            context.Notify("You are following: " + FollowerList.Names(db, FollowerList.Read(actor, FollowerList.Following)));
            context.Notify("You are followed by: " + FollowerList.Names(db, FollowerList.Read(actor, FollowerList.Followers)));
            return true;
        }

        if (!FollowerList.TryMatch(db, actor, context.Arguments, true, out var leader, out bool ambiguous) || leader == null)
        {
            context.Notify(ambiguous ? "I can't tell which one to follow." : "You don't see that here.");
            return false;
        }

        var room = actor.Location.IsValid ? db.GetObject(actor.Location) : null;
        bool offline = leader.Type == ObjectType.Player && _connected != null && !_connected(leader.Id);
        if (!actor.Location.IsValid || offline || !FollowerList.CanSee(actor, leader, room))
        {
            context.Notify("You don't see that here.");
            return false;
        }

        if (leader.Type != ObjectType.Player && leader.Type != ObjectType.Thing)
        {
            context.Notify("You can only follow players and things.");
            return false;
        }

        if (leader.Id == actor.Id)
        {
            context.Notify("You chase your tail for a while and feel silly.");
            return false;
        }

        if (FollowerList.IsFollowing(actor, leader.Id))
        {
            context.Notify($"You're already following {leader.Name}.");
            return false;
        }

        if (!leader.EvalLock(LockType.Follow, actor.Id, db))
        {
            context.Notify(FollowerList.Spoken(leader, "FOLLOW_LOCK`FAILURE", "You're not allowed to follow."));
            return false;
        }

        FollowerList.Add(leader, actor);
        context.NotifyTarget(leader.Id, $"{actor.Name} begins following you.");
        context.Notify(FollowerList.Spoken(leader, "FOLLOW", $"You begin following {leader.Name}."));
        FollowerList.TellOthers(context, db, actor, leader, "OFOLLOW");
        return true;
    }
}

public class UnfollowCommandHandler : ICommandHandler
{
    public string Name => "unfollow";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null)
            return false;

        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            FollowerList.ClearFollowing(actor, db, context, true);
            context.Notify("You stop following anyone.");
            return true;
        }

        if (!FollowerList.TryMatch(db, actor, context.Arguments, false, out var leader, out bool ambiguous) || leader == null)
        {
            context.Notify(ambiguous ? "I can't tell which one to stop following." : "I don't see that here.");
            return false;
        }

        if (!FollowerList.IsFollowing(actor, leader.Id))
        {
            context.Notify($"You're not following {leader.Name}.");
            return false;
        }

        Stop(context, leader, actor);
        return true;
    }

    internal static void Stop(CommandContext context, MUSHObject leader, MUSHObject follower)
    {
        FollowerList.Delete(leader, follower);
        context.NotifyTarget(follower.Id, FollowerList.Spoken(leader, "UNFOLLOW", $"You stop following {leader.Name}."));
        context.NotifyTarget(leader.Id, $"{follower.Name} stops following you.");
    }
}

public class DismissCommandHandler : ICommandHandler
{
    public string Name => "dismiss";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null)
            return false;

        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            FollowerList.ClearFollowers(actor, db, context, true);
            context.Notify("You dismiss all your followers.");
            return true;
        }

        if (!FollowerList.TryMatch(db, actor, context.Arguments, false, out var follower, out _))
        {
            context.Notify("I don't recognize who you want to dismiss.");
            return false;
        }

        if (follower == null || !FollowerList.IsFollowing(follower, actor.Id))
        {
            context.Notify($"{(follower == null ? context.Arguments.Trim() : follower.Name)} isn't following you.");
            return false;
        }

        UnfollowCommandHandler.Stop(context, actor, follower);
        return true;
    }
}

public class DesertCommandHandler : ICommandHandler
{
    public string Name => "desert";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null)
            return false;

        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            FollowerList.ClearFollowers(actor, db, context, true);
            FollowerList.ClearFollowing(actor, db, context, true);
            context.Notify("You desert everyone you're leading or following.");
            return true;
        }

        if (!FollowerList.TryMatch(db, actor, context.Arguments, false, out var who, out _) || who == null)
        {
            context.Notify("I don't recognize who you want to desert.");
            return false;
        }

        bool leads = FollowerList.IsFollowing(who, actor.Id);
        bool follows = FollowerList.IsFollowing(actor, who.Id);
        if (!leads && !follows)
        {
            context.Notify($"{who.Name} isn't following you, nor vice versa.");
            return false;
        }

        UnfollowCommandHandler.Stop(context, actor, who);
        UnfollowCommandHandler.Stop(context, who, actor);
        return true;
    }
}
