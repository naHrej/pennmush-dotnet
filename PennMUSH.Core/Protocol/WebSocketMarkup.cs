namespace PennMUSH.Core.Protocol;

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using PennMUSH.Core.Presentation;

public sealed record RenderedWebSocketOutput(
    string Text,
    IReadOnlyList<string> JsonPayloads,
    bool HadFrames,
    bool HasHtmlFrames);

public static class WebSocketMarkup
{
    public const char FrameStart = '\x02';
    public const char FieldSep = '\x1F';
    public const char FrameEnd = '\x03';
    private const char HtmlKind = 'H';
    private const char JsonKind = 'J';

    public static bool ContainsFrames(string? text)
        => !string.IsNullOrEmpty(text) && text.IndexOf(FrameStart) >= 0;

    public static string EncodeHtml(string html, string fallback = "")
        => $"{FrameStart}{HtmlKind}{Sanitize(html)}{FieldSep}{Sanitize(fallback)}{FrameEnd}";

    public static string EncodeJson(string json, string fallback = "")
        => $"{FrameStart}{JsonKind}{Sanitize(json)}{FieldSep}{Sanitize(fallback)}{FrameEnd}";

    private static string Sanitize(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        if (text.IndexOfAny(new[] { FrameStart, FieldSep, FrameEnd }) < 0)
            return text;
        var sb = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            if (c != FrameStart && c != FieldSep && c != FrameEnd)
                sb.Append(c);
        }
        return sb.ToString();
    }

    public static RenderedWebSocketOutput Render(
        string text,
        bool isWebSocket,
        bool escapeLiteralHtml = false,
        bool stripAccents = false)
    {
        if (string.IsNullOrEmpty(text))
            return new RenderedWebSocketOutput(string.Empty, Array.Empty<string>(), HadFrames: false, HasHtmlFrames: false);

        if (!ContainsFrames(text))
        {
            string plain = TransformPlain(text, escapeLiteralHtml, stripAccents);
            return new RenderedWebSocketOutput(plain, Array.Empty<string>(), HadFrames: false, HasHtmlFrames: false);
        }

        var sb = new StringBuilder(text.Length);
        var jsonPayloads = new List<string>();
        bool hasHtmlFrames = false;
        int pos = 0;

        while (pos < text.Length)
        {
            int start = text.IndexOf(FrameStart, pos);
            if (start < 0)
            {
                sb.Append(TransformPlain(text.Substring(pos), escapeLiteralHtml, stripAccents));
                break;
            }

            if (start > pos)
                sb.Append(TransformPlain(text.Substring(pos, start - pos), escapeLiteralHtml, stripAccents));

            if (start + 1 >= text.Length)
            {
                sb.Append(TransformPlain(text.Substring(start), escapeLiteralHtml, stripAccents));
                break;
            }

            char kind = text[start + 1];
            int sep = text.IndexOf(FieldSep, start + 2);
            int end = sep >= 0 ? text.IndexOf(FrameEnd, sep + 1) : -1;
            if (sep < 0 || end < 0 || (kind != HtmlKind && kind != JsonKind))
            {
                sb.Append(TransformPlain(text.Substring(start), escapeLiteralHtml, stripAccents));
                break;
            }

            string payload = text.Substring(start + 2, sep - (start + 2));
            string fallback = text.Substring(sep + 1, end - (sep + 1));

            if (kind == HtmlKind)
            {
                if (isWebSocket)
                {
                    sb.Append(stripAccents ? StripAccentsOutsideHtmlTags(payload) : payload);
                    hasHtmlFrames = true;
                }
                else if (fallback.Length > 0)
                {
                    sb.Append(TransformPlain(fallback, escapeLiteralHtml, stripAccents));
                }
            }
            else
            {
                if (isWebSocket)
                {
                    if (payload.Length > 0)
                        jsonPayloads.Add(payload);
                }
                else if (fallback.Length > 0)
                {
                    sb.Append(TransformPlain(fallback, escapeLiteralHtml, stripAccents));
                }
            }

            pos = end + 1;
        }

        return new RenderedWebSocketOutput(sb.ToString(), jsonPayloads, HadFrames: true, HasHtmlFrames: hasHtmlFrames);
    }

    private static string TransformPlain(string value, bool escapeHtml, bool stripAccents)
    {
        string result = stripAccents ? AccentStripper.Strip(value) : value;
        return escapeHtml ? WebUtility.HtmlEncode(result) : result;
    }

    private static string StripAccentsOutsideHtmlTags(string html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        var sb = new StringBuilder(html.Length);
        int i = 0;
        while (i < html.Length)
        {
            int lt = html.IndexOf('<', i);
            if (lt < 0)
            {
                sb.Append(AccentStripper.Strip(html.Substring(i)));
                break;
            }

            if (lt > i)
                sb.Append(AccentStripper.Strip(html.Substring(i, lt - i)));

            int gt = html.IndexOf('>', lt);
            if (gt < 0)
            {
                sb.Append(html.Substring(lt));
                break;
            }

            sb.Append(html, lt, gt - lt + 1);
            i = gt + 1;
        }
        return sb.ToString();
    }
}
