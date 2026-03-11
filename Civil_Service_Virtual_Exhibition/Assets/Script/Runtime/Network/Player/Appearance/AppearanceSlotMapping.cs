using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SlotEntry
{
    public AppearanceSlot Slot;
    public string[] MeshNames;
}

public class AppearanceSlotMapping : MonoBehaviour
{
    [SerializeField] public List<SlotEntry> Slots = new();

    public string[] GetMeshNames(AppearanceSlot slot)
    {
        foreach (var entry in Slots)
            if (entry.Slot == slot)
                return entry.MeshNames;
        return Array.Empty<string>();
    }
}
