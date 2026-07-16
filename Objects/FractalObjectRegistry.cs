using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MapEditor.Objects
{
    public static class FractalObjectRegistry
    {
        public class RegisteredObject
        {
            public FractalObject Definition;
            public AssetBundle bundle;
            public string jsonPath;
        }
        public static Dictionary<string, RegisteredObject> Objects = new Dictionary<string, RegisteredObject>();
        public static void InitializeRegistry()
        {
            Objects.Clear();
            string[] jsonFiles = Directory.GetFiles(NativeModLoader.GetModAssetsFolder(), "*.json", SearchOption.AllDirectories);
            foreach(string jsonPath in jsonFiles)
            {
                string directory = Path.GetDirectoryName(jsonPath);
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(jsonPath);

                string bundlePath = Path.Combine(directory, fileNameWithoutExt);

                if (!File.Exists(bundlePath))
                {
                    Debug.LogError($"[MapEditor] Found JSON definition '{fileNameWithoutExt}.json' but missing its asset bundle file at: {bundlePath}");
                    continue;
                }

                AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
                if(bundle == null)
                {
                    Debug.LogError($"[MapEditor] Failed to load Asset Bundle at: {bundlePath}");
                    continue;
                }

                string jsonText = File.ReadAllText(jsonPath);
                FractalObject def = Newtonsoft.Json.JsonConvert.DeserializeObject<FractalObject>(jsonText);

                if (def == null || string.IsNullOrEmpty(def.name))
                {
                    Debug.LogError($"[MapEditor] Invalid JSON content in: {jsonPath}");
                    bundle.Unload(true);
                    continue;
                }

                Objects[def.name] = new RegisteredObject
                {
                    Definition = def,
                    bundle = bundle,
                    jsonPath = jsonPath
                };

                Debug.Log($"[MapEditor] Successfully registered mod object: {def.name} [{def.type}]");
            }
        }
    }
}
