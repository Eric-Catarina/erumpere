using System;
using UnityEngine;

namespace Erumperem.Combat
{
    /// <summary>
    /// Eventos de apresentação de combate. UI subscreve; lógica de combate só publica — evita acoplamento direto a TMP/layout.
    /// </summary>
    public sealed class CombatPresentationHub : MonoBehaviour
    {
        public event Action<string> PlayerSkillHelpTextChanged;
        public event Action ActionPresentationStarted;
        public event Action ActionPresentationEnded;
        public event Action CombatEnded;

        public void PublishPlayerSkillHelp(string text)
        {
            PlayerSkillHelpTextChanged?.Invoke(text ?? string.Empty);
        }

        public void PublishActionPresentationStarted()
        {
            ActionPresentationStarted?.Invoke();
        }

        public void PublishActionPresentationEnded()
        {
            ActionPresentationEnded?.Invoke();
        }

        public void PublishCombatEnded()
        {
            CombatEnded?.Invoke();
        }
    }
}
