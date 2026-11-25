---
topics: [EVENT LIST]
---

Event names are of the format <type>`<event>. The 'type' is used simply to group similar events together for help.

Event syntax in the help is of the form:
<eventgroup>`<eventname> (What is passed as %0, %1, ... %9)

The following event types and events have been added to PennMUSH. To see the help for them, type 'help event <type>'.

dump: dump`5min, dump`1min, dump`complete, dump`error
db: db`dbck, db`purge, db`warnings
log: log`err, log`cmd, log`conn, log`trace, log`check, log`huh
object: object`create, object`destroy, object`move, object`rename, object`flag
sql: sql`connect, sql`connectfail, sql`disconnect
signal: signal`usr1, signal`usr2
player: player`create, player`connect, player`disconnect, player`inactivity
socket: socket`connect, socket`disconnect, socket`loginfail, socket`createfail
http: http`blocked http`fail http`command
