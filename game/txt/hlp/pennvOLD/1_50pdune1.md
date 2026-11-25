---
topics: [1.50pdune1]
---

```text
Version 1.50 patchlevel dune1              March 5, 1995

```

DuneSource pl1 changes

Major changes:

* Whem players attempt to log in to a Guest character that is already
```text
    in use, the server tries to find one that isn't, and connects the
    player to that. If it can't, you get the old behavior (two connections
    to the single Guest).
```

* Extended @mail system is available with many new mail commands.
* Dump warnings 5 minutes and 1 minute before non-forking
```text
    dump, and optional announcement at completion of dump.
```

* Guest players may not set attributes.
* Kill command can be disabled.
* @aconnect/@adisconnect messages on individual rooms.

Changes to commands:

* CD command at connection screen allows Wizards to connect to the game
```text
    DARKly.
```

* @sitelock command for on-the-fly sitelocking by Wizards.
* @dump/paranoid can try to fix the db in memory, too.
* @decomp/db, produces decomp output using dbref of object instead
```text
    of its name.
```

* ex/mortal, shows examine output as if player were mortal.
* Option to rename @destroy to @recycle, since @dest and @desc are
```text
    easy to confuse.
```

* @restart command. Combines an @halt with triggering @startup.
* @hide now provides feedback when used.
* @find may be restricted to royalty, the only ones for whom it might
```text
    possibly be useful.
```

* @function now lets you know when it updates an @function.
* The old (pl8?) @channel <channel>=<function> syntax is back, along
```text
    with @channel/<function> <channel>, for those who liked it.
```

* @grep can be either case-sensitive or not (the default).
* If you don't specify the destination attribute in @cpattr, it
```text
    assumes you want the same attrib name as the source attrib.
```

* @mvattr, like @cpattr, but removes old attribute.
* ANSI players now see their @edit additions in bold.
* Rooms and exits can @emit. Rooms @emit into themselves (like @remit),
```text
    and exits @emit to their source room.
```

* @squota can now be given quota levels as +<quota> or -<quota>, to
```text
    increase or decrease a player's quota from its current level.

```

Changes to functions:

* encrypt() and decrypt() functions.
* hidden() function for checking if a player is hidden.
* hastype() function for checking if a thing is a given type.
* pemit() function sends pemits to a list of objects.
* lparent() function, returns object's parent, grandparent, etc.
* quota() function, returns a player's quota.
* N-dimensional vector arithmetic functions: vadd(), vmul(), vsub(),
```text
    vmag(), vdim(), vunit()
```

* haspower() can be restricted to admin.
* "Lower math" functions: shr(), shl(), inc(), dec()
* beep() with no arguments produces a single beep.
* pmatch() will now accept *player or #dbref arguments.
* lsearchr() function does an lsearch *backwards* through the db.

Changes to flags/powers:

* Flags can be defined as dark (invisible to all but God),
```text
    mdark (invisible to mortals), and odark (invisible to mortals who don't
    own the object) in flags.c.
```

* @cemit can be granted as a power
* The ability to set/unset the DEBUG flag on objects the player controls
```text
    can be granted as the Can_Debug power. (Idea by Kenric@DuneII)
```

* Optional COLOR flag to control reception of ANSI color apart from
```text
    ANSI hiliting.
```

* OPAQUE flag on exits. OPAQUE exits in translucent rooms look
```text
    like normal exits in non-translucent rooms.
```

* FIXED flag prevents @tel and home.
* DARK Wizards need not trigger @aenter.
* The SUSPECT flag is now abbreviated 's'.
* NO_LEAVE flag on objects prevents 'leave' command from working in them.
```text
    Useful for flying vehicles and the like.

```

Minor changes:

* &adestroy attribute triggered when object is dest'd.
* Player/room/object names can appear automatically in bold.
* Dark and Hidden players are distinguished on the admin WHO list
* Dark and Hidden players are indicated on the admin DOING list
* Wizards who idle are set hidden, not DARK. Same for royalty.
* DARK wizards enter and leave @chat channels silently.
* Royalty can now see the Wizard @channel/list
* The mortal @channel/list lists only public channels.
* @idle/@haven/@away attribs which evaluate to nothing are not
```text
    sent to the paging player.

```

--- Fixes ---

* TRANSPARENT variable exits don't crash the MUSH when looked at.
* @channel/delete and @channel/privs take the right number of arguments
* @decomp now decompiles flags properly.
* When logins are disabled, players may not create characters.
* The controls() function is made safer. Defining SAFER_CONTROL prevents
```text
    anyone but God from controlling God, any non-wizard from controlling
    anything with a wizbit, and any non-admin from controlling anything with
    a roybit.
```

* Player names are made safer. Defining SAFER_PLAYER_NAMES prevents the
```text
    use of [, ], %, and \ in player names.
```

* The strupper() function is made safer.
* Mortals can remove the DEBUG flag on objects they own.
* The set functions now take delimiters like they should.
* Revwords() takes a delimeter, like it should.
* @config displays whether function side effects are available correctly.
```text
    It used to get it backwards.
```

* Some checks against NOTHING have been changed to use the GoodObject()
```text
    macro in look.c and possibly other places.
```

* It's more difficult for players to enter themselves.
* PLAYER_NAME_SPACES works right now, use double-quotes around
```text
    multi-word names.
```

* haspower() on unknown powers now returns #-1 NO SUCH POWER instead
```text
    of a null string.
```
