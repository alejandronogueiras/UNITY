using UnityEngine;

public class SensorVisualizer : MonoBehaviour
{
    [Header("Sensores")]
    public VisionSensor vision;
    public HearingSensor hearing;
    public PoliceBrain brain; 

    [Header("Ajustes visuales")]
    public float yOffset = 0.2f;
    public float lineWidth = 0.08f;

    [Header("Colores")]
    public Color colorTranquilo = Color.red;
    public Color colorSospecha = new Color(1f, 0.5f, 0f); 
    public Color colorAlerta = Color.green;

    public Color colorAudicionCerca = Color.cyan;
    public Color colorAudicionLejos = Color.blue;

    private LineRenderer lrVision;
    private LineRenderer lrAudCerca;
    private LineRenderer lrAudLejos;

    void Start()
    {
        lrVision = CrearLinea("VisionCone", colorTranquilo, loop: false);
        lrAudCerca = CrearLinea("HearingClose", colorAudicionCerca, loop: true);
        lrAudLejos = CrearLinea("HearingFar", colorAudicionLejos, loop: true);
    }

    void Update()
    {
        if (vision != null)
        {
            DibujarConoVision(lrVision, vision.viewRadius, vision.viewAngle);
        }

        if (hearing != null)
        {
            DibujarCirculo(lrAudCerca, hearing.rangoCerca);
            DibujarCirculo(lrAudLejos, hearing.rangoLejos);
        }

        ActualizarColores();
    }

    void ActualizarColores()
    {
        Color cVision = colorTranquilo;

        // ADAPTADO AL BDI: Leemos el "Deseo" de perseguir en lugar del estado
        if (brain != null && brain.intencionActual == PoliceBrain.Deseo.PerseguirLadron)
            cVision = colorAlerta;
        else if (vision != null && (vision.PlayerInCone || vision.CanSeePlayer))
            cVision = colorSospecha;

        if (vision != null && vision.CanSeePlayer)
            cVision = colorAlerta; 

        SetColor(lrVision, cVision);

        Color cCerca = colorAudicionCerca;
        Color cLejos = colorAudicionLejos;

        if (hearing != null)
        {
            if (hearing.EscuchaCerca) cCerca = colorSospecha;
            if (hearing.EscuchaLejos) cLejos = colorSospecha;
        }

        SetColor(lrAudCerca, cCerca);
        SetColor(lrAudLejos, cLejos);
    }

    void SetColor(LineRenderer lr, Color c)
    {
        if (lr == null) return;
        lr.startColor = c;
        lr.endColor = c;
    }

    LineRenderer CrearLinea(string nombre, Color color, bool loop)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.useWorldSpace = true;
        lr.loop = loop;
        lr.startColor = color;
        lr.endColor = color;
        return lr;
    }

    void DibujarCirculo(LineRenderer lr, float radio)
    {
        if (lr == null) return;

        int segmentos = 40;
        lr.positionCount = segmentos;

        Vector3 center = transform.position + Vector3.up * yOffset;

        for (int i = 0; i < segmentos; i++)
        {
            float ang = (i / (float)segmentos) * 360f;
            Vector3 dir = Quaternion.Euler(0, ang, 0) * Vector3.forward;
            lr.SetPosition(i, center + dir * radio);
        }
    }

    void DibujarConoVision(LineRenderer lr, float radio, float angulo)
    {
        if (lr == null) return;

        int segmentos = 20;
        lr.positionCount = segmentos + 2;

        Vector3 origin = transform.position + Vector3.up * yOffset;
        lr.SetPosition(0, origin);

        float angInicial = -angulo / 2f;
        float paso = angulo / segmentos;

        for (int i = 0; i <= segmentos; i++)
        {
            float angActual = angInicial + (paso * i);
            Vector3 dir = Quaternion.Euler(0, angActual, 0) * transform.forward;
            lr.SetPosition(i + 1, origin + dir * radio);
        }
    }
}