# Changelog

## 0.1.0

- First release.
- **Sword.** Cuts the recovery after each blow of the primary combo and after the
  secondary attack, right after the hit lands. Hold attack to chain.
- **Sword, Max combo (auto).** Optional, off by default: after the secondary attack lands,
  plays the second and third swings of the primary combo into it, then cancels.
  `Max combo mode`: `Cycle` (default) or `Loop` (keeps alternating the second and third
  swings while the secondary button is held; only the first secondary costs stamina).
  `Max combo loop cap` (default 40, 0 for no cap) sets how many extra swings Loop chains
  before it cancels.
  Works with Cancel secondary off and honours Delay after hit.
- **Berserker axes.** Cuts the recovery after the combo blows (the last one, or the
  middle ones as well) and, optionally, after Cleave. A blow is only cut when that is
  faster than the game's own chain point.
- **Dodge roll.** Ends the roll as soon as its invincibility window is over.
- **Combo lab log.** Advanced option, off by default: a measurement log of inputs, attack
  starts, animator states and hits, for bug reports. Needs Debug log on as well.
- **F8 toggle** with an on-screen message, and a master switch in the config.
- Configuration through BepInEx ConfigurationManager, including the emote used under the
  hood, a delay after the hit, and a debug log for measuring damage output. With Debug log
  off (the default) the mod only logs its startup line and errors.
- Client-side only: the cancel touches your own animator and never sends an emote.
