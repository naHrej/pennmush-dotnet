---
topics: [BOOLEAN VALUES]
---

A boolean variable, for those of you not familiar with programming, is a variable that is either true or false. Normally, a value of 1 is considered "true" and a value of 0 is considered "false". Many MUSH functions return either 1 if they are true or 0 if false. For example, the hasflag() function tests to see if an object has a certain flag set on it. If hasflag(<object>,<flag name>) is true (the object has the flag), it will return 1. If it is false, it will return 0.

Other functions expect to operate on boolean values. What they consider "true" or "false", however, depends on the setting of the "tiny_booleans" config option (@config tiny will show this).

Continued in 'help boolean2'.
