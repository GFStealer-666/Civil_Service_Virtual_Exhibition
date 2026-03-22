using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class GovernmentCatalogStore : MonoBehaviour
{
    public static GovernmentCatalogStore Instance { get; private set; }

    public bool HasData => RawData != null;
    public GovernmentCatalogResponseDto RawData { get; private set; }

    private readonly Dictionary<string, GovernmentMinistryDto> _ministriesById = new();
    private readonly Dictionary<string, List<GovernmentAgencyDto>> _agenciesByMinistryId = new();
    private readonly Dictionary<string, List<GovernmentProjectDto>> _projectsByAgencyId = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static GovernmentCatalogStore EnsureExists()
    {
        if (Instance != null)
            return Instance;

        GameObject go = new GameObject(nameof(GovernmentCatalogStore));
        return go.AddComponent<GovernmentCatalogStore>();
    }

    public void SetData(GovernmentCatalogResponseDto dto)
    {
        if (dto == null)
            return;

        Normalize(dto);
        BuildIndexes(dto);
        RawData = dto;

        LogMinistryKeys();
    }

    public void ClearData()
    {
        RawData = null;
        _ministriesById.Clear();
        _agenciesByMinistryId.Clear();
        _projectsByAgencyId.Clear();
    }

    public List<GovernmentMinistryDto> GetMinistries()
    {
        if (RawData == null || RawData.data == null)
            return new List<GovernmentMinistryDto>();

        return RawData.data;
    }

    public GovernmentMinistryDto GetMinistryById(string ministryId)
    {
        if (string.IsNullOrWhiteSpace(ministryId))
            return null;

        if (_ministriesById.TryGetValue(ministryId, out GovernmentMinistryDto ministry))
            return ministry;

        return null;
    }
    public GovernmentMinistryDto FindMinistry(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        List<GovernmentMinistryDto> ministries = GetMinistries();

        for (int i = 0; i < ministries.Count; i++)
        {
            GovernmentMinistryDto item = ministries[i];
            if (item == null)
                continue;

            if (string.Equals(item.runtimeId, key, StringComparison.OrdinalIgnoreCase))
                return item;

            if (string.Equals(item.ministry, key, StringComparison.OrdinalIgnoreCase))
                return item;

            if (string.Equals(item.ministryEn, key, StringComparison.OrdinalIgnoreCase))
                return item;
        }

        return null;
    }
    public List<GovernmentAgencyDto> GetAgenciesByMinistry(string ministryId)
    {
        if (string.IsNullOrWhiteSpace(ministryId))
            return new List<GovernmentAgencyDto>();

        if (_agenciesByMinistryId.TryGetValue(ministryId, out List<GovernmentAgencyDto> agencies))
            return agencies;

        return new List<GovernmentAgencyDto>();
    }

    public List<GovernmentProjectDto> GetProjectsByAgency(string agencyId)
    {
        if (string.IsNullOrWhiteSpace(agencyId))
            return new List<GovernmentProjectDto>();

        if (_projectsByAgencyId.TryGetValue(agencyId, out List<GovernmentProjectDto> projects))
            return projects;

        return new List<GovernmentProjectDto>();
    }

    private void Normalize(GovernmentCatalogResponseDto dto)
    {
        if (dto.data == null)
            dto.data = new List<GovernmentMinistryDto>();

        for (int i = 0; i < dto.data.Count; i++)
        {
            GovernmentMinistryDto ministry = dto.data[i];
            if (ministry == null)
                continue;

            if (ministry.submissions == null)
                ministry.submissions = new List<GovernmentAgencyDto>();

            for (int j = 0; j < ministry.submissions.Count; j++)
            {
                GovernmentAgencyDto agency = ministry.submissions[j];
                if (agency == null)
                    continue;

                if (agency.prVideoUrls == null)
                    agency.prVideoUrls = new List<string>();

                if (agency.contactEmails == null)
                    agency.contactEmails = new List<string>();

                if (agency.contactOthers == null)
                    agency.contactOthers = new List<string>();

                if (agency.prImageUrls == null)
                    agency.prImageUrls = new List<string>();

                if (agency.projects == null)
                    agency.projects = new List<GovernmentProjectDto>();

                for (int k = 0; k < agency.projects.Count; k++)
                {
                    GovernmentProjectDto project = agency.projects[k];
                    if (project == null)
                        continue;

                    if (project.imageUrls == null)
                        project.imageUrls = new List<string>();
                }
            }
        }
    }
    private void LogMinistryKeys()
    {
        if (RawData == null || RawData.data == null || RawData.data.Count == 0)
        {
            Debug.Log("[GovernmentCatalogStore] No ministry data to log.");
            return;
        }

        Debug.Log("[GovernmentCatalogStore] ===== Ministry Keys =====");

        for (int i = 0; i < RawData.data.Count; i++)
        {
            GovernmentMinistryDto ministry = RawData.data[i];
            if (ministry == null)
                continue;

            int agencyCount = 0;

            if (!string.IsNullOrWhiteSpace(ministry.runtimeId) &&
                _agenciesByMinistryId.TryGetValue(ministry.runtimeId, out List<GovernmentAgencyDto> agencies) &&
                agencies != null)
            {
                agencyCount = agencies.Count;
            }

            Debug.Log(
                $"[GovernmentCatalogStore] Ministry Index={i} | " +
                $"runtimeId='{ministry.runtimeId}' | " +
                $"ministry='{ministry.ministry}' | " +
                $"ministryEn='{ministry.ministryEn}' | " +
                $"agencies={agencyCount}"
            );
        }

        Debug.Log("[GovernmentCatalogStore] =========================");
    }
    private void BuildIndexes(GovernmentCatalogResponseDto dto)
    {
        _ministriesById.Clear();
        _agenciesByMinistryId.Clear();
        _projectsByAgencyId.Clear();

        for (int i = 0; i < dto.data.Count; i++)
        {
            GovernmentMinistryDto ministry = dto.data[i];
            if (ministry == null)
                continue;

            ministry.runtimeId = BuildMinistryId(ministry, i);

            if (!_ministriesById.ContainsKey(ministry.runtimeId))
                _ministriesById.Add(ministry.runtimeId, ministry);

            if (!_agenciesByMinistryId.ContainsKey(ministry.runtimeId))
                _agenciesByMinistryId.Add(ministry.runtimeId, new List<GovernmentAgencyDto>());

            for (int j = 0; j < ministry.submissions.Count; j++)
            {
                GovernmentAgencyDto agency = ministry.submissions[j];
                if (agency == null)
                    continue;

                agency.runtimeId = BuildAgencyId(agency, ministry.runtimeId, j);
                agency.parentMinistryId = ministry.runtimeId;

                _agenciesByMinistryId[ministry.runtimeId].Add(agency);

                if (!_projectsByAgencyId.ContainsKey(agency.runtimeId))
                    _projectsByAgencyId.Add(agency.runtimeId, new List<GovernmentProjectDto>());

                for (int k = 0; k < agency.projects.Count; k++)
                {
                    GovernmentProjectDto project = agency.projects[k];
                    if (project == null)
                        continue;

                    project.runtimeId = BuildProjectId(project, agency.runtimeId, k);
                    project.parentAgencyId = agency.runtimeId;

                    _projectsByAgencyId[agency.runtimeId].Add(project);
                }
            }
        }
    }

    private string BuildMinistryId(GovernmentMinistryDto ministry, int index)
    {
        string source = FirstNotEmpty(ministry.ministryEn, ministry.ministry);
        string slug = ToSlug(source);

        if (string.IsNullOrWhiteSpace(slug))
            slug = $"ministry-{index}";

        return slug;
    }

    private string BuildAgencyId(GovernmentAgencyDto agency, string ministryId, int index)
    {
        string source = FirstNotEmpty(
            agency.organizationId,
            agency.id,
            agency.organizationNameEn,
            agency.organizationName);

        string slug = ToSlug(source);

        if (string.IsNullOrWhiteSpace(slug))
            slug = $"{ministryId}-agency-{index}";

        return slug;
    }

    private string BuildProjectId(GovernmentProjectDto project, string agencyId, int index)
    {
        string source = FirstNotEmpty(
            project.id,
            project.nameEn,
            project.name);

        string slug = ToSlug(source);

        if (string.IsNullOrWhiteSpace(slug))
            slug = $"{agencyId}-project-{index}";

        return slug;
    }

    private string FirstNotEmpty(params string[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(values[i]))
                return values[i];
        }

        return string.Empty;
    }

    private string ToSlug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < value.Length; i++)
        {
            char c = char.ToLowerInvariant(value[i]);

            if (char.IsLetterOrDigit(c))
            {
                builder.Append(c);
            }
            else if (builder.Length > 0 && builder[builder.Length - 1] != '-')
            {
                builder.Append('-');
            }
        }

        return builder.ToString().Trim('-');
    }
}