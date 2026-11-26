---
topics: [speech_mogrifier, MOGRIFYSAY, MOGRIFYPOSE, MOGRIFYSEMIPOSE, "@speechmogrifier"]
---

# speech_mogrifier

`speech_mogrifier <dbref>` (configured in `mush.cnf` or via `@config/set speech_mogrifier=<dbref>`, default `#-1`)

Specifies a global mogrifier object whose attributes rewrite the full formatted output of `say`, `pose`, and `semipose` (unlike `SPEECHMOD`, which only rewrites the spoken text before wrapping).

## Mogrifier Attributes

When `speech_mogrifier` is set to a valid object, the server checks for the first matching attribute on that object (and its parents):

- **For `say` (`"`)**: `MOGRIFYSAY`, then `MOGRIFY`
- **For `pose` (`:`)**: `MOGRIFYPOSE`, then `MOGRIFY`
- **For `semipose` (`;`)**: `MOGRIFYSEMIPOSE`, then `MOGRIFYPOSE`, then `MOGRIFY`

## Arguments Passed to the Attribute

The attribute is evaluated on the mogrifier object (`%!` = mogrifier, `%#` = speaker) with:

| Register | Value |
|---|---|
| `%0` | Speech token (`"`, `:`, or `;`) |
| `%1` | Speaker's name |
| `%2` | Spoken/posed text (after `SPEECHMOD`) |
| `%3` | Default room output line |
| `%4` | Default speaker output line |
| `%5` | Speaker's `#dbref` |
| `%6` | Speaker's location `#dbref` |

If the attribute evaluates to a non-empty string, that string is sent to both the speaker and the room. If it evaluates to an empty string, the output is suppressed.

## Example

```text
> @create SpeechMogrifier
> @config/set speech_mogrifier=SpeechMogrifier
> &MOGRIFYSAY SpeechMogrifier=[%1] %2
> &MOGRIFYPOSE SpeechMogrifier=* %1 %2
> &MOGRIFYSEMIPOSE SpeechMogrifier=* %1%2
```

**See also:** say, pose, semipose, SPEECHMOD, @config
