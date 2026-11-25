---
topics: ["@CHATFORMAT2"]
---

Examples:

Walker's preferred @chatformat, which strips all ansi out, wraps every line to your width and prefixes them with <ChannelName>:

```text
    @chatformat me=<%1> [switch(%0,@,%2,edit(wrap(speak(&[if(%4,%4%b)]%3,%0[stripansi(%2)],%6\,),sub(width(%!),add(4,strlen(%1)))),%r,%r<%1>%b))]

```

If you're on a system with chat_strip_quote set to "no", you might want to change the '%0%2' arg to speak() to '[switch(%0,",%2,%0%2)]'

Suppose you want it just like the old version, but anytime somebody says your name, you want it all in red:

```text
    @chatformat me=ansi(switch(%2,*[name(%!)]*,r,n),%5)

```

See 'help @chatformat3' for more examples.
