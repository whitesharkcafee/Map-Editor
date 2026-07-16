// =============================================================================================
//  Fractal Space - mod sample (ModMain)
//
//  SETUP (once): copy this whole folder OUT of the game directory (so Steam updates don't wipe
//  it), then set the path to your game's "Managed" folder in ONE place -> Directory.Build.props
//  (NOT here in C# - reference paths can't live in source). After that, Build (Ctrl+Shift+B) and
//  the DLL drops straight into your Mods folder. Full steps in README.txt.
// =============================================================================================
using System.IO;
using System.Collections.Generic;  // Dictionary
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;       // UnityWebRequest, UnityWebRequestMultimedia, UnityWebRequestTexture
using UnityEngine.SceneManagement;  // SceneManager.sceneLoaded, Scene, LoadSceneMode
using Newtonsoft.Json;              // JsonConvert (full JSON: dictionaries, nesting, etc.)
using Debug = UnityEngine.Debug;
using MapEditor.UI; // pin Debug to Unity's, avoids clash with System.Diagnostics.Debug

// =============================================================================================
//  RELOAD-SAFE MODDING - READ THIS (it will save you a very confusing bug).
//
//  Your mod can be RELOADED at runtime: hot-reload (Ctrl+M) OR a player disabling then re-enabling
//  it. Each reload loads a SECOND copy of your assembly (Mono can't unload the first), so your types
//  now exist twice and Unity CAN'T tell the reloaded ones from the old ones. Anything tied to a
//  MonoBehaviour YOU define then breaks: AddComponent<YourMonoBehaviour>() returns null
//  (NullReference) or attaches the OLD type and runs OLD code, and Unity keeps calling
//  Awake/Update/OnEnable on the OLD instance. This bites on ANY reload, not just hot-reload.
//
//  => DON'T define your own MonoBehaviours. Use these game-side tools instead - all run YOUR latest
//     code after a reload. Set them up in OnModLoaded, tear them down in OnModUnloaded:
//
//     Awake / Start / OnEnable    ->  OnModLoaded
//     OnDestroy (your cleanup)    ->  OnModUnloaded
//     Update (per frame)          ->  NativeModLoader.Instance.RegisterUpdate(cb)  / UnregisterUpdate(cb)
//     Coroutines                  ->  NativeModLoader.Instance.RunModCoroutine(routine) / StopModCoroutine
//     Scene loads                 ->  SceneManager.sceneLoaded += handler   (static C# event, no MB)
//     A specific object's events  ->  ModObjectEvents.Get(obj).Destroyed / CollisionEntered / ...
//     "Is this object destroyed?" ->  just poll:  if (obj == null) { it's gone }   (Unity fake-null)
//
//  (For players, mods load ONCE at startup, so your own MonoBehaviours seem to "work" - until a
//   player toggles your mod off and on. Use the tools and you're safe in every case.)
//
//  Examples of every tool are below: RegisterUpdate (the Right-Shift+F9 / Backspace hotkeys), RunModCoroutine
//  (the web image/audio), the static sceneLoaded event, and ModObjectEvents (on the canvas).
// =============================================================================================
namespace DemoMod
{
    public class ModMain
    {
        // Mod metadata - REQUIRED. Shown in the in-game Mod Browser.
        public const string ModName = "Map Editor";
        public const string Author = "whitesharkcafe";
        public const string Version = "1.0.0";
        public const string Description = "";

        // This mod is fully reload-safe: it does all its teardown in OnModUnloaded and never relies on
        // its own MonoBehaviour types surviving a reload (it uses the loader's RegisterUpdate /
        // RunModCoroutine / ModObjectEvents services instead). So it opts into live disable/enable/update.
        // Omit this (or set false) if YOUR mod isn't reload-safe — the host then defers such changes to
        // the next game restart.
        public const bool SupportsHotReload = false;

        private static GameObject originalButton;

        public static void OnModLoaded()
        {
            Debug.Log("[MapEditor] OnModLoaded called.");
            SceneManager.sceneLoaded += OnSceneWasLoaded;
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
                originalButton = GameObject.Find("MainMenu/Camera/Holder/Main/LargeButtons/6_LevelEditor");
                NGUIModHelper.Setup(originalButton);
                CreateEditorButton();
            }
        }
        static void CreateEditorButton()
        {
            GameObject button = NGUIModHelper.CreateButton(originalButton.transform.parent, "Map Editor", new Vector3(0,0,0)).gameObject;
            button.SetActive(false);
            button.name = "6_MapEditor";
            GameObject.Destroy(originalButton);
            ButtonController.Destroy(button.GetComponent<ButtonController>());
            NGUIModHelper.InsertBefore(button, "8_ExitGame");
            button.transform.parent.GetComponent<UITable>().Reposition();
            button.transform.parent.GetComponent<UITable>().repositionNow = true;

            button.SetActive(true);

        }
    }
}
