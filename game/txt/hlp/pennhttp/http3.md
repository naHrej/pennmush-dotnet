---
topics: [HTTP3]
---

HTTP connections to PennMUSH are limited to BUFFER_LEN in header and body size.

Incoming headers will be set in Q-registers: %q<headers> contains a list of all headers by name. Individual headers will be set in %q<hdr.[name]>, prefixed with hdr.  e.g: %q<hdr.host> to obtain the value to the Host: header. Or %q<hdr.Cookie> for Cookies.

Multpile header lines will be added to the same q-register name, but %r-delimited.  So two "Cookie:" lines becomes %q<Cookies> with two %r-delimited lines.

HTTP Responses are limited to BUFFER_LEN in response size. Anything sent to the HTTPHandler player, whether it uses think or is @pemitted, is added to the response buffer.

**See also:** @respond, formdecode(), json_query(), urlencode(), urldecode()
