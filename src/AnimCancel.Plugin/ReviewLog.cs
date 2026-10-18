using AnimCancel.Core;
using BepInEx.Logging;
using UnityEngine;

namespace AnimCancel;

/// <summary>
/// Writes the structured "AC ..." review lines (see DebugLine) when Debug log is on (LogGate).
/// Callers check <see cref="On"/> first so building the key list costs nothing when off; Write
/// checks it again. Errors go straight to <see cref="Log"/>, log-once, whatever the settings.
/// </summary>
internal static class ReviewLog
{
    internal static ManualLogSource? Log;

    internal static bool On => Log != null && LogGate.Allows(LogKind.Review, Plugin.Current.DebugLog, Settings.ComboLabLog);

    internal static void Write(string ev, params (string k, string v)[] kv)
    {
        if (!On)
        {
            return;
        }

        Log!.LogInfo(DebugLine.Format(Time.time, Time.frameCount, ev, kv));
    }
}
