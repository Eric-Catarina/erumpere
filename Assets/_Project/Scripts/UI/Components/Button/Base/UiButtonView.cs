using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UiButtonView
{
    [Header("Effects — Default")]
    [SerializeField] public List<UiEffectSO> _defaultEnterEffects;
    [SerializeField] public List<UiEffectSO> _defaultExitEffects;

    [Header("Effects — Hover")]
    [SerializeField] public List<UiEffectSO> _hoverEnterEffects;
    [SerializeField] public List<UiEffectSO> _hoverExitEffects;

    [Header("Effects — Pressed")]
    [SerializeField] public List<UiEffectSO> _pressedEnterEffects;
    [SerializeField] public List<UiEffectSO> _pressedExitEffects;

    [Header("Effects — Disabled")]
    [SerializeField] public List<UiEffectSO> _disabledEnterEffects;
    [SerializeField] public List<UiEffectSO> _disabledExitEffects;
}
