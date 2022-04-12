using GLTFast;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace AfterNow.PrezSDK
{
    /// <summary>
    /// Class to load a GLTF model and render it to a Unity GameObject.
    /// </summary>
    internal static class GLBLoader
    {
        static readonly IDeferAgent _deferAgent;
        static readonly List<GltfImport> gltfImports = new List<GltfImport>();

        static GLBLoader()
        {
            _deferAgent = CoroutineRunner.Instance.gameObject.AddComponent<TimeBudgetPerFrameDeferAgent>();
        }

        /// <summary>
        /// Load GLTF model from a file at <paramref name="path"/> and child it to gameobject <paramref name="parent"/>.
        /// </summary>
        /// <param name="path"> GLTF model file path </param>
        /// <param name="parent"> Gameobject to which the now loaded GLTF model is childed to </param>
        /// <returns></returns>
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

        /// <summary>
        /// Disposing GLTF model after it is no longer needed.
        /// </summary>
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