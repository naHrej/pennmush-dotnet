---
topics: [MONEY]
---

The MUSH has a built-in money system, which gives a starting amount of money to new players and hands out a daily allowance thereafter. MUSH money (the default name is "pennies", but this may be different depending on the particular MUSH) is spent on some MUSH commands that are computationally expensive or alter the database. In addition, every time you "queue" a command, it costs you a certain amount of money -- this prevents looping from getting out of control, since when all your money is spent, you can't queue any more commands.

The money system can also be used on player-created objects by setting @cost/@payment/@opayment/@apayment attributes and using the "give" command to give pennies, or by setting @pricelist/@buy/@obuy/@abuy attributes and buying items with the "buy" command. The Pay @lock on an object controls who can give it pennies.

The "score" command tells you how many pennies you have.

**See also:** COSTS, give, @cost, @payment, @lock, buy, @buy, score, money()
