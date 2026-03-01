
using UnityEngine;
using UnityEngine.AI;

public class PoliceBrain : MonoBehaviour
{
    private bool salidaAsegurada = false;
    private bool protegiendoLlave = false;
    private bool llaveRobada = false;

    private bool investigandoZona = false;
    private float tiempoInvestigacion = 4f;
    private float timerInvestigacion = 0f;
    private Vector3 centroInvestigacion;
    public float radioInvestigacion = 5f;
    private Estado estadoAnterior;

    public enum Estado
    {
        Patrullando,
        Investigando,
        Persiguiendo,
        Buscando,
        ComprobandoLlave,
        VigilandoSalida
    }

    public Estado estadoActual = Estado.Patrullando;

    [Header("Sensores")]
    public VisionSensor vision;
    public HearingSensor hearing;

    [Header("Actuadores")]
    public PatrolBehaviour patrol;

    [Header("Referencias")]
    public Transform jugador;
    public Transform salida;

    [Header("Zonas")]
    public Transform zonaLlaveCentro;
    public float zonaLlaveRadio = 8f;

    [Header("Movimiento")]
    public float velocidadNormal = 3.5f;
    public float velocidadPersecucion = 6.5f;

    [Header("Animación")]
    public Animator animator;
    public float animSmooth = 8f;

    private NavMeshAgent agent;
    private Vector3 ultimoPuntoVisto;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.speed = velocidadNormal;

        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (agent == null) return;

        Detectar();
        GestionarEstados();
        ActualizarAnimacion();
    }


    void Detectar()
    {
        if (estadoActual == Estado.Buscando)
            return;

        if (vision != null && vision.CanSeePlayer)
        {
            ultimoPuntoVisto = vision.LastSeenPosition;
            estadoActual = Estado.Persiguiendo;
            return;
        }

        if (hearing != null)
        {
            if (hearing.EscuchaCerca || hearing.EscuchaLejos)
            {
                if (estadoActual != Estado.Persiguiendo &&
                    estadoActual != Estado.Investigando)
                {
                    estadoAnterior = estadoActual;
                    estadoActual = Estado.Investigando;

                    investigandoZona = hearing.EscuchaLejos;
                    centroInvestigacion = hearing.PuntoOido;
                    timerInvestigacion = 0f;

                    agent.ResetPath();
                }
            }
        }
    }

    void GestionarEstados()
    {
        switch (estadoActual)
        {
            case Estado.Patrullando:
                Debug.Log(">>> ESTADO: PATRULLANDO");
                agent.speed = velocidadNormal;
                agent.isStopped = false;

                if (patrol != null)
                    patrol.Ejecutar();
                break;


            case Estado.Investigando:

                Debug.Log(">>> ESTADO: INVESTIGANDO");      
                agent.speed = velocidadNormal;
                agent.isStopped = false;

                
                if (!investigandoZona && hearing.EscuchaCerca)
                {
                    agent.SetDestination(hearing.PuntoOido);

                    if (!agent.pathPending && agent.remainingDistance < 0.6f)
                    {
                        FinalizarInvestigacion();
                    }
                }
                
                else if (investigandoZona)
                {
                    timerInvestigacion += Time.deltaTime;

                    if (!agent.hasPath)
                    {
                        Vector3 punto = centroInvestigacion + Random.insideUnitSphere * radioInvestigacion;
                        punto.y = transform.position.y;

                        if (NavMesh.SamplePosition(punto, out NavMeshHit hit, radioInvestigacion, NavMesh.AllAreas))
                        {
                            agent.SetDestination(hit.position);
                        }
                    }

                    if (timerInvestigacion >= tiempoInvestigacion)
                    {
                        investigandoZona = false;
                        FinalizarInvestigacion();
                    }
                }

                break;
           
            case Estado.Persiguiendo:
                Debug.Log(">>> ESTADO: PERSIGUIENDO");
                agent.speed = velocidadPersecucion;
                agent.isStopped = false;

                if (jugador != null)
                    agent.SetDestination(jugador.position);

                if (!vision.CanSeePlayer)
                {
                    estadoActual = Estado.Buscando;
                    agent.ResetPath();
                }

                break;

            
            case Estado.Buscando:
            
                Debug.Log(">>> ESTADO: BUSCANDO");
                agent.speed = velocidadNormal;
                agent.isStopped = false;

                agent.SetDestination(ultimoPuntoVisto);

                if (!agent.pathPending && agent.remainingDistance < 0.6f)
                {
                    agent.ResetPath();

                    estadoActual = Estado.ComprobandoLlave;
                }

                break;

            
            case Estado.ComprobandoLlave:
                Debug.Log(">>> ESTADO: COMPROBANDO LLAVE");
                agent.speed = velocidadNormal;
                agent.isStopped = false;

                if (zonaLlaveCentro != null)
                    agent.SetDestination(zonaLlaveCentro.position);

                if (!agent.pathPending && agent.remainingDistance < 0.6f)
                {
                    GameObject llave = GameObject.FindWithTag("Llave");

                    if (llave == null)
                    {
                        protegiendoLlave = false;
                        llaveRobada = true;
                        estadoActual = Estado.VigilandoSalida;
                        agent.ResetPath();
                    }
                    else
                    {
                        protegiendoLlave = true;

                        if (patrol != null && zonaLlaveCentro != null)
                        {
                            patrol.zonas.Clear();
                            patrol.zonas.Add(new PatrolBehaviour.PatrolZone
                            {
                                centro = zonaLlaveCentro,
                                radio = zonaLlaveRadio
                            });
                        }

                        estadoActual = Estado.Patrullando;
                        agent.ResetPath();
                    }
                }

                break;

            
            case Estado.VigilandoSalida:
                Debug.Log(">>> ESTADO: VIGILANDO SALIDA");
                agent.speed = velocidadNormal;
                agent.isStopped = false;

                if (!salidaAsegurada)
                {
                    if (salida != null)
                        agent.SetDestination(salida.position);

                    if (!agent.pathPending && agent.remainingDistance < 0.6f)
                    {
                        if (patrol != null && salida != null)
                        {
                            patrol.zonas.Clear();
                            patrol.zonas.Add(new PatrolBehaviour.PatrolZone
                            {
                                centro = salida,
                                radio = 6f
                            });
                        }

                        salidaAsegurada = true;
                        estadoActual = Estado.Patrullando;
                        agent.ResetPath();
                    }
                }

                break;
        }
    }


    void FinalizarInvestigacion()
    {
        agent.ResetPath();

        
        if (estadoAnterior == Estado.Investigando)
            estadoActual = Estado.Patrullando;
        else
            estadoActual = estadoAnterior;
    }

    void ActualizarAnimacion()
    {
        if (animator == null || agent == null) return;

        float speed01 = agent.velocity.magnitude / Mathf.Max(0.01f, velocidadPersecucion);
        float smooth = Mathf.Lerp(animator.GetFloat("Speed"), speed01, Time.deltaTime * animSmooth);
        animator.SetFloat("Speed", smooth);
    }
}