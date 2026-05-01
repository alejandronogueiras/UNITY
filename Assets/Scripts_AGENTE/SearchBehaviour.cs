using UnityEngine;

public class SearchBehaviour : MonoBehaviour
{
    public float velocidadNormal = 3.5f;
    
    [Header("Ajustes de Búsqueda")]
    public float tiempoDeBusqueda = 3f; // Segundos que espera en el sitio antes de rendirse

    private Vector3 ultimoPuntoVisto;
    private bool busquedaCompletada = false;
    private float timer = 0f;

    // El cerebro usa esto para darle un nuevo punto a investigar
    public void SetUltimoPuntoVisto(Vector3 punto)
    {
        ultimoPuntoVisto = punto;
        busquedaCompletada = false; // Reiniciamos la bandera de completado
        timer = 0f;                 // Reiniciamos el tiempo de espera
    }

    // El ciclo BDI usa esto para saber si ya se puede ir a patrullar
    public bool HaTerminado()
    {
        return busquedaCompletada;
    }

    // El GOAP llama a esto en cada frame
    public bool Ejecutar(PoliceBrain brain)
    {
        // Si ya terminamos, no hacemos nada más
        if (busquedaCompletada) return true;

        brain.Agent.speed = velocidadNormal;
        brain.Agent.isStopped = false;
        
        // Vamos hacia el punto donde vimos al jugador
        brain.Agent.SetDestination(ultimoPuntoVisto);

        // Si ya hemos llegado al punto (o estamos muy cerca)
        if (!brain.Agent.pathPending && brain.Agent.remainingDistance < 0.6f)
        {
            // Empezamos a contar el tiempo (simulando que está buscando por la zona)
            timer += Time.deltaTime;
            
            if (timer >= tiempoDeBusqueda)
            {
                // Se acabó el tiempo, nos rendimos
                busquedaCompletada = true;
                Debug.Log($"[{gameObject.name}] Búsqueda terminada. Volviendo a patrullar.");
                return true; // Le decimos al GOAP que la acción ha finalizado
            }
        }
        
        return false; // Seguimos en proceso de búsqueda
    }
}