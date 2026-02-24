using UnityEngine;
using UnityEngine.InputSystem;

public class KeyPickup : MonoBehaviour
{
    public float distanciaRecoger = 2f;
    public Transform jugador;

    void Update()
    {
        if (jugador == null) return;

        float dist = Vector3.Distance(transform.position, jugador.position);

        // Si está cerca y pulsa E
        if (dist <= distanciaRecoger &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            RecogerLlave();
        }
    }

    void RecogerLlave()
    {
        Debug.Log("LLAVE RECOGIDA");

        GameManager.instance.tieneLlave = true;

        Destroy(gameObject);
    }
}