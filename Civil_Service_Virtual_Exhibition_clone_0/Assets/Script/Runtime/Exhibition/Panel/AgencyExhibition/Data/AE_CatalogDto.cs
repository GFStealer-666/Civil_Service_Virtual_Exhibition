using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GovernmentCatalogResponseDto
{
    public bool success;
    public List<GovernmentMinistryDto> data = new();
}

[Serializable]
public class GovernmentMinistryDto
{
    public string ministry;
    public string ministryEn;
    public string ministryLogo;

    public List<GovernmentAgencyDto> submissions = new();

    [NonSerialized] public string runtimeId;
}

[Serializable]
public class GovernmentAgencyDto
{
    public string id;
    public int year;
    public string organizationId;
    public string organizationName;
    public string organizationNameEn;
    public string contactInfo;
    public string websiteUrl;
    public List<string> prVideoUrls = new();
    public List<string> contactEmails = new();
    public List<string> contactOthers = new();
    public string officeAddress;
    public string submittedAt;
    public string logoUrl;
    public string coverUrl;
    public List<string> prImageUrls = new();
    public List<GovernmentProjectDto> projects = new();

    [NonSerialized] public string runtimeId;
    [NonSerialized] public string parentMinistryId;
}

[Serializable]
public class GovernmentProjectDto
{
    public string id;
    public int orderIndex;
    public string name;
    public string nameEn;
    public string description;
    public string descriptionEn;
    public string videoUrl;
    public List<string> imageUrls = new();

    [NonSerialized] public string runtimeId;
    [NonSerialized] public string parentAgencyId;
}

[Serializable]
public class ExhibitionAgencyData
{
    public string AgencyRuntimeId;
    public string Title;
    public string TitleTh;
    public string TitleEn;

    public string BackgroundUrl;
    public string LogoUrl;

    public Sprite FallbackBackgroundSprite;
    public Sprite FallbackLogoSprite;
}

[Serializable]
public class ExhibitionProjectData
{
    public string ProjectRuntimeId;
    public string AgencyRuntimeId;
    public string Title;

    public string BackgroundUrl;

    public Sprite FallbackBackgroundSprite;
}