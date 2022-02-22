using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AfterNow.PrezSDK.Editor
{
    static class PrezSDKConfigureRenderPipeline
    {
        [MenuItem("AnPrez SDK/RenderPipeline/Configure SDK for BuiltIn")]
        static void ConvertToBuiltIn()
        {
            Convert(true);
        }

        [MenuItem("AnPrez SDK/RenderPipeline/Configure SDK for URP or HDRP")]
        static void ConvertToSRP()
        {
            Convert(false);
        }

        static void Convert(bool defaultPipeline)
        {
            var guid = AssetDatabase.FindAssets("t:AfterNow.PrezSDK.Editor.PrezShaderContainer")[0];
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<PrezShaderContainer>(path);

            List<Shader> builtInShader = asset.GetBuiltInShaders(out bool validBuiltIn);
            List<Shader> srpShader = asset.GetSRPShaders(out bool validSRP);

            var graphicsSettingsObj = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
            var serializedObject = new SerializedObject(graphicsSettingsObj);
            SerializedProperty shadersArray = serializedObject.FindProperty("m_AlwaysIncludedShaders");

            if(defaultPipeline)
            {
                RemoveShaders(shadersArray, validSRP ? srpShader : null);
                InsertShader(shadersArray, builtInShader);
            }
            else
            {
                if(validSRP)
                {
                    RemoveShaders(shadersArray, builtInShader);
                    InsertShader(shadersArray, validSRP ? srpShader : null);
                }
                else
                {
                    Debug.LogError("Failed to configure project for SRP: SRP shaders invalid or SRP package missing.\nPlease try again after importing a valid SRP package");
                    return;
                }
            }

            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.Log($"Configured Prez SDK for {(defaultPipeline ? "Built-In Pipeline" : "URP/HDRP")}");
        }

        static void RemoveShaders(SerializedProperty array, List<Shader> shadersToRemove)
        {
            for(int i=0;i<array.arraySize;i++)
            {
                var element = array.GetArrayElementAtIndex(i);
                var reference = element.objectReferenceValue;
                var currentShader = reference != null ? reference as Shader : null;
                bool doesListHaveShader = shadersToRemove != null && shadersToRemove.Contains(currentShader);
                if(currentShader == null || doesListHaveShader)
                {
                    array.DeleteArrayElementAtIndex(i);
                    i--;
                }
            }
        }

        static void InsertShader(SerializedProperty array, List<Shader> shadersToAdd)
        {
            var newShaders = new List<Shader>(shadersToAdd);
            for(int i=0;i<array.arraySize;i++)
            {
                var element = array.GetArrayElementAtIndex(i);
                var currentShader = element.objectReferenceValue != null ? element.objectReferenceValue as Shader : null;
                if(currentShader != null && newShaders.Contains(currentShader))
                {
                    newShaders.Remove(currentShader);
                }
            }

            for(int i=0;i< newShaders.Count;i++)
            {
                int newIndex = array.arraySize;
                array.InsertArrayElementAtIndex(newIndex);
                var newElement = array.GetArrayElementAtIndex(newIndex);
                newElement.objectReferenceValue = newShaders[i];
            }
        }
    }
}
