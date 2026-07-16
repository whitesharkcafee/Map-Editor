using DemoMod;
using MapEditor.Editor;
using MapEditor.UI; 
using Newtonsoft.Json;              
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;      
using UnityEngine.SceneManagement; 
using UnityEngine.UI;
using Debug = UnityEngine.Debug;


namespace MapEditor
{
    public class ModMain
    {

        public const string ModName = "Map Editor";
        public const string Author = "whitesharkcafe";
        public const string Version = "1.0.0";
        public const string Description = "";
        public static bool isInEditor = false;

        //Hot reload cannot be supported in this economy - we'll do this, just so we won't have any headaches later.
        public const bool SupportsHotReload = false;

        private static GameObject originalButton;

        public static void OnModLoaded()
        {
            Debug.Log("[MapEditor] OnModLoaded called.");
            SceneManager.sceneLoaded += OnSceneWasLoaded;
            // When enabling the mod, there's a chance we are already in the menu, so we check if that is true and create the button.
            if(SceneManager.GetActiveScene().buildIndex == 1)
            {
                CreateEditorButton();
            }
        }
        public static void OnModUnloaded() 
        {
            Debug.Log("[MapEditor] OnModUnloaded called.");
            SceneManager.sceneLoaded -= OnSceneWasLoaded;
        }

        public static void OnSceneWasLoaded(Scene scene, LoadSceneMode lsm)
        {
            if(scene.buildIndex == 1)
            {
                // We need that to have a reference for how other buttons look. Setup() can also have tons of other stuff, which will be added later.
                originalButton = GameObject.Find("MainMenu/Camera/Holder/Main/LargeButtons/6_LevelEditor");
                NGUIModHelper.Setup(originalButton,null,null,GameObject.Find("MainMenu/Camera/Holder/Options"));
                CreateEditorButton();
            }
            else if(scene.buildIndex == 8)
            {
                EditorController.OnSceneEnter();
            }
        }
        static void CreateEditorButton()
        {
            // Simple as that - we create a button, parent it, rename for the correct order, destroy left-overs and reposition, so then we can manipulate to our heart's desire.
            GameObject button = NGUIModHelper.CreateButton(originalButton.transform.parent, "Map Editor", new Vector3(0,0,0)).gameObject;
            button.SetActive(false);
            button.name = "6_MapEditor";
            GameObject.Destroy(originalButton);
            ButtonController.Destroy(button.GetComponent<ButtonController>());
            NGUIModHelper.InsertBefore(button, "8_ExitGame");
            button.transform.parent.GetComponent<UITable>().Reposition();
            button.transform.parent.GetComponent<UITable>().repositionNow = true;
            button.SetActive(true);
            UIButtonPatcher buttonPatcher = button.GetComponent<UIButtonPatcher>();
            buttonPatcher.onClick += OnEditorOpenButtonClicked;
            CreateEditorPanel();
        }
        static void CreateEditorPanel()
        {
            //for now, just for the sake of time economy, we won't have any UI - it's better to have a working prototype and build upon it, not do stuff we don't need
            //and then rebuild it every damn time.
        }
        static void OnEditorOpenButtonClicked()
        {
            // for now - opens the modder scene. Scene index = 8.
            bool ok = ModdingTools.LoadModdingScene();
            Debug.Log("[MapEditor] LoadModdingScene returned " + ok);
            GameObject editor = new GameObject("Editor");
            editor.AddComponent<EditorController>();
        }
    }
}
