---
topics: [regexp syntax3]
---

Part of a pattern that is in square brackets is called a "character class". It matches any character listed in the class. In a character class, the only metacharacters are:

```text
       \      general escape character
       ^      negate the class, if the first character in the class
       -      indicates character range (e.g. A-Z, 0-4)
   [:NAME:]   A symbol for a group of characters that can vary according to the language the mush is using. See 'help regexp classes' for more information.
       ]      terminates the character class

```

A backslash will escape most metacharacters, and can turn some normal characters into generic character types:

```text
       \d     any decimal digit
       \D     any character that is not a decimal digit
       \s     any whitespace character
       \S     any character that is not a whitespace character
       \w     any "word" character (letter, digit, or underscore)
       \W     any "non-word" character

```

Continued in '"help regexp syntax4'.
