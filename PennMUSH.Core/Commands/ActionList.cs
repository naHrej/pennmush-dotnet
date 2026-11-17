namespace PennMUSH.Core.Commands;

using System.Globalization;

/// <summary>
/// The list <c>@include</c> is running. <c>@assert</c> and <c>@break</c> stop it.
/// <c>@retry</c> runs it again with new <c>%0</c> through <c>%9</c>.
/// </summary>
public sealed class ActionFrame
{
    public const int ArgumentCount = 10;

    public ActionFrame? Parent { get; init; }
    public bool NoBreak { get; init; }
    public bool Break { get; set; }
    public bool Retry { get; set; }
    public string[] Args { get; } = new string[ArgumentCount];

    public ActionFrame()
    {
        for (int i = 0; i < Args.Length; i++)
            Args[i] = string.Empty;
    }
}

public static class MushBoolean
{
    public static bool IsTrue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return false;
        if (value.Length >= 2 && value[0] == '#' && value[1] == '-')
            return false;

        string lead = value.TrimStart(' ');
        if (lead.Length > 0
            && !lead.EndsWith(' ')
            && double.TryParse(lead, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
            return number != 0;

        foreach (char c in value)
        {
            if (c != ' ')
                return true;
        }

        return false;
    }
}
