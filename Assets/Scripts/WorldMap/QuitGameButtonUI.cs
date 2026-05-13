using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class QuitGameButtonUI : MonoBehaviour
{
    [SerializeField] private Button quitButton;
    [SerializeField] private bool saveBeforeQuit = true;

    private void Awake()
    {
        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(HandleQuitClicked);
            quitButton.onClick.AddListener(HandleQuitClicked);
        }
    }

    private void OnDestroy()
    {
        if (quitButton != null)
            quitButton.onClick.RemoveListener(HandleQuitClicked);
    }

    private void HandleQuitClicked()
    {
        if (saveBeforeQuit)
            SaveCoordinator.Instance?.SaveAll();

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}