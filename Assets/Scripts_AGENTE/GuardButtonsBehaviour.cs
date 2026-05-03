using UnityEngine;
using UnityEngine.AI;

public class GuardButtonsBehaviour : MonoBehaviour
{
    [Header("Zona de botones")]
    public Transform zonaBotonesCentro;
    public float zonaBotonesRadio = 6f;
    public float velocidadNormal = 3.5f;

    [Header("Revisión")]
    public float tiempoRevision = 4f;

    private bool llegoAlCentro = false;
    private float timerRevision = 0f;

    public void Reiniciar()
    {
        llegoAlCentro = false;
        timerRevision = 0f;
    }

    public bool Ejecutar(PoliceBrain brain)
    {
        if (brain == null || brain.Agent == null)
            return true;

        brain.Agent.speed = velocidadNormal;
        brain.Agent.isStopped = false;

        if (zonaBotonesCentro == null)
        {
            Debug.LogWarning($"[{RutaCompleta()}] No tiene asignada zonaBotonesCentro en GuardButtonsBehaviour.", this);
            return true;
        }

        if (!llegoAlCentro)
        {
            brain.Agent.SetDestination(zonaBotonesCentro.position);

            if (!brain.Agent.pathPending && brain.Agent.remainingDistance < 0.7f)
            {
                llegoAlCentro = true;
                timerRevision = 0f;
            }

            return false;
        }

        timerRevision += Time.deltaTime;

        if (!brain.Agent.hasPath || brain.Agent.remainingDistance < 0.7f)
        {
            Vector3 punto = zonaBotonesCentro.position + Random.insideUnitSphere * zonaBotonesRadio;
            punto.y = zonaBotonesCentro.position.y;

            if (NavMesh.SamplePosition(punto, out NavMeshHit hit, zonaBotonesRadio, NavMesh.AllAreas))
            {
                brain.Agent.SetDestination(hit.position);
            }
        }

        if (timerRevision >= tiempoRevision)
        {
            Debug.Log($"[{RutaCompleta()}] Zona de botones revisada. No está el ladrón.");
            Reiniciar();
            return true;
        }

        return false;
    }

    private string RutaCompleta()
    {
        string ruta = gameObject.name;
        Transform actual = transform.parent;

        while (actual != null)
        {
            ruta = actual.name + "/" + ruta;
            actual = actual.parent;
        }

        return ruta;
    }
}