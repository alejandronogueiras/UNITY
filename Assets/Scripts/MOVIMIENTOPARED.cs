using UnityEngine;

public class WallOpener : MonoBehaviour
{
    [Header("Movimiento")]
    public Vector3 offset = new Vector3(0f, 0f, 4f); 
    public float speed = 2f;

    [Header("Opcional")]
    public bool desactivarColliderAlAbrir = true;
    public Collider colliderPared; 

    private Vector3 startPos;
    private Vector3 targetPos;
    private bool opening = false;

    void Start()
    {
        startPos = transform.position;
        targetPos = startPos + offset;

        if (colliderPared == null) colliderPared = GetComponent<Collider>();
    }

    void Update()
    {
        
        if (GameManager.instance != null && GameManager.instance.juegoTerminado) return;

        
        if (!opening && GameManager.instance != null && GameManager.instance.tieneLlave)
        {
            opening = true;

            if (desactivarColliderAlAbrir && colliderPared != null)
                colliderPared.enabled = false;
        }

       
        if (opening)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                speed * Time.deltaTime
            );
        }
    }
}