---
topics: [OBJIDS, OBJECT IDS]
---

When an object is destroyed, its dbref number will eventually be recycled and given to a newly created object. This can cause problems in code, particularly in database code which stores members of a group, as code which was meant to refer to the old object ends up referring to the new one by mistake.

To avoid this problem, you can use the "object id", or objid, instead of the dbref. An object id consists of the object's dbref, a colon, and then the object's creation time. Objids can be used anywhere dbrefs can and, because the creation time is different each time the dbref is recycled, the objid is totally unique to each object.

The objid() function returns the object id of an object, and the %: substitution evaluates to the objid of the enactor.

**See also:** objid(), @lock-objid, DBREFS
