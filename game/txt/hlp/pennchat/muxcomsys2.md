---
topics: [MUXCOMSYS2, MUXCOMM2]
---

To enable the MUX comsys aliases, set use_muxcomm to yes in game/mush.cnf and @shutdown/reboot.

Aliases are stored by "addcom" in attributes on the player, in the format:

```text
    &CHANALIAS`<alias> <player>=<channel-name>

```

Aliases are checked on parents, so you can add them to your ancestor player to have them usable by everyone.
