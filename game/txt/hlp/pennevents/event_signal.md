---
topics: [EVENT SIGNAL]
---

No arguments are passed to these events.

signal`usr1: Triggered when the PennMUSH process receives a "kill -USR1"

signal`usr2: Triggered when the PennMUSH process receives a "kill -USR2"

If these attributes exist, then penn will NOT perform what it usually does when it receives a signal. In effect, these override Penn's default actions.

To mimic old behaviour:
&SIGNAL`USR1 Event Handler=@nspemit/list lwho()=GAME: Reboot w/o disconnect from game account, please wait. ; @shutdown/reboot
&SIGNAL`USR2 Event Handler=@dump
