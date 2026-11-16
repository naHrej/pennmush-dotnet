namespace PennMUSH.Core.Communication.Channels;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PennMUSH.Core.Types;

public record ChannelBroadcastResult(IReadOnlyList<Dbref> Recipients, string FormattedMessage);

public class ChannelRegistry
{
    private readonly ConcurrentDictionary<string, Channel> _channels = new(StringComparer.OrdinalIgnoreCase);

    public bool CreateChannel(string name, Dbref owner, ChannelFlags flags = ChannelFlags.Public | ChannelFlags.Player, string description = "")
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var channel = new Channel(name.Trim(), owner, flags) { Description = description };
        return _channels.TryAdd(name.Trim(), channel);
    }

    public bool DeleteChannel(string name)
    {
        return _channels.TryRemove(name.Trim(), out _);
    }

    public Channel? GetChannel(string name)
    {
        return _channels.TryGetValue(name.Trim(), out var ch) ? ch : null;
    }

    public IReadOnlyList<Channel> GetAllChannels()
    {
        return _channels.Values.OrderBy(c => c.Name).ToList();
    }

    public IReadOnlyList<Channel> GetChannelsForPlayer(Dbref player)
    {
        return _channels.Values.Where(c => c.HasMember(player)).OrderBy(c => c.Name).ToList();
    }

    public ChannelBroadcastResult? Broadcast(
        string channelName,
        Dbref senderId,
        string senderName,
        string text,
        bool isNoisy = false,
        bool isPose = false,
        bool isEmit = false)
    {
        var channel = GetChannel(channelName);
        if (channel == null) return null;

        var member = channel.GetMember(senderId);
        string titlePrefix = (member != null && !string.IsNullOrEmpty(member.Title)) ? $"<{member.Title}> " : "";

        string formatted;
        if (isEmit)
        {
            formatted = $"[{channel.Name}] {text}";
        }
        else if (isPose)
        {
            formatted = $"[{channel.Name}] {titlePrefix}{senderName} {text}";
        }
        else
        {
            formatted = $"[{channel.Name}] {titlePrefix}{senderName}: {text}";
        }

        channel.AddMessage(senderName, senderId, formatted);

        var recipients = channel.Members
            .Where(m => !m.IsMuted)
            .Select(m => m.PlayerId)
            .ToList();

        return new ChannelBroadcastResult(recipients, formatted);
    }

    public bool HasAlias(Dbref player, string alias) =>
        AliasesFor(player).ContainsKey(alias);

    public void SetAlias(Dbref player, string alias, string channel) =>
        AliasesFor(player)[alias] = channel;

    public bool TryGetAlias(Dbref player, string alias, out string? channel) =>
        AliasesFor(player).TryGetValue(alias, out channel);

    public bool RemoveAlias(Dbref player, string alias, out string? channel) =>
        AliasesFor(player).TryRemove(alias, out channel);

    public int CountAliases(Dbref player, string channel) =>
        AliasesFor(player).Values.Count(value => value.Equals(channel, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<(string Alias, string Channel)> ListAliases(Dbref player) =>
        AliasesFor(player)
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => (pair.Key, pair.Value))
            .ToList();

    public Channel? MatchChannel(string name, out bool ambiguous)
    {
        ambiguous = false;
        if (string.IsNullOrWhiteSpace(name))
            return null;
        var exact = GetChannel(name);
        if (exact != null)
            return exact;
        var matches = GetAllChannels()
            .Where(channel => channel.Name.StartsWith(name.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (matches.Count == 1)
            return matches[0];
        ambiguous = matches.Count > 1;
        return null;
    }

    private readonly ConcurrentDictionary<Dbref, ConcurrentDictionary<string, string>> _aliases = new();

    private ConcurrentDictionary<string, string> AliasesFor(Dbref player) =>
        _aliases.GetOrAdd(player, _ => new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase));
}
