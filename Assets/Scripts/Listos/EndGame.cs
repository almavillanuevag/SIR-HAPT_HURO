using Firebase.Extensions;
using Firebase.Firestore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class EndGame : MonoBehaviour
{
    [Header("Asignar elementos para interacciones")]
    public Transform PlanetEndPoint; // Punto final donde se situará la nave
    public TrajectoryManager TrajectoryManager; // Scrpt de la trayectoria para calcular 
    public FollowHand followHand;    

    [Header("Asignar elementos de UI")]
    public GameObject UICanvasWin;
    public GameObject UICanvasLoadingNext;
    public GameObject star1;
    public GameObject star2;
    public GameObject star3;
    public GameObject star4;
    public GameObject star5;

    [Header("Log opcional para pruebas y errores")]
    public TextMeshProUGUI debugText; // Para Display Debugs

    [Header("Objetos publicos para lectura")]
    public float stars0;
    public float radio0 = 0f;
    public float TotalErrors0 = 0;
    public bool OutOfTube = false;
    public bool End = false;
    public int CurrentRepetition;
    public ShipMovement shipMovement;
    public float[] metrics;
    public bool HapticFeedback;

    // Tiempos
    public float TimeOut = 0f;
    public float OutBegin = 0;
    public float TotalTime0 = 0f;
    public float BeginningTime = 0f; // time cuando comenzó el juego
    public float InsideTimePercentage0 = 0f;

    // Variables internas
    GameObject ShipGameObject;
    float distance;
    float FinishTime;
    FirebaseFirestore db;
    string IDux;
    string IDSession;
    AudioSource audioSource;



    private void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        //Activar canvas de victoria
        UICanvasWin.SetActive(false);
        //UICanvasLoadingNext.SetActive(false);

    }

    private void Update()
    {
        // Calcular time fuera del tubo constantemente para métricas 
        distance = TrajectoryManager.distance;
        radio0 = TrajectoryManager.radio;

        // Verificar si la nave está dentro o fuera
        if (distance <= radio0)
        {
            if (OutOfTube) // Si estaba fuera y ahora adentro
            {
                // Sumar el time que estuvo fuera
                TimeOut += Time.time - OutBegin;
                OutOfTube = false;
            }
        }
        else
        {
            if (!OutOfTube) // Si estaba dentro y ahora está fuera
            {
                TotalErrors0++;
                OutBegin = Time.time; // Marcar cuando salió
                OutOfTube = true;
            }
        }

    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ship"))
        {
            // Posicionar la nave en el punto final y regresarle propiedades fisicas
            shipMovement = other.GetComponentInParent<ShipMovement>();
            shipMovement.ForceRelease();
            SetShipToFinalPosition(other);

            // Eliminar las funciones de retroalimentacion haptica 
            if (HapticFeedback) followHand.StopHapticFeedbackFunctions();

            // Calcular las metricas de desempeño solo una vez si ya termino
            if (End) return;
            End = true;  // Flag de que ya termino y que no se vuelvan a calcular

            // Obtener el tiempo cuando se colocó la nave en la mano (inicio del juego)
            BeginningTime = shipMovement.BeginningTime;
            // Obtener tiempo en el que llegó al planeta fin
            FinishTime = Time.time;

            // Calcular las metricas
            shipMovement.StopRecordingTrajectory();
            metrics = CalculatePerformanceMetrics();

            // Comenzar la secuencia de finalizar
            StartCoroutine(WinSequence(metrics));
        }
    }

    IEnumerator WinSequence(float[] m)
    {
        Log(" WinSequence comienza");
        // Guardar sesión (async, esperamos su finalización)
        bool savedDone = false;
        SaveSessionToFirestore(m, () => savedDone = true);
        yield return new WaitUntil(() => savedDone);

        // Mostrar UI de victoria con estrellas
        UICanvasWin.SetActive(true);
        DisplayStars();

        // Tiempo proporcional a estrellas: stars*2 + 1
        int stars = (int)m[3];
        float wait = stars * 1.1f + 2f;
        yield return new WaitForSeconds(wait);

        // Ejecutar NewSession para la lógica de progresión ----------------------------------*
        Log("Ejecutar NewSession para la lógica de progresión");
        NewSession newSession = gameObject.GetComponent<NewSession>();
        newSession.PlayAgain();
        Log("Fin PlayAgain en EndGame");
    }
    void SetShipToFinalPosition(Collider other)
    {
        ShipGameObject = other.gameObject;

        // Desparentar la nave
        ShipGameObject.transform.SetParent(null);

        // Posicionarlo arriba del planeta
        ShipGameObject.transform.position = PlanetEndPoint.position;
        ShipGameObject.transform.rotation = PlanetEndPoint.rotation;

        // Dejarlo fijo en el planeta: quitarle los colliders
        ShipGameObject.GetComponent<BoxCollider>().enabled = false;
        ShipGameObject.GetComponent<CapsuleCollider>().enabled = false;

        // Regresarle propiedades fisicas
        Rigidbody rb = ShipGameObject.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero; // Detener cualquier velocidad residual
        rb.angularVelocity = Vector3.zero;
    }

    float[] CalculatePerformanceMetrics()
    {
        if (OutOfTube) // Cuando que cuando vuelve a jugar se calcule el tiempo de esa partida
        {
            TimeOut += Time.time - OutBegin;
        }
        TotalTime0 = FinishTime - BeginningTime; // Tiempo total desde el inicio
        InsideTimePercentage0 = TotalTime0 > 0 ? ((TotalTime0 - TimeOut) / TotalTime0) * 100 : 0;

        stars0 = 1; // Calcular estrellas
        if (InsideTimePercentage0 >= 30)
        {
            stars0++;
            if (TotalErrors0 <= 5)
            {
                stars0++;
                if (InsideTimePercentage0 >= 30 + 20)
                {
                    stars0++;
                    if (TotalErrors0 <= 3)
                    {
                        stars0++;
                    }
                }
            }
        }
        // Crear arreglo de métricas
        float[] metrics = new float[4];
        metrics[0] = TotalErrors0;
        metrics[1] = TotalTime0;
        metrics[2] = InsideTimePercentage0;
        metrics[3] = stars0;

        return metrics;
    }

    async void SaveSessionToFirestore(float[] metrics, Action onDone)
    {
        // Validar que tenga acceso a la instancia de SelectUser
        if (SelectUser.Instance == null)
        {
            Log("No hay Instancia de SelectUser.cs");
            onDone?.Invoke();
            return;
        }
        
        // Acceder a la instancia para obtener el ID del paciente y sesion:
        IDux = SelectUser.Instance.IDux;
        IDSession = SelectUser.Instance.IDSession;
        CurrentRepetition = SelectUser.Instance.CurrentRepetition;

        // Buscar errores
        if (db == null || string.IsNullOrEmpty(IDux))
        {
            Log("ERROR: Firestore no inicializado");
            onDone?.Invoke();
            return;
        }

        // Configurar la informacion antes de guardarla ----------------------------------------------------------------------------*
        var idTraj = SelectUser.Instance.ActiveTrajectory;
        List<Vector3> patientTrajectory = null;
        patientTrajectory = shipMovement.patientTrajectory;

        // Organizar metricas como diccionario para Firestore
        var data = new Dictionary<string, object>
        {
            { "SessionIndex", CurrentRepetition },
            { "TrajectoryID", idTraj },
            { "TotalErrors", (int)metrics[0] },
            { "TotalTime", Mathf.Round(metrics[1] * 1000f) / 1000f },
            { "InsideTimePercentage", Mathf.Round(metrics[2] * 1000f) / 1000f },
            { "Stars", (int)metrics[3] }
        };

        // Almacenar la trayectoria como diccionario
        if (patientTrajectory != null && patientTrajectory.Count > 0)
        {
            var pointsList = new List<Dictionary<string, object>>(patientTrajectory.Count);
            foreach (var p in patientTrajectory)
            {
                pointsList.Add(new Dictionary<string, object>
                {
                    { "x", p.x },
                    { "y", p.y },
                    { "z", p.z }
                });
            }

            data.Add("trajectoryPoints", pointsList);
            data.Add("pointsCount", patientTrajectory.Count);
        }

        // Referenciar documento en firestore y almacenar los datos
        var sessionRef = db.Collection("Users").Document(IDux).Collection("Sesiones").Document(IDSession);

        await sessionRef.SetAsync(data);

        // Actualizar contador de sesiones completadas
        DocumentReference userRef = db.Collection("UnityConfig").Document(IDux);
        int rep = CurrentRepetition + 1;
        await userRef.UpdateAsync("CurrentRepetition", rep);

        // Actualizar el status en firestore
        int totalReps = SelectUser.Instance.totalReps;
        if(CurrentRepetition >= totalReps)
        {
            DocumentReference u = db.Collection("Users").Document(IDux);
            await u.UpdateAsync("status", "completed");
        }

        // También actualizar el valor de la instancia en SelectUser
        SelectUser.Instance.CurrentRepetition = rep;

        Log("GuardadoenFirestore");
        onDone?.Invoke();
    }

    void DisplayStars()
    {
        // Desactivar todas las estrellas por si acaso
        star1.SetActive(false);
        star2.SetActive(false);
        star3.SetActive(false);
        star4.SetActive(false);
        star5.SetActive(false);

        int stars = (int)metrics[3];
        if (stars >= 1) star1.SetActive(true);
        if (stars >= 2) star2.SetActive(true);
        if (stars >= 3) star3.SetActive(true);
        if (stars >= 4) star4.SetActive(true);
        if (stars >= 5) star5.SetActive(true);

    }
    void Log(string msg)
    {
        Debug.Log(msg);
        if (debugText != null) debugText.text += "\n" + msg;
    }

}