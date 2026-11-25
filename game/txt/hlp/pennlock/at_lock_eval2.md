---
topics: ["@lock-eval2", "@lock-evaluation2"]
---

Example:
```text
    @lock Thursday Cafe = whichday/Thu
    &whichday Thursday Cafe = first(time())
      This locks the object "Thursday Cafe" (probably an exit) unless today is Thursday.

      Whenever someone tries to pass through the exit, the attribute "whichday" will be evaluated, extracting the first word returned from time() (the day of the week). The result is compared with the value in the lock ("Thu"), and the lock will only be passable when the strings match--Only on Thursdays.

```

If you have an evaluation lock that just does [hasflag(%#,FLAGNAME)], you should probably use a bit lock instead.

**See also:** @lock-bit
