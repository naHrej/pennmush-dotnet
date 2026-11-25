namespace PennMUSH.Server.Network;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PennMUSH.Server.Engine;
using PennMUSH.Server.Session;

public class TelnetListenerService : BackgroundService
{
    private readonly MUSHServerHost _host;
    private readonly ILogger<TelnetListenerService> _logger;
    private readonly int _port;
    private readonly string? _ipAddr;
    private TcpListener? _listener;

    public TelnetListenerService(MUSHServerHost host, ILogger<TelnetListenerService> logger, int port = 4201, string? ipAddr = null)
    {
        _host = host;
        _logger = logger;
        _port = port;
        _ipAddr = ipAddr;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IPAddress bindAddr = !string.IsNullOrWhiteSpace(_ipAddr) && IPAddress.TryParse(_ipAddr.Trim(), out var parsed)
            ? parsed
            : IPAddress.Any;
        _listener = new TcpListener(bindAddr, _port);
        _listener.Start();
        _logger.LogInformation("PennMUSH Telnet Server listening on {Address}:{Port}...", bindAddr, _port);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var tcpClient = await _listener.AcceptTcpClientAsync(stoppingToken);
                _ = HandleClientAsync(tcpClient, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting Telnet TCP connection");
            }
        }

        _listener.Stop();
    }

    private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken stoppingToken)
    {
        var conn = new MUSHConnection(tcpClient);
        if (tcpClient.Client.RemoteEndPoint is IPEndPoint remote)
        {
            conn.Host = remote.Address.ToString();
            if (_host.Server.Config.UseDns)
            {
                try
                {
                    var dnsTask = Dns.GetHostEntryAsync(remote.Address);
                    if (await Task.WhenAny(dnsTask, Task.Delay(500, stoppingToken)) == dnsTask)
                        conn.Hostname = (await dnsTask).HostName;
                }
                catch
                {
                    // Keep numeric IP fallback when reverse DNS fails
                }
            }
        }
        _host.RegisterConnection(conn);
        _logger.LogInformation("New Telnet connection from {Endpoint} [Session {Id}]", tcpClient.Client.RemoteEndPoint, conn.Id);

        try
        {
            // 1. Send Telnet handshake
            byte[] handshake = conn.Negotiator.GetInitialHandshake();
            await conn.SendRawAsync(handshake, stoppingToken);

            // 2. Send welcome banner
            await conn.SendLineAsync("================================================================", stoppingToken);
            await conn.SendLineAsync("               Welcome to PennMUSH (.NET 10 Core)               ", stoppingToken);
            await conn.SendLineAsync("================================================================", stoppingToken);
            await conn.SendLineAsync("To connect to a character: connect <name> <password>", stoppingToken);
            await conn.SendLineAsync("To create a character:    create <name> <password>", stoppingToken);
            await conn.SendLineAsync("To see who is online:     WHO", stoppingToken);
            await conn.SendLineAsync("To disconnect:            QUIT", stoppingToken);
            await conn.SendLineAsync("================================================================", stoppingToken);

            var stream = tcpClient.GetStream();
            byte[] buffer = new byte[2048];
            var lineBuilder = new StringBuilder();
            bool prevWasCr = false;

            while (!stoppingToken.IsCancellationRequested && tcpClient.Connected)
            {
                int bytesRead = await stream.ReadAsync(buffer, stoppingToken);
                if (bytesRead == 0) break; // Client disconnected

                string processed = conn.Negotiator.ProcessInput(buffer.AsSpan(0, bytesRead));

                // Send any pending negotiation response bytes
                byte[] pending = conn.Negotiator.ConsumePendingOutput();
                if (pending.Length > 0)
                {
                    await conn.SendRawAsync(pending, stoppingToken);
                }

                // Process line breaks (\r\n, \n, or standalone \r)
                for (int i = 0; i < processed.Length; i++)
                {
                    char c = processed[i];
                    if (c == '\r')
                    {
                        prevWasCr = true;
                        string line = lineBuilder.ToString();
                        lineBuilder.Clear();
                        await _host.HandleInputAsync(conn, line);
                    }
                    else if (c == '\n')
                    {
                        if (prevWasCr)
                        {
                            prevWasCr = false;
                            continue;
                        }
                        string line = lineBuilder.ToString();
                        lineBuilder.Clear();
                        await _host.HandleInputAsync(conn, line);
                    }
                    else
                    {
                        prevWasCr = false;
                        lineBuilder.Append(c);
                    }
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Telnet client disconnected unexpectedly: {Message}", ex.Message);
        }
        finally
        {
            _host.UnregisterConnection(conn);
            conn.Close();
            _logger.LogInformation("Closed Telnet connection [Session {Id}]", conn.Id);
        }
    }
}
