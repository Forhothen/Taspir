using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Adds two perspective-fitted fabric panels without rebuilding the lobby.</summary>
public static class LobbyCurtainBuilder
{
    private const string Root = "Assets/LobbyBalcony/";
    private const string GroupName = "Curtains - replaceable placeholder";

    [MenuItem("Taspir/Add Lobby Curtains")]
    public static void AddFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        AddToScene();
    }

    public static void AddToScene()
    {
        AssetDatabase.Refresh();
        var scene = EditorSceneManager.OpenScene(LobbyBalconySceneBuilder.ScenePath);
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == GroupName)
            {
                Debug.Log("Curtains already exist; keeping the current panels and material.");
                return;
            }
        const string texturePath = Root + "Textures/CurtainPlaceholder.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.textureType = TextureImporterType.Default;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.sRGBTexture = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        string materialPath = Root + "Materials/Curtain.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            var shader = Shader.Find("Taspir/Lobby/Curtain");
            if (shader == null || ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("Curtain shader failed to compile.");
            material = new Material(shader) { name = "Curtain" };
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            AssetDatabase.CreateAsset(material, materialPath);
        }
        var group = new GameObject(GroupName);
        // Image coordinates: origin bottom-left. The near window top is above the crop.
        Panel(group.transform, material, "Near left curtain", false,
            new Vector2(-0.035f, 1.34f), new Vector2(0.19f, 1.155f),
            new Vector2(-0.035f, -0.25f), new Vector2(0.19f, -0.08f), 1, 0);
        Panel(group.transform, material, "Far right curtain", true,
            new Vector2(0.49f, 0.91f), new Vector2(0.61f, 0.81f),
            new Vector2(0.49f, 0.10f), new Vector2(0.61f, 0.19f), 0.5f, 1.4f);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void Panel(Transform parent, Material material, string name, bool flip,
        Vector2 topLeft, Vector2 topRight, Vector2 bottomLeft, Vector2 bottomRight,
        float perspectiveScale, float phase)
    {
        const int columns = 20, rows = 32;
        var vertices = new Vector3[(columns + 1) * (rows + 1)];
        var uv = new Vector2[vertices.Length];
        var motion = new Vector2[vertices.Length];
        var triangles = new int[columns * rows * 6];
        for (int y = 0; y <= rows; y++)
        for (int x = 0; x <= columns; x++)
        {
            float u = (float)x / columns, v = (float)y / rows;
            var point = Vector2.Lerp(Vector2.Lerp(bottomLeft, bottomRight, u),
                Vector2.Lerp(topLeft, topRight, u), v);
            int i = y * (columns + 1) + x;
            vertices[i] = new Vector3((point.x - 0.5f) * (40f / 3f), (point.y - 0.5f) * 10, 0);
            uv[i] = new Vector2(flip ? 1 - u : u, v);
            motion[i] = new Vector2(perspectiveScale, phase);
            if (x == columns || y == rows) continue;
            int t = (y * columns + x) * 6;
            triangles[t] = i;
            triangles[t + 1] = i + columns + 1;
            triangles[t + 2] = i + 1;
            triangles[t + 3] = i + 1;
            triangles[t + 4] = i + columns + 1;
            triangles[t + 5] = i + columns + 2;
        }
        var mesh = new Mesh { name = flip ? "FarCurtain" : "NearCurtain", vertices = vertices, uv = uv, uv2 = motion, triangles = triangles };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        // Include shader displacement in CPU culling bounds.
        var bounds = mesh.bounds;
        bounds.Expand(0.7f);
        mesh.bounds = bounds;
        string meshPath = Root + "Meshes/" + (flip ? "FarCurtain.asset" : "NearCurtain.asset");
        if (!AssetDatabase.IsValidFolder(Root + "Meshes")) AssetDatabase.CreateFolder(Root.TrimEnd('/'), "Meshes");
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (existing == null) AssetDatabase.CreateAsset(mesh, meshPath);
        else
        {
            existing.Clear();
            existing.vertices = mesh.vertices;
            existing.uv = mesh.uv;
            existing.uv2 = mesh.uv2;
            existing.triangles = mesh.triangles;
            existing.RecalculateNormals();
            existing.bounds = mesh.bounds;
            existing.name = mesh.name;
            UnityEngine.Object.DestroyImmediate(mesh);
            mesh = existing;
            EditorUtility.SetDirty(mesh);
        }
        var panel = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        panel.transform.SetParent(parent, false);
        panel.transform.localPosition = new Vector3(0, 0, -0.3f);
        panel.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = panel.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingOrder = 30;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    public static void AddAndValidate()
    {
        AddToScene();
        LobbyBalconySceneBuilder.Validate();
    }
}
