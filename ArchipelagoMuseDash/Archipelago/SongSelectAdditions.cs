using System;
using ArchipelagoMuseDash.Helpers;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ArchipelagoMuseDash.Archipelago
{
    public class SongSelectAdditions
    {
        public GameObject HintButton;
        public Button HintButtonComp;

        public GameObject ToggleSongsButton;
        public Button ToggleSongsButtonComp;
        public Text ToggleSongsText;

        public GameObject HintText;
        public Text HintTextComp;

        public GameObject SongText;
        public Text SongTitleComp;

        public void OnUpdate()
        {
            if (!ArchipelagoStatic.ActivatedEnableDisableHookers.Contains("PnlStage"))
                return;

            if (HintButton != null)
            {
                if (EventSystem.current == null)
                    return;

                if (EventSystem.current.currentSelectedGameObject == HintButton)
                {
                    ArchipelagoStatic.ArchLogger.LogDebug("SongSelectAdditions", "Force deselecting Hint Button");
                    EventSystem.current.SetSelectedGameObject(null);
                }
                if (EventSystem.current.currentSelectedGameObject == ToggleSongsButton)
                {
                    ArchipelagoStatic.ArchLogger.LogDebug("SongSelectAdditions", "Force deselecting Toggle Songs Button");
                    EventSystem.current.SetSelectedGameObject(null);
                }
                return;
            }

            if (!ArchipelagoStatic.HideSongDialogue || !ArchipelagoStatic.HideSongDialogue.m_YesButton
                || !ArchipelagoStatic.HideSongDialogue.m_NoButton || !ArchipelagoStatic.SongSelectPanel)
                return;

            var likeButton = ArchipelagoStatic.SongSelectPanel.gameObject.GetComponentInChildren<StageLikeToggle>(true);
            if (likeButton == null)
                return;

            ToggleSongsButton = CreateButton(likeButton, "ArchipelagoHideButton", "Show Locked Songs", new Vector2(660, 40), new Vector2(0, 0.5f), ArchipelagoStatic.SessionHandler.ItemHandler.PickNextSongShownMode);
            ToggleSongsButtonComp = ToggleSongsButton.GetComponent<Button>();
            ToggleSongsText = ToggleSongsButton.GetComponentInChildren<Text>();

            HintButton = CreateButton(likeButton, "ArchipelagoHintButton", "Get Hint", new Vector2(415, 40), new Vector2(0, 0.5f), ArchipelagoStatic.SessionHandler.HintHandler.ShowHintPopup);
            HintButtonComp = HintButton.GetComponent<Button>();

            AddSongTitleBox(likeButton);
            AddHintBox(likeButton);
        }

        public void MainSceneLoaded()
        {
            HintButton = null;
            ToggleSongsButton = null;
            ToggleSongsText = null;
            SongText = null;
        }

        public void AddHintBox(StageLikeToggle likeButton)
        {
            //Todo: This needs a bit of cleaning up. Maybe split into other methods to make it easier to follow.

            //The HideSongDialogue has the button we want, and it should be available at this time.
            var noButton = ArchipelagoStatic.HideSongDialogue.m_NoButton;
            var noButtonImage = noButton.GetComponent<Image>();
            var noText = noButton.transform.GetChild(0);
            var noTextComp = noText.GetComponent<Text>();

            HintText = new GameObject("ArchipelagoHintText");
            HintText.transform.SetParent(likeButton.transform.parent, false);

            var hintBackgroundImage = HintText.AddComponent<Image>();
            hintBackgroundImage.sprite = noButtonImage.sprite;
            hintBackgroundImage.type = noButtonImage.type;

            var hintTransform = HintText.GetComponent<RectTransform>();
            hintTransform.anchorMax = hintTransform.anchorMin = new Vector2(0.5f, 0.5f);
            hintTransform.anchoredPosition = new Vector2(400, 185);
            hintTransform.pivot = new Vector2(0, 0.5f);
            hintTransform.sizeDelta = new Vector2(500, 100);

            var hintText = new GameObject();
            hintText.transform.SetParent(HintText.transform, false);

            HintTextComp = hintText.AddComponent<Text>();
            AssetHelpers.CopyTextVariables(noTextComp, HintTextComp);
            HintTextComp.fontSize = 22;
            HintTextComp.resizeTextForBestFit = true;
            HintTextComp.resizeTextMaxSize = 22;
            HintTextComp.resizeTextMinSize = 12;
            HintTextComp.verticalOverflow = VerticalWrapMode.Truncate;

            var hintTextRect = hintText.GetComponent<RectTransform>();
            hintTextRect.anchorMin = new Vector2(0.1f, 0.1f);
            hintTextRect.anchorMax = new Vector2(0.9f, 0.9f);
            hintTextRect.sizeDelta = Vector2.zero; //Resets the size back to the anchors
        }

        public void AddSongTitleBox(StageLikeToggle likeButton)
        {
            //Todo: This needs a bit of cleaning up. Maybe split into other methods to make it easier to follow.

            //The HideSongDialogue has the button we want, and it should be available at this time.
            var noButton = ArchipelagoStatic.HideSongDialogue.m_NoButton;
            var noButtonImage = noButton.GetComponent<Image>();
            var noText = noButton.transform.GetChild(0);
            var noTextComp = noText.GetComponent<Text>();

            SongText = new GameObject("ArchipelagoSongTitle");
            SongText.transform.SetParent(likeButton.transform.parent, false);

            var songBackgroundImage = SongText.AddComponent<Image>();
            songBackgroundImage.sprite = noButtonImage.sprite;
            songBackgroundImage.type = noButtonImage.type;

            var songTransform = SongText.GetComponent<RectTransform>();
            songTransform.anchorMax = songTransform.anchorMin = new Vector2(0.5f, 0.5f);
            songTransform.anchoredPosition = new Vector2(400, 100);
            songTransform.pivot = new Vector2(0, 0.5f);
            songTransform.sizeDelta = new Vector2(500, 60);

            var songText = new GameObject();
            songText.transform.SetParent(SongText.transform, false);

            SongTitleComp = songText.AddComponent<Text>();
            AssetHelpers.CopyTextVariables(noTextComp, SongTitleComp);
            SongTitleComp.fontSize = 28;
            SongTitleComp.resizeTextForBestFit = true;
            SongTitleComp.resizeTextMaxSize = 28;
            SongTitleComp.resizeTextMinSize = 12;
            SongTitleComp.verticalOverflow = VerticalWrapMode.Truncate;

            var songTextRect = songText.GetComponent<RectTransform>();
            songTextRect.anchorMin = new Vector2(0.1f, 0.1f);
            songTextRect.anchorMax = new Vector2(0.9f, 0.9f);
            songTextRect.sizeDelta = Vector2.zero; //Resets the size back to the anchors
        }

        private GameObject CreateButton(StageLikeToggle likeButton, string buttonName, string buttonText, Vector2 offset, Vector2 pivot, Action onClick)
        {
            //The HideSongDialogue has the button we want, and it should be available at this time.
            var yesButton = ArchipelagoStatic.HideSongDialogue.m_YesButton;
            var yesButtonImage = yesButton.GetComponent<Image>();
            var yesText = yesButton.transform.GetChild(0);
            var yesTextComp = yesText.GetComponent<Text>();

            var buttonParent = new GameObject(buttonName);
            buttonParent.transform.SetParent(likeButton.transform.parent, false);


            var button = buttonParent.AddComponent<Button>();
            button.onClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(onClick));
            button.transition = yesButton.transition;
            button.navigation.mode = Navigation.Mode.None;

            var colours = yesButton.colors;
            colours.disabledColor = new Color(colours.disabledColor.r * 0.8f, colours.disabledColor.g * 0.8f, colours.disabledColor.b * 0.8f, 1);
            button.colors = colours;

            var image = buttonParent.AddComponent<Image>();
            image.sprite = yesButtonImage.sprite;
            image.type = yesButtonImage.type;
            button.targetGraphic = image;

            var imageTransform = buttonParent.GetComponent<RectTransform>();
            var yesButtonTransform = yesButton.gameObject.GetComponent<RectTransform>();

            imageTransform.sizeDelta = yesButtonTransform.sizeDelta * new Vector2(0.875f, 0.85f);
            imageTransform.anchorMax = imageTransform.anchorMin = new Vector2(0.5f, 0.5f);
            imageTransform.anchoredPosition = offset;
            imageTransform.pivot = pivot;

            var hintButtonText = new GameObject("Text");
            hintButtonText.transform.SetParent(buttonParent.transform, false);
            var hintTextComp = hintButtonText.AddComponent<Text>();

            AssetHelpers.CopyTextVariables(yesTextComp, hintTextComp);
            hintTextComp.fontSize -= 15;
            hintTextComp.text = buttonText;

            var rectTransfrom = hintButtonText.GetComponent<RectTransform>();
            var yesRectTransform = yesText.GetComponent<RectTransform>();

            rectTransfrom.anchorMin = yesRectTransform.anchorMin;
            rectTransfrom.anchorMax = yesRectTransform.anchorMax;
            rectTransfrom.pivot = yesRectTransform.pivot;
            rectTransfrom.sizeDelta = yesRectTransform.sizeDelta;

            return buttonParent;
        }
    }
}