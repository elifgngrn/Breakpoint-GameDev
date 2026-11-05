using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Creates small, low-poly librarian props and a readable exit sign at runtime.</summary>
[DisallowMultipleComponent]
public sealed class DoorSceneDressing : MonoBehaviour
{
    private const float CounterTopY = 1.245f;

    private Material paperMaterial;
    private Material paperShadeMaterial;
    private Material inkMaterial;
    private Material leatherMaterial;
    private Material brassMaterial;
    private Material sealMaterial;
    private Material signMaterial;
    private Material routeMaterial;
    private Material portalWoodMaterial;
    private Material nightSkyMaterial;
    private Material skylineMaterial;
    private Material moonMaterial;
    private Material windowMaterial;

    private void Awake()
    {
        ApplyDoorLighting();
        if (transform.Find("DoorScene_Dressing_Runtime") != null)
            return;

        CreateMaterials();
        Transform dressing = new GameObject("DoorScene_Dressing_Runtime").transform;
        dressing.SetParent(transform, false);
        AddDeskDocuments(dressing);
        AddExitVestibule(dressing);
        AddExitSign(dressing);
    }

    private void ApplyDoorLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.29f, 0.255f, 0.22f);
        RenderSettings.ambientIntensity = 0.95f;

        foreach (Light light in GetComponentsInChildren<Light>(true))
        {
            if (light.type != LightType.Directional)
                continue;

            light.color = new Color(1f, 0.79f, 0.59f);
            light.intensity = 0.95f;
        }
    }

    private void CreateMaterials()
    {
        paperMaterial = CreateMaterial("PetitionPaper", new Color(0.83f, 0.77f, 0.63f));
        paperShadeMaterial = CreateMaterial("PaperStack", new Color(0.68f, 0.61f, 0.49f));
        inkMaterial = CreateMaterial("DocumentInk", new Color(0.16f, 0.22f, 0.24f));
        leatherMaterial = CreateMaterial("LedgerLeather", new Color(0.12f, 0.23f, 0.23f), 0.08f);
        brassMaterial = CreateMaterial("LedgerBrass", new Color(0.62f, 0.39f, 0.16f), 0.55f);
        sealMaterial = CreateMaterial("PetitionSeal", new Color(0.39f, 0.09f, 0.08f), 0.08f);
        signMaterial = CreateMaterial("ExitSignEnamel", new Color(0.018f, 0.16f, 0.14f), 0.15f, new Color(0.04f, 0.34f, 0.29f));
        routeMaterial = CreateMaterial("ExitRouteGlow", new Color(0.9f, 0.53f, 0.19f), 0f, new Color(1f, 0.38f, 0.08f));
        portalWoodMaterial = CreateMaterial("ExitPortalWalnut", new Color(0.16f, 0.085f, 0.052f), 0.05f);
        nightSkyMaterial = CreateMaterial("NightBeyondExit", new Color(0.015f, 0.028f, 0.075f), 0f, new Color(0.025f, 0.055f, 0.16f));
        skylineMaterial = CreateMaterial("NightSkyline", new Color(0.009f, 0.017f, 0.045f));
        moonMaterial = CreateMaterial("Moonlight", new Color(0.78f, 0.83f, 0.82f), 0f, new Color(1.8f, 2.0f, 1.8f));
        windowMaterial = CreateMaterial("DistantWindowLight", new Color(0.72f, 0.35f, 0.09f), 0f, new Color(0.9f, 0.26f, 0.045f));
    }

    private void AddExitVestibule(Transform parent)
    {
        GameObject exitDoor = GameObject.Find("ExitDoor");
        if (exitDoor == null)
            return;

        Collider doorCollider = exitDoor.GetComponent<Collider>();
        Bounds doorway = doorCollider != null ? doorCollider.bounds : new Bounds(exitDoor.transform.position, new Vector3(3f, 5f, 0.5f));
        Vector3 forward = exitDoor.transform.forward;
        Vector3 approach = -forward;
        Vector3 frameCenter = doorway.center + approach * 0.42f;

        // A short canopy and warm timber frame make the door read as a deliberate threshold.
        CreateBlock(parent, "Exit lobby canopy", new Vector3(doorway.center.x, 6.92f, doorway.center.z - 2.8f),
            new Vector3(11.5f, 0.28f, 8.2f), portalWoodMaterial, Quaternion.identity);
        CreateBlock(parent, "Exit portal left pier", frameCenter + Vector3.left * 1.78f,
            new Vector3(0.28f, 5.5f, 0.42f), portalWoodMaterial, Quaternion.identity);
        CreateBlock(parent, "Exit portal right pier", frameCenter + Vector3.right * 1.78f,
            new Vector3(0.28f, 5.5f, 0.42f), portalWoodMaterial, Quaternion.identity);
        CreateBlock(parent, "Exit portal lintel", frameCenter + Vector3.up * 2.9f,
            new Vector3(3.9f, 0.25f, 0.42f), portalWoodMaterial, Quaternion.identity);
        CreateBlock(parent, "Exit threshold stone", doorway.center + approach * 0.12f + Vector3.down * (doorway.extents.y - 0.025f),
            new Vector3(3.25f, 0.08f, 0.72f), brassMaterial, Quaternion.identity);

        // A moonlit, low-poly city gives the opened doorway a finished destination instead of an empty horizon.
        Vector3 backdropCenter = doorway.center + forward * 4.2f + Vector3.up * 1.0f;
        CreateBlock(parent, "Night exterior backdrop", backdropCenter, new Vector3(12f, 7f, 0.18f), nightSkyMaterial, Quaternion.identity);
        AddDistantSkyline(parent, doorway.center.x, backdropCenter.y - 3.4f, backdropCenter.z - 0.11f);
        CreateSphere(parent, "Moon beyond the exit", new Vector3(doorway.center.x + 0.9f, doorway.center.y + 2.1f, backdropCenter.z - 0.2f),
            Vector3.one * 0.78f, moonMaterial);

        Vector3[] stars =
        {
            new Vector3(-3.1f, 5.9f, 0f), new Vector3(-2.25f, 4.45f, 0f), new Vector3(-1.9f, 5.45f, 0f),
            new Vector3(-0.85f, 4.95f, 0f), new Vector3(-0.45f, 5.95f, 0f), new Vector3(0.15f, 4.55f, 0f),
            new Vector3(2.05f, 5.55f, 0f), new Vector3(2.9f, 4.7f, 0f), new Vector3(3.35f, 5.95f, 0f)
        };
        for (int i = 0; i < stars.Length; i++)
        {
            Vector3 star = new Vector3(doorway.center.x + stars[i].x, stars[i].y, backdropCenter.z - 0.2f);
            float size = i % 3 == 0 ? 0.075f : 0.045f;
            CreateSphere(parent, "Distant star " + (i + 1), star, Vector3.one * size, moonMaterial);
        }

        // Warm ceiling panels preserve the library's palette inside the cooler night view.
        for (int i = 0; i < 2; i++)
        {
            float z = doorway.center.z - 4.4f + i * 3.3f;
            CreateBlock(parent, "Warm canopy light panel " + (i + 1), new Vector3(doorway.center.x, 6.76f, z),
                new Vector3(1.35f, 0.045f, 0.42f), brassMaterial, Quaternion.identity);
            GameObject lightObject = new GameObject("Warm exit lobby light " + (i + 1));
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.position = new Vector3(doorway.center.x, 6.55f, z);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.66f, 0.36f);
            light.intensity = 2.2f;
            light.range = 5.5f;
            light.shadows = LightShadows.None;
        }
    }

    private void AddDistantSkyline(Transform parent, float centerX, float groundY, float frontZ)
    {
        float[] xOffsets = { -4.5f, -3.1f, -1.95f, -0.85f, 0.2f, 1.3f, 2.55f, 3.85f };
        float[] widths = { 1.2f, 0.9f, 1.05f, 0.85f, 1.0f, 1.1f, 0.95f, 1.25f };
        float[] heights = { 1.7f, 2.4f, 1.25f, 1.9f, 1.15f, 2.2f, 1.45f, 1.9f };
        for (int i = 0; i < xOffsets.Length; i++)
        {
            Vector3 buildingPosition = new Vector3(centerX + xOffsets[i], groundY + heights[i] * 0.5f, frontZ);
            CreateBlock(parent, "Distant city silhouette " + (i + 1), buildingPosition,
                new Vector3(widths[i], heights[i], 0.08f), skylineMaterial, Quaternion.identity);

            if (i % 2 == 0)
            {
                Vector3 window = buildingPosition + Vector3.back * 0.05f + Vector3.up * 0.1f;
                CreateBlock(parent, "Distant lit window " + (i + 1), window,
                    new Vector3(0.12f, 0.18f, 0.025f), windowMaterial, Quaternion.identity);
            }
        }
    }

    private void AddDeskDocuments(Transform parent)
    {
        // A small bound register sits to the left of the papers, clear of the existing books and lamp.
        Vector3 ledgerPosition = new Vector3(2.8f, CounterTopY + 0.055f, 2.94f);
        CreateBlock(parent, "Librarian register", ledgerPosition, new Vector3(0.36f, 0.075f, 0.43f), leatherMaterial, Quaternion.Euler(0f, -5f, 0f));
        CreateBlock(parent, "Register spine", ledgerPosition + new Vector3(-0.155f, 0.041f, 0f), new Vector3(0.035f, 0.012f, 0.39f), brassMaterial, Quaternion.Euler(0f, -5f, 0f));
        CreateBlock(parent, "Register cover rule", ledgerPosition + new Vector3(0f, 0.041f, -0.14f), new Vector3(0.26f, 0.009f, 0.012f), brassMaterial, Quaternion.Euler(0f, -5f, 0f));
        AddDeskLabel(parent, "KAYIT", new Vector3(2.8f, CounterTopY + 0.097f, 2.94f), 30, 0.004f, brassMaterial.color);

        // The petition is deliberately placed in the open patch of counter between the books and lamp.
        Vector3 paperCenter = new Vector3(3.63f, CounterTopY + 0.018f, 2.94f);
        CreateBlock(parent, "Petition paper lower sheet", paperCenter + new Vector3(-0.025f, -0.012f, -0.018f),
            new Vector3(0.36f, 0.012f, 0.5f), paperShadeMaterial, Quaternion.Euler(0f, -3f, 0f));
        CreateBlock(parent, "Open petition", paperCenter, new Vector3(0.36f, 0.014f, 0.5f), paperMaterial, Quaternion.Euler(0f, -3f, 0f));

        float paperTop = paperCenter.y + 0.009f;
        for (int i = 0; i < 6; i++)
        {
            float z = 2.75f + i * 0.068f;
            float width = i == 0 ? 0.2f : 0.24f;
            CreateBlock(parent, i == 0 ? "Petition heading rule" : "Petition writing rule",
                new Vector3(3.63f, paperTop + 0.002f, z), new Vector3(width, 0.0025f, 0.006f), inkMaterial,
                Quaternion.Euler(0f, -3f, 0f));
        }

        // A red embossed seal and a brass pen make the paperwork readable from the player approach.
        CreateCylinder(parent, "Wax seal", new Vector3(3.72f, paperTop + 0.016f, 3.12f),
            new Vector3(0.075f, 0.012f, 0.075f), sealMaterial, Quaternion.identity);
        CreateCylinder(parent, "Wax seal center", new Vector3(3.72f, paperTop + 0.029f, 3.12f),
            new Vector3(0.037f, 0.004f, 0.037f), brassMaterial, Quaternion.identity);
        CreateCylinder(parent, "Desk pen", new Vector3(3.61f, CounterTopY + 0.041f, 2.74f),
            new Vector3(0.012f, 0.065f, 0.012f), brassMaterial, Quaternion.Euler(0f, 0f, 90f));

        // Small stamp placed at the far corner of the counter.
        CreateCylinder(parent, "Petition stamp base", new Vector3(4.18f, CounterTopY + 0.045f, 2.94f),
            new Vector3(0.085f, 0.025f, 0.085f), sealMaterial, Quaternion.identity);
        CreateCylinder(parent, "Petition stamp handle", new Vector3(4.18f, CounterTopY + 0.13f, 2.94f),
            new Vector3(0.035f, 0.07f, 0.035f), brassMaterial, Quaternion.identity);

        GameObject deskLightObject = new GameObject("Petition reading light");
        deskLightObject.transform.SetParent(parent, false);
        deskLightObject.transform.position = new Vector3(3.78f, 1.9f, 2.68f);
        Light deskLight = deskLightObject.AddComponent<Light>();
        deskLight.type = LightType.Point;
        deskLight.color = new Color(1f, 0.72f, 0.42f);
        deskLight.intensity = 1.15f;
        deskLight.range = 2.8f;
        deskLight.shadows = LightShadows.None;
    }

    private void AddExitSign(Transform parent)
    {
        GameObject exitDoor = GameObject.Find("ExitDoor");
        Bounds doorway = exitDoor != null && exitDoor.GetComponent<Collider>() != null
            ? exitDoor.GetComponent<Collider>().bounds
            : new Bounds(new Vector3(0f, 2.5f, 7.1f), new Vector3(3f, 5f, 0.5f));
        Vector3 approach = exitDoor != null ? -exitDoor.transform.forward : Vector3.back;
        Vector3 signPosition = doorway.center + Vector3.up * (doorway.extents.y + 0.58f) + approach * 0.34f;
        Quaternion signRotation = Quaternion.LookRotation(approach, Vector3.up);

        CreateBlock(parent, "Exit sign panel", signPosition, new Vector3(2.65f, 0.82f, 0.12f), signMaterial, signRotation);
        CreateBlock(parent, "Exit sign top brass edge", signPosition + Vector3.up * 0.42f + approach * 0.07f,
            new Vector3(2.72f, 0.035f, 0.025f), brassMaterial, signRotation);
        CreateBlock(parent, "Exit sign bottom brass edge", signPosition - Vector3.up * 0.42f + approach * 0.07f,
            new Vector3(2.72f, 0.035f, 0.025f), brassMaterial, signRotation);

        AddWorldLabel(parent, "ÇIKIŞ", signPosition + approach * 0.075f, signRotation, 68, 0.011f,
            new Color(1f, 0.94f, 0.72f));

        GameObject signGlow = new GameObject("ExitSignGlow");
        signGlow.transform.SetParent(parent, false);
        signGlow.transform.position = signPosition + approach * 0.3f;
        Light glow = signGlow.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = new Color(1f, 0.62f, 0.28f);
        glow.intensity = 1.35f;
        glow.range = 5f;
        glow.shadows = LightShadows.None;
    }

    public void ShowExitRoute(Vector3 exitPosition)
    {
        Transform oldRoute = transform.Find("ExitRoute_Runtime");
        if (oldRoute != null)
            return;

        PlayerController player = FindAnyObjectByType<PlayerController>();
        Vector3 start = player != null ? player.transform.position : new Vector3(0f, 0f, 3.4f);
        start.y = 0f;
        exitPosition.y = 0f;
        Vector3 direction = exitPosition - start;
        if (direction.sqrMagnitude < 0.01f)
            return;
        direction.Normalize();

        Transform route = new GameObject("ExitRoute_Runtime").transform;
        route.SetParent(transform, false);
        const int markerCount = 5;
        for (int i = 0; i < markerCount; i++)
        {
            Vector3 position = Vector3.Lerp(start, exitPosition, (i + 1f) / (markerCount + 1f));
            position += direction * 0.2f;
            CreateRouteArrow(route, position, direction, i);
        }
    }

    private void CreateRouteArrow(Transform parent, Vector3 position, Vector3 direction, int index)
    {
        position.y = 0.035f;
        Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;
        Quaternion facing = Quaternion.LookRotation(direction, Vector3.up);
        CreateBlock(parent, "Exit route arrow " + (index + 1) + " stem", position - direction * 0.06f,
            new Vector3(0.075f, 0.025f, 0.34f), routeMaterial, facing);
        CreateBlock(parent, "Exit route arrow " + (index + 1) + " left wing", position + direction * 0.105f - side * 0.105f,
            new Vector3(0.07f, 0.025f, 0.24f), routeMaterial, facing * Quaternion.Euler(0f, 42f, 0f));
        CreateBlock(parent, "Exit route arrow " + (index + 1) + " right wing", position + direction * 0.105f + side * 0.105f,
            new Vector3(0.07f, 0.025f, 0.24f), routeMaterial, facing * Quaternion.Euler(0f, -42f, 0f));
    }

    private static GameObject CreateBlock(Transform parent, string name, Vector3 position, Vector3 size, Material material, Quaternion rotation)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
        item.name = name;
        item.transform.SetParent(parent, false);
        item.transform.position = position;
        item.transform.rotation = rotation;
        item.transform.localScale = size;
        Collider collider = item.GetComponent<Collider>();
        if (collider != null) collider.enabled = false;
        item.GetComponent<Renderer>().sharedMaterial = material;
        return item;
    }

    private static GameObject CreateCylinder(Transform parent, string name, Vector3 position, Vector3 size, Material material, Quaternion rotation)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        item.name = name;
        item.transform.SetParent(parent, false);
        item.transform.position = position;
        item.transform.rotation = rotation;
        item.transform.localScale = size;
        Collider collider = item.GetComponent<Collider>();
        if (collider != null) collider.enabled = false;
        item.GetComponent<Renderer>().sharedMaterial = material;
        return item;
    }

    private static GameObject CreateSphere(Transform parent, string name, Vector3 position, Vector3 size, Material material)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        item.name = name;
        item.transform.SetParent(parent, false);
        item.transform.position = position;
        item.transform.localScale = size;
        Collider collider = item.GetComponent<Collider>();
        if (collider != null) collider.enabled = false;
        item.GetComponent<Renderer>().sharedMaterial = material;
        return item;
    }

    private static void AddDeskLabel(Transform parent, string text, Vector3 position, int fontSize, float characterSize, Color color)
    {
        GameObject labelObject = new GameObject(text + " label");
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.position = position;
        labelObject.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.text = text;
        label.fontSize = fontSize;
        label.characterSize = characterSize;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = color;
    }

    private static void AddWorldLabel(Transform parent, string text, Vector3 position, Quaternion rotation, int fontSize, float characterSize, Color color)
    {
        GameObject labelObject = new GameObject("Sign text - " + text);
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.position = position;
        labelObject.transform.rotation = rotation;
        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.text = text;
        label.fontSize = fontSize;
        label.characterSize = characterSize;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = color;
        label.richText = false;
    }

    private static Material CreateMaterial(string name, Color color, float metallic = 0f, Color? emission = null)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new Material(shader) { name = name };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (emission.HasValue && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission.Value);
        }
        return material;
    }

    private void OnDestroy()
    {
        Destroy(paperMaterial);
        Destroy(paperShadeMaterial);
        Destroy(inkMaterial);
        Destroy(leatherMaterial);
        Destroy(brassMaterial);
        Destroy(sealMaterial);
        Destroy(signMaterial);
        Destroy(routeMaterial);
        Destroy(portalWoodMaterial);
        Destroy(nightSkyMaterial);
        Destroy(skylineMaterial);
        Destroy(moonMaterial);
        Destroy(windowMaterial);
    }
}
