using System.Reflection;
using Archipelago.MultiClient.Net.Enums;
using ArchipelagoMuseDash.Archipelago;
using ArchipelagoMuseDash.Archipelago.Items;
using ArchipelagoMuseDash.Helpers;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.GameCore.HostComponent;
using Il2CppAssets.Scripts.GameCore.Managers;
using Il2CppAssets.Scripts.PeroTools.Managers;
using Il2CppAssets.Scripts.PeroTools.Nice.Interface;
using Il2CppAssets.Scripts.UI.Controls;
using Il2CppAssets.Scripts.UI.Panels;
using Il2CppAssets.Scripts.UI.Panels.PnlRole;
using Il2CppDG.Tweening;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppPeroTools2.Resources;
using UnityEngine;
using UnityEngine.UI;
using Object = Il2CppSystem.Object;


// Due to how IL2CPP works, some things can't be invoked as an extension.
// ReSharper disable InvokeAsExtensionMethod

namespace ArchipelagoMuseDash.Patches;

/// <summary>
///     Various changes so that we can use the Unlock Song panel to show archipelago stuff.
///     Also attempts to fix the rare bug where the texture itself is broken
/// </summary>
[HarmonyPatch(typeof(PnlUnlockStage), "UnlockNewSong")]
sealed class PnlUnlockStagePatch {
    private static void Postfix(PnlUnlockStage __instance) {
        if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
            return;

        try{
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

            if (currentItem.AuthorText != null) {
                if (__instance.authorTitle.font.name == "Normal") {
                    ArchipelagoStatic.ArchLogger.LogDebug("PnlUnlockStage", $"Invalid Font Found. PPG Issue: {__instance.authorTitle.font.name}");
                    __instance.authorTitle.font = __instance.musicTitle.font;
                }

                __instance.authorTitle.text = currentItem.AuthorText;
            }

            __instance.unlockText.text = currentItem.PreUnlockBannerText;

            if (currentItem.UseArchipelagoLogo) {
                var iconIndex = 0;
                if (currentItem is ExternalItem) {
                    if ((currentItem.Item.Flags & ItemFlags.Advancement) != 0)
                        iconIndex = 0;
                    else if ((currentItem.Item.Flags & ItemFlags.NeverExclude) != 0)
                        iconIndex = 1;
                    else if ((currentItem.Item.Flags & ItemFlags.Trap) != 0)
                        iconIndex = 3;
                    else
                        iconIndex = 2;
                }
                else if (currentItem is FillerItem)
                    iconIndex = 2;

                var icon = ArchipelagoStatic.ArchipelagoIcons[iconIndex];

                //Recreate the sprite here as for some reason it gets garbage collected
                var newSprite = Sprite.Create(icon, new Rect(0, 0, icon.width, icon.height), new Vector2(0.5f, 0.5f));
                newSprite.name = "ArchipelagoItem_cover";
                __instance.unlockCover.sprite = newSprite;
            }
            else if (__instance.unlockCover.sprite == null) //Todo: Is this needed anymore
                AttemptToFixBrokenAlbumImage(__instance);
        }
        catch (Exception e) {
            ArchipelagoStatic.ArchLogger.Error("PnlUnlockStage", e);
        }
    }

    private static void AttemptToFixBrokenAlbumImage(PnlUnlockStage instance) {
        ArchipelagoStatic.ArchLogger.Warning("PnlUnlockStage", "Base Song Sprite was null... attempting to fix.");

        var songIDs = new Il2CppSystem.Collections.Generic.List<string>();
        songIDs.Add(instance.newSongUid);
        var buffer = new Il2CppSystem.Collections.Generic.List<MusicInfo>();
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

    public static Text GetTitleText(GameObject pnlUnlockStageObject) {
        for (var i = 0; i < pnlUnlockStageObject.transform.childCount; i++) {
            if (pnlUnlockStageObject.transform.GetChild(i).gameObject.name != "AnimationTittleText")
                continue;

            return pnlUnlockStageObject.transform.GetChild(i).GetChild(0).GetComponent<Text>();
        }

        throw new Exception("Failed to find title text?");
    }

    public static void ShowPostBanner(PnlUnlockStage pnlUnlockStage, IMuseDashItem item) {
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
///     Gets called when the player completes the song. Uses this to activate location checks.
/// </summary>
[HarmonyPatch(typeof(PnlVictory), "OnVictory", typeof(Object), typeof(Object), typeof(Il2CppReferenceArray<Object>))]
[HarmonyPriority(Priority.Last)]
sealed class PnlVictoryPatch {
    public const int NEKO_CHARACTER_ID = 16;
    public const int SILENCER_ELFIN_ID = 9;
    public const int BETA_DOG_ELFIN_ID = 11;

    [HarmonyPriority(Priority.Last)]
    private static void Postfix() {
        //Don't override normal gameplay
        ArchipelagoStatic.ArchLogger.LogDebug("PnlVictory", $"Selected Role: {GlobalDataBase.dbBattleStage.selectedRole}");
        if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
            return;
        try{
            // Block Sleepwalker Rin (Auto Mode) from getting completions
            if (BattleHelper.isAutoSleepy) {
                var reason = "No Items Given:\nSleepwalker Rin was used without Silencer.";
                ShowText.ShowInfo(reason);
                ArchipelagoStatic.ArchLogger.Log("PnlVictory", reason);
                return;
            }

            var activeTrap = ArchipelagoStatic.SessionHandler.BattleHandler.SetTrapFinished();

            // Cover Neko's death
            if (GlobalDataBase.dbBattleStage.IsSelectRole(NEKO_CHARACTER_ID) && !GlobalDataBase.dbBattleStage.IsSelectElfin(SILENCER_ELFIN_ID)) {
                if (GlobalDataBase.dbSkill.nekoSkillInvoke) {
                    var reason = "No Items Given:\nDied as NEKO.";
                    ShowText.ShowInfo(reason);
                    ArchipelagoStatic.ArchLogger.Log("PnlVictory", reason);
                    return;
                }
            }

            if (GlobalDataBase.dbBattleStage.IsSelectElfin(BETA_DOG_ELFIN_ID)) {
                if (GlobalDataBase.dbSkill.betaDogSkillInvoke) {
                    var reason = "No Items Given:\nDied with BetaGo.";
                    ShowText.ShowInfo(reason);
                    ArchipelagoStatic.ArchLogger.Log("PnlVictory", reason);
                    return;
                }
            }

            var kvp = TaskStageTarget.instance.GetStageEvaluate();
            if (kvp.Value < (int)ArchipelagoStatic.SessionHandler.ItemHandler.GradeNeeded) {
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
            ArchipelagoStatic.SessionHandler.BattleHandler.OnBattleEnd(false, activeTrap);
        }
        catch (Exception e) {
            ArchipelagoStatic.ArchLogger.Error("PnlVictory", e);
        }
    }
}
/// <summary>
///     Called every time the Cell moves. Used to update the cell to show the right status.
///     Note that this is call per frame during movement.
/// </summary>
[HarmonyPatch(typeof(MusicStageCell), "RefreshData")]
sealed class MusicStageCellOnChangeCellPatch {
    private static Color? _originalTextColor;

    private static void Postfix(MusicStageCell __instance, int targetCellIndex) {
        //Don't override normal gameplay
        if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
            return;

        try {
            //Todo: Possibly fragile. PurchaseLock -> ImgDarken, ImgLock
            var darkenImage = __instance.m_LockObj.transform.GetChild(0).gameObject;
            var lockImage = __instance.m_LockObj.transform.GetChild(1).gameObject;
            var banner = __instance.m_LockObj.transform.GetChild(2).gameObject;
            var bannerImage = banner.GetComponent<Image>();
            bannerImage.color = new Color(bannerImage.color.r, bannerImage.color.g, bannerImage.color.b, 1f);

            if (!_originalTextColor.HasValue)
                _originalTextColor = __instance.m_LockTxt.color;
            __instance.m_LockTxt.color = _originalTextColor.Value;

            if (targetCellIndex == -1)
                targetCellIndex = VariableUtils.GetResult<int>(__instance.m_VariableBehaviour.Cast<IVariable>());

            var cellInfo = __instance.GetMusicStageCellInfo(targetCellIndex);
            if (cellInfo.uidIsRandom || cellInfo.musicUid == AlbumDatabase.RANDOM_PANEL_UID) {
                lockImage.SetActive(false);
                darkenImage.SetActive(false);
                __instance.m_LockObj.SetActive(false);
                return;
            }

            var itemHandler = ArchipelagoStatic.SessionHandler.ItemHandler;
            var uid = cellInfo.musicUid;

            if (itemHandler.GoalSong.uid == uid) {
                __instance.m_LockObj.SetActive(true);

                if (itemHandler.VictoryAchieved)
                    __instance.m_LockTxt.text = "Goal [Completed]";
                else if (itemHandler.NumberOfMusicSheetsToWin > 1 && itemHandler.NumberOfMusicSheetsToWin - itemHandler.CurrentNumberOfMusicSheets > 0)
                    __instance.m_LockTxt.text = $"Goal [{itemHandler.NumberOfMusicSheetsToWin - itemHandler.CurrentNumberOfMusicSheets} Left]";
                else
                    __instance.m_LockTxt.text = "Goal";

                var unlocked = itemHandler.UnlockedSongUids.Contains(uid);
                darkenImage.SetActive(!unlocked);
                lockImage.SetActive(!unlocked);
                banner.SetActive(true);
                __instance.m_LockTxt.gameObject.SetActive(true);
            }
            else if (itemHandler.StarterSongUIDs.Contains(uid)) {
                __instance.m_LockObj.SetActive(true);
                __instance.m_LockTxt.text = "Starter";
                __instance.m_LockTxt.color = Color.white;
                darkenImage.SetActive(false);
                lockImage.SetActive(false);
                banner.SetActive(true);
                bannerImage.color = new Color(bannerImage.color.r, bannerImage.color.g, bannerImage.color.b, 0.5f);
                __instance.m_LockTxt.gameObject.SetActive(true);
            }
            else {
                var locked = !itemHandler.UnlockedSongUids.Contains(uid);
                lockImage.SetActive(locked);
                darkenImage.SetActive(locked);
                __instance.m_LockObj.SetActive(locked);

                if (locked) {
                    var songInLogic = ArchipelagoStatic.SessionHandler.ItemHandler.SongsInLogic.Contains(uid);

                    if (songInLogic) {
                        __instance.m_LockTxt.text = "Not yet unlocked.";
                        banner.SetActive(true);
                        __instance.m_LockTxt.gameObject.SetActive(true);
                    }
                    else {
                        banner.SetActive(false);
                        __instance.m_LockTxt.gameObject.SetActive(false);
                    }
                }
            } 
        }
        catch (Exception e) {
            ArchipelagoStatic.ArchLogger.Error("MusicStageCell", e);
        }
    }
}
[HarmonyPatch(typeof(DBMusicTag), "SelectRandomMusic")]
sealed class DBMusicTagSelectRandomMusicPatch {
    private static bool Prefix(DBMusicTag __instance, out MusicInfo __result) {
        try { 
            __result = null;
            //Don't override normal gameplay
            if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
                return true;

            __result = ArchipelagoStatic.SessionHandler.ItemHandler.GetRandomUnfinishedSong();

            if (__result != null)
                __instance.SetSelectedMusic(__result, true);

            return false;
        }
        catch (Exception e) {
            ArchipelagoStatic.ArchLogger.Error("SelectRandomMusic", e);
            throw;
        }
    }
}
/// <summary>
///     Only allow the favourite/hide button to be clicked during normal gameplay
/// </summary>
[HarmonyPatch(typeof(StageLikeToggle), "OnClicked")]
sealed class OnClickedOnClickedPatch {
    private static bool Prefix() {
        //Don't override normal gameplay
        return !ArchipelagoStatic.SessionHandler.IsLoggedIn;
    }
}
/// <summary>
///     Only allow the favourite/hide button to be clicked during normal gameplay
/// </summary>
[HarmonyPatch(typeof(PnlRole), "OnApplyClicked")]
sealed class PnlRoleApplyPatch {
    private const int sleepwalker_rin_character_id = 2;
    private const int neko_character_id = 16;

    private static void Postfix() {
        try { 
            if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
                return;

            if (DataHelper.selectedRoleIndex == sleepwalker_rin_character_id)
                ShowText.ShowInfo("Sleepwalker Rin will not unlock items without the Silencer Elfin.");
            else if (DataHelper.selectedRoleIndex == neko_character_id)
                ShowText.ShowInfo("NEKO will not unlock items if she dies before completing the stage.");
        }
        catch (Exception e) {
            ArchipelagoStatic.ArchLogger.Error("PnlRole", e);
        }
    }
}
/// <summary>
///     Only allow the favourite/hide button to be clicked during normal gameplay
/// </summary>
[HarmonyPatch(typeof(PnlElfin), "OnApplyClicked")]
sealed class PnlElfinApplyPatch {
    private const int beta_dog_elfin_index = 11;

    private static void Postfix() {
        if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
            return;
        try { 
            ArchipelagoStatic.ArchLogger.LogDebug("Elfin", DataHelper.selectedElfinIndex.ToString());
            if (DataHelper.selectedElfinIndex == beta_dog_elfin_index)
                ShowText.ShowInfo("BetaGo will not unlock items if you die before completing the stage.");
        }
        catch (Exception e) {
            ArchipelagoStatic.ArchLogger.Error("PnlElfin", e);
        }
    }
}
/// <summary>
///     Show the reason briefly for deathlink
/// </summary>
[HarmonyPatch(typeof(PnlFail), "OnEnable")]
sealed class PnlFailOnEnablePatch {
    private static void Postfix() {
        if (!ArchipelagoStatic.SessionHandler.IsLoggedIn || ArchipelagoStatic.SessionHandler?.DeathLinkHandler == null)
            return;

        try { 
            var reason = ArchipelagoStatic.SessionHandler.DeathLinkHandler.GetDeathLinkReason();
            if (reason != null)
                ShowText.ShowInfo(reason);
        }
        catch (Exception e) {
            ArchipelagoStatic.ArchLogger.Error("PnlFail", e);
        }
    }
}
/// <summary>
///     Gets called when the player completes the song. Uses this to activate location checks.
/// </summary>
[HarmonyPatch(typeof(PnlVictory), "OnContinueClicked")]
sealed class PnlVictoryOnContinueClickedPatch {
    private static void Postfix() {
        if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
            return;

        try {
            if (ArchipelagoStatic.SessionHandler.ItemHandler.HiddenSongMode != ShownSongMode.Unplayed)
                return;

            ArchipelagoHelpers.SelectNextAvailableSong();
        }
        catch (Exception e) {
            ArchipelagoStatic.ArchLogger.Error("PnlVictory", e);
        }
    }
}
/// <summary>
///     Slightly extend Show Text messages so they are readable
/// </summary>
[HarmonyPatch(typeof(ShowText), "DoTweenInit")]
sealed class ShowTextDoTweenInitPatch {
    private static void Postfix(ShowText __instance) {
        try { 
            var tween = DOTweenModuleUI.DOFade(__instance.m_CanvasGroup, 1f, 5f);
            TweenSettingsExtensions.SetEase(tween, __instance.m_Curve);
            tween.onComplete = __instance.m_Tween.onComplete;
            TweenSettingsExtensions.SetAutoKill(tween, false);
            tween.onKill = __instance.m_Tween.onKill;
            __instance.m_Tween = tween;
        }
        catch (Exception e) {
            ArchipelagoStatic.ArchLogger.Error("ShowText", e);
        }
    }
}
/// <summary>
///     Disable the level up panel during archipelago
/// </summary>
[HarmonyPatch(typeof(PnlLevelUpAward), "OnLevelUp")]
sealed class PnlLevelUpAwardOnLevelUpPatch {
    private static bool Prefix() {
        return !ArchipelagoStatic.SessionHandler.IsLoggedIn;
    }
}
/// <summary>
///     Disable the level up panel during archipelago
/// </summary>
[HarmonyPatch(typeof(PnlUnlock), "OnStageUnlockOrNot")]
sealed class PnlUnlockOnStageUnlockOrNotPatch {
    private static bool Prefix() {
        return !ArchipelagoStatic.SessionHandler.IsLoggedIn;
    }
}
[HarmonyPatch(typeof(BattleRoleAttributeComponent), "Hurt")]
sealed class BattleRoleAttributeComponentHurtPatch {
    private static bool Prefix(BattleRoleAttributeComponent __instance, int hurtValue, bool isAir) {
        if (!ArchipelagoStatic.SessionHandler.IsLoggedIn)
            return true;

        try { 
            if (__instance.m_Hp + hurtValue > 0)
                return true;

            if (!ArchipelagoStatic.SessionHandler.BattleHandler.TryUseExtraLife())
                return true;

            __instance.AddHp(__instance.GetHpMax() - __instance.m_Hp);
            return false;
        }
        catch (Exception e) {
            ArchipelagoStatic.ArchLogger.Error("ShowText", e);
            return false;
        }
    }
}
[HarmonyPatch(typeof(ChangeHealthValue), "OnHpAdd")]
[HarmonyPatch(typeof(ChangeHealthValue), "OnHpDeduct")]
[HarmonyPatch(typeof(ChangeHealthValue), "OnHpRateChange")]
sealed class ChangeHealthValueExtraLifePatch {
    private static void Postfix(ChangeHealthValue __instance) {
        ArchipelagoStatic.ArchLogger.LogDebug("ChangeHealthValue", "Active");
        if (!ArchipelagoStatic.SessionHandler.IsLoggedIn || !__instance || !__instance.text)
            return;

        try {
            var extraLifeCount = ArchipelagoStatic.SessionHandler.BattleHandler.GetExtraLives();
            ArchipelagoStatic.ArchLogger.LogDebug("ChangeHealthValue", $"Logged in: {extraLifeCount <= 0}, {__instance.text.text.EndsWith(')')}");
            if (extraLifeCount <= 0 || __instance.text.text.EndsWith(')'))
                return;


            __instance.text.horizontalOverflow = HorizontalWrapMode.Overflow;
            var newText = $"{BattleRoleAttributeComponent.instance.m_Hp}/{BattleRoleAttributeComponent.instance.GetHpMax()}  (+{extraLifeCount})";
            __instance.text.text = newText;
        }
        catch (Exception e) {
            ArchipelagoStatic.ArchLogger.Error("ChangeHealthValueExtraLifePatch", e);
        }

        //MelonCoroutines.Start(ChangeHPText(__instance));
    }
}
[HarmonyPatch(typeof(MessageManager))]
sealed class MessageManagerReceivePatch {
    private static MethodInfo[] TargetMethods() {
        return typeof(MessageManager).GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(m => m.Name.StartsWith("Receive")).ToArray();
    }

    private static bool Prefix(string type) {
        ArchipelagoStatic.ArchLogger.LogDebug("Message Manager", $"Blocking Receive: {type}");
        return !ArchipelagoStatic.SessionHandler.IsLoggedIn;
    }
}
[HarmonyPatch(typeof(MessageManager))]
sealed class MessageManagerOnRewardPatch {
    private static MethodInfo[] TargetMethods() {
        return typeof(MessageManager).GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(m => m.Name.StartsWith("OnReward")).ToArray();
    }

    private static bool Prefix(object sender, object rev) {
        ArchipelagoStatic.ArchLogger.LogDebug("Message Manager", $"Blocking OnReward: {sender}, {rev}");
        return !ArchipelagoStatic.SessionHandler.IsLoggedIn;
    }
}

/// <summary>
///     Apply AP song order onto refreshed music uid list
///     the input buffer has all changes from filters and such, while
///     stageShowMusicList contains old "live" list of uids, and this
///     function replaces it in the process
/// </summary>
[HarmonyPatch(typeof(DBMusicTag), "RefreshShowMusicUids")]
sealed class DBMusicTagRefreshShowMusicUidsPatch {
    private static void Prefix(Il2CppSystem.Collections.Generic.List<string> buffer) {
        try { 
            ArchipelagoStatic.ArchLogger.LogDebug("DBMusicTag", "RefreshShowMusicUids");
            if (!ArchipelagoStatic.SessionHandler.IsLoggedIn || ArchipelagoStatic.IsLoadingAP)
                return;
            
            var apSongUidList = ArchipelagoStatic.SessionHandler.ItemHandler.UnlockedSongUids;
            if (buffer.Count == 0 || apSongUidList.Count == 0)
                return;
                
            var goalSongUid = ArchipelagoStatic.SessionHandler.ItemHandler.GoalSong.uid;
            // random panel might be last element or might not exist
            // we want to account for that and put the goal song
            // before the panel if it does exist
            
            int offset;
            if (buffer[^1] == AlbumDatabase.RANDOM_PANEL_UID)
                offset = 2;
            else 
                offset = 1;
            
            if (buffer.Count >= offset && goalSongUid != buffer[^offset]) {
                // `Count - offset` makes the loops ignore both slot for goal song and the random panel (if it exists)
                var goalIndex = buffer.SublistIndexOf(0, buffer.Count - offset, goalSongUid);
                if (goalIndex != -1) {
                    buffer[goalIndex] = buffer[^offset];
                    buffer[^offset] = goalSongUid;
                }
            }
            
            // `Count - offset` makes the loops ignore both goal song and the random panel (if it exists)
            var j = 0;
            for (var i = 0; i < apSongUidList.Count && j < buffer.Count - offset; i++) {
                if (apSongUidList[i] == goalSongUid)
                    continue;
                
                if (apSongUidList[i] == buffer[j]) {
                    j++;
                    continue;
                }
                
                var uidIndex = buffer.SublistIndexOf(j + 1, buffer.Count - offset, apSongUidList[i]);
                if (uidIndex != -1) {
                    buffer[uidIndex] = buffer[j];
                    buffer[j] = apSongUidList[i];
                    j++;
                }
            }
        }
        catch (Exception e) {
            ArchipelagoStatic.ArchLogger.Error("RefreshPatch", e);
        }
    }
}

