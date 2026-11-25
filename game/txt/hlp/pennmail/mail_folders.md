---
topics: [mail-folders, "@mail/folder", "@mail/unfolder", "@mail/file"]
---

The MUSH mail system allows each player 16 folders, numbered from 0 to 15. Mail can only be in 1 folder at a time. Folder 0 is the "inbox" where new mail is received. Most @mail commands operate on only the current folder.

@mail/folder
```text
        This commands lists all folders which contain mail, telling how many messages are in each, and what the current folder is.

```

@mail/folder <folder#|foldername>
```text
        This command sets your current folder to <folder#>.

```

@mail/folder <folder#> = <foldername>
```text
        This command gives <folder#> a name.

```

@mail/unfolder <folder#|foldername>
```text
        This command removes a folder's name

```

@mail/file <msg-list>=<folder#>
```text
        This command moves all messages in msg-list from the current folder to a new folder, <folder#>.

```

**See also:** @mailfilter
