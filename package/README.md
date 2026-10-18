# CombatFlow

Client-side combat helper for Valheim. Hold the attack button, CombatFlow does the timing.

- **Animation cancel** – cuts the recovery of sword, berserker-axe and dodge-roll animations right after the hit lands.
- **Max combo (on by default)** – sword: one secondary, then the light swings keep its ×3 damage. Berserker axes: the finisher's ×2 keeps going on the next swings. Hold the button to loop; stamina is paid once.
- **Auto perfect parry (off by default)** – blockable hits from the front become perfect parries, even mid-combo.
- **Swap attack buttons (off by default)** – with a sword, left and right click swap.
- **F8** toggles the mod. Everything is in the config (F1 with Configuration Manager).

**Heads-up**
- Max combo and auto parry change how the game counts attacks and blocks. Check your server's rules.
- Weapons wear out fast in a loop: repair often.
- Only your own client is modded; others see your attacks, not the trick.

Bugs: turn on *Advanced → Debug log* and attach `BepInEx/LogOutput.log` to an issue on [GitHub](https://github.com/I3-rett-valheim-mods/CombatFlow/issues). Full details (settings, measurements, compatibility): [README on GitHub](https://github.com/I3-rett-valheim-mods/CombatFlow#readme).
