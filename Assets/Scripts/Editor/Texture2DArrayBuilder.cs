using System.Linq;
using UnityEditor;
using UnityEngine;

public static class Texture2DArrayBuilder
{
    [MenuItem("Assets/Create/VAT/Texture2DArray from Selection", true)]
    static bool Validate() => Selection.objects.OfType<Texture2D>().Count() >= 1;

    [MenuItem("Assets/Create/VAT/Texture2DArray from Selection")]
    static void CreateArrayFromSelection()
    {
        var slices = Selection.objects.OfType<Texture2D>()
            .OrderBy(t => AssetDatabase.GetAssetPath(t)) // keep a stable order
            .ToArray();

        if (slices.Length == 0) { EditorUtility.DisplayDialog("Texture2DArray", "Select one or more Texture2D assets.", "OK"); return; }

        int w = slices[0].width;
        int h = slices[0].height;
        var fmt = slices[0].format; // must match across all slices

        // Check uniform size/format
        for (int i = 0; i < slices.Length; i++)
        {
            if (slices[i].width != w || slices[i].height != h || slices[i].format != fmt)
            {
                EditorUtility.DisplayDialog(
                    "Mismatch",
                    $"All slices must have identical width/height/format.\n" +
                    $"Offender: {slices[i].name} ({slices[i].width}x{slices[i].height}, {slices[i].format})\n" +
                    $"Expected: {w}x{h}, {fmt}",
                    "OK");
                return;
            }
        }

        // Create array (no mips, linear=true for VAT data)
        var texArray = new Texture2DArray(w, h, slices.Length, fmt, mipChain: false, linear: true)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
            anisoLevel = 0
        };

        // Fast GPU copy (requires identical format)
        for (int i = 0; i < slices.Length; i++)
            Graphics.CopyTexture(slices[i], 0, 0, texArray, i, 0);

        texArray.Apply(false, true);

        // Save as .asset
        string suggested = $"{slices[0].name}_Array";
        string path = EditorUtility.SaveFilePanelInProject("Save Texture2DArray", suggested, "asset", "");
        if (string.IsNullOrEmpty(path)) { Object.DestroyImmediate(texArray); return; }

        AssetDatabase.CreateAsset(texArray, path);
        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(texArray);
        Debug.Log($"Saved Texture2DArray: {path}  (slices: {slices.Length}, {w}x{h}, {fmt})");
    }
}
