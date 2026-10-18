using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>Locales are already included in the startup resources; no blocking locale load on WebGL.</summary>
[Serializable]
public sealed class GameLocalesProvider : ILocalesProvider
{
    [SerializeField] private List<Locale> locales = new List<Locale>();
    public GameLocalesProvider(List<Locale> locales) { this.locales = new List<Locale>(locales); }
    public List<Locale> Locales => locales;
    public Locale GetLocale(LocaleIdentifier id) => locales.Find(l => l.Identifier == id);
    public void AddLocale(Locale locale) { if (!locales.Contains(locale)) locales.Add(locale); }
    public bool RemoveLocale(Locale locale) => locales.Remove(locale);
}

/// <summary>Unity LocalizedString and the legacy facade share the same loaded StringTable assets.</summary>
[Serializable]
public sealed class GameStringTableProvider : ITableProvider
{
    [SerializeField] private StringTable[] tables;
    public GameStringTableProvider(StringTable[] tables) { this.tables = tables; }
    public AsyncOperationHandle<TTable> ProvideTableAsync<TTable>(string tableCollectionName, Locale locale) where TTable : LocalizationTable
    {
        var table = Array.Find(tables, t => t.TableCollectionName == tableCollectionName && t.LocaleIdentifier == locale.Identifier);
        return table is TTable typed ? Addressables.ResourceManager.CreateCompletedOperation(typed, null) : default;
    }
}
