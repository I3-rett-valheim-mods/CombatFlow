using System.Collections.Generic;
using System.Linq;
using CombatFlow.Core;
using UnityEngine;

namespace CombatFlow;

/// <summary>
/// Reads the damage/chain event times of the attack clip playing on layer 0, straight
/// from <see cref="AnimationClip.events"/> so it survives game updates. During a blend
/// both the current and the next clip are read and <see cref="ClipPick"/> keeps the one
/// that has a damage event at its own clip time (the clip that fired the hit); otherwise
/// the current one. Timings are cached by clip name.
/// </summary>
internal static class ClipTimingCache
{
    private static readonly Dictionary<string, ClipTiming> Cache = new();
    private static readonly List<AnimatorClipInfo> Clips = new();

    private readonly struct Read
    {
        public readonly ClipTiming Timing;
        public readonly float StateSpeed;
        public readonly string ClipName;
        public readonly float ClipT;
        public readonly float Length;

        public Read(ClipTiming timing, float stateSpeed, string clipName, float clipT, float length)
        {
            Timing = timing;
            StateSpeed = stateSpeed;
            ClipName = clipName;
            ClipT = clipT;
            Length = length;
        }
    }

    /// <summary>
    /// Timing of the clip on layer 0 that fired the damage event, or null when no clip plays.
    /// Also returns the state's speed, the clip's name, the clip time (fractional
    /// normalizedTime × length) and which clip was picked (current|next|ambiguous).
    /// </summary>
    public static ClipTiming? ForCurrent(Animator a, out float stateSpeed, out string clipName, out float clipT,
        out string clipPick)
    {
        stateSpeed = 1f;
        clipName = "";
        clipT = 0f;
        clipPick = ClipPick.LogValue(ClipPickResult.Current);

        Read? current = ReadState(a, next: false);
        Read? next = a.IsInTransition(0) ? ReadState(a, next: true) : null;

        ClipPickResult pick = ClipPick.Choose(
            current?.Timing, current?.ClipT ?? 0f, current?.Length ?? 0f,
            next?.Timing, next?.ClipT ?? 0f, next?.Length ?? 0f);
        clipPick = ClipPick.LogValue(pick);
        Read? chosen = ClipPick.UseNext(pick) || current == null ? next : current;
        if (chosen == null)
        {
            return null;
        }

        Read r = chosen.Value;
        stateSpeed = r.StateSpeed;
        clipName = r.ClipName;
        clipT = r.ClipT;
        return r.Timing;
    }

    private static Read? ReadState(Animator a, bool next)
    {
        AnimatorStateInfo state = next ? a.GetNextAnimatorStateInfo(0) : a.GetCurrentAnimatorStateInfo(0);

        Clips.Clear();
        if (next)
        {
            a.GetNextAnimatorClipInfo(0, Clips);
        }
        else
        {
            a.GetCurrentAnimatorClipInfo(0, Clips);
        }

        AnimationClip? clip = null;
        float bestWeight = float.NegativeInfinity;
        foreach (AnimatorClipInfo info in Clips)
        {
            if (info.clip != null && info.weight > bestWeight)
            {
                clip = info.clip;
                bestWeight = info.weight;
            }
        }

        if (clip == null)
        {
            return null;
        }

        string name = clip.name;
        float n = state.normalizedTime;
        float clipT = (n - Mathf.Floor(n)) * clip.length;

        if (!Cache.TryGetValue(name, out ClipTiming timing))
        {
            timing = ClipTiming.FromEvents(clip.events.Select(e => (e.time, e.functionName)));
            Cache[name] = timing;
        }

        return new Read(timing, state.speed, name, clipT, clip.length);
    }
}
