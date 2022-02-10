using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AfterNow.PrezSDK.Editor
{
    public class PrezShaderContainer : ScriptableObject
    {
        [SerializeField] TextAsset builtInShadersList;
        [SerializeField] TextAsset srpShadersList;

        public List<Shader> GetBuiltInShaders(out bool validShader)
        {
            if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(builtInShadersList, out string guid, out long localID))
            {
                throw new System.Exception("Unable to locate Built In shader files list");
            }


            return ObjectToShader(GetObjectsFromPath(guid, builtInShadersList.text), out validShader);
        }

        public List<Shader> GetSRPShaders(out bool validShader)
        {
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(srpShadersList, out string guid, out long localID))
            {
                throw new System.Exception("Unable to locate SRP shader files list");
            }


            return ObjectToShader(GetObjectsFromPath(guid, srpShadersList.text), out validShader);
        }

        private Object[] GetObjectsFromPath(string guid, string shadersList)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            path = path.Substring(0, path.LastIndexOf('/'));
            string[] shaders = shadersList.Split(',');
            for (int i = 0; i < shaders.Length; i++)
            {
                shaders[i] = path + "/" + shaders[i];
            }
            Object[] objects = new Object[shaders.Length];
            for (int i = 0; i < objects.Length; i++)
            {
                objects[i] = AssetDatabase.LoadAssetAtPath<Object>(shaders[i]);
            }
            return objects;
        }

        private List<Shader> ObjectToShader(Object[] arr, out bool validShader)
        {
            validShader = true;
            List<Shader> shaders = new List<Shader>(arr.Length);
            for (int i = 0; i < arr.Length; i++)
            {
                var shader = arr[i] as Shader;
                if (shader == null)
                {
                    validShader = false;
                }
                else
                {
                    shaders.Add(shader);
                }
            }
            return shaders;
        }
    }
}
