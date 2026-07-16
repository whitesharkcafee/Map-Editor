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
    [HarmonyPatch(typeof(MenuController), "ConfigureMenuForPause")]
    public class PausePatchButtons
    {
        public static void Postfix(MenuController __instance)
        {
            if(ModMain.isInEditor)
            {
                __instance.m_lastCheckpointButton.SetActive(false);
                __instance.m_restartLevelButton.SetActive(false);
                ModMain.editorButton.SetActive(false);
                EditorUIController.playtestButton.SetActive(true);
                __instance.levelToResumeLabel.text = "Map Editor";
                __instance.pausePlayerStats.SetActive(false);
                __instance.m_lastCheckpointButton.transform.parent.GetComponent<UITable>().Reposition();
                __instance.m_lastCheckpointButton.transform.parent.GetComponent<UITable>().repositionNow = true;
            }
            if(ModMain.isInPlaytest)
            {
                EditorUIController.playtestButton.SetActive(false);
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
