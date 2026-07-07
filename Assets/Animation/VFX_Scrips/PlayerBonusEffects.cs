using System.Collections;
using UnityEngine;

public class PlayerBonusEffects : MonoBehaviour
{
    [Header("Effects")]
    public ParticleSystem jumpEffect;
    public ParticleSystem speedEffect;
    public ParticleSystem gravityEffect;
    public ParticleSystem flightEffect;

    [Header("Durations")]
    public float jumpEffectDuration = 2f;
    public float speedEffectDuration = 2f;
    public float gravityEffectDuration = 2f;
    public float flightEffectDuration = 2f;

    private Coroutine jumpRoutine;
    private Coroutine speedRoutine;
    private Coroutine gravityRoutine;
    private Coroutine flightRoutine;

    private void Awake()
    {
        StopAllBonusEffects();
    }

    public void PlayJumpEffect()
    {
        jumpRoutine = RestartEffect(jumpEffect, jumpEffectDuration, jumpRoutine);
    }

    public void PlaySpeedEffect()
    {
        speedRoutine = RestartEffect(speedEffect, speedEffectDuration, speedRoutine);
    }

    public void PlayGravityEffect()
    {
        gravityRoutine = RestartEffect(gravityEffect, gravityEffectDuration, gravityRoutine);
    }

    public void PlayFlightEffect()
    {
        flightRoutine = RestartEffect(flightEffect, flightEffectDuration, flightRoutine);
    }

    public void StopAllBonusEffects()
    {
        StopEffect(jumpEffect);
        StopEffect(speedEffect);
        StopEffect(gravityEffect);
        StopEffect(flightEffect);
    }

    private Coroutine RestartEffect(
        ParticleSystem effect,
        float duration,
        Coroutine currentRoutine
    )
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        return StartCoroutine(PlayEffectRoutine(effect, duration));
    }

    private IEnumerator PlayEffectRoutine(ParticleSystem effect, float duration)
    {
        if (effect == null)
            yield break;

        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effect.Play(true);

        yield return new WaitForSeconds(duration);

        effect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private void StopEffect(ParticleSystem effect)
    {
        if (effect == null)
            return;

        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}