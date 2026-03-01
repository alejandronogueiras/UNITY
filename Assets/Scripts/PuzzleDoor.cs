using UnityEngine;

public class PuzzleDoor : MonoBehaviour
{
    public Vector3 offsetApertura = new Vector3(0, 3f, 0); 
    public float speed = 2f;

    public bool yaAbierta = false;

    Vector3 closedPos;
    Vector3 openPos;
    bool isOpen = false;

    void Start()
    {
        closedPos = transform.position;
        openPos = closedPos + offsetApertura;
    }

    void Update()
    {
        Vector3 target = isOpen ? openPos : closedPos;

        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            speed * Time.deltaTime
        );
    }

    public void Abrir()
    {
        Debug.Log("Intentando abrir puerta");
        if (yaAbierta) return;

        yaAbierta = true;
        isOpen = true;
        Debug.Log("PUERTA ABIERTA");
    }
}