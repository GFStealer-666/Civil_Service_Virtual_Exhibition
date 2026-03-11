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
    private Color[] _colors;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeDefaults();
    }

    private void InitializeDefaults()
    {
        int count = Enum.GetValues(typeof(AppearanceSlot)).Length;
        _colors = new Color[count];

        for (int i = 0; i < count; i++)
            _colors[i] = Color.clear;
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
}