---
topics: [HTTP POST]
---

Suppose you want a web hook for notifications from an extermal system.
HTTP via POST is ideal for that:

> &POST *HTTPHandler=@chat [formdecode(%1,channel)]=[formdecode(%1,msg)]

Check: Use a language or form to POST to http://yourmush:port/ with
values "channel" and "msg"

POST is often a good way to get a JSON blob as well.

> &POST *HTTPHandler=@chat [json_query(%1,extract,$.channel)]=[json_query(%1,extract,$.msg)]

Check: Same thing, but passing JSON as data.

Maybe you want to do either, depending on if the client is using JSON or not?

> &POST`JSON *HTTPHandler=@chat [json_query(%1,extract,$.channel)]=[json_query(%1,extract,$.msg)]
> &POST`FORM *HTTPHandler=@chat [formdecode(%1,channel)]=[formdecode(%1,msg)]
> &POST *HTTPHandler=@break strmatch(%q<hdr.content-type>,*json*)=@include me/post`json ; @include me/post`form

Check: Post with either form data OR json data!
