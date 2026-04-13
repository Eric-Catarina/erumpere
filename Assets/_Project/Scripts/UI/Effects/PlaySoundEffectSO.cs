using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "UI/Effects/PlaySound")]
public class PlaySoundEffectSO : UiEffectSO
{
    public AudioClip clip;
    public float volume = 1f;

    public override IEnumerator Execute(MonoBehaviour context)
    {
        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, context.transform.position, volume);
        yield break;
    }
}
