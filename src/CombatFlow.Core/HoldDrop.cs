namespace CombatFlow.Core
{
    /// <summary>
    /// Pure rules for the SetControls hold-drop measurement (Task 8).
    /// </summary>
    public static class HoldDrop
    {
        /// <summary>Seconds after a cancel during which SetControls is still observed.</summary>
        public const float WatchWindow = 0.5f;

        /// <summary>Observe while a cancel is in flight, or within the window after the last one.</summary>
        public static bool ShouldWatch(bool inFlight, float now, float lastCancelTime)
            => inFlight || (now - lastCancelTime >= 0f && now - lastCancelTime <= WatchWindow);

        /// <summary>The player held the button but SetControls left the field false.</summary>
        public static bool WasDropped(bool incomingHold, bool holdAfter)
            => incomingHold && !holdAfter;
    }
}
