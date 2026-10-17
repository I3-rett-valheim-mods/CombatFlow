using AnimCancel.Core;
using BepInEx.Logging;
using UnityEngine;

namespace AnimCancel;

/// <summary>
/// Writes the structured "AC ..." review lines (see DebugLine) when Debug log is on.
/// Callers check <see cref="On"/> first so building the key list costs nothing when off.
/// </summary>
internal static class ReviewLog
{
    internal static ManualLogSource? Log;

    internal static bool On => Log != null && Plugin.Current.DebugLog;

    internal static void Write(string ev, params (string k, string v)[] kv)
    {
        if (Log == null)
        {
            return;
        }

        Log.LogInfo(DebugLine.Format(Time.time, Time.frameCount, ev, kv));
    }
}
