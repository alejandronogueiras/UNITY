using System.Runtime.InteropServices;
using UnityEngine;

public class FloorButton : MonoBehaviour
{
    public bool activado = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            activado = true;
            Debug.Log("BOTON PRESIONADO");
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            activado = false;
    }
}