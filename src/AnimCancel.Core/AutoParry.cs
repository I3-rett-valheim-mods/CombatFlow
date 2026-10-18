namespace AnimCancel.Core
{
    /// <summary>
    /// Combat / Auto perfect parry: for a blockable hit from the front on the local player, the game's own
    /// Character.RPC_Damage takes its block branch (Humanoid.BlockAttack) as a perfect block, even mid-attack
    /// and without the block button. Everything inside BlockAttack stays the game's code. Applies to every
    /// attacker, creatures and players alike.
    /// </summary>
    public static class AutoParry
    {
        /// <summary><see cref="Decide"/> result when the block is forced.</summary>
        public const string Force = "force";

        /// <summary>
        /// m_blockTimer set for the one RPC_Damage call: BlockAttack's perfect window is
        /// m_blockTimer != -1 &amp;&amp; m_blockTimer &lt; 0.25 (with m_timedBlockBonus &gt; 1).
        /// </summary>
        public const float PerfectBlockTimer = 0f;

        /// <summary>The option is on and the mod is enabled (mod off means vanilla).</summary>
        public static bool IsOn(CancelSettings s) => s.Enabled && s.AutoParry;

        /// <summary>Humanoid.BlockAttack rejects a hit when Vector3.Dot(hit.m_dir, forward) &gt; 0; kept as is.</summary>
        public static bool FromFront(float dotDirForward) => !(dotDirForward > 0f);

        public static bool ShouldForce(bool enabled, bool autoParry, bool isLocalPlayer, bool blockable,
            bool fromFront, bool hasBlocker, bool inDodge)
            => Decide(enabled, autoParry, isLocalPlayer, blockable, fromFront, hasBlocker, inDodge) == Force;

        /// <summary><see cref="Force"/>, or why the hit is left to vanilla (the log's result field).</summary>
        public static string Decide(bool enabled, bool autoParry, bool isLocalPlayer, bool blockable,
            bool fromFront, bool hasBlocker, bool inDodge)
        {
            if (!enabled || !autoParry) return "off";
            if (!isLocalPlayer) return "not_local";
            if (!blockable) return "not_blockable";
            if (!fromFront) return "behind";
            if (!hasBlocker) return "no_blocker";
            if (inDodge) return "dodge";
            return Force;
        }

        /// <summary>
        /// What a forced block did: guard_broken (out of stamina or staggered), perfect (the blocker has a timed
        /// block bonus above 1, so BlockAttack's perfect branch ran), else an ordinary block.
        /// </summary>
        public static string Outcome(float timedBlockBonus, bool guardBroken)
        {
            if (guardBroken) return "guard_broken";
            return timedBlockBonus > 1f ? "perfect" : "blocked";
        }
    }
}
