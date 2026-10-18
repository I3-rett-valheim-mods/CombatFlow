using System;
using System.Collections.Generic;
using AnimCancel.Core;
using HarmonyLib;
using UnityEngine;

namespace AnimCancel.Patches;

/// <summary>
/// Postfix on Player.OnDodgeMortal (the DodgeMortal animation event, clip time 0.7 of the
/// 1.5 s dive: the invincibility frames just ended, m_dodgeInvincible = false). Cuts the
/// roll there through the same local emote cancel as the attacks.
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.OnDodgeMortal))]
internal static class DodgePatch
{
    private const string Kind = "Dodge";
    private static readonly int TagDodge = Animator.StringToHash("dodge");
    private static readonly List<AnimatorClipInfo> Clips = new();
    private static bool _reported;

    private static void Postfix(Player __instance)
    {
        try
        {
            if (__instance != Player.m_localPlayer)
            {
                return;
            }

            CancelSettings s = Plugin.Current;
            if (!s.Enabled)
            {
                return;
            }

            CancelDecision d = CancelPolicy.ShouldCancelDodge(s);
            Animator? animator = __instance.m_animator;
            if (d.Cancel && (animator == null || !IsDodging(animator)))
            {
                d = new CancelDecision(false, "not_dodging");
            }

            if (ReviewLog.On)
            {
                ReviewLog.Write("decision", ("kind", Kind), ("lvl", "0"),
                    ("cancel", DebugLine.Bool(d.Cancel)), ("reason", d.Reason));
            }

            if (!d.Cancel || animator == null)
            {
                return;
            }

            float clipT = ReviewLog.On ? ClipTime(animator) : 0f;
            Canceller.Cancel(__instance, s.Emote, TagDodge, Kind);
            if (ReviewLog.On)
            {
                ReviewLog.Write("dodge_cancel", ("clipT", DebugLine.Num(clipT)));
            }
        }
        catch (Exception e)
        {
            if (!_reported)
            {
                _reported = true;
                ReviewLog.Log?.LogError($"{nameof(DodgePatch)}: {e}");
            }
        }
    }

    /// <summary>Layer 0 is in, or blending into, a state tagged "dodge".</summary>
    private static bool IsDodging(Animator a) =>
        a.GetCurrentAnimatorStateInfo(0).tagHash == TagDodge
        || (a.IsInTransition(0) && a.GetNextAnimatorStateInfo(0).tagHash == TagDodge);

    /// <summary>Fractional normalizedTime x clip length of the dodge state.</summary>
    private static float ClipTime(Animator a)
    {
        bool useNext = a.IsInTransition(0) && a.GetNextAnimatorStateInfo(0).tagHash == TagDodge;
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
        float best = float.NegativeInfinity;
        foreach (AnimatorClipInfo info in Clips)
        {
            if (info.clip != null && info.weight > best)
            {
                clip = info.clip;
                best = info.weight;
            }
        }

        if (clip == null)
        {
            return 0f;
        }

        float n = state.normalizedTime;
        return (n - Mathf.Floor(n)) * clip.length;
    }
}
