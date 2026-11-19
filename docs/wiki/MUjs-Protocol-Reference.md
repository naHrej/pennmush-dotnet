# MUjs Protocol Reference

PennMUSH .NET implements a structured JSON-envelope protocol (modeled for the **MUjs** web client and extensible over Telnet GMCP or ASP.NET Core WebSockets) in `PennMUSH.Core/Protocol/` and `PennMUSH.Server/`.

---

## Transports & Endpoints

| Transport / Route | Mode | Behavior |
|---|---|---|
| `ws://<host>:5119/ws`, `/game` | WebSocket (`MujsWebSocket` after handshake, HTML pre-handshake) | Accepts text frames; supports both plain-text commands and `ProtocolEnvelope<T>` JSON messages |
| `ws://<host>:5119/api` | WebSocket / HTTP GET | Legacy dual-socket keepalive (`/api` HTTP returns `200 OK` `"MUjs API Endpoint Active."`) |
| `GET /html/stylesheet.css` | HTTP (`text/css; charset=utf-8`) | Serves game screen stylesheet with `Access-Control-Allow-Origin: *` (`ScreenStylesheet.Load`) |
| Telnet `:4201` with `GMCP` | `MujsTelnetGMCP` / `SupportsGmcp` | Negotiates `IAC WILL GMCP`; out-of-band payloads sent via `IAC SB 201 <type> <payload> IAC SE` |

Pre-handshake WebSocket frames default to `SupportsHtml = true`, `SupportsPueblo = true`, `ColorMode = ColorDepth.TrueColor`, `TelnetEnabled = false`.

---

## Envelope Wire Format (`ProtocolEnvelope<T>`)

Every JSON protocol frame uses `ProtocolSerializer` (`camelCase`, case-insensitive deserialization):

```json
{
  "type": "server.hello",
  "id": "optional-correlation-id",
  "timestamp": 1763549200000,
  "payload": { }
}
```

- `id` is omitted (`WhenWritingNull`) when `null`.
- Unmatched outbound lines (not JSON envelopes and not WebSocket markup frames) are wrapped in `terminal.output` once the connection enters `ClientProtocolMode.MujsWebSocket`.

---

## Client → Server Messages

Handled in `MUSHServer.HandleProtocolEnvelopeAsync()` (`PennMUSH.Server/Engine/MUSHServer.cs`):

### `client.hello`
Upgrades the connection to `ClientProtocolMode.MujsWebSocket` and responds with `server.hello` (plus `style.load` when `ScreenStylesheet` exists).

```json
{
  "type": "client.hello",
  "id": "1",
  "payload": {
    "client": "mujs",
    "version": "1.0.0",
    "capabilities": ["gmcp", "rgb24", "xterm256"]
  }
}
```

Capabilities recognized: `"gmcp"` (`SupportsGmcp`), `"rgb24"` / `"truecolor"` (`ColorDepth.TrueColor`), `"xterm256"` (`ColorDepth.Color256`).

### `terminal.input`
Dispatches a player command line into `HandleInputAsync`:

```json
{
  "type": "terminal.input",
  "payload": {
    "command": "look"
  }
}
```

### `editor.save`
Saves edited attribute code via `EditorSessionManager.SaveSession()`, persists `Storage.Save(Database, Cron)` on success, and replies with `editor.result`:

```json
{
  "type": "editor.save",
  "id": "save-1",
  "payload": {
    "sessionId": "edit_a1b2c3d4e5f6",
    "code": "@pemit %#=Hello"
  }
}
```

### `editor.close`
Closes and removes an active editor session (`EditorSessionManager.CloseSession`):

```json
{
  "type": "editor.close",
  "payload": {
    "sessionId": "edit_a1b2c3d4e5f6",
    "code": ""
  }
}
```

### Plugin Custom Envelopes
Any unrecognized `type` whose JSON object has a `payload` property is routed to `ExtensionManager.TryDispatchProtocolEnvelopeAsync(playerId, type, payload)` matching registered `IProtocolHandler` prefixes (see [Plugin Development Guide](Plugin-Development-Guide)).

---

## Server → Client Messages

### `server.hello`
Sent in response to `client.hello`, issuing an HMAC-SHA256 session token and feature advertisement (`ServerFeatures`):

```json
{
  "type": "server.hello",
  "id": "1",
  "payload": {
    "server": "pennmush-dotnet",
    "version": "1.0.0",
    "sessionToken": "<base64-hmac>",
    "features": {
      "maxWindowCount": 8,
      "supportedLanguages": ["mushcode", "csharp", "moocode"],
      "assetBaseUrl": "http://localhost:5000"
    }
  }
}
```

### `terminal.output`
Sent for game output lines once in `MujsWebSocket` mode:

```json
{
  "type": "terminal.output",
  "payload": {
    "stream": "main",
    "html": "<div>Hello!</div>",
    "ansi": null,
    "plain": "<div>Hello!</div>"
  }
}
```
*(When the output contains ANSI escapes, `plain` holds the stripped plain text while `html` holds the `AnsiToHtmlTranslator` result.)*

### `editor.open` & `editor.result`
Opened by `@client/edit <obj>/<attr>` or `cedit(<player>, <obj>, <attr>)` (requires controlling `<obj>` or being Wizard):

```json
{
  "type": "editor.open",
  "payload": {
    "sessionId": "edit_a1b2c3d4e5f6",
    "target": {
      "dbref": "#12",
      "name": "Lamp",
      "member": "DESC",
      "memberType": "attribute"
    },
    "language": "mushcode",
    "title": "Editing Lamp/DESC",
    "headers": ["Target: Lamp (#12)", "Attribute: DESC", "Owner: #1", "Type: Thing", "Modified: ..."],
    "code": "A brass lamp.",
    "readOnly": false
  }
}
```

`EditorSessionManager.SaveSession()` validates balanced `{}` / `[]` / `()` brackets/braces and replies with `editor.result`:

```json
{
  "type": "editor.result",
  "id": "save-1",
  "payload": {
    "sessionId": "edit_a1b2c3d4e5f6",
    "success": true,
    "diagnostics": [
      { "line": 1, "col": 14, "message": "Unclosed '1' bracket(s) '['", "severity": "warning" }
    ]
  }
}
```

### `window.update`, `style.load`, `audio.play`, `ws.json`

| Type | Triggered By | Payload Fields |
|---|---|---|
| `window.update` | `@client/window <player>=<winId>:<target>:<action>:<html>[:<cssUrl>]` or `cwindow()` | `windowId`, `target`, `title`, `action`, `html`, `cssUrl`, `dimensions` |
| `style.load` | `client.hello` handshake, `@client/style <player>=<url>`, or `cstyle()` | `id`, `url`, `scope` (default `"global"`) |
| `audio.play` | `@client/audio` / `@client/sound` or `caudio(<player>, <url>[, <vol>, <loop>])` | `url`, `volume`, `loop` |
| `ws.json` | `wsjson(<json>[, <fallback>])` embedded in text streams | Arbitrary parsed JSON `RootElement` sent out-of-band on WebSockets |
| Custom OOB (`client_emit()`) | `client_emit(<player>, <type>, <json>)` | Dispatched as JSON envelope on WebSockets, GMCP on `SupportsGmcp` Telnet sockets, or plain fallback text |
