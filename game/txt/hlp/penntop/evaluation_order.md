---
topics: [EVALUATION ORDER]
---

Whenever some text is entered by an object, the MUSH attempts to match it against a valid game command in the following order of possible commands:

```text
    Socket commands (*): QUIT, SCREENWIDTH, etc
    Single-token commands: ", :, ;, +, \, #
    MUX-style channel aliases, if enabled (see 'help muxcomm')
    Exits in the room
    &attribute setting
    Regular game commands: get, inventory, @emit, etc
    @attribute setting
    Enter aliases
    Leave aliases
    User-defined commands on nearby objects. All such $-commands are matched and executed.
    If there are no user-defined commands nearby:
      If the zone of the player's location is a zone master room,
        Zone master room exits
        Zone master room user-defined commands
      Else
        User-defined commands on the zone of the player's location

```

(*) Socket commands are only matched when the command is entered by a player directly from their client.

Continued in 'help evaluation2'.
