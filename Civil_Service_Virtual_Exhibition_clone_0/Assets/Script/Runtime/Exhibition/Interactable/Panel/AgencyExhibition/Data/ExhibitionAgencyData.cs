using System;
using UnityEngine;

[Serializable]
public class ExhibitionAgencyData
{
    public string AgencyRuntimeId;
    public string Title;

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