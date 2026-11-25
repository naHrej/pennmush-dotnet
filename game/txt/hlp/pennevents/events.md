---
topics: [EVENTS, EVENT]
---

PennMUSH Events are hardcoded events that may or may not be caused by players. The Event system lets administrators designate an object as an event handler (using the "event_handler" config option). The
event_handler object will then have attributes triggered, with arguments, on specified events.

To use the PennMUSH Event System:

> @create Event Handler
> @config/set event_handler=[num(Event Handler)]
> &<event name> Event Handler=<action list>

You will very likely want to set the event_handler option in your mush.cnf file to ensure it survives over dumps and is actively receiving events even during startup.

The enactor of an event is either:
```text
    1) The executor that caused it, or
    2) #-1 for system events without an executor.

```

For a list of events and their arguments, see 'help event list'.

For some examples of using events, see 'help event examples'.
