namespace AnimCancel.Core
{
    /// <summary>
    /// What a max combo run carries and chains, per weapon. The run keeps <see cref="Kind"/>'s Attack current
    /// (no StartAttack) and sets <see cref="Swing1Trigger"/> / <see cref="Swing2Trigger"/> on the local Animator
    /// at the last hit of <see cref="StartClip"/>, <see cref="Swing1Clip"/> and <see cref="Swing2Clip"/>, so
    /// every chained hit lands at that Attack's multiplier.
    /// </summary>
    public sealed class MaxComboProfile
    {
        /// <summary>Log value of the weapon key (sword|axes).</summary>
        public string Weapon { get; }

        /// <summary>The kind of the Attack the run carries.</summary>
        public AttackKind Kind { get; }

        /// <summary>Clip whose last hit starts the run (the carried Attack's own clip).</summary>
        public string StartClip { get; }

        public string Swing1Clip { get; }
        public string Swing2Clip { get; }

        /// <summary>Damage events per play of each clip; only the last one advances the run.</summary>
        public int StartHits { get; }

        public int Swing1Hits { get; }
        public int Swing2Hits { get; }

        public string Swing1Trigger { get; }
        public string Swing2Trigger { get; }

        /// <summary>Reset before a step's trigger is set, so no stale chain trigger cuts in.</summary>
        public string[] SwingTriggers { get; }

        /// <summary>Reset on abort.</summary>
        public string[] AbortTriggers { get; }

        /// <summary>Loop mode keeps going while the primary (true) or the secondary (false) button is held.</summary>
        public bool HoldPrimary { get; }

        private readonly bool _axes;

        private MaxComboProfile(string weapon, AttackKind kind, string startClip, int startHits, string swing1Clip,
            int swing1Hits, string swing2Clip, int swing2Hits, string swing1Trigger, string swing2Trigger,
            string[] swingTriggers, bool holdPrimary, bool axes)
        {
            Weapon = weapon;
            Kind = kind;
            StartClip = startClip;
            StartHits = startHits;
            Swing1Clip = swing1Clip;
            Swing1Hits = swing1Hits;
            Swing2Clip = swing2Clip;
            Swing2Hits = swing2Hits;
            Swing1Trigger = swing1Trigger;
            Swing2Trigger = swing2Trigger;
            SwingTriggers = swingTriggers;
            AbortTriggers = new[] { swing1Trigger, swing2Trigger };
            HoldPrimary = holdPrimary;
            _axes = axes;
        }

        /// <summary>
        /// Sword: carries the secondary (×3); Sword-Attack-R4's hit sets swing_longsword1, swing_sword 1
        /// (Attack2) swing_longsword2, swing_sword 2 (Attack3) cancels or loops. One hit per clip.
        /// </summary>
        public static MaxComboProfile Sword { get; } = new(
            "sword", AttackKind.SwordSecondary,
            MaxComboPolicy.SecondaryClip, 1, MaxComboPolicy.Swing1Clip, 1, MaxComboPolicy.Swing2Clip, 1,
            MaxComboPolicy.Swing1Trigger, MaxComboPolicy.Swing2Trigger, MaxComboPolicy.SwingTriggers,
            holdPrimary: false, axes: false);

        /// <summary>
        /// Berserker axes: carries the combo finisher (dualaxes level 3 of 4, ×2). The finisher clip
        /// (DualAxes 3, DualAxes_Attack_4) sets dualaxes2 at its second hit, DualAxes 2 (DualAxes_Attack_3_2)
        /// dualaxes3 at its second hit, DualAxes 3 cancels or loops at its second hit. Two hits per clip.
        /// </summary>
        public static MaxComboProfile Axes { get; } = new(
            "axes", AttackKind.AxesCombo,
            AxesFinisherClip, 2, AxesSwing1Clip, 2, AxesFinisherClip, 2,
            "dualaxes2", "dualaxes3", new[] { "dualaxes0", "dualaxes1", "dualaxes2", "dualaxes3" },
            holdPrimary: true, axes: true);

        public const string AxesPrimaryAnimation = "dualaxes";
        public const string AxesSecondaryAnimation = "dualaxes_secondary";
        public const int AxesMinChainLevels = 4;
        public const string AxesFinisherClip = "DualAxes_Attack_4";
        public const string AxesSwing1Clip = "DualAxes_Attack_3_2";

        /// <summary>The profile whose carried Attack is of this kind, or null.</summary>
        public static MaxComboProfile? For(AttackKind kind) => kind switch
        {
            AttackKind.SwordSecondary => Sword,
            AttackKind.AxesCombo => Axes,
            _ => null,
        };

        /// <summary>
        /// The fired clip is <paramref name="expected"/>. Spaces and underscores count as the same character: the
        /// review log writes clip names with spaces as underscores (DebugLine.Token), and the axes clip names were
        /// taken from the log (in-game pass axes2: the run never started on the raw names).
        /// </summary>
        public static bool SameClip(string? fired, string expected)
            => !string.IsNullOrEmpty(fired)
               && fired!.Length == expected.Length
               && string.Equals(fired.Replace(' ', '_'), expected.Replace(' ', '_'), System.StringComparison.Ordinal);

        /// <summary>This weapon's Max combo (auto) switch (Enabled is checked separately).</summary>
        public bool IsOn(CancelSettings s) => _axes ? s.AxesMaxCombo : s.MaxCombo;

        public string? TriggerFor(MaxComboActionKind k) => k switch
        {
            MaxComboActionKind.TriggerSwing1 => Swing1Trigger,
            MaxComboActionKind.TriggerSwing2 => Swing2Trigger,
            _ => null,
        };

        /// <summary>
        /// The current Attack can carry a run. Sword: it is the secondary and the weapon's primary is the 3-level
        /// longsword combo. Axes: it is the finisher (last level) of a weapon whose primary is the 4-level dualaxes
        /// combo and whose secondary is dualaxes_secondary.
        /// </summary>
        internal bool Eligible(string? attackAnimation, int attackLevel, int attackLevels, string? primaryAnimation,
            int primaryLevels, string? secondaryAnimation)
        {
            if (!_axes)
            {
                return MaxComboPolicy.Eligible(attackAnimation, primaryAnimation, primaryLevels);
            }

            return attackAnimation == AxesPrimaryAnimation
                   && primaryAnimation == AxesPrimaryAnimation
                   && secondaryAnimation == AxesSecondaryAnimation
                   && attackLevels >= AxesMinChainLevels
                   && attackLevel == attackLevels - 1;
        }
    }

    /// <summary>
    /// Damage events of the swing being played: counts per Attack instance and fired clip, restarting when
    /// either changes (a run alternates two clips, a relaunch is a new instance).
    /// </summary>
    public sealed class SwingHitCounter
    {
        private int _instance;
        private string? _clip;
        private int _count;

        /// <summary>Registers a hit and returns its 1-based number within the swing.</summary>
        public int Register(int instanceId, string? clip)
        {
            if (_count == 0 || instanceId != _instance || clip != _clip)
            {
                _instance = instanceId;
                _clip = clip;
                _count = 0;
            }

            return ++_count;
        }

        public void Reset()
        {
            _count = 0;
            _clip = null;
            _instance = 0;
        }
    }
}
