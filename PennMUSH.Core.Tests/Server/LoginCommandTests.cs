namespace PennMUSH.Core.Tests.Server;

using System.Collections.Generic;
using System.Threading.Tasks;
using PennMUSH.Server.Engine;
using PennMUSH.Server.Session;

public class LoginCommandTests
{
    [Fact]
    public async Task ShortConnect_LogsInThePlayer()
    {
        var server = new MUSHServer();
        var conn = new MUSHConnection(_ => Task.CompletedTask);
        server.RegisterConnection(conn);

        await server.HandleInputAsync(conn, "co One");

        Assert.True(conn.PlayerId.IsValid);
    }

    [Fact]
    public async Task ShortCreate_CreatesThePlayer()
    {
        var server = new MUSHServer();
        var conn = new MUSHConnection(_ => Task.CompletedTask);
        server.RegisterConnection(conn);

        await server.HandleInputAsync(conn, "cr Tester secret");

        Assert.True(conn.PlayerId.IsValid);
        Assert.Equal("Tester", server.Database.GetObject(conn.PlayerId)!.Name);
    }

    [Fact]
    public async Task UnknownShortForm_StaysAtLogin()
    {
        var server = new MUSHServer();
        var messages = new List<string>();
        var conn = new MUSHConnection(message =>
        {
            messages.Add(message);
            return Task.CompletedTask;
        });
        server.RegisterConnection(conn);
        messages.Clear();

        await server.HandleInputAsync(conn, "con One");

        Assert.False(conn.PlayerId.IsValid);
        Assert.Contains(messages, message => message.Contains("Valid login commands"));
    }
}
