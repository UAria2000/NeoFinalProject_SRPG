using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Economy;
using Unity.Services.Economy.Model;
using Unity.Services.Core;

public class MarketplaceUI : MonoBehaviour
{
    [Header("Manager Reference")]
    public MarketplaceManager marketplaceManager;
    public PersistentProfileController persistentProfileController;

    [Header("Owned Assets (Right)")]
    public TextMeshProUGUI goldText;

    [Header("Detail Panel")]
    public UnitDetailPanel detailPanel;

    [Header("Inventory (Left)")]
    public Transform inventoryContent;
    public GameObject inventorySlotPrefab;

    [Header("Market Store (Top-Center)")]
    public Transform marketContent;
    public GameObject marketSlotPrefab;

    [Header("Buttons (Bottom-Left)")]
    public Button sellButton;
    public Button buyButton;
    public Button cancelButton;

    [Header("Popups")]
    public MarketplaceSellPopup sellPopup;

    private string selectedInstanceId;
    private MarketplaceManager.MarketListing selectedListing;
    private SoldierCard currentSelectedCard;

    private async void OnEnable()
    {
        try
        {
            while (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await Task.Yield();
            }

            AuthenticationService.Instance.SignedIn += HandleSignedIn;

            if (AuthenticationService.Instance.IsSignedIn)
            {
                await RefreshAllUI();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Marketplace] 초기화 중 오류: {e.Message}");
        }
    }

    private void OnDisable()
    {
        if (UnityServices.State != ServicesInitializationState.Uninitialized && AuthenticationService.Instance != null)
        {
            AuthenticationService.Instance.SignedIn -= HandleSignedIn;
        }
    }

    private async void HandleSignedIn()
    {
        await RefreshAllUI();
    }

    public async Task RefreshAllUI()
    {
        await UpdateGoldUI();
        await PopulateInventory();
        await PopulateMarket();
        ResetSelection();
    }

    private async Task UpdateGoldUI()
    {
        if (goldText == null || marketplaceManager == null || persistentProfileController == null) return;

        try
        {
            var balances = await EconomyService.Instance.PlayerBalances.GetBalancesAsync();
            var goldBalance = balances.Balances.Find(b => b.CurrencyId == "GOLD");

            if (goldBalance != null)
            {
                int currentGold = (int)goldBalance.Balance;
                persistentProfileController.UpdateGoldBalance(currentGold);
                goldText.text = $"{currentGold:N0} GOLD";
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Marketplace] 골드 연동 중 오류: {e.Message}");
        }
    }

    private async Task PopulateInventory()
    {
        if (inventoryContent == null) return;
        await Task.Yield();

        try
        {
            foreach (Transform child in inventoryContent) Destroy(child.gameObject);

            // 1. 파일로부터 세이브 데이터 로드
            var profileData = SaveCoordinator.Instance.LoadProfileData();
            if (profileData == null || profileData.rosterUnits == null)
            {
                Debug.LogWarning("[Marketplace] 로드할 병사 데이터가 세이브 파일에 없습니다.");
                return;
            }

            // 2. 변수 선언 (오류 해결 핵심): 필터링에 사용할 HashSet을 루프 시작 전에 미리 선언
            List<string> partyIds = profileData.activePartyUnitInstanceIds ?? new List<string>();
            HashSet<string> activePartyIds = new HashSet<string>(partyIds);

            Debug.Log($"[Marketplace] 로드된 총 유닛 수: {profileData.rosterUnits.Count}");

            foreach (var unit in profileData.rosterUnits)
            {
                // 필터링 조건 체크 로그 (디버깅용)
                bool isNft = unit.isNft;
                bool isFavorite = unit.isFavorite;
                bool inParty = activePartyIds.Contains(unit.unitInstanceId);

                // 필터 적용
                if (!isNft) continue; // NFT가 아니면 제외
                if (isFavorite) continue; // 즐겨찾기면 제외
                if (inParty) continue; // 파티 중이면 제외

                var slot = Instantiate(inventorySlotPrefab, inventoryContent);
                var card = slot.GetComponent<SoldierCard>();
                if (card != null)
                {
                    card.SetupCard(unit);
                    slot.GetComponent<Button>().onClick.AddListener(() =>
                        SelectInventoryItem(card, unit.unitInstanceId));
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Marketplace] 인벤토리 로드 실패: {e.Message}");
        }
    }

    private async Task PopulateMarket()
    {
        if (marketContent == null) return;
        await Task.Yield();

        try
        {
            foreach (Transform child in marketContent) Destroy(child.gameObject);

            var listings = await marketplaceManager.GetAllListingsAsync();
            foreach (var listing in listings)
            {
                var slot = Instantiate(marketSlotPrefab, marketContent);
                var card = slot.GetComponent<SoldierCard>();
                if (card != null)
                {
                    card.SetupCard(listing.unitType, listing.price);
                    slot.GetComponent<Button>().onClick.AddListener(() => SelectMarketItem(card, listing));
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Marketplace] 상점 로드 실패: {e.Message}");
        }
    }

    private void UpdateHighlight(SoldierCard targetCard)
    {
        if (currentSelectedCard != null) currentSelectedCard.SetHighlight(false);
        currentSelectedCard = targetCard;
        if (currentSelectedCard != null) currentSelectedCard.SetHighlight(true);
    }

    private void SelectInventoryItem(SoldierCard clickedCard, string instanceId)
    {
        UpdateHighlight(clickedCard);
        selectedInstanceId = instanceId;

        var profileData = SaveCoordinator.Instance.LoadProfileData();
        var unitData = profileData.rosterUnits.Find(u => u.unitInstanceId == instanceId);

        if (unitData != null && detailPanel != null)
        {
            detailPanel.Setup(unitData);
        }

        if (sellButton != null) sellButton.gameObject.SetActive(true);
        if (buyButton != null) buyButton.gameObject.SetActive(false);
        if (cancelButton != null) cancelButton.gameObject.SetActive(false);
    }

    private void SelectMarketItem(SoldierCard clickedCard, MarketplaceManager.MarketListing listing)
    {
        UpdateHighlight(clickedCard);
        selectedListing = listing;
        selectedInstanceId = null;

        bool isMine = listing.sellerId == AuthenticationService.Instance.PlayerId;

        if (sellButton != null) sellButton.gameObject.SetActive(false);
        if (buyButton != null) buyButton.gameObject.SetActive(!isMine);
        if (cancelButton != null) cancelButton.gameObject.SetActive(isMine);
    }

    private void ResetSelection()
    {
        if (currentSelectedCard != null) currentSelectedCard.SetHighlight(false);
        currentSelectedCard = null;
        selectedInstanceId = null;
        selectedListing = null;

        if (sellButton != null) sellButton.gameObject.SetActive(false);
        if (buyButton != null) buyButton.gameObject.SetActive(false);
        if (cancelButton != null) cancelButton.gameObject.SetActive(false);
    }

    public void OnClickSellButton()
    {
        if (string.IsNullOrEmpty(selectedInstanceId) || sellPopup == null) return;
        sellPopup.Show((price) => ExecuteSellRequest(price));
    }

    private async void ExecuteSellRequest(int price)
    {
        await marketplaceManager.ListUnitAsync(selectedInstanceId, price);
        await RefreshAllUI();
    }

    public async void OnClickBuy()
    {
        if (selectedListing == null) return;
        await marketplaceManager.PurchaseUnitAsync(selectedListing.listingId);
        await RefreshAllUI();
    }

    public async void OnClickCancel()
    {
        if (selectedListing == null) return;
        await marketplaceManager.CancelListingAsync(selectedListing.listingId);
        await RefreshAllUI();
    }
}