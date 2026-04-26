using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AgentCommunicator : MonoBehaviour
{
    [Header("Identidad")]
    public string agentId;

    [Header("Decisión por distancia")]
    public float distanciaMaximaRespuesta = 9999f;

    [Header("CFP")]
    public float tiempoEsperaPropuestas = 0.5f;

    private List<FIPAMessage> history = new List<FIPAMessage>();
    private PoliceBrain brain;

    // Estado CFP cuando soy el iniciador
    private bool esperandoPropuestas = false;
    private float timerPropuestas = 0f;
    private string convIdCFP = "";
    private Vector3 posicionCFP;

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

    public void SendMessage(FIPAMessage.Performative perf, string receiver, string content, string convId = "")
    {
        FIPAMessage msg = new FIPAMessage
        {
            performative = perf,
            senderId = agentId,
            receiverId = receiver,
            content = content,
            conversationId = string.IsNullOrEmpty(convId) ? System.Guid.NewGuid().ToString() : convId,
            timestamp = System.DateTimeOffset.Now.ToUnixTimeMilliseconds()
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
        convIdCFP = System.Guid.NewGuid().ToString();
        posicionCFP = posJugador;
        esperandoPropuestas = true;
        timerPropuestas = 0f;

        string jsonPos = JsonUtility.ToJson(posJugador);
        SendMessage(FIPAMessage.Performative.CFP, "ALL", jsonPos, convIdCFP);
        Debug.Log($"[{agentId}] CFP enviado — esperando propuestas");
    }

    private void ProcesarMensaje(FIPAMessage msg)
    {
        switch (msg.performative)
        {
            case FIPAMessage.Performative.CFP:
                Vector3 pos;
                if (TryParseVector3(msg.content, out pos))
                {
                    float distancia = Vector3.Distance(transform.position, pos);
                    bool disponible = brain.estadoActual == PoliceBrain.Estado.Patrullando;
                    bool cercano = distancia <= distanciaMaximaRespuesta;

                    if (disponible && cercano)
                    {
                        SendMessage(FIPAMessage.Performative.PROPOSE, msg.senderId,
                                    distancia.ToString("F2", System.Globalization.CultureInfo.InvariantCulture), msg.conversationId);
                        Debug.Log($"[{agentId}] PROPOSE enviado a {msg.senderId} — distancia: {distancia:F1}u");
                    }
                    else
                    {
                        Debug.Log($"[{agentId}] CFP ignorado — {(!disponible ? "ocupado" : "demasiado lejos")}");
                    }
                }
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
        }
    }

    private void AsignarRoles()
    {
        List<FIPAMessage> propuestas = history
            .Where(m => m.conversationId == convIdCFP &&
                        m.performative == FIPAMessage.Performative.PROPOSE)
            .OrderBy(m => float.Parse(m.content, System.Globalization.CultureInfo.InvariantCulture))
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

    private void EjecutarRol(string contenido)
    {
        int sep = contenido.IndexOf(':');
        string rol = contenido.Substring(0, sep);
        string jsonPos = contenido.Substring(sep + 1);

        Vector3 pos;
        TryParseVector3(jsonPos, out pos);

        Debug.Log($"[{agentId}] Ejecutando rol '{rol}'");
        switch (rol)
        {
            case "Investigar":
                brain.investigate.IniciarInvestigacion(pos, true);
                brain.CambiarEstado(PoliceBrain.Estado.Investigando);
                break;

            case "VigilarSalida":
                brain.CambiarEstado(PoliceBrain.Estado.VigilandoSalida);
                break;

            case "VigilarLlave":
                brain.CambiarEstado(PoliceBrain.Estado.ComprobandoLlave);
                break;
        }
    }

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
