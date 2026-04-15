using TMPro;
using UnityEngine;

namespace Erumperem.Combat
{
    /// <summary>
    /// Pilha de linhas no canto (ex.: superior direito): instancia um prefab com TMP por mensagem.
    /// </summary>
    public sealed class CombatLogStackView : MonoBehaviour
    {
        [SerializeField] private RectTransform stackParent;
        [SerializeField] private GameObject linePrefab;
        [SerializeField] private int maxLines = 12;

        public void Push(string message)
        {
            if (stackParent == null || linePrefab == null || string.IsNullOrEmpty(message))
            {
                return;
            }

            var instance = Instantiate(linePrefab, stackParent);
            instance.transform.SetAsFirstSibling();

            var tmp = instance.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
            {
                tmp.text = message;
            }

            while (stackParent.childCount > maxLines)
            {
                var oldest = stackParent.GetChild(stackParent.childCount - 1);
                Destroy(oldest.gameObject);
            }
        }
    }
}
