using System;

public enum PSC_MinistryCategory
{
    All = 0,
    Economy = 1,
    Society = 2,
    Security = 3,
    Infrastructure = 4,
    Service = 5
}

[Serializable]
public class PSC_ServiceApiResponseDto
{
    public bool success;
    public PSC_ServiceMinistryDto[] data;
    public int total;
}

[Serializable]
public class PSC_ServiceMinistryDto
{
    public string ministry;
    public string ministryEn;
    public string ministryType;
    public string ministryTypeEn;
    public string ministryImage;
    public PSC_ServiceOrganizationDto[] organizations;

    [NonSerialized] public string runtimeId;
    [NonSerialized] public string runtimeFilterKey;
    [NonSerialized] public PSC_MinistryCategory runtimeFilter = PSC_MinistryCategory.All;
    [NonSerialized] public string runtimeSearchBlob;
    [NonSerialized] public int runtimeOrganizationCount;
    [NonSerialized] public int runtimeServiceCount;
}

[Serializable]
public class PSC_ServiceOrganizationDto
{
    public string name;
    public string nameEn;
    public string organizationImage;
    public PSC_ServiceItemDto[] services;

    [NonSerialized] public string runtimeId;
    [NonSerialized] public string parentMinistryId;
    [NonSerialized] public int runtimeServiceCount;
}

[Serializable]
public class PSC_ServiceItemDto
{
    public string serviceName;
    public string serviceNameEn;
    public string service;
    public string serviceEn;
    public string channel;
    public string channelEn;
    public string contact;
    public string contactEn;
    public string duration;
    public string durationEn;

    [NonSerialized] public string runtimeId;
    [NonSerialized] public string parentOrganizationId;
}