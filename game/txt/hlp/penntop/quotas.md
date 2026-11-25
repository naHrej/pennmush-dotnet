---
topics: [QUOTAS]
---

The Quota system controls how many objects a player may own. It is only used of the 'use_quota' @config option is set to Yes.

Each object created normally costs one quota (though this can be altered with the 'quota_cost' @config option), and every player starts with a fixed amount of quota, controlled with the 'starting_quota' @config option. Whenever you create an object of any type, your remaining quota, stored in the RQUOTA attribute, is decreased by the quota_cost.

Wizards can give specific players (for example, builder characters) unlimited quota by giving them the No_Quota @power.

You can view your current quota with the @quota command. Wizards can adjust a specific player's quota qith the @squota command, and God can see or alter all players' quotas with @allquota.

**See also:** @quota, quota(), Quotas Power, No_Quota Power
