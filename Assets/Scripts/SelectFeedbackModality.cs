using TMPro;
using UnityEngine;
using System.Collections;

public class SelectFeedbackModality : MonoBehaviour
{
    [Header("Asignar elementos en el inspector")]
    public GameObject HapticManager;
    public GameObject Trajectory;
    public EndGame EndGame;

    [Header("Para Log en texto (opcional)")]
    public TextMeshProUGUI debugText;
    void Start()
    {
        if (SelectUser.Instance == null)
        {
            Log("ERROR: No hay instancia de SelectUser.");
            return;
        }
        string modality = SelectUser.Instance.experimentalGroup;
        Log($"Modalidad: {modality}");
        // Retroalimentación visual 
        TrajectoryManager TrajectoryManager;
        TrajectoryManager = Trajectory.GetComponent<TrajectoryManager>();
        TrajectoryManager.VisualFeedback = modality == "VisualFeedback" || modality == "MultimodalFeedback";
        // No ver el tubo (MeshRenderer del SplineExtrude) cuando es hapticWithoutTube
        MeshRenderer tubeMesh = Trajectory.GetComponent<MeshRenderer>();
        if (tubeMesh != null)
            tubeMesh.enabled = !(modality == "HapticWithoutTube");
        // Tambien configurarlo dentro del TrajectoryDataLoad
        TrajectoryDataLoad TrajectoryDataLoad = Trajectory.GetComponent<TrajectoryDataLoad>();
        TrajectoryDataLoad.HapticWithoutTube = modality == "HapticWithoutTube";
        // Agregar más flechas al 
        // Retroalimentación háptica 
        bool useHaptic = modality == "HapticFeedback" ||
                         modality == "MultimodalFeedback" ||
                         modality == "HapticWithoutTube";
        HapticManager.SetActive(useHaptic);
        EndGame.HapticFeedback = useHaptic;
        Log($"Visual: {modality == "VisualFeedback" || modality == "MultimodalFeedback"} | Háptica: {useHaptic} | Tubo visible: {!(modality == "HapticWithoutTube")}");

        if(modality == "HapticWithoutTube")
            StartCoroutine(ApplyVisualConfig(modality));

    }
    IEnumerator ApplyVisualConfig(string modality)
    {
        TrajectoryDataLoad loader = Trajectory.GetComponent<TrajectoryDataLoad>();
        TableInitialPlacement table = Trajectory.GetComponent<TableInitialPlacement>();

        if (loader != null)
        {
            yield return new WaitUntil(() =>
                loader.State == TrajectoryDataLoad.LoadState.Ready ||
                loader.State == TrajectoryDataLoad.LoadState.Failed);
        }

        // Aquí se modifica únicamente el MeshRenderer
        MeshRenderer tubeMesh = Trajectory.GetComponent<MeshRenderer>();

        if (tubeMesh != null)
        {
            tubeMesh.enabled = false;
            Log("MeshRenderer desactivado (HapticWithoutTube)");
        }

        loader.HapticWithoutTube = true;
        table.HapticWithoutTube = true;
    }
    private void Log(string msg)
    {
        Debug.Log($"[SelectFeedbackModality] {msg}");
        if (debugText != null) debugText.text += "\n" + msg;
    }
}