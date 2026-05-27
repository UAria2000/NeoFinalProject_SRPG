using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum StorageItemFilterMode
{
    All,
    Equipment,
    Consumable,
    Other,
}

public enum StorageItemSortMode
{
    Recent,
    Name,
}

public enum PrisonerFilterMode
{
    All,
    InProgress,
    Ready,
}

public class StoragePanelUI : MainUIPanelBase
{
    private const int PrisonersPerPage = 5;
    private const int FixedPrisonerPages = 2;
    private const int MaxPrisonerCapacity = PrisonersPerPage * FixedPrisonerPages;

    private const int ItemsPerPage = 30;
    private const int FixedItemPages = 2;
    private const int MaxItemCapacity = ItemsPerPage * FixedItemPages;

    [Header("References")]
    [SerializeField] private StorageItemTooltipUI tooltipUI;
    [SerializeField] private BottomPartySummaryPanelUI bottomPartySummaryPanelUI;

    [Header("Inventory Policy")]
    [Tooltip("차기 구조에서는 인벤토리에 소비 아이템/장비 아이템만 표시합니다.")]
    [SerializeField] private bool inventoryOnlyEquipmentAndConsumables = true;
    [Tooltip("구 포로 카드 영역을 계속 표시해야 하는 임시 디버그 상황에서만 켭니다. 기본값은 꺼짐입니다.")]
    [SerializeField] private bool showLegacyPrisonerSection = false;
    [SerializeField] private GameObject legacyPrisonerSectionRoot;

    [Header("Prisoners - Auto Build")]
    [SerializeField] private RectTransform prisonerCardsRoot;
    [SerializeField] private StoragePrisonerCardUI prisonerCardPrefab;
    [SerializeField] private Button prisonerPrevButton;
    [SerializeField] private Button prisonerNextButton;
    [SerializeField] private TMP_Text prisonerPageText;

    [Header("Items - Auto Build")]
    [SerializeField] private RectTransform itemGridRoot;
    [SerializeField] private StorageItemSlotUI itemSlotPrefab;
    [SerializeField] private Button itemPrevButton;
    [SerializeField] private Button itemNextButton;
    [SerializeField] private TMP_Text itemPageText;

    [Header("Item Filters")]
    [SerializeField] private StorageItemFilterMode itemFilterMode = StorageItemFilterMode.All;
    [SerializeField] private StorageItemSortMode itemSortMode = StorageItemSortMode.Recent;

    [Header("Prisoner Filters")]
    [SerializeField] private PrisonerFilterMode prisonerFilterMode = PrisonerFilterMode.All;

    private readonly List<StoragePrisonerCardUI> runtimePrisonerCards = new List<StoragePrisonerCardUI>();
    private readonly List<StorageItemSlotUI> runtimeItemSlots = new List<StorageItemSlotUI>();

    private int prisonerPageIndex;
    private int itemPageIndex;

    protected override void Awake()
    {
        base.Awake();

        RefreshLegacyPrisonerSectionVisibility();
        EnsureRuntimePrisonerCards();
        EnsureRuntimeItemSlots();

        BindButton(prisonerPrevButton, PrevPrisonerPage);
        BindButton(prisonerNextButton, NextPrisonerPage);
        BindButton(itemPrevButton, PrevItemPage);
        BindButton(itemNextButton, NextItemPage);
    }

    private void Start()
    {
        RefreshAll();
    }

    protected override void OnPanelOpened()
    {
        RefreshLegacyPrisonerSectionVisibility();
        EnsureRuntimePrisonerCards();
        EnsureRuntimeItemSlots();

        if (worldRunManager != null)
            worldRunManager.OnStorageChanged += RefreshAll;

        if (bottomPartySummaryPanelUI != null)
            bottomPartySummaryPanelUI.SetStorageMode(true);

        RefreshAll();
    }

    protected override void OnPanelClosed()
    {
        if (worldRunManager != null)
            worldRunManager.OnStorageChanged -= RefreshAll;

        if (tooltipUI != null)
            tooltipUI.Hide();

        if (bottomPartySummaryPanelUI != null)
            bottomPartySummaryPanelUI.SetStorageMode(false);
    }

    public void SetItemFilterAll() => SetItemFilter(StorageItemFilterMode.All);
    public void SetItemFilterEquipment() => SetItemFilter(StorageItemFilterMode.Equipment);
    public void SetItemFilterConsumable() => SetItemFilter(StorageItemFilterMode.Consumable);
    public void SetItemFilterOther() => SetItemFilter(StorageItemFilterMode.Other); // 구버전 버튼 호환용. 새 정책에서는 기타 아이템이 표시되지 않는다.

    public void SetItemSortRecent() => SetItemSort(StorageItemSortMode.Recent);
    public void SetItemSortName() => SetItemSort(StorageItemSortMode.Name);

    public void SetPrisonerFilterAll() => SetPrisonerFilter(PrisonerFilterMode.All);
    public void SetPrisonerFilterInProgress() => SetPrisonerFilter(PrisonerFilterMode.InProgress);
    public void SetPrisonerFilterReady() => SetPrisonerFilter(PrisonerFilterMode.Ready);

    public void RefreshAll()
    {
        RefreshLegacyPrisonerSectionVisibility();

        if (showLegacyPrisonerSection)
            RefreshPrisoners();
        else
            ClearLegacyPrisonerCards();

        RefreshItems();

        if (bottomPartySummaryPanelUI != null)
            bottomPartySummaryPanelUI.RefreshAll();
    }

    public void HandleItemHovered(StorageItemSlotUI slot, InventoryStackData stack)
    {
        if (tooltipUI == null || slot == null || stack == null || stack.item == null)
            return;

        tooltipUI.Show(stack.item, slot.IsAssignedOrEquipped, slot.ColumnIndexInRow);
    }

    public void HideTooltip()
    {
        if (tooltipUI != null)
            tooltipUI.Hide();
    }

    public void HandleItemClicked(StorageItemSlotUI slot)
    {
        if (slot == null || slot.StackData == null || slot.StackData.item == null)
            return;

        if (bottomPartySummaryPanelUI != null)
            bottomPartySummaryPanelUI.TryHandleStorageItemClicked(slot.StackData.item);

        RefreshAll();
    }

    public void HandleItemDragBegin(StorageItemSlotUI slot)
    {
        if (slot == null || slot.StackData == null || slot.StackData.item == null)
            return;

        bottomPartySummaryPanelUI?.BeginStorageItemDrag(slot.StackData.item);
    }

    public void HandleItemDragEnd(StorageItemSlotUI slot)
    {
        if (slot == null || slot.StackData == null || slot.StackData.item == null)
            return;

        bottomPartySummaryPanelUI?.EndStorageItemDrag(slot.StackData.item);
    }

    public void HandlePrisonerAction(StoragePrisonerCardUI card, PrisonerRuntimeData prisoner)
    {
        if (worldRunManager == null || prisoner == null)
            return;

        if (prisoner.RequiresSoulPayment)
        {
            worldRunManager.TryPaySoulForPrisoner(prisoner);
            RefreshAll();
            return;
        }

        if (prisoner.IsReadyToCorrupt)
        {
            worldRunManager.TryCorruptReadyPrisoner(prisoner);
            RefreshAll();
        }
    }

    private void EnsureRuntimePrisonerCards()
    {
        if (prisonerCardsRoot == null || prisonerCardPrefab == null)
            return;

        runtimePrisonerCards.Clear();

        for (int i = 0; i < prisonerCardsRoot.childCount; i++)
        {
            StoragePrisonerCardUI existing = prisonerCardsRoot.GetChild(i).GetComponent<StoragePrisonerCardUI>();
            if (existing != null)
                runtimePrisonerCards.Add(existing);
        }

        while (runtimePrisonerCards.Count < PrisonersPerPage)
        {
            StoragePrisonerCardUI created = Instantiate(prisonerCardPrefab, prisonerCardsRoot);
            created.name = $"PrisonerCard{runtimePrisonerCards.Count + 1:00}";
            runtimePrisonerCards.Add(created);
        }

        for (int i = 0; i < runtimePrisonerCards.Count; i++)
        {
            if (runtimePrisonerCards[i] != null)
                runtimePrisonerCards[i].gameObject.SetActive(i < PrisonersPerPage);
        }

        if (runtimePrisonerCards.Count > PrisonersPerPage)
            runtimePrisonerCards.RemoveRange(PrisonersPerPage, runtimePrisonerCards.Count - PrisonersPerPage);
    }

    private void EnsureRuntimeItemSlots()
    {
        if (itemGridRoot == null || itemSlotPrefab == null)
            return;

        runtimeItemSlots.Clear();

        for (int i = 0; i < itemGridRoot.childCount; i++)
        {
            StorageItemSlotUI existing = itemGridRoot.GetChild(i).GetComponent<StorageItemSlotUI>();
            if (existing != null)
                runtimeItemSlots.Add(existing);
        }

        while (runtimeItemSlots.Count < ItemsPerPage)
        {
            StorageItemSlotUI created = Instantiate(itemSlotPrefab, itemGridRoot);
            created.name = $"ItemSlot_{runtimeItemSlots.Count + 1:00}";
            runtimeItemSlots.Add(created);
        }

        for (int i = 0; i < runtimeItemSlots.Count; i++)
        {
            if (runtimeItemSlots[i] != null)
                runtimeItemSlots[i].gameObject.SetActive(i < ItemsPerPage);
        }

        if (runtimeItemSlots.Count > ItemsPerPage)
            runtimeItemSlots.RemoveRange(ItemsPerPage, runtimeItemSlots.Count - ItemsPerPage);
    }

    private void RefreshPrisoners()
    {
        EnsureRuntimePrisonerCards();

        List<PrisonerRuntimeData> filtered = BuildFilteredPrisonerList();
        if (filtered.Count > MaxPrisonerCapacity)
            filtered = filtered.Take(MaxPrisonerCapacity).ToList();

        prisonerPageIndex = Mathf.Clamp(prisonerPageIndex, 0, FixedPrisonerPages - 1);
        int start = prisonerPageIndex * PrisonersPerPage;

        for (int i = 0; i < runtimePrisonerCards.Count; i++)
        {
            PrisonerRuntimeData data = (start + i) < filtered.Count ? filtered[start + i] : null;
            runtimePrisonerCards[i].gameObject.SetActive(true);
            runtimePrisonerCards[i].Bind(this, worldRunManager, data);
        }

        RefreshPrisonerPageUI();
    }

    private void RefreshItems()
    {
        EnsureRuntimeItemSlots();

        List<InventoryStackData> filtered = BuildFilteredItemList();
        if (filtered.Count > MaxItemCapacity)
            filtered = filtered.Take(MaxItemCapacity).ToList();

        itemPageIndex = Mathf.Clamp(itemPageIndex, 0, FixedItemPages - 1);
        int start = itemPageIndex * ItemsPerPage;

        Dictionary<ItemDefinition, int> assignedEquipmentRemaining = BuildAssignedEquipmentCountMap(filtered);

        for (int i = 0; i < runtimeItemSlots.Count; i++)
        {
            InventoryStackData stack = (start + i) < filtered.Count ? filtered[start + i] : null;
            int column = i % 10;

            bool assigned = IsDisplayStackAssigned(stack, assignedEquipmentRemaining);

            runtimeItemSlots[i].gameObject.SetActive(true);
            runtimeItemSlots[i].Bind(this, stack, column, assigned);
        }

        RefreshItemPageUI();
    }

    private void RefreshLegacyPrisonerSectionVisibility()
    {
        GameObject root = legacyPrisonerSectionRoot;
        if (root == null && prisonerCardsRoot != null)
            root = prisonerCardsRoot.gameObject;

        if (root != null)
            root.SetActive(showLegacyPrisonerSection);

        if (prisonerPrevButton != null)
            prisonerPrevButton.gameObject.SetActive(showLegacyPrisonerSection && prisonerPageIndex > 0);

        if (prisonerNextButton != null)
            prisonerNextButton.gameObject.SetActive(showLegacyPrisonerSection && prisonerPageIndex < FixedPrisonerPages - 1);

        if (prisonerPageText != null)
            prisonerPageText.gameObject.SetActive(showLegacyPrisonerSection);
    }

    private void ClearLegacyPrisonerCards()
    {
        for (int i = 0; i < runtimePrisonerCards.Count; i++)
        {
            if (runtimePrisonerCards[i] != null)
                runtimePrisonerCards[i].gameObject.SetActive(false);
        }

        RefreshLegacyPrisonerSectionVisibility();
    }

    private Dictionary<ItemDefinition, int> BuildAssignedEquipmentCountMap(List<InventoryStackData> visibleStacks)
    {
        Dictionary<ItemDefinition, int> result = new Dictionary<ItemDefinition, int>();
        if (visibleStacks == null || worldRunManager == null)
            return result;

        for (int i = 0; i < visibleStacks.Count; i++)
        {
            InventoryStackData stack = visibleStacks[i];
            if (stack == null || stack.item == null || !stack.item.IsEquipmentItem())
                continue;

            if (result.ContainsKey(stack.item))
                continue;

            int assignedCount = worldRunManager.GetAssignedEquipmentCount(stack.item);
            if (assignedCount > 0)
                result[stack.item] = assignedCount;
        }

        return result;
    }

    private bool IsDisplayStackAssigned(InventoryStackData stack, Dictionary<ItemDefinition, int> assignedEquipmentRemaining)
    {
        if (stack == null || stack.item == null || worldRunManager == null)
            return false;

        if (stack.item.IsConsumableItem())
            return worldRunManager.IsSharedConsumableAssigned(stack.item);

        if (!stack.item.IsEquipmentItem() || assignedEquipmentRemaining == null)
            return false;

        if (!assignedEquipmentRemaining.TryGetValue(stack.item, out int remaining) || remaining <= 0)
            return false;

        assignedEquipmentRemaining[stack.item] = remaining - 1;
        return true;
    }

    private void RefreshPrisonerPageUI()
    {
        int current = prisonerPageIndex + 1;

        if (prisonerPageText != null)
            prisonerPageText.text = $"{current}/{FixedPrisonerPages}";

        bool showPrev = prisonerPageIndex > 0;
        bool showNext = prisonerPageIndex < FixedPrisonerPages - 1;

        if (prisonerPrevButton != null)
            prisonerPrevButton.gameObject.SetActive(showPrev);

        if (prisonerNextButton != null)
            prisonerNextButton.gameObject.SetActive(showNext);
    }

    private void RefreshItemPageUI()
    {
        int current = itemPageIndex + 1;

        if (itemPageText != null)
            itemPageText.text = $"{current}/{FixedItemPages}";

        bool showPrev = itemPageIndex > 0;
        bool showNext = itemPageIndex < FixedItemPages - 1;

        if (itemPrevButton != null)
            itemPrevButton.gameObject.SetActive(showPrev);

        if (itemNextButton != null)
            itemNextButton.gameObject.SetActive(showNext);
    }

    private List<PrisonerRuntimeData> BuildFilteredPrisonerList()
    {
        List<PrisonerRuntimeData> result = new List<PrisonerRuntimeData>();
        IReadOnlyList<PrisonerRuntimeData> source = worldRunManager != null ? worldRunManager.GetStoragePrisoners() : null;
        if (source == null)
            return result;

        for (int i = 0; i < source.Count; i++)
        {
            PrisonerRuntimeData prisoner = source[i];
            if (prisoner == null)
                continue;

            bool include = prisonerFilterMode == PrisonerFilterMode.All;

            if (prisonerFilterMode == PrisonerFilterMode.InProgress)
                include = !prisoner.IsReadyToCorrupt;
            else if (prisonerFilterMode == PrisonerFilterMode.Ready)
                include = prisoner.IsReadyToCorrupt;

            if (include)
                result.Add(prisoner);
        }

        result.Sort((a, b) => a.captureSequence.CompareTo(b.captureSequence));
        return result;
    }

    private List<InventoryStackData> BuildFilteredItemList()
    {
        List<InventoryStackData> result = new List<InventoryStackData>();
        IReadOnlyList<InventoryStackData> source = worldRunManager != null ? worldRunManager.GetStorageInventory() : null;
        if (source == null)
            return result;

        for (int i = 0; i < source.Count; i++)
        {
            InventoryStackData stack = source[i];
            if (stack == null || stack.item == null || stack.amount <= 0)
                continue;

            if (inventoryOnlyEquipmentAndConsumables && !stack.item.IsInventoryItem())
                continue;

            if (!PassesItemFilter(stack.item))
                continue;

            int amount = Mathf.Max(1, stack.amount);
            if (stack.item.IsEquipmentItem())
            {
                // 장비는 향후 난수 옵션/개별 인스턴스 확장을 고려해 한 칸에 하나씩 표시한다.
                for (int copy = 0; copy < amount; copy++)
                    result.Add(new InventoryStackData { item = stack.item, amount = 1 });
            }
            else
            {
                result.Add(stack);
            }
        }

        if (itemSortMode == StorageItemSortMode.Name)
            result.Sort((a, b) => string.Compare(a.item.itemName, b.item.itemName, System.StringComparison.Ordinal));

        return result;
    }

    private bool PassesItemFilter(ItemDefinition item)
    {
        if (item == null)
            return false;

        if (inventoryOnlyEquipmentAndConsumables && !item.IsInventoryItem())
            return false;

        switch (itemFilterMode)
        {
            case StorageItemFilterMode.Equipment:
                return item.IsEquipmentItem();
            case StorageItemFilterMode.Consumable:
                return item.IsConsumableItem();
            case StorageItemFilterMode.Other:
                return !inventoryOnlyEquipmentAndConsumables && item.mainUICategory == MainUIItemCategory.Other;
            default:
                return !inventoryOnlyEquipmentAndConsumables || item.IsInventoryItem();
        }
    }

    private void SetItemFilter(StorageItemFilterMode mode)
    {
        itemFilterMode = mode;
        itemPageIndex = 0;
        RefreshItems();
    }

    private void SetItemSort(StorageItemSortMode mode)
    {
        itemSortMode = mode;
        itemPageIndex = 0;
        RefreshItems();
    }

    private void SetPrisonerFilter(PrisonerFilterMode mode)
    {
        prisonerFilterMode = mode;
        prisonerPageIndex = 0;
        RefreshPrisoners();
    }

    private void PrevPrisonerPage()
    {
        prisonerPageIndex = Mathf.Max(0, prisonerPageIndex - 1);
        RefreshPrisoners();
    }

    private void NextPrisonerPage()
    {
        prisonerPageIndex = Mathf.Min(FixedPrisonerPages - 1, prisonerPageIndex + 1);
        RefreshPrisoners();
    }

    private void PrevItemPage()
    {
        itemPageIndex = Mathf.Max(0, itemPageIndex - 1);
        RefreshItems();
    }

    private void NextItemPage()
    {
        itemPageIndex = Mathf.Min(FixedItemPages - 1, itemPageIndex + 1);
        RefreshItems();
    }

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }
}