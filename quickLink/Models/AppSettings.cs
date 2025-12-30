namespace quickLink.Models
{
    public class AppSettings
    {
        public uint HotkeyModifiers { get; set; } = 0x0006; // MOD_CONTROL | MOD_SHIFT
        public uint HotkeyKey { get; set; } = 0x41; // VK_A
        public bool HideFooter { get; set; } = true;
        public string SearchUrl { get; set; } = "https://chatgpt.com/?q={query}";
        public string ApiKey { get; set; } = string.Empty;
    }
}
