using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEditor.PackageManager;
using System.Linq;
using System.Threading;
using System.Text;
using System;

namespace AfterNow.AnPrez.SDK.Unity
{
    [InitializeOnLoad]
    public class PostPackageImport
    {
        static PostPackageImport()
        {
            AssetDatabase.importPackageStarted += OnImportPackageStarted;
            AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
            AssetDatabase.importPackageFailed += OnImportPackageFailed;
            AssetDatabase.importPackageCancelled += OnImportPackageCancelled;
        }

        private static void OnImportPackageCancelled(string packageName)
        {
            Debug.Log($"Cancelled the import of package: {packageName}");
        }

        private static void OnImportPackageCompleted(string packagename)
        {
            Debug.Log($"Imported package: {packagename}");

            //Check if TextMeshPro installed or TextMeshPro/DistanceField shader is present
            Shader tmp_distance_field_shader = Shader.Find("TextMeshPro/Distance Field");
            if (tmp_distance_field_shader == null)
            {
                Debug.LogError("Shader 'TextMeshPro/Distance Field' not found. Please import 'TextMeshPro->Essential Resources' from PackageManager.");
                return;
            }
            else
            {
                var allMaterials = Resources.LoadAll<Material>("Tmp_Fonts/");
                foreach (var mat in allMaterials)
                {
                    mat.shader = tmp_distance_field_shader;
                }
            }
        }

        private static void OnImportPackageFailed(string packagename, string errormessage)
        {
            Debug.Log($"Failed importing package: {packagename} with error: {errormessage}");
        }

        private static void OnImportPackageStarted(string packagename)
        {
            Debug.Log($"Started importing package: {packagename}");
        }
    }
}