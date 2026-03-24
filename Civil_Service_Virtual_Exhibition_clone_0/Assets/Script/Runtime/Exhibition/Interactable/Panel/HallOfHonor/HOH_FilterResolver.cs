using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HOH_FilterMappingEntry
{
    public string key;
    public HOH_FilterOption option;
}

public class HOH_FilterResolver : MonoBehaviour
{
    [Header("Ministry Mappings")]
    [SerializeField] private List<HOH_FilterMappingEntry> ministryMappings = new();

    [Header("Province Mappings")]
    [SerializeField] private List<HOH_FilterMappingEntry> provinceMappings = new();

    [Header("University Mappings")]
    [SerializeField] private List<HOH_FilterMappingEntry> universityMappings = new();

    private readonly Dictionary<string, HOH_FilterOption> _ministryLookup = new();
    private readonly Dictionary<string, HOH_FilterOption> _provinceLookup = new();
    private readonly Dictionary<string, HOH_FilterOption> _universityLookup = new();

    private void Awake()
    {
        RebuildLookups();
    }

    private void OnValidate()
    {
        RebuildLookups();
    }

    public bool Matches(HOH_CategoryKind categoryKind, HOH_UnitDto unit, HOH_FilterOption filter)
    {
        if (filter == HOH_FilterOption.All)
            return true;

        if (unit == null)
            return false;

        return categoryKind switch
        {
            HOH_CategoryKind.Ministry => MatchesMinistry(unit, filter),
            HOH_CategoryKind.Province => MatchesProvince(unit, filter),
            HOH_CategoryKind.University => MatchesUniversity(unit, filter),
            _ => false
        };
    }

    private bool MatchesMinistry(HOH_UnitDto unit, HOH_FilterOption filter)
    {
        return TryMatch(
            _ministryLookup,
            filter,
            unit.unit,
            unit.unitEn,
            unit.ministry,
            unit.ministryEn
        );
    }

    private bool MatchesProvince(HOH_UnitDto unit, HOH_FilterOption filter)
    {
        return TryMatch(
            _provinceLookup,
            filter,
            unit.unit,
            unit.unitEn
        );
    }

    private bool MatchesUniversity(HOH_UnitDto unit, HOH_FilterOption filter)
    {
        return TryMatch(
            _universityLookup,
            filter,
            unit.unit,
            unit.unitEn
        );
    }

    private bool TryMatch(
        Dictionary<string, HOH_FilterOption> lookup,
        HOH_FilterOption filter,
        params string[] candidateKeys)
    {
        if (lookup == null || lookup.Count == 0)
            return false;

        for (int i = 0; i < candidateKeys.Length; i++)
        {
            string normalized = Normalize(candidateKeys[i]);
            if (string.IsNullOrWhiteSpace(normalized))
                continue;

            if (lookup.TryGetValue(normalized, out HOH_FilterOption mapped))
                return mapped == filter;
        }

        return false;
    }

    private void RebuildLookups()
    {
        BuildLookup(ministryMappings, _ministryLookup);
        BuildLookup(provinceMappings, _provinceLookup);
        BuildLookup(universityMappings, _universityLookup);
    }

    private void BuildLookup(List<HOH_FilterMappingEntry> source, Dictionary<string, HOH_FilterOption> target)
    {
        target.Clear();

        if (source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            HOH_FilterMappingEntry entry = source[i];
            if (entry == null)
                continue;

            string normalized = Normalize(entry.key);
            if (string.IsNullOrWhiteSpace(normalized))
                continue;

            target[normalized] = entry.option;
        }
    }

    private string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }
}