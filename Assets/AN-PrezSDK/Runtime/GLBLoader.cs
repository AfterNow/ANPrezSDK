using AfterNow.AnPrez.SDK.Unity;
using GLTFast;
using System;
using System.Threading.Tasks;
using UnityEngine;

public static class GLBLoader
{
    static readonly IDeferAgent _deferAgent;
    static GLBLoader()
    {
        _deferAgent = CoroutineRunner.Instance.gameObject.AddComponent<TimeBudgetPerFrameDeferAgent>();
    }
    public static async Task<GameObject> LoadGLTF(byte[] data, string path, Transform parent)
    {

        var gltf = new GltfImport(null, _deferAgent);

        var settings = new ImportSettings
        {
            generateMipMaps = true,
            anisotropicFilterLevel = 3,
            nodeNameMethod = ImportSettings.NameImportMethod.OriginalUnique
        };

        var success = await gltf.LoadGltfBinary(data, new Uri(path), settings);
        return success && gltf.InstantiateMainScene(parent) ? parent.GetChild(0).gameObject : null;
    }
}
