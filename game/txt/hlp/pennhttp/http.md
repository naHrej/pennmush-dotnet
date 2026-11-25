---
topics: [HTTP, http_handler, http_per_second, "@config http_per_second", "@config http_handler"]
---

If http_handler @config is a dbref of a valid player, PennMUSH will support HTTP requests reaching its mush port. It is very low level, and a little tricky to understand.

If an HTTP Handler isn't set, or a given method attribute doesn't exist on the http handler object, Penn will default to responding with mud_url or an error page.

@config http_per_second must also be a postive number to enable HTTP commands, and they will be limited by that amount.

When an HTTP request hits the PennMUSH port, PennMUSH invisibly logs in to the HTTP Handler player (@config http_handler), and executes an @include me/<method>. e.g: @include me/get.

Immediately when the @include finishes, the http request is complete. Any queued entries (such as @wait, $-commands, etc) are not going to be sent to the HTTP client - you'll need to code using @include, /inline switches, and the like.

%0 will be the pathname, e.g: "/", "/path/to", "/foo?bar=baz", etc.

%1 will be the body of the request. If it's json, use json_query to deal with it. If it's form-encoded, look at help formdecode()

Anything sent to the HTTP Handler player during evaluation of this code is included in the body sent to the HTTP Client. There is a maximum size of BUFFER_LEN for the body of the response.

To modify the response headers, use the command "@respond"

Continued in: "help http2"
