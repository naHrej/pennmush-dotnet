---
topics: [HTTP SITELOCK]
---

You can configure what paths and IPs you want to limit access to via @sitelock.

HTTP Requests will check @sitelock for IP restrictions and path restrictions for the config(http_handler) player. Right now, we don't resolve hosts before HTTP connections are handled due to the time delay, but that may be an option in the future.

For path restrictions, @sitelock checks the pattern "<IP>`<METHOD>`<PATH"

Both IP and the "IP`Method`Path" approach check for "connect" option.

Examples:

Ban everybody using IP Address pattern matching 12.34.*.* from using HTTP
> @sitelock 12.34.*.*=!connect,[config(http_handler)]

Permit 12.34.56.78 to access ALL of HTTP, but block everybody else from accessing /admin/ and its subpages
> @sitelock 12.34.56.78=connect,[config(http_handler)]
> @sitelock *`*`/admin/*=!connect,[config(http_handler)]

Allow 12.34.56.78 to POST to /admin/*, allow POSTs to /do/* from anywhere, but prevent all other POSTs.
> @sitelock 12.34.56.78`POST`/admin/*=connect,[config(http_handler)]
> @sitelock *`POST`/do/*=connect,[config(http_handler)]
> @sitelock *`POST`*=!connect,[config(http_handler)]

Like all @sitelock commands - earlier rules take precedence over later rules
