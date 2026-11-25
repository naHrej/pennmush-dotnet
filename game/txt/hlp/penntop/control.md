---
topics: [CONTROL]
---

Controlling an object basically means that you have the power to change the object's characteristics such as flags and attributes. It may also mean that you have the ability to destroy it.

These checks are performed, from top to bottom, to see if object O controls object V:
1. If O has the Guest power, O does not control V
2. If O and V are the same object, O controls V
3. If V is God, O does not control V
4. If O is a Wizard, O controls V
5. If V is a Wizard, O does not control V
6. If V is Royalty and O is not, O does not control V
7. If O is MISTRUST, O does not control V
8. If O and V are owned by the same player, and either:
```text
       a. V is not set TRUST, or
       b. O is set TRUST
      then O controls V
```

9. If V is a player, or set TRUST, O does not control V
10. If the zone_control_zmp_only @config option is set to 'No', V is on a Zone, and O passes the Zone's @lock/zone, O controls V
11. If V is owned by a SHARED player, and O passes the owner's @lock/zone, O controls V
12. If V has an @lock/control, and O passes the lock, O controls V
13. O does not control V

**See also:** controls(), TRUST, MISTRUST, ZONES, SHARED PLAYERS
