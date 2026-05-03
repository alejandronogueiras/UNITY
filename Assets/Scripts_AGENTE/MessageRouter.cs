using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class MessageRouter
{
    private static Dictionary<string, AgentCommunicator> agents = new Dictionary<string, AgentCommunicator>();
    private static List<FIPAMessage> globalHistory = new List<FIPAMessage>();

    private static bool busquedaCoordinadaActiva = false;
    private static bool patrullaAlertaConfigurada = false;

    private static HashSet<string> zonasDespejadas = new HashSet<string>();
    private static Dictionary<string, string> zonaAsignadaPorAgente = new Dictionary<string, string>();

    public static bool BusquedaCoordinadaActiva => busquedaCoordinadaActiva;

    public static void RegisterAgent(AgentCommunicator agent)
    {
        if (agent == null) return;

        if (string.IsNullOrEmpty(agent.agentId))
        {
            Debug.LogWarning($"[{agent.gameObject.name}] Tiene agentId vacío. No se puede registrar correctamente.");
            return;
        }

        if (agents.ContainsKey(agent.agentId))
        {
            Debug.LogWarning($"Ya existe un agente registrado con id '{agent.agentId}'. Se reemplaza por el nuevo.");
            agents[agent.agentId] = agent;
        }
        else
        {
            agents.Add(agent.agentId, agent);
        }

        Debug.Log($"[MessageRouter] Agente registrado: {agent.agentId}. Total agentes: {agents.Count}");
    }

    public static void UnregisterAgent(AgentCommunicator agent)
    {
        if (agent == null) return;

        if (agents.ContainsKey(agent.agentId) && agents[agent.agentId] == agent)
        {
            agents.Remove(agent.agentId);
            Debug.Log($"[MessageRouter] Agente eliminado: {agent.agentId}. Total agentes: {agents.Count}");
        }
    }

    public static void RouteMessage(FIPAMessage msg)
    {
        if (msg == null) return;

        if (msg.performative == FIPAMessage.Performative.CFP)
        {
            CancelarBusquedaCoordinada();
            ActivarPatrullaDeAlerta();
        }

        globalHistory.Add(msg);

        if (msg.receiverId == "ALL")
        {
            Debug.Log($"[MessageRouter] Broadcast {msg.performative} de {msg.senderId} a {agents.Count - 1} agente(s).");

            foreach (var agent in agents.Values)
            {
                if (agent.agentId != msg.senderId)
                    agent.ReceiveMessage(msg);
            }
        }
        else if (agents.ContainsKey(msg.receiverId))
        {
            agents[msg.receiverId].ReceiveMessage(msg);
        }
        else
        {
            Debug.LogWarning($"No existe agente receptor con id '{msg.receiverId}'. Mensaje perdido.");
        }
    }

    public static void ReforzarSalida(PoliceBrain brain)
    {
        if (brain == null)
            return;

        string agentId = ObtenerAgentId(brain);

        Transform salida = PrimerTargetSalida(GetBrainsRegistrados());

        if (salida == null)
        {
            Debug.LogWarning($"[MessageRouter] No se puede reforzar salida con {agentId}: falta target de salida.");
            brain.AsignarRol("");
            return;
        }

        if (zonaAsignadaPorAgente.ContainsKey(agentId))
            zonaAsignadaPorAgente[agentId] = "Salida";
        else
            zonaAsignadaPorAgente.Add(agentId, "Salida");

        brain.AsignarRol("VigilarSalida");

        Debug.Log($"[MessageRouter] {agentId} refuerza la salida porque la llave no está.");
    }

    public static int GetNivelAlertaGlobal(long desdeTimestamp)
    {
        globalHistory.RemoveAll(m => m.timestamp < desdeTimestamp);

        return globalHistory
            .Where(m => m.performative == FIPAMessage.Performative.CFP &&
                        m.timestamp > desdeTimestamp)
            .Select(m => m.senderId)
            .Distinct()
            .Count();
    }

    public static bool HayAlgunAgenteViendoLadron()
    {
        foreach (PoliceBrain brain in GetBrainsRegistrados())
        {
            if (brain.creencias.jugadorDetectado)
                return true;
        }

        return false;
    }

    public static bool TodosHanPerdidoLadron()
    {
        List<PoliceBrain> brains = GetBrainsRegistrados();

        if (brains.Count == 0)
            return false;

        return !HayAlgunAgenteViendoLadron();
    }

    public static bool IntentarIniciarBusquedaCoordinada(int nivelAlertaGlobal)
    {
        if (busquedaCoordinadaActiva)
            return false;

        if (nivelAlertaGlobal < 2)
            return false;

        if (!TodosHanPerdidoLadron())
            return false;

        busquedaCoordinadaActiva = true;
        zonasDespejadas.Clear();
        zonaAsignadaPorAgente.Clear();

        Debug.Log("[MessageRouter] BÚSQUEDA COORDINADA ACTIVADA.");

        List<PoliceBrain> brains = GetBrainsRegistrados();

        foreach (PoliceBrain brain in brains)
        {
            if (!brain.creencias.jugadorDetectado)
                brain.AsignarRol("");
        }

        ReasignarAgentesLibres();

        return true;
    }

    public static void ActivarPatrullaDeAlerta()
    {
        if (patrullaAlertaConfigurada)
        {
            return;
        }

        List<PoliceBrain> brains = GetBrainsRegistrados();

        if (brains.Count == 0)
            return;

        patrullaAlertaConfigurada = true;

        Debug.Log("[MessageRouter] PATRULLA DE ALERTA ACTIVADA. Se sustituyen las patrullas iniciales por zonas críticas.");

        AsignarPatrullasCriticas(brains);
    }

    private static void AsignarPatrullasCriticas(List<PoliceBrain> brains)
    {
        List<PoliceBrain> libres = new List<PoliceBrain>(brains);

        Transform targetLlave = PrimerTargetLlave(brains);
        Transform targetSalida = PrimerTargetSalida(brains);
        Transform targetBotones = PrimerTargetBotones(brains);

        // 1 agente patrullará la llave.
        AsignarPatrullaMasCercana(libres, targetLlave, "Patrulla Llave", 8f);

        // 2 agentes patrullarán la salida.
        AsignarPatrullaMasCercana(libres, targetSalida, "Patrulla Salida", 6f);
        AsignarPatrullaMasCercana(libres, targetSalida, "Patrulla Salida", 6f);

        // 1 agente patrullará los botones.
        AsignarPatrullaMasCercana(libres, targetBotones, "Patrulla Botones", 6f);

        // Si sobran agentes, los mandamos también a la salida,
        // porque suele ser la zona de escape más importante.
        foreach (PoliceBrain restante in libres)
        {
            ConfigurarZonaPatrulla(restante, targetSalida, 6f, "Patrulla Refuerzo Salida");
        }
    }

    private static void AsignarPatrullaMasCercana(
        List<PoliceBrain> candidatos,
        Transform objetivo,
        string nombreZona,
        float radio)
    {
        if (candidatos.Count == 0)
            return;

        PoliceBrain elegido;

        if (objetivo != null)
        {
            elegido = candidatos
                .OrderBy(b => Vector3.Distance(b.transform.position, objetivo.position))
                .First();
        }
        else
        {
            elegido = candidatos[0];
        }

        candidatos.Remove(elegido);

        ConfigurarZonaPatrulla(elegido, objetivo, radio, nombreZona);
    }

    private static void ConfigurarZonaPatrulla(
        PoliceBrain brain,
        Transform centro,
        float radio,
        string nombreZona)
    {
        if (brain == null)
            return;

        if (brain.patrol == null)
        {
            Debug.LogWarning($"[{brain.gameObject.name}] No tiene PatrolBehaviour. No se puede configurar {nombreZona}.");
            return;
        }

        if (centro == null)
        {
            Debug.LogWarning($"[{brain.gameObject.name}] No se puede configurar {nombreZona}: falta el punto central.");
            return;
        }

        brain.patrol.zonas.Clear();

        brain.patrol.zonas.Add(new PatrolBehaviour.PatrolZone
        {
            centro = centro,
            radio = radio
        });

        if (brain.intencionActual == PoliceBrain.Deseo.Patrullar)
        {
            brain.patrol.ReiniciarDestino();
        }

        Debug.Log($"[Patrulla de Alerta] {brain.gameObject.name} -> {nombreZona}");
    }
    
    public static void CancelarBusquedaCoordinada()
    {
        if (!busquedaCoordinadaActiva)
            return;

        busquedaCoordinadaActiva = false;
        zonasDespejadas.Clear();
        zonaAsignadaPorAgente.Clear();

        foreach (PoliceBrain brain in GetBrainsRegistrados())
        {
            if (EsRolBusquedaCoordinada(brain.creencias.rolAsignado))
                brain.AsignarRol("");
        }

        Debug.Log("[MessageRouter] Búsqueda coordinada cancelada porque alguien volvió a detectar al ladrón.");
    }

    public static void ReportarZonaDespejada(PoliceBrain brain, string zona)
    {
        if (!busquedaCoordinadaActiva)
            return;

        if (brain == null)
            return;

        string agentId = ObtenerAgentId(brain);

        zonasDespejadas.Add(zona);

        if (zonaAsignadaPorAgente.ContainsKey(agentId))
            zonaAsignadaPorAgente.Remove(agentId);

        brain.AsignarRol("");

        Debug.Log($"[MessageRouter] {agentId} reporta zona despejada: {zona}");

        // Si dos agentes iban a la misma salida, y uno ya la ha despejado,
        // el otro no debe seguir perdiendo tiempo allí.
        List<string> agentesEnMismaZona = zonaAsignadaPorAgente
            .Where(kvp => kvp.Value == zona)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (string otroId in agentesEnMismaZona)
        {
            zonaAsignadaPorAgente.Remove(otroId);

            if (agents.ContainsKey(otroId))
            {
                PoliceBrain otroBrain = agents[otroId].GetComponent<PoliceBrain>();

                if (otroBrain != null && EsRolBusquedaCoordinada(otroBrain.creencias.rolAsignado))
                    otroBrain.AsignarRol("");
            }
        }

        if (TodasLasZonasCriticasDespejadas())
        {
            busquedaCoordinadaActiva = false;
            zonaAsignadaPorAgente.Clear();

            Debug.Log("[MessageRouter] Todas las zonas críticas están despejadas. Volviendo a patrulla/alerta.");

            foreach (PoliceBrain b in GetBrainsRegistrados())
            {
                if (EsRolBusquedaCoordinada(b.creencias.rolAsignado))
                    b.AsignarRol("");
            }

            return;
        }

        ReasignarAgentesLibres();
    }

    private static void ReasignarAgentesLibres()
    {
        List<PoliceBrain> brains = GetBrainsRegistrados();

        foreach (PoliceBrain brain in brains)
        {
            if (brain.creencias.jugadorDetectado)
                continue;

            if (!string.IsNullOrEmpty(brain.creencias.rolAsignado))
                continue;

            AsignarMejorZonaPendiente(brain, brains);
        }
    }

    private static void AsignarMejorZonaPendiente(PoliceBrain brain, List<PoliceBrain> todos)
    {
        List<TareaBusqueda> tareas = ObtenerTareasPendientes(todos);

        if (tareas.Count == 0)
        {
            brain.AsignarRol("");
            return;
        }

        TareaBusqueda mejor = tareas
            .OrderBy(t => Vector3.Distance(brain.transform.position, t.target.position))
            .First();

        string agentId = ObtenerAgentId(brain);

        zonaAsignadaPorAgente[agentId] = mejor.zona;
        brain.AsignarRol(mejor.rol);

        Debug.Log($"[Búsqueda Coordinada] {agentId} -> {mejor.rol} ({mejor.zona})");
    }

    private static List<TareaBusqueda> ObtenerTareasPendientes(List<PoliceBrain> brains)
    {
        List<TareaBusqueda> tareas = new List<TareaBusqueda>();

        Transform llave = PrimerTargetLlave(brains);
        Transform salida = PrimerTargetSalida(brains);
        Transform botones = PrimerTargetBotones(brains);

        if (llave != null && !zonasDespejadas.Contains("Llave"))
        {
            int ocupados = zonaAsignadaPorAgente.Values.Count(z => z == "Llave");

            if (ocupados < 1)
                tareas.Add(new TareaBusqueda("VigilarLlave", "Llave", llave));
        }

        if (salida != null && !zonasDespejadas.Contains("Salida"))
        {
            int ocupados = zonaAsignadaPorAgente.Values.Count(z => z == "Salida");

            // Dos agentes pueden ir inicialmente a la salida.
            if (ocupados < 2)
                tareas.Add(new TareaBusqueda("VigilarSalida", "Salida", salida));
        }

        if (botones != null && !zonasDespejadas.Contains("Botones"))
        {
            int ocupados = zonaAsignadaPorAgente.Values.Count(z => z == "Botones");

            if (ocupados < 1)
                tareas.Add(new TareaBusqueda("VigilarBotones", "Botones", botones));
        }

        return tareas;
    }

    private static bool TodasLasZonasCriticasDespejadas()
    {
        return zonasDespejadas.Contains("Llave") &&
               zonasDespejadas.Contains("Salida") &&
               zonasDespejadas.Contains("Botones");
    }

    private static bool EsRolBusquedaCoordinada(string rol)
    {
        return rol == "VigilarLlave" ||
               rol == "VigilarSalida" ||
               rol == "VigilarBotones" ||
               rol == "Investigar";
    }

    private static List<PoliceBrain> GetBrainsRegistrados()
    {
        return agents.Values
            .Where(a => a != null)
            .Select(a => a.GetComponent<PoliceBrain>())
            .Where(b => b != null)
            .ToList();
    }

    private static string ObtenerAgentId(PoliceBrain brain)
    {
        AgentCommunicator communicator = brain.GetComponent<AgentCommunicator>();

        if (communicator != null && !string.IsNullOrEmpty(communicator.agentId))
            return communicator.agentId;

        return brain.gameObject.name;
    }

    private static Transform PrimerTargetLlave(List<PoliceBrain> brains)
    {
        foreach (PoliceBrain brain in brains)
        {
            if (brain.checkKey != null && brain.checkKey.zonaLlaveCentro != null)
                return brain.checkKey.zonaLlaveCentro;
        }

        return null;
    }

    private static Transform PrimerTargetSalida(List<PoliceBrain> brains)
    {
        foreach (PoliceBrain brain in brains)
        {
            if (brain.guardExit != null && brain.guardExit.salida != null)
                return brain.guardExit.salida;
        }

        return null;
    }

    private static Transform PrimerTargetBotones(List<PoliceBrain> brains)
    {
        foreach (PoliceBrain brain in brains)
        {
            if (brain.guardButtons != null && brain.guardButtons.zonaBotonesCentro != null)
                return brain.guardButtons.zonaBotonesCentro;
        }

        return null;
    }

    private class TareaBusqueda
    {
        public string rol;
        public string zona;
        public Transform target;

        public TareaBusqueda(string rol, string zona, Transform target)
        {
            this.rol = rol;
            this.zona = zona;
            this.target = target;
        }
    }
}