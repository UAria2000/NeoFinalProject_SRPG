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

    [Header("Owned Assets (Right)")]
    public TextMeshProUGUI goldText;

    [Header("Detail Panel")]
    public UnitDetailPanel detailPanel; // 인스펙터에서 할당

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
        if (goldText == null || marketplaceManager == null) return;

        try
        {
            // 1. Unity Economy 서버에서 최신 잔액 가져오기
            var balances = await EconomyService.Instance.PlayerBalances.GetBalancesAsync();
            var goldBalance = balances.Balances.Find(b => b.CurrencyId == "GOLD");

            if (goldBalance != null)
            {
                // 2. 서버에서 받은 값을 PersistentProfileController에 동기화
                // 인스펙터에서 persistentProfileController가 연결되어 있어야 합니다.
                int currentGold = (int)goldBalance.Balance;
                persistentProfileController.UpdateGoldBalance(currentGold);

                // 3. UI 텍스트 업데이트 (이제 로컬 컨트롤러의 값을 신뢰함)
                goldText.text = $"{currentGold:N0} GOLD";
                Debug.Log($"[Marketplace] 서버-로컬 골드 동기화 완료: {currentGold}");
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

            var profileData = SaveCoordinator.Instance.LoadProfileData();
            if (profileData == null || profileData.rosterUnits == null) return;

            // 파티 편성 중인 ID 리스트를 미리 가져옵니다.
            HashSet<string> activePartyIds = new HashSet<string>(profileData.activePartyUnitInstanceIds);

            foreach (var unit in profileData.rosterUnits)
            {
                // 1. NFT 태그가 달려 있는 병사만 출력
                if (!unit.isNft) continue;

                // 2. 즐겨찾기 등록된 병사는 제외
                if (unit.isFavorite) continue;

                // 3. 파티에 편성 중인 병사는 제외
                if (activePartyIds.Contains(unit.unitInstanceId)) continue;

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

        // 1. SaveCoordinator에서 해당 병사의 전체 데이터를 찾습니다.
        var profileData = SaveCoordinator.Instance.LoadProfileData();
        var unitData = profileData.rosterUnits.Find(u => u.unitInstanceId == instanceId);

        // 2. 상세 정보 패널 갱신
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