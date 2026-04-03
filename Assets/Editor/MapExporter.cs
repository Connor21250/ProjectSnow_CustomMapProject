using UnityEngine;
using UnityEditor;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

/// <summary>
/// Map Exporter
/// Place in Assets/Editor/MapExporter.cs
/// Access via: Tools > Map Exporter
///
/// Exports ONLY the Assets folder, renamed to your map name inside the zip.
/// e.g. mapName = "ForestMap" → zip contains ForestMap/ instead of Assets/
/// </summary>
public class MapExporter : EditorWindow
{
    private string modName     = "MyMap";
    private string outputFolder = "";
    private string customExcludePatterns = "*.tmp,*.log,*.csproj,*.sln";

    private Vector2 scroll;
    private string  lastExportPath = "";
    private bool    exportDone     = false;

    [MenuItem("Tools/Map Exporter")]
    public static void ShowWindow()
    {
        var window = GetWindow<MapExporter>("Map Exporter");
        window.minSize = new Vector2(420, 340);
    }

    private void OnEnable()
    {
        outputFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        modName      = PlayerSettings.productName.Replace(" ", "_");
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        // Header
        var header = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 };
        GUILayout.Space(8);
        GUILayout.Label("Map Exporter", header);
        GUILayout.Label("Exports your Assets folder as the map name, ready for mod.io", EditorStyles.miniLabel);
        GUILayout.Space(14);

        // Map name
        EditorGUILayout.LabelField("Map Name", EditorStyles.boldLabel);
        modName = EditorGUILayout.TextField("Name", modName);
        EditorGUILayout.HelpBox(
            $"ZIP:  {modName}.zip\n" +
            $"Inside the zip the folder will be named:  {modName}/",
            MessageType.None);
        GUILayout.Space(10);

        // Output folder
        EditorGUILayout.LabelField("Save ZIP To", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        outputFolder = EditorGUILayout.TextField(outputFolder);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string picked = EditorUtility.OpenFolderPanel("Choose output folder", outputFolder, "");
            if (!string.IsNullOrEmpty(picked)) outputFolder = picked;
        }
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(10);

        // Exclude patterns
        EditorGUILayout.LabelField("Exclude File Patterns (comma-separated)", EditorStyles.boldLabel);
        customExcludePatterns = EditorGUILayout.TextField(customExcludePatterns);
        EditorGUILayout.HelpBox("Only the Assets/ folder is exported. .meta files are included so Unity can reimport correctly.", MessageType.Info);
        GUILayout.Space(14);

        // Export button
        GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
        if (GUILayout.Button("Export ZIP for Project Snow", GUILayout.Height(42)))
            RunExport();
        GUI.backgroundColor = Color.white;

        // Result
        if (exportDone && !string.IsNullOrEmpty(lastExportPath))
        {
            GUILayout.Space(8);
            EditorGUILayout.HelpBox("Export complete!\n" + lastExportPath, MessageType.Info);
            if (GUILayout.Button("Reveal in Explorer / Finder"))
                EditorUtility.RevealInFinder(lastExportPath);
        }

        EditorGUILayout.EndScrollView();
    }

    private void RunExport()
    {
        exportDone = false;

        // ── Validate ──────────────────────────────────────────
        string safeName = modName.Trim();
        if (string.IsNullOrWhiteSpace(safeName))
        {
            EditorUtility.DisplayDialog("Error", "Please enter a map name.", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(outputFolder) || !Directory.Exists(outputFolder))
        {
            EditorUtility.DisplayDialog("Error", "Output folder does not exist. Please choose a valid folder.", "OK");
            return;
        }

        // ── Paths ─────────────────────────────────────────────
        string assetsDir = Application.dataPath;            // …/YourProject/Assets
        string zipPath   = Path.Combine(outputFolder, safeName + ".zip");

        if (!Directory.Exists(assetsDir))
        {
            EditorUtility.DisplayDialog("Error", "Could not locate the Assets folder.", "OK");
            return;
        }

        if (File.Exists(zipPath)) File.Delete(zipPath);

        // ── Build excluded extensions set ─────────────────────
        var excludeExts = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(customExcludePatterns))
        {
            foreach (var pat in customExcludePatterns.Split(','))
            {
                string p = pat.Trim();
                if (p.StartsWith("*."))
                    excludeExts.Add(p.Substring(1)); // ".tmp", ".log", etc.
            }
        }

        // ── Write zip ─────────────────────────────────────────
        try
        {
            var allFiles = Directory.GetFiles(assetsDir, "*", SearchOption.AllDirectories);
            int total    = allFiles.Length;
            int done     = 0;

            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                foreach (var file in allFiles)
                {
                    // Skip excluded extensions
                    if (excludeExts.Contains(Path.GetExtension(file))) continue;

                    // Build the entry path:
                    // Original:  Assets/Scenes/MyScene.unity
                    // In zip:    MyMap/Scenes/MyScene.unity
                    string relativePath = file.Substring(assetsDir.Length)
                                             .TrimStart(Path.DirectorySeparatorChar,
                                                        Path.AltDirectorySeparatorChar)
                                             .Replace('\\', '/');

                    string entryName = safeName + "/" + relativePath;

                    EditorUtility.DisplayProgressBar(
                        "Exporting map…",
                        entryName,
                        (float)done / Mathf.Max(1, total));

                    zip.CreateEntryFromFile(file, entryName, System.IO.Compression.CompressionLevel.Optimal);
                    done++;
                }
            }

            EditorUtility.ClearProgressBar();
            lastExportPath = zipPath;
            exportDone     = true;
            Repaint();
            Debug.Log($"[Map Exporter] Done → {zipPath}  ({done} files, folder: {safeName}/)");
        }
        catch (System.Exception ex)
        {
            EditorUtility.ClearProgressBar();
            if (File.Exists(zipPath)) File.Delete(zipPath);
            EditorUtility.DisplayDialog("Export Failed", ex.Message, "OK");
            Debug.LogError("[Map Exporter] " + ex);
        }
    }
}