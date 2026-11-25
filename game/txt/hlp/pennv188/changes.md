---
topics: [changes, 1.8.8p1]
---

Version 1.8.8 patchlevel 1 ??? ?? 202?

This is a list of changes in this patchlevel which are probably of
interest to players. More information about new commands and functions
can probably be gotten via 'help <name of whatever>'. 'help credits'
lists the [initials] of developers and porters that are used in the list
of changes.

Information about changes in prior releases can be found under
help topics named for each release (e.g. 'help 1.8.7p0').
A list of the patchlevels associated with each release can
be read in 'help patchlevels'.

Minor changes:

* Sqlite updated to 3.36.0. [SW]
* cJSON updated to 1.7.13 [SW]
* PCRE updated to 10.35 [SW]
* New [1m--version[0m option to the netmush binary to display the version and exit. [SW]
* Fix setsymdiff missing from 'help function list'. PR by Jules@M*U*S*H.
* Add 'owner()' to help files dealing with attribute ownership. PR by Jules@M*U*S*H.

Fixes:

* [1mudefault[0m only accepted 12 arguments, not the documented 32. [MG]
* [1mMOGRIFY`FORMAT[0m was not being passed the mogrified channel name from [1mMOGRIFY`CHANNAME[0m as it should. Reported by Xperta [MT]
