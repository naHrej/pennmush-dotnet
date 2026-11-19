# API: JSON & Crypto

JSON construction, inspection, transformation, Base64/URL/form encoding, cryptographic hashing, HMAC, and cipher functions extracted from `PennMUSH.Core/MUSHcode/Functions/JsonFunctions.cs` (**5** core functions) and `PennMUSH.Core/MUSHcode/Functions/CryptoFunctions.cs` (**10** core functions).

* For WebSocket JSON framing (`wsjson()`), see [Client & MUjs UI](API-Client-and-MUjs-UI).
* For TLS connection inspection (`ssl()`), see [Connections & ConnLog](API-Connections-and-ConnLog).
* For SQL string escaping (`sqlescape()`), see [SQL, HTTP & System](API-SQL-HTTP-and-System).

---

## JSON Functions (`JsonFunctions.cs`)

| Function | Signature | Notes |
|---|---|---|
| `isjson` | `isjson(<string>)` | Returns `1` if valid JSON, `0` if empty, whitespace, or malformed |
| `json` | `json(<type>[, <arg...>])` | Constructs JSON of `<type>`: `null`, `boolean`, `number`, `string`, `array`, `object` |
| `json_query` | `json_query(<json>[, <op>[, <path...>]])` | Queries `<json>` with `<op>` (`type`, `size`, `exists`, `get`, `unescape`; default `type`) |
| `json_mod` | `json_mod(<json>, <op>, <path>[, <value>])` | Mutates `<json>` at `<path>` with `<op>`: `set`, `insert`, `replace`, `remove` |
| `json_map` | `json_map(<[obj/]attr>, <json>[, <osep>[, <args...>]])` | Maps `<[obj/]attr>` across array elements, object entries, or a scalar |

### Construction (`json()`)

`json()` strips ANSI from `<type>` (defaulting an empty type to `string`):
- `json(null)` / `json(null, null)`: Returns `null`. Rejects > 2 args with `#-1 WRONG NUMBER OF ARGUMENTS` or non-`null` 2nd arg with `#-1 INVALID VALUE`.
- `json(boolean, <val>)`: Strips ANSI and maps `true`/`1` -> `true`, `false`/`0` -> `false`, otherwise `#-1 INVALID VALUE`.
- `json(number, <num>)`: Strips ANSI and validates `double` (`InvariantCulture`), returning `#-1 ARGUMENT MUST BE NUMBER` on failure.
- `json(string, <text>)`: Serializes `<text>` as a quoted JSON string without stripping ANSI from the payload.
- `json(array[, <elem1>, <elem2>...])`: Strips ANSI and parses every `<elem>` as a valid JSON value (`#-1 INVALID VALUE` if any element is not valid JSON).
- `json(object[, <k1>, <jsonVal1>...])`: Requires pairs of keys and valid JSON values (non-empty keys; odd total arg count else `#-1 WRONG NUMBER OF ARGUMENTS`; `#-1 INVALID VALUE` on empty key or invalid JSON value).
- Unknown `<type>` returns `#-1 INVALID TYPE`.

### Query & Mutation (`json_query()`, `json_mod()`)

Path navigation accepts either a single JSONPath-style dot path (`$.user.items.0`) or separate key/index arguments (`user`, `items`, `0`) in `json_query()`:
- `type`: Returns `null`, `object`, `array`, `boolean`, `number`, or `string`.
- `size`: Returns key count for objects, length for arrays, `0` for `null`, `1` for scalars.
- `exists`: Returns `1` or `0` whether `<path...>` resolves.
- `get`: Returns raw JSON text of the matched node (`"null"` if JSON null), or `#-1` (`MushError.Nothing`) if missing.
- `unescape`: Unescapes a JSON string root to plain text, or returns `#-1` (`MushError.Nothing`) if root is not a JSON string.
- Unknown `<op>` returns `#-1 UNKNOWN OPERATION`. Invalid `<json>` returns `#-1` (`MushError.Nothing`).

In `json_mod(<json>, <op>, <path>[, <value>])`, `<path>` is parsed as a single path argument (`$.a.b` or key); an empty path returns the serialized JSON unchanged, and missing parents return `#-1 NO MATCH`:
- `<value>` is parsed as JSON if valid, or falls back to a JSON string if plain text (trimmed empty string becomes `""`).
- `set`: Overwrites or adds an object property, sets `arr[idx]`, or appends when `idx == arr.Count` (`#-1 OUT OF RANGE` otherwise).
- `insert`: On objects, inserts only when the key does not already exist (returns original JSON unchanged if present); on arrays, same as `set`.
- `replace`: Requires the target object key or in-bounds array index to already exist (`#-1 NO MATCH` / `#-1 OUT OF RANGE`).
- `remove`: Deletes the target object property or array element.

### Iteration (`json_map()`)

`json_map()` evaluates `<[obj/]attr>` via `Ufun` and joins outputs with `<osep>` (default `" "`):
- `%0`: JSON type (`object`, `array`, `string`, `number`, `boolean`, `null`)
- `%1`: MUSH value (`string` unescaped, `boolean` as `1`/`0`, numbers and nested containers as JSON text)
- `%2`: Property key for objects, zero-based index (`0`, `1`, ...) for arrays, or empty string for scalars
- `%3+`: Optional trailing `<args...>` forwarded verbatim

```text
think [json(object, name, [json(string, Alice)], hp, [json(number, 42)])]
-> {"name":"Alice","hp":42}

think [json_query({"a":{"b":[10,20]}}, get, $.a.b.1)]
-> 20

think [json_mod({"hp":42}, set, $.status, ready)]
-> {"hp":42,"status":"ready"}
```

---

## Encoding & Cryptography (`CryptoFunctions.cs`)

| Function | Signature | Notes |
|---|---|---|
| `encode64` | `encode64(<string>)` | UTF-8 Base64 encode |
| `decode64` | `decode64(<base64>)` | Trims & UTF-8 Base64 decodes; invalid Base64 returns `#-1` |
| `urlencode` | `urlencode(<string>)` | Percent-encodes `<string>` (`Uri.EscapeDataString`) |
| `urldecode` | `urldecode(<string>)` | Percent-decodes `<string>`; control characters become `?` |
| `formdecode` | `formdecode(<query>[, <key>[, <sep>]])` | Lists keys or extracts values for `<key>` from `k=v&...` |
| `digest` | `digest(<algo>, <string>)` / `digest(list)` | Lowercase hex digest (`md5`, `sha1`, `sha0`, `sha`, `sha256`, `sha384`, `sha512`) |
| `hmac` | `hmac(<algo>, <key>, <text>[, <encoding>])` | Keyed HMAC (`md5`, `sha1`, `sha256`, `sha384`, `sha512`) as `hex` (default) or `base64` |
| `sha0` | `sha0(<string>)` | Computes lowercase hex SHA-1 digest of `<string>` |
| `encrypt` | `encrypt(<text>, <key>[, <base64?>])` | Printable-ASCII (`32..126`) keyed shift cipher with optional Base64 wrap |
| `decrypt` | `decrypt(<text>, <key>[, <base64?>])` | Reverses `encrypt()` (with optional Base64 decode first) |

### Encoding Details
- **`formdecode(<query>[, <key>[, <sep>]])`**: Splits on `&` (`RemoveEmptyEntries`) and percent-decodes keys and values (`Uri.UnescapeDataString` — does not convert `+` to space), replacing control characters with `?`. Without `<key>`, returns space-separated decoded keys; with `<key>` (exact ordinal match), returns matching values joined by `<sep>` (default `" "`).
- **`digest()` vs `sha0()` vs `hmac()`**:
  - `digest(list)` returns `"md5 sha1 sha256 sha384 sha512"`. Passing `sha0` or `sha` to `digest()` uses SHA-1; unsupported algorithms return `#-1 UNSUPPORTED DIGEST TYPE`. Missing data argument returns `#-1 TOO FEW ARGUMENTS`.
  - `sha0(<string>)` always uses `SHA1.HashData` and returns lowercase hex (`""` when called with no arguments).
  - `hmac()` requires at least 3 arguments (`#-1 TOO FEW ARGUMENTS`), supports `md5`, `sha1`, `sha256`, `sha384`, `sha512` (returning `#-1` on unknown algorithm), and encodes as Base64 when `<encoding>` is `"base64"` (case-insensitive) or lowercase hex otherwise.
- **`encrypt()` / `decrypt()`**:
  - Characters in printable ASCII (`32`–`126`) are shifted modulo `95` by the repeating `<key>` character offset (`k - 32`, or `k % 95` if outside printable ASCII). Characters outside `32..126` pass through unchanged; an empty `<key>` returns `<text>` unmodified.
  - When truthy 3rd argument (`IsTruthy`) is supplied, `encrypt()` Base64-encodes the cipher text and `decrypt()` trims and Base64-decodes before decrypting (`#-1` on invalid Base64).

```text
think [encode64(hello)]
-> aGVsbG8=

think [digest(sha256, abc)]
-> ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad

think [hmac(sha256, secret, payload, base64)]
```

---

See also: [Client & MUjs UI](API-Client-and-MUjs-UI) (`wsjson`) · [Connections & ConnLog](API-Connections-and-ConnLog) (`ssl`) · [SQL, HTTP & System](API-SQL-HTTP-and-System) (`sqlescape`) · [Functions Index](Functions-Index) · [Home](Home)
