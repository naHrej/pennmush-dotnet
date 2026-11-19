namespace PennMUSH.Core.Tests.Commands;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using PennMUSH.Core.Commands;

[Collection("FeedbackTemplates")]
public class FeedbackTests
{
    [Fact]
    public void PermissionDeniedIsOneSentence()
    {
        Assert.Equal("Permission denied.", Feedback.PermissionDenied.Text());
        Assert.Equal("MAIL: Permission denied.", Feedback.MailPermissionDenied.Text());
        Assert.Equal("MAIL: Permission denied!", Feedback.MailPermissionDenied2.Text());
    }

    [Fact]
    public void TokenFillsTheHoleThePlayerTyped()
    {
        Assert.Equal(
            "Channel <Public> not found.",
            Feedback.ChannelNotFound.Text(("channel", "Public")));
        Assert.Equal(
            "I can't find 'lamp'.",
            Feedback.ICantFind.Text("value", "lamp"));
    }

    [Fact]
    public void ServerCanReplaceASentence()
    {
        try
        {
            FeedbackText.SetTemplate(Feedback.PermissionDenied, "You do not have permission.");
            Assert.Equal("You do not have permission.", Feedback.PermissionDenied.Text());
        }
        finally
        {
            FeedbackText.ResetTemplates();
        }

        Assert.Equal("Permission denied.", Feedback.PermissionDenied.Text());
    }

    [Fact]
    public void ReplacedSentenceKeepsItsTokens()
    {
        try
        {
            FeedbackText.SetTemplate(Feedback.ChannelNotFound, "No channel named {{channel}}.");
            Assert.Equal("No channel named Public.", Feedback.ChannelNotFound.Text(("channel", "Public")));
        }
        finally
        {
            FeedbackText.ResetTemplates();
        }
    }

    [Fact]
    public void NotifySendsTheCatalogSentence()
    {
        var context = new CommandContext();
        context.Notify(Feedback.PermissionDenied);
        context.Notify(Feedback.ChannelNotFound, ("channel", "Public"));
        Assert.Equal(
            new[] { "Permission denied.", "Channel <Public> not found." },
            context.Notifications);
    }

    [Fact]
    public void EveryReplyHasTextAndPlainRepliesAreUnique()
    {
        var plain = new Dictionary<string, Feedback>();
        foreach (Feedback message in Enum.GetValues<Feedback>())
        {
            var tokens = new List<(string Name, string Value)>();
            string text = "";
            for (var attempt = 0; attempt < 8; attempt++)
            {
                try
                {
                    text = message.Text(tokens.ToArray());
                    break;
                }
                catch (ArgumentException ex)
                {
                    var hole = Regex.Match(ex.Message, @"\{\{([^{}]+)\}\}");
                    Assert.True(hole.Success, ex.Message);
                    tokens.Add((hole.Groups[1].Value, "X"));
                }
            }

            Assert.DoesNotContain("{{", text);
            if (tokens.Count == 0)
            {
                Assert.False(plain.ContainsKey(text), $"{message} repeats {plain.GetValueOrDefault(text)}");
                plain[text] = message;
            }
        }

        Assert.True(plain.Count > 1);
    }

    [Fact]
    public void MissingTokenThrows()
    {
        var ex = Assert.Throws<ArgumentException>(() => Feedback.ChannelNotFound.Text());
        Assert.Contains("{{channel}}", ex.Message);
    }
}
