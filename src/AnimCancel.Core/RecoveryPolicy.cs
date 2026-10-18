namespace AnimCancel.Core
{
    /// <summary>
    /// After a cancel the player sits in the emote. When no attack button is held (no
    /// relaunch coming, or a dodge cancel), the emote is stopped so the pose does not linger.
    /// </summary>
    public static class RecoveryPolicy
    {
        public static bool ShouldStop(bool inFlight, bool currentTagIsEmote, bool inTransition, bool attackHeld)
            => inFlight && currentTagIsEmote && !inTransition && !attackHeld;
    }
}
