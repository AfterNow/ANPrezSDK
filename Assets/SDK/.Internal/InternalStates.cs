namespace AfterNow.AnPrez.SDK.Internal
{
    public static class InternalStates
    {
        internal static int UserID;
        internal static string AccessToken;
        internal static string AssetPath;

        public static void Reset()
        {
            UserID = 0;
            AccessToken = null;
        }
        public static void SetAccessToken(string token)
        {
            AccessToken = token;
        }

        public static void SetAssetDownloadPath(string path)
        {
            AssetPath = path;
        }
    }
}
