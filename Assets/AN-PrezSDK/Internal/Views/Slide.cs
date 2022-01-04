using System;
using System.Collections.Generic;

    [Serializable]
    public class Slide
    {
        public string id;
        public string name;
        public List<ARPAsset> assets = new List<ARPAsset>();
        public ARPSlideTransition transition;
        public List<ARPTransition> assetTransitions;

        public Slide(float delay, List<ARPAsset> assets, List<ARPTransition> assetTransitions)
        {
            this.assets.Clear();
            foreach (ARPAsset asset in assets)
            {
                this.assets.Add(asset);
            }
            this.assetTransitions = assetTransitions;
        }

        public string background;
        public int backgroundOrientation;
        public DateTime lastModifiedBg;
        public int bgFilesize;

        public string BackgroundUrl(string accessToken = null) => InternalHelper.GetURL(background, accessToken);

        /// <summary>
        /// Stores the index of the slide in Location. 
        /// This value is set from the code manually. Donot change this value anywhere else.
        /// </summary>
        public int SlideIdx { get; set; }

        /// <summary>
        /// Reference to the slide specific background vr, if it exists.
        /// Returns null when the slide doesn't have a background VR.
        /// This function might also return null when the slide hasn't finished downloading. So always check 'DownloadProgress' or 'State' before accessing this field.
        /// </summary>
        public object BackgroundTexture { get; set; }

        /// <summary>
        /// Shows the downloading progress of a slide from 0-1. Returns 0 when download hasn't started. Returns 1 when completed.
        /// </summary>
        public float DownloadProgress => State == SlideState.Finished ? 1 : (totalBytes != 0 ? downloadedBytes / totalBytes : 0);

        /// <summary>
        /// Shows the download progress of the slide
        /// </summary>
        public SlideState State = SlideState.NotInitialized; //kept public because this can be used in future to show a better loading progress.

        /// <summary>
        /// Stores the amount of data which is being downloaded.
        /// </summary>
        public float downloadedBytes;

        /// <summary>
        /// Total size of all slide contents in bytes.
        /// </summary>
        public float totalBytes;

        /// <summary>
        /// Count of total assets which needs to be loaded
        /// </summary>
        public int TotalAssetsToLoad => assets.Count;

        /// <summary>
        /// Count of total assets which are loading. This will be reset when slide state changes
        /// </summary>
        public int LoadedAssets;

        /// <summary>
        /// This bool will determine if progress for the slide will be shown or not
        /// </summary>
        public bool ShowProgress;

        public enum SlideState
        {
            NotInitialized,
            RetrievingData,
            DownloadingAssets,
            LoadingAssets,
            Finished
        }
    }

