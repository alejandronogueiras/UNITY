using UnityEngine;

public class CheckKeyBehaviour : MonoBehaviour
{
    public Transform zonaLlaveCentro;
    public float zonaLlaveRadio = 8f;
    public float velocidadNormal = 3.5f;

    [HideInInspector] public bool protegiendoLlave = false;

    public void Ejecutar(PoliceBrain brain)
    {
        brain.Agent.speed = velocidadNormal;
        brain.Agent.isStopped = false;

        if (zonaLlaveCentro != null)
            brain.Agent.SetDestination(zonaLlaveCentro.position);

        if (!brain.Agent.pathPending && brain.Agent.remainingDistance < 0.6f)
        {
            protegiendoLlave = true;

            // Configuramos la zona de patrulla para que vigile esta área
            if (brain.patrol != null && zonaLlaveCentro != null)
            {
                brain.patrol.zonas.Clear();
                brain.patrol.zonas.Add(new PatrolBehaviour.PatrolZone
                {
                    centro = zonaLlaveCentro,
                    radio = zonaLlaveRadio
                });
            }
            
            // Le decimos al cerebro que hemos cumplido el rol inicial. 
            // Esto hará que el BDI evalúe y pase al deseo "Patrullar".
            brain.AsignarRol("");
        }
    }
}