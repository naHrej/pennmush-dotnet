---
topics: ["@RESPOND2"]
---

@respond examples:

To modify the response code:

> @respond 200 OK
> @respond 404 Not Found

To change the Content Type:

> @respond/type application/json
> @respond/type text/html

Note that @respond/type is not syntactic sugar for "@respond/header Content-Type".  An HTTP @respond typically should only have one content-type, and @respond/type overrides it. Using @respond/header to add Content-Type will create a second header named Content-Type.

Add Headers:
> @respond/header X-Powered-By=MUSHCode
> @respond/header {Set-Cookie: name=Bob; Max-Age=3600; Version=1}

Adding a Content-Length header is not allowed - PennMUSH calculates it from the output before sending.
