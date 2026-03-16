using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomCapacityConfig", menuName = "Exhibition/Room Capacity Config")]
public class RoomCapacityConfig : ScriptableObject
{
    [Serializable]
    public struct RoomLimit
    {
        public string scenePath;
        public int    maxPlayers;
    }

    [SerializeField] private List<RoomLimit> _limits = new();
    [SerializeField] private int             _defaultMaxPlayers = 20;

    private Dictionary<string, int> _lookup;

    private void OnEnable()
    {
        _lookup = new Dictionary<string, int>();
        foreach (var entry in _limits)
            _lookup[entry.scenePath] = entry.maxPlayers;
    }

    public int GetMaxPlayers(string scenePath)
    {
        if (_lookup == null) OnEnable();
        return _lookup.TryGetValue(scenePath, out int cap) ? cap : _defaultMaxPlayers;
    }
}