using GLTFast;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace AfterNow.PrezSDK
{
    internal static class GLBLoader
    {
        static readonly IDeferAgent _deferAgent;
        static readonly List<GltfImport> gltfImports = new List<GltfImport>();

        static GLBLoader()
        {
            _deferAgent = CoroutineRunner.Instance.gameObject.AddComponent<TimeBudgetPerFrameDeferAgent>();
        }

        public static async Task<GameObject> LoadGLTFFromURL(string path, Transform parent)
        {

            var gltf = new GltfImport(null, _deferAgent);

            var settings = new ImportSettings
            {
                generateMipMaps = true,
                anisotropicFilterLevel = 3,
                nodeNameMethod = ImportSettings.NameImportMethod.OriginalUnique
            };

            var success = await gltf.Load(path, settings);

            gltfImports.Add(gltf);
            return success && gltf.InstantiateMainScene(parent) ? parent.GetChild(0).gameObject : null;
        }

        public static void DisposeGltf()
        {
            foreach (var item in gltfImports)
            {
                item.Dispose();
            }
            gltfImports.Clear();

        }
    }
}