---
topics: ["@if", "@ifelse", "@skip"]
---

@if <boolean>=<true>[, <false>]
@skip <boolean>=<false>

If <boolean> is true, the action list <true> is run, otherwise the action list <false> is run. The action list is not queued, it is run immediately, in the same action list as @if.

For RhostMUSH compatability, @skip runs the action list <false> when <boolean> is false, and does nothing for true values.

@ifelse and @skip/ifelse are aliases for @if.

See 'help @if2' for examples.
**See also:** @break, @switch, if(), BOOLEAN VALUES
