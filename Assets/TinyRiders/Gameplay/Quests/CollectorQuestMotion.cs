using UnityEngine;
using UnityEngine.EventSystems;

namespace TinyRiders.Quests
{
    // Uses unscaled time so feedback continues while the board pauses gameplay.
    public sealed class CollectorQuestMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public bool entrance, hover, floatBadge;
        Vector3 rest;
        float age, scale=1;
        bool over, pressed;
        void Awake() { rest=transform.localScale; }
        void OnEnable() { age=0; over=pressed=false; }
        void OnDisable() { transform.localScale=rest; scale=1; over=pressed=false; }
        void Update()
        {
            age+=Time.unscaledDeltaTime;
            float target=pressed ? .95f : over ? 1.035f : 1;
            scale=Mathf.Lerp(scale,target,1-Mathf.Exp(-18*Time.unscaledDeltaTime));
            float pop=1;
            if(entrance && age<.42f) {float t=age/.42f-1; pop=1+2.70158f*t*t*t+1.70158f*t*t; pop=Mathf.Lerp(.84f,1,pop);}
            float pulse=floatBadge ? 1+.09f*Mathf.Sin(age*3.5f) : 1;
            transform.localScale=rest*(scale*pop*pulse);
        }
        public void OnPointerEnter(PointerEventData e) {if(hover) over=true;}
        public void OnPointerExit(PointerEventData e) {over=pressed=false;}
        public void OnPointerDown(PointerEventData e) {if(hover) pressed=true;}
        public void OnPointerUp(PointerEventData e) {pressed=false;}
    }
}
