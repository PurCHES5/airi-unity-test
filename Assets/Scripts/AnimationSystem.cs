using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class AnimationSystem
{
    readonly PlayableGraph playableGraph;
    readonly AnimationMixerPlayable topLevelMixer;
    readonly AnimationMixerPlayable locomotionMixer;
    readonly MonoBehaviour host;

    AnimationClipPlayable oneShotPlayable;

    Coroutine blendInCoroutine;
    Coroutine blendOutCoroutine;
    Coroutine oneShotCoroutine;

    // Current weight of the action layer, or 0 if nothing is connected.
    float ActionLayerWeight =>
        oneShotPlayable.IsValid() ? topLevelMixer.GetInputWeight(1) : 0f;

    public AnimationSystem(
        MonoBehaviour host,
        Animator animator,
        AnimationClip idleClip,
        AnimationClip walkClip)
    {
        this.host = host;

        playableGraph = PlayableGraph.Create("AnimationSystem");

        var output = AnimationPlayableOutput.Create(playableGraph, "Animation", animator);

        topLevelMixer = AnimationMixerPlayable.Create(playableGraph, 2);
        output.SetSourcePlayable(topLevelMixer);

        locomotionMixer = AnimationMixerPlayable.Create(playableGraph, 2);
        topLevelMixer.ConnectInput(0, locomotionMixer, 0);
        topLevelMixer.SetInputWeight(0, 1f);

        var idlePlayable = AnimationClipPlayable.Create(playableGraph, idleClip);
        var walkPlayable = AnimationClipPlayable.Create(playableGraph, walkClip);

        idlePlayable.GetAnimationClip().wrapMode = WrapMode.Loop;
        walkPlayable.GetAnimationClip().wrapMode = WrapMode.Loop;

        locomotionMixer.ConnectInput(0, idlePlayable, 0);
        locomotionMixer.ConnectInput(1, walkPlayable, 0);

        playableGraph.Play();
    }

    public void UpdateLocomotion(Vector3 velocity, float maxSpeed)
    {
        float w = Mathf.InverseLerp(0f, maxSpeed, velocity.magnitude);
        locomotionMixer.SetInputWeight(0, 1f - w);
        locomotionMixer.SetInputWeight(1, w);
    }

    // ── One-shot ──────────────────────────────────────────────────────────────
    // Blends in from the current action-layer weight and plays once.
    // Does NOT auto-blend back to locomotion — the caller decides what comes
    // next via onFinished (e.g. PlayLooping, StopActionLayer, another PlayOneShot).

    public void PlayOneShot(AnimationClip clip, Action onFinished)
    {
        if (oneShotPlayable.IsValid() && oneShotPlayable.GetAnimationClip() == clip)
            return;

        float startWeight = ActionLayerWeight;  // snapshot BEFORE any disruption

        KillCoroutines();
        SwapPlayable(clip);

        float blendDur = Mathf.Clamp(clip.length * 0.1f, 0.1f, clip.length * 0.5f);

        if (startWeight < 0.99f)
            BlendIn(blendDur, startWeight);

        oneShotCoroutine = host.StartCoroutine(
            MonitorOneShot(clip.length, onFinished));
    }

    IEnumerator MonitorOneShot(float duration, Action onFinished)
    {
        yield return new WaitForSeconds(duration);
        // Don't touch the playable or weights here.
        // SwapPlayable (next clip) or StopActionLayer (back to loco) will clean up.
        onFinished?.Invoke();
    }

    // ── Looping action layer ──────────────────────────────────────────────────
    // Swaps in a looping clip at the current weight, blending in if below full.

    public void PlayLooping(AnimationClip clip, float blendDuration = 0.2f)
    {
        float startWeight = ActionLayerWeight;

        KillCoroutines();
        SwapPlayable(clip);

        if (startWeight < 0.99f)
            BlendIn(blendDuration, startWeight);
        // Already at full weight → clip swaps with no visible transition needed.
    }

    // ── Stop action layer ─────────────────────────────────────────────────────
    // Blends the action layer back to 0, then disconnects the playable.

    public void StopActionLayer(float blendDuration = 0.3f, Action onFinished = null)
    {
        float startWeight = ActionLayerWeight;

        KillCoroutines();

        blendOutCoroutine = host.StartCoroutine(BlendCoroutine(
            blendDuration,
            t => {
                float w = Mathf.Lerp(startWeight, 0f, t);
                topLevelMixer.SetInputWeight(0, 1f - w);
                topLevelMixer.SetInputWeight(1, w);
            },
            () => {
                if (oneShotPlayable.IsValid())
                    DisconnectAndDestroyOneShot();
                onFinished?.Invoke();
            }
        ));
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    // Replaces the clip on slot 1 while preserving the current blend weights,
    // so there is never a frame where the locomotion layer snaps to full weight.
    void SwapPlayable(AnimationClip clip)
    {
        float w = topLevelMixer.GetInputWeight(1);

        if (oneShotPlayable.IsValid())
            DisconnectAndDestroyOneShot();

        oneShotPlayable = AnimationClipPlayable.Create(playableGraph, clip);
        topLevelMixer.ConnectInput(1, oneShotPlayable, 0);
        topLevelMixer.SetInputWeight(0, 1f - w);
        topLevelMixer.SetInputWeight(1, w);
    }

    void BlendIn(float duration, float fromWeight)
    {
        blendInCoroutine = host.StartCoroutine(BlendCoroutine(
            duration,
            t => {
                float w = Mathf.Lerp(fromWeight, 1f, t);
                topLevelMixer.SetInputWeight(0, 1f - w);
                topLevelMixer.SetInputWeight(1, w);
            }
        ));
    }

    IEnumerator BlendCoroutine(float duration, Action<float> onBlend, Action onFinished = null)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            onBlend(Mathf.Clamp01(t));
            yield return null;
        }
        onBlend(1f);
        onFinished?.Invoke();
    }

    void KillCoroutines()
    {
        if (blendInCoroutine  != null) host.StopCoroutine(blendInCoroutine);
        if (blendOutCoroutine != null) host.StopCoroutine(blendOutCoroutine);
        if (oneShotCoroutine  != null) host.StopCoroutine(oneShotCoroutine);
        blendInCoroutine = blendOutCoroutine = oneShotCoroutine = null;
    }

    void DisconnectAndDestroyOneShot()
    {
        topLevelMixer.DisconnectInput(1);
        playableGraph.DestroyPlayable(oneShotPlayable);
    }

    public void Destroy()
    {
        KillCoroutines();
        if (playableGraph.IsValid())
            playableGraph.Destroy();
    }
}