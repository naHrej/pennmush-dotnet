namespace PennMUSH.Core.Commands;

using PennMUSH.Core.Database;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;

public static class SpeechMod
{
    public static string Apply(ObjectDatabase db, ExpressionEvaluator? evaluator, Dbref speaker, string message, string token)
    {
        var obj = db.GetObject(speaker);
        if (obj == null)
            return message;

        var attr = obj.GetAttribute("SPEECHMOD", db);
        if (attr == null)
            return message;

        string result = evaluator == null
            ? attr.Value.Replace("%0", message, StringComparison.Ordinal).Replace("%1", token, StringComparison.Ordinal)
            : evaluator.Evaluate(attr.Value, new EvaluationContext(speaker, speaker, location: obj.Location, args: new[] { message, token }));

        return string.IsNullOrEmpty(result) ? message : result;
    }
}
