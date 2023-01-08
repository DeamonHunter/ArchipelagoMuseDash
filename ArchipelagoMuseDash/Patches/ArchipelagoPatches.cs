using System;
using Assets.Scripts.Database;
using Assets.Scripts.PeroTools.Managers;
using Assets.Scripts.PeroTools.Nice.Interface;
using Assets.Scripts.UI.Controls;
using Assets.Scripts.UI.Panels;
using HarmonyLib;
using Il2CppSystem.Collections.Generic;
using PeroTools2.Resources;
using UnityEngine;
using UnityEngine.UI;

// Due to how IL2CPP works, some things can't be invoked as an extension.
// ReSharper disable InvokeAsExtensionMethod

namespace ArchipelagoMuseDash.Patches {
    /// <summary>
    /// Various changes so that we can use the Unlock Song panel to show archipelago stuff.
    /// Also attempts to fix the rare bug where the texture itself is broken
    /// </summary>
    [HarmonyPatch(typeof(PnlUnlockStage), "UnlockNewSong")]
    sealed class PnlUnlockStagePatch {
        static void Postfix(PnlUnlockStage __instance, IData newSong) {
            if (!ArchipelagoStatic.LoggedInToGame)
                return;

            ArchipelagoStatic.ArchLogger.Log("PnlUnlockStage", "UnlockNewSong");

            //If this is present than it is an arch item
            if (!newSong.fields.ContainsKey("archName")) {
                GetTitleText(__instance.gameObject).text = "New Song!!";
                __instance.unlockText.text = "Go play it!";

                //Sometimes it looks like the sprite is null? Log And resupply it.
                if (__instance.unlockCover.sprite == null)
                    AttemptToFixBrokenAlbumImage(__instance);
                return;
            }

            GetTitleText(__instance.gameObject).text = "New Item!!";
            __instance.unlockText.text = "Hope it's good!";

            //Recreate the sprite here as for some reason it gets garbage collected
            var newSprite = Sprite.Create(ArchipelagoStatic.ArchipelagoIcon, new Rect(0, 0, ArchipelagoStatic.ArchipelagoIcon.width, ArchipelagoStatic.ArchipelagoIcon.height), new Vector2(0.5f, 0.5f));
            newSprite.name = "ArchipelagoItem_cover";
            __instance.unlockCover.sprite = newSprite;

            var archipelagoName = newSong["archName"];
            var archipelagoPlayer = newSong["archPlayer"];

            //IL2CPP is weird on string casting. So to avoid that we use an deprecated type, then use .ToString(). This truly is gross but idk how to fix otherwise.
#pragma warning disable CS0618
            __instance.musicTitle.text = VariableUtils.GetResult(archipelagoName, Il2CppSystem.String.Il2CppType).ToString().Replace('_', ' ');
            __instance.authorTitle.text = VariableUtils.GetResult(archipelagoPlayer, Il2CppSystem.String.Il2CppType).ToString();
#pragma warning restore CS0618
        }

        static void AttemptToFixBrokenAlbumImage(PnlUnlockStage instance) {
            ArchipelagoStatic.ArchLogger.Warning("PnlUnlockStage", "Base Song Sprite was null... attempting to fix.");

            var songIDs = new List<string>();
            songIDs.Add(instance.newSongUid);
            var buffer = new List<MusicInfo>();
            GlobalDataBase.dbMusicTag.GetMusicInfosByUids(songIDs, buffer);
            if (buffer.Count <= 0) {
                ArchipelagoStatic.ArchLogger.Warning("PnlUnlockStage", "Fix failed couldn't find ID.");
                return;
            }

            //This is done to avoid ambiguous nature of List<T>[index] and MusicInfo[Index]. Why this conflicts, I do not know.
            foreach (var info in buffer)
                instance.unlockCover.sprite = ResourcesManager.instance.LoadFromName<Sprite>(info.coverName);
            ArchipelagoStatic.ArchLogger.Warning("PnlUnlockStage", $"Fix was attempted. Success: {instance.unlockCover.sprite != null}");
        }

        static Text GetTitleText(GameObject pnlUnlockStageObject) {
            for (int i = 0; i < pnlUnlockStageObject.transform.childCount; i++) {
                if (pnlUnlockStageObject.transform.GetChild(i).gameObject.name != "AnimationTittleText")
                    continue;

                return pnlUnlockStageObject.transform.GetChild(i).GetChild(0).GetComponent<Text>();
            }

            throw new Exception("Failed to find title text?");
        }
    }

    /// <summary>
    /// Gets called when the player completes the song. Uses this to activate location checks.
    /// </summary>
    [HarmonyPatch(typeof(PnlVictory), "OnVictory")]
    sealed class PnlVictoryPatch {
        static void Postfix() {
            //Don't override normal gameplay
            if (!ArchipelagoStatic.LoggedInToGame)
                return;

            ArchipelagoStatic.ArchLogger.Log("PnlVictory", $"Selected Role: {GlobalDataBase.dbBattleStage.selectedRole}");
            //Block Sleepwalker Rin (Auto Mode) from getting completions
            if (GlobalDataBase.dbBattleStage.selectedRole == 2)
                return;

            //Music info must be grabbed now. The next frame it will be nulled and be unusable.
            var musicInfo = GlobalDataBase.dbBattleStage.selectedMusicInfo;
            var locationName = ArchipelagoStatic.AlbumDatabase.GetItemNameFromMusicInfo(musicInfo);
            ArchipelagoStatic.SessionHandler.CheckLocation(musicInfo.uid, locationName);
        }
    }

    /// <summary>
    /// Called every time the Cell moves. Used to update the cell to show the right status.
    /// Note that this is call per frame during movement.
    /// </summary>
    [HarmonyPatch(typeof(MusicStageCell), "OnChangeCell")]
    sealed class MusicStageCellOnChangeCellPatch {
        static void Postfix(MusicStageCell __instance) {
            //Don't override normal gameplay
            if (!ArchipelagoStatic.LoggedInToGame || __instance.musicInfo == null)
                return;

            if (__instance.musicInfo.uid == "?")
                return; //This is the Random song cell

            //Todo: Possibly fragile. PurchaseLock -> ImgDarken, ImgLock
            var darkenImage = __instance.m_LockObj.transform.GetChild(0).gameObject;
            var lockImage = __instance.m_LockObj.transform.GetChild(1).gameObject;
            if (ArchipelagoStatic.SessionHandler.GoalSong.uid == __instance.musicInfo.uid) {
                //Todo: Does activating the lock block from accessing it?
                __instance.m_LockObj.SetActive(true);
                __instance.m_LockTxt.text = "Goal";

                var unlocked = ArchipelagoStatic.SessionHandler.IsSongUnlocked(__instance.musicInfo.uid);
                darkenImage.SetActive(!unlocked);
                lockImage.SetActive(!unlocked);
            }
            else if (!ArchipelagoStatic.SessionHandler.IsSongUnlocked(__instance.musicInfo.uid)) {
                ArchipelagoStatic.ArchLogger.Log("MusicStageCell", $"{__instance.musicInfo?.uid} {__instance.musicInfo?.name}");
                __instance.m_LockObj.SetActive(true);
                __instance.m_LockTxt.text = "Not yet unlocked.";
                lockImage.SetActive(true);
                darkenImage.SetActive(true);
            }
            else {
                __instance.m_LockObj.SetActive(false);
                __instance.m_LockTxt.text = "";
                lockImage.SetActive(false);
                darkenImage.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Overrides the Play Song Button click so that we can enforce our own restrictions, and bypass the level restriction
    /// </summary>
    [HarmonyPatch(typeof(PnlStage), "OnBtnPlayClicked")]
    sealed class PnlStagePatch {
        static bool Prefix(PnlStage __instance, out int __state) {
            __state = DataHelper.Level;
            //Don't override normal gameplay
            if (!ArchipelagoStatic.LoggedInToGame)
                return true;

            ArchipelagoStatic.ArchLogger.Log("PnlStage", "OnBtnPlayClicked");
            MusicInfo musicInfo = GlobalDataBase.s_DbMusicTag.CurMusicInfo();
            if (musicInfo.uid == "?" || ArchipelagoStatic.SessionHandler.IsSongUnlocked(musicInfo.uid)) {
                //This bypasses level checks in order to allow players to play everything
                DataHelper.Level = 999;
                return true;
            }

            AudioManager.instance.PlayOneShot(__instance.clickSfx, DataHelper.sfxVolume);
            ShowText.ShowInfo("The song isn't unlocked yet. Try another one.");
            return false;
        }

        static void Postfix(int __state) {
            //This should fix the level bypass
            DataHelper.Level = __state;
        }
    }

    [HarmonyPatch(typeof(DBMusicTag), "SelectRandomMusic")]
    sealed class DBMusicTagSelectRandomMusicPatch {
        static bool Prefix(DBMusicTag __instance, out MusicInfo __result) {
            __result = null;
            //Don't override normal gameplay
            if (!ArchipelagoStatic.LoggedInToGame)
                return true;

            __result = ArchipelagoStatic.SessionHandler.GetRandomUnfinishedSong();

            if (__result != null)
                __instance.SetSelectedMusic(__result);

            return false;
        }
    }

    /// <summary>
    /// Only allow the favourite/hide button to be clicked during normal gameplay
    /// </summary>
    [HarmonyPatch(typeof(StageLikeToggle), "OnClicked")]
    sealed class OnClickedOnClickedPatch {
        static bool Prefix() {
            //Don't override normal gameplay
            return !ArchipelagoStatic.LoggedInToGame;
        }
    }
}
