using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

public class DoorController : MonoBehaviour
{
    [Header("Referencia a la puerta (malla que rota)")]
    public Transform doorMesh;

    [Header("Ajustes")]
    public float openSpeed = 2f;
    public float openAngle = 90f;

    [Header("IA")]
    public float aiOpenRadius = 2.0f;     // distancia a la que la IA abre
    public LayerMask aiLayers = ~0;       // por defecto, todas las layers

    private bool playerNear = false;
    private bool isOpen = false;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    private NavMeshObstacle obstacle;

    void Start()
    {
        if (doorMesh == null)
        {
            Debug.LogError("No has asignado doorMesh en el inspector.");
            enabled = false;
            return;
        }

        closedRotation = doorMesh.rotation;
        openRotation = Quaternion.Euler(0, openAngle, 0) * closedRotation;

        // ✅ Coge el obstacle aunque esté en el padre
        obstacle = GetComponentInParent<NavMeshObstacle>();
        if (obstacle == null) obstacle = GetComponentInChildren<NavMeshObstacle>();

        if (obstacle == null)
            Debug.LogWarning("No hay NavMeshObstacle en la puerta/parents. La IA puede bloquearse.");
    }

    void Update()
    {
        // --- JUGADOR ABRE CON E ---
        if (playerNear && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            ToggleDoor();

        // --- IA ABRE AUTOMÁTICO SI ESTÁ CERCA ---
        Vector3 centro = doorMesh.position; // ✅ mejor que transform.position si el script está en otro objeto

        Collider[] hits = Physics.OverlapSphere(
            centro,
            aiOpenRadius,
            aiLayers,
            QueryTriggerInteraction.Collide // ✅ incluye triggers si el collider del agente es trigger
        );

        bool agentCerca = false;
        foreach (var h in hits)
        {
            // ✅ MUY IMPORTANTE: el agent suele estar en el padre del collider
            if (h.GetComponentInParent<NavMeshAgent>() != null)
            {
                agentCerca = true;
                break;
            }
        }

        if (agentCerca) isOpen = true;

        // --- ROTACIÓN SUAVE ---
        Quaternion targetRot = isOpen ? openRotation : closedRotation;
        doorMesh.rotation = Quaternion.Lerp(doorMesh.rotation, targetRot, Time.deltaTime * openSpeed);

        // --- NAVMESH ---
        if (obstacle != null)
            obstacle.enabled = !isOpen;
    }

    void ToggleDoor() => isOpen = !isOpen;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Personaje"))
            playerNear = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Personaje"))
            playerNear = false;
    }

    // Solo para ver el radio en escena
    void OnDrawGizmosSelected()
    {
        if (doorMesh == null) return;
        Gizmos.DrawWireSphere(doorMesh.position, aiOpenRadius);
    }
    
    public void ForceOpen()
    {
    isOpen = true;
    }
}