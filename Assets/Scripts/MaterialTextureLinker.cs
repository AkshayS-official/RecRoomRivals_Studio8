using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class MaterialTextureLinker : EditorWindow
{
    [MenuItem("Tools/Auto-Link Arena Textures")]
    static void LinkTextures()
    {
        // Arena materials
        string matPath = "Assets/MarpaStudio/Built-In/Materials";
        string texPath = "Assets/MarpaStudio/Textures";

        var overrides = new Dictionary<string, string>()
        {
            { "PlaneSeat",            "SeatPlane" },
            { "FoamPole",             "FoamPoleFinal_DefaultMaterial" },
            { "LongField",            "LongWall" },
            { "SecondFloorSeatsBase", "SecondFloorSeatsBase" },
            { "TopFloorSeatBase",     "TopFloorSeatBase" },
            { "SeatEntrance",         "SeatEntrance" },
        };

        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { matPath });
        foreach (string guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            string matName = Path.GetFileNameWithoutExtension(path);
            string texName = overrides.ContainsKey(matName) ? overrides[matName] : matName;

            TrySet(mat, texPath, texName + "_Albedo", "_BaseMap");
            TrySet(mat, texPath, texName + "_Normal", "_BumpMap");
            TrySet(mat, texPath, texName + "_Metallic", "_MetallicGlossMap");
            TrySet(mat, texPath, texName + "_Height", "_ParallaxMap");
            TrySet(mat, texPath, texName + "_AO", "_OcclusionMap");
            TrySet(mat, texPath, texName + "_Emissive", "_EmissionMap");

            EditorUtility.SetDirty(mat);
        }

        // Basketball materials
        string ballMatPath = "Assets/TierrasDeRol/Basketball/Built-In/Materials";
        string ballTexPath = "Assets/TierrasDeRol/Basketball/Textures";

        var ballOverrides = new Dictionary<string, string>()
        {
            { "BlackBasketball",         "Albedo3" },
            { "OrangeBasketball",        "Albedo1" },
            { "RedWhiteBlueBasketball",  "Albedo2" },
        };

        string[] ballMatGuids = AssetDatabase.FindAssets("t:Material", new[] { ballMatPath });
        foreach (string guid in ballMatGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            string matName = Path.GetFileNameWithoutExtension(path);

            if (ballOverrides.ContainsKey(matName))
            {
                TrySet(mat, ballTexPath, ballOverrides[matName], "_BaseMap");
                TrySet(mat, ballTexPath, "Normal", "_BumpMap");
                EditorUtility.SetDirty(mat);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("✓ All arena + basketball textures linked!");
    }

    static void TrySet(Material mat, string texPath, string search, string prop)
    {
        string[] guids = AssetDatabase.FindAssets(search + " t:Texture2D", new[] { texPath });
        if (guids.Length > 0)
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));
            if (tex != null) mat.SetTexture(prop, tex);
        }
    }
}