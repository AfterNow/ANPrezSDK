using AfterNow.PrezSDK.Internal.Helpers;
using System;
using System.Collections.Generic;

namespace AfterNow.PrezSDK.Internal.Views
{
    [Serializable]
    public class Location
    {
        public string id;
        public string name;
        public string bgm;
        public string background;
        public int backgroundOrientation;
        public DateTime lastModifiedBgm;
        public DateTime lastModifiedBg;
        public int bgFilesize;
        public int bgmFilesize;

        public string Bgm(string accessToken = null)
        {
            return InternalHelper.GetURL(bgm, accessToken);
        }

        public string Background(string accessToken = null)
        {
            return InternalHelper.GetURL(background, accessToken);
        }

        public List<Slide> slides = new List<Slide>();

        public ItemTransform itemTransform;
    }
}