namespace AnimCancel.Core
{
    /// <summary>
    /// The cancelled clip keeps playing during the blend into the emote and its own Speed
    /// animation events still fire (sword_secondary: Speed(0.2) right after its Hit), which
    /// would undo speed 1. Those calls are skipped while the cancelled state is current and
    /// the animator is blending away from it. State hashes are layer-0 fullPathHash values.
    /// </summary>
    public static class SpeedSkip
    {
        public static bool ShouldSkip(int? cancelledState, int currentState, bool inTransition)
            => cancelledState.HasValue && inTransition && currentState == cancelledState.Value;

        /// <summary>The remembered state is dropped once it is neither current nor next.</summary>
        public static bool ShouldForget(int? cancelledState, int currentState, int nextState)
            => cancelledState.HasValue && currentState != cancelledState.Value && nextState != cancelledState.Value;
    }
}
