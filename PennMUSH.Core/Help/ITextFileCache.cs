namespace PennMUSH.Core.Help;

using System.Collections.Generic;

public interface ITextFileCache
{
    IReadOnlyCollection<string> Categories { get; }
    bool HasCategory(string category);
    bool IsAdminOnly(string category);
    IReadOnlyList<string> GetEntries(string category, bool isAdmin = false);
    string? GetEntry(string category, string topic, bool isAdmin = false, bool allowPrefix = false);
    IReadOnlyList<string> SearchEntries(string category, string pattern, bool isAdmin = false);
    void Reload();
}
