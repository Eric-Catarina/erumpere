using System;
using UnityEngine;

[Serializable]
public class ChangeSceneAction : UiAction<ChangeSceneActionData>
{
    public override void Execute()
    {
        ScenesManager.Instance.LoadSceneByName(data.sceneName);
    }
}

[Serializable]
public struct ChangeSceneActionData : ActionData
{
    public string sceneName;
}
