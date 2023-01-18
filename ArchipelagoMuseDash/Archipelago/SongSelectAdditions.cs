using System;
using ArchipelagoMuseDash.Helpers;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ArchipelagoMuseDash.Archipelago {
    public class SongSelectAdditions {
        public GameObject HintButton;
        public Button HintButtonComp;

        public GameObject HideSongsButton;
        public Button HideSongsButtonComp;
        public Text HideSongsText;

        public GameObject HintText;
        public Text HintTextComp;

        public void OnUpdate() {
            if (!ArchipelagoStatic.ActivatedEnableDisableHookers.Contains("PnlStage"))
                return;

            if (HintButton != null)
                return;

            HintButton = CreateButton("ArchipelagoHintButton", "Get Hint", new Vector2(220, 65), new Vector2(0, 0.5f), ArchipelagoStatic.SessionHandler.HintHandler.ShowHintPopup);
            HintButtonComp = HintButton.GetComponent<Button>();

            HideSongsButton = CreateButton("ArchipelagoHideButton", "Show Locked Songs", new Vector2(-220, 65), new Vector2(1, 0.5f), ArchipelagoStatic.SessionHandler.ItemHandler.ToggleHiddenSongs);
            HideSongsButtonComp = HintButton.GetComponent<Button>();
            HideSongsText = HideSongsButton.GetComponentInChildren<Text>();

            AddHintBox();
        }

        public void MainSceneLoaded() {
            HintButton = null;
            HideSongsButton = null;
            HideSongsText = null;
        }

        public void AddHintBox() {
            //Todo: This needs a bit of cleaning up. Maybe split into other methods to make it easier to follow.

            var pnlStage = ArchipelagoStatic.SongSelectPanel;
            var likeButton = pnlStage.gameObject.GetComponentInChildren<StageLikeToggle>();

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
            hintTransform.anchoredPosition = new Vector2(400, 160);
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

        private GameObject CreateButton(string buttonName, string buttonText, Vector2 offset, Vector2 pivot, Action onClick) {
            var pnlStage = ArchipelagoStatic.SongSelectPanel;
            var likeButton = pnlStage.gameObject.GetComponentInChildren<StageLikeToggle>();

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

            var colours = yesButton.colors;
            colours.disabledColor = new UnityEngine.Color(colours.disabledColor.r * 0.8f, colours.disabledColor.g * 0.8f, colours.disabledColor.b * 0.8f, 1);
            button.colors = colours;

            var image = buttonParent.AddComponent<Image>();
            image.sprite = yesButtonImage.sprite;
            image.type = yesButtonImage.type;
            button.targetGraphic = image;

            var imageTransform = buttonParent.GetComponent<RectTransform>();
            var yesButtonTransform = yesButton.gameObject.GetComponent<RectTransform>();

            imageTransform.sizeDelta = yesButtonTransform.sizeDelta;
            imageTransform.anchorMax = imageTransform.anchorMin = new Vector2(0.5f, 0.5f);
            imageTransform.anchoredPosition = offset;
            imageTransform.pivot = pivot;

            var hintButtonText = new GameObject("Text");
            hintButtonText.transform.SetParent(buttonParent.transform, false);
            var hintTextComp = hintButtonText.AddComponent<Text>();

            AssetHelpers.CopyTextVariables(yesTextComp, hintTextComp);
            hintTextComp.fontSize -= 10;
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
