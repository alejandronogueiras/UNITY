using UnityEngine;
using UnityEngine.AI;

public class CheckKeyBehaviour : MonoBehaviour
{
    public Transform zonaLlaveCentro;
    public float zonaLlaveRadio = 8f;
    public float velocidadNormal = 3.5f;

    [Header("Llave")]
    public Transform llaveObjeto;
    public string tagLlave = "Llave";

    [Header("Revisión")]
    public float tiempoRevision = 4f;

    private bool llegoAlCentro = false;
    private float timerRevision = 0f;

    public bool LlaveSigueEnSuSitio { get; private set; } = true;

    public void Reiniciar()
    {
        llegoAlCentro = false;
        timerRevision = 0f;
        LlaveSigueEnSuSitio = true;
    }

    public bool Ejecutar(PoliceBrain brain)
    {
        if (brain == null || brain.Agent == null)
            return true;

        brain.Agent.speed = velocidadNormal;
        brain.Agent.isStopped = false;

        if (zonaLlaveCentro == null)
        {
            Debug.LogWarning($"[{RutaCompleta()}] No tiene asignada zonaLlaveCentro en CheckKeyBehaviour.", this);
            return true;
        }

        if (!llegoAlCentro)
        {
            brain.Agent.SetDestination(zonaLlaveCentro.position);

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
            Vector3 punto = zonaLlaveCentro.position + Random.insideUnitSphere * zonaLlaveRadio;
            punto.y = zonaLlaveCentro.position.y;

            if (NavMesh.SamplePosition(punto, out NavMeshHit hit, zonaLlaveRadio, NavMesh.AllAreas))
            {
                brain.Agent.SetDestination(hit.position);
            }
        }

        if (timerRevision >= tiempoRevision)
        {
            LlaveSigueEnSuSitio = ExisteLlave();

            if (LlaveSigueEnSuSitio)
            {
                Debug.Log($"[{RutaCompleta()}] La llave sigue en su sitio. Me quedo vigilando la llave.");

                timerRevision = 0f;
                return false;
            }
            else
            {
                Debug.Log($"[{RutaCompleta()}] La llave NO está. Voy a reforzar la salida.");

                Reiniciar();
                return true;
            }
        }

        return false;
    }

    private bool ExisteLlave()
    {
        if (llaveObjeto != null)
            return llaveObjeto.gameObject.activeInHierarchy;

        GameObject llave = GameObject.FindWithTag(tagLlave);
        return llave != null && llave.activeInHierarchy;
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