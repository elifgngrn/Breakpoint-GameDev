using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Başlangıç menüsünü çizer ve GameManager akışına bağlar.</summary>
public sealed class MainMenuUI : MonoBehaviour
{
    private const string BestEscapeTimeKey = "Breakpoint.BestEscapeTimeSeconds";
    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle buttonStyle;
    private Texture2D panelTexture;
    private Texture2D buttonTexture;
    private Texture2D buttonHoverTexture;
    private int selectedOption;
    private GUIStyle selectedButtonStyle;
    private bool quitConfirmationOpen;

    private void Update()
    {
        if (Gamepad.current == null) return;

        if (Gamepad.current.dpad.up.wasPressedThisFrame || Gamepad.current.dpad.down.wasPressedThisFrame ||
            Gamepad.current.leftStick.up.wasPressedThisFrame || Gamepad.current.leftStick.down.wasPressedThisFrame)
            selectedOption = 1 - selectedOption;

        if (quitConfirmationOpen)
        {
            if (Gamepad.current.buttonSouth.wasPressedThisFrame)
            {
                if (selectedOption == 0) Application.Quit();
                else quitConfirmationOpen = false;
            }
            else if (Gamepad.current.buttonEast.wasPressedThisFrame)
            {
                quitConfirmationOpen = false;
                selectedOption = 1;
            }
            return;
        }

        if (Gamepad.current.buttonSouth.wasPressedThisFrame)
            ActivateSelectedOption();
        else if (Gamepad.current.buttonEast.wasPressedThisFrame)
        {
            quitConfirmationOpen = true;
            selectedOption = 0;
        }
    }

    private void ActivateSelectedOption()
    {
        if (selectedOption == 1)
        {
            quitConfirmationOpen = true;
            selectedOption = 0;
            return;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.StartNewGame();
        else
            Debug.LogError("MainMenuUI: GameManager bulunamadı.", this);
    }

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

        if (quitConfirmationOpen)
        {
            DrawQuitConfirmation(panel);
            return;
        }

        GUILayout.BeginArea(new Rect(panel.x + 42f, panel.y + 34f, panel.width - 84f, panel.height - 68f));
        GUILayout.FlexibleSpace();
        GUILayout.Label("BREAKPOINT", titleStyle, GUILayout.Height(58f));
        GUILayout.Space(8f);
        GUILayout.Label("VİZE HAFTASI · KÜTÜPHANE FİRARI", subtitleStyle, GUILayout.Height(30f));
        GUILayout.Space(28f);

        if (GUILayout.Button("OYUNA BAŞLA", GetButtonStyle(0), GUILayout.Height(58f)))
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StartNewGame();
            else
                Debug.LogError("MainMenuUI: GameManager bulunamadı.", this);
        }

        GUILayout.Space(12f);
        if (GUILayout.Button("ÇIKIŞ", GetButtonStyle(1), GUILayout.Height(48f)))
        {
            quitConfirmationOpen = true;
            selectedOption = 0;
        }

        GUILayout.FlexibleSpace();
        int bestEscapeTime = PlayerPrefs.GetInt(BestEscapeTimeKey, int.MaxValue);
        if (bestEscapeTime != int.MaxValue)
        {
            string bestFormatted = $"{bestEscapeTime / 60:00}:{bestEscapeTime % 60:00}";
            GUILayout.Label($"EN İYİ KAÇIŞ  {bestFormatted}", subtitleStyle, GUILayout.Height(24f));
            GUILayout.Space(6f);
        }
        GUILayout.Label("Gece 04:00 — çıkış için robotun yardımına ihtiyacın var.", subtitleStyle, GUILayout.Height(24f));
        GUILayout.EndArea();
    }

    private void DrawQuitConfirmation(Rect panel)
    {
        GUILayout.BeginArea(new Rect(panel.x + 42f, panel.y + 78f, panel.width - 84f, panel.height - 156f));
        GUILayout.FlexibleSpace();
        GUILayout.Label("OYUNDAN ÇIKILSIN MI?", titleStyle, GUILayout.Height(58f));
        GUILayout.Space(26f);
        if (GUILayout.Button("EVET, ÇIK", GetButtonStyle(0), GUILayout.Height(56f)))
            Application.Quit();
        GUILayout.Space(12f);
        if (GUILayout.Button("VAZGEÇ", GetButtonStyle(1), GUILayout.Height(52f)))
        {
            quitConfirmationOpen = false;
            selectedOption = 1;
        }
        GUILayout.FlexibleSpace();
        GUILayout.Label("A/× onayla   ·   B/○ vazgeç", subtitleStyle, GUILayout.Height(24f));
        GUILayout.EndArea();
    }

    private GUIStyle GetButtonStyle(int option)
    {
        if (option != selectedOption) return buttonStyle;
        selectedButtonStyle ??= new GUIStyle(buttonStyle);
        selectedButtonStyle.normal.background = buttonHoverTexture;
        return selectedButtonStyle;
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
