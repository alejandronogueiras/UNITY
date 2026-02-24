using UnityEngine;

public class DoorInteraction : MonoBehaviour
{
    public bool playerNear = false;
    public Transform door; 
    public float openAngle = 90f;
    private bool isOpen = false;

    void Update()
    {
        if (playerNear && Input.GetKeyDown(KeyCode.E))
        {
            ToggleDoor();
        }
    }

    void ToggleDoor()
    {
        isOpen = !isOpen;

        if(isOpen)
            door.localRotation = Quaternion.Euler(0, openAngle, 0);
        else
            door.localRotation = Quaternion.Euler(0, 0, 0);
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
            playerNear = true;
    }

    void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Player"))
            playerNear = false;
    }
}
