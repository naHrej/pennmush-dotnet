---
topics: ["@RESPOND", "@RESPOND/TYPE", "@RESPOND/HEADER"]
---

@respond <code> <text>
@respond/type <content-type>
@respond/header <name>=<value>

Within the context of an HTTP Player connection, @respond is used to modify the headers sent back to the HTTP client.

If an attribute exists, Penn defaults to 200 OK, and Content-Type "text/plain"

@respond <code> <text> changes the 1st line sent to the client (200 OK)
@respond/type <text> replaces the current Content-Type header. (text/plain).
@respond/header <name>=<value> adds a new Header. This can't be undone, as it's appended to a buffer. So you can add multiple headers w/ same name.

@respond commands are _not_ required to be run before any output is sent to the player. For Content-Length purposes, Penn buffers all output before the @include finishes.

If @respond is run outside of an HTTP Context, the enactor will see "(HTTP): ..." for debugging, but it isn't buffered for output as if it was an active http request.

See examples in "help @respond2". HTTP limitations in "help @respond3"
