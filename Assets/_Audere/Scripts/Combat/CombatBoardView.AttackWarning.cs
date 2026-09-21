using UnityEngine;

namespace Audere.Combat
{
    public sealed partial class CombatBoardView
    {
        private object attackWarningOwner;
        private CombatAttackWarningGraphic attackWarning;
        public bool IsAttackWarningVisible => attackWarning!=null && attackWarning.gameObject.activeSelf;
        public Vector2 AttackWarningPosition => attackWarning!=null?WorldToPlayArea(attackWarning.transform.position):Vector2.zero;

        public void ShowAttackWarning(object owner,Vector2 position,float activeAge,float scale=1f)
        {
            if(owner==null || playArea==null)return;
            if(attackWarningOwner!=null && !ReferenceEquals(attackWarningOwner,owner))return;
            attackWarningOwner=owner;
            if(attackWarning==null)
            {
                var go=new GameObject("Attack Warning ! (runtime)",typeof(RectTransform),typeof(CanvasRenderer),typeof(CombatAttackWarningGraphic));
                go.layer=playArea.gameObject.layer;go.transform.SetParent(playArea.parent,false);
                attackWarning=go.GetComponent<CombatAttackWarningGraphic>();
                attackWarning.raycastTarget=false;attackWarning.maskable=false;
                attackWarning.rectTransform.sizeDelta=new Vector2(32,48);
            }
            attackWarning.rectTransform.sizeDelta=new Vector2(32,48)*Mathf.Clamp(scale,1f,1.6f);
            attackWarning.SetPositions(null);
            attackWarning.transform.SetAsLastSibling();
            Rect bounds=playArea.rect;
            position.x=Mathf.Clamp(position.x,bounds.xMin+22f,bounds.xMax-22f);
            position.y=Mathf.Clamp(position.y,bounds.yMin+24f,bounds.yMax-30f);
            attackWarning.transform.position=playArea.TransformPoint(position);
            float pulse=Mathf.Repeat(activeAge,.16f)<.09f?1f:.3f;
            attackWarning.color=new Color(1f,.08f,.16f,pulse);
            attackWarning.gameObject.SetActive(true);
        }
        public void ShowAttackWarnings(object owner, Vector2[] entryPoints, float activeAge)
        {
            if (entryPoints == null || entryPoints.Length == 0) { HideAttackWarning(owner); return; }
            ShowAttackWarning(owner, playArea.rect.center, activeAge);
            if (!ReferenceEquals(owner, attackWarningOwner) || attackWarning == null) return;
            attackWarning.transform.position = playArea.TransformPoint(Vector2.zero);
            attackWarning.rectTransform.sizeDelta = playArea.rect.size;
            attackWarning.SetPositions(entryPoints);
        }
        public void HideAttackWarning(object owner)
        {
            if(!ReferenceEquals(owner,attackWarningOwner))return;
            ClearAttackWarning();
        }
        private void ClearAttackWarning()
        {
            attackWarningOwner=null;
            if(attackWarning!=null)attackWarning.gameObject.SetActive(false);
        }
    }
}
