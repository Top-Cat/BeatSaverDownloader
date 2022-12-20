using BeatSaverDownloader.Misc;
using BeatSaverDownloader.UI;
using IPA;
using System;
using UnityEngine.SceneManagement;
using System.Collections.Concurrent;

namespace BeatSaverDownloader
{
    public enum SongQueueState { Queued, Downloading, Downloaded, Error };

    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        private IPA.Loader.PluginMetadata _metadata;
        public static IPA.Logging.Logger log;
        public static BeatSaverSharp.BeatSaver BeatSaver;

        [Init]
        public void Init(object nullObject, IPA.Logging.Logger logger, IPA.Loader.PluginMetadata metadata)
        {
            _metadata = metadata;
            log = logger;
            BeastSaber.BeastSaberApiHelper.InitializeBeastSaberHttpClient(metadata);
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

            PluginUI.instance.Setup();

            BS_Utils.Utilities.BSEvents.earlyMenuSceneLoadedFresh += OnMenuSceneLoadedFresh;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
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
                log.Critical("Exception on fresh menu scene change: " + e);
            }
        }

        private void Loader_SongsLoadedEvent(SongCore.Loader arg1, ConcurrentDictionary<string, CustomPreviewBeatmapLevel> arg2)
        {
            if (!PluginUI.instance.MoreSongsButton.Interactable)
                PluginUI.instance.MoreSongsButton.Interactable = true;
        }

        public void OnUpdate()
        {
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
        }

        public void OnSceneUnloaded(Scene scene)
        {
        }

        public void OnActiveSceneChanged(Scene prevScene, Scene nextScene)
        {
        }

        public void OnFixedUpdate()
        {
        }
    }
}
