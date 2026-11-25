---
topics: [CDESC(), CUSERS(), CMSGS(), CBUFFER()]
---

cdesc(<channel>)
cusers(<channel>)
cmsgs(<channel>)
cbuffer(<channel>)

Return the description (@channel/describe), number of users, number of messages in the recall buffer, or buffer size of <channel>, respectively. This information is also displayed in @channel/list and @channel/what.

**See also:** cbufferadd(), crecall(), channel functions
