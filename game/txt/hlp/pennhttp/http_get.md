---
topics: [HTTP GET]
---

GET requests are the simplest: There's no form data, and the path can be split into the path (before(%0,?)) and parameters (after(%0,?))

Return a JSON array of users to any GET request

> &LIST_TO_JSON`FOLD *HTTPHandler=json_mod(%0,insert,$\[[json_query(%0,size)]\],json(string,%1))
> &LIST_TO_JSON *HTTPHandler=fold(list_to_json`fold,%0,\[\],%1)
> &NAMES *HTTPHandler=u(list_to_json,map(#apply/name,mwho(),%b,^),^)
> &GET *HTTPHandler=@respond/type application/json ; think u(names)

Check: http://yourmush:port/dbrefs

As above, but if path is "who". If it's "dbrefs", no names.

> &GET`WHO *HTTPHandler=@respond/type application/json ; think u(names)
> &GET`DBREFS *HTTPHandler=@respond/type application/json ; think u(list_to_json,mwho(),%b)
> &GET *HTTPHandler=@break strmatch(%0,/who)=@include me/get`who ; @break strmatch(%0,/dbrefs)=@include me/get`dbrefs

Check: http://yourmush:port/dbrefs
Check: http://yourmush:port/who

Look at something, whose name is passed by ?name=... value

> &GET *HTTPHandler=look [formdecode(after(%0,?),name)]

Check: http://yourmush:port/look?name=here
