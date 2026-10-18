using TMPro;
using UnityEngine;

namespace TinyRiders.Quests
{
    /// <summary>Bounded reusable particles. Presentation never delivers rewards.</summary>
    public sealed class CollectorQuestRewardFX : MonoBehaviour
    {
        public RectTransform destination;
        public TMP_Text label;
        public CanvasGroup banner;
        public UnityEngine.UI.Image[] pieces;
        public bool celebrateOnEnable;
        Vector2 origin, end;
        float age=10;
        bool coins;
        void OnEnable() {if(celebrateOnEnable) Play(transform.position,null,false);}
        public void Play(Vector3 worldOrigin,string text,bool reward)
        {
            coins=reward; age=0; origin=((RectTransform)transform).InverseTransformPoint(worldOrigin);
            end=destination ? (Vector2)((RectTransform)transform).InverseTransformPoint(destination.position) : origin+Vector2.up*160;
            if(label && text!=null) label.SetLocalized(text); if(banner) banner.alpha=1;
            foreach(var p in pieces) p.gameObject.SetActive(true);
        }
        public void Cancel() {age=10; if(banner) banner.alpha=0; if(pieces!=null) foreach(var p in pieces) if(p) p.gameObject.SetActive(false);}
        void OnDisable()=>Cancel();
        void Update()
        {
            if(age>2.2f) return;
            age+=Time.unscaledDeltaTime;
            if(banner) {
                banner.alpha=Mathf.Clamp01(age/.12f)*Mathf.Clamp01((2.2f-age)/.4f);
                banner.transform.localScale=Vector3.one*(1+.08f*Mathf.Sin(Mathf.Clamp01(age/.4f)*Mathf.PI));
            }
            for(int i=0;i<pieces.Length;i++)
            {
                float t=Mathf.Clamp01((age-i*.015f)/1.15f);
                float angle=i*2.39996f;
                Vector2 burst=new Vector2(Mathf.Cos(angle)*125,Mathf.Sin(angle)*85+100);
                var p=pieces[i]; var r=p.rectTransform;
                r.anchoredPosition=coins ? Vector2.Lerp(origin,end,t*t)+burst*Mathf.Sin(t*Mathf.PI) : origin+burst*t+Vector2.down*(190*t*t);
                r.localRotation=Quaternion.Euler(0,0,coins ? 0 : i*31+t*270);
                r.localScale=Vector3.one*(coins ? .8f+.3f*Mathf.Sin(t*Mathf.PI) : 1);
                var c=p.color; c.a=1-t; p.color=c;
                p.gameObject.SetActive(t<1);
            }
        }
    }
}
