using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Editor authoring helper; the saved scene has no dependency on this builder.</summary>
public static class LobbyBalconySceneBuilder
{
    public const string ScenePath = "Assets/Scenes/LobbyBalcony.unity";
    private const string Root = "Assets/LobbyBalcony/";
    private const string ImagePath = Root + "taspir_balcony_morning_transparent_1980px.png";
    private const float Width = 40f / 3f;

    [MenuItem("Taspir/Create Lobby Balcony")]
    public static void CreateFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (File.Exists(ScenePath))
        {
            EditorSceneManager.OpenScene(ScenePath);
            return;
        }
        CreateScene();
    }

    public static void CreateScene()
    {
        AssetDatabase.Refresh();
        var importer = AssetImporter.GetAtPath(ImagePath) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Balcony PNG is missing.");
        // Only import settings change. The source PNG stays byte-for-byte intact.
        importer.textureType = TextureImporterType.Default;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        var image = AssetDatabase.LoadAssetAtPath<Texture2D>(ImagePath);
        if (Mathf.Abs((float)image.width / image.height - 4f / 3f) > 0.001f)
            throw new InvalidOperationException("Expected the original 4:3 balcony composition.");

        var sky = Material("MorningClouds", "Taspir/Lobby/Morning Clouds");
        var balcony = Material("Balcony", "Taspir/Lobby/Balcony Unlit");
        var light = Material("MorningLight", "Taspir/Lobby/Morning Light");
        balcony.SetTexture("_BaseMap", image);
        light.SetTexture("_BaseMap", image);
        EditorUtility.SetDirty(balcony);
        EditorUtility.SetDirty(light);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.skybox = null;
        RenderSettings.fog = false;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.black;

        var cameraObject = new GameObject("Lobby Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0, 0, -10);
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 30;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.027f, 0.022f, 1);
        camera.allowHDR = false;
        camera.allowMSAA = false;
        camera.useOcclusionCulling = false;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<LobbyCameraFit>();
        var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = false;
        cameraData.renderShadows = false;
        cameraData.requiresColorOption = CameraOverrideOption.Off;
        cameraData.requiresDepthOption = CameraOverrideOption.Off;

        var outside = new GameObject("Outside - replace this group for a new location");
        Quad("Morning sky and gently swaying clouds", outside.transform, 2, sky, 0);
        var room = new GameObject("Balcony artwork");
        Quad("Original balcony PNG", room.transform, 0, balcony, 10);
        var lighting = new GameObject("Soft morning light - painted surface overlay");
        Quad("Window rays and wall glow", lighting.transform, -0.1f, light, 20);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Taspir: saved " + ScenePath + ". Startup scene and render settings are unchanged.");
    }

    private static Material Material(string name, string shaderName)
    {
        var shader = Shader.Find(shaderName);
        if (shader == null || ShaderUtil.ShaderHasError(shader))
            throw new InvalidOperationException("Missing or invalid shader: " + shaderName);
        string path = Root + "Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(shader) { name = name };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void Quad(string name, Transform parent, float z, Material material, int order)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        obj.name = name;
        UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = new Vector3(0, 0, z);
        obj.transform.localScale = new Vector3(Width, 10, 1);
        var renderer = obj.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.sortingOrder = order;
    }

    // Batch entry point: compiles scripts/shaders, creates the scene and renders QA images.
    public static void CreateAndValidate()
    {
        CreateScene();
        Validate();
    }

    public static void Validate()
    {
        EditorSceneManager.OpenScene(ScenePath);
        string output = Path.Combine(Path.GetTempPath(), "TaspirLobbyQA");
        Directory.CreateDirectory(output);
        int originalQuality = QualitySettings.GetQualityLevel();
        var sky = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/MorningClouds.mat");
        var light = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/MorningLight.mat");
        try
        {
            for (int quality = 0; quality < QualitySettings.names.Length; quality++)
            {
                QualitySettings.SetQualityLevel(quality, true);
                sky.SetFloat("_PreviewTime", 0);
                light.SetFloat("_PreviewTime", 0);
                Capture(output, QualitySettings.names[quality] + "-4x3", 1200, 900);
                sky.SetFloat("_PreviewTime", 5);
                light.SetFloat("_PreviewTime", 5);
                Capture(output, QualitySettings.names[quality] + "-4x3-t5", 1200, 900);
            }
            Capture(output, "wide-16x9", 1600, 900);
            Capture(output, "portrait-9x16", 675, 1200);
            foreach (string name in new[] { "BalconyUnlit", "MorningClouds", "MorningLight" })
            {
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "Shaders/" + name + ".shader");
                foreach (var message in ShaderUtil.GetShaderMessages(shader))
                {
                    if (message.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)
                        throw new InvalidOperationException(shader.name + ": " + message.message);
                }
            }
            Debug.Log("LOBBY VALIDATION PASSED: both quality pipelines, animation times and aspect ratios. Captures: " + output);
        }
        finally
        {
            sky.SetFloat("_PreviewTime", -1);
            light.SetFloat("_PreviewTime", -1);
            QualitySettings.SetQualityLevel(originalQuality, true);
            EditorUtility.SetDirty(sky);
            EditorUtility.SetDirty(light);
            AssetDatabase.SaveAssets();
        }
    }

    private static void Capture(string directory, string name, int width, int height)
    {
        var camera = Camera.main;
        var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
        var previous = RenderTexture.active;
        var image = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.GetComponent<LobbyCameraFit>().Fit();
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
            RenderPipeline.SubmitRenderRequest(camera, request);
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(directory, name + ".png"), image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
