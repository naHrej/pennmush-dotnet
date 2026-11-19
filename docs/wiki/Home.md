# PennMUSH .NET Documentation Wiki

Welcome to the **PennMUSH .NET** API & operator documentation, extracted directly from the `.NET 10` (`net10.0`) codebase. Rather than copying flat legacy `.hlp` files, these pages document the exact runtime behavior, signatures, switches, permissions, error codes, and modern extensions implemented in `PennMUSH.Core`, `PennMUSH.Extensions`, and `PennMUSH.Server`.

## Codebase Snapshot

| Surface | Verified Count | Source Location |
|---|---|---|
| Built-in MUSHcode Functions | **567** distinct registrations (`40` `noParse`, `29` `sideEffect`) + **18** `alias.cnf` aliases (**578** callable under default config) | `PennMUSH.Core/MUSHcode/Functions/*.cs` |
| Built-in Player Commands | **139** handler registrations covering **201** C# command names + **21** `alias.cnf` aliases (**204** under default config) + pre-login commands | `PennMUSH.Server/Engine/MUSHServer.cs` |
| Flags & Powers | **45** built-in flags (13 aliases), **31** built-in powers (4 aliases) | `PennMUSH.Core/Flags/FlagTable.cs` |
| Standard Attributes & Locks | **221** table entries (**213** canonical attrs + **8** short aliases), **13** lock types | `PennMUSH.Core/Attributes/`, `PennMUSH.Core/Locks/` |
| Color Palette | **927** X11 / xterm-256 / RGB24 color definitions | `game/txt/colors.json` |
| Error & Feedback Catalogs | **91** `MushError` templates, **701** `Feedback` templates | `MushError.cs`, `Feedback.cs` |
| Automated Test Suite | **469** tests across **95** test files | `PennMUSH.Core.Tests/` |

---

## Choose Your Path

### Builders & Players
* Learn how rooms, exits, multi-parent/zone hierarchies, and permissions work in [Architecture & Concepts](Architecture-and-Concepts).
* Reference flags, powers, standard attributes, and lock types in [Flags, Powers, Attributes & Locks](Flags-Powers-Attributes-and-Locks).
* Style output with the 927-color table, `@moniker`, and `@sockset` in [Colors, Markup & Presentation](Colors-Markup-and-Presentation).
* Browse command syntax in the [Commands Index](Commands-Index) (coming in PR 2).

### MUSHcode Developers
* Master evaluation order, bracket/brace escaping, `noParse` rules, `%`-substitutions, Q-registers, regex captures, and `MushError` codes in [MUSHcode Language Guide](MUSHcode-Language-Guide).
* Build interactive web panels, live code editors, and WebSocket JSON frames with [Colors, Markup & Presentation](Colors-Markup-and-Presentation) and [MUjs Protocol Reference](MUjs-Protocol-Reference).
* Check exact compatibility differences vs PennMUSH 1.8 in [PennMUSH 1.8 Parity & Differences](PennMUSH-1.8-Parity-and-Differences).
* Browse the softcode function reference in the [Functions Index](Functions-Index) (coming in PRs 3–4).

### Server Operators & Hosting
* Configure `game/mush.cnf`, `alias.cnf`, `restrict.cnf`, SQLite databases (`mush.db`, `connlog.db`, optional SQL DB), text caches, HTTP routes, hot-reboots, and CI/CD in [Server Configuration & Persistence](Server-Configuration-and-Persistence).

### C# Extension Authors
* Build zero-lock, hot-reloadable `.NET 10` plugins with custom commands, functions, tick services, and MUjs protocol handlers in [Plugin Development Guide](Plugin-Development-Guide).
