using UnityEngine;

public class MetalFloorZone : MonoBehaviour
{
    [Tooltip("Cuánto multiplica el radio de ruido cuando pisas metal.")]
    public float multiplicadorRuido = 2f;

    private void OnTriggerEnter(Collider other)
    {
        var p = other.GetComponentInParent<PlayerLookMove>();
        if (p == null) return;

        p.SetMetalFloor(true, multiplicadorRuido);
    }

    private void OnTriggerExit(Collider other)
    {
        var p = other.GetComponentInParent<PlayerLookMove>();
        if (p == null) return;

        p.SetMetalFloor(false, 1f);
    }
}