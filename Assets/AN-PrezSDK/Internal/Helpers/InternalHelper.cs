
using System;
using System.Threading.Tasks;

    internal static class InternalHelper
    {
        internal static string GetURL(string relativePath, string accessToken)
        {
            if (accessToken == null) accessToken = InternalStates.AccessToken;
            return string.IsNullOrEmpty(relativePath) ? string.Empty : $"{SDKConstants.BASE_URL}/containers/{relativePath}?access_token={accessToken}";
        }
    }

