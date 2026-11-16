namespace PennMUSH.Core.Commands.Handlers;

using PennMUSH.Core.Database;
using PennMUSH.Core.Help;

public class SuggestCommandHandler : ICommandHandler
{
    private readonly SuggestionVocabulary _vocabulary;

    public string Name => "@suggest";

    public SuggestCommandHandler(SuggestionVocabulary vocabulary) => _vocabulary = vocabulary;

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        bool wizard = actor != null && actor.IsWizard;
        if (context.Switches.Contains("add"))
            return Add(context, wizard);
        if (context.Switches.Contains("delete"))
            return Delete(context, wizard);
        return List(context);
    }

    private bool Add(CommandContext context, bool wizard)
    {
        if (!wizard)
        {
            context.Notify("Your suggestion is not welcome.");
            return false;
        }

        if (string.IsNullOrEmpty(context.LeftArg) || string.IsNullOrEmpty(context.RightArg))
        {
            context.Notify("What did you want to add?");
            return false;
        }

        if (_vocabulary.Add(context.LeftArg, context.RightArg))
        {
            context.Notify("Suggestion vocabulary word added.");
            return true;
        }

        context.Notify("Unable to add word.");
        return false;
    }

    private bool Delete(CommandContext context, bool wizard)
    {
        if (!wizard)
        {
            context.Notify("Permission denied.");
            return false;
        }

        if (string.IsNullOrEmpty(context.LeftArg) || string.IsNullOrEmpty(context.RightArg))
        {
            context.Notify("What did you want to delete?");
            return false;
        }

        if (_vocabulary.Delete(context.LeftArg, context.RightArg))
        {
            context.Notify("Suggestion vocabulary word deleted.");
            return true;
        }

        context.Notify("Unable to delete word.");
        return false;
    }

    private bool List(CommandContext context)
    {
        context.Notify("Vocabulary suggestion categories:");
        var categories = _vocabulary.Categories();
        if (categories.Count == 0)
        {
            context.Notify("None found.");
            return true;
        }

        foreach (string category in categories)
            context.Notify("\t" + category);
        return true;
    }
}
