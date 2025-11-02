using System;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace quickLink.Services
{
    public sealed class MediaControlService
    {
        private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;

        public async Task InitializeAsync()
        {
            try
            {
                _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            }
            catch (Exception ex)
            {
                // Handle initialization failure
                System.Diagnostics.Debug.WriteLine($"Failed to initialize media control: {ex.Message}");
            }
        }

        public async Task<bool> SkipToNextAsync()
        {
            if (_sessionManager?.GetCurrentSession() is not { } session)
                return false;

            var playbackInfo = session.GetPlaybackInfo();
            if (!playbackInfo.Controls.IsNextEnabled)
                return false;

            return await session.TrySkipNextAsync();
        }

        public async Task<bool> SkipToPreviousAsync()
        {
            if (_sessionManager?.GetCurrentSession() is not { } session)
                return false;

            var playbackInfo = session.GetPlaybackInfo();
            if (!playbackInfo.Controls.IsPreviousEnabled)
                return false;

            return await session.TrySkipPreviousAsync();
        }

        public async Task<bool> PlayPauseAsync()
        {
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
