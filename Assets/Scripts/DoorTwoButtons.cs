using UnityEngine;

public class DoorTwoButtons : MonoBehaviour
{
    public FloorButton botonA;
    public FloorButton botonB;
    public PuzzleDoor puerta;

    private bool aRegistrado = false;
    private bool bRegistrado = false;
    private bool puertaAbierta = false;

    void Update()
    {
        if (botonA == null || botonB == null || puerta == null) return;
        if (puertaAbierta) return;

        if (botonA.activado) aRegistrado = true;
        if (botonB.activado) bRegistrado = true;

        if (aRegistrado && bRegistrado)
        {
            puerta.Abrir();
            puertaAbierta = true;
        }
    }
}