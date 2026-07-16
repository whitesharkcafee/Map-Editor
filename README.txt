===============================================================================
 Fractal Space - Mod Sample (ready to build)
===============================================================================

A complete, working example mod. Build it, get a DLL in your Mods folder, done.

-------------------------------------------------------------------------------
 SETUP (3 steps - do once)
-------------------------------------------------------------------------------
  1. COPY THIS WHOLE "ModSample" FOLDER OUT of the game directory, somewhere you
     own - e.g. Documents\FractalMods\ModSample.
       Why: Steam can DELETE files in the game folder ("Verify integrity of game
       files"), branch switches re-download, and you can't build inside Program
       Files. Working on a copy outside keeps your mod safe across updates.

  2. OPEN "Directory.Build.props" AND SET ONE LINE - the path to your game's
     Managed folder:
         <GameManaged>...\Fractal Space\Fractal_Space_Data\Managed</GameManaged>
       The default is the standard Steam location - if that's where your game is,
       you don't need to change anything. To find it: in Steam, right-click
       Fractal Space > Manage > Browse local files, then go into
       Fractal_Space_Data\Managed and copy that path.

  3. OPEN ModSample.csproj IN VISUAL STUDIO AND BUILD (Ctrl+Shift+B).
     That single line is the ONLY thing to configure - all ~80 references resolve
     from it, and the build drops ModSample.dll STRAIGHT into your Mods folder:
       %USERPROFILE%\AppData\LocalLow\Haze Games\Fractal Space MOD\Mods\
     (No .NET SDK? Install Visual Studio with ".NET desktop development", or run
      "dotnet build" from a terminal in this folder.)

  Then launch the game - the mod appears in the Mod Browser (Ctrl+B). Activate it.
  Rebuild any time and it overwrites the DLL in Mods (use the Browser's hot-reload
  / Ctrl+M in-game to pick up changes without restarting).

-------------------------------------------------------------------------------
 WHAT IT DEMONSTRATES
-------------------------------------------------------------------------------
  - "Hello World!" text + an optional logo image on screen.
  - Audio: .wav parsed by hand, and .mp3/.ogg via UnityWebRequestMultimedia.
  - Image via UnityWebRequestTexture.
  - AudioClip / Texture / FBX model from an AssetBundle (optional).
  - Right Shift + F9 loads the clean modding scene, from a DontDestroyOnLoad
    component that also reacts to SceneManager.sceneLoaded (good-practice pattern).
  - OnModLoaded / OnModUnloaded lifecycle and proper teardown.

-------------------------------------------------------------------------------
 NOTES
-------------------------------------------------------------------------------
  - Rename the mod: edit ModName / Author / Version / Description at the top of
    ModMain.cs.
  - Custom assets (logo.png, music.mp3, sound.wav, modbundle) go in your mod's
    "_Assets" folder - the sample no-ops gracefully if they're missing.
  - The optional in-game log console toggles with F12.
  - To output beside the project instead of into Mods, set
    <OutputPath>bin\Debug\</OutputPath> in ModSample.csproj.
===============================================================================
