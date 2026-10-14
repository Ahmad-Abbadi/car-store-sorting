using System.Collections.Generic;
using UnityEngine;

namespace TinyRiders
{
    [CreateAssetMenu(menuName = "Tiny Riders/Car Catalog")]
    public sealed class CarCatalog : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] List<CarVariantDefinition> entries = new List<CarVariantDefinition>();
        Dictionary<string, CarVariantDefinition> byId;
        public IReadOnlyList<CarVariantDefinition> Entries => entries;

        public bool TryGetById(string variantId, out CarVariantDefinition definition)
        {
            if (byId == null) RebuildLookup();
            return byId.TryGetValue(variantId ?? string.Empty, out definition);
        }

        void RebuildLookup()
        {
            byId = new Dictionary<string, CarVariantDefinition>(System.StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.variantId) && !byId.ContainsKey(entry.variantId))
                    byId.Add(entry.variantId, entry);
            }
        }

        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize() { byId = null; }

#if UNITY_EDITOR
        public void SetEntries(List<CarVariantDefinition> validatedEntries)
        {
            entries = validatedEntries ?? new List<CarVariantDefinition>();
            RebuildLookup();
        }
#endif
    }
}
