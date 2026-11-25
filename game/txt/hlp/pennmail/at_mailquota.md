---
topics: ["@mailquota"]
---

@mailquota <player>[=<limit>]

This attribute allows wizards to change the maximum number of messages a player can have in their inbox, override the mail_limit @config option for specific people. <limit> should be a number between 1 and 50000, inclusive.

Example:
```text
    > @alias *Walker=Complaints_Department
    > @mailquota *Complaints_Department=50000
    > @wall Please @mail any and all problems to Complaints_Department.
```
