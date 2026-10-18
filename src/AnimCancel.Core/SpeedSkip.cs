namespace AnimCancel.Core
{
    /// <summary>
    /// The cancelled clip keeps playing during the blend into the emote and its own Speed
    /// animation events still fire (sword_secondary: Speed(0.2) right after its Hit), which
    /// would undo speed 1. The cancel runs inside the Hit event, so such a Speed event can fire
    /// in the same Animator evaluation, before the blend has started: no transition is
    /// required. Those calls are skipped while the cancelled state is layer 0's current or next
    /// state, for at most <see cref="Window"/> seconds after the cancel. State hashes are
    /// layer-0 fullPathHash values; nextState is 0 when not in transition.
    /// </summary>
    public static class SpeedSkip
    {
        public const float Window = 0.5f;

        public static bool ShouldSkip(int? cancelledState, int currentState, int nextState, float sinceCancel)
            => cancelledState.HasValue
               && sinceCancel <= Window
               && (currentState == cancelledState.Value || nextState == cancelledState.Value);

        /// <summary>The remembered state is dropped once it is neither current nor next.</summary>
        public static bool ShouldForget(int? cancelledState, int currentState, int nextState)
            => cancelledState.HasValue && currentState != cancelledState.Value && nextState != cancelledState.Value;
    }
}
