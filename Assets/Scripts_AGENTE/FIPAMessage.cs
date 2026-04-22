using System;

[Serializable]
public class FIPAMessage
{
    // Actos comunicativos básicos de FIPA
    public enum Performative
    {
        INFORM,             // Compartir un dato (ej: "Vi al ladrón")
        REQUEST,            // Pedir una acción (ej: "Ve a la salida")
        PROPOSE,            // Proponer algo (ej: "Propongo hacer una emboscada")
        ACCEPT_PROPOSAL,    // Aceptar la propuesta
        REJECT_PROPOSAL,    // Rechazar la propuesta
        CFP                 // Call For Proposal (Llamada a propuestas, útil para formar equipos)
    }

    public Performative performative;
    public string senderId;
    public string receiverId;   // Usaremos "ALL" para gritar a todos
    public string content;      // Aquí meteremos datos (ej. un Vector3 en formato JSON)
    public string conversationId;
    public long timestamp;
}