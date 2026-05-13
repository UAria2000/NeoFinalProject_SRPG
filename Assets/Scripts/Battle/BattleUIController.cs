using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleUIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private CurrentUnitInfoPanel currentUnitInfoPanel;
    [SerializeField] private EnemyInfoPanel enemyInfoPanel;
    [SerializeField] private EnemyDetailPopupUI enemyDetailPopupUI;
    [SerializeField] private InventoryPanelUI inventoryPanelUI;
    [SerializeField] private BattleActionWheelUI actionWheelUI;
    [SerializeField] private BattleTurnOrderStripUI turnOrderStripUI;
    [SerializeField] private BattleBackgroundClickCatcherUI backgroundClickCatcherUI;

    [Header("Battle Settings Panel")]
    [SerializeField] private Button settingsButton;
    [SerializeField] private GameObject settingsDimRoot;
    [SerializeField] private Button settingsDimButton;
    [Tooltip("월드맵에서 쓰는 설정 패널 프리팹/오브젝트를 전투 UI 안에도 배치한 뒤 연결합니다.")]
    [SerializeField] private MainUIPanelBase settingsPanel;

    [Header("Bottom Portrait Slots")]
    [SerializeField] private BattleBottomPortraitBarUI allyBottomPortraitBarUI;
    [SerializeField] private BattleBottomPortraitBarUI enemyBottomPortraitBarUI;

    [Header("Tooltips")]
    [SerializeField] private SkillTooltipUI skillTooltipUI;
    [SerializeField] private EnemySkillTooltipUI enemySkillTooltipUI;
    [SerializeField] private TargetPreviewHoverUI targetPreviewHoverUI;
    [SerializeField] private FleeTooltipUI fleeTooltipUI;

    [Header("Legacy Bottom Context Roots")]
    [SerializeField] private GameObject enemyInfoContextRoot;
    [SerializeField] private GameObject inventoryContextRoot;
    [SerializeField] private GameObject mapContextRoot;

    [Header("Legacy Action Buttons")]
    [SerializeField] private Button[] actionButtons = new Button[4];
    [SerializeField] private Image[] actionIcons = new Image[4];
    [SerializeField] private Image[] actionCooldownOverlays = new Image[4];
    [SerializeField] private TMP_Text[] actionCooldownTexts = new TMP_Text[4];
    [SerializeField] private Button moveButton;
    [SerializeField] private Button captureButton;
    [SerializeField] private Image captureButtonImage;
    [SerializeField] private Sprite captureEnabledSprite;
    [SerializeField] private Sprite captureDisabledSprite;
    [SerializeField] private GameObject captureEnabledEffectRoot;
    [SerializeField] private GameObject captureDisabledEffectRoot;
    [SerializeField] private Button fleeButton;
    [SerializeField] private Button endTurnButton;
    [SerializeField] private Button inventoryButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button popupLogButton;
    [SerializeField] private Button mapButton;
    [SerializeField] private Button enemyDetailPopupButton;

    [Header("Round UI")]
    [SerializeField] private TMP_Text turnStartText;
    [SerializeField] private float turnStartTextShowTime = 1.0f;

    [Header("Cancel Button Colors")]
    [SerializeField] private Color cancelDisabledNormal = new Color(0.45f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color cancelDisabledHighlighted = new Color(0.50f, 0.50f, 0.50f, 1f);
    [SerializeField] private Color cancelDisabledPressed = new Color(0.38f, 0.38f, 0.38f, 1f);
    [SerializeField] private Color cancelEnabledNormal = new Color(0.82f, 0.20f, 0.20f, 1f);
    [SerializeField] private Color cancelEnabledHighlighted = new Color(0.92f, 0.28f, 0.28f, 1f);
    [SerializeField] private Color cancelEnabledPressed = new Color(0.66f, 0.12f, 0.12f, 1f);

    private BattleManager battleManager;
    private BattlePresentationController presentationController;

    public void Initialize(BattleManager manager)
    {
        battleManager = manager;

        RefreshRoundText();

        if (settingsPanel != null)
            settingsPanel.ClosePanel();
        if (settingsDimRoot != null)
            settingsDimRoot.SetActive(false);
        BindButton(settingsButton, OpenSettingsPanel);
        BindButton(settingsDimButton, CloseSettingsPanel);

        if (enemyDetailPopupUI != null)
            enemyDetailPopupUI.Hide();

        if (currentUnitInfoPanel != null)
            currentUnitInfoPanel.Hide();

        if (enemyInfoPanel != null)
            enemyInfoPanel.Hide();

        HideSkillTooltip();
        HideEnemySkillTooltip();
        HideTargetPreview();
        HideFleeTooltip();

        SetBottomContext(BottomContextType.Inventory);

        if (actionWheelUI != null)
            actionWheelUI.Initialize(manager);

        if (allyBottomPortraitBarUI != null)
            allyBottomPortraitBarUI.Initialize(manager);
        if (enemyBottomPortraitBarUI != null)
            enemyBottomPortraitBarUI.Initialize(manager);

        RefreshBottomPortraitBars(manager);

        ApplyButtonNavigationNone(moveButton);
        ApplyButtonNavigationNone(captureButton);
        ApplyButtonNavigationNone(fleeButton);
        ApplyButtonNavigationNone(endTurnButton);
        ApplyButtonNavigationNone(inventoryButton);
        ApplyButtonNavigationNone(cancelButton);
        ApplyButtonNavigationNone(popupLogButton);
        ApplyButtonNavigationNone(mapButton);
        ApplyButtonNavigationNone(enemyDetailPopupButton);

        for (int i = 0; i < actionButtons.Length; i++)
            ApplyButtonNavigationNone(actionButtons[i]);
    }


    public void SetPresentationController(BattlePresentationController controller)
    {
        presentationController = controller;
        if (turnOrderStripUI != null)
            turnOrderStripUI.Initialize(controller);
    }

    public void BindButtonEvents()
    {
        if (battleManager == null)
            return;

        for (int i = 0; i < actionButtons.Length; i++)
        {
            int slotIndex = i;
            if (actionButtons[i] == null)
                continue;

            actionButtons[i].onClick.RemoveAllListeners();
            actionButtons[i].onClick.AddListener(delegate { battleManager.OnActionSlotPressed(slotIndex); });

            SkillButtonHoverHandler handler = actionButtons[i].GetComponent<SkillButtonHoverHandler>();
            if (handler == null)
                handler = actionButtons[i].gameObject.AddComponent<SkillButtonHoverHandler>();
            handler.Initialize(battleManager, slotIndex);
        }

        Bind(moveButton, battleManager.OnMoveButtonPressed);
        if (captureButton != null)
        {
            captureButton.onClick.RemoveAllListeners();
            captureButton.onClick.AddListener(battleManager.OnCaptureButtonPressed);
        }

        if (fleeButton != null)
        {
            fleeButton.onClick.RemoveAllListeners();
            fleeButton.onClick.AddListener(battleManager.OnFleeButtonPressed);
            FleeButtonHoverHandler handler = fleeButton.GetComponent<FleeButtonHoverHandler>();
            if (handler == null)
                handler = fleeButton.gameObject.AddComponent<FleeButtonHoverHandler>();
            handler.Initialize(battleManager);
        }

        Bind(endTurnButton, battleManager.OnEndTurnButtonPressed);
        Bind(inventoryButton, battleManager.OnInventoryTogglePressed);
        Bind(cancelButton, battleManager.OnCancelButtonPressed);
        Bind(popupLogButton, battleManager.OnPopupLogButtonPressed);
        Bind(mapButton, battleManager.OnMapButtonPressed);
        Bind(enemyDetailPopupButton, battleManager.OnEnemyDetailPopupButtonPressed);

        if (enemyInfoPanel != null)
            enemyInfoPanel.SetLastWillButtonAction(battleManager.OnEnemyDetailPopupButtonPressed);
    }

    public void BindEnemySkillHoverEvents(GameObject[] enemySkillTargets)
    {
        if (battleManager == null || enemySkillTargets == null)
            return;

        for (int i = 0; i < enemySkillTargets.Length; i++)
        {
            if (enemySkillTargets[i] == null)
                continue;

            EnemySkillButtonHoverHandler handler = enemySkillTargets[i].GetComponent<EnemySkillButtonHoverHandler>();
            if (handler == null)
                handler = enemySkillTargets[i].AddComponent<EnemySkillButtonHoverHandler>();
            handler.Initialize(battleManager, i);
        }
    }

    public void RefreshInfoPanels(BattleUnit ally, BattleUnit enemy)
    {
        if (currentUnitInfoPanel != null)
        {
            if (ally != null) currentUnitInfoPanel.Show(ally);
            else currentUnitInfoPanel.Hide();
        }

        if (enemyInfoPanel != null)
        {
            if (enemy != null) enemyInfoPanel.Show(enemy);
            else enemyInfoPanel.Hide();
        }

        if (enemyDetailPopupUI != null && enemyDetailPopupUI.IsOpen())
        {
            if (enemy != null) enemyDetailPopupUI.Show(enemy);
            else enemyDetailPopupUI.Hide();
        }
    }

    public void RefreshCurrentUnitPanel(BattleUnit unit)
    {
        RefreshInfoPanels(unit, null);
    }

    public void RefreshEnemyPanels(BattleUnit enemy)
    {
        RefreshInfoPanels(null, enemy);
    }

    public void RefreshActionButtons(BattleUnit unit, bool interactable)
    {
        for (int i = 0; i < actionButtons.Length; i++)
        {
            SkillDefinition skill = unit != null ? unit.GetActionSkillAt(i) : null;
            bool hasSkill = skill != null;

            if (i < actionIcons.Length && actionIcons[i] != null)
            {
                actionIcons[i].sprite = hasSkill ? skill.icon : null;
                actionIcons[i].color = hasSkill ? Color.white : new Color(1f, 1f, 1f, 0.2f);
            }

            int remaining = hasSkill ? unit.GetRemainingCooldown(skill) : 0;
            if (i < actionCooldownOverlays.Length && actionCooldownOverlays[i] != null)
            {
                actionCooldownOverlays[i].gameObject.SetActive(hasSkill && remaining > 0);
                actionCooldownOverlays[i].fillAmount = hasSkill && remaining > 0
                    ? Mathf.Clamp01(remaining / Mathf.Max(1f, skill.cooldownTurns))
                    : 0f;
            }
            if (i < actionCooldownTexts.Length && actionCooldownTexts[i] != null)
                actionCooldownTexts[i].text = hasSkill && remaining > 0 ? remaining.ToString() : string.Empty;
            if (actionButtons[i] != null)
                actionButtons[i].interactable = interactable && hasSkill && unit != null && unit.CanUseSkill(skill);
        }

        bool canAct = interactable && battleManager != null && battleManager.InputMode == BattleInputMode.WaitingForAction;
        if (moveButton != null) moveButton.interactable = canAct;
        bool canCapture = canAct && battleManager != null && battleManager.CanActorUseCaptureCommand(unit);
        if (captureButton != null) captureButton.interactable = canCapture;
        if (captureButtonImage != null)
            captureButtonImage.sprite = canCapture && captureEnabledSprite != null ? captureEnabledSprite : captureDisabledSprite;
        if (captureEnabledEffectRoot != null) captureEnabledEffectRoot.SetActive(canCapture);
        if (captureDisabledEffectRoot != null) captureDisabledEffectRoot.SetActive(!canCapture);
        if (fleeButton != null) fleeButton.interactable = canAct && battleManager != null && battleManager.IsMainPlayerCharacter(unit);
        if (endTurnButton != null) endTurnButton.interactable = canAct;
        if (inventoryButton != null) inventoryButton.interactable = true;
        if (mapButton != null) mapButton.interactable = battleManager == null || !battleManager.IsBattleInProgress;
        if (cancelButton != null)
            cancelButton.interactable = battleManager != null && battleManager.CurrentState == TurnState.PlayerInput && battleManager.InputMode != BattleInputMode.WaitingForAction;
        RefreshCancelButtonState();
    }

    public void RefreshActionWheel(BattleUnit unit, bool interactable)
    {
        if (actionWheelUI == null || battleManager == null)
            return;

        actionWheelUI.Refresh(unit, interactable, battleManager.InputMode, battleManager.GetActiveAllyInventory());
    }

    public void RefreshTurnOrderStrip(IReadOnlyList<BattleUnit> order, int currentCursor)
    {
        RefreshRoundText();
        if (turnOrderStripUI != null)
            turnOrderStripUI.Refresh(order, currentCursor);
    }

    public void RefreshRoundText()
    {
        if (turnStartText == null)
            return;

        turnStartText.gameObject.SetActive(true);
        int round = battleManager != null ? Mathf.Max(0, battleManager.CurrentRound) : 0;
        turnStartText.text = round > 0 ? $"Round {round}" : "Round 0";
    }

    public void OpenSettingsPanel()
    {
        if (settingsPanel == null)
            return;

        settingsPanel.OpenPanel();
        if (settingsDimRoot != null)
            settingsDimRoot.SetActive(true);
    }

    public void CloseSettingsPanel()
    {
        if (settingsPanel != null && settingsPanel.IsOpen)
            settingsPanel.ClosePanel();

        if (settingsDimRoot != null)
            settingsDimRoot.SetActive(false);
    }

    public bool IsSettingsPanelOpen()
    {
        return settingsPanel != null && settingsPanel.IsOpen;
    }

    public void RefreshBottomPortraitBars(BattleManager manager)
    {
        if (allyBottomPortraitBarUI != null)
            allyBottomPortraitBarUI.Refresh(manager);
        if (enemyBottomPortraitBarUI != null)
            enemyBottomPortraitBarUI.Refresh(manager);
    }

    public void HandleBlankFieldLeftClick()
    {
        if (currentUnitInfoPanel != null) currentUnitInfoPanel.Hide();
        if (enemyInfoPanel != null) enemyInfoPanel.Hide();
        if (enemyDetailPopupUI != null) enemyDetailPopupUI.Hide();
        // 액션휠은 기존 규칙대로 우클릭/전용 버튼으로만 닫는다.
    }

    public void HandleCurrentActorClicked(BattleUnit unit)
    {
        if (actionWheelUI != null)
            actionWheelUI.HandleCurrentActorClicked(unit);
    }

    public void RefreshInventory(BattleManager manager, List<InventoryStackData> stacks, int selectedIndex)
    {
        if (inventoryPanelUI != null)
            inventoryPanelUI.Bind(manager, stacks, selectedIndex);
    }

    public void SetBottomContext(BottomContextType mode)
    {
        bool showEnemyInfo = mode == BottomContextType.EnemyInfo;
        bool showInventory = mode == BottomContextType.Inventory;
        bool showMap = mode == BottomContextType.Map;

        if (enemyInfoContextRoot != null)
            enemyInfoContextRoot.SetActive(showEnemyInfo);
        if (inventoryContextRoot != null)
            inventoryContextRoot.SetActive(showInventory);
        if (mapContextRoot != null)
            mapContextRoot.SetActive(showMap);
        if (inventoryPanelUI != null)
            inventoryPanelUI.Show(showInventory);

        if (!showEnemyInfo && enemyDetailPopupUI != null && enemyDetailPopupUI.IsOpen())
            enemyDetailPopupUI.Hide();
    }

    public void ShowEnemyDetailPopup(BattleUnit enemy)
    {
        if (enemyInfoContextRoot != null)
            enemyInfoContextRoot.SetActive(true);
        if (inventoryContextRoot != null)
            inventoryContextRoot.SetActive(false);
        if (mapContextRoot != null)
            mapContextRoot.SetActive(false);
        if (inventoryPanelUI != null)
            inventoryPanelUI.Show(false);
        if (enemyDetailPopupUI != null)
            enemyDetailPopupUI.Show(enemy);
    }

    public void ToggleEnemyDetailPopup(BattleUnit enemy)
    {
        if (enemyDetailPopupUI == null)
            return;

        if (enemyDetailPopupUI.IsOpen()) HideEnemyDetailPopup();
        else ShowEnemyDetailPopup(enemy);
    }

    public void HideEnemyDetailPopup()
    {
        if (enemyDetailPopupUI != null)
            enemyDetailPopupUI.Hide();
    }

    public bool IsEnemyDetailPopupOpen()
    {
        return enemyDetailPopupUI != null && enemyDetailPopupUI.IsOpen();
    }

    public void ShowPlayerSkillTooltip(SkillDefinition skill, Vector3 screenPosition)
    {
        if (skillTooltipUI != null)
            skillTooltipUI.Show(skill, screenPosition);
    }

    public void HideSkillTooltip()
    {
        if (skillTooltipUI != null)
            skillTooltipUI.Hide();
    }

    public void ShowEnemySkillTooltip(SkillDefinition skill, Vector3 screenPosition)
    {
        if (enemySkillTooltipUI != null)
            enemySkillTooltipUI.Show(skill, screenPosition);
    }

    public void HideEnemySkillTooltip()
    {
        if (enemySkillTooltipUI != null)
            enemySkillTooltipUI.Hide();
    }

    public void ShowTargetPreview(TargetPreviewData data, Vector3 screenPosition)
    {
        if (targetPreviewHoverUI != null)
            targetPreviewHoverUI.Show(data, screenPosition);
    }

    public void HideTargetPreview()
    {
        if (targetPreviewHoverUI != null)
            targetPreviewHoverUI.Hide();
    }

    public void ShowFleeTooltip(int fleeChancePercent, Vector3 screenPosition)
    {
        if (fleeTooltipUI != null)
            fleeTooltipUI.Show(fleeChancePercent, screenPosition);
    }

    public void HideFleeTooltip()
    {
        if (fleeTooltipUI != null)
            fleeTooltipUI.Hide();
    }

    public IEnumerator ShowTurnStartTextRoutine(int round)
    {
        RefreshRoundText();
        if (turnStartText != null)
        {
            turnStartText.gameObject.SetActive(true);
            turnStartText.text = $"Round {Mathf.Max(0, round)}";
        }

        // 라운드 표시는 전투 중 항상 켜 둔다. 기존 코루틴 호출부 호환을 위해 한 프레임만 반환한다.
        yield return null;
    }

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        if (action != null)
            button.onClick.AddListener(action);
    }

    private void RefreshCancelButtonState()
    {
        if (cancelButton == null || battleManager == null)
            return;

        bool canCancel = battleManager.CurrentState == TurnState.PlayerInput &&
                         (battleManager.InputMode == BattleInputMode.WaitingForSkillTarget ||
                          battleManager.InputMode == BattleInputMode.WaitingForMoveTarget ||
                          battleManager.InputMode == BattleInputMode.WaitingForItemTarget ||
                          battleManager.InputMode == BattleInputMode.WaitingForCaptureTarget);

        cancelButton.interactable = canCancel;
        ColorBlock colors = cancelButton.colors;
        if (canCancel)
        {
            colors.normalColor = cancelEnabledNormal;
            colors.highlightedColor = cancelEnabledHighlighted;
            colors.pressedColor = cancelEnabledPressed;
            colors.selectedColor = cancelEnabledNormal;
        }
        else
        {
            colors.normalColor = cancelDisabledNormal;
            colors.highlightedColor = cancelDisabledHighlighted;
            colors.pressedColor = cancelDisabledPressed;
            colors.selectedColor = cancelDisabledNormal;
        }

        cancelButton.colors = colors;
    }

    private void ApplyButtonNavigationNone(Button button)
    {
        if (button == null)
            return;

        Navigation nav = button.navigation;
        nav.mode = Navigation.Mode.None;
        button.navigation = nav;
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }
}
