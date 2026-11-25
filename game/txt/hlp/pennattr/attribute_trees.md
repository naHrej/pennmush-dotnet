---
topics: [ATTRIBUTE TREES, ATTR TREES, ATTRIB TREES, `]
---

Attributes can be arranged in a hierarchical tree; these are called "attribute trees", and a conceptually similar to the way that files and directories/folders are organized on computer filesystems. Attribute trees can be used to reduce spam when examining and to provide organized control over permissions for related attributes.

Attribute trees use the backtick (`) character to separate their components (much as filesystems use / or \). For example, the following attribute name would be a couple levels down in its tree:

CHAR`SKILLS`PHYSICAL

Attribute names may not start or end with the backtick, and may not contain two backticks in a row.

All attributes are either branch attributes or leaf attributes. A branch attribute is an attribute that has other branches or leaves beneath it; a leaf attribute is one that does not. Any attribute may act as a branch. If you try to create an unsupported leaf, branch attributes will be created as needed to support it.

See 'help attribute trees2' for more information and examples.
