using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerAppearance : NetworkBehaviour
{
    private readonly Dictionary<string, Material> _materialMap = new();
    private PlayerProfile         _profile;
    private AppearanceSlotMapping _mapping;
    private bool                  _uiSeeded = false;

    public override void Spawned()
    {
        _profile = GetComponent<PlayerProfile>();
        _mapping = GetComponent<AppearanceSlotMapping>();

        if (_mapping == null)
            Debug.LogError("[PlayerAppearance] No AppearanceSlotMapping found.");

        BuildMaterialMap();

        if (HasStateAuthority)
            SeedDefaultColors();
    }

    private void BuildMaterialMap()
    {
        _materialMap.Clear();

        var renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var smr in renderers)
            if (smr.materials.Length > 0)
                _materialMap[smr.gameObject.name] = smr.materials[0];

        Debug.Log($"[PlayerAppearance] Material map: {_materialMap.Count} entries");
    }

    private void SeedDefaultColors()
    {
        foreach (AppearanceSlot slot in Enum.GetValues(typeof(AppearanceSlot)))
        {
            int index = (int)slot;
            if (index >= _profile.AppearanceColors.Length) continue;
            if (_profile.ProfileReady) continue;

            string[] meshNames = _mapping.GetMeshNames(slot);
            if (meshNames == null || meshNames.Length == 0) continue;

            foreach (var meshName in meshNames)
            {
                if (_materialMap.TryGetValue(meshName, out var mat))
                {
                    _profile.AppearanceColors.Set(index, mat.color);
                    break;
                }
            }
        }

        Debug.Log("[PlayerAppearance] Default colors seeded.");
    }

    public override void Render()
    {
        if (_profile == null || _mapping == null)
            return;

        foreach (AppearanceSlot slot in Enum.GetValues(typeof(AppearanceSlot)))
        {
            int index = (int)slot;
            if (index >= _profile.AppearanceColors.Length) continue;

            Color color = _profile.GetColor(slot);

            // Skip unset (black/transparent) slots
            if (color == Color.clear || color == new Color(0, 0, 0, 0))
                continue;

            ApplySlot(slot, color);
        }

        if (!_uiSeeded && HasInputAuthority && ColorsAreReady())
        {
            _uiSeeded = true;

            var ui = FindFirstObjectByType<AppearanceCustomizeUI>();
            ui?.RefreshSlotColors();

            Debug.Log("[PlayerAppearance] UI slot colors refreshed.");
        }
    }

    private bool ColorsAreReady()
    {
        foreach (AppearanceSlot slot in Enum.GetValues(typeof(AppearanceSlot)))
        {
            int index = (int)slot;
            if (index >= _profile.AppearanceColors.Length) continue;

            Color c = _profile.GetColor(slot);
            if (c != Color.clear && c != new Color(0, 0, 0, 0))
                return true;
        }
        return false;
    }

    private void ApplySlot(AppearanceSlot slot, Color color)
    {
        var names = _mapping.GetMeshNames(slot);
        if (names == null) return;

        foreach (var meshName in names)
        {
            if (_materialMap.TryGetValue(meshName, out var mat))
                mat.color = color;
        }
    }

    public Color GetMaterialColor(AppearanceSlot slot)
    {
        if (_mapping == null) return Color.white;

        string[] meshNames = _mapping.GetMeshNames(slot);
        if (meshNames == null || meshNames.Length == 0) return Color.white;

        foreach (var meshName in meshNames)
        {
            if (_materialMap.TryGetValue(meshName, out var mat))
                return mat.color;
        }

        return Color.white;
    }
}