---
topics: [mogrify3]
---

There are mogrify attributes for each individual piece of the channel message. If any evaluate to a non-empty string, they replace that portion of the chat message. Each mogrify attribtute is passed eight arguments:

%0 - (Depends on the mogrifier.)
%1 - Channel name (unmogrified).
%2 - Chat type (", :, ;, |, @). See 'help @chatformat' for descriptions.
%3 - Message.
%4 - Player chan title*
%5 - Player name*
%6 - The "says" part of a message.
%7 - A space-separated list of extra options. Currently can contain: "silent" if a silent @cemit caused this message, otherwise "noisy"

The mogrify attributes available are:
MOGRIFY`CHANNAME: %0 = default channel display. (e.g: "<Public>").
MOGRIFY`TITLE: %0 = The player's title, if any.
MOGRIFY`PLAYERNAME: %0 = The player's name.
MOGRIFY`SPEECHTEXT: %0 = "says"
MOGRIFY`MESSAGE: %0 = <the content of the say, pose, semipose or emit>

* These fields respect the "notitles" and "nonames" channel privs. If both title and name would be empty, the name will default to "Someone".

See 'help mogrify4' for examples.
