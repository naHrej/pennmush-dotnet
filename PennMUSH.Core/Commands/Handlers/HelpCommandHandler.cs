namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Linq;
using System.Text.RegularExpressions;
using PennMUSH.Core.Database;
using PennMUSH.Core.Help;
using PennMUSH.Core.Types;

public class HelpCommandHandler : ICommandHandler
{
    private readonly HelpStore _helpStore;
    private readonly Func<Dbref, bool>? _receivesHtml;
    private readonly SuggestionVocabulary? _suggestions;

    public string Name => "help";

    public HelpCommandHandler(HelpStore helpStore, Func<Dbref, bool>? receivesHtml = null, SuggestionVocabulary? suggestions = null)
    {
        _helpStore = helpStore;
        _receivesHtml = receivesHtml;
        _suggestions = suggestions;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        string topic = string.IsNullOrWhiteSpace(context.Arguments) ? "help" : context.Arguments.Trim();
        string? result = _helpStore.Lookup(topic);
        if (string.IsNullOrEmpty(result))
        {
            string? suggestion = _suggestions?.Closest("HELP", topic);
            if (suggestion != null)
                context.Notify(ForClient(Feedback.NoHelpEntryDidYouMean.Text(("topic", topic), ("suggestion", suggestion)), context));
            else
                context.Notify(ForClient(Feedback.NoEntryFor.Text(("topic", topic)), context));
            return false;
        }

        context.Notify(ForClient(result, context));
        return true;
    }

    private string ForClient(string text, CommandContext context)
    {
        if (_receivesHtml?.Invoke(context.Executor) != true)
            return text;

        string escaped = text.Replace("<", "&lt;").Replace(">", "&gt;");
        escaped = Regex.Replace(escaped, " {2,}|\t", match => string.Concat(Enumerable.Repeat("&nbsp;", match.Value[0] == '\t' ? 8 : match.Length)));
        return "<div style=\"white-space:pre-wrap\">" + escaped + "</div>";
    }
}
