using UnityEngine;

public class ChaseBehaviour : MonoBehaviour
{
    public Transform jugador;
    public float velocidadPersecucion = 6.5f;
    
    [Header("Captura")]
    public float distanciaCaptura = 1.5f; 

    public bool Ejecutar(PoliceBrain brain)
    {
        brain.Agent.speed = velocidadPersecucion;
        brain.Agent.isStopped = false;
        brain.Agent.stoppingDistance = 0.2f; 

        if (jugador != null)
        {
            brain.Agent.SetDestination(jugador.position);

            float distancia = Vector3.Distance(transform.position, jugador.position);
            
            if (distancia <= distanciaCaptura)
            {
                Debug.Log($"¡{gameObject.name} te ha atrapado!");
                // GameManager.instance.Perder(); 
                return true; // Acción GOAP de persecución terminada con éxito
            }
        }

        return false; // Seguimos persiguiendo
    }
}