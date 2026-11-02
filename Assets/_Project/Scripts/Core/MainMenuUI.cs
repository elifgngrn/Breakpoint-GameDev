using UnityEngine;

/// <summary>Başlangıç menüsünü çizer ve GameManager akışına bağlar.</summary>
public sealed class MainMenuUI : MonoBehaviour
{
    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle buttonStyle;
    private Texture2D panelTexture;
    private Texture2D buttonTexture;
    private Texture2D buttonHoverTexture;

    private void Awake()
    {
        Camera menuCamera = FindFirstObjectByType<Camera>();
        if (menuCamera == null)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            menuCamera = cameraObject.AddComponent<Camera>();
        }

        menuCamera.gameObject.tag = "MainCamera";
        menuCamera.enabled = true;
        menuCamera.targetDisplay = 0;
        menuCamera.clearFlags = CameraClearFlags.SolidColor;
        menuCamera.backgroundColor = new Color(0.035f, 0.055f, 0.075f, 1f);
        menuCamera.cullingMask = Physics.DefaultRaycastLayers;
    }

    private void OnGUI()
    {
        EnsureStyles();

        GUI.color = new Color(0.035f, 0.055f, 0.075f, 1f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float width = Mathf.Min(560f, Screen.width - 40f);
        float height = 390f;
        var panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        GUI.Box(panel, GUIContent.none, panelStyle);

        GUILayout.BeginArea(new Rect(panel.x + 42f, panel.y + 34f, panel.width - 84f, panel.height - 68f));
        GUILayout.FlexibleSpace();
        GUILayout.Label("BREAKPOINT", titleStyle, GUILayout.Height(58f));
        GUILayout.Space(8f);
        GUILayout.Label("VİZE HAFTASI · KÜTÜPHANE FİRARI", subtitleStyle, GUILayout.Height(30f));
        GUILayout.Space(28f);

        if (GUILayout.Button("OYUNA BAŞLA", buttonStyle, GUILayout.Height(58f)))
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StartNewGame();
            else
                Debug.LogError("MainMenuUI: GameManager bulunamadı.", this);
        }

        GUILayout.Space(12f);
        if (GUILayout.Button("ÇIKIŞ", buttonStyle, GUILayout.Height(48f)))
            Application.Quit();

        GUILayout.FlexibleSpace();
        GUILayout.Label("Gece 04:00 — çıkış için robotun yardımına ihtiyacın var.", subtitleStyle, GUILayout.Height(24f));
        GUILayout.EndArea();
    }

    private void EnsureStyles()
    {
        if (panelStyle != null) return;

        panelTexture = MakeTexture(new Color(0.07f, 0.11f, 0.14f, 0.97f));
        buttonTexture = MakeTexture(new Color(0.13f, 0.34f, 0.38f, 1f));
        buttonHoverTexture = MakeTexture(new Color(0.19f, 0.48f, 0.49f, 1f));

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = panelTexture },
            border = new RectOffset(16, 16, 16, 16)
        };

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.Clamp(Screen.width / 32, 28, 48),
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.91f, 0.95f, 0.91f) }
        };

        subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14,
            normal = { textColor = new Color(0.64f, 0.76f, 0.75f) }
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { background = buttonTexture, textColor = Color.white },
            hover = { background = buttonHoverTexture, textColor = Color.white },
            active = { background = buttonHoverTexture, textColor = Color.white }
        };
    }

    private static Texture2D MakeTexture(Color color)
    {
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private void OnDestroy()
    {
        if (panelTexture != null) Destroy(panelTexture);
        if (buttonTexture != null) Destroy(buttonTexture);
        if (buttonHoverTexture != null) Destroy(buttonHoverTexture);
    }
}
