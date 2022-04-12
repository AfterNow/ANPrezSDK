///#define USE_NEWTONSOFT

#if USE_NEWTONSOFT
using Newtonsoft.Json;
#endif
using AfterNow.PrezSDK.Internal.Helpers;
using UnityEngine;

namespace AfterNow.PrezSDK
{
    /// <summary>
    /// Class which handles the initialization of the Prez SDK.
    /// </summary>
    internal class InitializeSDK
    {
        public static string DownloadFolderPath { get; private set; }
        /// <summary>
        /// Method to initialize the SDK requirements. Custom logger and custom json serializations can be used depending on what is used in the original Unity project.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void InitSDK()
        {
#if USE_NEWTONSOFT
            PrezSerializer.Initialize(JsonConvert.SerializeObject, JsonConvert.DeserializeObject);
#else
            PrezSerializer.Initialize(JsonUtility.ToJson, JsonUtility.FromJson);
#endif
            PrezDebugger.Initialize(Debug.Log, Debug.LogWarning, Debug.LogError, Debug.LogException);

            string downloadPath = Application.persistentDataPath;

            DownloadFolderPath = downloadPath;
            InternalStates.SetAssetDownloadPath(downloadPath);
        }
    }
}