using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class PoliceBrain : MonoBehaviour
{
    // ── Arquitectura BDI ─────────────────────────────────────────────────────

    [Serializable]
    public class Creencias
    {
        public bool jugadorDetectado;
        public Vector3 ultimaPosicionJugador;
        public bool escuchandoRuido;
        public Vector3 origenRuido;
        public bool ruidoCerca;
        public bool llaveRobada;
        public string rolAsignado; // "Investigar", "VigilarSalida", "VigilarLlave", o ""
        public int nivelAlertaGlobal;
    }

    public enum Deseo
    {
        Patrullar,
        InvestigarRuido,
        BuscarLadron,
        PerseguirLadron,
        CumplirRolAsignado
    }

    [Header("BDI")]
    public Creencias creencias = new Creencias();
    public Deseo intencionActual = Deseo.Patrullar;

    // ── GOAP ─────────────────────────────────────────────────────────────────

    private Queue<GOAPAction> planActual;
    private GOAPAction accionEnEjecucion;
    // Evita llamar al planificador cada frame si ya falló para el estado actual
    private bool planFallido = false;

    // ─────────────────────────────────────────────────────────────────────────

    [Header("Sensores y Comunicación")]
    public VisionSensor vision;
    public HearingSensor hearing;
    private AgentCommunicator communicator;

    [Header("Actuadores (Comportamientos)")]
    public PatrolBehaviour patrol;
    public InvestigateBehaviour investigate;
    public ChaseBehaviour chase;
    public SearchBehaviour search;
    public CheckKeyBehaviour checkKey;
    public GuardExitBehaviour guardExit;

    [Header("Animación")]
    public Animator animator;
    public float animSmooth = 8f;

    public NavMeshAgent Agent { get; private set; }

    // ── Ciclo de vida ────────────────────────────────────────────────────────

    void Start()
    {
        Agent = GetComponent<NavMeshAgent>();
        communicator = GetComponent<AgentCommunicator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (vision != null)
        {
            vision.OnPlayerSpotted += OnJugadorVisto;
            vision.OnPlayerLost    += OnJugadorPerdido;
        }

        if (hearing != null)
            hearing.OnNoiseDetected += OnRuidoDetectado;
    }

    void OnDestroy()
    {
        if (vision != null)
        {
            vision.OnPlayerSpotted -= OnJugadorVisto;
            vision.OnPlayerLost    -= OnJugadorPerdido;
        }

        if (hearing != null)
            hearing.OnNoiseDetected -= OnRuidoDetectado;
    }

    // ── Callbacks de sensores (solo actualizan CREENCIAS) ────────────────────

    private void OnJugadorVisto(Vector3 pos)
    {
        creencias.jugadorDetectado = true;
        creencias.ultimaPosicionJugador = pos;

        if (search != null)
            search.SetUltimoPuntoVisto(pos);

        if (intencionActual != Deseo.PerseguirLadron && communicator != null)
            communicator.IniciarCFP(pos);
    }

    private void OnJugadorPerdido()
    {
        creencias.jugadorDetectado = false;

        if (intencionActual == Deseo.PerseguirLadron)
            CambiarIntencion(Deseo.BuscarLadron);
    }

    private void OnRuidoDetectado(Vector3 punto, bool cerca)
    {
        if (intencionActual == Deseo.PerseguirLadron ||
            intencionActual == Deseo.InvestigarRuido  ||
            intencionActual == Deseo.BuscarLadron) return;

        creencias.escuchandoRuido = true;
        creencias.origenRuido     = punto;
        creencias.ruidoCerca      = cerca;
    }

    // ── Bucle principal BDI ──────────────────────────────────────────────────

    void Update()
    {
        if (Agent == null) return;

        ActualizarCreencias();
        GenerarYSeleccionarIntencion();
        EjecutarIntencion();
        ActualizarAnimacion();
    }

    void ActualizarCreencias()
    {
        // 1. ¿Sigue la llave en su sitio?
        GameObject llave = GameObject.FindWithTag("Llave");
        creencias.llaveRobada = (llave == null);

        // 2. Nivel de alerta global (CFPs de los últimos 30 s)
        if (communicator != null)
        {
            long hace30Seg = DateTimeOffset.Now.ToUnixTimeMilliseconds() - 30000;
            creencias.nivelAlertaGlobal = communicator.GetHistory()
                .Where(m => m.performative == FIPAMessage.Performative.CFP && m.timestamp > hace30Seg)
                .Count();
        }
    }

    void GenerarYSeleccionarIntencion()
    {
        Deseo nueva;

        if (creencias.jugadorDetectado)
        {
            nueva = Deseo.PerseguirLadron;
        }
        else if (intencionActual == Deseo.BuscarLadron && !BusquedaTerminada())
        {
            nueva = Deseo.BuscarLadron;
        }
        else if (!string.IsNullOrEmpty(creencias.rolAsignado))
        {
            nueva = Deseo.CumplirRolAsignado;
        }
        else if (creencias.escuchandoRuido)
        {
            nueva = Deseo.InvestigarRuido;
        }
        else
        {
            nueva = Deseo.Patrullar;
        }

        CambiarIntencion(nueva);
    }

    private bool BusquedaTerminada()
    {
        if (search == null) return true;
        return search.HaTerminado();
    }

    // ── Integración BDI → GOAP ───────────────────────────────────────────────

    void EjecutarIntencion()
    {
        // Los deseos de caza usan GOAP; el resto, ejecución directa
        if (intencionActual == Deseo.PerseguirLadron || intencionActual == Deseo.BuscarLadron)
        {
            // Solo planificamos si no tenemos plan, no falló antes y no hay acción en curso
            if (planActual == null && !planFallido && accionEnEjecucion == null)
            {
                GenerarPlanCaza();

                if (planActual == null)
                {
                    // El planificador no encontró solución: marcamos como fallido
                    // para no volver a intentarlo cada frame con el mismo estado
                    planFallido = true;
                    Debug.LogWarning($"[{gameObject.name}] GOAP no encontró plan. Esperando cambio de estado.");
                    return;
                }
            }

            if (accionEnEjecucion != null)
                EjecutarAccionGOAP(accionEnEjecucion.Nombre);
        }
        else
        {
            // Al salir del modo caza limpiamos todo el estado GOAP
            planActual        = null;
            accionEnEjecucion = null;
            planFallido       = false;

            switch (intencionActual)
            {
                case Deseo.Patrullar:
                    if (patrol != null) patrol.Ejecutar();
                    break;

                case Deseo.InvestigarRuido:
                    if (investigate != null)
                    {
                        investigate.IniciarInvestigacion(creencias.origenRuido, !creencias.ruidoCerca);
                        investigate.Ejecutar(this);
                    }
                    creencias.escuchandoRuido = false;
                    break;

                case Deseo.CumplirRolAsignado:
                    EjecutarComportamientoPorRol();
                    break;
            }
        }
    }

    void GenerarPlanCaza()
    {
        // SalidaVigilada: true solo si OTRO agente (no este) ya tiene ese rol asignado
        bool salidaVigilada = HayAgenteConRol("VigilarSalida");

        Dictionary<string, bool> estadoInicial = new Dictionary<string, bool>
        {
            { "LadronLocalizado", creencias.jugadorDetectado },
            { "SalidaVigilada",   salidaVigilada             },
            { "LadronAtrapado",   false                      }
        };

        Dictionary<string, bool> objetivo = new Dictionary<string, bool>
        {
            { "LadronAtrapado", true }
        };

        List<GOAPAction> acciones = new List<GOAPAction>();

        // Buscar: localiza al ladrón si no sabemos dónde está
        var buscar = new GOAPAction("Buscar", costo: 5f);
        buscar.Precondiciones.Add("LadronLocalizado", false);
        buscar.Efectos.Add("LadronLocalizado", true);
        acciones.Add(buscar);

        // Perseguir: va directo a por él
        var perseguir = new GOAPAction("Perseguir", costo: 3f);
        perseguir.Precondiciones.Add("LadronLocalizado", true);
        perseguir.Efectos.Add("LadronAtrapado", true);
        acciones.Add(perseguir);

        // Emboscar: más barato, pero requiere que otro agente ya vigile la salida
        var emboscar = new GOAPAction("Emboscar", costo: 1f);
        emboscar.Precondiciones.Add("LadronLocalizado", true);
        emboscar.Precondiciones.Add("SalidaVigilada",   true);
        emboscar.Efectos.Add("LadronAtrapado", true);
        acciones.Add(emboscar);

        planActual = GOAPPlanner.Planificar(estadoInicial, objetivo, acciones);

        if (planActual != null && planActual.Count > 0)
        {
            accionEnEjecucion = planActual.Dequeue();
            Debug.Log($"[{gameObject.name}] Plan GOAP generado. Primera acción: {accionEnEjecucion.Nombre}");
        }
        else
        {
            planActual = null; // Garantizamos null si el resultado era una cola vacía
        }
    }

    // Comprueba si algún otro PoliceBrain de la escena tiene el rol indicado
    private bool HayAgenteConRol(string rol)
    {
        return FindObjectsByType<PoliceBrain>(FindObjectsSortMode.None)
            .Any(b => b != this && b.creencias.rolAsignado == rol);
    }

    void EjecutarAccionGOAP(string nombreAccion)
    {
        bool accionTerminada = false;

        switch (nombreAccion)
        {
            case "Buscar":
                // search.Ejecutar() debe devolver true cuando haya terminado de buscar
                if (search != null) accionTerminada = search.Ejecutar(this);
                break;

            case "Perseguir":
            case "Emboscar":
                // chase.Ejecutar() debe devolver true cuando haya contactado al ladrón
                if (chase != null) accionTerminada = chase.Ejecutar(this);
                break;
        }

        if (accionTerminada)
        {
            if (planActual != null && planActual.Count > 0)
            {
                // Avanzamos a la siguiente acción del plan
                accionEnEjecucion = planActual.Dequeue();
                Debug.Log($"[{gameObject.name}] Siguiente acción GOAP: {accionEnEjecucion.Nombre}");
            }
            else
            {
                // Plan completado
                accionEnEjecucion = null;
                planActual        = null;
                Debug.Log($"[{gameObject.name}] Plan GOAP completado.");
            }
        }
    }

    void EjecutarComportamientoPorRol()
    {
        switch (creencias.rolAsignado)
        {
            case "Investigar":
                if (search != null)    search.Ejecutar(this);
                break;
            case "VigilarSalida":
                if (guardExit != null) guardExit.Ejecutar(this);
                break;
            case "VigilarLlave":
                if (checkKey != null)  checkKey.Ejecutar(this);
                break;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    void CambiarIntencion(Deseo nueva)
    {
        if (nueva == intencionActual) return;

        intencionActual   = nueva;
        planActual        = null;
        accionEnEjecucion = null;
        planFallido       = false; // nuevo estado = nueva oportunidad de planificar

        if (Agent != null) Agent.ResetPath();
    }

    public void AsignarRol(string nuevoRol)
    {
        creencias.rolAsignado = nuevoRol;
    }

    // ── Animación y colisiones ───────────────────────────────────────────────

    void ActualizarAnimacion()
    {
        if (animator == null || Agent == null) return;

        float maxVel  = chase != null ? chase.velocidadPersecucion : 6.5f;
        float speed01 = Agent.velocity.magnitude / Mathf.Max(0.01f, maxVel);
        float smooth  = Mathf.Lerp(animator.GetFloat("Speed"), speed01, Time.deltaTime * animSmooth);
        animator.SetFloat("Speed", smooth);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // GameManager.instance.Perder();
        }
    }
}
