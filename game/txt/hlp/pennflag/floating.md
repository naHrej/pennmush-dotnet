---
topics: [FLOATING]
---

Flag:  FLOATING (rooms)

If a room is set floating, you will not be notified every 10 minutes or so that you have a disconnected room. A disconnected room is one that can't be reached from room #0, or (if the exits_connect_rooms @config option is on) one which has no exits.

If you don't plan to have your main grid accessible from Room #0, it's better to enable exits_connect_rooms than to set all your rooms FLOATING.
