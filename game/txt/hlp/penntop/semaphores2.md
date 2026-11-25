---
topics: [SEMAPHORES2]
---

An object is notified using the "@notify" command. When you type "@wait <object>=<action>", you are adding one to the SEMAPHORE attribute on the object. When you type "@notify <object>", you are decreasing the SEMAPHORE attribute on the object by one. Whenever the attribute decreases, one of the actions waiting on the object takes place. The actions occur in the order they were added.

You can make the semaphore attribute of an object negative by @notify-ing it more times than things have been @wait-ed on it. If you do so, anything @wait-ed on the object will add one to the SEMAPHORE attribute and the action will take place immediately. You can also make all the actions waiting on an object take place right away by using "@notify/all", or wipe all the commands out and clear the SEMAPHORE attribute by using "@drain". Please note that all SEMAPHORE attributes are cleared out whenever the MUSH is restarted.

Semaphores can be used to make sure that events occur in the right order, or to make sure that two players can't use the same object at the same time.

Continued in 'help semaphores3'.
