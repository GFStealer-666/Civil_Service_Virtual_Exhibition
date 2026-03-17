using UnityEngine;
using System.Collections.Generic;

public static class AppearanceColorPalette
{
    public class Outfit
    {
        public string Name;
        public Color  Hair;
        public Color  Eyebrow;
        public Color  Shirt;
        public Color  Suit;
        public Color  Pants;
        // Add more slots as needed
    }
    private static readonly Outfit[] MaleOutfits =
    {
        new Outfit
        {
            Name    = "Navy Professional",
            Hair    = HexColor("1A1A1A"),  
            Eyebrow = HexColor("111111"),  
            Suit    = HexColor("1C2B4A"),  
            Shirt   = HexColor("FFFFFF"),  
            Pants   = HexColor("1C2B4A"),  
        },
        new Outfit
        {
            Name    = "Charcoal Classic",
            Hair    = HexColor("2C1810"),  // dark brown
            Eyebrow = HexColor("1A0F0A"),  // very dark brown
            Suit    = HexColor("2F2F2F"),  // charcoal
            Shirt   = HexColor("F5F5DC"),  // cream
            Pants   = HexColor("3D3D3D"),  // dark grey
        },
        new Outfit
        {
            Name    = "Brown Warmth",
            Hair    = HexColor("5C3317"),  // medium brown
            Eyebrow = HexColor("3B2310"),  // dark brown
            Suit    = HexColor("4A3728"),  // dark brown suit
            Shirt   = HexColor("D8E8F0"),  // light blue
            Pants   = HexColor("3D2B1F"),  // dark brown pants
        },
        new Outfit
        {
            Name    = "Midnight Blue",
            Hair    = HexColor("1C3A5E"),  // dark navy (stylized)
            Eyebrow = HexColor("1A1A2E"),  // very dark
            Suit    = HexColor("1A1A2E"),  // midnight blue
            Shirt   = HexColor("E8E0F0"),  // lavender
            Pants   = HexColor("2C3E50"),  // dark slate blue
        },
        new Outfit
        {
            Name    = "Forest Gentleman",
            Hair    = HexColor("2C1810"),  // dark brown
            Eyebrow = HexColor("1A0F0A"),  // very dark brown
            Suit    = HexColor("1C3A1C"),  // forest green
            Shirt   = HexColor("F5F5DC"),  // cream
            Pants   = HexColor("2F2F2F"),  // charcoal
        },
        new Outfit
        {
            Name    = "Blonde Casual",
            Hair    = HexColor("C8A951"),  // blonde
            Eyebrow = HexColor("8B6914"),  // dark blonde
            Suit    = HexColor("5C4033"),  // coffee brown
            Shirt   = HexColor("87CEEB"),  // sky blue
            Pants   = HexColor("4A4A4A"),  // dark grey
        },
        new Outfit
        {
            Name    = "Golden Classic",
            Hair    = HexColor("D4A017"),  // golden blonde
            Eyebrow = HexColor("8B6914"),  // dark blonde
            Suit    = HexColor("2A2A4A"),  // dark indigo
            Shirt   = HexColor("FFFFFF"),  // white
            Pants   = HexColor("1A1A1A"),  // black
        },
        new Outfit
        {
            Name    = "Burgundy Scholar",
            Hair    = HexColor("1A1A1A"),  // black
            Eyebrow = HexColor("111111"),  // black
            Suit    = HexColor("4A2C4A"),  // dark purple
            Shirt   = HexColor("F5F5DC"),  // cream
            Pants   = HexColor("2F2F2F"),  // charcoal
        },
        new Outfit
        {
            Name    = "Auburn Warm",
            Hair    = HexColor("7B3F00"),  // chestnut
            Eyebrow = HexColor("4A2800"),  // dark chestnut
            Suit    = HexColor("3D3D3D"),  // dark grey
            Shirt   = HexColor("E8D8D8"),  // light pink
            Pants   = HexColor("5C4A3A"),  // taupe
        },
        new Outfit
        {
            Name    = "Slate Modern",
            Hair    = HexColor("2F4F4F"),  
            Eyebrow = HexColor("1A2E2E"),  
            Suit    = HexColor("2C4A2C"), 
            Shirt   = HexColor("D8F0D8"), 
            Pants   = HexColor("2F2F2F"), 
        },
    };

    public static Dictionary<AppearanceSlot, Color> GetRandomMaleOutfit()
    {
        Outfit outfit = MaleOutfits[Random.Range(0, MaleOutfits.Length)];
        Debug.Log($"[AppearanceColorPalette] Selected outfit: {outfit.Name}");

        return new Dictionary<AppearanceSlot, Color>
        {
            { AppearanceSlot.Hair,    outfit.Hair    },
            { AppearanceSlot.Eyebrow, outfit.Eyebrow },
            { AppearanceSlot.Shirt,   outfit.Shirt   },
            { AppearanceSlot.Suit,    outfit.Suit    },
            { AppearanceSlot.Pants,   outfit.Pants   },
        };
    }

    private static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color color);
        return color;
    }
}