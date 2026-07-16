using System;
using System.Threading.Tasks;
using quickLink.Models;

namespace quickLink.Services
{
    /// <summary>
    /// Pragmatic (non-DI) owner of the loaded <see cref="AppSettings"/>. The standalone
    /// settings window mutates <see cref="Current"/> and calls <see cref="SaveAsync"/>;
    /// interested parties (MainWindow) subscribe to <see cref="SettingsChanged"/> to react.
    /// </summary>
    public sealed class SettingsService
    {
        private readonly DataService _dataService;

        public AppSettings Current { get; private set; }

        /// <summary>
        /// Raised on the UI thread after settings have been persisted.
        /// </summary>
        public event Action? SettingsChanged;

        /// <summary>
        /// Set by MainWindow. Given Win32 modifiers/key, re-registers the global hotkey and
        /// returns whether registration succeeded so the settings UI can report status.
        /// </summary>
        public Func<uint, uint, bool>? HotkeyReapplyCallback { get; set; }

        public SettingsService(DataService dataService, AppSettings initial)
        {
            _dataService = dataService;
            Current = initial;
        }

        public async Task SaveAsync()
        {
            await _dataService.SaveSettingsAsync(Current);
            SettingsChanged?.Invoke();
        }

        /// <summary>
        /// Writes the hotkey into <see cref="Current"/> and asks MainWindow to re-register it.
        /// Does not persist; call <see cref="SaveAsync"/> afterwards.
        /// </summary>
        public bool TryApplyHotkey(uint modifiers, uint key)
        {
            Current.HotkeyModifiers = modifiers;
            Current.HotkeyKey = key;
            return HotkeyReapplyCallback?.Invoke(modifiers, key) ?? false;
        }
    }
}
