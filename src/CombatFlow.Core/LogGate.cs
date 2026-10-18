namespace CombatFlow.Core
{
    /// <summary>Review (AC) lines, combo lab (CL) lines, and plain informational lines.</summary>
    public enum LogKind { Review, Lab, Info }

    /// <summary>
    /// Which lines reach the BepInEx log. Everything goes through Debug log; CL lines also need Combo lab log.
    /// Not gated: the single "loaded" line at startup and the log-once error lines.
    /// </summary>
    public static class LogGate
    {
        /// <summary>Shipped default of Advanced / Combo lab log (Debug log's is CancelSettings.Default.DebugLog).</summary>
        public const bool ComboLabLogDefault = false;

        public static bool Allows(LogKind kind, bool debugLog, bool comboLabLog)
            => debugLog && (kind != LogKind.Lab || comboLabLog);
    }
}
