using UnityEngine;

public class VisionSensor : MonoBehaviour
{
    [Header("Referencia")]
    public Transform jugador;

    [Header("Parámetros")]
    public float viewRadius = 12f;
    [Range(0, 360)] public float viewAngle = 90f;

    [Header("Capas (paredes/obstáculos)")]
    public LayerMask obstacleMask;

    // Outputs
    public bool CanSeePlayer { get; private set; }
    public bool PlayerInCone { get; private set; }      
    public Vector3 LastSeenPosition { get; private set; }
    public float DistanceToPlayer { get; private set; }

    void Update()
    {
        UpdateVision();
    }

    void UpdateVision()
    {
        CanSeePlayer = false;
        PlayerInCone = false;
        DistanceToPlayer = Mathf.Infinity;

        if (jugador == null) return;

        Vector3 toPlayer = jugador.position - transform.position;
        float distance = toPlayer.magnitude;
        DistanceToPlayer = distance;

        if (distance > viewRadius) return;

        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > viewAngle * 0.5f) return;

        PlayerInCone = true;

        Vector3 origin = transform.position + Vector3.up * 1.6f;
        Vector3 target = jugador.position + Vector3.up * 1.0f;
        Vector3 dir = (target - origin).normalized;

        RaycastHit hit;

        if (Physics.Raycast(origin, dir, out hit, viewRadius, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform != jugador)
                return;
        }

        CanSeePlayer = true;
        LastSeenPosition = jugador.position;
    }
}