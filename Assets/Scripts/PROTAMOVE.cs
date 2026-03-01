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
    public string velXParam = "VelX";
    public string velYParam = "VelY";

    [Header("Look")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2f;
    public float maxLookUp = 80f;

    [Header("Sigilo y Ruido (Pulsos)")]
    public float radioRuidoNormal = 4f;
    public float radioRuidoSigilo = 2f;

    public bool estaEnSigilo = false;

    [HideInInspector] public float radioActual; 
    [HideInInspector] public float radioRuido;  
    public bool estaHaciendoRuido = false;

    [Header("Configuración de Pasos")]
    public float tiempoEntrePasos = 0.5f;
    public float duracionDelPulso = 0.15f;

    [Header("Suelo metálico")]
    public bool enSueloMetalico = false;
    public float multRuidoMetalico = 1f;


    public float RadioRuidoActual => radioActual * multRuidoMetalico;

    public void SetMetalFloor(bool activo, float mult)
    {
        enSueloMetalico = activo;
        multRuidoMetalico = activo ? mult : 1f;
    }

    private CharacterController cc;
    private float pitch;
    private float verticalVelocity;


    private float temporizadorPasos = 0f;
    private float temporizadorPulso = 0f;

 
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

        radioActual = radioRuidoNormal;
        radioRuido = radioActual;

        temporizadorPasos = tiempoEntrePasos;
    }

    void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
  
        if (GameManager.instance != null && GameManager.instance.juegoTerminado) return;

        var kb = Keyboard.current;

    
        float velocidadActual = runSpeed;
        radioActual = radioRuidoNormal;

       estaEnSigilo = false;

        if (kb != null && kb.leftShiftKey.isPressed)
        {
            velocidadActual = runSpeed / 2f;
            radioActual = radioRuidoSigilo;
            estaEnSigilo = true; 
        }

  
        radioRuido = radioActual;


        float x = 0f; 
        float y = 0f; 

        if (kb != null)
        {
            if (kb.aKey.isPressed) x -= 1f;
            if (kb.dKey.isPressed) x += 1f;

            if (kb.wKey.isPressed) y += 1f;
            if (kb.sKey.isPressed) y -= 1f;
        }


        transform.Rotate(0f, x * rotationSpeed * Time.deltaTime, 0f);


        Vector3 move = transform.forward * (y * velocidadActual);


        bool seEstaMoviendo = Mathf.Abs(y) > 0.1f;

        if (seEstaMoviendo)
        {
            temporizadorPasos += Time.deltaTime;

            if (temporizadorPasos >= tiempoEntrePasos)
            {
                estaHaciendoRuido = true;
                temporizadorPasos = 0f;
                temporizadorPulso = duracionDelPulso;
            }
        }
        else
        {
            temporizadorPasos = tiempoEntrePasos; 
        }

        if (estaHaciendoRuido)
        {
            temporizadorPulso -= Time.deltaTime;
            if (temporizadorPulso <= 0f) estaHaciendoRuido = false;
        }

 
        if (lineaRuido != null)
        {
            lineaRuido.enabled = estaHaciendoRuido;
            if (estaHaciendoRuido) DibujarCirculo(lineaRuido, RadioRuidoActual);
        }


        if (cc.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -1f;

        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;

        cc.Move(move * Time.deltaTime);

        if (animator != null)
        {

            animator.SetFloat(velXParam, 0f);
            animator.SetFloat(velYParam, y);
        }

        if (Mouse.current != null && cameraTransform != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();

            float yaw = delta.x * mouseSensitivity * Time.deltaTime * 60f;
            transform.Rotate(Vector3.up * yaw);

            float look = delta.y * mouseSensitivity * Time.deltaTime * 60f;
            pitch -= look;
            pitch = Mathf.Clamp(pitch, -maxLookUp, maxLookUp);
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

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