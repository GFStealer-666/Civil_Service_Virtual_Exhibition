using UnityEngine;
using System.Collections.Generic;

public static class FemaleOutfitPalette
{
    public class Outfit
    {
        public string Name;
        public Color Hair;
        public Color Eyebrow;
        public Color Shirt;
        public Color ShirtInner;
        public Color Skirt;
        public Color Earring;
    }

    private static readonly Outfit[] Outfits =
    {
        new Outfit{ Name="Elegant White", Hair=Hex("1A1A1A"), Eyebrow=Hex("111111"), Shirt=Hex("FFFFFF"), ShirtInner=Hex("E8E8E8"), Skirt=Hex("2F2F2F"), Earring=Hex("FFD700")},
        new Outfit{ Name="Pastel Pink", Hair=Hex("C8A951"), Eyebrow=Hex("8B6914"), Shirt=Hex("FFD6E7"), ShirtInner=Hex("FFFFFF"), Skirt=Hex("E8D8F0"), Earring=Hex("FFD700")},
        new Outfit{ Name="Sky Blue", Hair=Hex("3B2310"), Eyebrow=Hex("1A0F0A"), Shirt=Hex("87CEEB"), ShirtInner=Hex("F5F5DC"), Skirt=Hex("2C3E50"), Earring=Hex("C0C0C0")},
        new Outfit{ Name="Autumn Brown", Hair=Hex("7B3F00"), Eyebrow=Hex("4A2800"), Shirt=Hex("D8E8F0"), ShirtInner=Hex("F5F5DC"), Skirt=Hex("5C4033"), Earring=Hex("FFD700")},
        new Outfit{ Name="Forest Style", Hair=Hex("2C1810"), Eyebrow=Hex("1A0F0A"), Shirt=Hex("D8F0D8"), ShirtInner=Hex("FFFFFF"), Skirt=Hex("1C3A1C"), Earring=Hex("C0C0C0")},
        new Outfit{ Name="Royal Violet", Hair=Hex("2F2F2F"), Eyebrow=Hex("1A1A1A"), Shirt=Hex("E8E0F0"), ShirtInner=Hex("FFFFFF"), Skirt=Hex("4A2C4A"), Earring=Hex("FFD700")},
        new Outfit{ Name="Summer Light", Hair=Hex("D4A017"), Eyebrow=Hex("8B6914"), Shirt=Hex("87CEFA"), ShirtInner=Hex("FFFFFF"), Skirt=Hex("FFDAB9"), Earring=Hex("FFD700")},
        new Outfit{ Name="Rose Classic", Hair=Hex("5C3317"), Eyebrow=Hex("3B2310"), Shirt=Hex("E8D8D8"), ShirtInner=Hex("FFFFFF"), Skirt=Hex("3D3D3D"), Earring=Hex("C0C0C0")},
        new Outfit{ Name="Office Lady", Hair=Hex("2C2C2C"), Eyebrow=Hex("111111"), Shirt=Hex("F5F5F5"), ShirtInner=Hex("FFFFFF"), Skirt=Hex("2F2F2F"), Earring=Hex("FFD700")},
        new Outfit{ Name="Mint Fresh", Hair=Hex("3A2A1A"), Eyebrow=Hex("2A1A0A"), Shirt=Hex("D8F0D8"), ShirtInner=Hex("FFFFFF"), Skirt=Hex("3A4A3A"), Earring=Hex("C0C0C0")},

        new Outfit{ Name="Soft Lavender", Hair=Hex("2F2F2F"), Eyebrow=Hex("1A1A1A"), Shirt=Hex("E6D6FF"), ShirtInner=Hex("FFFFFF"), Skirt=Hex("4A2C4A"), Earring=Hex("FFD700")},
        new Outfit{ Name="Peach Warm", Hair=Hex("6B4F2A"), Eyebrow=Hex("4A3720"), Shirt=Hex("FFDAB9"), ShirtInner=Hex("FFF5EE"), Skirt=Hex("8B7355"), Earring=Hex("FFD700")},
        new Outfit{ Name="Cool Grey", Hair=Hex("2F2F2F"), Eyebrow=Hex("1A1A1A"), Shirt=Hex("DDDDDD"), ShirtInner=Hex("FFFFFF"), Skirt=Hex("555555"), Earring=Hex("C0C0C0")},
        new Outfit{ Name="Deep Blue", Hair=Hex("1C3A5E"), Eyebrow=Hex("1A1A2E"), Shirt=Hex("D6EAF8"), ShirtInner=Hex("FFFFFF"), Skirt=Hex("1F4E79"), Earring=Hex("FFD700")},
        new Outfit{ Name="Elegant Beige", Hair=Hex("6B4F2A"), Eyebrow=Hex("4A3720"), Shirt=Hex("FFF8E7"), ShirtInner=Hex("FFFFFF"), Skirt=Hex("D2B48C"), Earring=Hex("FFD700")},
        new Outfit{ Name="Classic Black", Hair=Hex("111111"), Eyebrow=Hex("000000"), Shirt=Hex("FFFFFF"), ShirtInner=Hex("CCCCCC"), Skirt=Hex("111111"), Earring=Hex("FFD700")},
        new Outfit{ Name="Soft Coral", Hair=Hex("7B3F00"), Eyebrow=Hex("4A2800"), Shirt=Hex("FFB6B6"), ShirtInner=Hex("FFFFFF"), Skirt=Hex("8B4C39"), Earring=Hex("FFD700")},
        new Outfit{ Name="Spring Green", Hair=Hex("3A2A1A"), Eyebrow=Hex("2A1A0A"), Shirt=Hex("CFFFE5"), ShirtInner=Hex("FFFFFF"), Skirt=Hex("3C6E47"), Earring=Hex("C0C0C0")},
        new Outfit{ Name="Ocean Breeze", Hair=Hex("2C3E50"), Eyebrow=Hex("1B2836"), Shirt=Hex("AED6F1"), ShirtInner=Hex("FFFFFF"), Skirt=Hex("21618C"), Earring=Hex("C0C0C0")},
        new Outfit{ Name="Golden Style", Hair=Hex("D4A017"), Eyebrow=Hex("8B6914"), Shirt=Hex("FFF4CC"), ShirtInner=Hex("FFFFFF"), Skirt=Hex("C9A227"), Earring=Hex("FFD700")}
    };

    public static Dictionary<AppearanceSlot, Color> GetRandomOutfit()
    {
        Outfit o = Outfits[Random.Range(0, Outfits.Length)];

        return new Dictionary<AppearanceSlot, Color>
        {
            { AppearanceSlot.Hair, o.Hair },
            { AppearanceSlot.Eyebrow, o.Eyebrow },
            { AppearanceSlot.Shirt, o.Shirt },
            { AppearanceSlot.ShirtInner, o.ShirtInner },
            { AppearanceSlot.Skirt, o.Skirt },
            { AppearanceSlot.Earring, o.Earring }
        };
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}