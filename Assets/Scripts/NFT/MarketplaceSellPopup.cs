using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class MarketplaceSellPopup : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField priceInputField;
    public Button confirmButton;
    public Button cancelButton;

    private Action<int> onConfirmAction;

    private void Awake()
    {
        confirmButton.onClick.AddListener(HandleConfirm);
        cancelButton.onClick.AddListener(Hide);
    }

    public void Show(Action<int> onConfirm)
    {
        onConfirmAction = onConfirm;
        priceInputField.text = ""; // 입력창 초기화
        gameObject.SetActive(true);
    }

    private void HandleConfirm()
    {
        if (int.TryParse(priceInputField.text, out int price))
        {
            if (price <= 0) return; // 0원 이하 판매 방지

            onConfirmAction?.Invoke(price);
            Hide();
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}