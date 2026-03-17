using System;
using UnityEngine;

public class LocalPlayerData : MonoBehaviour
{
    public static LocalPlayerData Instance { get; private set; }

    [Header("Profile")]
    public string       PlayerName   = "Guest1";
    public PlayerGender Gender       = PlayerGender.Male;
    public string       Organization = "";
    public bool         IsGuest      = false;
    [Space]
    [Header("Contact")]
    public string Email = "";
    public string PhoneNumber = "";

    [Space]
    [Header("Local Settings")]
    [Range(0f, 1f)] public float BgmVolume = 1f;
    [Range(0f, 1f)] public float EffectVolume = 1f;
    public bool HasInitializedAppearance = false;
    private Color[] _colors;


    private void Awake()
    {
        // Auto-create and persist if not already existing
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // ← critical
        InitializeDefaults();
    }

    private void InitializeDefaults()
    {
        int count = Enum.GetValues(typeof(AppearanceSlot)).Length;
        _colors = new Color[count];

        for (int i = 0; i < count; i++)
            _colors[i] = Color.clear;

        RandomizeAppearance();
    }
    public void RandomizeAppearance()
    {
        var outfit = Gender == PlayerGender.Male
            ? MaleOutfitPalette.GetRandomOutfit()
            : FemaleOutfitPalette.GetRandomOutfit();

        foreach (var kvp in outfit)
            SetColor(kvp.Key, kvp.Value);

        Debug.Log($"[LocalPlayerData] Appearance randomized for {Gender} ");
    }
    public void SetColor(AppearanceSlot slot, Color color)
    {
        _colors[(int)slot] = color;
    }

    public Color GetColor(AppearanceSlot slot)
    {
        return _colors[(int)slot];
    }

    public bool HasColor(AppearanceSlot slot)
    {
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
}