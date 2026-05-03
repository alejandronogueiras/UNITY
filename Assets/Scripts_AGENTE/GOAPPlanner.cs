using System.Collections.Generic;
using System.Linq;

// Estructura que define una acción posible para el agente
public class GOAPAction
{
    public string Nombre;
    public float Costo;
    public Dictionary<string, bool> Precondiciones = new Dictionary<string, bool>();
    public Dictionary<string, bool> Efectos = new Dictionary<string, bool>();

    public GOAPAction(string nombre, float costo)
    {
        Nombre = nombre;
        Costo = costo;
    }
}

// Nodo para el algoritmo A*
public class GOAPNode
{
    public GOAPNode Padre;
    public GOAPAction Accion;
    public Dictionary<string, bool> Estado;
    public float CostoG; // Costo acumulado real
    public float CostoH; // Costo heurístico estimado
    public float CostoF => CostoG + CostoH;
}

public static class GOAPPlanner
{
    public static Queue<GOAPAction> Planificar(
        Dictionary<string, bool> estadoInicial, 
        Dictionary<string, bool> objetivo, 
        List<GOAPAction> accionesDisponibles)
    {
        List<GOAPNode> openList = new List<GOAPNode>();
        List<GOAPNode> closedList = new List<GOAPNode>();

        GOAPNode startNode = new GOAPNode { 
            Estado = new Dictionary<string, bool>(estadoInicial), 
            CostoG = 0, 
            CostoH = CalcularHeuristica(estadoInicial, objetivo) 
        };
        openList.Add(startNode);

        while (openList.Count > 0)
        {
            // Ordenar por el menor costo F (A*)
            openList.Sort((a, b) => a.CostoF.CompareTo(b.CostoF));
            GOAPNode actual = openList[0];

            // Si el estado actual cumple el objetivo, reconstruimos el plan
            if (CumpleObjetivo(actual.Estado, objetivo))
            {
                List<GOAPAction> planLista = new List<GOAPAction>();
                while (actual.Accion != null)
                {
                    planLista.Add(actual.Accion);
                    actual = actual.Padre;
                }
                planLista.Reverse(); // Invertimos porque trazamos desde la meta hasta el inicio
                return new Queue<GOAPAction>(planLista);
            }

            openList.Remove(actual);
            closedList.Add(actual);

            // Probar cada acción disponible
            foreach (GOAPAction accion in accionesDisponibles)
            {
                if (!CumplePrecondiciones(actual.Estado, accion.Precondiciones)) continue;

                Dictionary<string, bool> nuevoEstado = AplicarEfectos(actual.Estado, accion.Efectos);
                
                GOAPNode vecino = new GOAPNode
                {
                    Padre = actual,
                    Accion = accion,
                    Estado = nuevoEstado,
                    CostoG = actual.CostoG + accion.Costo,
                    CostoH = CalcularHeuristica(nuevoEstado, objetivo)
                };

                // Si ya evaluamos este estado de forma más eficiente, lo saltamos
                if (closedList.Any(n => DiccionariosIguales(n.Estado, vecino.Estado))) continue;

                // Evitamos añadir estados repetidos a openList si ya existe uno igual con menor o igual coste.
                if (openList.Any(n => DiccionariosIguales(n.Estado, vecino.Estado) && n.CostoF <= vecino.CostoF))
                    continue;
                    
                openList.Add(vecino);
            }
        }
        
        return null; // Imposible alcanzar el objetivo
    }

    private static bool CumpleObjetivo(Dictionary<string, bool> estado, Dictionary<string, bool> objetivo)
    {
        foreach (var kvp in objetivo)
            if (!estado.ContainsKey(kvp.Key) || estado[kvp.Key] != kvp.Value) return false;
        return true;
    }

    private static bool CumplePrecondiciones(Dictionary<string, bool> estado, Dictionary<string, bool> precondiciones)
    {
        foreach (var kvp in precondiciones)
            if (!estado.ContainsKey(kvp.Key) || estado[kvp.Key] != kvp.Value) return false;
        return true;
    }

    private static Dictionary<string, bool> AplicarEfectos(Dictionary<string, bool> actual, Dictionary<string, bool> efectos)
    {
        var nuevo = new Dictionary<string, bool>(actual);
        foreach (var kvp in efectos) nuevo[kvp.Key] = kvp.Value;
        return nuevo;
    }

    private static float CalcularHeuristica(Dictionary<string, bool> estado, Dictionary<string, bool> objetivo)
    {
        int faltan = 0;
        foreach (var kvp in objetivo)
            if (!estado.ContainsKey(kvp.Key) || estado[kvp.Key] != kvp.Value) faltan++;
        return faltan; // Heurística simple: número de condiciones que faltan por cumplir
    }

    private static bool DiccionariosIguales(Dictionary<string, bool> d1, Dictionary<string, bool> d2)
    {
        if (d1.Count != d2.Count) return false;
        foreach (var kvp in d1)
            if (!d2.ContainsKey(kvp.Key) || d2[kvp.Key] != kvp.Value) return false;
        return true;
    }
}