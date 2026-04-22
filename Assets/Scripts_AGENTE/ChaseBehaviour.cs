using UnityEngine;

public class ChaseBehaviour : MonoBehaviour
{
    public Transform jugador;
    public float velocidadPersecucion = 6.5f;
    
    [Header("Captura")]
    public float distanciaCaptura = 1.5f; // A qué distancia te elimina

    public void Ejecutar(PoliceBrain brain)
    {
        brain.Agent.speed = velocidadPersecucion;
        brain.Agent.isStopped = false;
        
        // Reducimos el stopping distance al mínimo durante la persecución 
        // para que intente pegarse a ti todo lo posible
        brain.Agent.stoppingDistance = 0.2f; 

        if (jugador != null)
        {
            brain.Agent.SetDestination(jugador.position);

            // COMPROBACIÓN DE CAPTURA MATEMÁTICA
            float distancia = Vector3.Distance(transform.position, jugador.position);
            
            if (distancia <= distanciaCaptura)
            {
                // ¡Atrapado!
                Debug.Log($"¡{gameObject.name} te ha atrapado!");
                
                // Descomenta la línea de abajo si tienes tu GameManager configurado
                // GameManager.instance.Perder(); 
            }
        }

        // Si pierde de vista al jugador, pasa al estado Buscando
        if (!brain.vision.CanSeePlayer)
        {
            brain.CambiarEstado(PoliceBrain.Estado.Buscando);
        }
    }
}