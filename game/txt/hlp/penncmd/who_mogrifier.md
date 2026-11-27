---
topics: [who_mogrifier, WHOHEADER, WHOROW, WHOFOOTER, WHOAHEADER, WHOAROW, WHOAFOOTER]
---

# who_mogrifier

who_mogrifier <dbref>

Set in mush.cnf, or with @config/set who_mogrifier=#dbref. Default is #-1.
The value is the object's dbref, not its name.

The object replaces the lines of who. session keeps its built-in columns.
HTML clients receive the whole listing as one block, so the header can open a table and the footer can close it.

When who_mogrifier is a valid object, who evaluates attributes on that object and its parents. %! is the mogrifier. %# is the player who typed who.

Players use WHOHEADER, WHOROW, and WHOFOOTER.
Royalty and See_All use WHOAHEADER, WHOAROW, and WHOAFOOTER.

A missing attribute keeps the built-in line for that part. An empty result sends nothing. A result may contain more than one line.

WHOROW and WHOAROW run once per connection, in descriptor order. A second connection from the same player is its own row. who <name> still limits which rows run. The footer count is every visible connected player, including rows the name filter omitted. Hidden connections, and players still connecting, appear only for Royalty and See_All.

WHOHEADER:
%0 - Poll text.
%1 - Visible connected count.

WHOAHEADER:
%0 - Poll text.
%1 - Connected count, including hidden.
%2 - Connection count, including players still connecting.

WHOROW:
%0 - Name.
%1 - #dbref.
%2 - On for.
%3 - Idle.
%4 - Doing.
%5 - D, H, or empty.
%6 - On-for seconds.
%7 - Idle seconds.

WHOAROW:
%0 - Name, or Connecting...
%1 - #dbref, or #-1.
%2 - Location #dbref, or #-1.
%3 - On for.
%4 - Idle.
%5 - Command count.
%6 - Descriptor.
%7 - Host.
%8 - Dark, Hide, Connecting, or empty.
%9 - On-for seconds.

WHOFOOTER and WHOAFOOTER:
%0 - The same count the header received.
%1 - There are no players connected. / There is one player connected. / There are N players connected.

Example:
@create WhoMogrifier
examine WhoMogrifier
@config/set who_mogrifier=#123
&WHOHEADER WhoMogrifier=Players online: %1
&WHOROW WhoMogrifier=%0 (%4)
&WHOFOOTER WhoMogrifier=%1

**See also:** who, session, @poll, doing, @config
