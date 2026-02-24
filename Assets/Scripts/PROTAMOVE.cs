using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerLookMove : MonoBehaviour
{
    [Header("Movement")]
    public float runSpeed = 7f;
    public float rotationSpeed = 250f;
    public float gravity = -9.81f;

    [Header("Animator")]
    public Animator animator;
    public string speedParam = "Speed"; // nombre del parámetro en tu Animator

    [Header("Look")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2f;
    public float maxLookUp = 80f;

    [Header("Sigilo")]
    public float radioRuido = 4f;
    public bool estaHaciendoRuido = false;

    private CharacterController cc;
    private float pitch;
    private float verticalVelocity;

    // Línea visual del ruido
    private LineRenderer lineaRuido;

    void Awake()
    {
        cc = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Start()
    {
        lineaRuido = CrearLineaVisual(Color.yellow);
    }

    void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        var kb = Keyboard.current;

        // --- INPUT (New Input System) ---
        float x = 0f; // rotación (A/D)
        float y = 0f; // avanzar (W/S)

        if (kb != null)
        {
            if (kb.aKey.isPressed) x -= 1f;
            if (kb.dKey.isPressed) x += 1f;

            if (kb.wKey.isPressed) y += 1f;
            if (kb.sKey.isPressed) y -= 1f;
        }

        // --- ROTACIÓN (A/D) ---
        transform.Rotate(0f, x * rotationSpeed * Time.deltaTime, 0f);

        // --- MOVIMIENTO (W/S) ---
        Vector3 move = (transform.forward * y + transform.right * x) * runSpeed;

        // --- SISTEMA DE RUIDO ---
        estaHaciendoRuido = Mathf.Abs(y) > 0.1f;
        if (lineaRuido != null)
        {
            lineaRuido.enabled = estaHaciendoRuido;
            if (estaHaciendoRuido) DibujarCirculo(lineaRuido, radioRuido);
        }

        // --- GRAVEDAD ---
        if (cc.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -1f; // pegado al suelo (ajusta entre -0.5 y -2 si hace falta)

        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;

        cc.Move(move * Time.deltaTime);

        // --- ANIMATOR (Idle/Walk) ---
        if (animator != null)
        {
            // 0 en idle, 1 andando (o usa Mathf.Abs(y) si prefieres valor continuo)
            animator.SetFloat("VelX", x);
            animator.SetFloat("VelY", y);
        }

        // --- MIRAR CON RATÓN ---
        if (Mouse.current != null && cameraTransform != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();

            // yaw -> personaje
            float yaw = delta.x * mouseSensitivity * Time.deltaTime * 60f;
            transform.Rotate(Vector3.up * yaw);

            // pitch -> cámara
            float look = delta.y * mouseSensitivity * Time.deltaTime * 60f;
            pitch -= look;
            pitch = Mathf.Clamp(pitch, -maxLookUp, maxLookUp);
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        // ESC libera ratón
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // ---------- LINE RENDERER ----------

    LineRenderer CrearLineaVisual(Color color)
    {
        LineRenderer lr = gameObject.AddComponent<LineRenderer>();
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
        lr.useWorldSpace = true;
        lr.loop = true;
        return lr;
    }

    void DibujarCirculo(LineRenderer lr, float radio)
    {
        int segmentos = 40;
        lr.positionCount = segmentos;

        for (int i = 0; i < segmentos; i++)
        {
            float angulo = (i / (float)segmentos) * 360f;
            Vector3 direccion = Quaternion.Euler(0f, angulo, 0f) * Vector3.forward;
            lr.SetPosition(i, transform.position + Vector3.up * 0.2f + direccion * radio);
        }
    }
}