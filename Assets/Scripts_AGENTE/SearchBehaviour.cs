using UnityEngine;

public class SearchBehaviour : MonoBehaviour
{
    public float velocidadNormal = 3.5f;
    private Vector3 ultimoPuntoVisto;

    public void SetUltimoPuntoVisto(Vector3 punto)
    {
        ultimoPuntoVisto = punto;
    }

    // El cerebro usa esto para saber si la intención de búsqueda finalizó
    public bool HaTerminado()
    {
        return false; // Puedes implementar una lógica de tiempo aquí si quieres que se rindan
    }

    public bool Ejecutar(PoliceBrain brain)
    {
        brain.Agent.speed = velocidadNormal;
        brain.Agent.isStopped = false;
        brain.Agent.SetDestination(ultimoPuntoVisto);

        // Si ha llegado al punto, devuelve true para decirle al GOAP que esta acción terminó
        if (!brain.Agent.pathPending && brain.Agent.remainingDistance < 0.6f)
        {
            return true; 
        }
        
        return false;
    }
}