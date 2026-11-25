---
topics: [TAG()]
---

tag(<name>[, <param1>[, ... , <paramN>]])

This function outputs the named HTML/Pueblo tag with the given paramaters.

Example:
tag(img,src="http://www.pennmush.org/image.jpg",align="left",width="300")

Will output (in HTML):
```text
    <img src="http://www.pennmush.org/image.jpg" align="left" width="300">

```

**See also:** endtag(), tagwrap(), html()
