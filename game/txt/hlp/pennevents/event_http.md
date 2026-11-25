---
topics: [EVENT HTTP]
---

http`blocked (former descriptor, ip, method, path, reason)
```text
      Triggered when an HTTP request is sitelocked !connect, by IP or path, 'reason' will describe if it's IP or path.

```

http`fail (former descriptor, ip, reason)
```text
      Triggered when an HTTP connection fails for poor formatting, malformed requests, or similar parsing errors. This can occur before method, path, etc are obtained, so is limited in information.

```

http`command (IP, method, path, resp_code, resp_content_type, resp_content_len)
```text
      Triggered after an HTTP command is executed.

    A sitelock rule with deny_silent will not trigger http`blocked
```
