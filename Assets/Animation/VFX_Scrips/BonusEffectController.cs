using UnityEngine;

public class BonusEffectController : MonoBehaviour
{
    [Header("Particles")]
    public ParticleSystem idleParticles;
    public ParticleSystem activationParticles;

    [Header("Settings")]
    public bool playIdleOnEnable = true;
    public bool stopIdleOnActivation = true;

    [Header("Idle Restart")]
    public bool restartIdleAfterActivation = true;
    public float restartIdleDelay = 1f;

    private float restartIdleTimer = 0f;
    private bool waitingForIdleRestart = false;

    private void Awake()
    {
        PrepareActivationParticles();
    }

    private void OnEnable()
    {
        ResetEffect();
    }

    private void Update()
    {
        if (!waitingForIdleRestart)
            return;

        restartIdleTimer -= Time.deltaTime;

        if (restartIdleTimer > 0f)
            return;

        waitingForIdleRestart = false;

        if (idleParticles != null)
            idleParticles.Play(true);
    }

    public void ResetEffect()
    {
        waitingForIdleRestart = false;
        restartIdleTimer = 0f;

        PrepareActivationParticles();

        if (activationParticles != null)
        {
            activationParticles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }

        if (playIdleOnEnable && idleParticles != null)
        {
            idleParticles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );

            idleParticles.Play(true);
        }
    }

    public void PlayActivation()
    {
        if (stopIdleOnActivation && idleParticles != null)
        {
            idleParticles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }

        if (activationParticles != null)
        {
            activationParticles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );

            activationParticles.Play(true);
        }

        if (restartIdleAfterActivation)
        {
            waitingForIdleRestart = true;
            restartIdleTimer = restartIdleDelay;
        }
    }

    private void PrepareActivationParticles()
    {
        if (activationParticles == null)
            return;

        ParticleSystem.MainModule main = activationParticles.main;
        main.playOnAwake = false;
        main.loop = false;
    }
}