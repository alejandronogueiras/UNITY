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
    
    // Propiedad pública para que los comportamientos accedan al NavMeshAgent
    public NavMeshAgent Agent { get; private set; }

    void Start()
    {
        Agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (Agent == null) return;

        Detectar();
        GestionarEstados();
        ActualizarAnimacion();
    }

    void Detectar()
    {
        if (estadoActual == Estado.Buscando) return;

        // Transición a Persecución
        if (vision != null && vision.CanSeePlayer)
        {
            search.SetUltimoPuntoVisto(vision.LastSeenPosition);
            CambiarEstado(Estado.Persiguiendo);
            return;
        }

        // Transición a Investigación
        if (hearing != null && (hearing.EscuchaCerca || hearing.EscuchaLejos))
        {
            if (estadoActual != Estado.Persiguiendo && estadoActual != Estado.Investigando)
            {
                estadoAnterior = estadoActual;
                investigate.IniciarInvestigacion(hearing.PuntoOido, hearing.EscuchaLejos);
                CambiarEstado(Estado.Investigando);
            }
        }
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
        estadoActual = nuevoEstado;
        if (Agent != null) Agent.ResetPath();
    }

    void ActualizarAnimacion()
    {
        if (animator == null || Agent == null) return;

        // Usamos la velocidad máxima configurada en el chase behaviour para normalizar
        float maxVel = chase != null ? chase.velocidadPersecucion : 6.5f;
        float speed01 = Agent.velocity.magnitude / Mathf.Max(0.01f, maxVel);
        float smooth = Mathf.Lerp(animator.GetFloat("Speed"), speed01, Time.deltaTime * animSmooth);
        animator.SetFloat("Speed", smooth);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // GameManager.instance.Perder(); // Descomenta si tienes tu GameManager
            Debug.Log("¡Jugador Atrapado!");
        }
    }
}