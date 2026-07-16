using System.Collections.Generic;

namespace quickLink.Models
{
    public enum AiProvider
    {
        XAI,
        OpenAI,
        Claude,
        // Legacy: kept so old settings.json values still deserialize. Migrated to Custom on load.
        Ollama,
        Custom
    }

    /// <summary>
    /// Per-provider AI configuration. BaseUrl is only meaningful for the Custom provider.
    /// </summary>
    public class ProviderConfig
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
    }

    public class AppSettings
    {
        public uint HotkeyModifiers { get; set; } = 0x0006; // MOD_CONTROL | MOD_SHIFT
        public uint HotkeyKey { get; set; } = 0x41; // VK_A
        public bool HideFooter { get; set; } = true;
        public string SearchUrl { get; set; } = "https://chatgpt.com/?q={query}";
        public AiProvider AiProvider { get; set; } = AiProvider.XAI;

        // Per-provider config keyed by provider. This is the source of truth for new files.
        public Dictionary<AiProvider, ProviderConfig> Providers { get; set; } = new();

        // Legacy single-provider fields. Kept nullable for migration only; new files omit them.
        public string? ApiKey { get; set; }
        public string? AiModel { get; set; }

        /// <summary>
        /// Returns (lazily creating) the config for the currently selected provider.
        /// </summary>
        public ProviderConfig GetActiveProviderConfig()
        {
            if (!Providers.TryGetValue(AiProvider, out var config))
            {
                config = new ProviderConfig();
                Providers[AiProvider] = config;
            }
            return config;
        }

        /// <summary>
        /// Migrates legacy settings into the new per-provider model. Idempotent.
        /// Call after decrypting keys on load.
        /// </summary>
        public void MigrateLegacy()
        {
            // Seed the per-provider dictionary from the old single ApiKey/AiModel fields.
            if (Providers.Count == 0 &&
                (!string.IsNullOrEmpty(ApiKey) || !string.IsNullOrEmpty(AiModel)))
            {
                Providers[AiProvider] = new ProviderConfig
                {
                    ApiKey = ApiKey ?? string.Empty,
                    Model = AiModel ?? string.Empty,
                    BaseUrl = string.Empty
                };
            }

            // Legacy Ollama provider becomes Custom with the Ollama endpoint as its base URL.
            if (AiProvider == AiProvider.Ollama)
            {
                var config = Providers.TryGetValue(AiProvider.Ollama, out var existing)
                    ? existing
                    : new ProviderConfig();

                if (string.IsNullOrWhiteSpace(config.BaseUrl))
                {
                    config.BaseUrl = "http://localhost:11434/v1/chat/completions";
                }

                Providers.Remove(AiProvider.Ollama);
                Providers[AiProvider.Custom] = config;
                AiProvider = AiProvider.Custom;
            }

            // Legacy fields are no longer the source of truth.
            ApiKey = null;
            AiModel = null;
        }
    }
}
