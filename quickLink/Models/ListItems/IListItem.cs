using System.Threading.Tasks;

namespace quickLink.Models.ListItems
{
    /// <summary>
    /// Base interface for all items that can appear in the QuickLink list
    /// </summary>
    public interface IListItem
    {
        /// <summary>
        /// Title displayed in the UI
        /// </summary>
        string DisplayTitle { get; }

        /// <summary>
        /// Value/description displayed in the UI
        /// </summary>
        string DisplayValue { get; }

        /// <summary>
        /// Segoe UI icon glyph code for this item type
        /// </summary>
        string IconGlyph { get; }

        /// <summary>
        /// Icon foreground color (hex format)
        /// </summary>
        string IconColor { get; }

        /// <summary>
        /// Whether this item shows the edit button in the UI
        /// </summary>
        bool ShowEditButton { get; }

        /// <summary>
        /// Whether this item shows the delete button in the UI
        /// </summary>
        bool ShowDeleteButton { get; }

        /// <summary>
        /// If true, pressing Enter will autocomplete rather than execute
        /// </summary>
        bool SupportsAutocomplete { get; }

        /// <summary>
        /// Autocomplete text to fill in the search box (only if SupportsAutocomplete is true)
        /// </summary>
        string AutocompleteText { get; }

        /// <summary>
        /// Execute this item's action
        /// </summary>
        Task ExecuteAsync(IExecutionContext context);

        /// <summary>
        /// Check if the item matches the search query
        /// </summary>
        bool MatchesSearch(string searchText);
    }

    /// <summary>
    /// Context provided to items for execution
    /// </summary>
    public interface IExecutionContext
    {
        Task OpenUrlAsync(string url);
        void CopyToClipboard(string text);
        Task ExecuteCommandAsync(string command);
        Task ExecuteMediaCommandAsync(string command);
        void HideWindow();
        void ShowEditPanel(IEditableItem item);
        void ShowCommandPanel(UserCommand? command);
        void ShowSettingsPanel();
        void ExitApplication();
    }

    /// <summary>
    /// Interface for items that can be edited (inherits from IListItem)
    /// </summary>
    public interface IEditableItem : IListItem
    {
        string Title { get; }
        string Value { get; }
        bool IsEncrypted { get; }
    }
}
