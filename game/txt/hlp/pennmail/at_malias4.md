---
topics: ["@malias4"]
---

@malias/use <alias>=<perm list>
@malias/see <alias>=<perm list>

@malias/use controls who may use an alias. Players who may use an alias will see it in their @malias list, and can @mail to the alias.

@malias/see controls who may list the members of an alias.

An empty permission list allows any player. The permission list may also be a space-separated list of one or more of "owner", "members" (of the alias), and "admin".

By default, the owner and alias members may see and use the alias, but only the owner may list the members. Note that admin may always list aliases and their members, regardless of these settings, but are treated like anyone else when trying to @mail with an alias.

Continued in 'help @malias5'.
