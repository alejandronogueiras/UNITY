using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Estado")]
    public bool tieneLlave = false;
    public bool juegoTerminado = false;

    [Header("UI")]
    public GameObject winPanelOrText;
    public GameObject losePanelOrText;   
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

        if (losePanelOrText != null)
            losePanelOrText.SetActive(false);
    }

    public void Ganar()
    {
        if (juegoTerminado) return;
        juegoTerminado = true;

        if (winPanelOrText != null)
            winPanelOrText.SetActive(true);

        FinalizarJuego();
    }

    public void Perder()   
    {
        if (juegoTerminado) return;
        juegoTerminado = true;

        if (losePanelOrText != null)
            losePanelOrText.SetActive(true);

        FinalizarJuego();
    }

    void FinalizarJuego()
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}