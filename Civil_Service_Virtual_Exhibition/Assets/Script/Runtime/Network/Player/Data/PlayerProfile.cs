using System;
using Fusion;
using UnityEngine;

public enum PlayerGender { Male = 0, Female = 1 }

public enum AppearanceSlot
{
    Hair = 0, Eyebrow = 1, Shoes = 2, Skin = 3,
    Shirt = 4, Pants = 5, Suit = 6, SuitButtons = 7, Belt = 8, Belthead = 9,
    Earring = 10, ShirtInner = 11, Skirt = 12,
}

public class PlayerProfile : NetworkBehaviour
{
    private const int SlotCapacity = 32;

    [Networked, Capacity(24)] public string       PlayerName   { get => default; set { } }
    [Networked]               public PlayerGender  Gender       { get; set; }
    [Networked]               public NetworkBool   ProfileReady { get; set; }

    [Networked, Capacity(SlotCapacity)]
    public NetworkArray<Color> AppearanceColors { get; }

    public Color GetColor(AppearanceSlot slot) => AppearanceColors[(int)slot];

    /// <summary>
    /// In Shared mode we are always StateAuthority for our own player object,
    /// so we write directly — no RPC needed.
    /// </summary>
    public void ApplyLocalProfile()
    {
        // Only the owning client should write their own profile
        if (!HasStateAuthority) return;

        var local = LocalPlayerData.Instance;
        if (local == null)
        {
            Debug.LogWarning("[PlayerProfile] LocalPlayerData missing — applying fallback.");
            ApplyFallbackProfile();  
            return;
        }

        PlayerName   = local.PlayerName;
        Gender       = local.Gender;
        ProfileReady = true;

        int count = Enum.GetValues(typeof(AppearanceSlot)).Length;
        for (int i = 0; i < count; i++)
            AppearanceColors.Set(i, local.GetColor((AppearanceSlot)i));

        Debug.Log($"[PlayerProfile] Profile applied | Name={PlayerName} | Gender={Gender}");
    }

    private void ApplyFallbackProfile()
    {
        PlayerName = "Guest";
        // Gender     = UnityEngine.Random.Range(0,2) == 0 ? PlayerGender.Male : PlayerGender.Female;
        Gender = PlayerGender.Female;
        var outfit = MaleOutfitPalette.GetRandomOutfit();

        int count = Enum.GetValues(typeof(AppearanceSlot)).Length;
        for (int i = 0; i < count; i++)
        {
            AppearanceSlot slot = (AppearanceSlot)i;

            // Use outfit color if available, otherwise white
            Color color = outfit.TryGetValue(slot, out var c) ? c : Color.white;
            AppearanceColors.Set(i, color);
        }

        ProfileReady = true;

        Debug.Log("[PlayerProfile] Fallback profile applied with random outfit.");
    }

    public void ChangeColor(AppearanceSlot slot, Color color)
    {
        if (!HasStateAuthority) return;

        // Persist so the color survives room travel via LocalPlayerData
        LocalPlayerData.Instance?.SetColor(slot, color);
        AppearanceColors.Set((int)slot, color);
    }
}