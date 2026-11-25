---
topics: [HTTP2]
---

To use PennMUSH HTTP Handler:

> @pcreate HTTPHandler
> @config/set http_handler=[num(*HTTPHandler)]
> &GET *HTTPHandler=say Somebody tried to HTTP GET %0!

You will very likely want to set the http_handler option in your mush.cnf file to ensure it survives over reboots and is actively receiving events even during startup.

By default, PennMUSH will respond with a 404 NOT FOUND. You will need to use @respond to control what is sent to the client.

For some examples of using HTTP, see 'help http examples'.
For limiting HTTP using @sitelock, see 'help http sitelock'
For HTTP events sent to Event Handler for monitoring, see 'help event http'

Continued in: "help http3"
