---
topics: [HTTP SIMPLE]
---

The examples on this page are all simple, single-result handlers.

Return the output of WHO to any GET request:

> &GET *HTTPHandler=WHO

Whenever a POST is performed, say the path and body.

> &POST *HTTPHandler=say POST attempted at %0: %1
