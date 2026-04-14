using UnityEngine;

public class SearchBehaviour : MonoBehaviour
{
    public float velocidadNormal = 3.5f;
    private Vector3 ultimoPuntoVisto;

    public void SetUltimoPuntoVisto(Vector3 punto)
    {
        ultimoPuntoVisto = punto;
    }

    public void Ejecutar(PoliceBrain brain)
    {
        brain.Agent.speed = velocidadNormal;
        brain.Agent.isStopped = false;

        brain.Agent.SetDestination(ultimoPuntoVisto);

        if (!brain.Agent.pathPending && brain.Agent.remainingDistance < 0.6f)
        {
            brain.CambiarEstado(PoliceBrain.Estado.ComprobandoLlave);
        }
    }
}