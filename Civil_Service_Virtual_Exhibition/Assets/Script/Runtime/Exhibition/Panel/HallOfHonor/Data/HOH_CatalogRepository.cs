using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class HOH_CatalogRepository : MonoBehaviour
{
    public static HOH_CatalogRepository Instance { get; private set; }

    [Header("Load")]
    [SerializeField] private bool preloadOnStart = true;
    [SerializeField] private bool dontDestroyOnLoad = true;

    [Header("Resolver")]
    [SerializeField] private HOH_FilterResolver filterResolver;

    public bool IsLoading { get; private set; }
    public bool HasData => RawData != null;
    public string LastError { get; private set; }
    public HOH_ResponseDto RawData { get; private set; }

    public event Action OnDataLoaded;
    public event Action<string> OnDataLoadFailed;

    private readonly Dictionary<string, HOH_CategoryDto> _categoriesById = new();
    private readonly Dictionary<HOH_CategoryKind, HOH_CategoryDto> _categoriesByKind = new();
    private readonly Dictionary<string, List<HOH_UnitDto>> _unitsByCategoryId = new();
    private readonly Dictionary<string, HOH_UnitDto> _unitsById = new();
    private readonly Dictionary<string, List<HOH_PersonDto>> _personsByUnitId = new();
    private readonly Dictionary<string, HOH_PersonDto> _personsById = new();

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
            filterResolver = FindObjectOfType<HOH_FilterResolver>();
    }

    private void Start()
    {
        if (preloadOnStart)
            Initialize();
    }

    public static HOH_CatalogRepository EnsureExists()
    {
        if (Instance != null)
            return Instance;

        GameObject go = new GameObject(nameof(HOH_CatalogRepository));
        return go.AddComponent<HOH_CatalogRepository>();
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

        _categoriesById.Clear();
        _categoriesByKind.Clear();
        _unitsByCategoryId.Clear();
        _unitsById.Clear();
        _personsByUnitId.Clear();
        _personsById.Clear();
    }

    public List<HOH_CategoryDto> GetCategories()
    {
        if (RawData == null || RawData.data == null)
            return new List<HOH_CategoryDto>();

        return RawData.data;
    }

    public HOH_CategoryDto GetCategoryById(string categoryId)
    {
        if (string.IsNullOrWhiteSpace(categoryId))
            return null;

        return _categoriesById.TryGetValue(categoryId, out HOH_CategoryDto category)
            ? category
            : null;
    }

    public HOH_CategoryDto GetCategoryByKind(HOH_CategoryKind kind)
    {
        return _categoriesByKind.TryGetValue(kind, out HOH_CategoryDto category)
            ? category
            : null;
    }

    public List<HOH_UnitDto> GetUnitsByCategory(string categoryId)
    {
        if (string.IsNullOrWhiteSpace(categoryId))
            return new List<HOH_UnitDto>();

        return _unitsByCategoryId.TryGetValue(categoryId, out List<HOH_UnitDto> units)
            ? units
            : new List<HOH_UnitDto>();
    }

    public List<HOH_UnitDto> GetUnitsByKind(HOH_CategoryKind kind)
    {
        HOH_CategoryDto category = GetCategoryByKind(kind);

        if (category == null)
            return new List<HOH_UnitDto>();

        return GetUnitsByCategory(category.runtimeId);
    }

    public HOH_UnitDto GetUnitById(string unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId))
            return null;

        return _unitsById.TryGetValue(unitId, out HOH_UnitDto unit)
            ? unit
            : null;
    }

    public List<HOH_PersonDto> GetPersonsByUnit(string unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId))
            return new List<HOH_PersonDto>();

        return _personsByUnitId.TryGetValue(unitId, out List<HOH_PersonDto> persons)
            ? persons
            : new List<HOH_PersonDto>();
    }

    public HOH_PersonDto GetPersonById(string personId)
    {
        if (string.IsNullOrWhiteSpace(personId))
            return null;

        return _personsById.TryGetValue(personId, out HOH_PersonDto person)
            ? person
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

        if (string.IsNullOrWhiteSpace(Api.HallOfHonorUrl))
        {
            Fail("HallOfHonorUrl is empty.");
            yield break;
        }

        Debug.Log($"[HOH_CatalogRepository] Downloading from: {Api.HallOfHonorUrl}");

        using UnityWebRequest request = Api.Get(Api.HallOfHonorUrl, accessToken);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Fail($"Download failed: {request.error}");
            yield break;
        }

        HOH_ResponseDto dto;

        try
        {
            dto = JsonUtility.FromJson<HOH_ResponseDto>(request.downloadHandler.text);
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
            Fail("Hall of Honor API returned success = false.");
            yield break;
        }

        SetData(dto);

        IsLoading = false;
        _loadRoutine = null;
        LastError = null;

        Debug.Log("[HOH_CatalogRepository] Downloaded success");
        OnDataLoaded?.Invoke();
    }

    private void Fail(string error)
    {
        IsLoading = false;
        _loadRoutine = null;
        LastError = error;

        Debug.LogError($"[HOH_CatalogRepository] {error}");
        OnDataLoadFailed?.Invoke(error);
    }

    private void SetData(HOH_ResponseDto dto)
    {
        if (dto == null)
            return;

        Normalize(dto);
        BuildIndexes(dto);
        RawData = dto;

        LogCategoryKeys();
    }

    private void Normalize(HOH_ResponseDto dto)
    {
        if (dto.data == null)
            dto.data = new List<HOH_CategoryDto>();

        for (int i = 0; i < dto.data.Count; i++)
        {
            HOH_CategoryDto category = dto.data[i];
            if (category == null)
                continue;

            category.runtimeKind = category.GetCategoryKind();

            if (category.ministries == null)
                category.ministries = new List<HOH_MinistryGroupDto>();

            category.units = new List<HOH_UnitDto>();

            for (int j = 0; j < category.ministries.Count; j++)
            {
                HOH_MinistryGroupDto group = category.ministries[j];
                if (group == null)
                    continue;

                if (group.units == null)
                    group.units = new List<HOH_UnitDto>();

                for (int k = 0; k < group.units.Count; k++)
                {
                    HOH_UnitDto unit = group.units[k];
                    if (unit == null)
                        continue;

                    if (unit.persons == null)
                        unit.persons = new List<HOH_PersonDto>();

                    if (string.IsNullOrWhiteSpace(unit.ministry))
                        unit.ministry = group.ministry;

                    if (string.IsNullOrWhiteSpace(unit.ministryEn))
                        unit.ministryEn = group.ministryEn;

                    unit.runtimeFilterKey = !string.IsNullOrWhiteSpace(group.ministryTypeEn)
                        ? group.ministryTypeEn
                        : group.ministryType;

                    if (filterResolver != null &&
                        filterResolver.TryResolve(unit.runtimeFilterKey, out HOH_FilterOption option))
                    {
                        unit.runtimeFilter = option;
                    }
                    else
                    {
                        unit.runtimeFilter = HOH_FilterOption.All;
                    }

                    category.units.Add(unit);
                }
            }
        }
    }

    private void BuildIndexes(HOH_ResponseDto dto)
    {
        _categoriesById.Clear();
        _categoriesByKind.Clear();
        _unitsByCategoryId.Clear();
        _unitsById.Clear();
        _personsByUnitId.Clear();
        _personsById.Clear();

        for (int i = 0; i < dto.data.Count; i++)
        {
            HOH_CategoryDto category = dto.data[i];
            if (category == null)
                continue;

            category.runtimeId = BuildCategoryId(category, i);
            HOH_CategoryKind kind = category.GetCategoryKind();
            category.runtimeKind = kind;

            if (!_categoriesById.ContainsKey(category.runtimeId))
                _categoriesById.Add(category.runtimeId, category);

            if (!_categoriesByKind.ContainsKey(kind))
                _categoriesByKind.Add(kind, category);

            if (!_unitsByCategoryId.ContainsKey(category.runtimeId))
                _unitsByCategoryId.Add(category.runtimeId, new List<HOH_UnitDto>());

            List<HOH_UnitDto> sourceUnits = category.units ?? new List<HOH_UnitDto>();

            for (int j = 0; j < sourceUnits.Count; j++)
            {
                HOH_UnitDto unit = sourceUnits[j];
                if (unit == null)
                    continue;

                unit.runtimeId = BuildUnitId(unit, category.runtimeId, j);
                unit.parentCategoryId = category.runtimeId;

                _unitsByCategoryId[category.runtimeId].Add(unit);

                if (!_unitsById.ContainsKey(unit.runtimeId))
                    _unitsById.Add(unit.runtimeId, unit);

                if (!_personsByUnitId.ContainsKey(unit.runtimeId))
                    _personsByUnitId.Add(unit.runtimeId, new List<HOH_PersonDto>());

                for (int k = 0; k < unit.persons.Count; k++)
                {
                    HOH_PersonDto person = unit.persons[k];
                    if (person == null)
                        continue;

                    person.runtimeId = BuildPersonId(person, unit.runtimeId, k);
                    person.parentUnitId = unit.runtimeId;

                    _personsByUnitId[unit.runtimeId].Add(person);

                    if (!_personsById.ContainsKey(person.runtimeId))
                        _personsById.Add(person.runtimeId, person);
                }
            }
        }
    }

    private void LogCategoryKeys()
    {
        if (RawData == null || RawData.data == null || RawData.data.Count == 0)
        {
            Debug.Log("[HOH_CatalogRepository] No category data to log.");
            return;
        }

        Debug.Log("[HOH_CatalogRepository] ===== Category Keys =====");

        for (int i = 0; i < RawData.data.Count; i++)
        {
            HOH_CategoryDto category = RawData.data[i];
            if (category == null)
                continue;

            int unitCount = 0;

            if (!string.IsNullOrWhiteSpace(category.runtimeId) &&
                _unitsByCategoryId.TryGetValue(category.runtimeId, out List<HOH_UnitDto> units) &&
                units != null)
            {
                unitCount = units.Count;
            }

            Debug.Log(
                $"[HOH_CatalogRepository] Category Index={i} | " +
                $"runtimeId='{category.runtimeId}' | " +
                $"type='{category.type}' | " +
                $"units={unitCount}"
            );
        }

        Debug.Log("[HOH_CatalogRepository] =========================");
    }

    private string BuildCategoryId(HOH_CategoryDto category, int index)
    {
        HOH_CategoryKind kind = category.GetCategoryKind();

        switch (kind)
        {
            case HOH_CategoryKind.Ministry:
                return "ministry";

            case HOH_CategoryKind.Province:
                return "province";

            case HOH_CategoryKind.University:
                return "university";
        }

        string slug = ToSlug(category.type);

        if (string.IsNullOrWhiteSpace(slug))
            slug = $"category-{index}";

        return slug;
    }

    private string BuildUnitId(HOH_UnitDto unit, string categoryId, int index)
    {
        string source = FirstNotEmpty(
            unit.unitEn,
            unit.unit,
            unit.ministryEn,
            unit.ministry
        );

        string slug = ToSlug(source);

        if (string.IsNullOrWhiteSpace(slug))
            slug = $"{categoryId}-unit-{index}";

        return $"{categoryId}-{slug}";
    }

    private string BuildPersonId(HOH_PersonDto person, string unitId, int index)
    {
        string fullNameEn = $"{person.firstNameEn} {person.lastNameEn}".Trim();
        string fullNameTh = $"{person.firstName} {person.lastName}".Trim();

        string source = FirstNotEmpty(
            person.id,
            fullNameEn,
            fullNameTh
        );

        string slug = ToSlug(source);

        if (string.IsNullOrWhiteSpace(slug))
            slug = $"{unitId}-person-{index}";

        return $"{unitId}-{slug}";
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