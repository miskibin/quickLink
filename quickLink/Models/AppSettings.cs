namespace quickLink.Models
{
    public class AppSettings
    {
        public uint HotkeyModifiers { get; set; } = 0x0002; // MOD_CONTROL
        public uint HotkeyKey { get; set; } = 0x20; // VK_SPACE
        public bool HideFooter { get; set; } = true;
        public string SearchUrl { get; set; } = "https://chatgpt.com/?q={query}";
        public string ApiKey { get; set; } = string.Empty;
    }
}
