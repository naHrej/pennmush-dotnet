---
topics: [INTERIORS]
---

Here's a quick description of how to make things that can be entered:

@create Car
@desc Car=A shiny red car.
@idesc car=You are sitting inside a luxurious sportscar.
@set Car=enter_ok
```text
  @oxleave car=climbs out of the car.   { The 'ox' messages are shown to
  @oxenter car=climbs into the car.     { those OUTSIDE the object.
```

@oenter car=joins you inside the car. { The 'o' messages are shown to
```text
  @oleave car=gets out of the car.      { those INSIDE the object
  @enter car=You get into the car.      { The plain messages are shown to
  @leave car=You get out of the car.    { the one entering or leaving

```

Continued in 'help interiors2'.
