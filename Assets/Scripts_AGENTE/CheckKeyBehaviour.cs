using UnityEngine;

public class CheckKeyBehaviour : MonoBehaviour
{
    public Transform zonaLlaveCentro;
    public float zonaLlaveRadio = 8f;
    public float velocidadNormal = 3.5f;

    [HideInInspector] public bool protegiendoLlave = false;
    [HideInInspector] public bool llaveRobada = false;

    public void Ejecutar(PoliceBrain brain)
    {
        brain.Agent.speed = velocidadNormal;
        brain.Agent.isStopped = false;

        if (zonaLlaveCentro != null)
            brain.Agent.SetDestination(zonaLlaveCentro.position);

        if (!brain.Agent.pathPending && brain.Agent.remainingDistance < 0.6f)
        {
            GameObject llave = GameObject.FindWithTag("Llave");

            if (llave == null)
            {
                protegiendoLlave = false;
                llaveRobada = true;
                brain.CambiarEstado(PoliceBrain.Estado.VigilandoSalida);
            }
            else
            {
                protegiendoLlave = true;

                if (brain.patrol != null && zonaLlaveCentro != null)
                {
                    brain.patrol.zonas.Clear();
                    brain.patrol.zonas.Add(new PatrolBehaviour.PatrolZone
                    {
                        centro = zonaLlaveCentro,
                        radio = zonaLlaveRadio
                    });
                }
                brain.CambiarEstado(PoliceBrain.Estado.Patrullando);
            }
        }
    }
}