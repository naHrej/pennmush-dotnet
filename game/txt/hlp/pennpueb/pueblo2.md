---
topics: [PUEBLO2]
---

PennMUSH makes the following enhancements visible to Pueblo users when Pueblo support is enabled:

```text
    * Object/Room names are highlighted
    * Support for VRML graphics
    * Unordered list for contents and transparent exits
    * Contents and exits lists have links (Click an exit to walk through it)
    * Object lists (like the ones found in 'examine'/'inventory') have links
    * Conversion of accented characters into &entity; codes

```

While Pueblo brings a number of new features and markups to MUSHes, in many ways it's not well suited. Because it's based on HTML, multiple spaces are compressed, and Pueblo typically defaults to a variable width font. Because of this, supporting Pueblo is not just a matter of enabling the option. The output of any commands which rely on fixed spacing, such as a +who, must be wrapped in <pre> tags to ensure they appear correctly for players using Pueblo. For instance:

```text
    > &cmd`who Globals=$+who: @nspemit %#=tagwrap(pre, u(fun`who))

```

**See also:** pueblo(), HTML Functions
