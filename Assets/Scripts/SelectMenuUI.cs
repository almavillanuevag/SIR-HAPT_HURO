using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SelectMenuUI : MonoBehaviour
{
    [Header("Asignar dropdowns para interacciones")]
    public TMP_Dropdown usersDropdown;
    public TextMeshProUGUI summaryText;
    public Button startSessionButton;
    public Button refreshButton;

    [Header("(Opcional) Logs")]
    public TextMeshProUGUI debugText;
    private void OnEnable()
    {
        StartCoroutine(BindWhenReady());
    }

    private IEnumerator BindWhenReady()
    {
        // Esperar hasta que SelectUser.Instance exista
        while (SelectUser.Instance == null)
            yield return null;  // espera un frame y reintenta

        Bind();
    }

    public void Bind()
    {
        if (SelectUser.Instance == null) return;

        SelectUser.Instance.usersDropdown = usersDropdown;
        SelectUser.Instance.summaryText = summaryText;
        SelectUser.Instance.startSessionButton = startSessionButton;
        SelectUser.Instance.debugText = debugText;
        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveAllListeners();
            refreshButton.onClick.AddListener(SelectUser.Instance.LoadUsers);
        }
        if (SelectUser.Instance._initialized)
            SelectUser.Instance.LoadUsers();
    }
}