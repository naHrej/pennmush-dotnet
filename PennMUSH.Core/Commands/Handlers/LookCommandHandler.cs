namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Collections.Generic;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Locks;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;

/// <summary>
/// Explicit is a typed look. Automatic is the look shown on connect and after arrival.
/// </summary>
public enum LookKind
{
    Explicit,
    Automatic
}

public class LookCommandHandler : ICommandHandler
{
    private readonly ExpressionEvaluator? _evaluator;

    public string Name => "look";

    public LookCommandHandler(ExpressionEvaluator? evaluator = null)
    {
        _evaluator = evaluator;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var viewer = db.GetObject(context.Executor);
        if (viewer == null)
            return false;

        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            if (!viewer.Location.IsValid)
            {
                context.Notify("You are nowhere.");
                return false;
            }

            var here = db.GetObject(viewer.Location);
            if (here == null)
            {
                context.Notify("I can't see that here.");
                return false;
            }

            ShowPlace(context, db, viewer, here, LookKind.Explicit);
            return true;
        }

        var target = db.MatchObject(context.Executor, context.Arguments);
        if (target == null)
        {
            context.Notify("I can't see that here.");
            return false;
        }

        if (target.Type == ObjectType.Room || target.Id == viewer.Location)
            ShowPlace(context, db, viewer, target, LookKind.Explicit);
        else
            ShowObject(context, db, viewer, target);

        return true;
    }

    /// <summary>
    /// Show the viewer's current location. Connect and movement use Automatic.
    /// </summary>
    public void ShowHere(CommandContext context, ObjectDatabase db, Dbref viewerId, LookKind kind)
    {
        var viewer = db.GetObject(viewerId);
        if (viewer == null || !viewer.Location.IsValid)
            return;

        var place = db.GetObject(viewer.Location);
        if (place == null)
            return;

        ShowPlace(context, db, viewer, place, kind);
    }

    private void ShowPlace(CommandContext context, ObjectDatabase db, MUSHObject viewer, MUSHObject place, LookKind kind)
    {
        bool describe = kind == LookKind.Explicit || !viewer.HasFlag("TERSE");
        Tell(context, viewer.Id, NameForLook(db, viewer, place));

        if (describe)
        {
            if (place.Type == ObjectType.Room)
            {
                Tell(context, viewer.Id, FormattedText(db, viewer, place, "DESCRIBE", "DESCFORMAT", null));
                TellOthers(context, place.Id, viewer.Id, EvalAttr(place, viewer, "ODESCRIBE"));
            }
            else
            {
                var inside = EvalAttr(place, viewer, "IDESCRIBE");
                if (inside != null)
                {
                    Tell(context, viewer.Id, FormattedText(db, viewer, place, "IDESCRIBE", "IDESCFORMAT", null));
                    TellOthers(context, place.Id, viewer.Id, EvalAttr(place, viewer, "OIDESCRIBE"));
                }
                else if (place.GetAttribute("IDESCFORMAT") != null)
                {
                    Tell(context, viewer.Id, FormattedText(db, viewer, place, "DESCRIBE", "IDESCFORMAT", null));
                }
                else
                {
                    Tell(context, viewer.Id, FormattedText(db, viewer, place, "DESCRIBE", "DESCFORMAT", null));
                    TellOthers(context, place.Id, viewer.Id, EvalAttr(place, viewer, "ODESCRIBE"));
                }
            }
        }

        if (place.Type == ObjectType.Room)
        {
            bool allowed = place.EvalLock(LockType.Basic, viewer.Id, db);
            if (kind == LookKind.Automatic && viewer.HasFlag("TERSE"))
            {
                TellOthers(context, place.Id, viewer.Id, EvalAttr(place, viewer, allowed ? "OSUCCESS" : "OFAILURE"));
            }
            else if (allowed)
            {
                Tell(context, viewer.Id, EvalAttr(place, viewer, "SUCCESS"));
                TellOthers(context, place.Id, viewer.Id, EvalAttr(place, viewer, "OSUCCESS"));
            }
            else
            {
                Tell(context, viewer.Id, EvalAttr(place, viewer, "FAILURE"));
                TellOthers(context, place.Id, viewer.Id, EvalAttr(place, viewer, "OFAILURE"));
            }
        }

        ShowVisible(context, db, viewer, place, "Contents:", contents: true, formatName: "CONFORMAT");
        if (place.Type == ObjectType.Room)
            ShowVisible(context, db, viewer, place, "Obvious exits:", contents: false, formatName: "EXITFORMAT");
    }

    private void ShowObject(CommandContext context, ObjectDatabase db, MUSHObject viewer, MUSHObject target)
    {
        Tell(context, viewer.Id, VisibleName(target));
        Tell(context, viewer.Id, FormattedText(db, viewer, target, "DESCRIBE", "DESCFORMAT", "You see nothing special."));
        TellOthers(context, target.Location, viewer.Id, EvalAttr(target, viewer, "ODESCRIBE"));

        if ((target.Type == ObjectType.Thing || target.Type == ObjectType.Player) && !target.HasFlag("OPAQUE"))
            ShowVisible(context, db, viewer, target, "Carrying:", contents: true, formatName: "CONFORMAT");
    }

    private void ShowVisible(CommandContext context, ObjectDatabase db, MUSHObject viewer, MUSHObject place, string header, bool contents, string? formatName = null)
    {
        bool privileged = viewer.IsWizard;
        bool hidden = contents && place.HasFlag("DARK") && !privileged;
        var visible = new List<MUSHObject>();
        if (!hidden)
        {
            if (contents)
            {
                foreach (var item in db.GetContents(place.Id))
                {
                    if (item.Id == viewer.Id)
                        continue;
                    if (item.HasFlag("DARK") && !privileged)
                        continue;
                    visible.Add(item);
                }
            }
            else
            {
                bool roomDark = place.HasFlag("DARK");
                foreach (var exit in db.GetExits(place.Id))
                {
                    if (privileged || (roomDark ? exit.HasFlag("LIGHT") : !exit.HasFlag("DARK")))
                        visible.Add(exit);
                }
            }
        }

        if (formatName != null)
        {
            string? formatted = FormatAttributes.Evaluate(
                db,
                _evaluator,
                place,
                viewer.Id,
                viewer.Id,
                formatName,
                string.Join(' ', visible.Select(item => item.Id.ToString())),
                string.Join('|', visible.Select(VisibleName)));
            if (formatted != null)
            {
                Tell(context, viewer.Id, formatted);
                return;
            }
        }

        if (visible.Count == 0)
            return;

        Tell(context, viewer.Id, header);
        foreach (var item in visible)
            Tell(context, viewer.Id, "  " + VisibleName(item));
    }

    private string NameForLook(ObjectDatabase db, MUSHObject viewer, MUSHObject place)
    {
        string plain = VisibleName(place);
        return FormatAttributes.Evaluate(db, _evaluator, place, viewer.Id, viewer.Id, "NAMEFORMAT", place.Id.ToString(), plain) ?? plain;
    }

    private string? FormattedText(ObjectDatabase db, MUSHObject viewer, MUSHObject host, string bodyName, string formatName, string? fallback)
    {
        string? body = EvalAttr(host, viewer, bodyName);
        string? format = FormatAttributes.Evaluate(db, _evaluator, host, viewer.Id, viewer.Id, formatName, body ?? string.Empty);
        if (format != null)
            return format;
        return body ?? fallback;
    }

    private string? EvalAttr(MUSHObject obj, MUSHObject viewer, string name)
    {
        var attr = obj.GetAttribute(name);
        if (attr == null || string.IsNullOrWhiteSpace(attr.Value))
            return null;

        if (_evaluator == null)
            return attr.Value;

        var evalCtx = new EvaluationContext(obj.Id, viewer.Id, caller: viewer.Id, location: obj.Location);
        var text = _evaluator.Evaluate(attr.Value, evalCtx);
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static void Tell(CommandContext context, Dbref viewer, string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (viewer == context.Executor)
            context.Notify(message);
        else
            context.NotifyTarget(viewer, message);
    }

    private static void TellOthers(CommandContext context, Dbref place, Dbref viewer, string? message)
    {
        if (string.IsNullOrWhiteSpace(message) || !place.IsValid)
            return;

        context.NotifyExcept(place, viewer, message);
    }

    private static string VisibleName(MUSHObject obj)
    {
        int semi = obj.Name.IndexOf(';');
        return semi > 0 ? obj.Name[..semi] : obj.Name;
    }
}
