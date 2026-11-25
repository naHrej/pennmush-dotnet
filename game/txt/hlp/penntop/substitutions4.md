---
topics: [SUBSTITUTIONS4, %4]
---

Example:

> @sex me=male
> @drop box=%n just dropped %p box.
> drop box
Cyclonus just dropped his box.

Let's say that Cyclonus's dbref number is #10 and the box's dbref number is #11. The dbref of the room Cyclonus is standing in is #13. When Cyclonus dropped the box above, these were the values of the following %-subs:

%n = Cyclonus
%# = #10
%@ = #10
%! = #11
%L = #13
