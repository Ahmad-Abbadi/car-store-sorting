using UnityEngine;
namespace TinyRiders
{
    /// <summary>Identity of a physical copy, transferred between the template's world, hand and shelf forms.</summary>
    public sealed class ThemedWorldInstance : MonoBehaviour
    {
        [SerializeField] private string instanceId;
        public string Id => instanceId;
        public void SetId(string value) => instanceId = value;
        public static string Read(Component obj)
        {
            if (obj == null) return null;
            var identity = obj.GetComponent<ThemedWorldInstance>();
            return identity != null ? identity.Id : null;
        }
        public static void Stamp(Component obj, string id)
        {
            if (obj == null || string.IsNullOrEmpty(id)) return;
            var identity = obj.GetComponent<ThemedWorldInstance>();
            if (identity == null) identity = obj.gameObject.AddComponent<ThemedWorldInstance>();
            identity.SetId(id);
        }
    }
}
