using System;
using Fusion;
using UnityEngine;

public enum PlayerGender
{
    Male   = 0,
    Female = 1,
}

public enum AppearanceSlot
{
    // ── Shared ──
    Hair        = 0,
    Eyebrow     = 1,
    Shoes       = 2,
    Skin        = 3,

    // ── Male only ──
    Shirt       = 4,
    Pants       = 5,
    Suit        = 6,
    SuitButtons = 7,
    Belt        = 8,
    Belthead    = 9,

    // ── Female only ──
    Earring     = 10,
    ShirtInner  = 11,
    Skirt       = 12,

}
public class PlayerProfile : NetworkBehaviour
{
    // Slot count must match or exceed AppearanceSlot enum length
    private const int SlotCapacity = 32;

    [Networked, Capacity(24)]
    public string PlayerName { get => default; set { } }

    [Networked] public PlayerGender Gender       { get; set; }
    [Networked] public NetworkBool  ProfileReady { get; set; }

    // Single networked array replaces all individual Color fields
    // Adding a new slot = just add to enum, no new [Networked] field needed
    [Networked, Capacity(SlotCapacity)]
    public NetworkArray<Color> AppearanceColors { get; }

    public Color GetColor(AppearanceSlot slot)
    {
        return AppearanceColors[(int)slot];
    }

    private void SetColorInternal(AppearanceSlot slot, Color color)
    {
        AppearanceColors.Set((int)slot, color);
    }

    public void SendProfileToServer()
    {
        var local = LocalPlayerData.Instance; // fall back

        if (local == null)
        {
            Debug.LogWarning("[PlayerProfile] LocalPlayerData not found — using fallback.");
            RPC_SubmitProfile("Guest", PlayerGender.Male);
            return;
        }

        RPC_SubmitProfile(local.PlayerName, local.Gender);

        // Send each color individually
        // This avoids large RPC payloads and works with any slot count
        int count = Enum.GetValues(typeof(AppearanceSlot)).Length;
        for (int i = 0; i < count; i++)
        {
            AppearanceSlot slot = (AppearanceSlot)i;
            RPC_UpdateColor(slot, local.GetColor(slot));
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SubmitProfile(string name, PlayerGender gender)
    {
        PlayerName   = name;
        Gender       = gender;
        ProfileReady = true;

        Debug.Log($"[PlayerProfile] Profile set | Name={PlayerName} | Gender={Gender}");
        var gameplay = FindAnyObjectByType<Gameplay>();
        if (gameplay != null)
        {
            gameplay.OnProfileReceived(Object.InputAuthority, name, gender);
        }
        else
        {
            Debug.Log("[PlayerProfile] Gameplay is null");
        }
            
    }
    public void ChangeColor(AppearanceSlot slot, Color color)
    {
        if (!HasInputAuthority)
            return;

        // Persist locally so color survives room travel
        LocalPlayerData.Instance?.SetColor(slot, color);

        RPC_UpdateColor(slot, color);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_UpdateColor(AppearanceSlot slot, Color color)
    {
        SetColorInternal(slot, color);
    }

    // Legacy fallback
    public void ApplyProfile(string playerName, PlayerGender gender)
    {
        if (!HasStateAuthority) return;
        PlayerName   = playerName;
        Gender       = gender;
        ProfileReady = true;
    }
}