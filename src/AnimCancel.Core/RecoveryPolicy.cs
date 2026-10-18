namespace AnimCancel.Core
{
    public enum RecoveryAction
    {
        /// <summary>Keep waiting.</summary>
        None,

        /// <summary>Send the local emote_stop and end the in-flight state.</summary>
        Stop,

        /// <summary>End the in-flight state without emote_stop (not in our emote).</summary>
        Expire,
    }

    public readonly struct RecoveryDecision
    {
        public RecoveryAction Action { get; }

        /// <summary>released | held_timeout | expired, empty for None.</summary>
        public string Reason { get; }

        public RecoveryDecision(RecoveryAction action, string reason)
        {
            Action = action;
            Reason = reason;
        }
    }

    /// <summary>
    /// After a cancel the player sits in the emote. When no attack button is held (no
    /// relaunch coming, or a dodge cancel), the emote is stopped so the pose does not linger.
    /// A held button normally relaunches as the blend starts, which ends the in-flight state;
    /// still in flight once fully in the emote means the relaunch failed (no stamina,
    /// encumbered), so the emote is stopped anyway after <see cref="HeldGrace"/>. Whatever
    /// happens, the in-flight state never outlives <see cref="Expiry"/>, or a later real emote
    /// would be cut locally.
    /// </summary>
    public static class RecoveryPolicy
    {
        /// <summary>Seconds fully in the emote, button held, before the failed relaunch is given up.</summary>
        public const float HeldGrace = 0.1f;

        /// <summary>Seconds after the cancel at which the in-flight state is dropped.</summary>
        public const float Expiry = 1.5f;

        private static readonly RecoveryDecision Wait = new(RecoveryAction.None, "");

        /// <param name="fullyInEmoteFor">Seconds layer 0 has been fully in the emote (current tag emote, no transition) while in flight.</param>
        /// <param name="sinceCancel">Seconds since the cancel.</param>
        public static RecoveryDecision Decide(
            bool inFlight, bool currentTagIsEmote, bool inTransition, bool attackHeld,
            float fullyInEmoteFor, float sinceCancel)
        {
            if (!inFlight) return Wait;

            bool fullyInEmote = currentTagIsEmote && !inTransition;
            if (fullyInEmote && !attackHeld) return new RecoveryDecision(RecoveryAction.Stop, "released");
            if (fullyInEmote && fullyInEmoteFor > HeldGrace) return new RecoveryDecision(RecoveryAction.Stop, "held_timeout");
            if (sinceCancel > Expiry)
                return new RecoveryDecision(fullyInEmote ? RecoveryAction.Stop : RecoveryAction.Expire, "expired");
            return Wait;
        }
    }
}
