---
topics: [HTML FUNCTIONS]
---

HTML Functions are used to output HTML tags to HTML capable users. These tags will be stripped by the system for anything non-HTML related. These functions are only available when Pueblo support is enabled (see '@config pueblo').

```text
  html()      tag()      endtag()      tagwrap()      wshtml()

```

Examples:
```text
    > say html(a href="http://www.pennmush.org")PennMUSH[html(/a)]
    > say tag(a,href="http://www.pennmush.org")PennMUSH[endtag(a)]
    > say tagwrap(a,href="http://www.pennmush.org",PennMUSH)
    > say wshtml(<a href="http://www.pennmush.org">PennMUSH</a>)

```

Each of these produces the HTML output:
```text
    <a href="http://www.pennmush.org">PennMUSH</a>

```

Mortals are restricted in the tags they may use. Most standard HTML tags are ok; protocol-specific tags like SEND and XCH_CMD can only be sent by Wizards or those with the Send_OOB @power.
