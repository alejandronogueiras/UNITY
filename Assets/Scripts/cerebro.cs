using UnityEngine;
using UnityEngine.AI;

public class cerebro : MonoBehaviour
{
    public enum Estado { Patrullando, Investigando, Buscando, Persiguiendo }
    public Estado estadoActual = Estado.Patrullando;

    [Header("Ruta")]
    public Transform[] puntosDeRuta;
    private int indiceActual = 0;

    [Header("Visión (Ojos)")]
    public Transform jugador;
    public float rangoVision = 10f;
    public float anguloVision = 90f;
    public float rangoEscape = 15f;

    [Header("Audición (Orejas)")]
    public float rangoAudicion = 5f;

    [Header("Comportamiento de Búsqueda")]
    public float tiempoDeBusqueda = 3f;
    public float velocidadGiro = 4f;
    public float anguloDeGiro = 60f;

    [Header("Puertas")]
    public float distanciaAnticipacionPuerta = 6f;
    public LayerMask doorLayers = ~0;

    [Header("Animación")]
    public Animator animator;                 // <-- AQUÍ LO ARRASTRAS (componente Animator)
    public float animSmooth = 8f;            // suavizado

    private NavMeshAgent agent;
    private PlayerLookMove scriptJugador;

    private Vector3 puntoDeInteres;
    private float temporizadorBusqueda = 0f;
    private float rotacionInicialY;

    private LineRenderer lineaVision;
    private LineRenderer lineaAudicion;

    private Color colorTranquilo = Color.red;
    private Color colorAlerta = Color.green;


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (jugador != null)
            scriptJugador = jugador.GetComponent<PlayerLookMove>();

        agent.autoRepath = true;

        if (puntosDeRuta != null && puntosDeRuta.Length > 0 && puntosDeRuta[indiceActual] != null)
            agent.destination = puntosDeRuta[indiceActual].position;

        lineaVision = CrearLineaVisual(colorTranquilo, "LineaVision");
        lineaAudicion = CrearLineaVisual(Color.cyan, "LineaAudicion");

        // Si no lo asignaste a mano, intenta pillarlo del mismo objeto
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // --- 1. DIBUJOS ---
        if (lineaVision != null)
            DibujarConoVision(lineaVision, rangoVision, anguloVision);

        if (lineaAudicion != null)
            DibujarCirculo(lineaAudicion, rangoAudicion);

        if (lineaVision != null)
        {
            if (estadoActual == Estado.Persiguiendo)
            {
                lineaVision.startColor = colorAlerta;
                lineaVision.endColor = colorAlerta;
            }
            else
            {
                lineaVision.startColor = colorTranquilo;
                lineaVision.endColor = colorTranquilo;
            }
        }

        // --- 2. DETECCIÓN ---
        if (jugador == null) return; // evita nulls

        Vector3 direccionAlJugador = jugador.position - transform.position;
        float distancia = direccionAlJugador.magnitude;
        float angulo = Vector3.Angle(transform.forward, direccionAlJugador);

        bool loVe = false;

        if (distancia < rangoVision && angulo < (anguloVision / 2f))
        {
            Vector3 origenOjos = transform.position + Vector3.up * 1f;
            Vector3 destinoCuerpo = jugador.position + Vector3.up * 1f;
            Vector3 direccionRayo = destinoCuerpo - origenOjos;

            if (Physics.Raycast(origenOjos, direccionRayo, out RaycastHit impacto, rangoVision))
            {
                if (impacto.transform.IsChildOf(jugador))
                    loVe = true;
            }
        }

        bool loEscucha = false;
        if (scriptJugador != null)
            loEscucha = scriptJugador.estaHaciendoRuido && distancia < (rangoAudicion + scriptJugador.radioRuido);

        // --- 3. CAMBIOS DE ESTADO ---
        if (loVe)
        {
            estadoActual = Estado.Persiguiendo;
            agent.isStopped = false;
        }
        else if (estadoActual == Estado.Persiguiendo && distancia > rangoEscape)
        {
            estadoActual = Estado.Patrullando;
        }
        else if (loEscucha && estadoActual != Estado.Persiguiendo)
        {
            estadoActual = Estado.Investigando;
            puntoDeInteres = jugador.position;
            agent.isStopped = false;
        }

        // --- 4. ACCIONES ---
        switch (estadoActual)
        {
            case Estado.Patrullando:  Patrullar(); break;
            case Estado.Investigando: Investigar(); break;
            case Estado.Buscando:     BuscarAAlrededor(); break;
            case Estado.Persiguiendo: Perseguir(); break;
        }

        // --- 5. ANIMACIÓN desde la velocidad real del NavMeshAgent ---
        ActualizarAnimacion();
    }

    void ActualizarAnimacion()
    {
        if (animator == null || agent == null) return;

        // velocidad real del NavMeshAgent
        float speed01 = agent.velocity.magnitude / Mathf.Max(0.01f, agent.speed);

        // suavizado
        float smooth = Mathf.Lerp(animator.GetFloat("Speed"), speed01, Time.deltaTime * animSmooth);

        animator.SetFloat("Speed", smooth);
    }
    void IntentarAbrirPuertaHacia(Vector3 target)
    {
        Vector3 origen = transform.position + Vector3.up * 1f;
        Vector3 dir = (target - transform.position);
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f) return;

        dir.Normalize();

        if (Physics.Raycast(origen, dir, out RaycastHit hit, distanciaAnticipacionPuerta, doorLayers, QueryTriggerInteraction.Collide))
        {
            DoorController door = hit.collider.GetComponentInParent<DoorController>();
            if (door != null)
            {
                door.ForceOpen();
                agent.ResetPath();
                agent.SetDestination(target);
            }
        }
    }

    void Perseguir()
    {
        IntentarAbrirPuertaHacia(jugador.position);
        agent.destination = jugador.position;
    }

    void Investigar()
    {
        IntentarAbrirPuertaHacia(puntoDeInteres);
        agent.destination = puntoDeInteres;

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            estadoActual = Estado.Buscando;
            temporizadorBusqueda = 0f;
            rotacionInicialY = transform.eulerAngles.y;
            agent.isStopped = true;
        }
    }

    void BuscarAAlrededor()
    {
        temporizadorBusqueda += Time.deltaTime;
        float rotacionPendulo = Mathf.Sin(temporizadorBusqueda * velocidadGiro) * anguloDeGiro;
        transform.rotation = Quaternion.Euler(0, rotacionInicialY + rotacionPendulo, 0);

        if (temporizadorBusqueda >= tiempoDeBusqueda)
        {
            estadoActual = Estado.Patrullando;
            agent.isStopped = false;
        }
    }

    void Patrullar()
    {
        if (puntosDeRuta == null || puntosDeRuta.Length == 0) return;
        if (puntosDeRuta[indiceActual] == null) return;

        Vector3 target = puntosDeRuta[indiceActual].position;

        IntentarAbrirPuertaHacia(target);

        agent.destination = target;

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            indiceActual = (indiceActual + 1) % puntosDeRuta.Length;
        }
    }

    LineRenderer CrearLineaVisual(Color color, string nombre)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(this.transform);
        go.transform.localPosition = Vector3.zero;

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.startWidth = 0.08f;
        lr.endWidth = 0.08f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
        lr.useWorldSpace = true;
        lr.loop = true;
        return lr;
    }

    void DibujarCirculo(LineRenderer lr, float radio)
    {
        if (lr == null) return;

        int segmentos = 40;
        lr.positionCount = segmentos;
        for (int i = 0; i < segmentos; i++)
        {
            float ang = (i / (float)segmentos) * 360f;
            Vector3 direccion = Quaternion.Euler(0, ang, 0) * Vector3.forward;
            lr.SetPosition(i, transform.position + Vector3.up * 0.2f + direccion * radio);
        }
    }

    void DibujarConoVision(LineRenderer lr, float radio, float angulo)
    {
        if (lr == null) return;

        int segmentos = 20;
        lr.positionCount = segmentos + 2;

        lr.SetPosition(0, transform.position + Vector3.up * 0.2f);

        float anguloInicial = -angulo / 2f;
        float pasoAngulo = angulo / segmentos;

        for (int i = 0; i <= segmentos; i++)
        {
            float anguloActual = anguloInicial + (pasoAngulo * i);
            Vector3 direccion = Quaternion.Euler(0, anguloActual, 0) * transform.forward;
            lr.SetPosition(i + 1, transform.position + Vector3.up * 0.2f + direccion * radio);
        }
    }
}