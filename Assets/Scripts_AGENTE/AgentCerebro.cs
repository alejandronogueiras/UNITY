using UnityEngine;
using UnityEngine.AI;

public class PoliceBrain : MonoBehaviour
{
    public enum Estado
    {
        Patrullando, Investigando, Persiguiendo, Buscando, ComprobandoLlave, VigilandoSalida
    }

    public Estado estadoActual = Estado.Patrullando;
    public Estado estadoAnterior;

    [Header("Sensores")]
    public VisionSensor vision;
    public HearingSensor hearing;

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

    void Start()
    {
        Agent = GetComponent<NavMeshAgent>();
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

    // ── Callbacks de sensores ────────────────────────────────────────────────

    private void OnJugadorVisto(Vector3 pos)
    {
        if (search != null) search.SetUltimoPuntoVisto(pos);

        if (estadoActual != Estado.Persiguiendo)
        {
            AgentCommunicator comms = GetComponent<AgentCommunicator>();
            if (comms != null) comms.IniciarCFP(pos);
        }

        CambiarEstado(Estado.Persiguiendo);
    }

    private void OnJugadorPerdido()
    {
        if (estadoActual == Estado.Persiguiendo)
            CambiarEstado(Estado.Buscando);
    }

    private void OnRuidoDetectado(Vector3 punto, bool cerca)
    {
        if (estadoActual == Estado.Persiguiendo ||
            estadoActual == Estado.Investigando ||
            estadoActual == Estado.Buscando) return;

        estadoAnterior = estadoActual;
        if (investigate != null) investigate.IniciarInvestigacion(punto, !cerca);
        CambiarEstado(Estado.Investigando);
    }

    // ── Bucle principal ──────────────────────────────────────────────────────

    void Update()
    {
        if (Agent == null) return;
        GestionarEstados();
        ActualizarAnimacion();
    }

    void GestionarEstados()
    {
        switch (estadoActual)
        {
            case Estado.Patrullando:
                if (patrol != null) patrol.Ejecutar();
                break;
            case Estado.Investigando:
                if (investigate != null) investigate.Ejecutar(this);
                break;
            case Estado.Persiguiendo:
                if (chase != null) chase.Ejecutar(this);
                break;
            case Estado.Buscando:
                if (search != null) search.Ejecutar(this);
                break;
            case Estado.ComprobandoLlave:
                if (checkKey != null) checkKey.Ejecutar(this);
                break;
            case Estado.VigilandoSalida:
                if (guardExit != null) guardExit.Ejecutar(this);
                break;
        }
    }

    public void CambiarEstado(Estado nuevoEstado)
    {
        if (nuevoEstado == estadoActual) return;
        estadoActual = nuevoEstado;
        if (Agent != null) Agent.ResetPath();
    }

    void ActualizarAnimacion()
    {
        if (animator == null || Agent == null) return;

        float maxVel = chase != null ? chase.velocidadPersecucion : 6.5f;
        float speed01 = Agent.velocity.magnitude / Mathf.Max(0.01f, maxVel);
        float smooth = Mathf.Lerp(animator.GetFloat("Speed"), speed01, Time.deltaTime * animSmooth);
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
