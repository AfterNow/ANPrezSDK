using System;
using System.Collections.Generic;
using System.IO;

    [Serializable]
    public class ARPAsset
    {
        public string id;
        public string url;
        public string status;
        public DateTime lastModified;
        public int filesize;
        public bool locked;
        public PrezVector3[] UndoPosition = null;
        public ItemTransform itemTransform;
        public ANPAssetType type;
        public float volumn;
        public ARPTransition transition;
        public ARPText text;
        public List<string> animations;
        public bool isClickable;
        public string clickTarget;

        public ARPAsset(string url, ANPAssetType type)
        {
            this.url = url;
            this.type = type;
        }

        public string Url(string replacement, string accessToken = null)
        {
            if (accessToken == null) accessToken = InternalStates.AccessToken;

            if (type == ANPAssetType.TEXT)
            {
                return null;
            }
            //return string.IsNullOrEmpty(url) ? string.Empty : $"{SDKConstants.BASE_URL}/containers/{url}?access_token={accessToken}";

            string newUrl = url;
            if (!string.IsNullOrEmpty(replacement) && url.Contains(".unitypackage"))
            {
                newUrl = url.Replace(".unitypackage", replacement).ToLower();
            }

            string _url = string.IsNullOrEmpty(newUrl) ? string.Empty : $"{SDKConstants.BASE_URL}/containers/{newUrl}?access_token={accessToken}";
            //PrezDebugger.Log("_url : " + _url);
            return _url;
        }

        public string FileName()
        {
            if (!string.IsNullOrEmpty(url))
            {
                return /*id + "-" + */url.Substring(url.LastIndexOf("/") + 1);
            }
            return null;
        }

        public string AbsoluteDownloadPath(string relativePath)
        {
            string typePath = null;

            switch (type)
            {
                case ANPAssetType.IMAGE:
                    typePath = "images";
                    break;
                case ANPAssetType.VIDEO:
                    typePath = "videos";
                    break;
                case ANPAssetType.AUDIO:
                    typePath = "audio";
                    break;
                case ANPAssetType.OBJECT:
                    typePath = "objects";
                    break;
            }
            return Path.Combine(relativePath, typePath, FileName());
        }
    }

    public enum ANPAssetType { IMAGE, VIDEO, OBJECT, AUDIO, TEXT }

    [Serializable]
    public class ARPText
    {
        public string value;

        public ARPFont font;

        public ARPFontStyle fontStyle;

        public ARPFontAlignment alignment;

        public string color;

        public string secondaryColor;

        public int shadowLevel;

        public string GetFontName() => font.ToString() + "-" + fontStyle.ToString();
    }

    public enum ARPFont { Brandon, KievitslabOT, Lato, Montserrat, Roboto, SourceSansPro }
    public enum ARPFontStyle { Regular, RegularItalic, Bold, BoldItalic, Light, LightItalic, Black, BlackItalic }
    public enum ARPFontAlignment { LEFT, CENTER, RIGHT }
