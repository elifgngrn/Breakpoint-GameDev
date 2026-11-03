using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Escape ile açılan duraklatma menüsünü çizer.</summary>
public sealed class PauseMenuUI : MonoBehaviour
{
    private const int MenuOptionCount = 9;
    private const string MasterVolumeKey = "Breakpoint.MasterVolume";
    private const string MouseSensitivityKey = "Breakpoint.MouseSensitivity";
    private const string InvertLookKey = "Breakpoint.InvertLookY";
    private const string FullscreenKey = "Breakpoint.Fullscreen";
    private const string RumbleKey = "Breakpoint.GamepadRumble";
    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;
    private Texture2D panelTexture;
    private Texture2D buttonTexture;
    private Texture2D buttonHoverTexture;
    private PlayerController playerController;
    private int selectedMenuOption;
    private GUIStyle selectedButtonStyle;
    private bool quitToMenuConfirmation;
    private bool restartConfirmation;
    private int confirmationSelection = 1;

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Paused || Gamepad.current == null)
            return;

        if (quitToMenuConfirmation || restartConfirmation)
        {
            if (Gamepad.current.dpad.left.wasPressedThisFrame || Gamepad.current.dpad.right.wasPressedThisFrame ||
                Gamepad.current.leftStick.left.wasPressedThisFrame || Gamepad.current.leftStick.right.wasPressedThisFrame)
                confirmationSelection = 1 - confirmationSelection;

            if (Gamepad.current.buttonEast.wasPressedThisFrame)
                CloseConfirmation();
            else if (Gamepad.current.buttonSouth.wasPressedThisFrame)
            {
                if (confirmationSelection == 0)
                {
                    if (restartConfirmation) GameManager.Instance.Restart();
                    else GameManager.Instance.QuitToMenu();
                }
                else CloseConfirmation();
            }
            return;
        }

        if (Gamepad.current.dpad.up.wasPressedThisFrame || Gamepad.current.leftStick.up.wasPressedThisFrame)
            selectedMenuOption = (selectedMenuOption + MenuOptionCount - 1) % MenuOptionCount;
        if (Gamepad.current.dpad.down.wasPressedThisFrame || Gamepad.current.leftStick.down.wasPressedThisFrame)
            selectedMenuOption = (selectedMenuOption + 1) % MenuOptionCount;

        float horizontal = Gamepad.current.dpad.left.wasPressedThisFrame || Gamepad.current.leftStick.left.wasPressedThisFrame
            ? -1f
            : Gamepad.current.dpad.right.wasPressedThisFrame || Gamepad.current.leftStick.right.wasPressedThisFrame ? 1f : 0f;
        if (horizontal != 0f)
            AdjustSelectedSetting(horizontal);

        if (Gamepad.current.buttonEast.wasPressedThisFrame)
            GameManager.Instance.Resume();
        else if (Gamepad.current.buttonSouth.wasPressedThisFrame)
            ActivateSelectedOption();
    }

    private void ActivateSelectedOption()
    {
        var gameManager = GameManager.Instance;
        if (gameManager == null) return;
        if (selectedMenuOption == 2)
            SetInvertLook(!(playerController != null ? playerController.tersDikeyBakış : PlayerPrefs.GetInt(InvertLookKey, 0) == 1));
        else if (selectedMenuOption == 3)
            SetFullscreen(!Screen.fullScreen);
        else if (selectedMenuOption == 4) SetRumbleEnabled(!IsRumbleEnabled);
        else if (selectedMenuOption == 5) ResetSettings();
        else if (selectedMenuOption == 6) gameManager.Resume();
        else if (selectedMenuOption == 7) OpenRestartConfirmation();
        else if (selectedMenuOption == 8) OpenQuitConfirmation();
    }

    private void AdjustSelectedSetting(float direction)
    {
        if (selectedMenuOption == 0)
        {
            AudioListener.volume = Mathf.Clamp01(AudioListener.volume + direction * 0.05f);
            PlayerPrefs.SetFloat(MasterVolumeKey, AudioListener.volume);
        }
        else if (selectedMenuOption == 1 && playerController != null)
        {
            playerController.fareHassasiyeti = Mathf.Clamp(playerController.fareHassasiyeti + direction * 0.1f, 0.5f, 5f);
            PlayerPrefs.SetFloat(MouseSensitivityKey, playerController.fareHassasiyeti);
        }
        else if (selectedMenuOption == 2)
            SetInvertLook(direction > 0f);
        else if (selectedMenuOption == 3)
            SetFullscreen(direction > 0f);
        else if (selectedMenuOption == 4)
            SetRumbleEnabled(direction > 0f);
        else
            return;

        PlayerPrefs.Save();
    }

    private void SetInvertLook(bool value)
    {
        if (playerController != null) playerController.tersDikeyBakış = value;
        PlayerPrefs.SetInt(InvertLookKey, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void SetFullscreen(bool value)
    {
        Screen.fullScreen = value;
        PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    private bool IsRumbleEnabled => PlayerPrefs.GetInt(RumbleKey, 1) == 1;

    private void SetRumbleEnabled(bool value)
    {
        PlayerPrefs.SetInt(RumbleKey, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ResetSettings()
    {
        AudioListener.volume = 1f;
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            playerController.fareHassasiyeti = 2f;
            playerController.tersDikeyBakış = false;
        }
        Screen.fullScreen = true;
        PlayerPrefs.SetFloat(MasterVolumeKey, 1f);
        PlayerPrefs.SetFloat(MouseSensitivityKey, 2f);
        PlayerPrefs.SetInt(InvertLookKey, 0);
        PlayerPrefs.SetInt(FullscreenKey, 1);
        PlayerPrefs.SetInt(RumbleKey, 1);
        PlayerPrefs.Save();
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
        const float height = 500f;
        var panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        GUI.Box(panel, GUIContent.none, panelStyle);

        if (quitToMenuConfirmation || restartConfirmation)
        {
            GUILayout.BeginArea(new Rect(panel.x + 36f, panel.y + 95f, panel.width - 72f, panel.height - 190f));
            GUILayout.FlexibleSpace();
            string prompt = restartConfirmation ? "OYUN YENİDEN BAŞLATILSIN MI?" : "ANA MENÜYE DÖNÜLSÜN MÜ?";
            GUILayout.Label(prompt, titleStyle, GUILayout.Height(56f));
            GUILayout.Space(20f);
            GUI.color = confirmationSelection == 0 ? new Color(0.65f, 1f, 0.8f) : Color.white;
            string confirmLabel = restartConfirmation ? "EVET, YENİDEN BAŞLAT" : "EVET, ANA MENÜYE DÖN";
            if (GUILayout.Button(confirmLabel, buttonStyle, GUILayout.Height(48f)))
            {
                if (restartConfirmation) gameManager.Restart();
                else gameManager.QuitToMenu();
            }
            GUILayout.Space(10f);
            GUI.color = confirmationSelection == 1 ? new Color(0.65f, 1f, 0.8f) : Color.white;
            if (GUILayout.Button("VAZGEÇ", buttonStyle, GUILayout.Height(48f)))
                CloseConfirmation();
            GUI.color = Color.white;
            GUILayout.FlexibleSpace();
            GUILayout.EndArea();
            return;
        }

        GUILayout.BeginArea(new Rect(panel.x + 36f, panel.y + 28f, panel.width - 72f, panel.height - 56f));
        GUILayout.FlexibleSpace();
        GUILayout.Label("DURAKLATILDI", titleStyle, GUILayout.Height(56f));
        GUILayout.Space(12f);

        GUI.color = selectedMenuOption == 0 ? new Color(0.65f, 1f, 0.8f) : Color.white;
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
        GUI.color = Color.white;
        DrawMouseSensitivity();
        DrawInvertLookToggle();
        DrawFullscreenToggle();
        DrawRumbleToggle();
        if (GUILayout.Button("VARSAYILAN AYARLARI SIFIRLA", GetButtonStyle(5), GUILayout.Height(36f)))
            ResetSettings();
        GUILayout.Space(12f);

        if (GUILayout.Button("DEVAM ET", GetButtonStyle(6), GUILayout.Height(52f)))
            gameManager.Resume();
        GUILayout.Space(10f);
        if (GUILayout.Button("YENİDEN BAŞLAT", GetButtonStyle(7), GUILayout.Height(52f)))
            OpenRestartConfirmation();
        GUILayout.Space(10f);
        if (GUILayout.Button("ANA MENÜ", GetButtonStyle(8), GUILayout.Height(52f)))
            OpenQuitConfirmation();

        GUILayout.FlexibleSpace();
        GUILayout.EndArea();
    }

    private void OpenQuitConfirmation()
    {
        restartConfirmation = false;
        quitToMenuConfirmation = true;
        confirmationSelection = 1;
    }

    private void OpenRestartConfirmation()
    {
        quitToMenuConfirmation = false;
        restartConfirmation = true;
        confirmationSelection = 1;
    }

    private void CloseConfirmation()
    {
        quitToMenuConfirmation = false;
        restartConfirmation = false;
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

        GUI.color = selectedMenuOption == 1 ? new Color(0.65f, 1f, 0.8f) : Color.white;
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
        GUI.color = Color.white;
    }

    private void DrawInvertLookToggle()
    {
        bool inverted = playerController != null
            ? playerController.tersDikeyBakış
            : PlayerPrefs.GetInt(InvertLookKey, 0) == 1;

        GUI.color = selectedMenuOption == 2 ? new Color(0.65f, 1f, 0.8f) : Color.white;
        bool selected = GUILayout.Toggle(inverted, "TERS DİKEY BAKIŞ", GUILayout.Height(26f));
        GUI.color = Color.white;
        if (playerController != null && selected != inverted)
            SetInvertLook(selected);
    }

    private void DrawFullscreenToggle()
    {
        GUI.color = selectedMenuOption == 3 ? new Color(0.65f, 1f, 0.8f) : Color.white;
        bool selected = GUILayout.Toggle(Screen.fullScreen, "TAM EKRAN", GUILayout.Height(26f));
        GUI.color = Color.white;
        if (selected != Screen.fullScreen)
            SetFullscreen(selected);
    }

    private void DrawRumbleToggle()
    {
        GUI.color = selectedMenuOption == 4 ? new Color(0.65f, 1f, 0.8f) : Color.white;
        bool selected = GUILayout.Toggle(IsRumbleEnabled, "GAMEPAD TİTREŞİMİ", GUILayout.Height(26f));
        GUI.color = Color.white;
        if (selected != IsRumbleEnabled)
            SetRumbleEnabled(selected);
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
