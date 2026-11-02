using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Escape ile açılan duraklatma menüsünü çizer.</summary>
public sealed class PauseMenuUI : MonoBehaviour
{
    private const string MasterVolumeKey = "Breakpoint.MasterVolume";
    private const string MouseSensitivityKey = "Breakpoint.MouseSensitivity";
    private const string InvertLookKey = "Breakpoint.InvertLookY";
    private const string FullscreenKey = "Breakpoint.Fullscreen";
    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;
    private Texture2D panelTexture;
    private Texture2D buttonTexture;
    private Texture2D buttonHoverTexture;
    private PlayerController playerController;
    private int selectedMenuOption;
    private GUIStyle selectedButtonStyle;

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Paused || Gamepad.current == null)
            return;

        if (Gamepad.current.dpad.up.wasPressedThisFrame || Gamepad.current.leftStick.up.wasPressedThisFrame)
            selectedMenuOption = (selectedMenuOption + 2) % 3;
        if (Gamepad.current.dpad.down.wasPressedThisFrame || Gamepad.current.leftStick.down.wasPressedThisFrame)
            selectedMenuOption = (selectedMenuOption + 1) % 3;

        if (Gamepad.current.buttonEast.wasPressedThisFrame)
            GameManager.Instance.Resume();
        else if (Gamepad.current.buttonSouth.wasPressedThisFrame)
            ActivateSelectedOption();
    }

    private void ActivateSelectedOption()
    {
        var gameManager = GameManager.Instance;
        if (gameManager == null) return;
        if (selectedMenuOption == 0) gameManager.Resume();
        else if (selectedMenuOption == 1) gameManager.Restart();
        else gameManager.QuitToMenu();
    }

    private void Awake()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(MasterVolumeKey, AudioListener.volume);
        Screen.fullScreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
    }

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
        const float height = 430f;
        var panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        GUI.Box(panel, GUIContent.none, panelStyle);

        GUILayout.BeginArea(new Rect(panel.x + 36f, panel.y + 28f, panel.width - 72f, panel.height - 56f));
        GUILayout.FlexibleSpace();
        GUILayout.Label("DURAKLATILDI", titleStyle, GUILayout.Height(56f));
        GUILayout.Space(12f);

        GUILayout.BeginHorizontal();
        GUILayout.Label($"ANA SES  {Mathf.RoundToInt(AudioListener.volume * 100f)}%", GUILayout.Width(120f), GUILayout.Height(28f));
        float selectedVolume = GUILayout.HorizontalSlider(AudioListener.volume, 0f, 1f, GUILayout.Height(28f));
        if (!Mathf.Approximately(selectedVolume, AudioListener.volume))
        {
            AudioListener.volume = selectedVolume;
            PlayerPrefs.SetFloat(MasterVolumeKey, selectedVolume);
            PlayerPrefs.Save();
        }
        GUILayout.EndHorizontal();
        DrawMouseSensitivity();
        DrawInvertLookToggle();
        DrawFullscreenToggle();
        GUILayout.Space(12f);

        if (GUILayout.Button("DEVAM ET", GetButtonStyle(0), GUILayout.Height(52f)))
            gameManager.Resume();
        GUILayout.Space(10f);
        if (GUILayout.Button("YENİDEN BAŞLAT", GetButtonStyle(1), GUILayout.Height(52f)))
            gameManager.Restart();
        GUILayout.Space(10f);
        if (GUILayout.Button("ANA MENÜ", GetButtonStyle(2), GUILayout.Height(52f)))
            gameManager.QuitToMenu();

        GUILayout.FlexibleSpace();
        GUILayout.EndArea();
    }

    private GUIStyle GetButtonStyle(int option)
    {
        if (option != selectedMenuOption) return buttonStyle;
        selectedButtonStyle ??= new GUIStyle(buttonStyle);
        selectedButtonStyle.normal.background = buttonHoverTexture;
        return selectedButtonStyle;
    }

    private void DrawMouseSensitivity()
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
            if (playerController != null)
                playerController.fareHassasiyeti = PlayerPrefs.GetFloat(MouseSensitivityKey, playerController.fareHassasiyeti);
        }

        GUILayout.BeginHorizontal();
        float sensitivity = playerController != null ? playerController.fareHassasiyeti : PlayerPrefs.GetFloat(MouseSensitivityKey, 2f);
        GUILayout.Label($"FARE HASSASİYETİ  {sensitivity:0.0}", GUILayout.Width(170f), GUILayout.Height(28f));
        float selectedSensitivity = GUILayout.HorizontalSlider(sensitivity, 0.5f, 5f, GUILayout.Height(28f));
        if (playerController != null && !Mathf.Approximately(selectedSensitivity, sensitivity))
        {
            playerController.fareHassasiyeti = selectedSensitivity;
            PlayerPrefs.SetFloat(MouseSensitivityKey, selectedSensitivity);
            PlayerPrefs.Save();
        }
        GUILayout.EndHorizontal();
    }

    private void DrawInvertLookToggle()
    {
        bool inverted = playerController != null
            ? playerController.tersDikeyBakış
            : PlayerPrefs.GetInt(InvertLookKey, 0) == 1;

        bool selected = GUILayout.Toggle(inverted, "TERS DİKEY BAKIŞ", GUILayout.Height(26f));
        if (playerController != null && selected != inverted)
        {
            playerController.tersDikeyBakış = selected;
            PlayerPrefs.SetInt(InvertLookKey, selected ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    private void DrawFullscreenToggle()
    {
        bool selected = GUILayout.Toggle(Screen.fullScreen, "TAM EKRAN", GUILayout.Height(26f));
        if (selected != Screen.fullScreen)
        {
            Screen.fullScreen = selected;
            PlayerPrefs.SetInt(FullscreenKey, selected ? 1 : 0);
            PlayerPrefs.Save();
        }
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
