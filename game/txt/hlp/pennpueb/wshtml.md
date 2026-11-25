---
topics: [WSHTML(), WSJSON()]
---

wshtml(<html string>[, <default string>])
wsjson(<json string>[, <default string>])

These functions are used to embed HTML and JSON markup into the output for WebSocket-enabled clients. You must have the Pueblo_Send power to use them.

wshtml() embeds <html string> as HTML markup, to be rendered as HTML by a WebSocket client.

wsjson() embeds <json string> as a JSON object which can be captured by a WebSocket client. See 'help json()' for information about formatting JSON object strings.

In both cases, the <default string> is shown as plain text if the recipient is not WebSocket-enabled.

For example, if one uses:

```text
    @emit [wshtml(<a href="http://pennmush.org">PennMUSH</a>,Go to http://pennmush.org)]

```

then any players in the room with a WebSocket connection would see (rendered as HTML)

```text
    <a href="http://pennmush.org">PennMUSH</a>

```

while non-WebSocket connections and listening objects would see

```text
    Go to http://pennmush.org

```

**See also:** WebSockets, Pueblo, HTML Functions, JSON Functions
