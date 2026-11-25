---
topics: ["@CHATFORMAT3"]
---

A popular feature in clients now available in PennMUSH directly: Let's suppose you want "Public" channel chatter to all be green, "Softcode" to be blue and "Admin" to be cyan.

```text
    @chatformat me=ansi(switch(%1,Public,g,Softcode,b,Admin,c,n),%5)

```

Maybe you dislike players who re-@name themselves a lot:

```text
    &playernames me=#6061:Walker #7:Javelin #6388:Cheetah
    @chatformat me=<%1> [switch(%0,@,%2,speak(&[if(%4,%4%b)][firstof(after(grab(v(playernames),%#:*),:),%3)],%2,%6\,))]

```

Or you're writing a loggerbot, and you want to convert all channel input to HTML:

```text
    @chatformat me=CHAT:%1:[edit(switch(%0,@,%2,speak(if(%4,%4%b)%3,%0%2,%6\,)),&,&amp;,<,&lt;,>,&gt;,%r,<BR>,%b%b,%b&nbsp;)]
     or
    @chatformat me=CHAT:%1:[render(switch(%0,@,%2,speak(if(%4,%4%b)%3,%0%2,%6\,)),html)]
```
