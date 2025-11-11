using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using quickLink.Constants;
using quickLink.Models;
using quickLink.Services.Helpers;

namespace quickLink.Services
{
    public sealed class CommandService
    {
        private readonly string _commandsFilePath;
        private readonly SemaphoreSlim _fileLock;
        private readonly JsonSerializerOptions _jsonOptions;
        private List<UserCommand>? _cachedCommands;

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CommandDto))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SourceConfigDto))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CommandSourceType))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CommandIcon))]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "AOT is not used for this application")]
        public CommandService()
        {
            _commandsFilePath = ServiceInitializer.GetDataFilePath(AppConstants.Files.CommandsFile);
            _fileLock = new SemaphoreSlim(1, 1);
            _jsonOptions = ServiceInitializer.GetJsonSerializerOptions();
        }

        public async Task EnsureCommandsFileExistsAsync()
        {
            if (!File.Exists(_commandsFilePath))
            {
                // Create empty commands file
                var emptyCommands = new List<UserCommand>();
                await SaveCommandsAsync(emptyCommands);
                System.Diagnostics.Debug.WriteLine("CommandService: Created empty commands.json file");
            }
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Trimming is disabled for this application")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "AOT is not used for this application")]
        public async Task<List<UserCommand>> LoadCommandsAsync()
        {
            // Return cached commands if available
            if (_cachedCommands != null)
            {
                return new List<UserCommand>(_cachedCommands);
            }

            await _fileLock.WaitAsync();
            try
            {
                if (!File.Exists(_commandsFilePath))
                {
                    // Return empty list if file doesn't exist yet
                    _cachedCommands = new List<UserCommand>();
                    return new List<UserCommand>();
                }

                var json = await File.ReadAllTextAsync(_commandsFilePath);
                var commands = JsonSerializer.Deserialize<List<CommandDto>>(json, _jsonOptions)
                    ?? new List<CommandDto>();

                _cachedCommands = commands.Select(MapDtoToCommand).ToList();
                return new List<UserCommand>(_cachedCommands);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CommandService.LoadCommandsAsync ERROR: {ex.Message}");
                _cachedCommands = new List<UserCommand>();
                return new List<UserCommand>();
            }
            finally
            {
                _fileLock.Release();
            }
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Trimming is disabled for this application")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "AOT is not used for this application")]
        public async Task SaveCommandsAsync(List<UserCommand> commands)
        {
            await _fileLock.WaitAsync();
            try
            {
                var dtos = commands.Select(MapCommandToDto).ToList();
                var json = JsonSerializer.Serialize(dtos, _jsonOptions);
                await File.WriteAllTextAsync(_commandsFilePath, json);

                _cachedCommands = new List<UserCommand>(commands);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public Task AddCommandAsync(UserCommand command, List<UserCommand> currentCommands)
        {
            currentCommands.Add(command);
            return SaveCommandsAsync(currentCommands);
        }

        public Task UpdateCommandAsync(UserCommand oldCommand, UserCommand newCommand, List<UserCommand> currentCommands)
        {
            var index = currentCommands.IndexOf(oldCommand);
            if (index >= 0)
            {
                currentCommands[index] = newCommand;
                return SaveCommandsAsync(currentCommands);
            }
            return Task.CompletedTask;
        }

        public Task DeleteCommandAsync(UserCommand command, List<UserCommand> currentCommands)
        {
            currentCommands.Remove(command);
            return SaveCommandsAsync(currentCommands);
        }

        private UserCommand MapDtoToCommand(CommandDto dto)
        {
            return new UserCommand
            {
                Prefix = dto.Prefix ?? string.Empty,
                Source = dto.Source,
                SourceConfig = new SourceConfig
                {
                    Path = dto.SourceConfig?.Path ?? string.Empty,
                    Recursive = dto.SourceConfig?.Recursive ?? true,
                    Glob = dto.SourceConfig?.Glob ?? "*.*",
                    Items = dto.SourceConfig?.Items ?? new List<string>()
                },
                ExecuteTemplate = dto.ExecuteTemplate ?? string.Empty,
                Icon = dto.Icon
            };
        }

        private CommandDto MapCommandToDto(UserCommand command)
        {
            return new CommandDto
            {
                Prefix = command.Prefix,
                Source = command.Source,
                SourceConfig = new SourceConfigDto
                {
                    Path = command.SourceConfig.Path,
                    Recursive = command.SourceConfig.Recursive,
                    Glob = command.SourceConfig.Glob,
                    Items = command.SourceConfig.Items
                },
                ExecuteTemplate = command.ExecuteTemplate,
                Icon = command.Icon
            };
        }

        private sealed class CommandDto
        {
            public string? Prefix { get; set; }
            public CommandSourceType Source { get; set; }
            public SourceConfigDto? SourceConfig { get; set; }
            public string? ExecuteTemplate { get; set; }
            public CommandIcon Icon { get; set; }
        }

        private sealed class SourceConfigDto
        {
            public string? Path { get; set; }
            public bool Recursive { get; set; }
            public string? Glob { get; set; }
            public List<string>? Items { get; set; }
        }
    }
}
