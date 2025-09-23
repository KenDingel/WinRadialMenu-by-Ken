using System.Collections.Generic;
using System.Linq;

namespace RadialMenu.Models
{
    public class ColorOption
    {
        public string Name { get; set; } = "";
        public string HexValue { get; set; } = "";
        public string Description { get; set; } = "";

        public override string ToString() => Name;
    }

    public static class ColorPalette
    {
        public static List<ColorOption> PredefinedColors { get; } = new List<ColorOption>
        {
            // Primary colors - vibrant and distinct
            new ColorOption { Name = "Ocean Blue", HexValue = "#FF2196F3", Description = "Modern blue, great for productivity apps" },
            new ColorOption { Name = "Forest Green", HexValue = "#FF4CAF50", Description = "Fresh green, perfect for utilities and tools" },
            new ColorOption { Name = "Sunset Orange", HexValue = "#FFFF9800", Description = "Warm orange, ideal for media and creativity" },
            new ColorOption { Name = "Ruby Red", HexValue = "#FFF44336", Description = "Bold red, excellent for system and power actions" },
            new ColorOption { Name = "Royal Purple", HexValue = "#FF9C27B0", Description = "Rich purple, great for advanced features" },
            
            // Secondary colors - more muted but still vibrant
            new ColorOption { Name = "Sky Cyan", HexValue = "#FF00BCD4", Description = "Light cyan, perfect for communication apps" },
            new ColorOption { Name = "Lime Green", HexValue = "#FF8BC34A", Description = "Bright lime, great for nature and health apps" },
            new ColorOption { Name = "Amber Gold", HexValue = "#FFFFC107", Description = "Golden amber, ideal for documents and files" },
            new ColorOption { Name = "Deep Pink", HexValue = "#FFE91E63", Description = "Vibrant pink, excellent for social and media" },
            new ColorOption { Name = "Indigo", HexValue = "#FF3F51B5", Description = "Deep indigo, perfect for professional tools" },
            
            // Tertiary colors - softer tones
            new ColorOption { Name = "Teal", HexValue = "#FF009688", Description = "Calming teal, great for productivity and focus" },
            new ColorOption { Name = "Light Blue", HexValue = "#FF03A9F4", Description = "Soft blue, ideal for information and data" },
            new ColorOption { Name = "Brown", HexValue = "#FF795548", Description = "Earthy brown, perfect for files and documents" },
            new ColorOption { Name = "Blue Grey", HexValue = "#FF607D8B", Description = "Neutral grey-blue, great for utilities" },
            new ColorOption { Name = "Deep Orange", HexValue = "#FFFF5722", Description = "Rich orange, excellent for creative tools" },
            
            // Monochrome options
            new ColorOption { Name = "Charcoal", HexValue = "#FF424242", Description = "Dark charcoal, professional and subtle" },
            new ColorOption { Name = "Silver", HexValue = "#FF9E9E9E", Description = "Light silver, clean and modern" },
            new ColorOption { Name = "Midnight", HexValue = "#FF212121", Description = "Deep black, sleek and minimal" }
        };

        public static ColorOption GetColorByHex(string hexValue)
        {
            return PredefinedColors.FirstOrDefault(c => c.HexValue.Equals(hexValue, System.StringComparison.OrdinalIgnoreCase))
                ?? new ColorOption { Name = "Custom", HexValue = hexValue, Description = "Custom color" };
        }

        public static ColorOption GetDefaultColor()
        {
            return PredefinedColors.First(); // Ocean Blue as default
        }
    }
}