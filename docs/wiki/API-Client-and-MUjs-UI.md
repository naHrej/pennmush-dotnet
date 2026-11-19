# API: Client & MUjs UI

Out-of-band WebSocket / GMCP / MUjs UI notifications and rich HTML/JSON inline markup functions extracted from `PennMUSH.Core/MUSHcode/Functions/ClientFunctions.cs` (**5** functions), `TextFormatFunctions.cs` (**2** functions: `oob()`, `wshtml()`), and `JsonFunctions.cs` (**1** function: `wsjson()`).

* See [MUjs Protocol Reference](MUjs-Protocol-Reference) for the JSON envelope schemas (`editor.open`, `window.update`, `style.load`, `audio.play`, custom GMCP/OOB packages) and [`@client`](Commands-Server-Admin-and-Extensions) for the command equivalent.
* See [Colors, Markup & Presentation](Colors-Markup-and-Presentation) and [String & Formatting](API-String-and-Formatting) for ANSI / Pueblo rendering (`ansi()`, `html()`, `tag*()`, `pueblo()`).

---

## Client & MUjs Extension Functions

| Function | Signature | Notes |
|---|---|---|
| `cedit` | `cedit(<playerDbref>, <targetDbref>, <attr>)` | Opens `editor.open` UI session for `<targetDbref>/<attr>`; returns `1` |
| `cwindow` | `cwindow(<playerDbref>, <id>, <target>, <action>, <html>[, <cssUrl>])` | Sends `window.update` panel payload (`WindowUpdatePayload`); returns `1` |
| `cstyle` | `cstyle(<playerDbref>, <url>)` | Sends `style.load` global stylesheet directive (`StyleLoadPayload`); returns `1` |
| `caudio` | `caudio(<playerDbref>, <url>[, <vol>[, <loop?>]])` | Sends `audio.play` directive (`AudioPlayPayload`; default vol `1.0`, loop `0`); returns `1` |
| `client_emit` | `client_emit(<playerDbref>, <type>, <json>)` | Dispatches raw OOB protocol frame `<type>` with `<json>` payload; returns `1` |
| `oob` | `oob(<targets>, <package>[, <json>])` | Stock PennMUSH GMCP/OOB push to 1+ targets; returns delivered count |
| `wshtml` | `wshtml(<html>[, <fallback>])` | Encodes WebSocket inline HTML markup frame (`Pueblo_Send` / `Wizard`) |
| `wsjson` | `wsjson(<json>[, <fallback>])` | Validates `<json>` and encodes WebSocket JSON markup frame (`Pueblo_Send` / `Wizard`) |

---

## Permissions, Validation & Errors

* **`cedit` / `cwindow` / `cstyle` / `caudio` / `client_emit`** (`ClientFunctions.cs`):
  - Require strict `#dbref` arguments (`Dbref.TryParse`), returning `#-1 INVALID PLAYER` (or `#-1 INVALID TARGET` on `cedit`) on failure, and `#-1 TOO FEW ARGUMENTS` when required arguments are missing.
  - `cedit()` delegates session creation to `EditorSessionManager.StartAttributeSession`, returning `#-1 PERMISSION DENIED` if the player cannot edit `<target>/<attr>`.
  - `caudio()` parses optional `<vol>` (default `1.0`) and `<loop?>` (`"1"` or `"true"` case-insensitive -> `true`).
  - `client_emit()` is the PennMUSH .NET WebSocket OOB extension (distinct from channel `cemit()`, covered in [Channels & Mail](API-Channels-and-Mail)).
* **`oob(<targets>, <package>[, <json>])`** (`TextFormatFunctions.cs`):
  - Returns `0` if `< 2` arguments or no database is attached.
  - Validates non-empty `<json>` with `JsonDocument.Parse` (`#-1 INVALID JSON` on malformed JSON).
  - Resolves each space-separated target (`#dbref` or `MatchObject`); sender must be `Wizard`, hold `Send_OOB` power, or `Controls(actor, target)`. Returns the count of notified targets, `#-1 PERMISSION DENIED` if at least one target was denied and `0` succeeded, or `#-1 NO VALID PLAYERS` if no valid target received the frame.
* **`wshtml()` & `wsjson()`**:
  - Require `Wizard` or `Pueblo_Send` power (`#-1 PERMISSION DENIED` otherwise).
  - `wsjson()` parses `<json>` with `JsonDocument.Parse` and returns `#-1 INVALID JSON` if malformed. Both embed an optional plain-text `<fallback>` rendered for non-WebSocket/ANSI clients (see `strip()` in [String & Formatting](API-String-and-Formatting)).

```text
think [cedit(#5, #10, DESCRIPTION)]
-> 1

think [wshtml(<b>Alert</b>, *Alert*)]
think [wsjson({"hp":42}, HP: 42)]
think [oob(me, Char.Vitals, {"hp":42})]
-> 1
```

---

See also: [MUjs Protocol Reference](MUjs-Protocol-Reference) · [JSON & Crypto](API-JSON-and-Crypto) · [String & Formatting](API-String-and-Formatting) · [Functions Index](Functions-Index) · [Home](Home)
