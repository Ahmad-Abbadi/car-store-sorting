using UnityEngine;

namespace TinyRiders
{
    public sealed class CarVariantIdentity : MonoBehaviour
    {
        [SerializeField] string variantId;
        public string VariantId => variantId;

#if UNITY_EDITOR
        public void SetVariantId(string value) => variantId = value;
#endif
    }
}
