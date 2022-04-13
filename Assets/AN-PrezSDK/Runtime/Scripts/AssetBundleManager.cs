using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace AfterNow.PrezSDK
{
    /// <summary>
    /// Class to handle loading and unloading of asset bundles present in a presentation
    /// </summary>
    public class AssetBundleManager : MonoBehaviour
    {
        internal static AssetBundleManager Instance
        {
            get
            {

                if (_instance == null)
                {
                    _instance = CoroutineRunner.Instance.gameObject.AddComponent<AssetBundleManager>();
                }

                return _instance;
            }
        }

        private static AssetBundleManager _instance;

        // A collection of all the assetbundles that are currently loaded
        private static Dictionary<string, AssetBundleContainer> _bundles = new Dictionary<string, AssetBundleContainer>();

        /// <summary>
        /// This method handles logic to properly unload all the asset bundles
        /// </summary>
        public static void Cleanup()
        {
            foreach (var item in _bundles)
            {
                if (item.Value.Bundle)
                {
                    item.Value.Bundle.Unload(true);
                }
            }

            _bundles.Clear();
        }

        /// <summary>
        /// Loads an asset bundle which is present in <paramref name="path"/> The action <paramref name="OnLoaded"/> returns the instantiated gameobject
        /// of the asset bundle
        /// </summary>
        /// <param name="path"></param>
        /// <param name="OnLoaded"></param>
        /// <returns></returns>
        public static IEnumerator LoadAssetBundle(string path, Action<GameObject> OnLoaded)
        {
            if (_bundles.TryGetValue(path, out AssetBundleContainer bundle))
            {
                OnLoaded?.Invoke(bundle.GameObject);
            }
            else
            {
                string localPath = UriBuilderExtension.UriPath(path);
                UnityWebRequest unityWebRequest = UnityWebRequestAssetBundle.GetAssetBundle(localPath);
                yield return unityWebRequest.SendWebRequest();
                if (string.IsNullOrEmpty(unityWebRequest.error))
                {
                    if (unityWebRequest.isDone)
                    {
                        var assetBundle = DownloadHandlerAssetBundle.GetContent(unityWebRequest);
                        var assetObject = (GameObject)assetBundle.LoadAsset(assetBundle.GetAllAssetNames()[0]);
                        var container = new AssetBundleContainer(assetBundle, Instantiate(assetObject));
                        _bundles.Add(path, container);
                        OnLoaded?.Invoke(container.GameObject);
                    }
                }
                else
                {
                    Debug.LogError("Assetbundleerror : " + unityWebRequest.error);
                    OnLoaded?.Invoke(null);
                }
            }
        }

        /// <summary>
        /// An asset bundle container
        /// </summary>
        private struct AssetBundleContainer
        {
            public AssetBundle Bundle;
            public GameObject GameObject;

            /// <summary>
            /// An asset bundle container which holds references to the asset bundle <paramref name="bundle"/> and the gameobject <paramref name="go"/>
            /// instantiated from it
            /// </summary>
            /// <param name="bundle"></param>
            /// <param name="go"></param>
            public AssetBundleContainer(AssetBundle bundle, GameObject go)
            {
                Bundle = bundle;
                GameObject = go;
            }
        }
    }
}