using System.Collections.Generic;
using UnityEngine;

public class ButtonDefault : UiState
{
    public override string StateName => "Default";

    public ButtonDefault(
        MonoBehaviour         context,
        List<UiEffectSO> enter,
        List<UiEffectSO>  exit) : base(context, enter, exit) { }
}

public class ButtonHover : UiState
{
    public override string StateName => "Hover";

    public ButtonHover(
        MonoBehaviour         context,
        List<UiEffectSO> enter,
        List<UiEffectSO>  exit) : base(context, enter, exit) { }
}

public class ButtonPressed : UiState
{
    public override string StateName => "Pressed";

    public ButtonPressed(
        MonoBehaviour         context,
        List<UiEffectSO> enter,
        List<UiEffectSO>  exit) : base(context, enter, exit) { }
}

public class ButtonDisabled : UiState
{
    public override string StateName => "Disabled";

    public ButtonDisabled(
        MonoBehaviour         context,
        List<UiEffectSO> enter,
        List<UiEffectSO>  exit) : base(context, enter, exit) { }
}
