using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class AssetBundleManager : MonoBehaviour
{
    public static AssetBundleManager Instance
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

    private static Dictionary<string, AssetBundleContainer> _bundles = new Dictionary<string, AssetBundleContainer>();

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
                    var instantiatedAsset = Instantiate(assetObject);
                    var container = new AssetBundleContainer(assetBundle, instantiatedAsset);
                    foreach(var collider in instantiatedAsset.GetComponentsInChildren<Collider>(true))
                    {
                        Destroy(collider);
                    }
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

    private struct AssetBundleContainer
    {
        public AssetBundle Bundle;
        public GameObject GameObject;

        public AssetBundleContainer(AssetBundle bundle, GameObject go)
        {
            Bundle = bundle;
            GameObject = go;
        }
    }
}



