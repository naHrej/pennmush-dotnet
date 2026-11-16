namespace PennMUSH.Core.Tests.Protocol;

using System.Threading.Tasks;
using PennMUSH.Core.Protocol;
using PennMUSH.Server.Session;
using Xunit;

public class HtmlExamineEncodingTests
{
    [Fact]
    public async Task HtmlClient_EscapesExamineTextAndKeepsLineBreaks()
    {
        string? sent = null;
        var conn = new MUSHConnection(text =>
        {
            sent = text;
            return Task.CompletedTask;
        });
        conn.Capabilities.SupportsHtml = true;
        conn.ProtocolMode = ClientProtocolMode.MujsWebSocket;

        await conn.SendLineAsync("SPEECHMOD: <span>x</span>\nnext", escapeHtml: true);

        var payload = ProtocolSerializer.DeserializePayload<TerminalOutputPayload>(sent!);
        Assert.NotNull(payload);
        Assert.Contains("&lt;span&gt;x&lt;/span&gt;", payload.Html);
        Assert.Contains("<br/>", payload.Html);
        Assert.DoesNotContain("<span>", payload.Html);
    }

    [Fact]
    public async Task TelnetClient_LeavesAngleBracketsInTheText()
    {
        string? sent = null;
        var conn = new MUSHConnection(text =>
        {
            sent = text;
            return Task.CompletedTask;
        });

        await conn.SendLineAsync("1 < 2", escapeHtml: true);

        Assert.NotNull(sent);
        Assert.Contains("1 < 2", sent);
        Assert.DoesNotContain("&lt;", sent);
    }
}
