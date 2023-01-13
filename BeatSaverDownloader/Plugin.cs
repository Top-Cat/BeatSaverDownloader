using BeatSaverDownloader.Misc;
using BeatSaverDownloader.UI;
using IPA;
using System;
using System.Collections.Concurrent;
using BeatSaverDownloader.Bookmarks;
using BeatSaverDownloader.UI.ViewControllers.DownloadQueue;
using BeatSaverSharp.Http;
using BS_Utils.Utilities;
using IPA.Loader;
using IPA.Utilities;

namespace BeatSaverDownloader
{
    public enum SongQueueState { Queued, Downloading, Downloaded, Error };

    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        private PluginMetadata _metadata;
        public static IPA.Logging.Logger LOG;
        public static BeatSaverSharp.BeatSaver BeatSaver;
        internal CallbackListener Listener;
        internal BookmarksApi BookmarksApi;
        internal QueueManager QueueManager;
        public static string UserAgent = "";

        [Init]
        public void Init(object nullObject, IPA.Logging.Logger logger, PluginMetadata metadata)
        {
            UserAgent = $"BeatSaverDownloader/{metadata.HVersion}";
            _metadata = metadata;
            LOG = logger;
        }

        public void OnApplicationQuit()
        {
            PluginConfig.SaveConfig();
        }

        [OnStart]
        public void OnApplicationStart()
        {
            BeatSaver = new BeatSaverSharp.BeatSaver(
                new BeatSaverSharp.BeatSaverOptions(
                    "BeatSaverDownloader",
                    new Version((int) _metadata.HVersion.Major, (int) _metadata.HVersion.Minor, (int) _metadata.HVersion.Patch)
                )
            );

            PluginConfig.LoadConfig();
            Sprites.ConvertToSprites();

            if (OauthConfig.Current.AppAuth != null)
            {
                var httpService = (UnityWebRequestService) BeatSaver.GetField<IHttpService, BeatSaverSharp.BeatSaver>("_httpService");
                httpService.Headers.Add("X-App-Auth", OauthConfig.Current.AppAuth);
            }

            var tokenApi = new TokenApi(_metadata);
            Listener = new CallbackListener(tokenApi);
            QueueManager = new QueueManager();
            BookmarksApi = new BookmarksApi(tokenApi, QueueManager);

            PluginUI.instance.Setup(BookmarksApi, QueueManager);

            if (PluginManager.GetPlugin("BetterSongList") != null)
                RegisterBookmarksFilter();

            BSEvents.earlyMenuSceneLoadedFresh += OnMenuSceneLoadedFresh;
        }

        private void RegisterBookmarksFilter()
        {
            var _ = new BookmarksFilter(BookmarksApi);
        }

        [OnEnable]
        public void OnEnable()
        {
            Listener.Start();
        }

        [OnDisable]
        public void OnDisable()
        {
            Listener.Stop();
        }

        [OnExit]
        public void OnExit()
        {
            BookmarksApi.Store();
        }

        private void OnMenuSceneLoadedFresh(ScenesTransitionSetupDataSO data)
        {
            try
            {
                PluginUI.SetupLevelDetailClone();
                Settings.SetupSettings();
                SongCore.Loader.SongsLoadedEvent += Loader_SongsLoadedEvent;
            }
            catch (Exception e)
            {
                LOG.Critical("Exception on fresh menu scene change: " + e);
            }
        }

        private async void Loader_SongsLoadedEvent(SongCore.Loader arg1, ConcurrentDictionary<string, CustomPreviewBeatmapLevel> arg2)
        {
            if (PluginUI.instance.MoreSongsButton.Interactable) return;

            PluginUI.instance.MoreSongsButton.Interactable = true;

            if (PluginConfig.UserTokens?.CouldBeValid == true && PluginConfig.SyncOnLoad)
            {
                try
                {
                    await BookmarksApi.Sync(false);
                }
                catch (TokenApi.InvalidOauthCredentialsException e)
                {
                    // Do nothing
                }
            }
        }
    }
}
