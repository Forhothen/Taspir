using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Fits continuous sheer tulle to the ceiling rail and windowsill without rebuilding the lobby.</summary>
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
            if (root.name == GroupName) UnityEngine.Object.DestroyImmediate(root);
        string materialPath = Root + "Materials/Curtain.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            var shader = Shader.Find("Taspir/Lobby/Curtain");
            if (shader == null || ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("Curtain shader failed to compile.");
            material = new Material(shader) { name = "Curtain" };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        material.SetTexture("_BaseMap", null);
        material.SetVector("_TextureRect", new Vector4(0, 0, 1, 1));
        material.SetColor("_Tint", new Color(1f, 0.97f, 0.9f, 0.3f));
        material.SetFloat("_Amplitude", 0.22f);
        material.SetFloat("_Period", 6.5f);
        EditorUtility.SetDirty(material);
        var group = new GameObject(GroupName);
        Panel(group.transform, material, "Continuous sheer tulle - ceiling to sill", false,
            new Vector2(-0.015f, 1.337f), new Vector2(0.601f, 0.827f),
            new Vector2(-0.015f, 0.472f), new Vector2(0.614f, 0.527f), 1, 0);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void Panel(Transform parent, Material material, string name, bool flip,
        Vector2 topLeft, Vector2 topRight, Vector2 bottomLeft, Vector2 bottomRight,
        float perspectiveScale, float phase)
    {
        const int columns = 72, rows = 24;
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
            // Perspective compresses the folds toward the far end of the window.
            float fabricU = -Mathf.Log(1 - 0.67f * u) / -Mathf.Log(0.33f);
            uv[i] = new Vector2(fabricU, v);
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
        var mesh = new Mesh { name = "Tulle", vertices = vertices, uv = uv, uv2 = motion, triangles = triangles };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        // Include shader displacement in CPU culling bounds.
        var bounds = mesh.bounds;
        bounds.Expand(0.7f);
        mesh.bounds = bounds;
        string meshPath = Root + "Meshes/" + "Tulle.asset";
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
