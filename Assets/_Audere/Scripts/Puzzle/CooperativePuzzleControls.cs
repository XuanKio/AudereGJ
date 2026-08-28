using Audere.Dialogue;
using Audere.GameplayInput;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Puzzle
{
    /// <summary>Scene-authored controls for the active pair; never constructs UI or actors.</summary>
    public sealed class CooperativePuzzleControls : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text status;
        public const string Objective = "Giúp Audere và Bianca lấy đồ về lớp";
        private CooperativePuzzleSession session;
        public CooperativePuzzleSession Session => session;
        private void Awake() { Show(false); }
        public void Bind(CooperativePuzzleSession value)
        {
            session=value;
            status.text=Objective;
            Show(true);
        }
        public void Unbind(CooperativePuzzleSession value) { if(session==value){session=null;Show(false);} }
        private void Show(bool visible)
        {
            if(group==null)return;
            group.alpha=1f;group.interactable=false;group.blocksRaycasts=false;
            if(status!=null)status.gameObject.SetActive(visible);
        }
        private void OnDisable() {session=null;Show(false);}
    }
}
