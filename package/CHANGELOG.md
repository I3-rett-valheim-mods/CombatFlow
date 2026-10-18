# Changelog

## 0.1.1

- Shorter Thunderstore page; full details stay in the README on GitHub. No gameplay change.

## 0.1.0

- First release.
- Heads-up: Max combo (auto) exploits how the game counts the current attack (the damage multiplier is
  carried into other swings, stamina is paid once) and Auto perfect parry forces the game's perfect-block
  path. Both are client-side and can change the balance on multiplayer servers: check the server's
  rules. Both can be switched off.
- Measured on the dummy (base damage per second): sword 1.5 to 1.6 vanilla, 2.5 to 2.7 with the cancel,
  7.6 with Max combo Loop; Berserker axes 2.57 vanilla, 3.2 to 3.4 with the cancel, about 6.4 with Loop
  once it runs. Loop weapon durability drops fast.
- Known limitations, not yet verified in game: Auto perfect parry against real creatures, Swap attack
  buttons, what other players see in multiplayer.
- Renamed from AnimCancel before the first release; the config file is now `I3_rett.CombatFlow.cfg`.
- **Sword.** Always cuts the recovery after each blow of the primary combo and after the
  secondary attack, right after the hit lands. Hold attack to chain.
- **Sword, Max combo (auto).** On by default: after the secondary attack lands,
  plays the second and third swings of the primary combo into it, then cancels.
  `Max combo mode`: `Loop` (default; keeps alternating the second and third
  swings while the secondary button is held; only the first secondary costs stamina) or `Cycle`.
  `Max combo loop cap` (default 0 = no cap; 1 to 1000 to limit) sets how many extra swings Loop chains
  before it cancels.
  Honours Delay after hit.
- **Sword, Swap attack buttons.** Off by default: with a sword, the primary and
  secondary attack buttons trade places, so holding the primary button runs the Max combo
  loop. Other weapons, tools and build mode are untouched; off whenever the mod is off.
- **Berserker axes.** Cuts the recovery after the combo blows and after Cleave. A blow is
  only cut when that is faster than the game's own chain point.
- **Berserker axes, Max combo (auto).** On by default: after the combo's last blow lands,
  plays the third and fourth blows into it, so they keep its bonus damage and cost no extra
  stamina, then cancels. Uses the sword's `Max combo mode` and `Max combo loop cap`; `Loop`
  keeps going while the primary button is held.
- **Dodge roll.** Ends the roll as soon as its invincibility window is over.
- **Auto perfect parry.** Off by default (Combat section): a blockable hit from the front is
  parried as a perfect block, even mid-attack and without the block button. Blocks with the shield,
  weapon or bare hands, as the game does; hits from behind and during a dodge are unchanged; the game's own block rules apply.
  Applies to all attackers, creatures and players.
- **Combo lab log.** Advanced option, off by default: a measurement log of inputs, attack
  starts, animator states and hits, for bug reports. Needs Debug log on as well.
- **F8 toggle** with an on-screen message, and a master switch in the config.
- Configuration through BepInEx ConfigurationManager, including the emote used under the
  hood, a delay after the hit, and a debug log for measuring damage output. With Debug log
  off (the default) the mod only logs its startup line and errors.
- No per-attack on/off settings: sword primary and secondary, Berserker axes combo blows and Cleave
  are always cancelled (axes blows only when that beats the chain point). Old keys left in an
  existing config file (`Cancel primary combo`, `Cancel secondary`, `Combo blows to cancel`,
  `Cancel secondary (Cleave)`) are simply ignored.
- Client-side only: the cancel touches your own animator and never sends an emote.
