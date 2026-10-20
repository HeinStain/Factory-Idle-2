using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// Imports ItemData.craftTicks from a CSV with columns: item,ticks
// Matches rows to ItemData assets by displayName (case-insensitive).
// Never deletes or renames anything, and re-running with the same CSV changes nothing.
public static class CraftTimesImporter
{
    private const string LogPrefix = "[Import Craft Times]";

    private struct CsvRow
    {
        public string name;
        public int ticks;
        public int line;
    }

#region Menu

    [MenuItem("Tools/Factory Architect/Import Craft Times")]
    private static void Import()
    {
        string path = EditorUtility.OpenFilePanel("Import Craft Times", "", "csv");
        if (string.IsNullOrEmpty(path)) return;

        // 0 = Apply, 1 = Cancel, 2 = Dry Run
        int choice = EditorUtility.DisplayDialogComplex(
            "Import Craft Times",
            $"Import craft times from:\n{path}\n\nDry Run only logs what would change.",
            "Apply", "Cancel", "Dry Run");

        if (choice == 1) return;

        Run(path, dryRun: choice == 2);
    }

#endregion

#region Import

    private static void Run(string csvPath, bool dryRun)
    {
        string mode = dryRun ? "DRY RUN" : "APPLY";

        // Read CSV
        Dictionary<string, CsvRow> rows = ReadCsv(csvPath, out List<string> invalidRows, out List<string> duplicateRows);

        // Load every ItemData asset
        List<ItemData> items = AssetDatabase.FindAssets("t:ItemData")
            .Select(guid => AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(item => item != null)
            .ToList();

        // Warn about assets that share a displayName, since one CSV row will update all of them
        List<string> sharedNames = items
            .Where(item => !string.IsNullOrWhiteSpace(item.displayName))
            .GroupBy(item => item.displayName.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => $"  \"{group.Key}\": {string.Join(", ", group.Select(AssetDatabase.GetAssetPath))}")
            .ToList();

        List<string> updated = new List<string>();
        List<string> noCsvRow = new List<string>();
        HashSet<string> matchedRows = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int unchanged = 0;

        foreach (ItemData item in items)
        {
            string assetPath = AssetDatabase.GetAssetPath(item);
            string key = item.displayName?.Trim() ?? "";

            if (key.Length == 0 || !rows.TryGetValue(key, out CsvRow row))
            {
                noCsvRow.Add($"  {(key.Length == 0 ? "(empty displayName)" : key)}  ({assetPath})");
                continue;
            }

            matchedRows.Add(key);

            SerializedObject serializedItem = new SerializedObject(item);
            SerializedProperty craftTicks = serializedItem.FindProperty("craftTicks");
            if (craftTicks == null)
            {
                Debug.LogError($"{LogPrefix} ItemData has no serialized craftTicks field. Aborting.");
                return;
            }

            int oldTicks = craftTicks.intValue;
            if (oldTicks == row.ticks) { unchanged++; continue; }

            if (!dryRun)
            {
                craftTicks.intValue = row.ticks;
                serializedItem.ApplyModifiedProperties();
                EditorUtility.SetDirty(item);
            }

            updated.Add($"  {item.displayName}: {oldTicks} -> {row.ticks}  ({assetPath})");
        }

        if (!dryRun && updated.Count > 0) AssetDatabase.SaveAssets();

        List<string> unmatchedRows = rows.Values
            .Where(row => !matchedRows.Contains(row.name))
            .OrderBy(row => row.line)
            .Select(row => $"  line {row.line}: {row.name},{row.ticks}")
            .ToList();

        // Logs
        Debug.Log($"{LogPrefix} {mode}: {Path.GetFileName(csvPath)}, {rows.Count} rows, {items.Count} ItemData assets. " +
                  $"{(dryRun ? "Would update" : "Updated")} {updated.Count}, unchanged {unchanged}, " +
                  $"unmatched rows {unmatchedRows.Count}, assets without a row {noCsvRow.Count}.");

        Debug.Log(FormatList($"{LogPrefix} {mode}: items {(dryRun ? "that would be updated" : "updated")} (old -> new)", updated));

        LogListIfAny($"{LogPrefix} {mode}: CSV rows with no matching ItemData", unmatchedRows);
        LogListIfAny($"{LogPrefix} {mode}: ItemData assets with no CSV row", noCsvRow);
        LogListIfAny($"{LogPrefix} {mode}: invalid CSV rows (skipped)", invalidRows);
        LogListIfAny($"{LogPrefix} {mode}: duplicate CSV rows (first row kept)", duplicateRows);
        LogListIfAny($"{LogPrefix} {mode}: ItemData assets sharing a displayName (all get the same value)", sharedNames);
    }

#endregion

#region CSV

    private static Dictionary<string, CsvRow> ReadCsv(string csvPath, out List<string> invalidRows, out List<string> duplicateRows)
    {
        Dictionary<string, CsvRow> rows = new Dictionary<string, CsvRow>(StringComparer.OrdinalIgnoreCase);
        invalidRows = new List<string>();
        duplicateRows = new List<string>();

        string[] lines = File.ReadAllLines(csvPath);
        bool firstRow = true;

        for (int i = 0; i < lines.Length; i++)
        {
            int lineNumber = i + 1;
            string line = lines[i].Trim();
            if (line.Length == 0) continue;

            // Split on the last comma so item names may contain commas
            int comma = line.LastIndexOf(',');
            string name = comma >= 0 ? Unquote(line.Substring(0, comma)) : Unquote(line);
            string ticksText = comma >= 0 ? Unquote(line.Substring(comma + 1)) : "";

            // Skip the header row
            if (firstRow)
            {
                firstRow = false;
                if (name.Equals("item", StringComparison.OrdinalIgnoreCase) && ticksText.Equals("ticks", StringComparison.OrdinalIgnoreCase)) continue;
            }

            if (name.Length == 0 || !int.TryParse(ticksText, out int ticks) || ticks < 0)
            {
                invalidRows.Add($"  line {lineNumber}: {lines[i]}");
                continue;
            }

            if (rows.TryGetValue(name, out CsvRow existing))
            {
                duplicateRows.Add($"  line {lineNumber}: {name},{ticks} (kept line {existing.line}: {existing.ticks})");
                continue;
            }

            rows[name] = new CsvRow { name = name, ticks = ticks, line = lineNumber };
        }

        return rows;
    }

    private static string Unquote(string value)
    {
        value = value.Trim();
        if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
            value = value.Substring(1, value.Length - 2).Replace("\"\"", "\"").Trim();
        return value;
    }

#endregion

#region Logging

    private static string FormatList(string header, List<string> entries)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"{header} [{entries.Count}]");
        if (entries.Count == 0) builder.AppendLine("  (none)");
        foreach (string entry in entries) builder.AppendLine(entry);
        return builder.ToString();
    }

    private static void LogListIfAny(string header, List<string> entries)
    {
        if (entries.Count == 0) { Debug.Log($"{header} [0]"); return; }
        Debug.LogWarning(FormatList(header, entries));
    }

#endregion
}
