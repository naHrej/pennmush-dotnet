---
topics: [MAILFROM(), MAILTIME(), MAILSTATUS(), MAILSUBJECT()]
---

mailfrom([<player>, ][<folder #>:]<mail message #>)
mailtime([<player>, ][<folder #>:]<mail message #>)
mailstatus([<player>, ][<folder #>:]<mail message #>)
mailsubject([<player>, ][<folder #>:]<mail message #>)

mailfrom() returns the dbref number of the sender of a mail message.
mailtime() is similar, but returns the time the mail was sent.
mailsubject() is similar, but returns the subject of the message.
mailstatus() returns the mail's status characters (as per @mail/list).

**See also:** mail(), maillist()
