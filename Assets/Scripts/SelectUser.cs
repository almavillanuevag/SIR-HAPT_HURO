using Firebase.Extensions;
using Firebase.Firestore;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public class SelectUser : MonoBehaviour
{
    [Header("Elementos de UI (reasignados por SelectMenuUI)")]
    public TMP_Dropdown usersDropdown;
    public TextMeshProUGUI summaryText;
    public Button startSessionButton;

    [Header("Variables públicas para lectura")]
    public static SelectUser Instance;
    public string IDux;
    public string IDSession;
    public int CurrentRepetition;
    public string experimentalGroup;
    public List<string> trajectoryOrder = new List<string>();
    public int totalReps;
    public string ActiveTrajectory;
    public bool _initialized = false;

    [Header("Para Log en texto (opcional)")]
    public TextMeshProUGUI debugText;

    // Variables internas
    FirebaseFirestore db;
    readonly List<string> iduxList = new List<string>();
    List<string> trajectoriesIncluded = new List<string>();
    int repsPerTrajectory;
    

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        _initialized = true;
    }

    public void LoadUsers()
    {
        if (usersDropdown == null) { Log("usersDropdown no asignado."); return; }

        usersDropdown.onValueChanged.RemoveAllListeners();
        usersDropdown.ClearOptions();
        iduxList.Clear();

        db.Collection("UnityConfig").GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Log("Error cargando UnityConfig: " + task.Exception);
                return;
            }

            QuerySnapshot snapshot = task.Result;
            List<string> options = new List<string>();

            usersDropdown.ClearOptions();
            iduxList.Clear();

            List<Task> filterTasks = new List<Task>();
            List<string> allIds = new List<string>();
            foreach (DocumentSnapshot doc in snapshot.Documents)
                allIds.Add(doc.Id);

            int pending = allIds.Count;
            if (pending == 0)
            {
                // sin documentos
                usersDropdown.AddOptions(new List<string> { "No hay IDs disponibles" });
                return;
            }

            foreach (string id in allIds)
            {
                db.Collection("Users").Document(id).GetSnapshotAsync().ContinueWithOnMainThread(userTask =>
                {
                    if (!userTask.IsFaulted && !userTask.IsCanceled && userTask.Result.Exists)
                    {
                        string status = userTask.Result.ContainsField("status")
                            ? userTask.Result.GetValue<string>("status")
                            : "pending";

                        if (status != "completed")
                        {
                            options.Add(id);
                            iduxList.Add(id);
                        }
                    }
                    pending--;
                    if (pending == 0)   // cuando terminaron TODAS las lecturas
                        UpdateDropdown(options);
                });
            }
        });
    }
    void UpdateDropdown(List<string> options)
    {
        if (options.Count > 0)
        {
            usersDropdown.AddOptions(options);
            usersDropdown.onValueChanged.AddListener(OnUserSelected);
            usersDropdown.value = 0;

            int idxToSelect = 0;
            usersDropdown.value = -1;
            usersDropdown.value = idxToSelect;
            usersDropdown.RefreshShownValue();
        }
        else
        {
            Log("No se encontraron usuarios disponibles.");
            usersDropdown.AddOptions(new List<string> { "No hay IDs disponibles" });
            ClearSessionData();
            RefreshSummary();
        }
    }

    public void OnUserSelected(int index)
    {
        if (index < 0 || index >= iduxList.Count) return;

        // El IDux es el Document ID correspondiente al índice seleccionado
        IDux = iduxList[index];

        if (summaryText != null) summaryText.text = "Cargando...";

        db.Collection("UnityConfig").Document(IDux).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Log($"Error leyendo UnityConfig/{IDux}: " + task.Exception);
                return;
            }

            DocumentSnapshot doc = task.Result;
            if (!doc.Exists)
            {
                Log($"Documento UnityConfig/{IDux} no existe.");
                return;
            }

            // Leer campos
            experimentalGroup = doc.ContainsField("experimentalGroup") ? doc.GetValue<string>("experimentalGroup") : "N/A";
            CurrentRepetition = doc.ContainsField("CurrentRepetition") ? doc.GetValue<int>("CurrentRepetition") : 1;
            repsPerTrajectory = doc.ContainsField("repsPerTrajectory") ? doc.GetValue<int>("repsPerTrajectory") : 0;

            trajectoryOrder.Clear();
            if (doc.ContainsField("trajectoryOrder"))
                trajectoryOrder = new List<string>(doc.GetValue<List<string>>("trajectoryOrder"));

            trajectoriesIncluded.Clear();
            if (doc.ContainsField("trajectoriesIncluded"))
                trajectoriesIncluded = new List<string>(doc.GetValue<List<string>>("trajectoriesIncluded"));

            totalReps = trajectoriesIncluded.Count * repsPerTrajectory;

            // Generar IDSession
            IDSession = $"SessionNum{CurrentRepetition:D3}-{IDux}";
            // Almacenar la ActiveTrajectory
            ActiveTrajectory = trajectoryOrder[CurrentRepetition-1];

            RefreshSummary();
        });
    }

    private void RefreshSummary()
    {
        if (summaryText == null) return;

        if (string.IsNullOrEmpty(IDux)) 
        {
            // Si no está seleccionado un IDux mostar el texto y desactivar el botón
            summaryText.text = "Selecciona un usuario para comenzar";
            startSessionButton.interactable = false;
            return;
        }
        // Si si esta seleccionado el IDux entonces activar el botón y mostrar un resumen de la conf
        startSessionButton.interactable = true;
        string trajs = trajectoriesIncluded.Count > 0 ? string.Join(", ", trajectoriesIncluded) : "—";

        summaryText.text =
            $"Grupo: {experimentalGroup}\n" +
            $"Trayectorias: {trajs}\n" +
            $"{repsPerTrajectory} repeticiones por trayectoria";
    }

    private void ClearSessionData()
    {
        experimentalGroup = "";
        trajectoryOrder.Clear();
        trajectoriesIncluded.Clear();
        CurrentRepetition = 1;
        totalReps = 0;
        repsPerTrajectory = 0;
    }
    private void Log(string msg)
    {
        Debug.Log($"[SelectUser] {msg}");
        if (debugText != null) debugText.text += "\n" + msg;
    }
}