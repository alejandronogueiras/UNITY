using UnityEngine;

public class ChaseBehaviour : MonoBehaviour
{
    public Transform jugador;
    public float velocidadPersecucion = 6.5f;
    
    [Header("Captura")]
    public float distanciaCaptura = 1.5f; 

    public bool Ejecutar(PoliceBrain brain)
    {
        if (brain == null || brain.Agent == null)
            return true;

        brain.Agent.speed = velocidadPersecucion;
        brain.Agent.isStopped = false;
        brain.Agent.stoppingDistance = 0.2f; 

        if (jugador != null && brain.creencias.jugadorDetectado)
        {
            brain.Agent.SetDestination(jugador.position);

            float distancia = Vector3.Distance(transform.position, jugador.position);

            if (distancia <= distanciaCaptura)
            {
                Debug.Log($"¡{gameObject.name} te ha atrapado!");
                // GameManager.instance.Perder();
                return true;
            }

            return false;
        }

        return true; // Si no lo ve, termina la acción
    }
}