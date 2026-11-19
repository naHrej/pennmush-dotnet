# Colors, Markup & Presentation

This guide covers color resolution, `@moniker` / `%k`, ANSI-to-HTML and HTML-to-ANSI translation, terminal capability negotiation, `@sockset` transforms, and WebSocket framing.

---

## 927-Color Palette (`colors.json` & `AnsiColorTable`)

`AnsiColorTable` (`PennMUSH.Core/Presentation/AnsiColorTable.cs`) loads the embedded **927-entry** PennMUSH color palette (`game/txt/colors.json`), mapping X11/CSS named colors, `xterm0`–`xterm255`, 24-bit hex RGB values, and ANSI-16 fallbacks.

### `ansi(<spec>, <text>)` Color Spec Syntax
Verified from `AnsiColorTable.TryParseAnsiSpec()`:
- **Compact Formatting Codes**:
  - `n` (reset), `h`/`H` (bold on/off), `u`/`U` (underline on/off), `f`/`F` (flash/blink on/off), `i`/`I` (inverse on/off)
  - `d`/`D` (default foreground / default background SGR 39/49)
  - Single-letter ANSI-16 codes: `x` (black), `r` (red), `g` (green), `y` (yellow), `b` (blue), `m` (magenta), `c` (cyan), `w` (white); uppercase sets background (`X`, `R`, `G`, `Y`, `B`, `M`, `C`, `W`).
- **Named Colors**: `+cornflowerblue`, `dodgerblue`, `+xterm196`, `xterm42`, bare integer `0`..`255` (whitespace collapsed inside color names).
- **Hex & RGB24 Colors**: `#RGB`, `#RRGGBB`, `0xN`/`0xNN`/`0xRGB`/`0xRRGGBB`, `<#RRGGBB>`, `<R G B>`.
- **Background Separator**: Prefix or separate background with `/` or `!` (e.g., `y/b`, `+#ffd700/+#001122`, `/red`).

---

## `@moniker` & `%k` / `%K`

- `@moniker <obj>=<colored text>` evaluates `<colored text>` and passes it through `AnsiColorTable.SanitizeMoniker()`:
  - Strips WebSocket/Pueblo markup frames (`\x02...\x03`) and non-SGR escape sequences.
  - Strips underline (`SGR 4`, `24`) and flash/blink (`SGR 5`, `6`, `25`), matching PennMUSH `sanitize_moniker()`.
  - Rejects monikers with no remaining ANSI styling (`You need to specify a moniker with some ANSI.`).
- `@moniker <obj>=` (empty RHS) clears `MONIKER`.
- `MONIKER` carries `AttributeFlags.NoProg | AttributeFlags.Visual | AttributeFlags.Wizard | AttributeFlags.Locked`.
- `%k` / `%K` and `moniker()` read `<obj>`'s `MONIKER` (falling back to plain `Name` if unset); `%K` capitalizes the first non-ANSI character.

---

## ANSI-Aware Alignment & Wrapping

Functions in `StringFunctions.cs` and `TextFormatFunctions.cs` measure visible character columns rather than raw escape-byte lengths (`AnsiString` / ANSI-aware `strlen()`, `align()`, `wrap()`):
- `align()` supports `(ansi)` color specs, truncation vs wrapping, and preserves active SGR sequences cleanly across wrapped lines and resets.

---

## Translation & Color Downsampling Pipeline

`OutputFormatter` and `MUSHConnection` adapt output per connection based on `ClientCapabilities` (`PennMUSH.Core/Presentation/ClientCapabilities.cs`):

| `ColorDepth` | Level | Rendered Behavior |
|---|---|---|
| `None` | Plaintext | `AnsiToHtmlTranslator.StripAnsi` / HTML tags stripped |
| `Standard16` | ANSI 16-color | SGR 30–37 / 40–47 / 90–97 (nearest match via `RgbToNearestAnsi16`) |
| `Color256` | xterm-256 | SGR `38;5;N` / `48;5;N` (nearest xterm via `RgbToNearestXterm`) |
| `TrueColor` | 24-bit RGB | SGR `38;2;R;G;B` / `48;2;R;G;B` (or inline CSS `#rrggbb` in HTML mode) |

- **`AnsiToHtmlTranslator`**: Converts SGR bold (`1`), dim (`2`), italic (`3`), underline (`4`), blink (`5`/`6` → `class="ansi-blink"`), inverse (`7`, swaps fg/bg defaulting `#000000` / `#e5e5e5`), strikethrough (`9`), 16-color, xterm-256, and RGB24 codes into minimal `<span>` elements, HTML-escaping text via `SearchValues.Create("&<>\"")` when enabled.
- **`HtmlToAnsiTranslator`**: Converts Pueblo/HTML tags (`<b>`/`<strong>`, `<i>`/`<em>`, `<u>`, `<font color="...">`, `<span fg="...">`, `<br/>`, `<hr/>`, closing block tags) down to the socket's `ColorDepth` while stripping `<head>`, `<style>`, and `<script>` blocks for Telnet clients.

---

## Telnet Negotiation & `@sockset` Transforms

### Telnet Negotiation (`TelnetNegotiator.cs`)
On TCP connect (port `4201`), `PennMUSH.Server` negotiates `NAWS` (window width/height), `TTYPE` (terminal type auto-detecting `256` → `Color256` and `truecolor`/`24bit` → `TrueColor`), `SGA`, and `GMCP`. Clients sending `PUEBLOCLIENT` activate `SupportsPueblo` and `SupportsHtml`.

### `@sockset` Options (`ConnectionDirectory.cs` & `MUSHConnection.cs`)
Running `@sockset` inspects the active session; `@sockset [<player|descriptor>=]<option> <value> [...]` sets options on the outbound path:

| Option | Behavior | Restrictions |
|---|---|---|
| `OUTPUTPREFIX <str>` | Sent before each command output block (empty clears) | Own session or Wizard |
| `OUTPUTSUFFIX <str>` | Sent after each command output block (empty clears) | Own session or Wizard |
| `PUEBLO <on/off>` | Toggles `SupportsPueblo` and `SupportsHtml` | Own session or Wizard |
| `TELNET <yes/no>` | Toggles Telnet IAC / GMCP processing (`TelnetEnabled`) | Own session or Wizard |
| `WIDTH <n>` / `HEIGHT <n>` | Sets positive integer terminal dimensions | Own session or Wizard |
| `TERMINALTYPE <str>` | Overrides `TerminalType` | Own session or Wizard |
| `COLORSTYLE <style>` | `auto`, `plain`/`none`, `hilite`/`highlight`, `16color`, `xterm256`/`256`, `rgb24`/`truecolor` | Own session or Wizard |
| `STRIPACCENTS <yes/no>` | Alias `NOACCENTS`; runs `AccentStripper.Strip` (`ß`→`ss`, `æ`→`ae`, Unicode NFD diacritic removal) on outbound text (preserving HTML tags in `wshtml()`) | Own session or Wizard |
| `PROMPT_NEWLINES <yes/no>` | Forces `@prompt` / `prompt()` to emit a trailing newline (`SendLineAsync`) | Own session or Wizard |
| `NOQUOTA <yes/no>` | Exempts that connection from command quota (`NoQuota`) | Subject must be Wizard |

---

## WebSocket Channel Framing: `wshtml()` & `wsjson()`

`WebSocketMarkup` (`PennMUSH.Core/Protocol/WebSocketMarkup.cs`) enables dual-mode output inside ordinary messages (`say`, `@pemit`, `@emit`, etc.):

- **`wshtml(<html[, <fallback>])`**: Wraps HTML in `\x02H<html>\x1F<fallback>\x03`.
  - Delivered to WebSocket clients (`IsWebSocket == true`) as unescaped HTML (with `StripAccents` applied only outside `<...>` tags).
  - Delivered to Telnet / non-WebSocket clients as `<fallback>` (or omitted if `<fallback>` is empty).
- **`wsjson(<json[, <fallback>])`**: Wraps JSON in `\x02J<json>\x1F<fallback>\x03`.
  - Delivered to WebSocket clients as out-of-band `ws.json` `ProtocolEnvelope` frames (`SendEnvelopeAsync("ws.json", ...)`).
  - Delivered to non-WebSocket clients as `<fallback>`.
