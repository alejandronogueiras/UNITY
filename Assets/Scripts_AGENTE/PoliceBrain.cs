using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
[RequireComponent(typeof(AgentCommunicator))]
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

    // Evita reiniciar la investigación cada frame
    private bool investigacionRuidoIniciada = false;

    // Evita spamear consola cada frame
    private int ultimoNivelAlertaImpreso = -1;

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
    public GuardButtonsBehaviour guardButtons;

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

        // Chivato 1: Si no hay comunicador, la consola nos gritará un error rojo.
        if (communicator == null)
        {
            Debug.LogError($"[{gameObject.name}] ¡ERROR! Falta el AgentCommunicator. La IA no puede enviar mensajes FIPA.");
            return;
        }

        // Chivato 2: Nos avisa justo antes de llamar a la función IniciarCFP
        if (intencionActual != Deseo.PerseguirLadron)
        {
            Debug.Log($"[{gameObject.name}] Te he visto. Enviando petición de ayuda (CFP)...");
            communicator.IniciarCFP(pos);
        }
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

    private void RegistrarJugadorVisto(Vector3 pos, bool enviarCFP)
    {
        bool antesNoLoVeia = !creencias.jugadorDetectado;

        creencias.jugadorDetectado = true;
        creencias.ultimaPosicionJugador = pos;

        if (search != null)
            search.SetUltimoPuntoVisto(pos);

        if (communicator == null)
        {
            Debug.LogError($"[{gameObject.name}] ERROR: Falta AgentCommunicator. No se pueden enviar mensajes FIPA.");
            return;
        }
        if (enviarCFP && antesNoLoVeia)
        {
            Debug.Log($"[{gameObject.name}] Te he visto. Enviando petición de ayuda (CFP)...");
            communicator.IniciarCFP(pos);
        }
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
        // Sincronizar visión
        if (vision != null)
        {
            if (vision.CanSeePlayer)
            {
                RegistrarJugadorVisto(vision.LastSeenPosition, !creencias.jugadorDetectado);
            }
            else if (creencias.jugadorDetectado)
            {
                creencias.jugadorDetectado = false;

                if (intencionActual == Deseo.PerseguirLadron)
                    CambiarIntencion(Deseo.BuscarLadron);
            }
        }
        // sincronizar audicion
        if (hearing != null &&
            !creencias.jugadorDetectado &&
            intencionActual != Deseo.PerseguirLadron &&
            intencionActual != Deseo.BuscarLadron &&
            intencionActual != Deseo.InvestigarRuido)
        {
            if (hearing.EscuchaCerca || hearing.EscuchaLejos)
            {
                creencias.escuchandoRuido = true;
                creencias.origenRuido = hearing.PuntoOido;
                creencias.ruidoCerca = hearing.EscuchaCerca;
            }
        }
        // 1. ¿Sigue la llave en su sitio?
        GameObject llave = GameObject.FindWithTag("Llave");
        creencias.llaveRobada = (llave == null);

        // 2. Nivel de alerta global (CFPs de los últimos 30 s)
        long hace30Seg = DateTimeOffset.Now.ToUnixTimeMilliseconds() - 30000;
        creencias.nivelAlertaGlobal = MessageRouter.GetNivelAlertaGlobal(hace30Seg);

        if (creencias.nivelAlertaGlobal != ultimoNivelAlertaImpreso)
        {
            ultimoNivelAlertaImpreso = creencias.nivelAlertaGlobal;

            if (creencias.nivelAlertaGlobal > 0)
                Debug.Log($"[{gameObject.name}] Mi nivel de alerta es: {creencias.nivelAlertaGlobal}");
        }
    }

    void GenerarYSeleccionarIntencion()
    {
        Deseo nueva;

        if (creencias.jugadorDetectado)
        {
            nueva = Deseo.PerseguirLadron;
        }
        else if (creencias.nivelAlertaGlobal >= 2 && MessageRouter.TodosHanPerdidoLadron())
        {
            MessageRouter.IntentarIniciarBusquedaCoordinada(creencias.nivelAlertaGlobal);

            if (!string.IsNullOrEmpty(creencias.rolAsignado))
            {
                nueva = Deseo.CumplirRolAsignado;
            }
            else if (intencionActual == Deseo.BuscarLadron && !BusquedaTerminada())
            {
                nueva = Deseo.BuscarLadron;
            }
            else
            {
                nueva = Deseo.Patrullar;
            }
        }
        else if (MessageRouter.BusquedaCoordinadaActiva && !string.IsNullOrEmpty(creencias.rolAsignado))
        {
            nueva = Deseo.CumplirRolAsignado;
        }
        else if (intencionActual == Deseo.BuscarLadron && !BusquedaTerminada())
        {
            nueva = Deseo.BuscarLadron;
        }
        else if (creencias.escuchandoRuido)
        {
            nueva = Deseo.InvestigarRuido;
        }
        else if (!string.IsNullOrEmpty(creencias.rolAsignado))
        {
            nueva = Deseo.CumplirRolAsignado;
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
        if (intencionActual == Deseo.PerseguirLadron)
        {
            EjecutarPersecucionGOAP();
            return;
        }
        if (intencionActual == Deseo.BuscarLadron)
        {   
            planActual = null;
            accionEnEjecucion = null;
            planFallido = false;

            if (search != null)
            {
                bool terminado = search.Ejecutar(this);

                if (terminado)
                {
                    creencias.jugadorDetectado = false;
                    CambiarIntencion(Deseo.Patrullar);
                }
            }

            return;
        }
        // Al salir del modo persecución limpiamos GOAP.
        planActual = null;
        accionEnEjecucion = null;
        planFallido = false;

        switch (intencionActual)
        {
            case Deseo.Patrullar:
                if (patrol != null) patrol.Ejecutar();
                break;

            case Deseo.InvestigarRuido:
                EjecutarInvestigacionRuido();
                break;

            case Deseo.CumplirRolAsignado:
                EjecutarComportamientoPorRol();
                break;
        }
    }

    //persecución GOAP
    private void EjecutarPersecucionGOAP()
    {
        if (planActual == null && !planFallido && accionEnEjecucion == null)
        {
            GenerarPlanCaza();

            if (planActual == null)
            {
                planFallido = true;
                Debug.LogWarning($"[{gameObject.name}] GOAP no encontró plan. Esperando cambio de estado.");
                return;
            }
        }

        if (accionEnEjecucion != null)
            EjecutarAccionGOAP(accionEnEjecucion.Nombre);
    }

    private void EjecutarInvestigacionRuido()
    {
        if (investigate == null)
        {
            creencias.escuchandoRuido = false;
            investigacionRuidoIniciada = false;
            return;
        }

        if (!investigacionRuidoIniciada)
        {
            investigate.IniciarInvestigacion(creencias.origenRuido, !creencias.ruidoCerca);
            investigacionRuidoIniciada = true;

            Debug.Log($"[{gameObject.name}] Investigando ruido en {creencias.origenRuido}");
        }

        bool terminado = investigate.Ejecutar(this);

        if (terminado)
        {
            creencias.escuchandoRuido = false;
            investigacionRuidoIniciada = false;
            CambiarIntencion(Deseo.Patrullar);
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

        // Perseguir: va directo a por él
        var perseguir = new GOAPAction("Perseguir", costo: 3f);
        perseguir.Precondiciones.Add("LadronLocalizado", true);
        perseguir.Efectos.Add("LadronAtrapado", true);
        acciones.Add(perseguir);

        // Emboscar: más barato, pero requiere que otro agente ya vigile la salida
        var emboscar = new GOAPAction("Emboscar", costo: 1f);
        emboscar.Precondiciones.Add("LadronLocalizado", true);
        emboscar.Precondiciones.Add("SalidaVigilada", true);
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
                if (search != null)
                {
                    bool terminado = search.Ejecutar(this);

                    if (terminado)
                        TerminarRolCoordinado("Investigacion");
                }
                else
                {
                    AsignarRol("");
                }
                break;

            case "VigilarSalida":
                if (guardExit != null)
                {
                    bool salidaRevisada = guardExit.Ejecutar(this);

                    if (salidaRevisada)
                        TerminarRolCoordinado("Salida");
                }
                else
                {
                    AsignarRol("");
                }
                break;

            case "VigilarLlave":
                if (checkKey != null)
                {
                    bool terminado = checkKey.Ejecutar(this);

                    if (terminado)
                    {
                        if (!checkKey.LlaveSigueEnSuSitio)
                        {
                            MessageRouter.ReforzarSalida(this);
                        }
                        else
                        {
                            AsignarRol("VigilarLlave");
                        }
                    }
                }
                else
                {
                    AsignarRol("");
                }
                break;

            case "VigilarBotones":
                if (guardButtons != null)
                {
                    bool botonesRevisados = guardButtons.Ejecutar(this);

                    if (botonesRevisados)
                        TerminarRolCoordinado("Botones");
                }
                else
                {
                    AsignarRol("");
                }
                break;

            default:
                AsignarRol("");
                break;
        }
    }

    private void TerminarRolCoordinado(string zona)
    {
        if (MessageRouter.BusquedaCoordinadaActiva && communicator != null)
        {
            communicator.InformarZonaDespejada(zona);
        }
        else
        {
            AsignarRol("");
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

        if (nueva != Deseo.InvestigarRuido) investigacionRuidoIniciada = false;
        if (Agent != null) Agent.ResetPath();

        Debug.Log($"[{gameObject.name}] Nueva intención: {intencionActual}");
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