using UnityEngine;
using System.Collections.Generic;

public static class MaleOutfitPalette
{
    public class Outfit
    {
        public string Name;
        public Color Hair;
        public Color Eyebrow;
        public Color Shirt;
        public Color Suit;
        public Color Pants;
    }

    private static readonly Outfit[] Outfits =
    {
        new Outfit{ Name="Navy Professional", Hair=Hex("1A1A1A"), Eyebrow=Hex("111111"), Shirt=Hex("FFFFFF"), Suit=Hex("1C2B4A"), Pants=Hex("1C2B4A")},
        new Outfit{ Name="Charcoal Classic", Hair=Hex("2C1810"), Eyebrow=Hex("1A0F0A"), Shirt=Hex("F5F5DC"), Suit=Hex("2F2F2F"), Pants=Hex("3D3D3D")},
        new Outfit{ Name="Brown Formal", Hair=Hex("5C3317"), Eyebrow=Hex("3B2310"), Shirt=Hex("D8E8F0"), Suit=Hex("4A3728"), Pants=Hex("3D2B1F")},
        new Outfit{ Name="Midnight Blue", Hair=Hex("1C3A5E"), Eyebrow=Hex("1A1A2E"), Shirt=Hex("E8E0F0"), Suit=Hex("1A1A2E"), Pants=Hex("2C3E50")},
        new Outfit{ Name="Forest Gentleman", Hair=Hex("2C1810"), Eyebrow=Hex("1A0F0A"), Shirt=Hex("F5F5DC"), Suit=Hex("1C3A1C"), Pants=Hex("2F2F2F")},
        new Outfit{ Name="Blonde Casual", Hair=Hex("C8A951"), Eyebrow=Hex("8B6914"), Shirt=Hex("87CEEB"), Suit=Hex("5C4033"), Pants=Hex("4A4A4A")},
        new Outfit{ Name="Golden Classic", Hair=Hex("D4A017"), Eyebrow=Hex("8B6914"), Shirt=Hex("FFFFFF"), Suit=Hex("2A2A4A"), Pants=Hex("1A1A1A")},
        new Outfit{ Name="Burgundy Scholar", Hair=Hex("1A1A1A"), Eyebrow=Hex("111111"), Shirt=Hex("F5F5DC"), Suit=Hex("4A2C4A"), Pants=Hex("2F2F2F")},
        new Outfit{ Name="Auburn Warm", Hair=Hex("7B3F00"), Eyebrow=Hex("4A2800"), Shirt=Hex("E8D8D8"), Suit=Hex("3D3D3D"), Pants=Hex("5C4A3A")},
        new Outfit{ Name="Slate Modern", Hair=Hex("2F4F4F"), Eyebrow=Hex("1A2E2E"), Shirt=Hex("D8F0D8"), Suit=Hex("2C4A2C"), Pants=Hex("2F2F2F")},

        new Outfit{ Name="Office Grey", Hair=Hex("2F2F2F"), Eyebrow=Hex("1A1A1A"), Shirt=Hex("F5F5F5"), Suit=Hex("3A3A3A"), Pants=Hex("2F2F2F")},
        new Outfit{ Name="Coffee Style", Hair=Hex("3B2310"), Eyebrow=Hex("2A140B"), Shirt=Hex("FFFFFF"), Suit=Hex("4A3728"), Pants=Hex("4A3728")},
        new Outfit{ Name="Royal Indigo", Hair=Hex("1A1A1A"), Eyebrow=Hex("111111"), Shirt=Hex("F0F0FF"), Suit=Hex("2A2A4A"), Pants=Hex("2A2A4A")},
        new Outfit{ Name="Olive Modern", Hair=Hex("3A2F1B"), Eyebrow=Hex("2A1E12"), Shirt=Hex("E8F0D8"), Suit=Hex("3C4A2C"), Pants=Hex("2F2F2F")},
        new Outfit{ Name="Dark Formal", Hair=Hex("111111"), Eyebrow=Hex("000000"), Shirt=Hex("FFFFFF"), Suit=Hex("222222"), Pants=Hex("222222")},
        new Outfit{ Name="Vintage Brown", Hair=Hex("4A2E14"), Eyebrow=Hex("2A1A0A"), Shirt=Hex("EFE6D8"), Suit=Hex("5C4033"), Pants=Hex("3D2B1F")},
        new Outfit{ Name="Cool Blue", Hair=Hex("2C3E50"), Eyebrow=Hex("1B2836"), Shirt=Hex("D6EAF8"), Suit=Hex("1F4E79"), Pants=Hex("2C3E50")},
        new Outfit{ Name="Urban Black", Hair=Hex("1A1A1A"), Eyebrow=Hex("000000"), Shirt=Hex("CCCCCC"), Suit=Hex("1A1A1A"), Pants=Hex("111111")},
        new Outfit{ Name="Beige Smart", Hair=Hex("6B4F2A"), Eyebrow=Hex("4A3720"), Shirt=Hex("FFF8E7"), Suit=Hex("D2B48C"), Pants=Hex("8B7355")},
        new Outfit{ Name="Green Business", Hair=Hex("2A2A2A"), Eyebrow=Hex("111111"), Shirt=Hex("E8FFE8"), Suit=Hex("2E5E2E"), Pants=Hex("2F2F2F")}
    };

    public static Dictionary<AppearanceSlot, Color> GetRandomOutfit()
    {
        Outfit o = Outfits[Random.Range(0, Outfits.Length)];

        return new Dictionary<AppearanceSlot, Color>
        {
            { AppearanceSlot.Hair, o.Hair },
            { AppearanceSlot.Eyebrow, o.Eyebrow },
            { AppearanceSlot.Shirt, o.Shirt },
            { AppearanceSlot.Suit, o.Suit },
            { AppearanceSlot.Pants, o.Pants }
        };
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}