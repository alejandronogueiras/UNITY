using UnityEngine;

public class GuardExitBehaviour : MonoBehaviour
{
    public Transform salida;
    public float velocidadNormal = 3.5f;
    private bool salidaAsegurada = false;

    public void Ejecutar(PoliceBrain brain)
    {
        brain.Agent.speed = velocidadNormal;
        brain.Agent.isStopped = false;

        if (!salidaAsegurada)
        {
            if (salida != null)
                brain.Agent.SetDestination(salida.position);

            if (!brain.Agent.pathPending && brain.Agent.remainingDistance < 0.6f)
            {
                if (brain.patrol != null && salida != null)
                {
                    brain.patrol.zonas.Clear();
                    brain.patrol.zonas.Add(new PatrolBehaviour.PatrolZone
                    {
                        centro = salida,
                        radio = 6f
                    });
                }

                salidaAsegurada = true;
                
                // Limpiamos el rol para que pase a Patrullar la salida de forma natural
                brain.AsignarRol("");
            }
        }
    }
}