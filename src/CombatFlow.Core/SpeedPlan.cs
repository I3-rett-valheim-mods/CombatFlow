namespace CombatFlow.Core
{
    /// <summary>
    /// How to bring the animator back to speed 1 after a cancel. While the game's hit-stop
    /// (CharacterAnimEvent.FreezeFrame) runs, the animator sits at 0.0001 and is restored to
    /// m_pauseSpeed when the timer ends, so writing the speed then would be overwritten and
    /// would also cut the visible hit-stop: set the restore value instead.
    /// </summary>
    public static class SpeedPlan
    {
        public static (bool setAnimatorSpeed, bool setPauseRestore) For(float pauseTimer)
            => pauseTimer > 0f ? (false, true) : (true, false);
    }
}
