using System;
using UnityEngine;

public class LocalPlayerData : MonoBehaviour
{
    public static LocalPlayerData Instance { get; private set; }

    [Header("Profile")]
    public string PlayerID = "";
    public string PlayerToken = "";
    public string PlayerName = "Guest1";
    public PlayerGender Gender = PlayerGender.Male;
    public string Organization = "";
    public bool IsGuest = false;

    [Space]
    [Header("Contact")]
    public string Email = "";
    public string PhoneNumber = "";

    [Space]
    [Header("Local Settings")]
    [Range(0f, 1f)] public float BgmVolume = 0.5f;
    [Range(0f, 1f)] public float EffectVolume = 0.5f;
    public bool HasInitializedAppearance = false;

    private Color[] _colors;

    public bool HasValidToken => !string.IsNullOrWhiteSpace(PlayerToken);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeDefaults();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void InitializeDefaults()
    {
        int count = Enum.GetValues(typeof(AppearanceSlot)).Length;
        _colors = new Color[count];

        for (int i = 0; i < count; i++)
            _colors[i] = Color.clear;

        RandomizeAppearance();
        PlayerToken = "2316d8bc1cd981320552a5c96e4ea256917cd31dd5a59b1e35a70ae0bfd6f65e"; // Temporary token for testing
    }

    public void RandomizeAppearance()
    {
        var outfit = Gender == PlayerGender.Male
            ? MaleOutfitPalette.GetRandomOutfit()
            : FemaleOutfitPalette.GetRandomOutfit();

        foreach (var kvp in outfit)
            SetColor(kvp.Key, kvp.Value);

        HasInitializedAppearance = true;
        Debug.Log($"[LocalPlayerData] Appearance randomized for {Gender}");
    }

    private void EnsureColorBuffer()
    {
        int count = Enum.GetValues(typeof(AppearanceSlot)).Length;

        if (_colors == null || _colors.Length != count)
            _colors = new Color[count];
    }

    public void ClearForSignOut()
    {
        PlayerID = string.Empty;
        PlayerToken = string.Empty;
        PlayerName = string.Empty;
        Organization = string.Empty;
        IsGuest = false;
        Gender = PlayerGender.Male;
        Email = string.Empty;
        PhoneNumber = string.Empty;
        HasInitializedAppearance = false;

        EnsureColorBuffer();

        for (int i = 0; i < _colors.Length; i++)
            _colors[i] = Color.clear;

        Debug.Log("[LocalPlayerData] Cleared for sign out.");
    }

    public void SetColor(AppearanceSlot slot, Color color)
    {
        EnsureColorBuffer();
        _colors[(int)slot] = color;
    }

    public Color GetColor(AppearanceSlot slot)
    {
        EnsureColorBuffer();
        return _colors[(int)slot];
    }

    public bool HasColor(AppearanceSlot slot)
    {
        EnsureColorBuffer();
        return _colors[(int)slot] != Color.clear;
    }

    public void SetContact(string email, string phoneNumber)
    {
        Email = email ?? "";
        PhoneNumber = phoneNumber ?? "";
    }

    public void SetAudioSettings(float bgmVolume, float effectVolume)
    {
        BgmVolume = Mathf.Clamp01(bgmVolume);
        EffectVolume = Mathf.Clamp01(effectVolume);
    }

    public void SetBgmVolume(float value)
    {
        BgmVolume = Mathf.Clamp01(value);
    }

    public void SetEffectVolume(float value)
    {
        EffectVolume = Mathf.Clamp01(value);
    }

    public void SetAuth(string playerId, string playerToken)
    {
        PlayerID = playerId ?? "";
        PlayerToken = string.IsNullOrWhiteSpace(playerToken) ? "" : playerToken.Trim();
    }

    public void ClearToken()
    {
        PlayerToken = string.Empty;
    }
}