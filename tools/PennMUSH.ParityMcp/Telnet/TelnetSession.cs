namespace PennMUSH.ParityMcp.Telnet;

using System.Net.Sockets;
using System.Text;

/// <summary>
/// Minimal TCP/Telnet client that strips IAC negotiation bytes and frames
/// PennMUSH command output using a unique <c>think</c> sentinel.
/// </summary>
public sealed class TelnetSession : IAsyncDisposable
{
    private readonly TcpClient _client = new();
    private NetworkStream? _stream;
    private readonly StringBuilder _lineBuffer = new();
    private readonly List<byte> _rawBytes = new();
    private bool _inIac;
    private byte _iacCmd;

    public bool IsConnected => _client.Connected;

    public async Task ConnectAndLoginAsync(string host, int port, string loginCommand = "connect One", TimeSpan? timeout = null, CancellationToken ct = default)
    {
        TimeSpan effectiveTimeout = timeout ?? TimeSpan.FromSeconds(15);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(effectiveTimeout);

        await _client.ConnectAsync(host, port, cts.Token);
        _stream = _client.GetStream();

        // Drain initial connect screen + login + MOTD using a sentinel
        await SendRawLineAsync(loginCommand, cts.Token);
        string sentinel = $"__PARITY_LOGIN_{Guid.NewGuid():N}__";
        await SendRawLineAsync($"think {sentinel}", cts.Token);
        await ReadUntilSentinelAsync(sentinel, cts.Token);
    }

    public async Task<string> ExecuteAsync(string command, bool queued = false, TimeSpan? timeout = null, CancellationToken ct = default)
    {
        if (_stream == null || !_client.Connected)
            throw new InvalidOperationException("Telnet session is not connected.");

        TimeSpan effectiveTimeout = timeout ?? TimeSpan.FromSeconds(10);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(effectiveTimeout);

        string sentinel = $"__PARITY_END_{Guid.NewGuid():N}__";
        await SendRawLineAsync(command, cts.Token);
        string sentinelCmd = queued ? $"@wait 0={{think {sentinel}}}" : $"think {sentinel}";
        await SendRawLineAsync(sentinelCmd, cts.Token);

        var lines = await ReadUntilSentinelAsync(sentinel, cts.Token);
        return string.Join("\n", lines);
    }

    private async Task SendRawLineAsync(string line, CancellationToken ct)
    {
        if (_stream == null)
            throw new InvalidOperationException("Telnet stream is not initialized.");

        byte[] payload = Encoding.UTF8.GetBytes(line + "\r\n");
        await _stream.WriteAsync(payload, ct);
        await _stream.FlushAsync(ct);
    }

    private async Task<List<string>> ReadUntilSentinelAsync(string sentinel, CancellationToken ct)
    {
        if (_stream == null)
            throw new InvalidOperationException("Telnet stream is not initialized.");

        var collected = new List<string>();
        byte[] buffer = new byte[4096];

        while (true)
        {
            while (TryDequeueLine(out string? line))
            {
                if (line!.Contains(sentinel, StringComparison.Ordinal))
                    return collected;
                collected.Add(line);
            }

            int read = await _stream.ReadAsync(buffer, ct);
            if (read == 0)
                throw new IOException("Telnet connection closed before sentinel was received.");

            FeedBytes(buffer.AsSpan(0, read));
        }
    }

    private void FeedBytes(ReadOnlySpan<byte> span)
    {
        foreach (byte b in span)
        {
            if (_inIac)
            {
                if (_iacCmd == 0)
                {
                    // IAC IAC -> literal 0xFF
                    if (b == 255)
                    {
                        _rawBytes.Add(b);
                        _inIac = false;
                    }
                    else if (b is 251 or 252 or 253 or 254) // WILL/WONT/DO/DONT take 1 option byte
                    {
                        _iacCmd = b;
                    }
                    else
                    {
                        _inIac = false;
                    }
                }
                else
                {
                    // Subnegotiation (SB=250) ends on IAC SE (240), otherwise 1 option byte completes it
                    _inIac = false;
                    _iacCmd = 0;
                }
                continue;
            }

            if (b == 255)
            {
                _inIac = true;
                _iacCmd = 0;
                continue;
            }

            _rawBytes.Add(b);
        }

        if (_rawBytes.Count > 0)
        {
            _lineBuffer.Append(Encoding.UTF8.GetString(_rawBytes.ToArray()));
            _rawBytes.Clear();
        }
    }

    private bool TryDequeueLine(out string? line)
    {
        for (int i = 0; i < _lineBuffer.Length; i++)
        {
            if (_lineBuffer[i] == '\n')
            {
                int len = i > 0 && _lineBuffer[i - 1] == '\r' ? i - 1 : i;
                line = _lineBuffer.ToString(0, len);
                _lineBuffer.Remove(0, i + 1);
                return true;
            }
        }

        line = null;
        return false;
    }

    public ValueTask DisposeAsync()
    {
        _stream?.Dispose();
        _client.Dispose();
        return ValueTask.CompletedTask;
    }
}
