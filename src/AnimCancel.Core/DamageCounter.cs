namespace AnimCancel.Core
{
    /// <summary>
    /// Counts damage events per attack. The game makes a fresh <c>Attack</c> clone for
    /// every swing, so a change of instance reference means a new swing.
    /// </summary>
    public sealed class DamageCounter
    {
        private object? _current;
        private int _seen;

        /// <summary>Records one damage event and returns how many this instance has had.</summary>
        public int Register(object attackInstance)
        {
            if (!ReferenceEquals(attackInstance, _current))
            {
                _current = attackInstance;
                _seen = 0;
            }
            return ++_seen;
        }
    }
}
