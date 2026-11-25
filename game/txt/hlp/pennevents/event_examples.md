---
topics: [EVENT EXAMPLES]
---

Suppose you want random dbsave messages:
> &DUMP`COMPLETE Event Handler=@config/set dump_complete=SAVE: [v(randword(lattr(me/dumpmsg`*)))]
> &DUMPMSG`NOTHING Event=The Database has been saved, nothing to see here.
> &DUMPMSG`GRETZKY Event=The Database saves, but Gretzky scores!
> &DUMPMSG`GEICO Event=The Database saved 15% by switching to Geico!
> @dump
SAVE: The Database has been saved, nothing to see here.
> @dump
SAVE: The Database saved 15% by switching to Geico!

Or admin want to be notified when a player connect attempt fails.
> @set Event=wizard
> &SOCKET`LOGINFAIL Event=@wizwall/emit On descriptor '%0' from IP '%1' a failed connect attempt to '%4': '%3'
(Later, a player attempts to log in as #1)
Broadcast: [Event Handler]: On descriptor 3, from IP '127.0.0.1', a failed connect attempt to '#1': 'invalid password'

For more examples, see 'help event examples2'.
