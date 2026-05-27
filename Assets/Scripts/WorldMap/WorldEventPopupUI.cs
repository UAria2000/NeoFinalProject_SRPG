using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldEventPopupUI : MonoBehaviour
{
    private enum PopupRewardMode
    {
        None = 0,
        Treasure = 1,
    }

    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text confirmButtonText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button closeButton;

    [Header("Dim Close")]
    [Tooltip("패널 뒤에 깔리는 딤 오브젝트. 연결하면 팝업 열림/닫힘과 함께 활성화됩니다.")]
    [SerializeField] private GameObject dimRoot;
    [Tooltip("딤 클릭 닫기용 버튼. 비워두면 dimRoot의 Button을 찾고, 없으면 런타임에 추가합니다.")]
    [SerializeField] private Button dimCloseButton;
    [SerializeField] private bool closeOnDimClick = true;

    [Header("Optional Reward Slots")]
    [Tooltip("보물 이벤트 보상을 슬롯으로 보여줄 때 연결한다. 비워두면 기존처럼 확인 시 전부 자동 지급된다.")]
    [SerializeField] private List<WorldEventRewardSlotUI> rewardSlots = new List<WorldEventRewardSlotUI>(4);
    [SerializeField] private GameObject rewardSlotsRoot;
    [SerializeField] private StorageItemTooltipUI itemTooltipUI;
    [SerializeField] private BottomPartySummaryPanelUI bottomPartySummaryPanelUI;

    [Header("Treasure Claim Behaviour")]
    [Tooltip("켜면 보물 팝업의 확인 버튼이 남은 보상 아이템을 모두 창고/인벤토리에 넣고 닫습니다. 끄면 기존처럼 슬롯이 없을 때만 자동 지급합니다.")]
    [SerializeField] private bool claimAllTreasureOnConfirm = true;
    [Tooltip("닫기 버튼/딤 클릭은 보상을 받지 않고 닫는 동작으로 유지합니다. 보상 슬롯을 쓰는 경우 확인 버튼은 일괄 받기, 닫기는 포기/닫기 역할입니다.")]
    [SerializeField] private bool closeButtonDiscardsUnclaimedTreasure = true;

    private Action confirmAction;
    private Action closeAction;

    private PopupRewardMode rewardMode = PopupRewardMode.None;
    private WorldRunManager rewardRunManager;
    private WorldTreasureResult activeTreasure;
    private bool autoGrantTreasureWhenNoRewardSlots;

    public bool HasUsableRewardSlots => rewardSlots != null && rewardSlots.Count > 0;
    public bool IsOpen => root != null ? root.activeInHierarchy : gameObject.activeInHierarchy;

    private void Awake()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(HandleConfirmClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(HandleCloseClicked);
        }

        InitializeDimCloseButton();

        if (bottomPartySummaryPanelUI == null)
            bottomPartySummaryPanelUI = UnityEngine.Object.FindFirstObjectByType<BottomPartySummaryPanelUI>();

        CloseSilently();
    }

    public void Open(string title, string body, string confirmLabel, Action onConfirm, Action onClose = null)
    {
        ClearRewardMode();
        confirmAction = onConfirm;
        closeAction = onClose;

        ApplyTexts(title, body, confirmLabel);
        SetOpen(true);
    }

    public void OpenTreasure(
        string title,
        string body,
        string confirmLabel,
        WorldTreasureResult treasure,
        WorldRunManager runManager,
        Action onConfirmAfterClose,
        Action onClose = null)
    {
        ClearRewardMode();

        rewardMode = PopupRewardMode.Treasure;
        activeTreasure = treasure;
        rewardRunManager = runManager;
        autoGrantTreasureWhenNoRewardSlots = !HasUsableRewardSlots;

        confirmAction = onConfirmAfterClose;
        closeAction = onClose;

        ApplyTexts(title, body, confirmLabel);
        RefreshTreasureRewardSlots();
        SetOpen(true);
    }

    public void CloseSilently()
    {
        confirmAction = null;
        closeAction = null;
        ClearRewardMode();
        SetOpen(false);
    }

    public void HandleRewardSlotClicked(int rewardIndex)
    {
        if (!TryGetTreasureReward(rewardIndex, out WorldTreasureRewardItemEntry reward))
            return;

        if (bottomPartySummaryPanelUI != null && bottomPartySummaryPanelUI.CanUseAsDirectLoadoutItem(reward.item))
        {
            bool handled = bottomPartySummaryPanelUI.TrySelectExternalLoadoutItem(
                this,
                rewardIndex,
                reward.item,
                Mathf.Max(1, reward.amount),
                () => ConsumeTreasureRewardAt(rewardIndex));

            if (handled)
            {
                RefreshTreasureRewardSlots();
                return;
            }
        }

        if (rewardRunManager == null || !rewardRunManager.AddStorageItem(reward.item, Mathf.Max(1, reward.amount)))
            return;

        ConsumeTreasureRewardAt(rewardIndex);
    }

    public bool HandleRewardSlotDragBegin(int rewardIndex)
    {
        if (!TryGetTreasureReward(rewardIndex, out WorldTreasureRewardItemEntry reward))
            return false;

        if (bottomPartySummaryPanelUI == null || !bottomPartySummaryPanelUI.CanUseAsDirectLoadoutItem(reward.item))
            return false;

        bool started = bottomPartySummaryPanelUI.TryBeginExternalLoadoutItemDrag(
            this,
            rewardIndex,
            reward.item,
            Mathf.Max(1, reward.amount),
            () => ConsumeTreasureRewardAt(rewardIndex));

        if (started)
            RefreshTreasureRewardSlots();

        return started;
    }

    public void HandleRewardSlotDragEnd(int rewardIndex)
    {
        if (bottomPartySummaryPanelUI == null)
            return;

        bottomPartySummaryPanelUI.EndExternalLoadoutItemDrag(this, rewardIndex);
        RefreshTreasureRewardSlots();
    }

    public void HandleRewardSlotHoverEnter(ItemDefinition item)
    {
        if (itemTooltipUI == null || item == null)
            return;

        itemTooltipUI.Show(item, false, 0);
    }

    public void HandleRewardSlotHoverExit()
    {
        if (itemTooltipUI == null)
            return;

        itemTooltipUI.Hide();
    }

    private void ApplyTexts(string title, string body, string confirmLabel)
    {
        if (titleText != null)
            titleText.text = title;

        if (bodyText != null)
        {
            bodyText.richText = true;
            bodyText.text = body;
        }

        if (confirmButtonText != null)
            confirmButtonText.text = string.IsNullOrWhiteSpace(confirmLabel) ? "확인" : confirmLabel;
    }

    private void SetOpen(bool open)
    {
        if (dimRoot != null)
            dimRoot.SetActive(open);

        if (dimCloseButton != null)
            dimCloseButton.interactable = open && closeOnDimClick;

        if (root != null)
            root.SetActive(open);
        else
            gameObject.SetActive(open);

        if (bottomPartySummaryPanelUI != null)
            bottomPartySummaryPanelUI.SetExternalMainPanelOpen(open && rewardMode == PopupRewardMode.Treasure);
    }

    private void InitializeDimCloseButton()
    {
        if (dimCloseButton == null && dimRoot != null)
            dimCloseButton = dimRoot.GetComponent<Button>();

        if (dimCloseButton == null && dimRoot != null)
            dimCloseButton = dimRoot.AddComponent<Button>();

        if (dimCloseButton == null)
            return;

        dimCloseButton.onClick.RemoveAllListeners();
        dimCloseButton.onClick.AddListener(HandleDimClicked);
    }

    private void HandleDimClicked()
    {
        if (!closeOnDimClick)
            return;

        HandleCloseClicked();
    }

    private void HandleConfirmClicked()
    {
        if (rewardMode == PopupRewardMode.Treasure && activeTreasure != null && rewardRunManager != null)
        {
            if (claimAllTreasureOnConfirm || autoGrantTreasureWhenNoRewardSlots)
                GrantAllRemainingTreasureRewardsToStorage();
        }

        Action action = confirmAction;
        CloseSilently();
        action?.Invoke();
    }

    private void HandleCloseClicked()
    {
        // 닫기 버튼/딤 클릭은 보상 수령 없이 닫는 동작으로 유지한다.
        // 확인 버튼은 GrantAllRemainingTreasureRewardsToStorage()를 통해 남은 보상을 일괄 수령한다.
        if (rewardMode == PopupRewardMode.Treasure && !closeButtonDiscardsUnclaimedTreasure)
            GrantAllRemainingTreasureRewardsToStorage();

        Action action = closeAction;
        CloseSilently();
        action?.Invoke();
    }

    private bool GrantAllRemainingTreasureRewardsToStorage()
    {
        if (rewardMode != PopupRewardMode.Treasure || activeTreasure == null || rewardRunManager == null)
            return false;

        bool grantedAny = false;

        if (!activeTreasure.soulGranted && activeTreasure.soulAmount > 0)
        {
            rewardRunManager.AddWorldSoul(activeTreasure.soulAmount);
            activeTreasure.soulGranted = true;
            grantedAny = true;
        }

        if (activeTreasure.rewards != null)
        {
            for (int i = 0; i < activeTreasure.rewards.Count; i++)
            {
                WorldTreasureRewardItemEntry reward = activeTreasure.rewards[i];
                if (reward == null || reward.item == null || reward.amount <= 0)
                    continue;

                if (!reward.item.IsInventoryItem())
                {
                    reward.item = null;
                    reward.amount = 0;
                    continue;
                }

                if (rewardRunManager.AddStorageItem(reward.item, Mathf.Max(1, reward.amount)))
                {
                    reward.item = null;
                    reward.amount = 0;
                    grantedAny = true;
                }
            }
        }

        RefreshTreasureRewardSlots();
        return grantedAny;
    }

    private bool TryGetTreasureReward(int rewardIndex, out WorldTreasureRewardItemEntry reward)
    {
        reward = null;

        if (rewardMode != PopupRewardMode.Treasure || activeTreasure == null)
            return false;

        if (activeTreasure.rewards == null || rewardIndex < 0 || rewardIndex >= activeTreasure.rewards.Count)
            return false;

        reward = activeTreasure.rewards[rewardIndex];
        return reward != null && reward.item != null && reward.item.IsInventoryItem() && reward.amount > 0;
    }

    private bool ConsumeTreasureRewardAt(int rewardIndex)
    {
        if (!TryGetTreasureReward(rewardIndex, out WorldTreasureRewardItemEntry reward))
            return false;

        reward.item = null;
        reward.amount = 0;
        RefreshTreasureRewardSlots();
        return true;
    }

    private void RefreshTreasureRewardSlots()
    {
        bool showSlots = rewardMode == PopupRewardMode.Treasure && HasUsableRewardSlots;

        if (rewardSlotsRoot != null)
            rewardSlotsRoot.SetActive(showSlots);

        if (rewardSlots == null)
            return;

        for (int i = 0; i < rewardSlots.Count; i++)
        {
            WorldEventRewardSlotUI slot = rewardSlots[i];
            if (slot == null)
                continue;

            ItemDefinition item = null;
            int amount = 0;

            if (activeTreasure != null && activeTreasure.rewards != null && i < activeTreasure.rewards.Count)
            {
                WorldTreasureRewardItemEntry reward = activeTreasure.rewards[i];
                if (reward != null)
                {
                    if (reward.item != null && reward.item.IsInventoryItem())
                    {
                        item = reward.item;
                        amount = reward.amount;
                    }
                }
            }

            bool selected = bottomPartySummaryPanelUI != null && bottomPartySummaryPanelUI.IsExternalLoadoutItemPending(this, i);
            slot.Bind(this, i, item, amount, canInteract: showSlots && item != null && amount > 0, selected: selected);
        }
    }

    private void ClearRewardMode()
    {
        HandleRewardSlotHoverExit();

        if (bottomPartySummaryPanelUI != null)
            bottomPartySummaryPanelUI.CancelExternalLoadoutItemSource(this);

        rewardMode = PopupRewardMode.None;
        rewardRunManager = null;
        activeTreasure = null;
        autoGrantTreasureWhenNoRewardSlots = false;

        if (rewardSlotsRoot != null)
            rewardSlotsRoot.SetActive(false);

        if (rewardSlots != null)
        {
            for (int i = 0; i < rewardSlots.Count; i++)
                rewardSlots[i]?.Clear();
        }
    }
}
