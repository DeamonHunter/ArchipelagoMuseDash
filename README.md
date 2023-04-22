# ArchipelagoMuseDash
This is a mod for [Muse Dash](https://store.steampowered.com/app/774171/Muse_Dash/) that integrates it into [Archipelago.gg](https://archipelago.gg/).

## What it Does
This mod creates a randomiser and does the following:

- You will be given a number of starting songs. The number of which depends on your settings.
- Completing any song will give you 1 or 2 rewards.
- The rewards for completing songs will range from songs to traps and **Music Sheets**.

The goal of this randomiser is to collect a number of **Music Sheets**. Once you've collected enough Music Sheets, the goal song will be unlocked. Completing the goal song will complete your seed.

## What is Required

Only the base Muse Dash game is required in order to play this game.

However, the **Just as Planned DLC** is recommended as the number of possible songs increases from 60+ to 400+ songs, which adds to the variety and increases replayability.

# Installing this Mod

Check the [latest release](https://github.com/DeamonHunter/ArchipelagoMuseDash/releases/latest) for instructions on installing this mod.

# Building this Mod

1. First install [Melon Loader v0.6.1](https://github.com/LavaGang/MelonLoader/releases/tag/v0.6.1) and run the game.
2. Download/Clone this repository. Then open up ArchipelagoMuseDash.sln
3. You will need to import the following dlls from the Muse Dash folder:
  - MuseDash/MelonLoader/net6
    - 0Harmony.dll
    - Il2CppInterop.Common.dll
    - Il2CppInterop.Generator.dll
    - Il2CppInterop.Runtime.dll
    - MelonLoader.dll
  - MuseDash/MelonLoader/Il2CppAssemblies
    - Assembly-CSharp.dll
    - Assembly-CSharp-firstpass.dll
    - Il2CppDOTween.dll
    - Il2CppDOTweenPro.dll
    - Il2Cppmscorlib.dll
    - Il2CppPeroTools2.dll
    - Il2CppSirenix.Serialization.dll
    - UnityEngine.AudioModule.dll
    - UnityEngine.CoreModule.dll
    - UnityEngine.ImageConversionModule.dll
    - UnityEngine.IMGUIModule.dll
    - UnityEngine.InputLegacyModule.dll
    - UnityEngine.TextRenderingModule.dll
    - UnityEngine.UI.dll
    - UnityEngine.UIModule.dll
4. You should now be able to build the project. Once you've built the project, you will need to move the `ArchipelagoMuseDash.dll` file that was generated in `/bin/` into the MuseDash folder for it to work.
