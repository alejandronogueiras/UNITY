using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PatrolBehaviour : MonoBehaviour
{
    [System.Serializable]
    public class PatrolZone
    {
        public Transform centro;
        public float radio = 10f;
    }

    [Header("Zonas de patrulla")]
    public List<PatrolZone> zonas = new List<PatrolZone>();

    [Header("Ajustes")]
    public bool aleatorio = true;
    public float waitTime = 1f;

    private NavMeshAgent agent;
    private float waitTimer = 0f;
    private int zonaActual = 0;
    private bool tieneDestino = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public void Ejecutar()
    {
        if (agent == null) return;
        if (zonas == null || zonas.Count == 0) return;

        agent.isStopped = false;

        if (!tieneDestino)
        {
            IrANuevoPunto();
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= 0.6f)
        {
            waitTimer += Time.deltaTime;

            if (waitTimer >= waitTime)
            {
                waitTimer = 0f;
                IrANuevoPunto();
            }
        }
    }

    public void ReiniciarDestino()
    {
        tieneDestino = false;
        waitTimer = 0f;

        if (agent != null)
            agent.ResetPath();
    }

    void IrANuevoPunto()
    {
        PatrolZone zona = ElegirZona();
        if (zona == null || zona.centro == null) return;

        Vector3 punto = zona.centro.position + Random.insideUnitSphere * zona.radio;
        punto.y = zona.centro.position.y;

        if (NavMesh.SamplePosition(punto, out NavMeshHit hit, zona.radio, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            tieneDestino = true;
        }
    }

    PatrolZone ElegirZona()
    {
        if (zonas.Count == 1) return zonas[0];

        if (aleatorio)
        {
            return zonas[Random.Range(0, zonas.Count)];
        }
        else
        {
            PatrolZone z = zonas[zonaActual];
            zonaActual = (zonaActual + 1) % zonas.Count;
            return z;
        }
    }
}