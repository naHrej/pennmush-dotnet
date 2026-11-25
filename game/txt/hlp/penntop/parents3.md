---
topics: [PARENTS3]
---

If a child and its parent both have the same attribute set, the attribute on the child will always be used first. However, a child can use the pfun() function to get the value of an attribute from its parent, even when it has an attribute with the same name.

For example:
```text
    > &TEST Bar=$test:@emit I'm the parent ([name(me)])
    > &TEST Foo=$check:@emit I'm the child ([name(me)])
    > @parent Foo=Bar
    > test
    Huh?  (Type "help" for help.)
    > check
    I'm the child (Foo)

```

Continued in 'help parents4'.
