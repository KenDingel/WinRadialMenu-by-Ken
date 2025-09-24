using System.Collections.Generic;
using System.Linq;

namespace RadialMenu.Models
{
    public class IconOption
    {
        public string Emoji { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";

        public override string ToString() => Name;
    }

    public static class IconPalette
    {
        public static List<IconOption> PredefinedIcons { get; } = new List<IconOption>
        {
            // File and document icons
            new IconOption { Emoji = "📄", Name = "Document", Description = "Generic document or file" },
            new IconOption { Emoji = "📁", Name = "Folder", Description = "Directory or folder" },
            new IconOption { Emoji = "📝", Name = "Note", Description = "Text note or memo" },
            new IconOption { Emoji = "📊", Name = "Chart", Description = "Data or statistics" },
            new IconOption { Emoji = "📈", Name = "Graph", Description = "Chart or analytics" },

            // Application and tool icons
            new IconOption { Emoji = "⚙️", Name = "Settings", Description = "Settings or configuration" },
            new IconOption { Emoji = "🔧", Name = "Tools", Description = "Tools or utilities" },
            new IconOption { Emoji = "💻", Name = "Computer", Description = "Computer or software" },
            new IconOption { Emoji = "🌐", Name = "Web", Description = "Internet or web browser" },
            new IconOption { Emoji = "📱", Name = "Mobile", Description = "Mobile device or app" },

            // Action icons
            new IconOption { Emoji = "▶️", Name = "Play", Description = "Start or play action" },
            new IconOption { Emoji = "⏸️", Name = "Pause", Description = "Pause or stop action" },
            new IconOption { Emoji = "🔄", Name = "Refresh", Description = "Reload or refresh" },
            new IconOption { Emoji = "🔍", Name = "Search", Description = "Search or find" },
            new IconOption { Emoji = "✏️", Name = "Edit", Description = "Edit or modify" },

            // Communication icons
            new IconOption { Emoji = "💬", Name = "Chat", Description = "Chat or messaging" },
            new IconOption { Emoji = "📧", Name = "Email", Description = "Email or mail" },
            new IconOption { Emoji = "📞", Name = "Phone", Description = "Phone or call" },
            new IconOption { Emoji = "📢", Name = "Announcement", Description = "Notification or alert" },

            // Media and creative icons
            new IconOption { Emoji = "🎵", Name = "Music", Description = "Music or audio" },
            new IconOption { Emoji = "🎬", Name = "Video", Description = "Video or movie" },
            new IconOption { Emoji = "📷", Name = "Camera", Description = "Photo or image" },
            new IconOption { Emoji = "🎨", Name = "Art", Description = "Art or design" },
            new IconOption { Emoji = "🎯", Name = "Target", Description = "Goal or objective" },

            // System and utility icons
            new IconOption { Emoji = "🔒", Name = "Lock", Description = "Security or lock" },
            new IconOption { Emoji = "🔓", Name = "Unlock", Description = "Unlock or access" },
            new IconOption { Emoji = "🗑️", Name = "Trash", Description = "Delete or remove" },
            new IconOption { Emoji = "📋", Name = "Clipboard", Description = "Copy or clipboard" },
            new IconOption { Emoji = "⭐", Name = "Star", Description = "Favorite or important" },

            // Additional common icons
            new IconOption { Emoji = "🏠", Name = "Home", Description = "Home or main page" },
            new IconOption { Emoji = "👤", Name = "User", Description = "User or profile" },
            new IconOption { Emoji = "⚡", Name = "Power", Description = "Power or energy" },
            new IconOption { Emoji = "🔥", Name = "Fire", Description = "Hot or trending" },
            new IconOption { Emoji = "❄️", Name = "Ice", Description = "Cool or frozen" }
        };

        public static IconOption GetIconByEmoji(string emoji)
        {
            return PredefinedIcons.FirstOrDefault(i => i.Emoji == emoji)
                ?? new IconOption { Emoji = emoji, Name = "Custom", Description = "Custom icon" };
        }
    }
}