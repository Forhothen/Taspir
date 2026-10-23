using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Editor-only helper. The saved scene works without running this script again.
public static class FirstPersonSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/FirstPersonRoom.unity";

    [MenuItem("Taspir/Create First Person Room")]
    public static void CreateScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var normalImporter = (TextureImporter)AssetImporter.GetAtPath("Assets/Textures/ForestGround/ForestGround_Normal.jpg");
        normalImporter.textureType = TextureImporterType.NormalMap;
        normalImporter.SaveAndReimport();

        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ForestGround.mat");
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, "Assets/Materials/ForestGround.mat");
        }
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/ForestGround/ForestGround_Color.jpg"));
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/ForestGround/ForestGround_Normal.jpg"));
        material.EnableKeyword("_NORMALMAP");
        material.SetFloat("_Smoothness", 0.05f);
        material.SetTextureScale("_BaseMap", new Vector2(5, 5));
        material.SetTextureScale("_BumpMap", new Vector2(5, 5));
        EditorUtility.SetDirty(material);

        var room = new GameObject("Room (10 x 10 x 10 metres)");
        CreateWall("Floor", new Vector3(0, -0.1f, 0), new Vector3(10.4f, 0.2f, 10.4f), room.transform, material);
        CreateWall("Ceiling", new Vector3(0, 10.1f, 0), new Vector3(10.4f, 0.2f, 10.4f), room.transform, material);
        CreateWall("North Wall", new Vector3(0, 5, 5.1f), new Vector3(10.4f, 10, 0.2f), room.transform, material);
        CreateWall("South Wall", new Vector3(0, 5, -5.1f), new Vector3(10.4f, 10, 0.2f), room.transform, material);
        CreateWall("East Wall", new Vector3(5.1f, 5, 0), new Vector3(0.2f, 10, 10.4f), room.transform, material);
        CreateWall("West Wall", new Vector3(-5.1f, 5, 0), new Vector3(0.2f, 10, 10.4f), room.transform, material);

        var player = new GameObject("Player");
        player.transform.position = new Vector3(0, 0.05f, -2);
        var controller = player.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.3f;
        controller.center = new Vector3(0, 0.9f, 0);
        controller.stepOffset = 0.25f;
        var cameraObject = new GameObject("Player Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(player.transform, false);
        cameraObject.transform.localPosition = new Vector3(0, 1.65f, 0);
        var camera = cameraObject.AddComponent<Camera>();
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 60;
        camera.fieldOfView = 75;
        cameraObject.AddComponent<AudioListener>();
        player.AddComponent<FirstPersonController>();

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.45f);
        var lightObject = new GameObject("Room Light");
        lightObject.transform.position = new Vector3(0, 6, 0);
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 20;
        light.intensity = 5;
        light.shadows = LightShadows.None;

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("Taspir: FirstPersonRoom created and set as the startup scene.");
    }

    private static void CreateWall(string name, Vector3 position, Vector3 size, Transform parent, Material material)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); // Includes a BoxCollider.
        wall.name = name;
        wall.transform.SetParent(parent);
        wall.transform.position = position;
        wall.transform.localScale = size;
        wall.GetComponent<Renderer>().sharedMaterial = material;
    }
}
