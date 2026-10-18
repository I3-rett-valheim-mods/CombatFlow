using CombatFlow.Core;
using Xunit;

namespace CombatFlow.Tests
{
    public class RecoveryPolicyTests
    {
        static RecoveryDecision D(bool inFlight, bool emote, bool transition, bool held, float inEmoteFor = 0f, float sinceCancel = 0.3f)
            => RecoveryPolicy.Decide(inFlight, emote, transition, held, inEmoteFor, sinceCancel);

        static void Check(RecoveryDecision d, RecoveryAction action, string reason)
        {
            Assert.Equal(action, d.Action);
            Assert.Equal(reason, d.Reason);
        }

        [Fact]
        public void Recovery_requests_emote_stop_when_hold_released()
            => Check(D(inFlight: true, emote: true, transition: false, held: false), RecoveryAction.Stop, "released");

        [Fact]
        public void Not_in_flight_never_stops()
        {
            Check(D(false, true, false, false), RecoveryAction.None, "");
            Check(D(false, true, false, true, inEmoteFor: 5f, sinceCancel: 10f), RecoveryAction.None, "");
        }

        [Fact]
        public void Still_blending_into_emote_waits()
            => Check(D(true, true, true, false), RecoveryAction.None, "");

        [Fact]
        public void Not_yet_in_emote_waits()
            => Check(D(true, false, false, false), RecoveryAction.None, "");

        [Fact]
        public void Hold_still_down_lets_the_relaunch_happen()
            => Check(D(true, true, false, true, inEmoteFor: 0.05f), RecoveryAction.None, "");

        [Fact]
        public void Held_but_no_relaunch_stops_after_a_tenth_of_a_second()
        {
            // A real relaunch would have cleared InFlight; still set while fully in the emote = it failed.
            Check(D(true, true, false, true, inEmoteFor: RecoveryPolicy.HeldGrace), RecoveryAction.None, "");
            Check(D(true, true, false, true, inEmoteFor: 0.11f), RecoveryAction.Stop, "held_timeout");
        }

        [Fact]
        public void In_flight_expires_after_one_and_a_half_seconds()
        {
            Check(D(true, false, false, true, sinceCancel: RecoveryPolicy.Expiry), RecoveryAction.None, "");
            // Not in our emote: just forget, no emote_stop (it would cut the next real emote).
            Check(D(true, false, false, true, sinceCancel: 1.6f), RecoveryAction.Expire, "expired");
            Check(D(true, true, true, false, sinceCancel: 1.6f), RecoveryAction.Expire, "expired");
            // Fully in our emote at expiry: stop it.
            Check(D(true, true, false, true, inEmoteFor: 0.05f, sinceCancel: 1.6f), RecoveryAction.Stop, "expired");
        }

        [Fact]
        public void Emote_trigger_spent_once_emote_is_current_or_next()
        {
            Assert.True(RecoveryPolicy.EmoteTriggerSpent(currentTagIsEmote: true, nextTagIsEmote: false));
            Assert.True(RecoveryPolicy.EmoteTriggerSpent(currentTagIsEmote: false, nextTagIsEmote: true));
            Assert.False(RecoveryPolicy.EmoteTriggerSpent(false, false));
        }

        [Fact]
        public void Disable_mid_cancel_stops_the_emote_only_when_in_or_entering_it()
        {
            Assert.True(RecoveryPolicy.StopOnDisable(inFlight: true, currentTagIsEmote: false, nextTagIsEmote: true));
            Assert.True(RecoveryPolicy.StopOnDisable(inFlight: true, currentTagIsEmote: true, nextTagIsEmote: false));
            Assert.False(RecoveryPolicy.StopOnDisable(inFlight: true, currentTagIsEmote: false, nextTagIsEmote: false));
            Assert.False(RecoveryPolicy.StopOnDisable(inFlight: false, currentTagIsEmote: true, nextTagIsEmote: false));
        }
    }
}
