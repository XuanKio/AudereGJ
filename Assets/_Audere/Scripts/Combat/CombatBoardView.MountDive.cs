using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    public sealed partial class CombatBoardView
    {
        private object mountDiveOwner;
        private Vector3 mountHomePosition, mountVisualOffset;
        private Quaternion mountHomeRotation;
        private Vector2 mountDiveHome;
        private CombatMountRainbowEcho mountEcho;
        private CombatEnemyActor echoActor;
        private Material echoMaterial;
        private bool echoMonochrome;
        private CombatSplitBoardGraphic splitBoardGraphic;
        private Image splitFrameImage, splitFieldImage;
        private RectMask2D splitFieldMask;
        private Vector4 splitMaskAuthoredPadding;
        private bool splitFrameWasEnabled, splitFieldWasEnabled, splitVisualCaptured;
        private float boardSeparation, boardSplitX;
        private bool gapConstrainsHeart, mountBodyPending;
        private Rect mountSweptBody;

        public Vector2 MountDiveHome => mountDiveHome;
        public bool IsMountDiveActive => mountDiveOwner != null;
        public float BoardSeparation => boardSeparation;
        public int ActiveMountEchoes => mountEcho?.ActiveCount ?? 0;
        public bool OwnsMountDive(object owner) => ReferenceEquals(mountDiveOwner, owner);

        public bool BeginMountDive(object owner, CombatEnemyActor actor, Material material, bool monochrome = false)
        {
            ResolveReferences();
            if(owner==null || actor==null || enemyMount==null || playArea==null || battleBoxFrame==null)return false;
            if(mountDiveOwner!=null && !OwnsMountDive(owner))return false;
            mountDiveOwner=owner;
            mountHomePosition=enemyMount.localPosition;mountHomeRotation=enemyMount.localRotation;
            Transform visual=actor.VisualRoot!=null?actor.VisualRoot:actor.transform;
            mountDiveHome=WorldToPlayArea(visual.position);
            mountVisualOffset=enemyMount.position-visual.position;
            if(mountEcho==null || echoActor!=actor || echoMaterial!=material || echoMonochrome!=monochrome)
            {
                mountEcho?.Dispose();mountEcho=new CombatMountRainbowEcho(enemyMount,actor,material,monochrome);
                echoActor=actor;echoMaterial=material;echoMonochrome=monochrome;
            }
            mountEcho.Clear();
            splitFrameImage=battleBoxFrame.GetComponent<Image>();splitFieldImage=playArea.GetComponent<Image>();
            splitFieldMask=playArea.GetComponent<RectMask2D>();
            if(splitFieldMask!=null)splitMaskAuthoredPadding=splitFieldMask.padding;
            splitFrameWasEnabled=splitFrameImage!=null && splitFrameImage.enabled;
            splitFieldWasEnabled=splitFieldImage!=null && splitFieldImage.enabled;
            splitVisualCaptured=true;
            return true;
        }

        public void SetMountDivePose(object owner, Vector2 center, float tilt, float deltaTime, bool emitEcho)
        {
            if(!OwnsMountDive(owner) || enemyMount==null)return;
            enemyMount.position=playArea.TransformPoint(center)+mountVisualOffset;
            enemyMount.localRotation=mountHomeRotation*Quaternion.Euler(0,0,tilt);
            mountEcho?.Tick(deltaTime,emitEcho);
        }

        public void AccumulateMountDiveBody(object owner, Vector2 from, Vector2 to, float width, float height)
        {
            if(!OwnsMountDive(owner))return;
            Rect swept=Rect.MinMaxRect(Mathf.Min(from.x,to.x)-width*.5f,Mathf.Min(from.y,to.y)-height*.5f,
                Mathf.Max(from.x,to.x)+width*.5f,Mathf.Max(from.y,to.y)+height*.5f);
            if(mountBodyPending)swept=Rect.MinMaxRect(Mathf.Min(swept.xMin,mountSweptBody.xMin),
                Mathf.Min(swept.yMin,mountSweptBody.yMin),Mathf.Max(swept.xMax,mountSweptBody.xMax),Mathf.Max(swept.yMax,mountSweptBody.yMax));
            mountSweptBody=swept;mountBodyPending=true;
        }

        private int ConsumeMountDiveHit(float invulnerability)
        {
            if(!mountBodyPending || mountDiveOwner==null)return 0;
            mountBodyPending=false;
            Vector2 p=PlayerPosition;
            Vector2 half=playerView!=null?playerView.RectTransform.rect.size*.5f:Vector2.zero;
            Rect body=mountSweptBody;
            bool overlap=p.x+half.x>=body.xMin && p.x-half.x<=body.xMax &&
                p.y+half.y>=body.yMin && p.y-half.y<=body.yMax;
            return overlap && playerView!=null && playerView.TryRegisterHit(invulnerability)?1:0;
        }

        public void SetMountDiveSplit(object owner, float cutX, float separation, float glow, bool constrainHeart)
        {
            if(!OwnsMountDive(owner))return;
            boardSplitX=cutX;boardSeparation=Mathf.Max(0f,separation);gapConstrainsHeart=constrainHeart;
            if(boardSeparation<=.01f){RestoreSplitGraphics();return;}
            if(splitFieldMask!=null)
            {
                Canvas canvas=splitFieldMask.GetComponentInParent<Canvas>();
                Vector3 world=playArea.TransformVector(Vector3.right*boardSeparation);
                float padding=canvas!=null?Mathf.Abs(canvas.transform.InverseTransformVector(world).x):boardSeparation;
                splitFieldMask.padding=splitMaskAuthoredPadding+new Vector4(-padding,0,-padding,0);
            }
            if(splitBoardGraphic==null)
            {
                var go=new GameObject("Separated Board Halves (runtime)",typeof(RectTransform),typeof(CanvasRenderer),typeof(CombatSplitBoardGraphic));
                go.layer=battleBoxFrame.gameObject.layer;
                go.transform.SetParent(battleBoxFrame.parent,false);
                go.transform.SetSiblingIndex(battleBoxFrame.GetSiblingIndex()+1);
                splitBoardGraphic=go.GetComponent<CombatSplitBoardGraphic>();
            }
            RectTransform rt=splitBoardGraphic.rectTransform;
            rt.anchorMin=battleBoxFrame.anchorMin;rt.anchorMax=battleBoxFrame.anchorMax;
            rt.pivot=battleBoxFrame.pivot;rt.sizeDelta=battleBoxFrame.sizeDelta;
            rt.anchoredPosition=battleBoxFrame.anchoredPosition;rt.localScale=battleBoxFrame.localScale;
            rt.localRotation=battleBoxFrame.localRotation;
            Vector3 fMin=battleBoxFrame.InverseTransformPoint(playArea.TransformPoint(playArea.rect.min));
            Vector3 fMax=battleBoxFrame.InverseTransformPoint(playArea.TransformPoint(playArea.rect.max));
            float unit=playArea.lossyScale.x/battleBoxFrame.lossyScale.x;
            float cut=battleBoxFrame.InverseTransformPoint(playArea.TransformPoint(new Vector2(cutX,0))).x;
            Color border=splitFrameImage!=null?splitFrameImage.color:new Color(.25f,.22f,.29f);
            Color fill=splitFieldImage!=null?splitFieldImage.color:new Color(.06f,.04f,.09f);
            splitBoardGraphic.SetGeometry(battleBoxFrame.rect,Rect.MinMaxRect(fMin.x,fMin.y,fMax.x,fMax.y),cut,boardSeparation*unit,border,fill,glow);
            splitBoardGraphic.gameObject.SetActive(true);
            if(splitFrameImage!=null)splitFrameImage.enabled=false;
            if(splitFieldImage!=null)splitFieldImage.enabled=false;
        }

        public Rect GetDiceMovementBounds(Vector2 position)
        {
            Rect r=playArea!=null?playArea.rect:default;
            if(boardSeparation<=.01f)return r;
            return position.x<boardSplitX
                ? Rect.MinMaxRect(r.xMin-boardSeparation,r.yMin,boardSplitX-boardSeparation-7f,r.yMax)
                : Rect.MinMaxRect(boardSplitX+boardSeparation+7f,r.yMin,r.xMax+boardSeparation,r.yMax);
        }

        private Vector2 ClampToSplitBoard(Vector2 p, Vector2 half)
        {
            if(boardSeparation<=.01f)return p;
            Rect r=playArea.rect;
            p.x=Mathf.Clamp(p.x,r.xMin-boardSeparation+half.x,r.xMax+boardSeparation-half.x);
            if(gapConstrainsHeart)
            {
                float heartHalf=GetHeartHalfSizeInPlayArea().x;
                float gap=boardSeparation+heartHalf+7f;
                if(Mathf.Abs(p.x-boardSplitX)<gap)p.x=boardSplitX+(p.x<boardSplitX?-gap:gap);
            }
            return p;
        }

        private Vector2 GetHeartHalfSizeInPlayArea()
        {
            if(playerView==null || playerView.RectTransform==null)return Vector2.one*14f;
            RectTransform heart=playerView.RectTransform;
            Vector3 x=playArea.InverseTransformVector(heart.TransformVector(Vector3.right*heart.rect.width*.5f));
            Vector3 y=playArea.InverseTransformVector(heart.TransformVector(Vector3.up*heart.rect.height*.5f));
            return new Vector2(Mathf.Abs(x.x)+Mathf.Abs(y.x),Mathf.Abs(x.y)+Mathf.Abs(y.y));
        }

        public void EndMountDive(object owner)
        {
            if(!OwnsMountDive(owner))return;
            ResetMountDive();
        }

        private void ResetMountDive()
        {
            if(mountDiveOwner!=null && enemyMount!=null)
            {enemyMount.localPosition=mountHomePosition;enemyMount.localRotation=mountHomeRotation;}
            mountDiveOwner=null;mountBodyPending=false;boardSeparation=0f;gapConstrainsHeart=false;
            mountEcho?.Clear();RestoreSplitGraphics();splitVisualCaptured=false;
            if(playArea!=null && catchCursor!=null)catchCursor.anchoredPosition=ClampCursorToBattleBox(catchCursor.anchoredPosition);
        }

        private void RestoreSplitGraphics()
        {
            if(splitBoardGraphic!=null)splitBoardGraphic.gameObject.SetActive(false);
            if(!splitVisualCaptured)return;
            if(splitFieldMask!=null)splitFieldMask.padding=splitMaskAuthoredPadding;
            if(splitFrameImage!=null)splitFrameImage.enabled=splitFrameWasEnabled;
            if(splitFieldImage!=null)splitFieldImage.enabled=splitFieldWasEnabled;
        }
    }
}
