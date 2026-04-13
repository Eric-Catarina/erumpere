using System;
using UnityEngine;
using UnityEngine.SceneManagement;


[Serializable]
public class ChangePanelAction : UiAction<ChangePanelActionData>
{

    public override void Execute()
    {
        UIManager.Instance.ClosePanel(data.close);
        UIManager.Instance.OpenPanel(data.open);
    }
}

[Serializable]
public struct ChangePanelActionData : ActionData
{
    [SerializeField] public GameObject open;
    [SerializeField] public GameObject close;
}