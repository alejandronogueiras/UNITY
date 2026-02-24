using UnityEngine;
using UnityEngine.InputSystem;

public class VictoryZone : MonoBehaviour
{
    public string playerTag = "Player";
    private bool jugadorDentro = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
            jugadorDentro = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
            jugadorDentro = false;
    }

    void Update()
    {
        if (GameManager.instance == null) return;
        if (GameManager.instance.juegoTerminado) return;
        if (!jugadorDentro) return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (GameManager.instance.tieneLlave)
            {
                GameManager.instance.Ganar();
            }
            else
            {
                Debug.Log("❌ Necesitas la llave");
            }
        }
    }
}