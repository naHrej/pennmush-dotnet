namespace PennMUSH.Server.Session;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PennMUSH.Core.Presentation;
using PennMUSH.Core.Protocol;
using PennMUSH.Core.Types;

public class MUSHConnection
{
    private readonly TcpClient? _tcpClient;
    private readonly WebSocket? _webSocket;
    private readonly Func<string, Task>? _onSendLine;
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public string Id { get; } = Guid.NewGuid().ToString("N");
    public ClientCapabilities Capabilities { get; } = new();
    public TelnetNegotiator Negotiator { get; }
    public Dbref PlayerId { get; set; } = Dbref.Nothing;
    public ClientProtocolMode ProtocolMode { get; set; } = ClientProtocolMode.StandardTelnet;
    public string? SessionToken { get; set; }
    public HashSet<string> ClientCapabilities { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool IsConnected => _onSendLine != null || (_tcpClient != null && _tcpClient.Connected) || (_webSocket != null && _webSocket.State == WebSocketState.Open);
    private static int NextDescriptor;
    public int Descriptor { get; } = System.Threading.Interlocked.Increment(ref NextDescriptor);
    public string Host { get; set; } = "";
    public long ConnLogId { get; set; }
    public string? PublicBaseUrl { get; set; }
    public bool IsWebSocket
    {
        get => _webSocket != null || Capabilities.IsWebSocket || ProtocolMode == ClientProtocolMode.MujsWebSocket;
        set => Capabilities.IsWebSocket = value;
    }
    public bool IsSsl
    {
        get => Capabilities.IsSsl;
        set => Capabilities.IsSsl = value;
    }
    public bool Hidden { get; set; }
    public DateTimeOffset ConnectedAt { get; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastActivity { get; private set; }
    public int Commands { get; private set; }
    public long BytesSent { get; private set; }
    public long BytesReceived { get; private set; }
    public string OutputPrefix { get; set; } = "";
    public string OutputSuffix { get; set; } = "";
    public bool TelnetEnabled { get; set; } = true;
    public bool StripAccents
    {
        get => Capabilities.StripAccents;
        set => Capabilities.StripAccents = value;
    }
    public bool PromptNewlines
    {
        get => Capabilities.PromptNewlines;
        set => Capabilities.PromptNewlines = value;
    }
    public bool NoQuota { get; set; }
    public string? ColorStyle { get; set; }

    public MUSHConnection(Func<string, Task>? onSendLine)
    {
        LastActivity = ConnectedAt;
        _onSendLine = onSendLine;
        Negotiator = new TelnetNegotiator(Capabilities);
    }

    public MUSHConnection(TcpClient tcpClient)
    {
        LastActivity = ConnectedAt;
        _tcpClient = tcpClient;
        Negotiator = new TelnetNegotiator(Capabilities);
    }

    public MUSHConnection(WebSocket webSocket)
    {
        LastActivity = ConnectedAt;
        _webSocket = webSocket;
        Capabilities.IsWebSocket = true;
        Capabilities.SupportsHtml = true;
        Capabilities.SupportsPueblo = true;
        Capabilities.ColorMode = ColorDepth.TrueColor;
        Negotiator = new TelnetNegotiator(Capabilities);
        TelnetEnabled = false;
    }

    public async Task SendEnvelopeAsync<T>(string type, T payload, string? id = null, CancellationToken ct = default)
    {
        string json = ProtocolSerializer.Serialize(type, payload, id);
        if (_onSendLine != null)
        {
            BytesSent += Encoding.UTF8.GetByteCount(json);
            await _onSendLine(json);
            return;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(json);
        await SendRawAsync(bytes, ct);
    }

    public Task SendLineAsync(string text, CancellationToken ct = default, bool escapeHtml = false)
        => SendAsync(ComposeLine(text, escapeHtml), ct);

    public Task SendRawAsync(string text, CancellationToken ct = default)
        => SendAsync(ComposeRaw(text), ct);

    public Task SendPromptAsync(string text, CancellationToken ct = default)
        => SendAsync(PromptNewlines ? ComposeLine(text, escapeHtml: false) : ComposePrompt(text), ct);

    /// <summary>
    /// A finished line. Accent stripping and HTML policy run here, once.
    /// OUTPUTPREFIX and OUTPUTSUFFIX are their own records, sent around a command.
    /// </summary>
    private OutboundRecord ComposeLine(string text, bool escapeHtml)
    {
        if (IsProtocolText(text) && !WebSocketMarkup.ContainsFrames(text))
            return OutboundRecord.Passthrough(text + "\r\n");

        var rendered = Render(text, Capabilities.SupportsHtml && escapeHtml);
        if (rendered.HadFrames && rendered.Text.Length == 0)
            return new OutboundRecord("", null, false, rendered.JsonPayloads);

        if (!Capabilities.SupportsHtml)
            return new OutboundRecord(rendered.Text + "\r\n", null, rendered.HasHtmlFrames, rendered.JsonPayloads);

        bool hadAnsi = AnsiToHtmlTranslator.ContainsAnsi(rendered.Text);
        string body = hadAnsi
            ? AnsiToHtmlTranslator.Translate(rendered.Text, escapeHtml: false, EffectiveHtmlColorDepth())
            : rendered.Text;
        string html = "<div>" + ApplyHtmlBreaks(body) + "</div>";
        if (ProtocolMode != ClientProtocolMode.MujsWebSocket)
        {
            // A div is a block, so the next line starts on its own. Keep a wire
            // newline for clients that still split records on \r\n, including a
            // websocket that has not finished MUjs negotiation.
            html += "\r\n";
        }

        return new OutboundRecord(
            html,
            hadAnsi ? AnsiToHtmlTranslator.StripAnsi(rendered.Text) : null,
            true,
            rendered.JsonPayloads);
    }

    private OutboundRecord ComposeRaw(string text)
    {
        if (IsProtocolText(text) && !WebSocketMarkup.ContainsFrames(text))
            return OutboundRecord.Passthrough(text);

        var rendered = Render(text, escapeLiteralHtml: false);
        if (rendered.HadFrames && rendered.Text.Length == 0)
            return new OutboundRecord("", null, false, rendered.JsonPayloads);

        string wire = rendered.Text;
        string? plain = null;
        if (Capabilities.SupportsHtml)
        {
            bool hadAnsi = AnsiToHtmlTranslator.ContainsAnsi(wire);
            if (hadAnsi)
            {
                plain = AnsiToHtmlTranslator.StripAnsi(wire);
                wire = AnsiToHtmlTranslator.Translate(wire, escapeHtml: false, EffectiveHtmlColorDepth());
            }
            wire = ApplyHtmlBreaks(wire);
            if (ProtocolMode != ClientProtocolMode.MujsWebSocket)
                wire += "\r\n";
        }

        return new OutboundRecord(wire, plain, Capabilities.SupportsHtml || rendered.HasHtmlFrames, rendered.JsonPayloads);
    }

    private OutboundRecord ComposePrompt(string text)
    {
        if (IsProtocolText(text) && !WebSocketMarkup.ContainsFrames(text))
            return OutboundRecord.Passthrough(text);

        var rendered = Render(text, escapeLiteralHtml: false);
        if (rendered.HadFrames && rendered.Text.Length == 0)
            return new OutboundRecord("", null, false, rendered.JsonPayloads);

        string wire = rendered.Text;
        string? plain = null;
        if (Capabilities.SupportsHtml)
        {
            bool hadAnsi = AnsiToHtmlTranslator.ContainsAnsi(wire);
            if (hadAnsi)
            {
                plain = AnsiToHtmlTranslator.StripAnsi(wire);
                wire = AnsiToHtmlTranslator.Translate(wire, escapeHtml: false, EffectiveHtmlColorDepth());
            }
            wire = ApplyHtmlBreaks(wire);
        }

        return new OutboundRecord(wire, plain, Capabilities.SupportsHtml || rendered.HasHtmlFrames, rendered.JsonPayloads);
    }

    private RenderedWebSocketOutput Render(string text, bool escapeLiteralHtml)
        => WebSocketMarkup.Render(text, IsWebSocket, escapeLiteralHtml, StripAccents);

    public async Task SendGmcpAsync(string package, string payload, CancellationToken ct = default)
    {
        if (!Capabilities.SupportsGmcp || !TelnetEnabled || IsWebSocket || string.IsNullOrWhiteSpace(package))
            return;

        string body = string.IsNullOrEmpty(payload) ? package.Trim() : $"{package.Trim()} {payload}";
        byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
        var packet = new List<byte>(bodyBytes.Length + 6)
        {
            TelnetConstants.IAC,
            TelnetConstants.SB,
            TelnetConstants.TELOPT_GMCP
        };
        foreach (byte b in bodyBytes)
        {
            packet.Add(b);
            if (b == TelnetConstants.IAC)
                packet.Add(TelnetConstants.IAC);
        }
        packet.Add(TelnetConstants.IAC);
        packet.Add(TelnetConstants.SE);
        await SendRawAsync(packet.ToArray(), ct);
    }

    public Task SendMarkupAsync(string markup, CancellationToken ct = default)
        => SendAsync(ComposeMarkup(markup), ct);

    private OutboundRecord ComposeMarkup(string markup)
    {
        if (!Capabilities.SupportsHtml || IsProtocolText(markup))
            return ComposeRaw(markup);

        // One record. A line-oriented client would split a stylesheet or an
        // HTML screen on raw newlines, so newlines become spaces instead of <br/>.
        string compact = StripAccents ? AccentStripper.Strip(markup) : markup;
        compact = compact.Replace("\r\n", "\n").Replace('\r', '\n').Replace('\n', ' ');
        string? plain = null;
        if (AnsiToHtmlTranslator.ContainsAnsi(compact))
        {
            plain = AnsiToHtmlTranslator.StripAnsi(compact);
            compact = AnsiToHtmlTranslator.Translate(compact, escapeHtml: false, EffectiveHtmlColorDepth());
        }
        if (ProtocolMode != ClientProtocolMode.MujsWebSocket)
            compact += "\r\n";

        return new OutboundRecord(compact, plain, true, Array.Empty<string>());
    }

    private async Task SendAsync(OutboundRecord record, CancellationToken ct)
    {
        if (record.JsonPayloads.Count > 0)
            await SendWebSocketJsonPayloadsAsync(record.JsonPayloads, ct);
        if (record.Wire.Length == 0)
            return;
        await DeliverTextAsync(record.Wire, ct, record.PreserveHtml, record.Plain);
    }

    private ColorDepth EffectiveHtmlColorDepth()
    {
        if (!string.IsNullOrEmpty(ColorStyle) && !ColorStyle.Equals("auto", StringComparison.OrdinalIgnoreCase))
            return Capabilities.ColorMode;
        return Capabilities.ColorMode == ColorDepth.None ? ColorDepth.None : ColorDepth.TrueColor;
    }

    private async Task SendWebSocketJsonPayloadsAsync(IReadOnlyList<string> payloads, CancellationToken ct)
    {
        foreach (string jsonText in payloads)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonText);
                await SendEnvelopeAsync("ws.json", doc.RootElement.Clone(), null, ct);
            }
            catch
            {
                // Ignore malformed JSON frames
            }
        }
    }

    private async Task DeliverTextAsync(string text, CancellationToken ct, bool preserveHtml = false, string? plainText = null)
    {
        if (ProtocolMode == ClientProtocolMode.MujsWebSocket)
        {
            var output = new TerminalOutputPayload(
                Stream: "main",
                Html: text,
                Plain: (plainText ?? text).Replace("\r", "").Replace("\n", "")
            );
            await SendEnvelopeAsync("terminal.output", output, null, ct);
            return;
        }

        string formatted = preserveHtml ? text : OutputFormatter.FormatOutput(text, Capabilities);
        if (_onSendLine != null)
        {
            BytesSent += Encoding.UTF8.GetByteCount(formatted);
            await _onSendLine(formatted);
            return;
        }
        byte[] bytes = Encoding.UTF8.GetBytes(formatted);
        await SendRawAsync(bytes, ct);
    }

    private static string ApplyHtmlBreaks(string text)
    {
        string broken = text.Replace("\r", "").Replace("\n", "<br/>");
        return PreserveSpaces(broken);
    }

    private static string PreserveSpaces(string text)
    {
        var sb = new StringBuilder(text.Length);
        bool inTag = false;
        int spaces = 0;
        void FlushSpaces()
        {
            if (spaces >= 2)
            {
                for (int i = 0; i < spaces; i++)
                    sb.Append("&nbsp;");
            }
            else if (spaces == 1)
            {
                sb.Append(' ');
            }
            spaces = 0;
        }

        foreach (char ch in text)
        {
            if (ch == '<')
            {
                FlushSpaces();
                inTag = true;
                sb.Append(ch);
            }
            else if (ch == '>')
            {
                inTag = false;
                sb.Append(ch);
            }
            else if (!inTag && ch == ' ')
            {
                spaces++;
            }
            else if (!inTag && ch == '\t')
            {
                FlushSpaces();
                for (int i = 0; i < 8; i++)
                    sb.Append("&nbsp;");
            }
            else
            {
                FlushSpaces();
                sb.Append(ch);
            }
        }
        FlushSpaces();
        return sb.ToString();
    }

    private static bool IsProtocolText(string text)
    {
        string trimmed = text.Trim();
        return trimmed.StartsWith('{')
            && trimmed.EndsWith('}')
            && trimmed.Contains("\"type\"", StringComparison.Ordinal);
    }

    public async Task SendRawAsync(byte[] bytes, CancellationToken ct = default)
    {
        await _sendLock.WaitAsync(ct);
        try
        {
            BytesSent += bytes.Length;
            if (_tcpClient != null && _tcpClient.Connected)
            {
                var stream = _tcpClient.GetStream();
                await stream.WriteAsync(bytes, ct);
                await stream.FlushAsync(ct);
            }
            else if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                await _webSocket.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
            }
        }
        catch
        {
            // Socket disconnected
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public void NoteInput(int bytes)
    {
        LastActivity = DateTimeOffset.UtcNow;
        if (bytes > 0)
            BytesReceived += bytes;
        Commands++;
    }

    public void Close()
    {
        try { _tcpClient?.Close(); } catch { }
        try { _webSocket?.Dispose(); } catch { }
    }
}
