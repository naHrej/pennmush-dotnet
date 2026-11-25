---
topics: [EVENT OBJECT]
---

object`create (new objid, cloned-from)
```text
      Triggered on the creation of any object except player. If it was created using @clone, then <cloned-from> will be a objid. Otherwise <cloned-from> will be null.

```

object`destroy (objid, origname, type, owner, parent, zone)
```text
      Triggered _after_ the object is totally destroyed. Passed arguments are former objid, name, type, owner, etc. Enactor is always #-1, so use former owner.

```

object`move (objid, newloc, origloc, issilent, cause)
```text
      Triggered after the object is moved, @tel'd, or otherwise sent to a new location. If <issilent> is 1, then the object was moved using @tel/silent.

```

object`rename (objid, new name, old name)
```text
      Triggered when any object is renamed.

```

object`flag (objid of object with flag, flag name, type, setbool, setstr)
```text
      Triggered when a flag or power which has the "event" restriction is set or cleared. <type> is one of FLAG or POWER. <setbool> is 1 if the flag/power is being set, and 0 if it's being cleared. <setstr> is either "SET" or "CLEARED".
    Example:
      &OBJECT`FLAG event handler=@cemit Admin=capstr(lcstr(%2)) %1 [lcstr(%4)] on [name(%0)] by %n.
```
