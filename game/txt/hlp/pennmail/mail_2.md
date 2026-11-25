---
topics: [MAIL()]
---

mail()
mail(<player name>)
mail([<folder #>:]<mail message #>)
mail(<player>, [<folder #>:]<mail message #>)

Without arguments, mail() returns the number of messages in all the player's mail folders. With a player name argument, mail() returns the number of read, unread, and cleared messages <player> has in all folders. Only Wizards can use this on other players.

When given numeric arguments, mail() returns the text of the corresponding message in the current folder. The message number may also be prefaced by the folder number and a colon, to indicate a message in a different folder.

Example:
> think mail(3:2)
(text of the second message in the player's third folder)

**See also:** maillist(), mailfrom()
