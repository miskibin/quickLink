using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace quickLink.Services
{
    public sealed class MediaControlService
    {
        private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private bool _isInitialized;

        private async Task EnsureInitializedAsync()
        {
            if (_isInitialized) return;

            await _initLock.WaitAsync();
            try
            {
                if (_isInitialized) return;

                _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize media control: {ex.Message}");
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async Task<bool> SkipToNextAsync()
        {
            await EnsureInitializedAsync();
            if (_sessionManager?.GetCurrentSession() is not { } session)
                return false;

            var playbackInfo = session.GetPlaybackInfo();
            if (!playbackInfo.Controls.IsNextEnabled)
                return false;

            return await session.TrySkipNextAsync();
        }

        public async Task<bool> SkipToPreviousAsync()
        {
            await EnsureInitializedAsync();
            if (_sessionManager?.GetCurrentSession() is not { } session)
                return false;

            var playbackInfo = session.GetPlaybackInfo();
            if (!playbackInfo.Controls.IsPreviousEnabled)
                return false;

            return await session.TrySkipPreviousAsync();
        }

        public async Task<bool> PlayPauseAsync()
        {
            await EnsureInitializedAsync();
            if (_sessionManager?.GetCurrentSession() is not { } session)
                return false;

            var playbackInfo = session.GetPlaybackInfo();
            if (!playbackInfo.Controls.IsPlayPauseToggleEnabled)
                return false;

            return await session.TryTogglePlayPauseAsync();
        }

        public bool CanSkipNext()
        {
            var session = _sessionManager?.GetCurrentSession();
            return session?.GetPlaybackInfo().Controls.IsNextEnabled ?? false;
        }

        public bool CanSkipPrevious()
        {
            var session = _sessionManager?.GetCurrentSession();
            return session?.GetPlaybackInfo().Controls.IsPreviousEnabled ?? false;
        }

        public bool CanPlayPause()
        {
            var session = _sessionManager?.GetCurrentSession();
            return session?.GetPlaybackInfo().Controls.IsPlayPauseToggleEnabled ?? false;
        }
    }
}
