namespace AfterNow.AnPrez.SDK.Internal
{
    public class SDKConstants
    {
        internal const string BASE_URL = "https://api.anprez-staging.afternow.io/api";
        internal const string GuestEmail = "spatialape_user@afternow.io";
        internal const string GuestPassword = "spatialApe_AfterNow";
        internal const string CHECK_USEREXIST_API = BASE_URL + "/Presenters/doesExist";
        internal const string LOGIN_API = BASE_URL + "/Presenters/login";
        internal const string MATCH_FLITER = "/Matches/findOne?filter=";
        internal const string ACC_TOKEN = "&access_token=";
        internal const string ACC_TOKEN_PUT = "?access_token=";
        internal const string PRESENTER = "/Presenters/";
        internal const string PREZ_FILTER = "/presentations?filter=";

        #region FileExtentions

        public const string ASSETBUNDLE = ".assetbundle";
        public const string UNITYPACKAGE = ".unitypackage";
        public const string GLTF = ".gltf";
        public const string GLB = ".glb";
        public const string ANDROID_BUNDLE_EXTENTION = "-android.assetbundle";
        public const string UWP_BUNDLE_EXTENTION = "-uwp.assetbundle";
        public const string LINUX_BUNDLE_EXTENSION = "-linux.assetbundle";
        public const string OSX_BUNDLE_EXTENSION = "-osx.assetbundle";

        #endregion
    }
}
