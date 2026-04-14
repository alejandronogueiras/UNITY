using UnityEngine;

public class ChaseBehaviour : MonoBehaviour
{
    public Transform jugador;
    public float velocidadPersecucion = 6.5f;

    public void Ejecutar(PoliceBrain brain)
    {
        brain.Agent.speed = velocidadPersecucion;
        brain.Agent.isStopped = false;

        if (jugador != null)
            brain.Agent.SetDestination(jugador.position);

        if (!brain.vision.CanSeePlayer)
        {
            brain.CambiarEstado(PoliceBrain.Estado.Buscando);
        }
    }
}