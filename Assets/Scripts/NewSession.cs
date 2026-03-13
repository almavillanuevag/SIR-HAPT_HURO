using Firebase.Firestore;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
public class NewSession : MonoBehaviour  // PENDIENTE: Actualizar el valor de Active trajectory
{
    [Header("Log opcional para pruebas y errores")]
    public TextMeshProUGUI debugText; // Para Display Debugs

    // Variables para uso interno
    FirebaseFirestore db;
    public void Start()
    {
        db = FirebaseFirestore.DefaultInstance; // Inicializar firestore 
    }
    public void PlayAgain()
    {
        Log("COMIENZA PlayAgain");
        // Validar que existan los IDs de trayectoria y UnityConfig y declarar variables de firebase
        if (SelectUser.Instance == null || string.IsNullOrEmpty(SelectUser.Instance.IDux))
        {
            Log("SelectUser.Instance == null || string.IsNullOrEmpty(SelectUser.Instance.IDux");
            return;
        }

        // Leer valores del ID y el número de  repetición
        string IDux = SelectUser.Instance.IDux;
        int newRep = SelectUser.Instance.CurrentRepetition; // EnGame ya lo aumentó

        //Ver si ya terminó
        if (newRep > SelectUser.Instance.totalReps)
        {
            Log($"Protocolo completado. {SelectUser.Instance.totalReps} repeticiones finalizadas.");
            SceneManager.LoadScene("0 Seleccionar usuario"); // volver al menú de selección
            return;
        }

        // Actualizar el IDSession
        string idses = $"SessionNum{newRep:D3}-{IDux}";
        SelectUser.Instance.IDSession = idses;

        // Pasar a la siguiente trayectoria
        List<string> trajectoryOrder = SelectUser.Instance.trajectoryOrder;
        string nextTrajectory = trajectoryOrder[newRep-1];

        SelectUser.Instance.ActiveTrajectory = nextTrajectory;

        // Recargar escena
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    void Log(string msg)
    {
        Debug.Log(msg);
        if (debugText != null) debugText.text += "\n" + msg;
    }
}