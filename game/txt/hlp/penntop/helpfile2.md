---
topics: [helpfile2, help query]
---

Displays help entries that match <pattern> using an advanced full text search. If the /brief switch is given, only shows the names of matching entries. Without it, also shows a fragment of the entry with matches underlined.

The pattern is one or more phrases, where each phrase is one or more words (enclosed with quote marks if there's more than one word, or searching for a word with characters like @ or +). A * at the end treats the phrase as a prefix. Phrases can be combined with AND (The default), OR and NOT. The function NEAR(phrase phrase...) matches entries where two or more phrases are near each other. An optional second argument controls how close the matching is. All these operators must appear in upper case. For full details, see https://www.sqlite.org/fts5.html#full_text_query_syntax

Examples:

help/query cosine OR tangent
help/query "treated as"
help/query NEAR(player wizard, 20)
help/query "@attrib"*
