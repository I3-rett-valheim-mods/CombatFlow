# CombatFlow

[![Thunderstore](https://img.shields.io/badge/Thunderstore-CombatFlow-2f4f8f)](https://thunderstore.io/c/valheim/p/I3_rett/CombatFlow/)
[![CI](https://github.com/I3-rett/CombatFlow/actions/workflows/ci.yml/badge.svg)](https://github.com/I3-rett/CombatFlow/actions/workflows/ci.yml)
[![Licence](https://img.shields.io/badge/licence-MIT-blue)](LICENSE)
[![BepInEx](https://img.shields.io/badge/BepInEx-5.4.2351-lightgrey)](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)

Cuts the recovery of your sword and Berserker axe attacks, and of the dodge roll, right
after the hit lands, so you attack faster. It does what the emote animation-cancel does
(bind an emote and spam it while holding attack), but automatically and with perfect
timing. You just hold attack.

**Client-side only. The server does not need it, and it will not stop you joining a server
that does not have it.**

## What it does

- **Sword, primary combo:** cancels the recovery after each blow, right after the hit.
- **Sword, secondary:** cancels the recovery after the blow.
- **Sword, max combo (on by default, optional):** when the secondary attack lands, the
  mod plays the second and third swings of the primary combo straight after it, on your
  own animator, then cancels the recovery as usual. The game counts those swings as part of
  the secondary attack. Stamina is spent once, for the secondary. Only for weapons whose
  primary is the three-blow sword combo. `Max combo mode` picks what follows the third
  swing: `Loop` (default) keeps alternating the second and third swings
  while the secondary button is held, and cancels at the current swing's hit once it is
  released (or after `Max combo loop cap` extra swings, no cap by default). `Cycle` cancels after the third swing instead.
- **Berserker axes:** cancels after the last hit of a blow, and only when that is faster
  than the game's own chain point. The last blow of the combo and Cleave are
  always eligible.
- **Berserker axes, max combo (on by default, optional):** after the second hit of the
  combo's last blow, the mod plays the third and fourth blows again straight after it, on
  your own animator. The game counts them as part of the last blow, so they keep its bonus
  damage and cost no extra stamina. It shares `Max combo mode` and `Max combo loop cap` with
  the sword: `Loop` keeps alternating the third and fourth blows while the primary button is
  held and cancels at the end of the current blow once it is released; `Cycle` cancels after
  one extra third and fourth blow.
- **Dodge roll:** ends the roll as soon as its invincibility window is over. The roll
  covers a shorter distance.

Other weapons are not touched.

## How to use

Hold the attack button; the game chains the next blow as soon as the recovery is cut.
Tapping attack is not the goal: a tap during a cancel may be swallowed, as with the manual
emote cancel. Release attack and your character returns to normal movement, with no stuck
pose.

**F8** turns animation cancelling on or off in game and shows a short message.

**Swap attack buttons** (Sword section, off by default): with a sword, the primary button
starts the secondary attack, so holding it runs the Max combo loop, and the secondary button
plays the light combo. Only the two attack buttons change (keyboard, mouse and gamepad); other
weapons, tools and build mode keep the game's layout, and the swap is off whenever the mod is.

**Auto perfect parry** (Combat section, off by default): a blockable hit from the front is
parried as a perfect block, even mid-attack and without the block button. It blocks with what the
game blocks with (shield, weapon or bare hands); hits from behind and hits during a dodge are taken as usual. The rest is the
game's own block: stamina cost, guard break when out of stamina, attacker stagger, skill and
effects. It applies to every attacker, creatures and players alike, and is off whenever the mod is.
Only the block button and the mid-attack rule are skipped: as in vanilla, there is no block while
staggered, encumbered, in build mode or during a minor action.

## Configuration

Edit `BepInEx/config/I3_rett.CombatFlow.cfg`, or use BepInEx ConfigurationManager.

| Section | Key | Default | What it does |
|---|---|---|---|
| General | Enabled | true | Master switch. |
| General | Toggle key | F8 | Turns cancelling on or off in game. |
| Sword | Max combo (auto) | true | While the secondary attack plays, chain the second and third primary swings into it, then cancel or loop. The final cancel honours Delay after hit. |
| Sword | Max combo mode | Loop | Used with Max combo (auto) of the sword and of the Berserker axes. `Cycle`: cancel after the chained swings. `Loop`: keep alternating the chained swings while the button is held (secondary for the sword, primary for the axes; only the first attack costs stamina); releasing takes effect at the next hit. |
| Sword | Max combo loop cap | 0 | Loop mode only, both weapons: extra swings chained past the first pass before the run ends with a cancel, 0 to 1000. A still-held button then starts a new attack, which costs stamina again. 0 means no cap. |
| Sword | Swap attack buttons | false | With a sword only, swap the primary and secondary attack buttons (press and hold). Block, dodge, jump, movement, other weapons, tools and build mode are unchanged. Needs Enabled on. |
| Berserker axes | Max combo (auto) | true | After the combo's last blow, chain the third and fourth blows into it, then cancel or loop (Max combo mode and loop cap of the Sword section). |
| Dodge | Cancel after invincibility | true | End the roll once invincibility is over. |
| Combat | Auto perfect parry | false | Parry a blockable hit from the front as a perfect block, even mid-attack and without the block button. Blocks with the shield, weapon or bare hands, as the game does; hits from behind and during a dodge are unchanged. Applies to all attackers. Needs Enabled on. |
| Advanced | Cancel emote | NoNoNo | The emote played under the hood, `NoNoNo` or `Cheer`. |
| Advanced | Delay after hit (s) | 0 | Seconds to wait after the hit before cancelling, 0 to 0.3. |
| Advanced | Debug log | false | Log the time of every damage event and cancel. While it is off, the mod writes nothing to the log apart from its startup line and errors. |
| Advanced | Combo lab log | false | Detailed measurement log for bug reports. Needs Debug log on as well. |

## How much faster

Measured in game on the dev profile (sword, solo, hitting a target; base-damage units per second,
counting each swing's damage multiplier; factor against the vanilla secondary):

| Setup | Units/s | vs vanilla secondary |
|---|---|---|
| Vanilla combo | 1.51 | x0.92 |
| Vanilla secondary | 1.64 | x1.00 |
| Cancel, combo | 2.46 | x1.50 |
| Cancel, secondary | 2.68 | x1.63 |
| Max combo, Cycle | 5.01 | x3.05 |
| Max combo, Loop | 7.63 (peak 8.56) | x4.65 |

These figures are for the sword. Berserker axes max combo has not been measured yet.

Stamina: Cycle spends stamina on every relaunched secondary. Loop only charges the first
secondary (the chain swings are part of it), so stamina stays flat while you hold the button. After `Max combo loop cap` extra swings a
still-held button starts a new secondary, which costs stamina again.
To measure it yourself, turn on **Debug log**, switch the per-weapon cancel options off for a
vanilla run, and compare the timestamps of the damage events. **Combo lab log** (Advanced)
writes much more detail (it needs Debug log on as well), but it is a debugging aid for bug
reports, not something to leave on.

## Multiplayer

The cancel only touches your own character's animation on your own machine. The game never
sends an emote, so other players see your attacks but no emote. Animation speed is synced
by the game, so they may see your recoveries finish faster. Nothing is installed
server-side. The mod appears in the plugin list like any other.

## Compatibility

It patches these game methods, so other mods that patch the same ones may conflict:
`Humanoid.OnAttackTrigger`, `Humanoid.StartAttack`, `Humanoid.UnequipItem`, `Player.OnDodgeMortal`,
`Player.SetControls` and `CharacterAnimEvent.Speed`.

## Reporting a bug

Set **Debug log** to true under Advanced, reproduce the problem, and attach
`BepInEx/LogOutput.log` to an issue at https://github.com/I3-rett/CombatFlow/issues.
Say which weapon was in your hands and whether you were holding or tapping attack.
