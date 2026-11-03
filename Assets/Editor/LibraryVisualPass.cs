using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Breakpoint.Robot;
using Type = System.Type;
using Enum = System.Enum;

/// <summary>One-shot, repeatable art pass for the two playable library scenes.</summary>
public static class LibraryVisualPass
{
    private const string ArtRootName = "LIBRARY_ART_PASS";
    private const string MaterialsPath = "Assets/_Project/Materials/LibraryArt";
    private const string StudentModelPath = "Assets/_Project/Models/male-character-in-caual-clothing/source/base.obj";
    private const string GuardModelPath = "Assets/_Project/Models/security-guard-rigged-3d-model/source/Guard.fbx";
    private const string LibraryModelPath = "Assets/_Project/Models/library-low-poly/source/biblioteca.fbx";
    private const string RobotModelPath = "Assets/_Project/Models/simple-little-robot-model/source/model.gltf";
    private const string GuardTexturesPath = "Assets/_Project/Models/security-guard-rigged-3d-model/textures/";

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
        EnsureGuardMaterials();
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
            exitTrigger.transform.position = new Vector3(0f, 1f, 30.1f);
            BoxCollider exitCollider = exitTrigger.GetComponent<BoxCollider>();
            if (exitCollider != null) { exitCollider.isTrigger = true; exitCollider.size = new Vector3(8.5f, 2.5f, 1f); }
        }

        // Reflection keeps this editor utility independent of the AI Navigation assembly reference.
        Type surfaceType = Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
        if (surfaceType != null)
        {
            Component surface = root.GetComponent(surfaceType);
            if (surface == null) surface = root.AddComponent(surfaceType);
            var geometryProperty = surfaceType.GetProperty("useGeometry");
            if (geometryProperty != null)
                geometryProperty.SetValue(surface, Enum.Parse(geometryProperty.PropertyType, "PhysicsColliders"));
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
            ? new Vector3(-1.6f, 4.03f, -4.7f)
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
        // Move the third-person camera over the right shoulder so the reticle and
        // command ray no longer run through the player's own back/capsule.
        if (cam != null) { cam.localPosition = new Vector3(0.72f, 1.82f, -3.8f); cam.localRotation = Quaternion.identity; }
        EditorUtility.SetDirty(player);
    }

    private static void AddStoryFloorplan(Transform root)
    {
        Material floor = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Floor_Terrazzo.mat");
        Material wood = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Wood_Walnut.mat");
        Material brass = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Brass.mat");
        Material teal = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Teal_Enamel.mat");
        GameObject upper = Cube(root, "Upper Study Floor", new Vector3(0f, 3.82f, -5.2f), new Vector3(14.5f, .36f, 10.2f), "Floor_Terrazzo");
        upper.GetComponent<Collider>().enabled = true;
        GameObject lower = Cube(root, "Ground Floor", new Vector3(0f, -.18f, 13.6f), new Vector3(22.8f, .36f, 35.8f), "Floor_Terrazzo");
        lower.GetComponent<Collider>().enabled = true;
        // Upper-floor guardrails prevent exploration from turning into accidental falls.
        Transform boundaries = NewGroup("Upper_Floor_Guardrails", root);
        foreach (float x in new[] { -7.05f, 7.05f })
        {
            for (int z = -9; z <= -1; z += 2)
            {
                GameObject post = Cube(boundaries, "Oak safety baluster", new Vector3(x, 4.53f, z), new Vector3(.12f, 1.02f, .12f), "Wood_Walnut");
                post.GetComponent<Collider>().enabled = true;
            }
            GameObject rail = Cube(boundaries, "Upper gallery handrail", new Vector3(x, 5.0f, -5.2f), new Vector3(.18f, .16f, 9.8f), "Wood_Walnut");
            rail.GetComponent<Collider>().enabled = true;
        }
        GameObject rearRail = Cube(boundaries, "Gallery rear handrail", new Vector3(0f, 5.0f, -10.15f), new Vector3(14.1f, .16f, .16f), "Wood_Walnut");
        rearRail.GetComponent<Collider>().enabled = true;
        // Keep the central stair opening clear while guarding both balcony wings.
        foreach (float side in new[] { -1f, 1f })
        {
            GameObject stairWingRail = Cube(boundaries, "Stairwell wing handrail", new Vector3(side * 4.5f, 5.0f, -.2f), new Vector3(5.0f, .16f, .16f), "Wood_Walnut");
            stairWingRail.GetComponent<Collider>().enabled = true;
        }
        // Covered reading hall with a high ceiling and visible support piers.
        GameObject ceiling = Cube(root, "Covered Library Roof", new Vector3(0f, 8.05f, 13f), new Vector3(23.5f, .32f, 43f), "Wood_Walnut");
        ceiling.GetComponent<Collider>().enabled = false;
        // Enclosed lower library wing; wall sections leave the central exit clear.
        for (float z = -1f; z <= 29f; z += 6f)
        foreach (int side in new[] { -1, 1 })
        {
            GameObject wall = Cube(root, "Ground floor perimeter wall", new Vector3(side * 11.35f, 3.95f, z), new Vector3(.28f, 7.9f, 5.8f), "Wall_Cream");
            wall.GetComponent<Collider>().enabled = true;
            Cube(root, "Ground floor walnut dado", new Vector3(side * 11.18f, .55f, z), new Vector3(.08f, .28f, 5.8f), "Wood_Walnut");
        }
        // Exit end wall panels frame the security checkpoint and leave a broad doorway.
        foreach (float side in new[] { -1f, 1f })
        {
            GameObject endPanel = Cube(root, "Exit hall end wall", new Vector3(side * 7.2f, 3.95f, 31.1f), new Vector3(8.4f, 7.9f, .28f), "Wall_Cream");
            endPanel.GetComponent<Collider>().enabled = true;
        }
        for (int z = -3; z <= 27; z += 6)
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject pier = Cube(root, "Roof support column", new Vector3(side * 11.0f, 3.95f, z), new Vector3(.38f, 8f, .38f), "Wood_Walnut");
            pier.GetComponent<Collider>().enabled = true;
            Cube(root, "Column brass collar", new Vector3(side * 11.0f, 1.0f, z), new Vector3(.42f, .16f, .42f), "Brass");
        }
        AddUpperFloorDecor(root, wood, brass);
        // Start at the desk and wake via E.
        Transform desk = NewGroup("Opening_Study_Desk", root);
        GameObject deskTop = Cube(desk, "Desk top", new Vector3(-1.6f, 4.62f, -6.6f), new Vector3(2.1f, .14f, 1.05f), "Wood_Walnut");
        deskTop.GetComponent<Collider>().enabled = true;
        GameObject deskLegs = Cube(desk, "Desk legs", new Vector3(-1.6f, 4.28f, -6.6f), new Vector3(1.85f, .62f, .82f), "Wood_Walnut");
        deskLegs.GetComponent<Collider>().enabled = true;
        Cube(desk, "Open notebook", new Vector3(-1.6f, 4.72f, -6.5f), new Vector3(.55f, .035f, .42f), "Book_Ivory");
        Transform chair = NewGroup("Occupied_Study_Chair", root);
        GameObject seat = Cube(chair, "Upholstered chair seat", new Vector3(-1.6f, 4.53f, -4.7f), new Vector3(.78f, .16f, .72f), "Teal_Enamel"); seat.GetComponent<Collider>().enabled = false;
        GameObject chairBack = Cube(chair, "Upholstered chair back", new Vector3(-1.6f, 4.78f, -4.42f), new Vector3(.78f, .5f, .14f), "Teal_Enamel"); chairBack.GetComponent<Collider>().enabled = false;
        foreach (float cx in new[] { -1.87f, -1.33f })
        foreach (float cz in new[] { -4.95f, -4.45f })
        {
            GameObject chairLeg = Cube(chair, "Chair leg", new Vector3(cx, 4.25f, cz), new Vector3(.08f, .56f, .08f), "Wood_Walnut"); chairLeg.GetComponent<Collider>().enabled = false;
        }
        GameObject wake = new GameObject("Wake_At_Desk_Interactable"); wake.transform.SetParent(root, false); wake.transform.position = new Vector3(-1.6f, 4.03f, -5.75f);
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
        // A smooth, invisible NavMesh ramp bridges the stair treads. The visible
        // stairs remain unchanged while small agents can calculate a continuous path.
        GameObject stairNavRamp = new GameObject("Stair_NavMesh_Ramp");
        stairNavRamp.transform.SetParent(stairs, false);
        stairNavRamp.transform.position = new Vector3(0f, 2.0f, 2.05f);
        stairNavRamp.transform.rotation = Quaternion.Euler(33.5f, 0f, 0f);
        BoxCollider rampCollider = stairNavRamp.AddComponent<BoxCollider>();
        rampCollider.size = new Vector3(2.65f, .12f, 7.35f);
        // Collider only: no visible block in the staircase.
        Cube(stairs, "Left stair rail", new Vector3(-1.48f, 2.1f, 1.55f), new Vector3(.09f, 2.9f, 6.1f), "Wood_Walnut");
        Cube(stairs, "Right stair rail", new Vector3(1.48f, 2.1f, 1.55f), new Vector3(.09f, 2.9f, 6.1f), "Wood_Walnut");
        GameObject stairTrigger = new GameObject("Stair_Descent_Trigger"); stairTrigger.transform.SetParent(root, false); stairTrigger.transform.position = new Vector3(0f, .45f, 5.25f);
        BoxCollider stairCol = stairTrigger.AddComponent<BoxCollider>(); stairCol.isTrigger = true; stairCol.size = new Vector3(3.4f, 2f, 1.2f); stairTrigger.AddComponent<StairDescentTrigger>();
        AddWorldLabel(root, "ZEMİN KAT ↓", new Vector3(0f, 2.6f, 5.8f), .22f, brass.color);

        RobotController robot = Object.FindFirstObjectByType<RobotController>();
        GameObject robotRoot = robot != null ? robot.gameObject : null;
        if (robotRoot != null)
        {
            // Keep the model out of the table/shelf silhouette and close to spawn.
            robotRoot.transform.position = new Vector3(-2.55f, 4.02f, -7.0f);
            SphereCollider robotInteract = robotRoot.GetComponent<SphereCollider>();
            if (robotInteract == null) robotInteract = robotRoot.AddComponent<SphereCollider>();
            robotInteract.isTrigger = true; robotInteract.radius = 1.2f; robotInteract.center = new Vector3(0f, .45f, 0f);
            LibraryStoryInteractable activate = robotRoot.GetComponent<LibraryStoryInteractable>();
            if (activate == null) activate = robotRoot.AddComponent<LibraryStoryInteractable>();
            activate.Configure(LibraryInteractionKind.ActivateRobot, "E · Tozu temizle / robotu etkinleştir", 3);
            foreach (Transform child in robotRoot.GetComponentsInChildren<Transform>(true))
                if (child.parent == robotRoot.transform && child.name == "Dust_Cover")
                    Object.DestroyImmediate(child.gameObject);
            Vector3 robotPosition = robotRoot.transform.position;
            AddWorldLabel(root, "ROBOT · E İLE ETKİNLEŞTİR", robotPosition + new Vector3(0f, 1.25f, 0f), .24f, Color.white);
            GameObject beacon = new GameObject("Robot_Search_Beacon"); beacon.transform.SetParent(root, false); beacon.transform.position = robotPosition + Vector3.up * 1.75f;
            Light beaconLight = beacon.AddComponent<Light>(); beaconLight.type = LightType.Point; beaconLight.color = new Color(.1f, .8f, 1f); beaconLight.intensity = 3.4f; beaconLight.range = 4.5f;
            GameObject beaconStem = GameObject.CreatePrimitive(PrimitiveType.Cylinder); beaconStem.name = "Robot location beacon stem"; beaconStem.transform.SetParent(beacon.transform, false); beaconStem.transform.localPosition = new Vector3(0f, -.28f, 0f); beaconStem.transform.localScale = new Vector3(.055f,.55f,.055f); beaconStem.GetComponent<Collider>().enabled = false; beaconStem.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Brass.mat");
            GameObject beaconOrb = GameObject.CreatePrimitive(PrimitiveType.Sphere); beaconOrb.name = "Robot location beacon orb"; beaconOrb.transform.SetParent(beacon.transform, false); beaconOrb.transform.localPosition = new Vector3(0f, .18f, 0f); beaconOrb.transform.localScale = Vector3.one * .22f; beaconOrb.GetComponent<Collider>().enabled = false; beaconOrb.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Laser_Cyan.mat");
        }

        // Ground-floor debris clusters are actual visual obstacles; the robot travels to a clear NavMesh work point.
        AddLowerFloorBookcases(root);
        AddLowerFloorBookScatter(root);
        AddDebris(root, "Fallen_Shelf_Blockage", -1.15f, 8.4f, robot, "Devrilmiş rafı kaldır");
        AddDebris(root, "Book_Trolley_Blockage", 1.15f, 15.3f, robot, "Kitap arabasını çek");
        AddDebris(root, "Books_And_Tools_Blockage", 0f, 23.0f, robot, "Kitap ve araç gereçleri topla");
        AddFinalDebrisMarker(root, new Vector3(0f, 0f, 23.0f));
        AddFallenShelfDecor(root, -7.7f, 10.5f, -1f);
        AddFallenShelfDecor(root, 7.65f, 17.6f, 1f);
        AddFallenShelfDecor(root, -7.7f, 24.0f, 1f);
        GameObject guardAsset = AssetDatabase.LoadAssetAtPath<GameObject>(GuardModelPath);
        if (guardAsset != null)
        {
            GameObject guard = (GameObject)PrefabUtility.InstantiatePrefab(guardAsset); guard.name = "SecurityGuard_Exit_Post"; SceneManager.MoveGameObjectToScene(guard, SceneManager.GetActiveScene()); guard.transform.SetParent(root, true); guard.transform.position = new Vector3(0f, 0f, 27.6f); FitModelHeight(guard, 1.82f); guard.transform.rotation = Quaternion.Euler(0f, 180f, 0f); ApplyGuardMaterials(guard);
            CapsuleCollider trigger = guard.AddComponent<CapsuleCollider>(); trigger.height=1.8f; trigger.radius=.55f; trigger.center=new Vector3(0f,.9f,0f); trigger.isTrigger=true; guard.AddComponent<LibrarianNPC>();
            AddWorldLabel(root, "GÜVENLİK GÖREVLİSİ", new Vector3(0f,2.45f,27.6f),.2f,brass.color);
        }
    }

    private static void AddDebris(Transform root, string name, float x, float z, RobotController robot, string prompt)
    {
        Transform group = NewGroup(name, root);
        if (name.Contains("Trolley"))
        {
            GameObject cartBase = Cube(group, "Overturned book cart frame", new Vector3(x, .34f, z), new Vector3(1.55f, .12f, .82f), "Wood_Walnut");
            cartBase.GetComponent<Collider>().enabled = true;
            foreach (float side in new[] { -1f, 1f })
            {
                GameObject handle = Cube(group, "Book cart handle", new Vector3(x + side * .68f, .68f, z), new Vector3(.08f, .64f, .08f), "Brass");
                handle.transform.rotation = Quaternion.Euler(0f, 0f, side * 14f);
                GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = "Book cart wheel"; wheel.transform.SetParent(group, true);
                wheel.transform.position = new Vector3(x + side * .68f, .14f, z + .28f);
                wheel.transform.rotation = Quaternion.Euler(90f, 0f, 0f); wheel.transform.localScale = Vector3.one * .25f;
                wheel.GetComponent<Collider>().enabled = false;
                wheel.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Brass.mat");
            }
        }
        else
        {
            // Broken shelf parts lie at different angles, so the obstacle reads as
            // collapsed furniture rather than a solid rectangular barricade.
            GameObject plank = Cube(group, "Fallen shelf plank", new Vector3(x, .34f, z), new Vector3(2.15f, .12f, .44f), "Wood_Walnut");
            plank.transform.rotation = Quaternion.Euler(0f, name.Contains("Books") ? 24f : -18f, name.Contains("Books") ? -9f : 13f);
            plank.GetComponent<Collider>().enabled = true;
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject rail = Cube(group, "Broken shelf upright", new Vector3(x + side * .76f, .32f, z + side * .12f), new Vector3(.12f, .66f, .13f), "Wood_Walnut");
                rail.transform.rotation = Quaternion.Euler(0f, 0f, side * (name.Contains("Books") ? 27f : -36f));
                rail.GetComponent<Collider>().enabled = true;
            }
            GameObject looseShelf = Cube(group, "Tilted loose shelf", new Vector3(x - .12f, .52f, z + .2f), new Vector3(1.25f, .09f, .4f), "Wood_Walnut");
            looseShelf.transform.rotation = Quaternion.Euler(0f, 12f, 31f);
            looseShelf.GetComponent<Collider>().enabled = true;
        }
        string[] debrisBookColors = { "Red", "Blue", "Gold", "Green", "Purple", "Ivory" };
        for (int i = 0; i < 18; i++)
        {
            float angle = i * 2.399f;
            float radius = .35f + (i % 5) * .19f;
            float bx = x + Mathf.Cos(angle) * radius;
            float bz = z + Mathf.Sin(angle) * radius * .72f;
            float height = .08f + Random.Range(0f, .08f);
            GameObject book = Cube(group, "Scattered book", new Vector3(bx, height, bz),
                new Vector3(.26f + Random.Range(0f, .1f), .1f, .34f + Random.Range(0f, .1f)),
                "Book_" + debrisBookColors[i % debrisBookColors.Length]);
            book.transform.rotation = Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(0, 180), Random.Range(-10f, 10f));
        }
        GameObject tool = Cube(group, "Loose maintenance tool", new Vector3(x + .85f, .13f, z + .55f), new Vector3(.58f, .08f, .1f), "Brass");
        tool.transform.rotation = Quaternion.Euler(0f, -28f, 8f);
        GameObject interact = new GameObject(name + "_Command"); interact.transform.SetParent(root,false); interact.transform.position=new Vector3(x,0,z);
        BoxCollider trigger=interact.AddComponent<BoxCollider>(); trigger.isTrigger=true;
        bool finalBlockage = name.Contains("Books");
        trigger.size = finalBlockage ? new Vector3(5f, 2.8f, 4.6f) : new Vector3(3.2f, 2f, 2.6f);
        trigger.center = finalBlockage ? new Vector3(0f, 1.25f, 0f) : Vector3.up;
        GameObject work = new GameObject("Robot_Work_Point"); work.transform.SetParent(root,false);
        work.transform.position = new Vector3(finalBlockage ? -3.6f : (x<0?3f:-3f), 0f, z);
        interact.AddComponent<LibraryStoryInteractable>().Configure(LibraryInteractionKind.ClearDebris,"E · "+prompt,1,robot,work.transform,group.gameObject);
        AddWorldLabel(root,"ROBOT · "+prompt.ToUpper(),new Vector3(x,1.85f,z),.13f,Brass);
    }

    private static void AddFinalDebrisMarker(Transform root, Vector3 position)
    {
        Material cyan = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Laser_Cyan.mat");
        GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        beacon.name = "Final_Debris_Command_Beacon";
        beacon.transform.SetParent(root, false);
        beacon.transform.position = position + new Vector3(0f, 2.1f, 0f);
        beacon.transform.localScale = Vector3.one * .28f;
        beacon.GetComponent<Collider>().enabled = false;
        beacon.GetComponent<Renderer>().sharedMaterial = cyan;
        Light glow = beacon.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = new Color(.08f, .85f, 1f);
        glow.intensity = 2.2f;
        glow.range = 5f;
        AddWorldLabel(root, "SON ENGEL · E İLE ROBOTU GÖNDER", position + new Vector3(0f, 2.65f, 0f), .17f, new Color(.25f, .9f, 1f));
    }

    private static void AddFallenShelfDecor(Transform root, float x, float z, float lean)
    {
        Transform decor = NewGroup("Collapsed_Shelf_Side_Decor", root);
        GameObject board = Cube(decor, "Scattered fallen shelf", new Vector3(x, .22f, z), new Vector3(2.6f, .14f, .62f), "Wood_Walnut");
        board.transform.rotation = Quaternion.Euler(0f, lean * 19f, lean * 12f);
        board.GetComponent<Collider>().enabled = true;
        for (int i = 0; i < 5; i++)
        {
            GameObject looseBook = Cube(decor, "Book from collapsed shelf", new Vector3(x + (i - 2) * .32f, .12f + (i % 2) * .08f, z + .42f), new Vector3(.27f, .12f, .36f), "Book_" + new[] { "Red", "Blue", "Gold", "Green", "Purple" }[i]);
            looseBook.transform.rotation = Quaternion.Euler(0f, i * 29f, (i % 2 == 0 ? 8f : -11f));
        }
    }

    private static void AddLowerFloorBookcases(Transform root)
    {
        Transform banks = NewGroup("Ground_Floor_Bookcase_Aisles", root);
        string[] colors = { "Red", "Blue", "Gold", "Green", "Purple", "Ivory" };
        float[] centersX = { -9.5f, -6.0f, 6.0f, 9.5f };
        float[] centersZ = { 6.5f, 12.8f, 19.1f, 25.4f };
        int index = 0;
        foreach (float x in centersX)
        foreach (float z in centersZ)
        {
            Transform caseRoot = NewGroup("Double-sided bookcase " + (++index).ToString("00"), banks);
            GameObject back = Cube(caseRoot, "Bookcase back panel", new Vector3(x, 1.35f, z), new Vector3(.1f, 2.7f, 4.65f), "Wood_Walnut");
            back.GetComponent<Collider>().enabled = true;
            foreach (float side in new[] { -1f, 1f })
            {
                GameObject upright = Cube(caseRoot, "Bookcase upright", new Vector3(x + side * .52f, 1.35f, z), new Vector3(.12f, 2.7f, 4.75f), "Wood_Walnut");
                upright.GetComponent<Collider>().enabled = true;
            }
            for (int shelf = 0; shelf < 4; shelf++)
            {
                float y = .16f + shelf * .63f;
                GameObject ledge = Cube(caseRoot, "Oak shelf", new Vector3(x, y, z), new Vector3(1.12f, .09f, 4.7f), "Wood_Walnut");
                ledge.GetComponent<Collider>().enabled = true;
                for (int book = 0; book < 9; book++)
                {
                    float bookZ = z - 2.05f + book * .49f;
                    float height = .38f + ((book + shelf) % 4) * .055f;
                    GameObject volume = Cube(caseRoot, "Library volume", new Vector3(x + .1f, y + .08f + height * .5f, bookZ),
                        new Vector3(.2f, height, .32f), "Book_" + colors[(book + shelf + index) % colors.Length]);
                    volume.transform.rotation = Quaternion.Euler(0f, 0f, ((book + shelf) % 5 == 0) ? 5f : 0f);
                }
            }
            Cube(caseRoot, "Bookcase crown moulding", new Vector3(x, 2.73f, z), new Vector3(1.2f, .16f, 4.85f), "Wood_Walnut");
        }
    }

    private static void AddLowerFloorBookScatter(Transform root)
    {
        Transform scatter = NewGroup("Ground_Floor_Reading_Debris", root);
        string[] colors = { "Red", "Blue", "Gold", "Green", "Purple", "Ivory" };
        Vector2[] piles = { new Vector2(-3.7f, 5.6f), new Vector2(3.8f, 7.1f), new Vector2(-3.6f, 13f),
            new Vector2(3.7f, 17f), new Vector2(-3.8f, 20f), new Vector2(3.8f, 24f) };
        int serial = 0;
        foreach (Vector2 pile in piles)
        for (int i = 0; i < 7; i++)
        {
            float offsetX = Random.Range(-.72f, .72f);
            float offsetZ = Random.Range(-.9f, .9f);
            GameObject book = Cube(scatter, "Loose reading-room book " + (++serial),
                new Vector3(pile.x + offsetX, .075f + (i % 3) * .045f, pile.y + offsetZ),
                new Vector3(.28f + Random.Range(0f, .1f), .12f, .38f + Random.Range(0f, .08f)), "Book_" + colors[i % colors.Length]);
            book.transform.rotation = Quaternion.Euler(0f, Random.Range(0, 180), Random.Range(-8f, 8f));
        }
    }

    private static void AddUpperFloorDecor(Transform root, Material wood, Material brass)
    {
        Transform shelves = NewGroup("Upper_Gallery_Bookcases", root);
        string[] upperBookColors = { "Red", "Blue", "Gold", "Green", "Purple", "Ivory" };
        foreach (float side in new[] { -1f, 1f })
        {
            float x = side * 5.8f;
            GameObject back = Cube(shelves, "Oak bookcase back", new Vector3(x, 5.05f, -5.15f), new Vector3(.12f, 2.0f, 4.8f), "Wood_Walnut");
            back.GetComponent<Collider>().enabled = true;
            foreach (float z in new[] { -7.5f, -2.8f })
            {
                GameObject upright = Cube(shelves, "Bookcase side", new Vector3(x, 5.05f, z), new Vector3(1.05f, 2.0f, .14f), "Wood_Walnut");
                upright.GetComponent<Collider>().enabled = true;
            }
            for (int row = 0; row < 4; row++)
            {
                float y = 4.25f + row * .52f;
                GameObject ledge = Cube(shelves, "Bookcase shelf", new Vector3(x, y, -5.15f), new Vector3(1.0f, .09f, 4.65f), "Brass");
                ledge.GetComponent<Collider>().enabled = true;
                for (int book = 0; book < 9; book++)
                {
                    float z = -7.3f + book * .54f;
                    GameObject volume = Cube(shelves, "Upper-floor book", new Vector3(x + side * .25f, y + .2f, z), new Vector3(.3f, .38f, .42f), "Book_" + upperBookColors[book % upperBookColors.Length]);
                    volume.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-4f, 4f));
                }
            }
        }

        // Two occupied reading tables add landmarks while preserving a wide central route.
        foreach (Vector3 position in new[] { new Vector3(4.0f, 0f, -4.4f), new Vector3(-4.0f, 0f, -2.4f) })
        {
            GameObject top = Cube(root, "Upper gallery reading table", new Vector3(position.x, 4.65f, position.z), new Vector3(1.9f, .14f, 1.15f), "Wood_Walnut");
            top.GetComponent<Collider>().enabled = true;
            GameObject baseBlock = Cube(root, "Reading table pedestal", new Vector3(position.x, 4.28f, position.z), new Vector3(.35f, .62f, .35f), "Wood_Walnut");
            baseBlock.GetComponent<Collider>().enabled = true;
            Cube(root, "Reading lamp base", new Vector3(position.x, 4.78f, position.z), new Vector3(.22f, .06f, .22f), "Brass");
            GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lamp.name = "Warm reading lamp"; lamp.transform.SetParent(root, false);
            lamp.transform.position = new Vector3(position.x, 4.98f, position.z);
            lamp.transform.localScale = Vector3.one * .18f;
            lamp.GetComponent<Collider>().enabled = false;
            lamp.GetComponent<Renderer>().sharedMaterial = brass;
            Light lampLight = lamp.AddComponent<Light>(); lampLight.type = LightType.Point;
            lampLight.color = new Color(1f, .72f, .42f); lampLight.intensity = 1.8f; lampLight.range = 3.2f;
        }

        // A quiet seating nook and a potted plant give the broad gallery navigable landmarks.
        GameObject bench = Cube(root, "Reading nook bench", new Vector3(4.8f, 4.48f, -8.2f), new Vector3(1.7f, .2f, .55f), "Teal_Enamel");
        bench.GetComponent<Collider>().enabled = true;
        GameObject plantPot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        plantPot.name = "Library planter"; plantPot.transform.SetParent(root, false);
        plantPot.transform.position = new Vector3(-5.0f, 4.35f, -8.6f); plantPot.transform.localScale = new Vector3(.7f,.65f,.7f);
        plantPot.GetComponent<Renderer>().sharedMaterial = wood;
        GameObject plant = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        plant.name = "Indoor plant"; plant.transform.SetParent(root, false);
        plant.transform.position = new Vector3(-5.0f, 5.0f, -8.6f); plant.transform.localScale = new Vector3(1f,1.2f,1f);
        plant.GetComponent<Collider>().enabled = false;
        plant.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Book_Green.mat");
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
        RemoveDialoguePanelControllerDuplicate(scene);
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

    private static void EnsureGuardMaterials()
    {
        CreateTexturedGuardMaterial("Security_Body", "RGB_9192397b71444a7bb139dbd7dc7ac83f_SecurityGuard_Body_D.jpeg");
        CreateTexturedGuardMaterial("Security_Gear", "RGB_0d3f171de3de4737b712d2ff93b08ab7_SecurityGuard_Gear_D.jpeg");
        CreateTexturedGuardMaterial("Security_Head", "RGB_942ffe12241e42f3b848c48eff35c05b_SecurityGuard_Head_3_D.jpeg");
    }

    private static Material CreateTexturedGuardMaterial(string name, string textureName)
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
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(GuardTexturesPath + textureName);
        if (texture != null)
        {
            material.mainTexture = texture;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
        }
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ApplyGuardMaterials(GameObject guard)
    {
        Material body = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Security_Body.mat");
        Material gear = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Security_Gear.mat");
        Material head = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Security_Head.mat");
        foreach (Renderer renderer in guard.GetComponentsInChildren<Renderer>(true))
        {
            Material[] slots = renderer.sharedMaterials;
            for (int i = 0; i < slots.Length; i++)
            {
                string slotName = slots[i] != null ? slots[i].name.ToLowerInvariant() : renderer.name.ToLowerInvariant();
                slots[i] = slotName.Contains("head") || slotName.Contains("face") ? head
                    : slotName.Contains("gear") || slotName.Contains("access") ? gear
                    : slotName.Contains("body") || slotName.Contains("uniform") ? body
                    : slots.Length >= 3 ? (i == 1 ? gear : i == 2 ? head : body) : body;
            }
            if (slots.Length == 0) slots = new[] { body };
            renderer.sharedMaterials = slots;
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
        for (int z = -7; z <= 28; z += 7)
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
        ApplyGuardMaterials(guard);
        CapsuleCollider collider = guard.AddComponent<CapsuleCollider>();
        collider.height = 2.5f;
        collider.radius = 1.35f;
        collider.center = new Vector3(0f, 1.2f, 0f);
        collider.isTrigger = true;
        guard.AddComponent<LibrarianNPC>();
        AddWorldLabel(root, "GÜVENLİK", new Vector3(0f, 2.38f, 3.35f), 0.18f, Brass);
    }

    private static void RemoveDialoguePanelControllerDuplicate(Scene scene)
    {
        foreach (DialogueUI dialogue in Object.FindObjectsByType<DialogueUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (dialogue.gameObject.scene != scene || dialogue.dialoguePanel != dialogue.gameObject) continue;
            Object.DestroyImmediate(dialogue);
            Debug.Log("Removed duplicate DialogueUI from DialoguePanel; Canvas controller remains active.");
        }
    }

    private static void AddRobotVisual(Scene scene)
    {
        GameObject robot = GameObject.Find("Robot");
        GameObject robotModel = RobotGltfImporter.EnsureImported();
        if (robot == null)
        {
            Debug.LogWarning("Robot root was not found in Level_Corridor.");
            return;
        }
        Transform oldVisual = robot.transform.Find("CharacterVisual_Robot");
        if (oldVisual != null) Object.DestroyImmediate(oldVisual.gameObject);

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
        var orphanedVisuals = new System.Collections.Generic.List<GameObject>();
        foreach (Transform candidate in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (candidate == visual.transform || candidate.name != "CharacterVisual_Robot") continue;
            if (candidate.parent == null)
                orphanedVisuals.Add(candidate.gameObject);
        }
        foreach (GameObject orphan in orphanedVisuals)
            if (orphan != null) Object.DestroyImmediate(orphan);
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
