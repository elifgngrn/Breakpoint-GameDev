using UnityEngine;

/// <summary>Escape ile açılan duraklatma menüsünü çizer.</summary>
public sealed class PauseMenuUI : MonoBehaviour
{
    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;
    private Texture2D panelTexture;
    private Texture2D buttonTexture;
    private Texture2D buttonHoverTexture;

    private void OnGUI()
    {
        var gameManager = GameManager.Instance;
        if (gameManager == null || gameManager.State != GameState.Paused)
            return;

        EnsureStyles();
        GUI.color = new Color(0f, 0f, 0f, 0.58f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float width = Mathf.Min(460f, Screen.width - 40f);
        const float height = 340f;
        var panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        GUI.Box(panel, GUIContent.none, panelStyle);

        GUILayout.BeginArea(new Rect(panel.x + 36f, panel.y + 28f, panel.width - 72f, panel.height - 56f));
        GUILayout.FlexibleSpace();
        GUILayout.Label("DURAKLATILDI", titleStyle, GUILayout.Height(56f));
        GUILayout.Space(20f);

        if (GUILayout.Button("DEVAM ET", buttonStyle, GUILayout.Height(52f)))
            gameManager.Resume();
        GUILayout.Space(10f);
        if (GUILayout.Button("YENİDEN BAŞLAT", buttonStyle, GUILayout.Height(52f)))
            gameManager.Restart();
        GUILayout.Space(10f);
        if (GUILayout.Button("ANA MENÜ", buttonStyle, GUILayout.Height(52f)))
            gameManager.QuitToMenu();

        GUILayout.FlexibleSpace();
        GUILayout.EndArea();
    }

    private void EnsureStyles()
    {
        if (panelStyle != null) return;

        panelTexture = MakeTexture(new Color(0.07f, 0.11f, 0.14f, 0.98f));
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
            fontSize = Mathf.Clamp(Screen.width / 32, 26, 40),
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.91f, 0.95f, 0.91f) }
        };
        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 17,
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
