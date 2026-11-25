---
topics: [EVENT DB]
---

db`dbck: Run after the regular database consistency check.
db`purge: Run after the regular purging of destroyed objects.
db`wcheck: Run after the regular @warnings check.

Note: These events are only triggered after the automatic scheduled checks, and not if someone manually runs @dbck, @purge or @wcheck.
