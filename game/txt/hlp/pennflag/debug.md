---
topics: [DEBUG]
---

Flag: DEBUG  (all types)

The DEBUG flag is used for debugging MUSHcode. When an object is set DEBUG, all parser evaluation results will be shown to the object's owner and to any dbrefs in the object's DEBUGFORWARDLIST, in the format:

#dbref! <string to evaluate> :
#dbref!  recursive evaluation of functions in string
#dbref! <string to evaluate> => <evaluated string>

Because the parser does recursive evaluations, you will see successive messages evaluating specific parts of an expression. This enables you to pinpoint exactly which evaluation is going wrong.

Objects run under this flag are computationally expensive, and can generate large amounts of spam, so this flag should only be set when needed, and cleared afterwards.

There's also a DEBUG attribute flag, which only affects a single attribute; see 'help attribute flags' for more information. You can also use the "}" command prefix to run a command with DEBUG output just once.

See 'help debug2' for an example.
**See also:** VERBOSE, PUPPET, }
