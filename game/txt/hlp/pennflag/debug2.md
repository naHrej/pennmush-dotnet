---
topics: [DEBUG2]
---

> @create Test
> @set Test=DEBUG
> &cmd test=$wc *: say String %0 has [strlen(%0)] letters and [words(%0)] words.
> wc This is my test string

#14! String %0 has [strlen(%0)] letters and [words(%0)] words. :
#14!  strlen(%0) :
```text
  #14!   %0 => This is my test string
```

#14!  strlen(%0) => 22
#14!  words(%0) :
```text
  #14!   %0 => This is my test string
```

#14!  words(%0) => 5
#14! String %0 has [strlen(%0)] letters and [words(%0)] words. =>
```text
    String This is my test string has 22 letters and 5 words.

```

Test says, "String This is my test string has 22 letters and 5 words."
