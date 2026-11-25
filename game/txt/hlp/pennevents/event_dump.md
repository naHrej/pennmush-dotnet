---
topics: [EVENT DUMP]
---

dump`5min (Original message, isforking)
```text
      Database save will occur in 5 minutes.
```

dump`1min (Original message, isforking)
```text
      Database save will occur in 1 minute.
```

dump`complete (Original message, wasforking)
```text
      Database save has completed.
```

dump`error (Error message, wasforking, exit_status)
```text
      Database save failed! You might want this to alert any admin on.
      exit_status has different meanings in forking and non-forking dumps.
      In forking: exit_status is a string, either "SIGNAL <int>" or "EXIT <int>". SIGNAL <int> refers to the mush process receiving error message via signal while EXIT <int> refers to mush process exiting abnormally.
      In nonforking: exit_status is "PERROR <string>" - string being the error message returned by strerror(errno). If you are seeing errors on dbsave, we recommend setting forking_dump to 0, as nonforking dumps have more verbose error messages.

```

The standard messages shown on dumps are still displayed when these events are set. To disable the standard message, set them to empty strings via @config or in mush.cnf.
