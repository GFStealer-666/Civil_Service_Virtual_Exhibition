using System;
using UnityEngine;

[Serializable]
public class ExhibitionOfficeData
{
    public string Id;
    public string Title;
    public Sprite BackgroundSprite;
    public Sprite LogoSprite;

    [TextArea]
    public string Description;
}