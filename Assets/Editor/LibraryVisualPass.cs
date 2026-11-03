using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Breakpoint.Robot;
using Type = System.Type;

/// <summary>One-shot, repeatable art pass for the two playable library scenes.</summary>
public static class LibraryVisualPass
{
    private const string ArtRootName = "LIBRARY_ART_PASS";
    private const string MaterialsPath = "Assets/_Project/Materials/LibraryArt";
    private const string StudentModelPath = "Assets/_Project/Models/male-character-in-caual-clothing/source/base.obj";
    private const string GuardModelPath = "Assets/_Project/Models/security-guard-rigged-3d-model/source/Guard.fbx";
    private const string LibraryModelPath = "Assets/_Project/Models/library-low-poly/source/biblioteca.fbx";
    private const string RobotModelPath = "Assets/_Project/Models/simple-little-robot-model/source/model.gltf";

    private static readonly Color Walnut = new Color(0.18f, 0.105f, 0.075f);
    private static readonly Color Cream = new Color(0.76f, 0.68f, 0.52f);
    private static readonly Color Brass = new Color(0.72f, 0.48f, 0.19f);
    private static readonly Color Teal = new Color(0.035f, 0.32f, 0.34f);
    private static readonly Color FloorColor = new Color(0.39f, 0.31f, 0.23f);
    private static readonly Color[] BookColors =
    {
        new Color(0.42f, 0.12f, 0.09f), new Color(0.11f, 0.25f, 0.29f),
        new Color(0.63f, 0.39f, 0.13f), new Color(0.16f, 0.31f, 0.2f),
        new Color(0.36f, 0.23f, 0.4f), new Color(0.72f, 0.64f, 0.47f)
    };

    [MenuItem("Breakpoint/Art/Build Library Visuals (Corridor + Librarian)")]
    public static void BuildScenes()
    {
        EditorSettings.serializationMode = SerializationMode.ForceText;
        EnsureMaterials();
        BuildCorridor();
        BuildLibrarianScene();
        EditorSceneManager.OpenScene("Assets/Scenes/Level_Corridor.unity", OpenSceneMode.Single);
        AssetDatabase.ForceReserializeAssets(new[]
        {
            "Assets/Scenes/Level_Corridor.unity",
            "Assets/Scenes/Level_Door.unity"
        });
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Library art pass complete: corridor, student model, security guard and librarian desk updated.");
    }

    private static void BuildCorridor()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Level_Corridor.unity", OpenSceneMode.Single);
        GameObject root = NewRoot(scene);
        SetLibraryLighting(root.transform);
        foreach (string legacyName in new[] { "Plane", "SolDuvar", "SagDuvar", "Raf1", "Raf2", "Salter", "LazerBariyeri", "SahneCikisi" })
        {
            GameObject legacy = GameObject.Find(legacyName);
            if (legacy != null) legacy.SetActive(false);
        }
        AddLibraryArchitecture(root.transform);
        AddStoryFloorplan(root.transform);
        AddStudentVisual(scene, root.transform);
        AddRobotVisual(scene);
        ConfigureStoryPlayer(scene);
        GameObject exitTrigger = GameObject.Find("CorridorExitTrigger");
        if (exitTrigger != null)
        {
            exitTrigger.transform.position = new Vector3(0f, 1f, 12.15f);
            BoxCollider exitCollider = exitTrigger.GetComponent<BoxCollider>();
            if (exitCollider != null) { exitCollider.isTrigger = true; exitCollider.size = new Vector3(8.5f, 2.5f, 1f); }
        }

        // Reflection keeps this editor utility independent of the AI Navigation assembly reference.
        Type surfaceType = Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
        if (surfaceType != null)
        {
            Component surface = root.GetComponent(surfaceType);
            if (surface == null) surface = root.AddComponent(surfaceType);
            surfaceType.GetMethod("BuildNavMesh")?.Invoke(surface, null);
        }
        else
        {
            Debug.LogError("AI Navigation package is missing: cannot build the library NavMesh surface.");
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureStoryPlayer(Scene scene)
    {
        PlayerController playerController = Object.FindFirstObjectByType<PlayerController>();
        if (playerController == null) return;
        GameObject player = playerController.gameObject;
        player.transform.position = scene.name == "Level_Corridor"
            ? new Vector3(-1.6f, 4.03f, -5.2f)
            : new Vector3(0f, 0.03f, 0f);
        player.transform.rotation = scene.name == "Level_Corridor"
            ? Quaternion.Euler(0f, 180f, 0f)
            : Quaternion.identity;
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.height = 1.8f; cc.radius = 0.34f; cc.center = new Vector3(0f, 0.9f, 0f); cc.stepOffset = 0.3f;
        }
        foreach (UnityEngine.AI.NavMeshObstacle obstacle in player.GetComponents<UnityEngine.AI.NavMeshObstacle>()) obstacle.enabled = false;
        Transform cam = playerController.oyuncuKamerasi;
        if (cam != null) { cam.localPosition = new Vector3(0f, 1.62f, -3.55f); cam.localRotation = Quaternion.identity; }
        EditorUtility.SetDirty(player);
    }

    private static void AddStoryFloorplan(Transform root)
    {
        Material floor = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Floor_Terrazzo.mat");
        Material wood = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Wood_Walnut.mat");
        Material brass = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Brass.mat");
        Material teal = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Teal_Enamel.mat");
        GameObject upper = Cube(root, "Upper Study Floor", new Vector3(0f, 3.82f, -5.0f), new Vector3(9.6f, .36f, 9.0f), "Floor_Terrazzo");
        upper.GetComponent<Collider>().enabled = true;
        GameObject lower = Cube(root, "Ground Floor", new Vector3(0f, -.18f, 8.5f), new Vector3(9.6f, .36f, 10.5f), "Floor_Terrazzo");
        lower.GetComponent<Collider>().enabled = true;
        // Start at the desk and wake via E.
        Transform desk = NewGroup("Opening_Study_Desk", root);
        Cube(desk, "Desk top", new Vector3(-1.6f, 4.62f, -6.6f), new Vector3(2.1f, .14f, 1.05f), "Wood_Walnut");
        Cube(desk, "Desk legs", new Vector3(-1.6f, 4.28f, -6.6f), new Vector3(1.85f, .62f, .82f), "Wood_Walnut");
        Cube(desk, "Open notebook", new Vector3(-1.6f, 4.72f, -6.5f), new Vector3(.55f, .035f, .42f), "Book_Ivory");
        GameObject wake = new GameObject("Wake_At_Desk_Interactable"); wake.transform.SetParent(root, false); wake.transform.position = new Vector3(-.8f, 4.03f, -5.75f);
        BoxCollider wakeCol = wake.AddComponent<BoxCollider>(); wakeCol.isTrigger = true; wakeCol.size = new Vector3(2.1f, 2f, 1.8f); wakeCol.center = Vector3.up;
        wake.AddComponent<LibraryStoryInteractable>().Configure(LibraryInteractionKind.WakeFromDesk, "E · Masadan kalk", 1);
        AddWorldLabel(root, "GECE ÇALIŞMA SALONU · ÜST KAT", new Vector3(0f, 5.45f, -9.25f), .23f, brass.color);

        // Stair run is walkable geometry, with handrails and a stage trigger at its foot.
        Transform stairs = NewGroup("Staircase_To_Ground_Floor", root);
        const int steps = 15;
        for (int i = 0; i < steps; i++)
        {
            float z = -1.0f + i * .39f, y = 4.0f - (i + 1) * .267f - .15f;
            GameObject step = Cube(stairs, $"Stone stair {i + 1:00}", new Vector3(0f, y, z), new Vector3(2.8f, .3f, .43f), "Floor_Terrazzo");
            step.GetComponent<Collider>().enabled = true;
            Cube(stairs, "Brass stair edge", new Vector3(0f, y + .145f, z - .17f), new Vector3(2.79f, .025f, .035f), "Brass");
        }
        Cube(stairs, "Left stair rail", new Vector3(-1.48f, 2.1f, 1.55f), new Vector3(.09f, 2.9f, 6.1f), "Wood_Walnut");
        Cube(stairs, "Right stair rail", new Vector3(1.48f, 2.1f, 1.55f), new Vector3(.09f, 2.9f, 6.1f), "Wood_Walnut");
        GameObject stairTrigger = new GameObject("Stair_Descent_Trigger"); stairTrigger.transform.SetParent(root, false); stairTrigger.transform.position = new Vector3(0f, .45f, 5.25f);
        BoxCollider stairCol = stairTrigger.AddComponent<BoxCollider>(); stairCol.isTrigger = true; stairCol.size = new Vector3(3.4f, 2f, 1.2f); stairTrigger.AddComponent<StairDescentTrigger>();
        AddWorldLabel(root, "ZEMİN KAT ↓", new Vector3(0f, 2.6f, 5.8f), .22f, brass.color);

        RobotController robot = Object.FindFirstObjectByType<RobotController>();
        GameObject robotRoot = robot != null ? robot.gameObject : null;
        if (robotRoot != null)
        {
            robotRoot.transform.position = new Vector3(1.1f, 4.02f, -3.7f);
            SphereCollider robotInteract = robotRoot.GetComponent<SphereCollider>();
            if (robotInteract == null) robotInteract = robotRoot.AddComponent<SphereCollider>();
            robotInteract.isTrigger = true; robotInteract.radius = 1.2f; robotInteract.center = new Vector3(0f, .45f, 0f);
            LibraryStoryInteractable activate = robotRoot.GetComponent<LibraryStoryInteractable>();
            if (activate == null) activate = robotRoot.AddComponent<LibraryStoryInteractable>();
            activate.Configure(LibraryInteractionKind.ActivateRobot, "E · Tozu temizle / robotu etkinleştir", 3);
            GameObject dust = GameObject.CreatePrimitive(PrimitiveType.Sphere); dust.name = "Dust_Cover"; dust.transform.SetParent(robotRoot.transform, false); dust.transform.localPosition = new Vector3(0f, .62f, 0f); dust.transform.localScale = new Vector3(1.2f,.18f,1.05f); dust.GetComponent<Collider>().enabled = false; dust.GetComponent<Renderer>().sharedMaterial = floor;
            AddWorldLabel(root, "YARDIMCI ROBOT · ÇALIŞMIYOR", new Vector3(1.1f, 5.0f, -3.7f), .15f, brass.color);
        }

        // Ground-floor debris clusters are actual visual obstacles; the robot travels to a clear NavMesh work point.
        AddDebris(root, "Fallen_Shelf_Blockage", -1.15f, 6.4f, robot, "Devrilmiş rafı kaldır");
        AddDebris(root, "Book_Trolley_Blockage", 1.15f, 8.55f, robot, "Kitap arabasını çek");
        AddDebris(root, "Books_And_Tools_Blockage", -.75f, 10.35f, robot, "Kitap ve araç gereçleri topla");
        GameObject guardAsset = AssetDatabase.LoadAssetAtPath<GameObject>(GuardModelPath);
        if (guardAsset != null)
        {
            GameObject guard = (GameObject)PrefabUtility.InstantiatePrefab(guardAsset); guard.name = "SecurityGuard_Exit_Post"; SceneManager.MoveGameObjectToScene(guard, SceneManager.GetActiveScene()); guard.transform.SetParent(root, true); guard.transform.position = new Vector3(0f, 0f, 13.1f); FitModelHeight(guard, 1.82f); guard.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            CapsuleCollider trigger = guard.AddComponent<CapsuleCollider>(); trigger.height=1.8f; trigger.radius=.55f; trigger.center=new Vector3(0f,.9f,0f); trigger.isTrigger=true; guard.AddComponent<LibrarianNPC>();
            AddWorldLabel(root, "GÜVENLİK GÖREVLİSİ", new Vector3(0f,2.45f,13.1f),.2f,brass.color);
        }
    }

    private static void AddDebris(Transform root, string name, float x, float z, RobotController robot, string prompt)
    {
        Transform group = NewGroup(name, root);
        Cube(group, "Fallen bookcase", new Vector3(x, .56f, z), new Vector3(2.6f, 1.05f, .52f), "Wood_Walnut");
        string[] debrisBookColors = { "Red", "Blue", "Gold", "Green", "Purple", "Ivory" };
        for (int i=0;i<8;i++) { float bx=x+Random.Range(-1.25f,1.25f), bz=z+Random.Range(-1.05f,1.05f); GameObject book=Cube(group,"Scattered book",new Vector3(bx,.08f+Random.Range(0f,.1f),bz),new Vector3(.3f,.12f,.42f),"Book_"+debrisBookColors[i%debrisBookColors.Length]); book.transform.rotation=Quaternion.Euler(0f,Random.Range(0,180),Random.Range(-8,8)); }
        Cube(group, "Loose maintenance tool", new Vector3(x+.75f,.13f,z+.45f), new Vector3(.62f,.09f,.12f), "Brass");
        GameObject interact = new GameObject(name + "_Command"); interact.transform.SetParent(root,false); interact.transform.position=new Vector3(x,0,z);
        BoxCollider trigger=interact.AddComponent<BoxCollider>(); trigger.isTrigger=true; trigger.size=new Vector3(3.2f,2f,2.6f); trigger.center=Vector3.up;
        GameObject work = new GameObject("Robot_Work_Point"); work.transform.SetParent(root,false); work.transform.position=new Vector3(x<0?3f:-3f,0,z);
        interact.AddComponent<LibraryStoryInteractable>().Configure(LibraryInteractionKind.ClearDebris,"E · "+prompt,1,robot,work.transform,group.gameObject);
        AddWorldLabel(root,"ROBOT · "+prompt.ToUpper(),new Vector3(x,1.85f,z),.13f,Brass);
    }

    private static void BuildLibrarianScene()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Level_Door.unity", OpenSceneMode.Single);
        GameObject root = NewRoot(scene);
        SetLibraryLighting(root.transform);
        ApplyMaterial("Plane", "Floor");
        AddDoorLobby(root.transform);
        AddLibraryArchitecture(root.transform);
        AddStudentVisual(scene, root.transform);
        AddSecurityOfficer(root.transform, scene);
        RepositionDoor();
        ConfigureStoryPlayer(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void AddLibraryArchitecture(Transform root)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(LibraryModelPath);
        if (asset == null)
        {
            Debug.LogWarning("Library FBX was not imported yet: " + LibraryModelPath);
            return;
        }

        GameObject architecture = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        architecture.name = "Library_Interior_Imported_Model";
        SceneManager.MoveGameObjectToScene(architecture, SceneManager.GetActiveScene());
        architecture.transform.SetParent(root, true);
        architecture.transform.position = Vector3.zero;
        Renderer[] renderers = architecture.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        float horizontalSize = Mathf.Max(bounds.size.x, bounds.size.z);
        if (horizontalSize < 0.01f) return;
        architecture.transform.localScale *= 18f / horizontalSize;
        renderers = architecture.GetComponentsInChildren<Renderer>(true);
        bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        architecture.transform.position += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
        Debug.Log($"Imported library interior size: {bounds.size} (scaled to 18 m footprint).");
    }

    private static void RepositionDoor()
    {
        GameObject exit = GameObject.Find("ExitDoor");
        if (exit == null) return;
        exit.transform.position = new Vector3(0f, 2.5f, 7.1f);
        exit.transform.localScale = new Vector3(3f, 5f, 0.5f);
        Renderer renderer = exit.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Wood_Walnut.mat");
    }

    private static GameObject NewRoot(Scene scene)
    {
        GameObject previous = GameObject.Find(ArtRootName);
        if (previous != null) Object.DestroyImmediate(previous);
        GameObject root = new GameObject(ArtRootName);
        SceneManager.MoveGameObjectToScene(root, scene);
        return root;
    }

    private static Material MaterialAsset(string name, Color color, bool emission = false)
    {
        string path = MaterialsPath + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = shader;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (emission && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 2.2f);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureMaterials()
    {
        ModelImporter studentImporter = AssetImporter.GetAtPath(StudentModelPath) as ModelImporter;
        if (studentImporter != null && !studentImporter.isReadable)
        {
            studentImporter.isReadable = true;
            studentImporter.SaveAndReimport();
        }

        if (!AssetDatabase.IsValidFolder(MaterialsPath))
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Materials")) AssetDatabase.CreateFolder("Assets/_Project", "Materials");
            AssetDatabase.CreateFolder("Assets/_Project/Materials", "LibraryArt");
        }
        MaterialAsset("Wood_Walnut", Walnut);
        MaterialAsset("Wall_Cream", Cream);
        MaterialAsset("Brass", Brass, true);
        MaterialAsset("Teal_Enamel", Teal);
        MaterialAsset("Floor_Terrazzo", FloorColor);
        MaterialAsset("Laser_Cyan", new Color(0.05f, 0.85f, 1f), true);
        MaterialAsset("Book_Red", BookColors[0]);
        MaterialAsset("Book_Blue", BookColors[1]);
        MaterialAsset("Book_Gold", BookColors[2]);
        MaterialAsset("Book_Green", BookColors[3]);
        MaterialAsset("Book_Purple", BookColors[4]);
        MaterialAsset("Book_Ivory", BookColors[5]);
        Material student = MaterialAsset("Student_Fabric", Color.white);
        Texture2D studentTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Models/male-character-in-caual-clothing/textures/base.png");
        if (studentTexture != null)
        {
            student.mainTexture = studentTexture;
            if (student.HasProperty("_BaseMap")) student.SetTexture("_BaseMap", studentTexture);
        }
    }

    private static void ApplyMaterial(string objectName, string materialName)
    {
        GameObject target = GameObject.Find(objectName);
        if (target == null) return;
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/{MaterialFile(materialName)}.mat");
    }

    private static string MaterialFile(string name)
    {
        switch (name)
        {
            case "Floor": return "Floor_Terrazzo";
            case "Wall": return "Wall_Cream";
            case "Laser": return "Laser_Cyan";
            case "Brass": return "Brass";
            default: return "Wood_Walnut";
        }
    }

    private static void ConfigureShelf(string objectName, float x, Transform root)
    {
        GameObject shelf = GameObject.Find(objectName);
        if (shelf == null) return;
        shelf.transform.position = new Vector3(x, 1.5f, 0f);
        shelf.transform.localScale = new Vector3(1.55f, 3f, 16f);
        Renderer renderer = shelf.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Wood_Walnut.mat");
        shelf.transform.SetParent(root, true);
    }

    private static void AddBookcaseDetails(Transform root, float shelfX, string groupName)
    {
        Transform group = NewGroup(groupName, root);
        float inward = shelfX < 0f ? 1f : -1f;
        for (int row = 0; row < 3; row++)
        {
            float y = 0.56f + row * 0.76f;
            Cube(group, $"Shelf ledge {row + 1}", new Vector3(shelfX, y, 0f), new Vector3(1.58f, 0.075f, 15.8f), "Brass");
            for (int i = 0; i < 36; i++)
            {
                float z = -7.55f + i * 0.43f;
                float h = Random.Range(0.42f, 0.63f);
                GameObject book = Cube(group, $"Book {row + 1}-{i + 1:00}",
                    new Vector3(shelfX + inward * Random.Range(0.27f, 0.64f), y + h * 0.5f + 0.045f, z),
                    new Vector3(Random.Range(0.22f, 0.39f), h, Random.Range(0.16f, 0.31f)), "Book_" + new[] { "Red", "Blue", "Gold", "Green", "Purple", "Ivory" }[Random.Range(0, 6)]);
                book.transform.localRotation = Quaternion.Euler(0f, Random.Range(-3f, 3f), 0f);
            }
        }
        Cube(group, "Cornice", new Vector3(shelfX, 2.91f, 0f), new Vector3(1.7f, 0.18f, 16.1f), "Brass");
    }

    private static void AddFloorInlay(Transform root)
    {
        Transform group = NewGroup("Floor_Inlay", root);
        for (int z = -8; z <= 8; z += 2)
        {
            Cube(group, "Brass floor divider", new Vector3(0f, 0.012f, z), new Vector3(7.7f, 0.022f, 0.035f), "Brass");
            Cube(group, "Center carpet tile", new Vector3(0f, 0.014f, z + 1f), new Vector3(3.25f, 0.018f, 1.84f), "Teal_Enamel");
        }
    }

    private static void AddCorridorLighting(Transform root)
    {
        Transform group = NewGroup("Warm_Pendant_Lights", root);
        for (int z = -7; z <= 7; z += 7)
        {
            Cube(group, "Pendant canopy", new Vector3(0f, 3.72f, z), new Vector3(0.85f, 0.08f, 0.48f), "Walnut");
            Cube(group, "Luminous panel", new Vector3(0f, 3.64f, z), new Vector3(0.72f, 0.06f, 0.36f), "Brass");
            GameObject lightObject = new GameObject("Warm ceiling light");
            lightObject.transform.SetParent(group, false);
            lightObject.transform.position = new Vector3(0f, 3.45f, z);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.72f, 0.42f);
            light.intensity = 3.3f;
            light.range = 8f;
            light.shadows = LightShadows.None;
        }
        Cube(group, "Wall dado left", new Vector3(-4.94f, 0.46f, 0f), new Vector3(0.1f, 0.24f, 19.8f), "Wood_Walnut");
        Cube(group, "Wall dado right", new Vector3(4.94f, 0.46f, 0f), new Vector3(0.1f, 0.24f, 19.8f), "Wood_Walnut");
    }

    private static void AddWallPlaque(Transform root, string label, Vector3 position, Quaternion rotation)
    {
        GameObject plaque = Cube(root, "Plaque - " + label, position, new Vector3(2.6f, 0.72f, 0.12f), "Wood_Walnut");
        plaque.transform.rotation = rotation;
        AddWorldLabel(root, label, position + Vector3.back * 0.071f + Vector3.up * 0.015f, 0.26f, Brass);
    }

    private static void AddWorldLabel(Transform root, string text, Vector3 position, float size, Color color)
    {
        GameObject go = new GameObject("Sign - " + text);
        go.transform.SetParent(root, false);
        go.transform.position = position;
        TextMesh label = go.AddComponent<TextMesh>();
        label.text = text;
        label.fontSize = Mathf.Clamp(Mathf.RoundToInt(size * 100f), 18, 72);
        label.characterSize = 0.01f;
        label.anchor = TextAnchor.MiddleCenter;
        label.color = color;
        label.richText = false;
        label.alignment = TextAlignment.Center;
    }

    private static void SetLibraryLighting(Transform root)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.43f, 0.39f, 0.31f);
        RenderSettings.ambientIntensity = 1.3f;
        RenderSettings.fog = false;
        Transform group = NewGroup("Library_Warm_Lighting", root);
        GameObject keyObject = new GameObject("Warm key light");
        keyObject.transform.SetParent(group, false);
        keyObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
        Light key = keyObject.AddComponent<Light>();
        key.type = LightType.Directional;
        key.color = new Color(1f, 0.83f, 0.62f);
        key.intensity = 1.25f;
        key.shadows = LightShadows.Soft;
    }

    private static void AddGuardToCorridor(Transform root)
    {
        GameObject guardAsset = AssetDatabase.LoadAssetAtPath<GameObject>(GuardModelPath);
        if (guardAsset == null) return;
        GameObject guard = (GameObject)PrefabUtility.InstantiatePrefab(guardAsset);
        guard.name = "SecurityGuard_Exit_Post";
        SceneManager.MoveGameObjectToScene(guard, SceneManager.GetActiveScene());
        guard.transform.SetParent(root, true);
        guard.transform.position = new Vector3(0f, 0f, 8.2f);
        FitModelHeight(guard, 1.82f);
        guard.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        CapsuleCollider trigger = guard.AddComponent<CapsuleCollider>();
        trigger.height = 1.8f;
        trigger.radius = 0.42f;
        trigger.center = new Vector3(0f, 0.9f, 0f);
        trigger.isTrigger = true;
        guard.AddComponent<LibrarianNPC>();
        AddWorldLabel(root, "GÜVENLİK", new Vector3(0f, 2.5f, 8.2f), 0.22f, Brass);
    }

    private static void AddStudentVisual(Scene scene, Transform artRoot)
    {
        GameObject player = Object.FindFirstObjectByType<PlayerController>()?.gameObject;
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(StudentModelPath);
        if (player == null || model == null) return;
        if (player.transform.Find("CharacterVisual_Student") != null)
        {
            if (player.GetComponent<ProceduralHumanoidAnimator>() == null)
                player.AddComponent<ProceduralHumanoidAnimator>();
            return;
        }
        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
        visual.name = "CharacterVisual_Student";
        visual.transform.SetParent(player.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        FitModelHeight(visual, 1.72f);
        Material fabric = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Student_Fabric.mat");
        foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            if (fabric != null) renderer.sharedMaterial = fabric;
        foreach (Renderer renderer in player.GetComponents<Renderer>()) renderer.enabled = false;
        if (player.GetComponent<ProceduralHumanoidAnimator>() == null)
            player.AddComponent<ProceduralHumanoidAnimator>();
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void AddDoorLobby(Transform root)
    {
        Transform group = NewGroup("Librarian_Checkout_Lobby", root);
        Cube(group, "Circulation counter", new Vector3(3.55f, 0.58f, 2.7f), new Vector3(1.8f, 1.12f, 0.88f), "Wood_Walnut");
        Cube(group, "Counter brass inlay", new Vector3(3.55f, 1.18f, 2.24f), new Vector3(1.75f, 0.075f, 0.07f), "Brass");
        Cube(group, "Counter top", new Vector3(3.55f, 1.18f, 2.7f), new Vector3(1.9f, 0.13f, 0.98f), "Teal_Enamel");
        Cube(group, "Book pile one", new Vector3(3.2f, 1.39f, 2.65f), new Vector3(0.4f, 0.28f, 0.37f), "Book_Red");
        Cube(group, "Book pile two", new Vector3(3.2f, 1.57f, 2.65f), new Vector3(0.36f, 0.1f, 0.34f), "Book_Gold");
        Cube(group, "Desk lamp base", new Vector3(3.95f, 1.32f, 2.68f), new Vector3(0.26f, 0.08f, 0.26f), "Brass");
        GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lamp.name = "Desk lamp shade";
        lamp.transform.SetParent(group, false);
        lamp.transform.localPosition = new Vector3(3.95f, 1.55f, 2.68f);
        lamp.transform.localScale = Vector3.one * 0.22f;
        lamp.GetComponent<Collider>().enabled = false;
        lamp.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Brass.mat");
        AddWorldLabel(root, "GÜVENLİK NOKTASI", new Vector3(3.55f, 2.35f, 2.18f), 0.2f, Brass);
    }

    private static void AddLibrarian(Scene scene, Transform root)
    {
        GameObject maleAsset = AssetDatabase.LoadAssetAtPath<GameObject>(StudentModelPath);
        GameObject librarian = maleAsset != null ? (GameObject)PrefabUtility.InstantiatePrefab(maleAsset) : null;
        if (librarian == null) return;
        librarian.name = "Librarian_On_Duty";
        SceneManager.MoveGameObjectToScene(librarian, scene);
        librarian.transform.SetParent(root, true);
        librarian.transform.position = new Vector3(0f, 0f, 3.65f);
        librarian.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        FitModelHeight(librarian, 1.72f);
        Material fabric = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Student_Fabric.mat");
        foreach (Renderer renderer in librarian.GetComponentsInChildren<Renderer>(true))
            if (fabric != null) renderer.sharedMaterial = fabric;
        CapsuleCollider collider = librarian.AddComponent<CapsuleCollider>();
        collider.height = 1.8f;
        collider.radius = 0.42f;
        collider.center = new Vector3(0f, 0.9f, 0f);
        collider.isTrigger = true;
        librarian.AddComponent<LibrarianNPC>();
        AddWorldLabel(root, "NÖBETÇİ KÜTÜPHANECİ", new Vector3(0f, 2.45f, 3.65f), 0.17f, Brass);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void AddSecurityOfficer(Transform root, Scene scene)
    {
        GameObject guardAsset = AssetDatabase.LoadAssetAtPath<GameObject>(GuardModelPath);
        if (guardAsset == null) return;
        GameObject guard = (GameObject)PrefabUtility.InstantiatePrefab(guardAsset);
        guard.name = "Security_Officer_Exit_Post";
        SceneManager.MoveGameObjectToScene(guard, scene);
        guard.transform.SetParent(root, true);
        guard.transform.position = new Vector3(0f, 0f, 3.35f);
        guard.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        FitModelHeight(guard, 1.82f);
        CapsuleCollider collider = guard.AddComponent<CapsuleCollider>();
        collider.height = 1.8f;
        collider.radius = 0.42f;
        collider.center = new Vector3(0f, 0.9f, 0f);
        collider.isTrigger = true;
        guard.AddComponent<LibrarianNPC>();
        AddWorldLabel(root, "GÜVENLİK", new Vector3(0f, 2.38f, 3.35f), 0.18f, Brass);
    }

    private static void AddRobotVisual(Scene scene)
    {
        GameObject robot = GameObject.Find("Robot");
        GameObject robotModel = AssetDatabase.LoadAssetAtPath<GameObject>(RobotModelPath);
        if (robot == null)
        {
            Debug.LogWarning("Robot root was not found in Level_Corridor.");
            return;
        }
        if (robot.transform.Find("CharacterVisual_Robot") != null) return;

        GameObject visual;
        if (robotModel != null)
            visual = (GameObject)PrefabUtility.InstantiatePrefab(robotModel);
        else
        {
            Debug.LogWarning("Robot GLTF root could not be loaded. Building a visible low-poly fallback.");
            visual = BuildRobotFallback();
        }
        visual.name = "CharacterVisual_Robot";
        SceneManager.MoveGameObjectToScene(visual, scene);
        visual.transform.SetParent(robot.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        if (robotModel != null) FitModelHeight(visual, 0.82f);
        foreach (Renderer renderer in robot.GetComponents<Renderer>()) renderer.enabled = false;
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static GameObject BuildRobotFallback()
    {
        GameObject root = new GameObject("RobotVisualFallback");
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "Rounded robot shell";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.43f, 0f);
        body.transform.localScale = new Vector3(0.62f, 0.46f, 0.52f);
        body.GetComponent<Collider>().enabled = false;
        body.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Teal_Enamel.mat");

        GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = "Robot cyan sensor";
        eye.transform.SetParent(root.transform, false);
        eye.transform.localPosition = new Vector3(0f, 0.49f, 0.255f);
        eye.transform.localScale = new Vector3(0.2f, 0.11f, 0.07f);
        eye.GetComponent<Collider>().enabled = false;
        eye.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Laser_Cyan.mat");
        return root;
    }

    private static void FitModelHeight(GameObject instance, float targetHeight)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        if (bounds.size.y < 0.001f) return;
        float factor = targetHeight / bounds.size.y;
        instance.transform.localScale *= factor;
        renderers = instance.GetComponentsInChildren<Renderer>(true);
        bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        instance.transform.position += Vector3.up * (instance.transform.position.y - bounds.min.y);
    }

    private static Transform NewGroup(string name, Transform parent)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    private static GameObject Cube(Transform parent, string name, Vector3 position, Vector3 scale, string materialName)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        go.transform.localScale = scale;
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) collider.enabled = false;
        string file = MaterialFile(materialName);
        if (materialName.StartsWith("Book_")) file = "Book_" + materialName.Substring(5);
        Renderer renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/{file}.mat");
        return go;
    }
}
