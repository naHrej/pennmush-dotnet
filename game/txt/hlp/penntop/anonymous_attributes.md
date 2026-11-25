---
topics: [ANONYMOUS ATTRIBUTES, LAMBDA, "#LAMBDA", "#APPLY"]
---

In many cases where a function expects a object/attribute pair that refers to an attribute to evaluate, you can use the form

#lambda/<code>

instead, and the code will be treated as an attribute's body. The code will normally be parsed twice, so special characters should be escaped where needed.

If the #lambda just calls one other function, the form

#apply[<number of arguments>]/<function name>

can be used instead. If the argument count is left out, it defaults to 1.

These anonymous attributes should be used for short and simple pieces of code. Anything long or complicated should go in an actual attribute, for readability and maintainability.

See 'HELP ANONYMOUS2' for examples.
