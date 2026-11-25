---
topics: [mail-forward, "@mailforwardlist"]
---

@mailforwardlist <object>[=<space-separated list of dbrefs or objids>]
@lock/mailforward <object>[=<lock>]

By setting a @mailforwardlist attribute, a player can direct that @mail they receive should be delivered to the specified list of dbrefs of other players. The list may include the player's own dbref, in which case the player will receive a copy of the message, or omit it, in which case the message will be delivered to those listed but the player will not receive a copy.

To deliver messages to other players this way, you must control them (i.e. you're delivering to yourself or you're a wizard) or pass their @lock/mailforward. An empty @lock/mailforward disallows forwarding to you, and is the default.
