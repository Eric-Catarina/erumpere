using System.Collections;
using UnityEngine;

public abstract class UiEffectSO : ScriptableObject
{
    public abstract IEnumerator Execute(MonoBehaviour context);
}
