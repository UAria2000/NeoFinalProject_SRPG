using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.CloudSave;
using Unity.Services.Economy;
using Unity.Services.Economy.Model;

public class MarketplaceManager : MonoBehaviour
{
    [Serializable]
    public class MarketListing
    {
        public string listingId;
        public string sellerId;
        public string unitType;
        public string instanceId;
        public int price;
    }

    [Serializable]
    private class SerializationWrapper<T>
    {
        public List<T> items;
    }

    private async void Start()
    {
        await InitializeServicesAsync();
    }

    /// <summary>
    /// 유니티 서비스 초기화 및 익명 인증
    /// </summary>
    private async Task InitializeServicesAsync()
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[Marketplace] Signed in as: {AuthenticationService.Instance.PlayerId}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Marketplace] Initialization Failed: {e.Message}");
        }
    }

    #region Cloud Code 호출 (서버 로직 실행)

    /// <summary>
    /// 병사를 거래소에 판매 등록합니다.
    /// </summary>
    public async Task ListUnitAsync(string unitInstanceId, int price)
    {
        try
        {
            var args = new Dictionary<string, object>
            {
                { "unitInstanceId", unitInstanceId },
                { "price", price }
            };

            await CloudCodeService.Instance.CallModuleEndpointAsync("Marketplace", "ListUnit", args);
            Debug.Log("[Marketplace] Unit listed successfully.");
        }
        catch (CloudCodeException e)
        {
            Debug.LogError($"[Marketplace] Listing failed: {e.Message}");
        }
    }

    /// <summary>
    /// 본인이 올린 매물을 취소하고 병사를 회수합니다.
    /// </summary>
    public async Task CancelListingAsync(string listingId)
    {
        try
        {
            var args = new Dictionary<string, object> { { "listingId", listingId } };
            await CloudCodeService.Instance.CallModuleEndpointAsync("Marketplace", "CancelListing", args);
            Debug.Log("[Marketplace] Listing cancelled.");
        }
        catch (CloudCodeException e)
        {
            Debug.LogError($"[Marketplace] Cancellation failed: {e.Message}");
        }
    }

    /// <summary>
    /// 거래소의 매물을 구매합니다.
    /// </summary>
    public async Task PurchaseUnitAsync(string listingId)
    {
        try
        {
            var args = new Dictionary<string, object> { { "listingId", listingId } };
            await CloudCodeService.Instance.CallModuleEndpointAsync("Marketplace", "PurchaseUnit", args);
            Debug.Log("[Marketplace] Purchase successful.");
        }
        catch (CloudCodeException e)
        {
            Debug.LogError($"[Marketplace] Purchase failed: {e.Message}");
        }
    }

    #endregion

    #region 데이터 조회 (Cloud Save - LoadAsync 사용)

    /// <summary>
    /// 현재 거래소에 등록된 모든 매물 목록을 가져옵니다.
    /// </summary>
    public async Task<List<MarketListing>> GetAllListingsAsync()
    {
        try
        {
            var keys = new HashSet<string> { "MARKET_LIST" };

            // 핵심 수정: GetAsync -> LoadAsync (Unity 6 호환)
            var data = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            if (data.TryGetValue("MARKET_LIST", out var item))
            {
                // JsonUtility는 최상위 배열을 지원하지 않으므로 래퍼 형식을 사용합니다.
                string json = "{\"items\":" + item.Value.GetAsString() + "}";
                return JsonUtility.FromJson<SerializationWrapper<MarketListing>>(json).items;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Marketplace] Failed to fetch listings: {e.Message}");
        }

        return new List<MarketListing>();
    }

    #endregion
}