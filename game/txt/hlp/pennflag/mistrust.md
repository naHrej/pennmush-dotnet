---
topics: [MISTRUST]
---

Flag:  MISTRUST  (things, rooms, exits)

The MISTRUST flag prevents an object from controlling another object via Control @locks, Zone @locks on ZMOs or SHARED players, or from controlling other objects with the same owner. It also prevents the object from inheriting the no_pay and no_quota powers from its owner.

This flag can be used when you with a single player to retain ownership of objects that other players will control or run arbitrary code on, and don't want those objects to be able ot affect your other objects.

Note that if a MISTRUST object is set WIZARD it will still control all objects in the database, and that MISTRUST does not prevent no_pay or other powers from being applied to the object directly.

**See also:** control, TRUST
