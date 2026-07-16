using MapEditor.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MapEditor.Editor
{
    public class EditorUIController
    {
        public static GameObject playtestButton;
        public static void CreatePlaytestButton()
        {
            if(playtestButton == null) {
                NGUIModHelper.Setup(GameObject.Find("MainMenu/Camera/Holder/Main/LargeButtons/5_Options"));
                playtestButton = NGUIModHelper.CreateButton(ModMain.editorButton.transform.parent, "Playtest", new Vector3(0, 0, 0)).gameObject;
                playtestButton.gameObject.SetActive(false);
                playtestButton.gameObject.name = "1e_Playtest";
                NGUIModHelper.InsertBefore(playtestButton.gameObject, "5_Options");
                playtestButton.gameObject.transform.parent.GetComponent<UITable>().Reposition();
                playtestButton.gameObject.transform.parent.GetComponent<UITable>().repositionNow = true;
                ButtonController.Destroy(playtestButton.GetComponent<ButtonController>());
                playtestButton.gameObject.SetActive(true);
                UIButtonPatcher patcher = playtestButton.GetComponent<UIButtonPatcher>();
                patcher.onClick += EditorController.PlayFromSpawnPoint;
            }
            else
            {
                playtestButton.SetActive(true);
            }
        }
    }
}
