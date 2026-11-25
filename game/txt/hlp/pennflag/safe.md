---
topics: [SAFE]
---

Flag:  SAFE  (all types)

Objects set SAFE cannot be destroyed with @destroy (@nuke must be used instead). If the "really_safe" @config option is on, objects set SAFE will not be destroyed when their owner is @destroyed, but will be @chown'd to God instead.

There is also a SAFE attribute flag, which protects a single attribute from being altered; see 'help attribute flags' for more information.

**See also:** @destroy, DESTROY_OK
