namespace AnimCancel.Core
{
    /// <summary>
    /// While the Animator blends from the cancelled attack into the emote, layer 0's
    /// next-or-current state is the emote, so Player.InAttack() is false and the held button
    /// makes Player.UpdateAttack call Humanoid.StartAttack every fixed frame. Each call would
    /// succeed and replace the relaunched attack with a fresh level-0 one (the relaunch never
    /// started, so its m_nextAttackChainLevel is still 0), queueing extra attack triggers and
    /// breaking the chain. Once the first relaunch after a cancel was consumed (<c>active</c>),
    /// further StartAttack calls of the local player are refused until the relaunched attack has
    /// begun on layer 0 (<see cref="AttackBegun"/>) or <see cref="Window"/> seconds after the cancel.
    /// </summary>
    public static class RelaunchBlock
    {
        public const float Window = 0.5f;

        /// <summary>A relaunch consumed this long after the cancel still needs the block (a later one has nothing to block).</summary>
        public static bool ShouldStart(float dtSinceCancel) => dtSinceCancel >= 0f && dtSinceCancel < Window;

        /// <summary>
        /// The relaunched attack has begun on layer 0: its next-or-current state (the rule
        /// Player.InAttack() uses) is tagged "attack", but not the cancelled attack still current
        /// before the blend into the emote has started. During that blend the current state is
        /// still the cancelled attack and next is the emote, which InAttack() reads as "not in attack".
        /// The cancelled state is excluded only while it is the current state with no transition into
        /// an attack: once the blend leads into an attack (next), that attack is the relaunch, even
        /// when the sword secondary relaunches into the very state it was cancelled in.
        /// </summary>
        public static bool AttackBegun(bool inTransition, bool currentIsAttack, bool nextIsAttack, bool currentIsCancelled)
            => inTransition ? nextIsAttack : currentIsAttack && !currentIsCancelled;

        /// <summary>The block ends: the relaunched attack plays, or the window is over.</summary>
        public static bool ShouldLift(bool active, float sinceCancel, bool layer0Attack)
            => active && (layer0Attack || sinceCancel < 0f || sinceCancel >= Window);

        /// <summary>This StartAttack call is a duplicate of the relaunch and is refused.</summary>
        public static bool ShouldRefuse(bool isLocalPlayer, bool active, float sinceCancel, bool layer0Attack)
            => isLocalPlayer && active && !ShouldLift(active, sinceCancel, layer0Attack);
    }
}
