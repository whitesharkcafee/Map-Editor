using System;
using System.IO;
using System.Reflection;
using System.Globalization;
using UnityEngine;
using Newtonsoft.Json;

namespace MapEditor.Objects
{
    public class FractalObjectParser : MonoBehaviour
    {
        public GameObject LoadAndBuildObject(string jsonPath, AssetBundle bundle)
        {
            string jsonText = File.ReadAllText(jsonPath);
            FractalObject def = JsonConvert.DeserializeObject<FractalObject>(jsonText);
            if(def == null)
            {
                Debug.LogError($"[MapEditor] Failed to parse JSON at '{jsonPath}'");
                return null;
            }

            GameObject prefab = bundle.LoadAsset<GameObject>(def.name);
            if(prefab == null)
            {
                Debug.LogError($"[MapEditor] Prefab '{def.name}' not found in Asset Bundle!");
                return null;
            }

            bool wasPrefabActive = prefab.activeSelf;
            prefab.SetActive(false);

            GameObject instance = Instantiate(prefab);

            ApplyTransforms(instance, def);

            if(def.type == "DYNAMIC" && !string.IsNullOrEmpty(def.controller))
            {
                Type controllerType = FindTypeInAssemblies(def.controller);
                if(controllerType != null )
                {
                    Component component = instance.AddComponent(controllerType);
                    if(def.arguments != null)
                    {
                        foreach(var arg in def.arguments)
                        {
                            AssignValueToComponent(component, controllerType, arg.Key, arg.Value, bundle);
                        }
                    }
                }
                else
                {
                    Debug.LogError($"[MapEditor] Controller class '{def.controller}' was not found in the assembly!");
                }
            }

            prefab.SetActive(wasPrefabActive);
            return instance;    
        }

        private void ApplyTransforms(GameObject go, FractalObject def)
        {
            if (def.defaultScale != null && def.defaultScale.Length == 3)
                go.transform.localScale = new Vector3(def.defaultScale[0], def.defaultScale[1], def.defaultScale[2]);

            if (def.defaultRotation != null && def.defaultRotation.Length == 3)
                go.transform.localRotation = Quaternion.Euler(def.defaultRotation[0], def.defaultRotation[1], def.defaultRotation[2]);
        }

        private Type FindTypeInAssemblies(string typeName)
        {
            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = assembly.GetType(typeName);
                if (t != null) return t;
            }
            return null;
        }

        private void AssignValueToComponent(Component comp, Type type, string fieldName, string stringValue, AssetBundle bundle)
        {
            FieldInfo field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if(field != null)
            {
                object convertedValue = ConvertValue(stringValue, field.FieldType, bundle);
                field.SetValue(comp, convertedValue);
                return;
            }

            PropertyInfo prop = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if(prop!=null && prop.CanWrite)
            {
                object convertedValue = ConvertValue(stringValue, prop.PropertyType, bundle);
                prop.SetValue(comp, convertedValue, null);
            }
        }

        private object ConvertValue(string value, Type targetType, AssetBundle bundle)
        {
            if(typeof(UnityEngine.Object).IsAssignableFrom(targetType))
            {
                if(bundle == null)
                {
                    Debug.LogError($"[MapEditor] Cannot load asset resource because the AB is null!");
                    return null;
                }

                UnityEngine.Object loadedAsset = bundle.LoadAsset(value, targetType);

                if(loadedAsset != null)
                {
                    return loadedAsset;
                }

                Debug.LogWarning($"Resource '{value}' of type '{targetType.Name}' not found in Asset Bundle.");
                return null;
            }

            if (targetType == typeof(string)) return value;
            if (targetType == typeof(bool)) return bool.Parse(value);
            if (targetType == typeof(int)) return int.Parse(value);
            if (targetType == typeof(float)) return float.Parse(value);

            if(targetType == typeof(Vector3))
            {
                string clean = value.Trim('[', ']');
                string[] split = clean.Split(',');
                if(split.Length == 3)
                {
                    return new Vector3(
                        float.Parse(split[0], CultureInfo.InvariantCulture),
                        float.Parse(split[1], CultureInfo.InvariantCulture),
                        float.Parse(split[2], CultureInfo.InvariantCulture)
                    );
                }
            }

            if(targetType.IsEnum)
            {
                return Enum.Parse(targetType, value, true);
            }

            return Convert.ChangeType(value, targetType);
        }
    }
}
