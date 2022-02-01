using AfterNow.PrezSDK.Internal.Views;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AfterNow.PrezSDK.Internal.Helpers
{
    public static class PrezWebCalls
    {
        /// <summary>
        /// Authentication request with ANPrez remote server. Callback should not be null and shouldn't throw
        /// </summary>
        /// <param name="OnAuthenticate"></param>
        /// <returns></returns>
        public static async Task OnAuthenticationRequest(Action<bool> OnAuthenticate)
        {
            try
            {
                var prezLogin = new LoginPost
                {
                    email = SDKConstants.GuestEmail,
                    password = SDKConstants.GuestPassword
                };
                string responseJson = await PrezAPIHelper.Post(SDKConstants.LOGIN_API, PrezSerializer.SerializeObject(prezLogin));
                if (!string.IsNullOrEmpty(responseJson))
                {
                    LoginResponse response = PrezSerializer.DeserializeObject<LoginResponse>(responseJson);
                    if (response != null)
                    {
                        InternalStates.AccessToken = response.id;
                        InternalStates.UserID = response.userId;
                        OnAuthenticate(true);
                    }
                }
            }
            catch (Exception e)
            {
                PrezDebugger.Exception(e);
                OnAuthenticate(false);
            }
        }

        /// <summary>
        /// Join a presentation with its ID. Returns a View on success which has the data needed for the presentation.
        /// </summary>
        /// <param name="shortId"></param>
        /// <param name="OnPresentation"></param>
        /// <returns></returns>
        public static async Task JoinPresentation(string shortId, Action<Presentation> OnPresentation)
        {
            try
            {
                MatchFilter filter = new MatchFilter();
                filter.where.shortId = shortId;
                string currentFilterString = Uri.EscapeUriString(PrezSerializer.Serialize(filter));
                string url = SDKConstants.BASE_URL + SDKConstants.MATCH_FLITER + currentFilterString + SDKConstants.ACC_TOKEN + InternalStates.AccessToken;
                string responseJson = await PrezAPIHelper.Get(url);
                if (!string.IsNullOrEmpty(responseJson))
                {
                    MatchIdResponse response = PrezSerializer.DeserializeObject<MatchIdResponse>(responseJson);
                    int presentationID = int.Parse(response.presentationId);
                    int presenterID = int.Parse(response.presenterId);
                    var currentFilter = new PresentationFilter(presentationID, new string[] { "locations", "match" });
                    string prezJsonUrl = SDKConstants.BASE_URL + SDKConstants.PRESENTER + presenterID + SDKConstants.PREZ_FILTER + Uri.EscapeUriString(PrezSerializer.Serialize(currentFilter)) + SDKConstants.ACC_TOKEN + InternalStates.AccessToken;
                    var prezJson = await PrezAPIHelper.Get(prezJsonUrl);
                    if (!string.IsNullOrEmpty(prezJson))
                    {
                        prezJson = "{\"presentations\":" + prezJson + "}";
                        PresentationList prez = PrezSerializer.DeserializeObject<PresentationList>(prezJson);
                        OnPresentation(prez.presentations[0]);
                    }
                }
            }
            catch (Exception e)
            {
                PrezDebugger.Exception(e);
                OnPresentation(null);
            }
        }

        public static Task DownloadAsset(this ARPAsset asset, string replacement)
        {
            string downloadPath = asset.AbsoluteDownloadPath(InternalStates.AssetPath);
            return PrezAPIHelper.Download(asset.Url(replacement), downloadPath);
        }
    }
}