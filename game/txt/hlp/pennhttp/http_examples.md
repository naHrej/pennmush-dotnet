---
topics: [HTTP EXAMPLES]
---

There are a number of HTTP Examples.

Examples all assume the following:

> @pcreate HTTPHandler=digest(md5,rand())
> @config/set http_handler=pmatch(HTTPHandler)

For the basic examples: "help http simple"

For using urls and url parameters: "help http get"

For handling POSTs and POST data: "help http post"
