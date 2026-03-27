using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class PSC_Repository : MonoBehaviour
{
    public static PSC_Repository Instance { get; private set; }

    [Header("Load")]
    [SerializeField] private bool preloadOnStart = true;
    [SerializeField] private bool dontDestroyOnLoad = true;

    [Header("Resolver")]
    [SerializeField] private PSC_FilterResolver filterResolver;

    public bool IsLoading { get; private set; }
    public bool HasData => RawData != null;
    public string LastError { get; private set; }
    public PSC_ServiceApiResponseDto RawData { get; private set; }

    public event Action OnDataLoaded;
    public event Action<string> OnDataLoadFailed;

    private readonly Dictionary<string, PSC_ServiceMinistryDto> _ministriesById = new();
    private readonly Dictionary<string, List<PSC_ServiceOrganizationDto>> _organizationsByMinistryId = new();
    private readonly Dictionary<string, PSC_ServiceOrganizationDto> _organizationsById = new();
    private readonly Dictionary<string, List<PSC_ServiceItemDto>> _servicesByOrganizationId = new();
    private readonly Dictionary<string, PSC_ServiceItemDto> _servicesById = new();

    private Coroutine _loadRoutine;

    private ApiService Api => ApiService.Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        if (filterResolver == null)
            filterResolver = FindObjectOfType<PSC_FilterResolver>();
    }

    private void Start()
    {
        if (preloadOnStart)
            Initialize();
    }

    public static PSC_Repository EnsureExists()
    {
        if (Instance != null)
            return Instance;

        GameObject go = new GameObject(nameof(PSC_Repository));
        return go.AddComponent<PSC_Repository>();
    }

    public void Initialize(bool forceRefresh = false, string accessToken = null)
    {
        if (IsLoading)
            return;

        if (!forceRefresh && HasData)
        {
            OnDataLoaded?.Invoke();
            return;
        }

        if (_loadRoutine != null)
            StopCoroutine(_loadRoutine);

        _loadRoutine = StartCoroutine(DownloadRoutine(accessToken));
    }

    public void Refresh(string accessToken = null)
    {
        Initialize(true, accessToken);
    }

    public void ClearData()
    {
        RawData = null;
        LastError = null;

        _ministriesById.Clear();
        _organizationsByMinistryId.Clear();
        _organizationsById.Clear();
        _servicesByOrganizationId.Clear();
        _servicesById.Clear();
    }

    public List<PSC_ServiceMinistryDto> GetMinistries()
    {
        if (RawData == null || RawData.data == null)
            return new List<PSC_ServiceMinistryDto>();

        return new List<PSC_ServiceMinistryDto>(RawData.data);
    }

    public PSC_ServiceMinistryDto GetMinistryById(string ministryId)
    {
        if (string.IsNullOrWhiteSpace(ministryId))
            return null;

        return _ministriesById.TryGetValue(ministryId, out PSC_ServiceMinistryDto ministry)
            ? ministry
            : null;
    }

    public List<PSC_ServiceOrganizationDto> GetOrganizationsByMinistry(string ministryId)
    {
        if (string.IsNullOrWhiteSpace(ministryId))
            return new List<PSC_ServiceOrganizationDto>();

        return _organizationsByMinistryId.TryGetValue(ministryId, out List<PSC_ServiceOrganizationDto> organizations)
            ? organizations
            : new List<PSC_ServiceOrganizationDto>();
    }

    public PSC_ServiceOrganizationDto GetOrganizationById(string organizationId)
    {
        if (string.IsNullOrWhiteSpace(organizationId))
            return null;

        return _organizationsById.TryGetValue(organizationId, out PSC_ServiceOrganizationDto organization)
            ? organization
            : null;
    }

    public List<PSC_ServiceItemDto> GetServicesByOrganization(string organizationId)
    {
        if (string.IsNullOrWhiteSpace(organizationId))
            return new List<PSC_ServiceItemDto>();

        return _servicesByOrganizationId.TryGetValue(organizationId, out List<PSC_ServiceItemDto> services)
            ? services
            : new List<PSC_ServiceItemDto>();
    }

    public PSC_ServiceItemDto GetServiceById(string serviceId)
    {
        if (string.IsNullOrWhiteSpace(serviceId))
            return null;

        return _servicesById.TryGetValue(serviceId, out PSC_ServiceItemDto service)
            ? service
            : null;
    }

    private IEnumerator DownloadRoutine(string accessToken)
    {
        IsLoading = true;
        LastError = null;

        if (Api == null)
        {
            Fail("ApiService.Instance is null.");
            yield break;
        }

        string url = !string.IsNullOrWhiteSpace(Api.PublicServiceCenterUrl)
            ? Api.PublicServiceCenterUrl
            : Api.PublicServiceUrl;

        if (string.IsNullOrWhiteSpace(url))
        {
            Fail("PublicServiceCenterUrl is empty.");
            yield break;
        }

        Debug.Log($"[PSC_Repository] Downloading from: {url}");

        using UnityWebRequest request = Api.Get(url, accessToken);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Fail($"Download failed: {request.error}");
            yield break;
        }

        PSC_ServiceApiResponseDto dto;

        try
        {
            dto = JsonUtility.FromJson<PSC_ServiceApiResponseDto>(request.downloadHandler.text);
        }
        catch (Exception ex)
        {
            Fail($"Parse failed: {ex.Message}");
            yield break;
        }

        if (dto == null)
        {
            Fail("Parsed dto is null.");
            yield break;
        }

        if (!dto.success)
        {
            Fail("Public Service API returned success = false.");
            yield break;
        }

        SetData(dto);

        IsLoading = false;
        _loadRoutine = null;
        LastError = null;

        Debug.Log("[PSC_Repository] Downloaded success");
        OnDataLoaded?.Invoke();
    }

    private void Fail(string error)
    {
        IsLoading = false;
        _loadRoutine = null;
        LastError = error;

        Debug.LogError($"[PSC_Repository] {error}");
        OnDataLoadFailed?.Invoke(error);
    }

    private void SetData(PSC_ServiceApiResponseDto dto)
    {
        if (dto == null)
            return;

        Normalize(dto);
        BuildIndexes(dto);
        RawData = dto;

        LogMinistryKeys();
    }

    private void Normalize(PSC_ServiceApiResponseDto dto)
    {
        if (dto.data == null)
            dto.data = Array.Empty<PSC_ServiceMinistryDto>();

        for (int i = 0; i < dto.data.Length; i++)
        {
            PSC_ServiceMinistryDto ministry = dto.data[i];
            if (ministry == null)
                continue;

            if (ministry.organizations == null)
                ministry.organizations = Array.Empty<PSC_ServiceOrganizationDto>();

            ministry.runtimeOrganizationCount = 0;
            ministry.runtimeServiceCount = 0;
            ministry.runtimeFilterKey = !string.IsNullOrWhiteSpace(ministry.ministryTypeEn)
                ? ministry.ministryTypeEn
                : ministry.ministryType;

            if (filterResolver != null &&
                filterResolver.TryResolve(ministry.runtimeFilterKey, out PSC_MinistryCategory option))
            {
                ministry.runtimeFilter = option;
            }
            else
            {
                ministry.runtimeFilter = PSC_MinistryCategory.All;
            }

            StringBuilder searchBuilder = new StringBuilder();
            AppendSearch(searchBuilder, ministry.ministry);
            AppendSearch(searchBuilder, ministry.ministryEn);
            AppendSearch(searchBuilder, ministry.ministryType);
            AppendSearch(searchBuilder, ministry.ministryTypeEn);

            for (int j = 0; j < ministry.organizations.Length; j++)
            {
                PSC_ServiceOrganizationDto organization = ministry.organizations[j];
                if (organization == null)
                    continue;

                ministry.runtimeOrganizationCount++;

                if (organization.services == null)
                    organization.services = Array.Empty<PSC_ServiceItemDto>();

                organization.runtimeServiceCount = 0;

                AppendSearch(searchBuilder, organization.name);
                AppendSearch(searchBuilder, organization.nameEn);

                for (int k = 0; k < organization.services.Length; k++)
                {
                    PSC_ServiceItemDto service = organization.services[k];
                    if (service == null)
                        continue;

                    organization.runtimeServiceCount++;
                    ministry.runtimeServiceCount++;

                    AppendSearch(searchBuilder, service.serviceName);
                    AppendSearch(searchBuilder, service.serviceNameEn);
                    AppendSearch(searchBuilder, service.service);
                    AppendSearch(searchBuilder, service.serviceEn);
                    AppendSearch(searchBuilder, service.channel);
                    AppendSearch(searchBuilder, service.channelEn);
                    AppendSearch(searchBuilder, service.contact);
                    AppendSearch(searchBuilder, service.contactEn);
                    AppendSearch(searchBuilder, service.duration);
                    AppendSearch(searchBuilder, service.durationEn);
                }
            }

            ministry.runtimeSearchBlob = searchBuilder.ToString();
        }
    }

    private void BuildIndexes(PSC_ServiceApiResponseDto dto)
    {
        _ministriesById.Clear();
        _organizationsByMinistryId.Clear();
        _organizationsById.Clear();
        _servicesByOrganizationId.Clear();
        _servicesById.Clear();

        for (int i = 0; i < dto.data.Length; i++)
        {
            PSC_ServiceMinistryDto ministry = dto.data[i];
            if (ministry == null)
                continue;

            ministry.runtimeId = BuildMinistryId(ministry, i);

            if (!_ministriesById.ContainsKey(ministry.runtimeId))
                _ministriesById.Add(ministry.runtimeId, ministry);

            if (!_organizationsByMinistryId.ContainsKey(ministry.runtimeId))
                _organizationsByMinistryId.Add(ministry.runtimeId, new List<PSC_ServiceOrganizationDto>());

            for (int j = 0; j < ministry.organizations.Length; j++)
            {
                PSC_ServiceOrganizationDto organization = ministry.organizations[j];
                if (organization == null)
                    continue;

                organization.runtimeId = BuildOrganizationId(organization, ministry.runtimeId, j);
                organization.parentMinistryId = ministry.runtimeId;

                _organizationsByMinistryId[ministry.runtimeId].Add(organization);

                if (!_organizationsById.ContainsKey(organization.runtimeId))
                    _organizationsById.Add(organization.runtimeId, organization);

                if (!_servicesByOrganizationId.ContainsKey(organization.runtimeId))
                    _servicesByOrganizationId.Add(organization.runtimeId, new List<PSC_ServiceItemDto>());

                for (int k = 0; k < organization.services.Length; k++)
                {
                    PSC_ServiceItemDto service = organization.services[k];
                    if (service == null)
                        continue;

                    service.runtimeId = BuildServiceId(service, organization.runtimeId, k);
                    service.parentOrganizationId = organization.runtimeId;

                    _servicesByOrganizationId[organization.runtimeId].Add(service);

                    if (!_servicesById.ContainsKey(service.runtimeId))
                        _servicesById.Add(service.runtimeId, service);
                }
            }
        }
    }

    private void LogMinistryKeys()
    {
        if (RawData == null || RawData.data == null || RawData.data.Length == 0)
        {
            Debug.Log("[PSC_Repository] No ministry data to log.");
            return;
        }

        Debug.Log("[PSC_Repository] ===== Ministry Keys =====");

        for (int i = 0; i < RawData.data.Length; i++)
        {
            PSC_ServiceMinistryDto ministry = RawData.data[i];
            if (ministry == null)
                continue;

            int organizationCount = 0;

            if (!string.IsNullOrWhiteSpace(ministry.runtimeId) &&
                _organizationsByMinistryId.TryGetValue(ministry.runtimeId, out List<PSC_ServiceOrganizationDto> organizations) &&
                organizations != null)
            {
                organizationCount = organizations.Count;
            }

            Debug.Log(
                $"[PSC_Repository] Ministry Index={i} | " +
                $"runtimeId='{ministry.runtimeId}' | " +
                $"filter='{ministry.runtimeFilter}' | " +
                $"organizations={organizationCount} | " +
                $"services={ministry.runtimeServiceCount}"
            );
        }

        Debug.Log("[PSC_Repository] ======================");
    }

    private string BuildMinistryId(PSC_ServiceMinistryDto ministry, int index)
    {
        string source = FirstNotEmpty(
            ministry.ministryEn,
            ministry.ministry
        );

        string slug = ToSlug(source);

        if (string.IsNullOrWhiteSpace(slug))
            slug = $"ministry-{index}";

        return slug;
    }

    private string BuildOrganizationId(PSC_ServiceOrganizationDto organization, string ministryId, int index)
    {
        string source = FirstNotEmpty(
            organization.nameEn,
            organization.name
        );

        string slug = ToSlug(source);

        if (string.IsNullOrWhiteSpace(slug))
            slug = $"{ministryId}-org-{index}";

        return $"{ministryId}-{slug}";
    }

    private string BuildServiceId(PSC_ServiceItemDto service, string organizationId, int index)
    {
        string source = FirstNotEmpty(
            service.serviceNameEn,
            service.serviceName,
            service.serviceEn,
            service.service
        );

        string slug = ToSlug(source);

        if (string.IsNullOrWhiteSpace(slug))
            slug = $"{organizationId}-service-{index}";

        return $"{organizationId}-{slug}-{index}";
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

    private void AppendSearch(StringBuilder builder, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        builder.Append(value.Trim());
        builder.Append(' ');
    }
}