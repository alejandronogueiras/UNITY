using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Estado")]
    public bool tieneLlave = false;
    public bool juegoTerminado = false;

    [Header("UI")]
    public GameObject winPanelOrText; // arrastra aquí WinText (o un Panel)

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void Start()
    {
        if (winPanelOrText != null)
            winPanelOrText.SetActive(false);
    }

    public void Ganar()
    {
        if (juegoTerminado) return;
        juegoTerminado = true;

        if (winPanelOrText != null)
            winPanelOrText.SetActive(true);

        // Para TODO el juego
        Time.timeScale = 0f;

        // Libera el cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}