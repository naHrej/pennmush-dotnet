---
topics: [GUEST POWER]
---

Objects set Guest are very limited: they cannot control anything in the database (including themselves), which prevents them from setting flags or attributes, etc (though, for backwards compatability, they can still examine themselves). They cannot join channels, or create objects. MUSHes often restrict other commands so that guests cannot use them.

It's also possible to connect to a Guest player without using a password, allowing them to be used by newcomers to visit a game without making a character.

Guests also have (and receive) a different amount of pennies to normal players (determined by the max_guest_pennies and guest_paycheck @config options).  They receive a different message when they connect (set via the guest_file config option). The way Guest connections work can be controlled via the max_guests @config option, and Guest logins can be disabled entirely with the guests @config option.
