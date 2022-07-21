# Monolit
A small project to create currently the best weapon and later full loadout for a given set of parameters for the game Escape From Tarkov. Makes use of the [RatStash library](https://github.com/RatScanner/RatStash). Currently working at a proof of concept level, aiming to have it as MVP by Q4 2022 by the latest.

## Current Features
- Sort mods/weapon by the best recoil or ergo stats at cheapest price.
- Filter options by broad trader LL (eg, 1, 2, 3, 4 for all).
- Filter weapon options by weapon category.
- Filter weapon options by penetration of availible ammo.
- Restrict options to loud, silenced or both options.

## Planned Features
- Make it into a website, heh.
  + Setup a CI/CD DevOps pipeline.
  + Accounts and saved builds for users.
  + Allow uses to "like" a build and track the popularity of these weapons and mods. (SQL?)
- Account for exclusion of incompatible items, choose the one which works best.
- Filter all options by merc level.
- Filter by individual trader LL.
- Include quest unlocks.
- Account for Flea Market prices/options.
- Filter mods by a maximum value per mod.
- Filter Weapons by ammo type.
- Set a maximum budget for a build.
- Advanced consideration of attributes, such as choosing the most efficent mods for a given result.

## Stretch/Additional Goals
- Integration with other platforms such as tarkov-tracker for level progression and display of only non-locked items.
- Transform the items JSON database into an SQL, so that users can make quick queries.
