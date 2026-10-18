# Changelog

## 0.1.0

- First release.
- **Sword.** Cuts the recovery after each blow of the primary combo and after the
  secondary attack, right after the hit lands. Hold attack to chain.
- **Berserker axes.** Cuts the recovery after the combo blows (the last one, or the
  middle ones as well) and, optionally, after Cleave. A blow is only cut when that is
  faster than the game's own chain point.
- **Dodge roll.** Ends the roll as soon as its invincibility window is over.
- **F8 toggle** with an on-screen message, and a master switch in the config.
- Configuration through BepInEx ConfigurationManager, including the emote used under the
  hood, a delay after the hit, and a debug log for measuring damage output.
- Client-side only: the cancel touches your own animator and never sends an emote.
