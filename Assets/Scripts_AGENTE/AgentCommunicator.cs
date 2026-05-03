using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

public class AgentCommunicator : MonoBehaviour
{
    [Header("Identidad")]
    public string agentId;

    [Header("Decisión por distancia")]
    public float distanciaMaximaRespuesta = 800f;

    [Header("CFP")]
    public float tiempoEsperaPropuestas = 0.5f;

    private List<FIPAMessage> history = new List<FIPAMessage>();
    private PoliceBrain brain;

    // Estado CFP cuando soy el iniciador
    private bool esperandoPropuestas = false;
    private float timerPropuestas = 0f;
    private string convIdCFP = "";
    private Vector3 posicionCFP;

    // ── Ciclo de vida ────────────────────────────────────────────────────────

    void Start()
    {
        brain = GetComponent<PoliceBrain>();
        if (string.IsNullOrEmpty(agentId)) agentId = gameObject.name;
        MessageRouter.RegisterAgent(this);
    }

    void OnDestroy()
    {
        MessageRouter.UnregisterAgent(this);
    }

    void Update()
    {
        if (esperandoPropuestas)
        {
            timerPropuestas += Time.deltaTime;
            if (timerPropuestas >= tiempoEsperaPropuestas)
            {
                esperandoPropuestas = false;
                AsignarRoles();
            }
        }
    }


    // Expone el historial de forma segura (solo lectura) para que
    // PoliceBrain.ActualizarCreencias() pueda leer los CFPs recientes
    public IReadOnlyList<FIPAMessage> GetHistory()
    {
        return history.AsReadOnly();
    }

    public void SendMessage(FIPAMessage.Performative perf, string receiver, string content, string convId = "")
    {
        FIPAMessage msg = new FIPAMessage
        {
            performative    = perf,
            senderId        = agentId,
            receiverId      = receiver,
            content         = content,
            conversationId  = string.IsNullOrEmpty(convId) ? System.Guid.NewGuid().ToString() : convId,
            timestamp       = System.DateTimeOffset.Now.ToUnixTimeMilliseconds()
        };

        history.Add(msg);
        MessageRouter.RouteMessage(msg);
    }

    public void ReceiveMessage(FIPAMessage msg)
    {
        history.Add(msg);
        ProcesarMensaje(msg);
    }

    public void IniciarCFP(Vector3 posJugador)
    {
        convIdCFP       = System.Guid.NewGuid().ToString();
        posicionCFP     = posJugador;
        esperandoPropuestas = true;
        timerPropuestas = 0f;

        string jsonPos = JsonUtility.ToJson(posJugador);
        SendMessage(FIPAMessage.Performative.CFP, "ALL", jsonPos, convIdCFP);
        Debug.Log($"[{agentId}] CFP enviado — esperando propuestas");
    }

    public void InformarZonaDespejada(string zona)
    {
        string contenido = "ZONA_DESPEJADA:" + zona;

        SendMessage(
            FIPAMessage.Performative.INFORM,
            "ALL",
            contenido,
            "BUSQUEDA_COORDINADA"
        );

        MessageRouter.ReportarZonaDespejada(brain, zona);

        Debug.Log($"[{agentId}] Informa por radio: {zona} despejada.");
    }

    // ── Procesado de mensajes ────────────────────────────────────────────────

    private void ProcesarMensaje(FIPAMessage msg)
    {
        if (brain == null)
        {
            Debug.LogWarning($"[{agentId}] No tiene PoliceBrain asociado.");
            return;
        }

        switch (msg.performative)
        {
            case FIPAMessage.Performative.CFP:
                ProcesarCFP(msg);
                break;

            case FIPAMessage.Performative.PROPOSE:
                if (esperandoPropuestas && msg.conversationId == convIdCFP)
                    Debug.Log($"[{agentId}] PROPOSE recibido de {msg.senderId} — distancia: {msg.content}u");
                break;

            case FIPAMessage.Performative.ACCEPT_PROPOSAL:
                Debug.Log($"[{agentId}] Rol aceptado: {msg.content.Split(':')[0]}");
                EjecutarRol(msg.content);
                break;

            case FIPAMessage.Performative.REJECT_PROPOSAL:
                Debug.Log($"[{agentId}] Propuesta rechazada, sigo patrullando");
                break;

            case FIPAMessage.Performative.INFORM:
                if (msg.content.StartsWith("ZONA_DESPEJADA:"))
                {
                    string zona = msg.content.Replace("ZONA_DESPEJADA:", "");
                    Debug.Log($"[{agentId}] Recibido informe: {zona} despejada por {msg.senderId}");
                }
                break;
        }
    }

    private void ProcesarCFP(FIPAMessage msg)
    {
        Vector3 pos;

        if (!TryParseVector3(msg.content, out pos))
            return;

        float distancia = Vector3.Distance(transform.position, pos);

        bool disponible =
            brain.intencionActual == PoliceBrain.Deseo.Patrullar &&
            string.IsNullOrEmpty(brain.creencias.rolAsignado);

        bool cercano = distancia <= distanciaMaximaRespuesta;

        if (disponible && cercano)
        {
            SendMessage(
                FIPAMessage.Performative.PROPOSE,
                msg.senderId,
                distancia.ToString("F2", CultureInfo.InvariantCulture),
                msg.conversationId
            );

            Debug.Log($"[{agentId}] PROPOSE enviado a {msg.senderId} — distancia: {distancia:F1}u");
        }
        else
        {
            Debug.Log($"[{agentId}] CFP ignorado — {(!disponible ? "ocupado o con rol" : "demasiado lejos")}");
        }
    }

    // ── Asignación de roles ──────────────────────────────────────────────────

    private void AsignarRoles()
    {
        List<FIPAMessage> propuestas = history
            .Where(m => m.conversationId == convIdCFP &&
                        m.performative   == FIPAMessage.Performative.PROPOSE)
            .OrderBy(m => 
            {
                //evitamos que un Parse mal formado rompa el juego.
                if (float.TryParse(m.content, NumberStyles.Float, CultureInfo.InvariantCulture, out float d))
                    return d;

                return float.MaxValue;
            })
            .ToList();

        Debug.Log($"[{agentId}] Asignando roles — {propuestas.Count} propuesta(s) recibida(s)");

        string[] roles = { "Investigar", "VigilarSalida", "VigilarLlave" };
        string jsonPos = JsonUtility.ToJson(posicionCFP);

        for (int i = 0; i < propuestas.Count; i++)
        {
            string rol = i < roles.Length ? roles[i] : "Investigar";
            Debug.Log($"[{agentId}] Asigna rol '{rol}' a {propuestas[i].senderId}");
            SendMessage(FIPAMessage.Performative.ACCEPT_PROPOSAL,
                        propuestas[i].senderId, rol + ":" + jsonPos, convIdCFP);
        }
    }

    // Actualiza las Creencias del cerebro BDI en lugar de cambiar el estado directamente.
    // El ciclo BDI de PoliceBrain.Update() se encargará de ejecutar el comportamiento adecuado.
    private void EjecutarRol(string contenido)
    {
        int sep      = contenido.IndexOf(':');
        //validación para evitar errores si el contenido llega mal.
        if (sep <= 0)
        {
            Debug.LogWarning($"[{agentId}] Mensaje de rol mal formado: {contenido}");
            return;
        }

        string rol   = contenido.Substring(0, sep);
        string jsonPos = contenido.Substring(sep + 1);

        Vector3 pos;
        TryParseVector3(jsonPos, out pos);

        Debug.Log($"[{agentId}] Ejecutando rol '{rol}'");

        // Delegar en el cerebro BDI: solo actualizamos la creencia
        brain.AsignarRol(rol);

        // Si el rol implica buscar en una posición concreta, la registramos en el actuador
        if (rol == "Investigar" && brain.search != null)
            brain.search.SetUltimoPuntoVisto(pos);
    }

    // ── Utilidades ───────────────────────────────────────────────────────────

    private bool TryParseVector3(string data, out Vector3 result)
    {
        try
        {
            result = JsonUtility.FromJson<Vector3>(data);
            return true;
        }
        catch
        {
            result = Vector3.zero;
            return false;
        }
    }
}
