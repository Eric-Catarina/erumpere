using System;
using UnityEngine;

[Serializable]
public struct ChangePanelButtonModel : UiButtonModel
{
    public GameObject panelToOpen;
    public GameObject panelToHide;
}