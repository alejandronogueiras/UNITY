using UnityEngine;
using UnityEngine.InputSystem;

public class ProtaInteract : MonoBehaviour
{
    [SerializeField] Camera cam;
    [SerializeField] float maxDistance = 3f;
    [SerializeField] LayerMask interactMask;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            TryInteract();
    }

    void TryInteract()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactMask))
        {
            Door door = hit.collider.GetComponentInParent<Door>();
            if (door != null) door.Interact();
        }
    }
}