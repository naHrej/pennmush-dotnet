---
topics: [EVENT EXAMPLES2]
---

Suppose you want @pcreated players to be powered builder, set shared and zonelocked to roys, but players created at the connect screen to not be.
> @set Event=wizard
> &PLAYER`CREATE Event=@assert %# ; @pemit %#=Auto-Setting [name(%0)] Builder and shared ; @power %0=builder ; @lock/zone %0=FLAG^ROYALTY ; @set %0=shared
> @pcreate Grid-BC
Auto-Setting Grid-BC Builder and Shared

The Event Handler object, since it's handling so many events, may become cluttered with attributes. We recommend using @trigger and @include to separate events to multiple objects.
