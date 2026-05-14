using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TutorialOverlayUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button clickCatcherButton;

    [Header("Image Tutorial")]
    [SerializeField] private Image tutorialImage;
    [SerializeField] private Sprite[] tutorialSprites = new Sprite[16];

    [Header("Final Message")]
    [SerializeField] private GameObject finalMessageRoot;
    [SerializeField] private TMP_Text finalTitleText;
    [SerializeField] private TMP_Text finalBodyText;
    [SerializeField] private string finalTitle = "축하합니다";
    [TextArea(2, 6)]
    [SerializeField] private string finalBody = "첫 세계를 모두 수확하셨습니다\n\n이제 이 쓸모없어진 세상을 떠나 다음 세계를 찾아나서십시오";

    private bool waitingForClick;
    private bool suppressHideOnAwake;

    private void Awake()
    {
        if (root == null)
            root = gameObject;
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (clickCatcherButton != null)
        {
            clickCatcherButton.onClick.RemoveListener(HandleClick);
            clickCatcherButton.onClick.AddListener(HandleClick);
        }

        ConfigureRaycastTargets();

        if (!suppressHideOnAwake)
            HideImmediate();
    }

    private void OnDestroy()
    {
        if (clickCatcherButton != null)
            clickCatcherButton.onClick.RemoveListener(HandleClick);
    }

    public Sprite GetSpriteByStepNumber(int stepNumber)
    {
        int index = stepNumber - 1;
        if (tutorialSprites == null || index < 0 || index >= tutorialSprites.Length)
            return null;
        return tutorialSprites[index];
    }

    public IEnumerator ShowSpriteSequence(Func<int, bool> shouldShowStep, Action<int> markShown, params int[] stepNumbers)
    {
        if (stepNumbers == null || stepNumbers.Length == 0)
            yield break;

        bool showedAny = false;

        for (int i = 0; i < stepNumbers.Length; i++)
        {
            int step = stepNumbers[i];
            if (shouldShowStep != null && !shouldShowStep(step))
                continue;

            Sprite sprite = GetSpriteByStepNumber(step);
            if (sprite == null)
            {
                Debug.LogWarning($"[TutorialOverlayUI] Tutorial sprite for step {step} is missing. The step will not be marked as shown.");
                continue;
            }

            showedAny = true;
            yield return ShowSingleSprite(sprite);
            markShown?.Invoke(step);
        }

        if (showedAny)
            HideImmediate();
    }

    public IEnumerator ShowFinalMessage(Action onClicked)
    {
        PrepareVisible();

        if (tutorialImage != null)
            tutorialImage.gameObject.SetActive(false);
        if (finalMessageRoot != null)
            finalMessageRoot.SetActive(true);
        if (finalTitleText != null)
            finalTitleText.text = finalTitle;
        if (finalBodyText != null)
            finalBodyText.text = finalBody;

        waitingForClick = true;
        while (waitingForClick)
            yield return null;

        HideImmediate();
        onClicked?.Invoke();
    }

    private IEnumerator ShowSingleSprite(Sprite sprite)
    {
        PrepareVisible();

        if (finalMessageRoot != null)
            finalMessageRoot.SetActive(false);
        if (tutorialImage != null)
        {
            tutorialImage.gameObject.SetActive(true);
            tutorialImage.sprite = sprite;
            tutorialImage.preserveAspect = true;
        }

        waitingForClick = true;
        while (waitingForClick)
            yield return null;
    }

    private void PrepareVisible()
    {
        ConfigureRaycastTargets();

        if (root != null)
        {
            suppressHideOnAwake = true;
            root.SetActive(true);
            suppressHideOnAwake = false;
        }
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    private void ConfigureRaycastTargets()
    {
        if (tutorialImage != null)
            tutorialImage.raycastTarget = false;
        if (finalTitleText != null)
            finalTitleText.raycastTarget = false;
        if (finalBodyText != null)
            finalBodyText.raycastTarget = false;
    }

    public void HideImmediate()
    {
        waitingForClick = false;
        if (tutorialImage != null)
        {
            tutorialImage.sprite = null;
            tutorialImage.gameObject.SetActive(false);
        }
        if (finalMessageRoot != null)
            finalMessageRoot.SetActive(false);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        if (root != null)
            root.SetActive(false);
    }

    private void HandleClick()
    {
        waitingForClick = false;
    }
}
