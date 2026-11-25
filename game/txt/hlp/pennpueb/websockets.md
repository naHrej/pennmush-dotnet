---
topics: [WEBSOCKETS]
---

WebSockets are a network protocol used by JavaScript-enabled web browsers to make persistent network connections, similar to the telnet connection you use to connect to PennMUSH. With WebSockets enabled in mush.cnf, it is possible to connect from MUSH clients embedded in HTML pages using JavaScript. A WebSocket client can natively render HTML, but can also parse Pueblo links into HTML links that send a command to the MUSH when clicked. For safety, we separate plain text from the other kinds of HTML/Pueblo code that we want rendered. In order to render HTML/Pueblo, a player with the Pueblo_Send power uses special functions to embed HTML/Pueblo markup. Players without the PUEBLO_SEND power can not use these markup functions. Any HTML code strings that are not properly marked up will simply show up as unrendered plain text.

See https://github.com/grapenut/websockclient for an example client.

The different kinds of markup that can be sent to clients are plain text, HTML tags, Pueblo links, JSON objects, and prompts. Without using any HTML markup functions, output is rendered as normal plain text (including ANSI and xterm256 color).

See 'help HTML Functions' for functions used to embed HTML markup tags one at a time.
See 'help wshtml()' for help embedding large segments of raw HTML markup to be sent to WebSocket clients.

Support for Pueblo links depends on the WebSocket client, however the example client above supports xch_cmd for command links and xch_hint for tooltip text popups. For clickable command links, embed a link tag with the command to be executed in the "xch_cmd" attribute, e.g. <a xch_cmd="+who">Who is online?</a>.

You can also send data encapsulated in a JSON object.

See 'help json()' for information about formatting data into JSON object strings.
See 'help wsjson()' for help sending formatted JSON object strings to WebSocket clients as a JavaScript object.

See 'help @prompt' for information about sending telnet GOAHEAD prompts. Support for prompts depends on the WebSocket client. The example client above shows prompts on their own line, separating the input and output windows, but requires PROMPT_NEWLINES to be turned off.

**See also:** HTML Functions, json(), pueblo, wshtml(), wsjson()
