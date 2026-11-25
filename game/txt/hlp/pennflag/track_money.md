---
topics: [TRACK_MONEY]
---

Flag: TRACK_MONEY (players)

By setting the TRACK_MONEY flag, a player can determine which objects may be using their money. TRACK_MONEY reports all charges to a player and their objects except the queue deposit.

> @set me=TRACK_MONEY
> give Javelin=50
You give 50 pennies to Javelin.
GAME: Walker spent 50 pennies!
> @create foo
GAME: Walker spent 10 pennies!
Created: Object #345.
> @for foo=@search eval=1
GAME: foo(#345) spent 100 pennies!
(search results)
> <a whole buncha commands>
GAME: Object Walker(#123) lost a Penny to queue loss.

**See also:** no_pay
