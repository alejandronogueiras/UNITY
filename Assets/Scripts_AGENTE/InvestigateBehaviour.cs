using UnityEngine;
using UnityEngine.AI;

public class InvestigateBehaviour : MonoBehaviour
{
    public float velocidadNormal = 3.5f;
    public float tiempoInvestigacion = 4f;
    public float radioInvestigacion = 5f;

    private float timerInvestigacion = 0f;
    private Vector3 centroInvestigacion;

    private bool ruidoLejano = false;
    private bool llegoAlCentro = false;

    public void IniciarInvestigacion(Vector3 punto, bool esLejos)
    {
        centroInvestigacion = punto;
        ruidoLejano = esLejos;
        llegoAlCentro = false;
        timerInvestigacion = 0f;
    }

    public bool Ejecutar(PoliceBrain brain)
    {
        if (brain == null || brain.Agent == null)
            return true;

        brain.Agent.speed = velocidadNormal;
        brain.Agent.isStopped = false;

        if (!llegoAlCentro)
        {
            brain.Agent.SetDestination(centroInvestigacion);

            if (!brain.Agent.pathPending && brain.Agent.remainingDistance < 0.6f)
            {
                llegoAlCentro = true;
                timerInvestigacion = 0f;
            }

            return false;
        }

        timerInvestigacion += Time.deltaTime;

        if (ruidoLejano)
        {
            if (!brain.Agent.hasPath || brain.Agent.remainingDistance < 0.6f)
            {
                Vector3 punto = centroInvestigacion + Random.insideUnitSphere * radioInvestigacion;
                punto.y = transform.position.y;

                if (NavMesh.SamplePosition(punto, out NavMeshHit hit, radioInvestigacion, NavMesh.AllAreas))
                {
                    brain.Agent.SetDestination(hit.position);
                }
            }
        }

        if (timerInvestigacion >= tiempoInvestigacion)
        {
            Debug.Log($"[{gameObject.name}] Investigación de ruido terminada.");
            return true;
        }

        return false; 
    }
}