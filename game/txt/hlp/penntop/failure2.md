---
topics: [failure2]
---

The following failures are defined in PennMUSH:

```text
  Failure to...                                 Lock     Attribute
```

--------------------------------------------- -------- --------------------
```text
  "get" a player/thing, pass through an exit,   Basic    @failure
    or "look" in a room
  run an $-command on an object                 Command* COMMAND_LOCK`FAILURE
  use zwho()                                    Zone     ZONE_LOCK`FAILURE
  leave your current location                   Leave    @lfail
  "take" from an object                         Take     TAKE_LOCK`FAILURE
  "drop" a thing, or drop something in a room   Drop     DROP_LOCK`FAILURE
   enter an object                              Enter    @efail
  "follow" an object                            Follow   FOLLOW_LOCK`FAILURE
  "give" an object away                         Give     GIVE_LOCK`FAILURE
  "give" money to or "buy" from an object       Pay      PAY_LOCK`FAILURE
  "@chzone" something to a zone                 Chzone   CHZONE_LOCK`FAILURE
  "use" an object                               Use      @ufail
  speak via say/pose/@*emit/teach in a room     Speech   SPEECH_LOCK`FAILURE
  "page" or "@pemit" to an object               Page     PAGE_LOCK`FAILURE**
  "@mail" a player                              Mail     MAIL_LOCK`FAILURE

```

* The Use lock can also prevent you from running an $-command, but it will still trigger COMMAND_LOCK`FAILURE.
** @haven or @away will also be shown on failure to "page", if set
