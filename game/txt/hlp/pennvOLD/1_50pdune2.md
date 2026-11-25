---
topics: [1.50pdune2]
---

```text
Version 1.50 patchlevel dune2              March 5, 1995

```

DuneSource pl2 changes

Major changes:

* Extensive warning system for things missing on objects, rooms, etc.
```text
    @warnings command for players to set the level of warnings they want,
    @wcheck[/all] command for players to check an object (checks can also run
    at a God-configurable interval on the whole db), NOWARN flag for objects
    and players. Idea from Kalkin's adaptation of MUSE code, totally
    rewritten.


```

New options (dune.h):

* GUEST_TEXTFILE option enables a textfile (guest.txt by default)
```text
    to be shown to Guests who log in. Idea and source code from Kalkin.
```

* PAGELOCK_NOT_USELOCK option causes @lock/use to fail for players,
```text
    requiring them to type @lock/page. The lock itself has not changed,
    just the interface, to remind players of its function. By Ralph Melton.
```

* More control over possessive gets with the POSSESSIVE_GET define
```text
    and the POSSGET_ON_DISCONNECTED define. Possessive get can be
    disabled, enabled for all but disconnected players, or enabled
    at all times (the default PennMUSH behavior)

```

New functions:

* lit() returns its input totally unparsed, and without spaces compressed.
* t() returns the truthvalue of its input, guarranteed to be either
```text
    1 or 0.
```

* objmem() and playermem() functions, return the memory usage of
```text
    an object or all of a player's objects. Requires search_all power.
```

* grab(list,pattern[,delimiter]), returns the first element of list
```text
    which patches the pattern. Patterns are of the match() type.
    From the TinyMUSH 2.2 code.

```

Minor Changes:

* You must actually own an object (not just control it) or be a
```text
    Wizard in order to set it chown_ok or dest_ok. By Ralph Melton.
```

* You can use #$ in the actions in a switch() function
```text
    or @switch/@select statement, and it will be replaced by the switch
    expression before executing or evaluating. This can improve
    efficiency and save space. For example:
        switch(complexfunc(%0),Bob,BobResult,#$)
    is equivalent to:
        switch(complexfunc(%0),Bob,BobResult,complexfunc(%0))
    but only requires a single evaluation of complexfunc(%0).
    Suggested by Kenric@DuneII.
```

* "things" is a synonym for "objects" in @search now. By Ralph Melton

--- Fixes ---

* #-2 is treated as a "0" (false, like #-1) in functions which need a
```text
    logical value for it. Previously, it was treated as -2 (true).
```

* @select is now considerably more efficient - it no longer will
```text
    evaluate anything after the matched action. The old behavior
    could well be a bug in the right conditions as well.
```

* atr_add now rejects null attribute names, so you probably can't set them
```text
    any more. Suggested by Kalkin, the code's by Mike@StarWars.
```

* Players can reset a DARK flag on themselves, but still can not
```text
    set themselves DARK.
```

* andflags(me,!Dc) used to return 1 if I am !dark and !connected,
```text
    instead of !D and connected, as the help states. Fix by Ralph
    Melton.
```

* Halting an object which is @wait'ing used to fail to decrement
```text
    the owner's queue attrib. Fixed now. Patch by Ralph Melton.
```

* set_flag uses strupper instead of upcasestr now, which should fix
```text
    a bug on some systems wherein "cd" command would crash the MUSH and
    a similar bug wherein players connecting would crash the MUSH if
    the ON-VACATION flag was in use.
```

* The old channel syntax (@channel foo=command) works again.
* The RULES option now works.
* The convtime() has been rewritten to work on NeXT's correctly.
```text
    Previously, its malloc failed and it returned -1.
```

* Systems which have getrlimit, but *don't* have RLIMIT_NOFILE,
```text
    are now supported. Notably, Aix 2.x and 3.x.
```

* The installation hints for Solaris 2 have been improved.
```text
    WAIT_TYPE is defined as int, and if NBPC can't be found for
    getpagesize(), it'll try PAGESIZE instead. Thanks to Kalkin for these.
```

* Installation for AIX has been improved. AIX required inclusion of
```text
    sys/select.h in the IDENT stuff.
```

* Various rewrites of tests against NOTHING to use the GoodObject()
```text
    macro instead. Thanks to Ralph Melton for some of these.
```

* Got rid of some warnings when compiling mkindx
