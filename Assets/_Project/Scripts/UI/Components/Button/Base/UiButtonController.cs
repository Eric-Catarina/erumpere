using UnityEngine;
using UnityEngine.EventSystems;
using Core.StateMachine.FiniteStateMachine;

public abstract class UiButtonController<T> : MonoBehaviour where T : UiButtonModel
{
    [SerializeField] protected T uiButtonModel;
    [SerializeField] protected UiButtonView uiButtonView;
    [SerializeField] protected bool isDisabled;
    protected MooreFiniteStateMachine<UiState> _fsm = new MooreFiniteStateMachine<UiState>();

    private void Start() => _fsm.TransitionTo(new ButtonDefault(this, uiButtonView._defaultEnterEffects, uiButtonView._defaultExitEffects));

    //Set enabled/disabled
    public void SetDisabled(bool disabled)
    {
        isDisabled = disabled;
        _fsm.TransitionTo(isDisabled ? new ButtonDisabled(this, uiButtonView._disabledEnterEffects, uiButtonView._disabledExitEffects) : new ButtonDefault(this, uiButtonView._defaultEnterEffects, uiButtonView._defaultExitEffects));
    }
}
