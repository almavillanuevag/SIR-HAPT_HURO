using System.Collections;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class TrajectoryManager : MonoBehaviour
{
    [Header("Asignar elementos para interacciones")]
    public Transform Ship;
    public GameObject Arrows;
    public SplineArrowFollow splineArrowFollow;
    public Material Green;
    public Material Red;
    public Material Blue;


    [Header("Log opcional para pruebas y errores")]
    public TextMeshProUGUI debugText;

    [Header("Variables públicas para lectura")]
    public int knotCount = 15;
    public SplineContainer KnotsSpline;
    public SplineExtrude splineExtrude;
    public float radio;
    public float distance = 0;
    public float erroresMax = 3f;
    public float radioMax = 0.2f;
    public float radioSUM = 0.05f;
    public float TotalError = -1;
    public AudioSource audioSource;
    public bool VisualFeedback = false;

    // variables privadas de funcionamiento interno
    //TrajectoryDataLoad loader;
    bool gameplayEnabled = false;
    bool OutOfTube = false;
    bool animationSet = false;
    TrajectoryDataLoad loader;


    private void Start()
    {
        loader = GetComponent<TrajectoryDataLoad>();
        if (loader == null)
            Debug.LogError("Falta TrajectoryDataLoad en el mismo GameObject.");
        if (KnotsSpline == null) KnotsSpline = GetComponent<SplineContainer>();
        if (splineExtrude == null) splineExtrude = GetComponent<SplineExtrude>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        StartCoroutine(WaitForSplineReady());
    }

    private void Update()
    {
        // Validaciones iniciales
        if (!gameplayEnabled) return;
        if (KnotsSpline == null || splineExtrude == null) return;
        if (Ship == null) return;

        // Activar la animación de las flechas una vez
        if (!animationSet)
        {
            if (splineArrowFollow == null) splineArrowFollow = GetComponentInChildren<SplineArrowFollow>();
            splineArrowFollow.LoadSpline(KnotsSpline);
            animationSet = true;
        }

        // Logica central del gameplay
        radio = splineExtrude.Radius;
        distance = ShipDistanceFromSpline(Ship.position, KnotsSpline);

        MeshRenderer mr = GetComponent<MeshRenderer>();

        if (distance <= radio)
        {
            if(VisualFeedback) mr.material = Green;
            else mr.material = Blue;
            if (OutOfTube) OutOfTube = false;
        }
        else
        {
            if (VisualFeedback) mr.material = Red;
            else mr.material = Blue;
            if (!OutOfTube)
            {
                TotalError++;
                OutOfTube = true;
            }
        }
    }

    float ShipDistanceFromSpline(Vector3 shipPos, SplineContainer splineContainer)
    {
        float steps = 100f;
        float minDist = Mathf.Infinity;

        for (float i = 0; i <= steps; i++)
        {
            float t = i / steps;
            float3 p = splineContainer.EvaluatePosition(t);
            Vector3 splineDot = new Vector3(p.x, p.y, p.z);

            float d = Vector3.Distance(shipPos, splineDot);
            if (d < minDist) minDist = d;
        }

        return minDist;
    }
    IEnumerator WaitForSplineReady()
    {
        // Espera a que el loader exista
        while (loader == null) yield return null;

        // Espera a que termine en Ready o Failed
        yield return new WaitUntil(() =>
            loader.State == TrajectoryDataLoad.LoadState.Ready ||
            loader.State == TrajectoryDataLoad.LoadState.Failed);

        if (loader.State == TrajectoryDataLoad.LoadState.Failed)
        {
            if (debugText != null) debugText.text += "\nNo se pudo cargar spline ";
            yield break; // no habilitar gameplay
        }

        // Ya está lista, habilitar el gameplay
        gameplayEnabled = true;
    }
}
