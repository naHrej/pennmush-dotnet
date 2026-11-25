---
topics: [REGEXPS2]
---

Regular expressions are extremely useful when you want to enforce a data type. For example, if you have a command where you want a player to enter a string and a number ('+setnum <player>=<number>', for example), you might do it like this:

```text
    > &DO_NUM Command Object=$^\+setnum (.+)=([0-9]+)$: @va me=Data: %1 = %2
    > @set Command Object/DO_NUM=regexp

```

Then, '+setnum cookies=30' would set VA to "Data: cookies = 30". This eliminates your having to check to see if the player entered a number, since the regular expression matches only numbers. Furthermore, the '+' guarantees that there needs to be at least one character there, so a player can't enter '+setnum cookies=' or '+setnum =10' or similarly malformed input.

The '+' sign in the command has to be escaped out, or it is taken as a regexp token. Furthermore, the pattern-match has to be anchored with ^ and $, or something like 'try +setnum cookies=30 now' would also match.

However, keep in mind that players who attempt to use the command and give invalid input (such as "+setnum cookies=thirty") will receive the normal, non-descriptive Huh? message. Using a broader match and validating the input in softcode, so you can give more descriptive error messages, may be desirable.

Regular expression syntax is explained in 'help regexp syntax'.
