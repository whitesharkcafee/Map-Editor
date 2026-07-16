using HarmonyLib;

namespace MapEditor.Editor.Patches
{
    [HarmonyPatch(typeof(Localization), "Get")]
    public class LocalizationPatches
    {
        public static bool Prefix(ref string __result, string key)
        {
            if(ModMain.isInEditor)
            {
                if(key == "Chapter6")
                {
                    __result = "Map Editor";
                    return false;
                }
            }
            return true;
        }
    }
}
