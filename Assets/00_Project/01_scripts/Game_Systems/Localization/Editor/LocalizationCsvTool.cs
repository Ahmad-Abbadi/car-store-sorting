#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary> Menu tools that import and export the localization CSV. </summary>
public static class LocalizationCsvTool
{
    private const string AssetPath = "Assets/00_Project/03_scriptableObjects/Resources/LocalizationDatabase.asset";

    /// <summary> Loads the database asset, creating it when missing. </summary>
    public static LocalizationDatabase_SO LoadOrCreateDatabase()
    {
        var db = AssetDatabase.LoadAssetAtPath<LocalizationDatabase_SO>(AssetPath);
        if (db != null) return db;

        db = ScriptableObject.CreateInstance<LocalizationDatabase_SO>();
        AssetDatabase.CreateAsset(db, AssetPath);
        return db;
    }

    [MenuItem("Tools/Localization/Import CSV...")]
    private static void ImportMenu()
    {
        string path = EditorUtility.OpenFilePanel("Import localization CSV", "", "csv");
        if (string.IsNullOrEmpty(path)) return;
        Import(File.ReadAllText(path, Encoding.UTF8));
    }

    [MenuItem("Tools/Localization/Export CSV...")]
    private static void ExportMenu()
    {
        string path = EditorUtility.SaveFilePanel("Export localization CSV", "", "localization", "csv");
        if (string.IsNullOrEmpty(path)) return;
        File.WriteAllText(path, Export(LoadOrCreateDatabase()), new UTF8Encoding(true));
    }

    /// <summary> Header is key,code,code...; columns named #... are ignored. </summary>
    public static void Import(string csv)
    {
        List<List<string>> rows = Parse(csv);
        if (rows.Count < 2 || rows[0].Count < 2 || rows[0][0].Trim().ToLowerInvariant() != "key")
        {
            Debug.LogError("Localization import: first header cell must be 'key' followed by language codes.");
            return;
        }

        List<string> header = rows[0];
        var columns = new List<LocalizationDatabase_SO.Column>();
        var columnIndex = new List<int>();
        for (int c = 1; c < header.Count; c++)
        {
            string code = header[c].Trim();
            if (code.Length == 0 || code.StartsWith("#")) continue;
            columns.Add(new LocalizationDatabase_SO.Column());
            columns[columns.Count - 1].Code = code;
            columnIndex.Add(c);
        }

        var keys = new List<string>();
        var seen = new HashSet<string>();
        var values = new List<List<string>>();
        foreach (var _ in columns) values.Add(new List<string>());

        for (int r = 1; r < rows.Count; r++)
        {
            List<string> row = rows[r];
            string key = row.Count > 0 ? row[0].Trim() : string.Empty;
            if (key.Length == 0 || key.StartsWith("#")) continue;
            if (!seen.Add(key))
            {
                Debug.LogWarning($"Localization import: duplicate key '{key}' skipped.");
                continue;
            }

            keys.Add(key);
            for (int i = 0; i < columns.Count; i++)
            {
                int c = columnIndex[i];
                values[i].Add(c < row.Count ? row[c] : string.Empty);
            }
        }

        for (int i = 0; i < columns.Count; i++) columns[i].Values = values[i].ToArray();

        LocalizationDatabase_SO db = LoadOrCreateDatabase();
        db.SetTable(keys.ToArray(), columns);
        AssetDatabase.SaveAssets();
        Debug.Log($"Localization import: {keys.Count} keys, {columns.Count} languages.");
    }

    /// <summary> CSV text of the whole database, ready for a spreadsheet. </summary>
    public static string Export(LocalizationDatabase_SO db)
    {
        var sb = new StringBuilder();
        sb.Append("key");
        foreach (var column in db.Columns) sb.Append(',').Append(Escape(column.Code));
        sb.Append('\n');

        string[] keys = db.Keys;
        for (int i = 0; i < keys.Length; i++)
        {
            sb.Append(Escape(keys[i]));
            foreach (var column in db.Columns)
                sb.Append(',').Append(Escape(i < column.Values.Length ? column.Values[i] : string.Empty));
            sb.Append('\n');
        }
        return sb.ToString();
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        bool needsQuotes = value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
        return needsQuotes ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
    }

    /// <summary> Splits CSV text into rows of cells; handles quotes and line breaks inside quotes. </summary>
    private static List<List<string>> Parse(string text)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var cell = new StringBuilder();
        bool inQuotes = false;

        if (text.Length > 0 && text[0] == '﻿') text = text.Substring(1);

        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                    else inQuotes = false;
                }
                else cell.Append(ch);
                continue;
            }

            if (ch == '"') inQuotes = true;
            else if (ch == ',') { row.Add(cell.ToString()); cell.Length = 0; }
            else if (ch == '\n' || ch == '\r')
            {
                if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                row.Add(cell.ToString());
                cell.Length = 0;
                if (row.Count > 1 || row[0].Length > 0) rows.Add(row);
                row = new List<string>();
            }
            else cell.Append(ch);
        }

        if (cell.Length > 0 || row.Count > 0)
        {
            row.Add(cell.ToString());
            rows.Add(row);
        }
        return rows;
    }
}
#endif
