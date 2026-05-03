using System;
using System.Collections;
using UnityEngine;

public class HearingSensor : MonoBehaviour
{
    [Header("Referencia")]
    public Transform jugador;

    [Header("Rangos")]
    public float rangoCerca = 4f;
    public float rangoLejos = 8f;
    public float intervaloComprobacion = 0.1f;

    public bool EscuchaCerca { get; private set; }
    public bool EscuchaLejos { get; private set; }
    public Vector3 PuntoOido { get; private set; }

    public event Action<Vector3, bool> OnNoiseDetected;

    private PlayerLookMove scriptJugador;

    void Start()
    {
        if (jugador != null)
            scriptJugador = jugador.GetComponent<PlayerLookMove>();

        StartCoroutine(RutinaOido());
    }

    private IEnumerator RutinaOido()
    {
        while (true)
        {
            bool antesEscuchabaCerca = EscuchaCerca;
            bool antesEscuchabaLejos = EscuchaLejos;

            UpdateHearing();

            bool detectaNuevoRuido = (EscuchaCerca && !antesEscuchabaCerca) ||
                                     (EscuchaLejos && !antesEscuchabaLejos);

            if (detectaNuevoRuido)
                OnNoiseDetected?.Invoke(PuntoOido, EscuchaCerca);

            yield return new WaitForSeconds(intervaloComprobacion);
        }
    }

    void UpdateHearing()
    {
        EscuchaCerca = false;
        EscuchaLejos = false;

        if (jugador == null || scriptJugador == null) return;
        if (!scriptJugador.estaHaciendoRuido) return;

        float distancia = Vector3.Distance(transform.position, jugador.position);

        if (distancia <= rangoCerca + scriptJugador.radioRuido)
        {
            EscuchaCerca = true;
            PuntoOido = jugador.position;
        }
        else if (distancia <= rangoLejos + scriptJugador.radioRuido)
        {
            EscuchaLejos = true;
            Vector3 dir = (jugador.position - transform.position).normalized;
            PuntoOido = transform.position + dir * rangoLejos;
        }
    }
}
