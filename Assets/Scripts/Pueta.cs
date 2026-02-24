using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] Transform doorPivot;   // la parte que gira (puerta)
    [SerializeField] float openAngle = 90f;
    [SerializeField] float speed = 6f;

    bool isOpen = false;
    Quaternion closedRot;
    Quaternion openRot;

    void Awake()
    {
        if (doorPivot == null) doorPivot = transform;

        closedRot = doorPivot.localRotation;
        openRot = Quaternion.Euler(0, openAngle, 0) * closedRot;
    }

    public void Interact()
    {
        isOpen = !isOpen;
    }

    void Update()
    {
        var target = isOpen ? openRot : closedRot;
        doorPivot.localRotation = Quaternion.Slerp(doorPivot.localRotation, target, Time.deltaTime * speed);
    }
}
