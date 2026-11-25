---
topics: [help search, help/search, helpfile]
---

help <textname>
help <namepattern>
help/search <pattern>
help/query[/brief] <sqlite3query>

These commands and switches also work with other things using our helpfile setup: news, ahelp, and any others added.

help <textname> is how to see a help file.
> help helpfile
> help @search

help <namepattern> is a good way to read long helpfilenames or find a pattern you're looking for. If <namepattern> matches multiple entries, it will show you their names.
> help @tri*2
> help sort*()

If you want to find helpfiles that _contain_ a pattern, use "help/search". For example:
> help/search @trigger
> help/search @switch

help/query is a more advanced and complex way to search our helpfiles, see "help helpfile2" for more.

To add more categories and commands (like 'news'), read "game/txt/README"

**See also:** textfile(), textentries(), textsearch()
