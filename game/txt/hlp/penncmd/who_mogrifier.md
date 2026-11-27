---
topics: [who_mogrifier, WHOHEADER, WHOROW, WHOFOOTER, WHOAHEADER, WHOAROW, WHOAFOOTER]
---

# who_mogrifier

`who_mogrifier <dbref>` (configured in `mush.cnf` or via `@config/set who_mogrifier=<dbref>`, default `#-1`)

Specifies an object whose attributes replace the lines of `who`. `session` keeps its built-in columns.

## Mogrifier Attributes

When `who_mogrifier` is a valid object, `who` evaluates these attributes on that object (and its parents). `%!` is the mogrifier. `%#` is the player who typed `who`.

Players use `WHOHEADER`, `WHOROW`, and `WHOFOOTER`. Royalty and players with `See_All` use `WHOAHEADER`, `WHOAROW`, and `WHOAFOOTER`.

A missing attribute keeps the built-in line for that part. An empty result sends nothing for that part. A result may contain more than one line.

`WHOROW` / `WHOAROW` run once per connection, in descriptor order. A second connection from the same player is its own row. `who <name>` still limits which rows run. The footer count is every visible connected player, including rows the name filter omitted. Hidden connections, and players who are still connecting, appear only in the admin view.

## Header

| Register | Players | Royalty / See_All |
|---|---|---|
| `%0` | Poll text | Poll text |
| `%1` | Visible connected count | Connected count, including hidden |
| `%2` | | Connection count, including players still connecting |

## Row

| Register | Players (`WHOROW`) | Royalty / See_All (`WHOAROW`) |
|---|---|---|
| `%0` | Name | Name, or `Connecting...` |
| `%1` | `#dbref` | `#dbref`, or `#-1` |
| `%2` | On for | Location `#dbref`, or `#-1` |
| `%3` | Idle | On for |
| `%4` | Doing | Idle |
| `%5` | `D`, `H`, or empty | Command count |
| `%6` | On-for seconds | Descriptor |
| `%7` | Idle seconds | Host |
| `%8` | | `Dark`, `Hide`, `Connecting`, or empty |
| `%9` | | On-for seconds |

## Footer

| Register | Value |
|---|---|
| `%0` | The same count the header received |
| `%1` | The built-in sentence (`There are no players connected.`, `There is one player connected.`, or `There are N players connected.`) |

## Example

```text
> @create WhoMogrifier
> @config/set who_mogrifier=WhoMogrifier
> &WHOHEADER WhoMogrifier=Players online: %1
> &WHOROW WhoMogrifier=%0 (%4)
> &WHOFOOTER WhoMogrifier=%1
```

**See also:** who, session, @poll, doing, @config
