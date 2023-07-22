using System;
using Archipelago.MultiClient.Net.Enums;
using ArchipelagoMuseDash.Archipelago;
using ArchipelagoMuseDash.Archipelago.Items;
using ArchipelagoMuseDash.Helpers;
using Assets.Scripts.Database;
using Assets.Scripts.GameCore.HostComponent;
using Assets.Scripts.PeroTools.Managers;
using Assets.Scripts.UI.Controls;
using Assets.Scripts.UI.Panels;
using DG.Tweening;
using HarmonyLib;
using PeroTools2.Resources;
using UnhollowerBaseLib;
using UnityEngine;
using UnityEngine.UI;


// Due to how IL2CPP works, some things can't be invoked as an extension.
// ReSharper disable InvokeAsExtensionMethod

namespace ArchipelagoMuseDash.Patches
{

    /// <summary>
    /// Various changes so that we can use the Unlock Song panel to show archipelago stuff.
    /// Also attempts to fix the rare bug where the texture itself is broken
    /// </summary>
    [HarmonyPatch(typeof(PnlUnlockStage), "UnlockNewSong")]
    sealed class PnlUnlockStagePatch
    {
        private static void Postfix(PnlUnlockStage __instance)
        {
            if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
                return;

            ArchipelagoStatic.ArchLogger.LogDebug("PnlUnlockStage", "Handling new Item.");

            var currentItem = ArchipelagoStatic.SessionHandler.ItemHandler.Unlocker.GetCurrentItem();

            //Something else triggered this unlock. Avoid doing anything
            if (currentItem == null)
                return;

            ArchipelagoStatic.ArchLogger.LogDebug("PnlUnlockStage", $"Demo: {GlobalDataBase.dbMusicTag.m_AllMusicInfo[currentItem.UnlockSongUid].demo}");

            if (currentItem.TitleText != null)
                GetTitleText(__instance.gameObject).text = currentItem.TitleText;

            if (currentItem.SongText != null)
                __instance.musicTitle.text = currentItem.SongText;

            if (currentItem.AuthorText != null)
                __instance.authorTitle.text = currentItem.AuthorText;

            __instance.unlockText.text = currentItem.PreUnlockBannerText;

            if (currentItem.UseArchipelagoLogo)
            {
                int iconIndex = 0;
                if (currentItem is ExternalItem)
                {
                    if ((currentItem.Item.Flags & ItemFlags.Advancement) != 0)
                        iconIndex = 0;
                    else if ((currentItem.Item.Flags & ItemFlags.NeverExclude) != 0)
                        iconIndex = 1;
                    else if ((currentItem.Item.Flags & ItemFlags.Trap) != 0)
                        iconIndex = 3;
                    else
                        iconIndex = 2;
                }

                var icon = ArchipelagoStatic.ArchipelagoIcons[iconIndex];

                //Recreate the sprite here as for some reason it gets garbage collected
                var newSprite = Sprite.Create(icon, new Rect(0, 0, icon.width, icon.height), new Vector2(0.5f, 0.5f));
                newSprite.name = "ArchipelagoItem_cover";
                __instance.unlockCover.sprite = newSprite;
            }
            else if (__instance.unlockCover.sprite == null) //Todo: Is this needed anymore
                AttemptToFixBrokenAlbumImage(__instance);
        }

        private static void AttemptToFixBrokenAlbumImage(PnlUnlockStage instance)
        {
            ArchipelagoStatic.ArchLogger.Warning("PnlUnlockStage", "Base Song Sprite was null... attempting to fix.");

            var songIDs = new Il2CppSystem.Collections.Generic.List<string>();
            songIDs.Add(instance.newSongUid);
            var buffer = new Il2CppSystem.Collections.Generic.List<MusicInfo>();
            GlobalDataBase.dbMusicTag.GetMusicInfosByUids(songIDs, buffer);
            if (buffer.Count <= 0)
            {
                ArchipelagoStatic.ArchLogger.Warning("PnlUnlockStage", "Fix failed couldn't find ID.");
                return;
            }

            //This is done to avoid ambiguous nature of List<T>[index] and MusicInfo[Index]. Why this conflicts, I do not know.
            foreach (var info in buffer)
                instance.unlockCover.sprite = ResourcesManager.instance.LoadFromName<Sprite>(info.coverName);
            ArchipelagoStatic.ArchLogger.Warning("PnlUnlockStage", $"Fix was attempted. Success: {instance.unlockCover.sprite != null}");
        }

        public static Text GetTitleText(GameObject pnlUnlockStageObject)
        {
            for (int i = 0; i < pnlUnlockStageObject.transform.childCount; i++)
            {
                if (pnlUnlockStageObject.transform.GetChild(i).gameObject.name != "AnimationTittleText")
                    continue;

                return pnlUnlockStageObject.transform.GetChild(i).GetChild(0).GetComponent<Text>();
            }

            throw new Exception("Failed to find title text?");
        }

        public static void ShowPostBanner(PnlUnlockStage pnlUnlockStage, IMuseDashItem item)
        {
            var postBannerText = item.PostUnlockBannerText;
            if (postBannerText == null)
                return;

            var lockParent = pnlUnlockStage.unlockText.transform.parent;
            lockParent.gameObject.SetActive(true);

            pnlUnlockStage.unlockText.transform.parent.GetChild(3).gameObject.SetActive(true);
            pnlUnlockStage.unlockText.gameObject.SetActive(true);
            pnlUnlockStage.unlockText.text = postBannerText;
        }
    }
    /// <summary>
    /// Gets called when the player completes the song. Uses this to activate location checks.
    /// </summary>
    [HarmonyPatch(typeof(PnlVictory), "OnVictory", new[] { typeof(Il2CppSystem.Object), typeof(Il2CppSystem.Object), typeof(Il2CppReferenceArray<Il2CppSystem.Object>) })]
    sealed class PnlVictoryPatch
    {
        public const int NEKO_CHARACTER_ID = 16;
        public const int SILENCER_ELFIN_ID = 9;

        private static void Postfix()
        {
            //Don't override normal gameplay
            ArchipelagoStatic.ArchLogger.LogDebug("PnlVictory", $"Selected Role: {GlobalDataBase.dbBattleStage.selectedRole}");
            if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
                return;

            // Block Sleepwalker Rin (Auto Mode) from getting completions
            if (BattleHelper.isAutoSleepy)
            {
                var reason = "No Items Given:\nSleepwalker Rin was used without Silencer.";
                ShowText.ShowInfo(reason);
                ArchipelagoStatic.ArchLogger.Log("PnlVictory", reason);
                return;
            }

            ArchipelagoStatic.SessionHandler.TrapHandler.SetTrapFinished();

            // Cover Neko's death
            if (GlobalDataBase.dbBattleStage.IsSelectRole(NEKO_CHARACTER_ID) && !GlobalDataBase.dbBattleStage.IsSelectElfin(SILENCER_ELFIN_ID))
            {
                if (GlobalDataBase.dbSkill.nekoSkillInvoke)
                {
                    var reason = "No Items Given:\nDied as NEKO.";
                    ShowText.ShowInfo(reason);
                    ArchipelagoStatic.ArchLogger.Log("PnlVictory", reason);
                    return;
                }
            }

            var kvp = TaskStageTarget.instance.GetStageEvaluate();
            if (kvp.Value < (int)ArchipelagoStatic.SessionHandler.ItemHandler.GradeNeeded)
            {
                var reason = $"No Items Given:\nGrade result was worse than {ArchipelagoStatic.SessionHandler.ItemHandler.GradeNeeded}";
                ShowText.ShowInfo(reason);
                ArchipelagoStatic.ArchLogger.Log("PnlVictory", reason);
                ArchipelagoStatic.SessionHandler.DeathLinkHandler.PlayerDied();
                return;
            }

            //Music info must be grabbed now. The next frame it will be nulled and be unusable.
            var musicInfo = GlobalDataBase.dbBattleStage.selectedMusicInfo;
            var locationName = ArchipelagoStatic.AlbumDatabase.GetItemNameFromMusicInfo(musicInfo);
            ArchipelagoStatic.SessionHandler.ItemHandler.CheckLocation(musicInfo.uid, locationName);
        }
    }
    /// <summary>
    /// Called every time the Cell moves. Used to update the cell to show the right status.
    /// Note that this is call per frame during movement.
    /// </summary>
    [HarmonyPatch(typeof(MusicStageCell), "OnChangeCell")]
    sealed class MusicStageCellOnChangeCellPatch
    {
        private static void Postfix(MusicStageCell __instance)
        {
            //Don't override normal gameplay
            if (!ArchipelagoStatic.SessionHandler.IsLoggedIn || __instance.musicInfo == null)
                return;

            if (__instance.musicInfo.uid == "?")
                return; //This is the Random song cell

            var itemHandler = ArchipelagoStatic.SessionHandler.ItemHandler;

            //Todo: Possibly fragile. PurchaseLock -> ImgDarken, ImgLock
            var darkenImage = __instance.m_LockObj.transform.GetChild(0).gameObject;
            var lockImage = __instance.m_LockObj.transform.GetChild(1).gameObject;
            var banner = __instance.m_LockObj.transform.GetChild(2).gameObject;
            if (itemHandler.GoalSong.uid == __instance.musicInfo.uid)
            {
                __instance.m_LockObj.SetActive(true);

                if (itemHandler.VictoryAchieved)
                    __instance.m_LockTxt.text = "Goal [Completed]";
                else if (itemHandler.NumberOfMusicSheetsToWin > 1 && itemHandler.NumberOfMusicSheetsToWin - itemHandler.CurrentNumberOfMusicSheets > 0)
                    __instance.m_LockTxt.text = $"Goal [{itemHandler.NumberOfMusicSheetsToWin - itemHandler.CurrentNumberOfMusicSheets} Left]";
                else
                    __instance.m_LockTxt.text = "Goal";

                var unlocked = itemHandler.UnlockedSongUids.Contains(__instance.musicInfo.uid);
                darkenImage.SetActive(!unlocked);
                lockImage.SetActive(!unlocked);
                banner.SetActive(true);
                __instance.m_LockTxt.gameObject.SetActive(true);
            }
            else
            {
                var locked = !itemHandler.UnlockedSongUids.Contains(__instance.musicInfo.uid);
                lockImage.SetActive(locked);
                darkenImage.SetActive(locked);
                __instance.m_LockObj.SetActive(locked);

                if (locked)
                {
                    var songInLogic = ArchipelagoStatic.SessionHandler.ItemHandler.SongsInLogic.Contains(__instance.musicInfo.uid);

                    if (songInLogic)
                    {
                        __instance.m_LockTxt.text = "Not yet unlocked.";
                        banner.SetActive(true);
                        __instance.m_LockTxt.gameObject.SetActive(true);
                    }
                    else
                    {
                        banner.SetActive(false);
                        __instance.m_LockTxt.gameObject.SetActive(false);
                    }
                }
            }
        }
    }
    /// <summary>
    /// Overrides the Play Song Button click so that we can enforce our own restrictions, and bypass the level restriction
    /// </summary>
    [HarmonyPatch(typeof(PnlStage), "OnBtnPlayClicked")]
    sealed class PnlStageOnBtnPlayClickedPatch
    {
        private static bool Prefix(PnlStage __instance, out int __state)
        {
            __state = DataHelper.Level;
            //Don't override normal gameplay
            if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
                return true;

            ArchipelagoStatic.ArchLogger.LogDebug("PnlStage", "OnBtnPlayClicked");
            MusicInfo musicInfo = GlobalDataBase.s_DbMusicTag.CurMusicInfo();
            if (musicInfo.uid == "?" || ArchipelagoStatic.SessionHandler.ItemHandler.UnlockedSongUids.Contains(musicInfo.uid))
            {
                //This bypasses level checks in order to allow players to play everything
                DataHelper.Level = 999;
                return true;
            }

            AudioManager.instance.PlayOneShot(__instance.clickSfx, DataHelper.sfxVolume);
            ShowText.ShowInfo("The song isn't unlocked yet. Try another one.");
            return false;
        }

        private static void Postfix(int __state)
        {
            //This should fix the level bypass
            DataHelper.Level = __state;
        }
    }
    [HarmonyPatch(typeof(DBMusicTag), "SelectRandomMusic")]
    sealed class DBMusicTagSelectRandomMusicPatch
    {
        private static bool Prefix(DBMusicTag __instance, out MusicInfo __result)
        {
            __result = null;
            //Don't override normal gameplay
            if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
                return true;

            __result = ArchipelagoStatic.SessionHandler.ItemHandler.GetRandomUnfinishedSong();

            if (__result != null)
                __instance.SetSelectedMusic(__result);

            return false;
        }
    }
    /// <summary>
    /// Only allow the favourite/hide button to be clicked during normal gameplay
    /// </summary>
    [HarmonyPatch(typeof(StageLikeToggle), "OnClicked")]
    sealed class OnClickedOnClickedPatch
    {
        private static bool Prefix()
        {
            //Don't override normal gameplay
            return !ArchipelagoStatic.SessionHandler.IsLoggedIn;
        }
    }
    /// <summary>
    /// Only allow the favourite/hide button to be clicked during normal gameplay
    /// </summary>
    [HarmonyPatch(typeof(PnlRole), "OnApplyClicked")]
    sealed class PnlRoleApplyPatch
    {
        private const int sleepwalker_rin_character_id = 2;
        private const int neko_character_id = 16;

        private static void Postfix()
        {
            if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
                return;

            if (DataHelper.selectedRoleIndex == sleepwalker_rin_character_id)
                ShowText.ShowInfo("Sleepwalker Rin will not unlock items without the Silencer Elfin.");
            else if (DataHelper.selectedRoleIndex == neko_character_id)
                ShowText.ShowInfo("NEKO will not unlock items if she dies before completing the stage.");
        }
    }
    /// <summary>
    /// Show the reason briefly for deathlink
    /// </summary>
    [HarmonyPatch(typeof(PnlFail), "OnEnable")]
    sealed class PnlFailOnEnablePatch
    {
        private static void Postfix()
        {
            if (!ArchipelagoStatic.SessionHandler.IsLoggedIn || ArchipelagoStatic.SessionHandler?.DeathLinkHandler == null)
                return;

            var reason = ArchipelagoStatic.SessionHandler.DeathLinkHandler.GetDeathLinkReason();
            if (reason != null)
                ShowText.ShowInfo(reason);
        }
    }
    /// <summary>
    /// Gets called when the player completes the song. Uses this to activate location checks.
    /// </summary>
    [HarmonyPatch(typeof(PnlVictory), "OnContinueClicked")]
    sealed class PnlVictoryOnContinueClickedPatch
    {
        private static void Postfix()
        {
            if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
                return;

            if (ArchipelagoStatic.SessionHandler.ItemHandler.HiddenSongMode != ShownSongMode.Unplayed)
                return;

            ArchipelagoHelpers.SelectNextAvailableSong();
        }
    }
    /// <summary>
    /// Slightly extend Show Text messages so they are readable
    /// </summary>
    [HarmonyPatch(typeof(ShowText), "DoTweenInit")]
    sealed class ShowTextDoTweenInitPatch
    {
        private static void Postfix(ShowText __instance)
        {
            var tween = DOTweenModuleUI.DOFade(__instance.m_CanvasGroup, 1f, 5f);
            TweenSettingsExtensions.SetEase(tween, __instance.m_Curve);
            tween.onComplete = __instance.m_Tween.onComplete;
            TweenSettingsExtensions.SetAutoKill(tween, false);
            tween.onKill = __instance.m_Tween.onKill;
            __instance.m_Tween = tween;
        }
    }
    /// <summary>
    /// Disable the level up panel during archipelago
    /// </summary>
    [HarmonyPatch(typeof(PnlLevelUpAward), "OnLevelUp")]
    sealed class PnlLevelUpAwardOnLevelUpPatch
    {
        private static bool Prefix()
        {
            return !ArchipelagoStatic.SessionHandler.IsLoggedIn;
        }
    }
}