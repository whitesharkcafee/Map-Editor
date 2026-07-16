using HarmonyLib;
using System;

namespace MapEditor.Editor.Patches
{
    [HarmonyPatch(typeof(Controls), "Start")]
    public class InitPatch
    {
        public static void Postfix()
        {
            if(ModMain.isInEditor)
            {
                MenuController.ShowCursor(true);
            }
        }
    }
    [HarmonyPatch(typeof(Controls), nameof(Controls.SetPause), new Type[] { typeof(bool) })]
    public class PausePatch
    {
        public static void Postfix()
        {
            if (ModMain.isInEditor)
            {
                MenuController.ShowCursor(true);
            }
        }
    }
    [HarmonyPatch(typeof(InGameUIManager), nameof(InGameUIManager.OnResumeGame))]
    public class PausePatchINGUI
    {
        public static bool Prefix()
        {
            if(ModMain.isInEditor)
            {
                return false;
            }
            return true;
        }
    }
}
