---
topics: [DATABASE, DBREFS, DBREF NUMBER, "DBREF #"]
---

You will find the term "dbref" or "dbref number" used frequently in these help files and in MUSHcode. It is an abbreviation of "database reference number".

The database is the part of MUSH that stores all the information about this particular MUSH. Players, things, rooms, and exits are all objects in the database. Each object in the database has a unique dbref number that is set when the object is created. You can use the dbref number to refer to an object that is not in your current location, and it is especially important for global code.

Using DBREFs is also faster than using names, even if the object is in your location. This is because whenever you try to do something with an object (such as look at it, take it, etc.), the MUSH first has to locate the object. Since the dbref is unique, it can immediately find the object rather than checking through all the contents of your area to see if one matches the name.

Continued in 'help dbref2'.
