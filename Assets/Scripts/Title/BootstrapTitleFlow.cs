using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BootstrapTitleFlow : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private GameObject titleRoot;
    [SerializeField] private GameObject newWorldSetupRoot;

    [Header("Main Menu Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newWorldButton;
    [SerializeField] private Button deleteAccountButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    [Header("Main Menu Disabled Overlays")]
    [Tooltip("진행 중인 월드가 없을 때 계속하기 버튼 위에 켜둘 비활성화 Dim 오브젝트입니다.")]
    [SerializeField] private GameObject continueDisabledDim;

    [Header("Difficulty Buttons")]
    [SerializeField] private Button easyButton;
    [SerializeField] private Button normalButton;
    [SerializeField] private Button hardButton;

    [Header("Difficulty Optional Visuals")]
    [Tooltip("선택 상태에서 직접 스프라이트를 바꾸고 싶을 때만 연결합니다. 월드맵 Hover/Press 버튼 UI만 사용할 경우 비워둬도 됩니다.")]
    [SerializeField] private Image easyButtonImage;
    [SerializeField] private Image normalButtonImage;
    [SerializeField] private Image hardButtonImage;
    [SerializeField] private Sprite difficultyNormalSprite;
    [SerializeField] private Sprite difficultySelectedSprite;
    [Tooltip("선택 표시용 오브젝트가 따로 있으면 연결합니다. 선택된 난이도만 켜집니다.")]
    [SerializeField] private GameObject easySelectedRoot;
    [SerializeField] private GameObject normalSelectedRoot;
    [SerializeField] private GameObject hardSelectedRoot;

    [Header("Map Size Slider")]
    [SerializeField] private Slider mapSizeSlider;
    [SerializeField] private TMP_Text mapSizeValueText;
    [SerializeField] private string smallLabel = "소형";
    [SerializeField] private string mediumLabel = "중형";
    [SerializeField] private string largeLabel = "대형";

    [Header("Setup Buttons")]
    [SerializeField] private Button startWorldButton;
    [Tooltip("난이도/크기 선택이 완료되지 않아 새 세계 생성 버튼이 잠겨 있을 때 버튼 위에 켜둘 Dim 오브젝트입니다.")]
    [SerializeField] private GameObject startWorldDisabledDim;
    [SerializeField] private Button backButton;

    [Header("Texts")]
    [SerializeField] private TMP_Text accountText;
    [SerializeField] private TMP_Text versionText;
    [SerializeField] private string versionLabel = "v.0.01.a";

    [Header("Confirm Popup")]
    [SerializeField] private BootstrapConfirmPopupUI overwriteWorldConfirmPopup;
    [SerializeField] private BootstrapConfirmPopupUI deleteAccountConfirmPopup;

    [Header("Overwrite World Popup Text")]
    [TextArea(4, 10)]
    [SerializeField]
    private string overwriteWorldMessage =
        "이미 진행 중인 세계가 있습니다.\n\n" +
        "새 세계를 시작하면 현재 세계는 결산 없이 폐기됩니다.\n" +
        "현재 세계의 아이템, 포로, 전투 기록, 퀘스트 진행도는 정산되지 않습니다.\n" +
        "또한 세계 실패 처리로 인해 다음 세계의 시작 마나가 감소할 수 있습니다.\n\n" +
        "정말 새 세계를 시작하시겠습니까?";

    [SerializeField] private string overwriteWorldConfirmLabel = "동의";
    [SerializeField] private string overwriteWorldCancelLabel = "취소";

    [Header("Delete Account Progress Popup Text")]
    [TextArea(4, 10)]
    [SerializeField]
    private string deleteAccountMessage =
        "계정 진행 데이터를 삭제하시겠습니까?\n\n" +
        "저장된 세계, 군단, 창고, 재화, 진행 기록이 삭제됩니다.\n" +
        "삭제된 데이터는 복구할 수 없습니다.";

    [SerializeField] private string deleteAccountConfirmLabel = "동의";
    [SerializeField] private string deleteAccountCancelLabel = "취소";

    [Header("Scene")]
    [SerializeField] private string worldMapSceneName = "WorldMap";

    [Header("Screen Transition")]
    [SerializeField] private bool useFadeTransition = true;
    [SerializeField, Min(0f)] private float fadeDuration = 0.18f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Defaults")]
    [SerializeField] private int defaultMapRadius = 5;
    [SerializeField] private bool clearDifficultyWhenOpenSetup = true;

    private SaveCoordinator saveCoordinator;

    private string selectedDifficultyId;
    private int selectedMapRadius = 5;
    private bool hasSelectedMapSize = true;
    private Coroutine transitionRoutine;

    private void Awake()
    {
        saveCoordinator = SaveCoordinator.Instance;

        WireButtons();
        ConfigureMapSizeSlider();

        ShowTitleImmediate();
        RefreshTexts();
        RefreshContinueButton();
        RefreshSetupState();
    }

    private void Start()
    {
        saveCoordinator = SaveCoordinator.Instance;
        RefreshTexts();
        RefreshContinueButton();
        RefreshSetupState();
    }

    private void WireButtons()
    {
        WireButton(continueButton, HandleContinueClicked);
        WireButton(newWorldButton, HandleNewWorldClicked);
        WireButton(deleteAccountButton, HandleDeleteAccountClicked);
        WireButton(settingsButton, HandleSettingsClicked);
        WireButton(exitButton, HandleExitClicked);
        WireButton(startWorldButton, HandleStartWorldClicked);
        WireButton(backButton, HandleBackClicked);

        WireButton(easyButton, () => SelectDifficulty("easy"));
        WireButton(normalButton, () => SelectDifficulty("normal"));
        WireButton(hardButton, () => SelectDifficulty("hard"));
    }

    private void WireButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void ConfigureMapSizeSlider()
    {
        selectedMapRadius = Mathf.Clamp(defaultMapRadius, 4, 6);
        hasSelectedMapSize = true;

        if (mapSizeSlider != null)
        {
            mapSizeSlider.wholeNumbers = true;
            mapSizeSlider.minValue = 0f;
            mapSizeSlider.maxValue = 2f;
            mapSizeSlider.SetValueWithoutNotify(MapRadiusToSliderStep(selectedMapRadius));

            mapSizeSlider.onValueChanged.RemoveAllListeners();
            mapSizeSlider.onValueChanged.AddListener(HandleMapSizeSliderChanged);
        }

        RefreshMapSizeText();
    }

    private int MapRadiusToSliderStep(int radius)
    {
        switch (radius)
        {
            case 4:
                return 0;
            case 6:
                return 2;
            case 5:
            default:
                return 1;
        }
    }

    private void HandleMapSizeSliderChanged(float value)
    {
        hasSelectedMapSize = true;

        int step = Mathf.RoundToInt(value);
        switch (step)
        {
            case 0:
                selectedMapRadius = 4;
                break;
            case 1:
                selectedMapRadius = 5;
                break;
            case 2:
                selectedMapRadius = 6;
                break;
            default:
                selectedMapRadius = 5;
                break;
        }

        RefreshMapSizeText();
        RefreshSetupState();
    }

    private void RefreshMapSizeText()
    {
        if (mapSizeValueText == null)
            return;

        switch (selectedMapRadius)
        {
            case 4:
                mapSizeValueText.text = smallLabel;
                break;
            case 5:
                mapSizeValueText.text = mediumLabel;
                break;
            case 6:
                mapSizeValueText.text = largeLabel;
                break;
            default:
                mapSizeValueText.text = mediumLabel;
                break;
        }
    }

    private void RefreshTexts()
    {
        if (accountText != null)
        {
            string nick = saveCoordinator != null ? saveCoordinator.Nickname : "Player";
            accountText.text = $"환영합니다. {nick}";
        }

        if (versionText != null)
            versionText.text = versionLabel;
    }

    private void RefreshContinueButton()
    {
        bool canContinue = saveCoordinator != null && saveCoordinator.HasSavedActiveWorld();

        if (continueButton != null)
            continueButton.interactable = canContinue;

        if (continueDisabledDim != null)
            continueDisabledDim.SetActive(!canContinue);
    }

    private void RefreshSetupState()
    {
        RefreshDifficultyVisuals();

        bool canStart = !string.IsNullOrEmpty(selectedDifficultyId) && hasSelectedMapSize;

        if (startWorldButton != null)
            startWorldButton.interactable = canStart;

        if (startWorldDisabledDim != null)
            startWorldDisabledDim.SetActive(!canStart);
    }

    private void RefreshDifficultyVisuals()
    {
        bool easySelected = selectedDifficultyId == "easy";
        bool normalSelected = selectedDifficultyId == "normal";
        bool hardSelected = selectedDifficultyId == "hard";

        ApplyDifficultyVisual(easyButtonImage, easySelectedRoot, easySelected);
        ApplyDifficultyVisual(normalButtonImage, normalSelectedRoot, normalSelected);
        ApplyDifficultyVisual(hardButtonImage, hardSelectedRoot, hardSelected);

        ApplyHoverPressToggle(easyButton, easySelected);
        ApplyHoverPressToggle(normalButton, normalSelected);
        ApplyHoverPressToggle(hardButton, hardSelected);
    }

    private static void ApplyHoverPressToggle(Button button, bool selected)
    {
        WorldMapHoverPressButtonUI visual = button != null ? button.GetComponent<WorldMapHoverPressButtonUI>() : null;
        if (visual != null)
            visual.SetToggleOn(selected);
    }

    private void ApplyDifficultyVisual(Image targetImage, GameObject selectedRoot, bool selected)
    {
        if (selectedRoot != null)
            selectedRoot.SetActive(selected);

        if (targetImage == null)
            return;

        Sprite targetSprite = selected ? difficultySelectedSprite : difficultyNormalSprite;
        if (targetSprite != null)
            targetImage.sprite = targetSprite;
    }

    private void SelectDifficulty(string difficultyId)
    {
        selectedDifficultyId = difficultyId;
        RefreshSetupState();
    }

    private void ShowTitleImmediate()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        SetRootVisible(titleRoot, true, 1f, true);
        SetRootVisible(newWorldSetupRoot, false, 0f, false);
    }

    private void ShowTitle()
    {
        TransitionTo(titleRoot, newWorldSetupRoot);
    }

    private void ShowNewWorldSetup()
    {
        if (clearDifficultyWhenOpenSetup)
            selectedDifficultyId = null;

        selectedMapRadius = Mathf.Clamp(defaultMapRadius, 4, 6);
        hasSelectedMapSize = true;

        if (mapSizeSlider != null)
            mapSizeSlider.SetValueWithoutNotify(MapRadiusToSliderStep(selectedMapRadius));

        RefreshSetupState();
        RefreshMapSizeText();
        TransitionTo(newWorldSetupRoot, titleRoot);
    }

    private void TransitionTo(GameObject showRoot, GameObject hideRoot)
    {
        if (!useFadeTransition || fadeDuration <= 0f)
        {
            SetRootVisible(showRoot, true, 1f, true);
            SetRootVisible(hideRoot, false, 0f, false);
            return;
        }

        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(TransitionRoutine(showRoot, hideRoot));
    }

    private IEnumerator TransitionRoutine(GameObject showRoot, GameObject hideRoot)
    {
        CanvasGroup showGroup = PrepareGroup(showRoot, true, 0f, false);
        CanvasGroup hideGroup = PrepareGroup(hideRoot, true, 1f, false);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float curved = fadeCurve != null ? fadeCurve.Evaluate(t) : t;

            if (showGroup != null)
                showGroup.alpha = curved;

            if (hideGroup != null)
                hideGroup.alpha = 1f - curved;

            yield return null;
        }

        SetRootVisible(showRoot, true, 1f, true);
        SetRootVisible(hideRoot, false, 0f, false);
        transitionRoutine = null;
    }

    private CanvasGroup PrepareGroup(GameObject root, bool active, float alpha, bool interactable)
    {
        if (root == null)
            return null;

        root.SetActive(active);
        CanvasGroup group = root.GetComponent<CanvasGroup>();
        if (group == null)
            group = root.AddComponent<CanvasGroup>();

        group.alpha = alpha;
        group.interactable = interactable;
        group.blocksRaycasts = interactable;
        return group;
    }

    private void SetRootVisible(GameObject root, bool active, float alpha, bool interactable)
    {
        if (root == null)
            return;

        root.SetActive(active);
        CanvasGroup group = root.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = alpha;
            group.interactable = interactable;
            group.blocksRaycasts = interactable;
        }
    }

    private void HandleContinueClicked()
    {
        if (saveCoordinator == null || !saveCoordinator.HasSavedActiveWorld())
            return;

        saveCoordinator.QueueContinueWorld();
        SceneManager.LoadScene(worldMapSceneName);
    }

    private void HandleNewWorldClicked()
    {
        if (saveCoordinator == null)
            saveCoordinator = SaveCoordinator.Instance;

        if (saveCoordinator != null && !saveCoordinator.HasCompletedTutorial())
        {
            if (saveCoordinator.HasSavedActiveWorld())
            {
                if (overwriteWorldConfirmPopup != null)
                {
                    overwriteWorldConfirmPopup.Show(
                        overwriteWorldMessage,
                        overwriteWorldConfirmLabel,
                        overwriteWorldCancelLabel,
                        ConfirmOverwriteAndStartTutorialWorld,
                        null);
                }
                else
                {
                    ConfirmOverwriteAndStartTutorialWorld();
                }
                return;
            }

            StartQueuedTutorialWorld();
            return;
        }

        ShowNewWorldSetup();
    }

    private void HandleDeleteAccountClicked()
    {
        BootstrapConfirmPopupUI popup = deleteAccountConfirmPopup != null ? deleteAccountConfirmPopup : overwriteWorldConfirmPopup;
        if (popup != null)
        {
            popup.Show(
                deleteAccountMessage,
                deleteAccountConfirmLabel,
                deleteAccountCancelLabel,
                ConfirmDeleteAccountProgress,
                null);
        }
        else
        {
            ConfirmDeleteAccountProgress();
        }
    }

    private void ConfirmDeleteAccountProgress()
    {
        if (saveCoordinator == null)
            saveCoordinator = SaveCoordinator.Instance;

        saveCoordinator?.DeleteAccountProgressData();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void HandleSettingsClicked()
    {
        Debug.Log("[BootstrapTitleFlow] 타이틀 설정 패널은 아직 기획/연결 전입니다.");
    }

    private void HandleExitClicked()
    {
        if (saveCoordinator == null)
            saveCoordinator = SaveCoordinator.Instance;

        saveCoordinator?.SaveAll();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void HandleBackClicked()
    {
        ShowTitle();
    }

    private void HandleStartWorldClicked()
    {
        if (saveCoordinator == null)
        {
            Debug.LogWarning("[BootstrapTitleFlow] SaveCoordinator is missing.");
            return;
        }

        if (string.IsNullOrEmpty(selectedDifficultyId) || !hasSelectedMapSize)
            return;

        if (saveCoordinator.HasSavedActiveWorld())
        {
            if (overwriteWorldConfirmPopup != null)
            {
                overwriteWorldConfirmPopup.Show(
                    overwriteWorldMessage,
                    overwriteWorldConfirmLabel,
                    overwriteWorldCancelLabel,
                    ConfirmOverwriteAndStartNewWorld,
                    null);
            }
            else
            {
                ConfirmOverwriteAndStartNewWorld();
            }

            return;
        }

    StartQueuedNewWorld();
    }

    private void ConfirmOverwriteAndStartTutorialWorld()
    {
        if (saveCoordinator == null)
            return;

        saveCoordinator.ClearSavedWorldRunAsAbandoned();
        StartQueuedTutorialWorld();
    }

    private void StartQueuedTutorialWorld()
    {
        if (saveCoordinator == null)
            return;

        saveCoordinator.QueueTutorialWorldStart();
        SceneManager.LoadScene(worldMapSceneName);
    }

    private void ConfirmOverwriteAndStartNewWorld()
    {
        if (saveCoordinator == null)
            return;

        saveCoordinator.ClearSavedWorldRunAsAbandoned();
        StartQueuedNewWorld();
    }

    private void StartQueuedNewWorld()
    {
        if (saveCoordinator == null)
            return;

        saveCoordinator.QueueNewWorldStart(selectedDifficultyId, selectedMapRadius);
        SceneManager.LoadScene(worldMapSceneName);
    }
}
