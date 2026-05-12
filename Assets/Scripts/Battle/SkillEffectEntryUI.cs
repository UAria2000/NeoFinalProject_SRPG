using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SkillEffectEntryUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text labelText;

    private void Awake()
    {
        AutoWireIfNeeded();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoWireIfNeeded();
    }
#endif

    public void Set(Sprite icon, string text)
    {
        AutoWireIfNeeded();

        if (iconImage != null)
        {
            bool hasIcon = icon != null;
            iconImage.gameObject.SetActive(hasIcon);
            iconImage.sprite = hasIcon ? icon : null;
            iconImage.enabled = hasIcon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

        if (labelText != null)
            labelText.text = text ?? string.Empty;

        gameObject.SetActive(true);
    }

    public void Clear()
    {
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
            iconImage.gameObject.SetActive(false);
        }

        if (labelText != null)
            labelText.text = string.Empty;

        gameObject.SetActive(false);
    }

    [ContextMenu("Auto Wire From Children")]
    public void AutoWireIfNeeded()
    {
        if (iconImage == null)
        {
            Transform t = transform.Find("IconImage");
            if (t != null)
                iconImage = t.GetComponent<Image>();
        }

        if (labelText == null)
        {
            Transform t = transform.Find("LabelText");
            if (t != null)
                labelText = t.GetComponent<TMP_Text>();
        }

        if (iconImage == null)
            iconImage = GetComponentInChildren<Image>(true);
        if (labelText == null)
            labelText = GetComponentInChildren<TMP_Text>(true);
    }
}
