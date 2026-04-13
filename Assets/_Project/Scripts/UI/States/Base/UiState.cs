using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UiState : IState
{
    protected MonoBehaviour         Context      { get; }
    protected List<UiEffectSO> EnterEffects { get; }
    protected List<UiEffectSO>  ExitEffects  { get; }

    private Coroutine _enterCoroutine;

    protected UiState(
        MonoBehaviour         context,
        List<UiEffectSO> enterEffects,
        List<UiEffectSO>  exitEffects)
    {
        Context      = context;
        EnterEffects = enterEffects ?? new List<UiEffectSO>(0);
        ExitEffects  = exitEffects  ?? new List<UiEffectSO>(0);
    }

    public abstract string StateName { get; }

    public virtual void OnEnter()
    {
        _enterCoroutine = Context.StartCoroutine(RunEnterEffects());
    }

    public virtual void OnExit()
    {
        if (_enterCoroutine != null)
        {
            Context.StopCoroutine(_enterCoroutine);
            _enterCoroutine = null;
        }

        Context.StartCoroutine(RunExitEffects());
    }

    private IEnumerator RunEnterEffects()
    {
        foreach (var effect in EnterEffects)
            yield return effect.Execute(Context);
    }

    private IEnumerator RunExitEffects()
    {
        foreach (var effect in ExitEffects)
            yield return effect.Execute(Context);
    }
}
