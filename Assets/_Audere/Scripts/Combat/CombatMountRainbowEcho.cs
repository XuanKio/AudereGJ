using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    // Snapshot only the authored image renderers, never clone actor logic or collision.
    internal sealed class CombatMountRainbowEcho
    {
        private const int Count = 12;
        private const float Lifetime = .34f, Interval = .027f;
        private readonly Transform root;
        private readonly Image[] sources;
        private readonly Image[,] images;
        private readonly float[] ages = new float[Count];
        private readonly bool[] live = new bool[Count];
        private readonly Color[] colors = new Color[Count];
        private float cooldown;
        private int next, emitted;

        public CombatMountRainbowEcho(Transform mount, CombatEnemyActor actor, Material material)
        {
            sources = actor.GetComponentsInChildren<Image>(true);
            root = new GameObject("Mount Rainbow Echo (runtime)", typeof(RectTransform)).transform;
            root.SetParent(mount.parent, false);
            root.SetSiblingIndex(mount.GetSiblingIndex());
            images = new Image[Count, sources.Length];
            for (int i=0;i<Count;i++) for (int j=0;j<sources.Length;j++)
            {
                var go=new GameObject("Echo "+i+" / "+sources[j].name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
                go.layer=mount.gameObject.layer;
                go.transform.SetParent(root,false);
                var image=go.GetComponent<Image>();
                image.material=material;image.raycastTarget=false;image.maskable=false;
                image.preserveAspect=sources[j].preserveAspect;
                images[i,j]=image;go.SetActive(false);
            }
        }

        public int ActiveCount { get { int n=0;foreach(bool active in live)if(active)n++;return n; } }

        public void Tick(float dt, bool emit)
        {
            for(int i=0;i<Count;i++)
            {
                if(!live[i])continue;
                ages[i]+=dt;
                if(ages[i]>=Lifetime){Hide(i);continue;}
                Color c=colors[i];c.a=.48f*Mathf.Pow(1f-ages[i]/Lifetime,1.5f);
                for(int j=0;j<sources.Length;j++)images[i,j].color=c;
            }
            cooldown-=dt;
            if(!emit || cooldown>0f)return;
            cooldown=Interval;
            int slot=next;next=(next+1)%Count;live[slot]=true;ages[slot]=0f;
            colors[slot]=Color.HSVToRGB(Mathf.Repeat(emitted++*.105f,1f),.88f,1f);
            for(int j=0;j<sources.Length;j++)
            {
                Image source=sources[j], image=images[slot,j];
                if(source==null || !source.enabled || !source.gameObject.activeInHierarchy){image.gameObject.SetActive(false);continue;}
                image.sprite=source.sprite;
                var rt=image.rectTransform;var sr=source.rectTransform;
                rt.pivot=sr.pivot;rt.sizeDelta=sr.rect.size;
                rt.SetPositionAndRotation(sr.position,sr.rotation);
                Vector3 parentScale=root.lossyScale,scale=sr.lossyScale;
                rt.localScale=new Vector3(scale.x/parentScale.x,scale.y/parentScale.y,1f);
                Color c=colors[slot];c.a=.48f;image.color=c;
                image.gameObject.SetActive(true);
            }
        }

        public void Clear(){for(int i=0;i<Count;i++)Hide(i);cooldown=0f;}
        private void Hide(int i){live[i]=false;for(int j=0;j<sources.Length;j++)if(images[i,j]!=null)images[i,j].gameObject.SetActive(false);}
        public void Dispose()
        {
            Clear();
            if(root==null)return;
            root.gameObject.SetActive(false);
            if(Application.isPlaying)Object.Destroy(root.gameObject);else Object.DestroyImmediate(root.gameObject);
        }
    }
}
