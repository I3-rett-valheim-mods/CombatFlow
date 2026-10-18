# AnimCancel

[![Thunderstore](https://img.shields.io/badge/Thunderstore-AnimCancel-2f4f8f)](https://thunderstore.io/c/valheim/p/I3_rett/AnimCancel/)
[![CI](https://github.com/I3-rett/AnimCancel/actions/workflows/ci.yml/badge.svg)](https://github.com/I3-rett/AnimCancel/actions/workflows/ci.yml)
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
- **Berserker axes:** cancels after the last hit of a blow, and only when that is faster
  than the game's own chain point. The last blow of the combo and, if enabled, Cleave are
  always eligible.
- **Dodge roll:** ends the roll as soon as its invincibility window is over. The roll
  covers a shorter distance.

Other weapons are not touched.

## How to use

Hold the attack button; the game chains the next blow as soon as the recovery is cut.
Tapping attack is not the goal: a tap during a cancel may be swallowed, as with the manual
emote cancel. Release attack and your character returns to normal movement, with no stuck
pose.

**F8** turns animation cancelling on or off in game and shows a short message.

## Configuration

Edit `BepInEx/config/I3_rett.AnimCancel.cfg`, or use BepInEx ConfigurationManager.

| Section | Key | Default | What it does |
|---|---|---|---|
| General | Enabled | true | Master switch. |
| General | Toggle key | F8 | Turns cancelling on or off in game. |
| Sword | Cancel primary combo | true | Cut the recovery after each primary blow. |
| Sword | Cancel secondary | true | Cut the recovery after the secondary attack. |
| Berserker axes | Combo blows to cancel | MiddleAndFinisher | `FinisherOnly` or `MiddleAndFinisher`. |
| Berserker axes | Cancel secondary (Cleave) | false | Cut the recovery after Cleave. |
| Dodge | Cancel after invincibility | true | End the roll once invincibility is over. |
| Advanced | Cancel emote | NoNoNo | The emote played under the hood, `NoNoNo` or `Cheer`. |
| Advanced | Delay after hit (s) | 0 | Seconds to wait after the hit before cancelling, 0 to 0.3. |
| Advanced | Debug log | false | Log the time of every damage event and cancel. |

## How much faster

In the author's model the gain is about x1.6-1.8 on the sword. Measured results may
differ. To measure it yourself, turn on **Debug log** and compare the timestamps of the
damage events with the mod on and off.

## Multiplayer

The cancel only touches your own character's animation on your own machine. The game never
sends an emote, so other players see your attacks but no emote. Animation speed is synced
by the game, so they may see your recoveries finish faster. Nothing is installed
server-side. The mod appears in the plugin list like any other.

## Compatibility

It patches these game methods, so other mods that patch the same ones may conflict:
`Humanoid.OnAttackTrigger`, `Humanoid.StartAttack`, `Player.OnDodgeMortal`,
`Player.SetControls` and `CharacterAnimEvent.Speed`.

## Reporting a bug

Set **Debug log** to true under Advanced, reproduce the problem, and attach
`BepInEx/LogOutput.log` to an issue at https://github.com/I3-rett/AnimCancel/issues.
Say which weapon was in your hands and whether you were holding or tapping attack.
