using UnityEngine;
using UnityEngine.AI;

public class InvestigateBehaviour : MonoBehaviour
{
    public float velocidadNormal = 3.5f;
    public float tiempoInvestigacion = 4f;
    public float radioInvestigacion = 5f;

    private bool investigandoZona = false;
    private float timerInvestigacion = 0f;
    private Vector3 centroInvestigacion;

    public void IniciarInvestigacion(Vector3 punto, bool esLejos)
    {
        centroInvestigacion = punto;
        investigandoZona = esLejos;
        timerInvestigacion = 0f;
    }

    public void Ejecutar(PoliceBrain brain)
    {
        brain.Agent.speed = velocidadNormal;
        brain.Agent.isStopped = false;

        if (!investigandoZona && brain.hearing.EscuchaCerca)
        {
            brain.Agent.SetDestination(brain.hearing.PuntoOido);
            if (!brain.Agent.pathPending && brain.Agent.remainingDistance < 0.6f)
            {
                FinalizarInvestigacion(brain);
            }
        }
        else if (investigandoZona)
        {
            timerInvestigacion += Time.deltaTime;
            if (!brain.Agent.hasPath)
            {
                Vector3 punto = centroInvestigacion + Random.insideUnitSphere * radioInvestigacion;
                punto.y = transform.position.y;

                if (NavMesh.SamplePosition(punto, out NavMeshHit hit, radioInvestigacion, NavMesh.AllAreas))
                {
                    brain.Agent.SetDestination(hit.position);
                }
            }

            if (timerInvestigacion >= tiempoInvestigacion)
            {
                investigandoZona = false;
                FinalizarInvestigacion(brain);
            }
        }
    }

    private void FinalizarInvestigacion(PoliceBrain brain)
    {
        PoliceBrain.Estado siguiente = (brain.estadoAnterior == PoliceBrain.Estado.Investigando) 
            ? PoliceBrain.Estado.Patrullando 
            : brain.estadoAnterior;
            
        brain.CambiarEstado(siguiente);
    }
}