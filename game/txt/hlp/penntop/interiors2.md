---
topics: [INTERIORS2]
---

Now, if you want people inside to be able to hear and communicate with the outside, you also need to do the following.

@set car=audible  (lets people outside hear what's being said in the car.
```text
  @listen car=*     (lets people inside hear what's being said outside.
```

@prefix car=From inside the car,
@inprefix car=From outside,
@filter car=* has arrived.,* has left.,joins you inside the car., gets out of the car.
@infilter car=* has arrived.,* has left.,* climbs out of the car., * climbs into the car.

(The filters will keep people on the outside from seeing the 'o' messages and people on the inside from seeing the 'ox' messages which is a good thing.)

**See also:** enter, leave, @prefix, @filter, AUDIBLE, @listen
