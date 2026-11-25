---
topics: [PARENTS4]
---

If a parent has the same $-command name in a different attribute, however, BOTH the parent and child commands will execute:

(continued from previous example)
```text
    > &CHECK Bar=$check:@emit No, I'm the parent! ([name(me)])

    > check
    I'm the child (Foo)
    No, I'm the parent! (Foo)

```

The attributes inherited from the parent are treated just like its own attributes by the child. Thus, when a $-command or @trigger is executed, "me", for example, refers to the child, not the parent, and the $-command's associated actions are performed by the child.

@parent is most useful when several objects use common attributes.

Continued in 'help parents5'.
