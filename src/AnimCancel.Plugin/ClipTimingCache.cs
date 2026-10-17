using System.Collections.Generic;
using System.Linq;
using AnimCancel.Core;
using UnityEngine;

namespace AnimCancel;

/// <summary>
/// Reads the damage/chain event times of the attack clip playing on layer 0, straight
/// from <see cref="AnimationClip.events"/> so it survives game updates. During a blend
/// the next state is used when it is the attack (tag "attack", as Humanoid.s_animatorTagAttack),
/// otherwise the current one. Timings are cached by clip name.
/// </summary>
internal static class ClipTimingCache
{
    private static readonly int TagAttack = Animator.StringToHash("attack");
    private static readonly Dictionary<string, ClipTiming> Cache = new();
    private static readonly List<AnimatorClipInfo> Clips = new();

    /// <summary>
    /// Timing of the attack clip on layer 0, or null when no clip plays. Also returns the
    /// state's speed, the clip's name and the clip time (fractional normalizedTime × length).
    /// </summary>
    public static ClipTiming? ForCurrent(Animator a, out float stateSpeed, out string clipName, out float clipT)
    {
        stateSpeed = 1f;
        clipName = "";
        clipT = 0f;

        bool useNext = a.IsInTransition(0) && a.GetNextAnimatorStateInfo(0).tagHash == TagAttack;
        AnimatorStateInfo state = useNext ? a.GetNextAnimatorStateInfo(0) : a.GetCurrentAnimatorStateInfo(0);

        Clips.Clear();
        if (useNext)
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

        stateSpeed = state.speed;
        clipName = clip.name;
        float n = state.normalizedTime;
        clipT = (n - Mathf.Floor(n)) * clip.length;

        if (!Cache.TryGetValue(clipName, out ClipTiming timing))
        {
            timing = ClipTiming.FromEvents(clip.events.Select(e => (e.time, e.functionName)));
            Cache[clipName] = timing;
        }

        return timing;
    }
}
