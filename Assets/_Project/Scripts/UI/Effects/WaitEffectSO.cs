using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "UI/Effects/Wait")]
public class WaitEffectSO : UiEffectSO
{
    public float seconds = 0.1f;

    public override IEnumerator Execute(MonoBehaviour context)
    {
        yield return new WaitForSeconds(seconds);
    }
}
