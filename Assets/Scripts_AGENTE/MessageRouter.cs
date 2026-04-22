using System.Collections.Generic;
using UnityEngine;

public static class MessageRouter
{
    // Diccionario con todos los agentes registrados (Páginas Amarillas de FIPA)
    private static Dictionary<string, AgentCommunicator> agents = new Dictionary<string, AgentCommunicator>();

    public static void RegisterAgent(AgentCommunicator agent)
    {
        if (!agents.ContainsKey(agent.agentId))
        {
            agents.Add(agent.agentId, agent);
        }
    }

    public static void UnregisterAgent(AgentCommunicator agent)
    {
        if (agents.ContainsKey(agent.agentId))
        {
            agents.Remove(agent.agentId);
        }
    }

    public static void RouteMessage(FIPAMessage msg)
    {
        // Si el mensaje es para todos (Broadcast)
        if (msg.receiverId == "ALL")
        {
            foreach (var agent in agents.Values)
            {
                // No enviarse el mensaje a sí mismo
                if (agent.agentId != msg.senderId) 
                {
                    agent.ReceiveMessage(msg);
                }
            }
        }
        // Si el mensaje es privado para un agente en concreto
        else if (agents.ContainsKey(msg.receiverId))
        {
            agents[msg.receiverId].ReceiveMessage(msg);
        }
    }
}