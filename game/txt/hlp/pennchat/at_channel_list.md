---
topics: ["@channel list", channel-list]
---

Here's the legend for reading the @channel/list output:

```text
Channel Name               Num Users Num Msgs  Access Locks     Status   Buf
Sample                             1        0 [DPTWQHo jsmvh*] [On  QHC]   4
                                               ||||||| ||||||   |   |||    |
Channel is DISABLED----------------------------/|||||| ||||||   |   |||    |
Channel allows PLAYERS--------------------------/||||| ||||||   |   |||    |
Channel allows THINGS----------------------------/|||| ||||||   |   |||    |
Channel is Wizard-only (W) or Admin-only (A)------/||| ||||||   |   |||    |
Channel is QUIET-----------------------------------/|| ||||||   |   |||    |
Channel is HIDE_OK----------------------------------/| ||||||   |   |||    |
Channel is OPEN (non-members can speak on it)--------/ ||||||   |   |||    |
Channel has @clock/join set----------------------------||||||   |   |||    |
Channel has @clock/speak set----------------------------/||||   |   |||    |
Channel has @clock/mod set-------------------------------/|||   |   |||    |
Channel has @clock/see set--------------------------------/||   |   |||    |
Channel has @clock/hide set--------------------------------/|   |   |||    |
Player is the owner of the channel--------------------------/   |   |||    |
Player is currently on/off/gagging the channel------------------/   |||    |
If on, player has the channel muted---------------------------------/||    |
If on, player is hiding on the channel-------------------------------/|    |
If on, player has @channel/combined the channel-----------------------/    |
```

Size of the channel buffer in full-length lines----------------------------/
