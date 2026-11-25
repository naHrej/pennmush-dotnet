---
topics: [KEEPALIVE]
---

Flag:  KEEPALIVE  (players)

When this flag is set on a player with a telnet-capable connection, a telnet NOP (no-operation) is sent after there's been no activity on the connection for a minute, to generate socket activity without generating any output. In a way, it's the opposite of the IDLE command. IDLE is sent by clients to keep a connection open, while KEEPALIVE tells the server to send a message. Both are intended for use by people going through home router/NAT appliances with short inactivity timeouts.

**See also:** IDLE, terminfo()
