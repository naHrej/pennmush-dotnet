---
topics: ["@mailfilter", mailfilter]
---

The @mailfilter attribute specifies automatic filing of incoming @mail messages into folders. When an @mail message is received, the contents of @mailfilter are evaluated, with the following arguments passed:
```text
     %0     dbref of message sender
     %1     message subject
     %2     message body
     %3     message status flags (a string containing U, F, and/or R, for urgent, forwarded, and/or reply, respectively)

```

If @mailfilter evaluates to a folder name or number, the message will be filed into that folder. If @mailfilter evaluates to a null string, the message remains in the incoming folder.

Example: Filter urgent messages into folder 1
> @mailfilter me=if(strmatch(%3,*U*),1)

**See also:** mail-folders
