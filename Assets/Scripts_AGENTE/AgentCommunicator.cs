using System.Collections.Generic;
using UnityEngine;

public class AgentCommunicator : MonoBehaviour
{
    [Header("Identidad")]
    public string agentId; // Ej: "Policia_1", "Policia_2"

    // Memoria del agente: Almacena conversaciones completas
    private List<FIPAMessage> history = new List<FIPAMessage>();
    
    // Referencia al cerebro para pasarle la información procesada
    private PoliceBrain brain;

    void Start()
    {
        brain = GetComponent<PoliceBrain>();
        
        // Si no le pones nombre, usa el nombre del GameObject
        if (string.IsNullOrEmpty(agentId)) agentId = gameObject.name;

        // Darse de alta en la red al nacer
        MessageRouter.RegisterAgent(this);
    }

    void OnDestroy()
    {
        // Darse de baja al morir/destruirse
        MessageRouter.UnregisterAgent(this);
    }

    /// <summary>
    /// Construye y envía un mensaje FIPA
    /// </summary>
    public void SendMessage(FIPAMessage.Performative perf, string receiver, string content, string convId = "")
    {
        FIPAMessage msg = new FIPAMessage
        {
            performative = perf,
            senderId = this.agentId,
            receiverId = receiver,
            content = content,
            // Si no hay ID de conversación, creamos uno nuevo
            conversationId = string.IsNullOrEmpty(convId) ? System.Guid.NewGuid().ToString() : convId,
            timestamp = System.DateTimeOffset.Now.ToUnixTimeMilliseconds()
        };

        history.Add(msg); // Guardo lo que yo mismo he dicho
        MessageRouter.RouteMessage(msg); // Lo mando a la red
    }

    /// <summary>
    /// Recibe un mensaje de la red
    /// </summary>
    public void ReceiveMessage(FIPAMessage msg)
    {
        history.Add(msg); // Guardar en el historial
        ProcesarMensaje(msg);
    }

    /// <summary>
    /// Analiza el mensaje e interactúa con el Cerebro
    /// </summary>
    private void ProcesarMensaje(FIPAMessage msg)
    {
        Debug.Log($"[{agentId}] Recibe {msg.performative} de {msg.senderId}: {msg.content}");

        // EJEMPLO DE LÓGICA (Aquí es donde debes conectar con la arquitectura BDI/Híbrida de tu Cerebro)
        switch (msg.performative)
        {
            case FIPAMessage.Performative.INFORM:
                // Si alguien informa de una posición, intentar parsearla
                Vector3 pos;
                if (TryParseVector3(msg.content, out pos))
                {
                    // Si estoy patrullando, voy a ayudar
                    if (brain.estadoActual == PoliceBrain.Estado.Patrullando)
                    {
                        brain.investigate.IniciarInvestigacion(pos, true);
                        brain.CambiarEstado(PoliceBrain.Estado.Investigando);
                    }
                }
                break;

            case FIPAMessage.Performative.CFP:
                // Responder a llamadas de formación de equipos
                break;
        }
    }

    // Helper para transformar el contenido del mensaje en un Vector3
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