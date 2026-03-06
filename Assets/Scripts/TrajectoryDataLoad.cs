using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class TrajectoryDataLoad : MonoBehaviour
{
    [Header("Log opcional para pruebas y errores")]
    public TextMeshProUGUI debugText;

    [Header("Variables públicas para lectura")]
    public int knotCount = 15;
    public SplineContainer KnotsSpline;
    public SplineExtrude splineExtrude;
    public float radio;
    public bool HapticWithoutTube;

    public enum LoadState { Idle, Loading, Ready, NoData, Failed }
    public LoadState State { get; private set; } = LoadState.Idle;

    // Variables privadas
    string jsonFileName = "Trajectory_DATA";
    float tubeRadius = 0.05f;

    [System.Serializable]
    private class TrajectoryPoint
    {
        public float x;
        public float y;
        public float z;
    }

    [System.Serializable]
    private class TrajectoryEntry
    {
        public string id;
        public List<TrajectoryPoint> puntos;
    }

    [System.Serializable]
    private class TrajectoryFile
    {
        public List<TrajectoryEntry> trayectorias;
    }

    private void Start()
    {
        if (KnotsSpline == null) KnotsSpline = GetComponent<SplineContainer>();
        if (splineExtrude == null) splineExtrude = GetComponent<SplineExtrude>();

        TableInitialPlacement tp = GetComponent<TableInitialPlacement>();
        if (tp != null) tp.SetSceneObjectsVisible(false);

        string IDux = SelectUser.Instance.IDux;
        string activeTrajectory = SelectUser.Instance.ActiveTrajectory;
        StartCoroutine(LoadTrajectoryFromJson(activeTrajectory));
    }

    private IEnumerator LoadTrajectoryFromJson(string trajectoryId)
    {
        // Esperar a que SelectUser tenga datos válidos
        while (SelectUser.Instance == null)
            yield return null;
        while (string.IsNullOrEmpty(SelectUser.Instance.IDux))
            yield return null;
        while (string.IsNullOrEmpty(SelectUser.Instance.ActiveTrajectory))
            yield return null;

        // Usar el ID activo si no se pasó uno concreto
        if (string.IsNullOrEmpty(trajectoryId))
            trajectoryId = SelectUser.Instance.ActiveTrajectory;

        State = LoadState.Loading;

        Log($"Cargando trayectoria '{trajectoryId}' desde JSON...");

        // --- Leer el archivo JSON desde StreamingAssets ---
        string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, jsonFileName + ".json");

        // En Android (Quest) StreamingAssets se lee con UnityWebRequest
        string jsonText = null;

#if UNITY_ANDROID && !UNITY_EDITOR
        using (var req = UnityEngine.Networking.UnityWebRequest.Get(filePath))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Log($"ERROR leyendo JSON (Android): {req.error}");
                State = LoadState.Failed;
                yield break;
            }
            jsonText = req.downloadHandler.text;
        }
#else
        if (!System.IO.File.Exists(filePath))
        {
            Log($"ERROR: No se encontró el archivo '{filePath}'");
            State = LoadState.Failed;
            yield break;
        }
        jsonText = System.IO.File.ReadAllText(filePath);
        yield return null; // un frame para no bloquear
#endif

        // --- Deserializar ---
        TrajectoryFile trajectoryFile = null;
        try
        {
            trajectoryFile = JsonUtility.FromJson<TrajectoryFile>(jsonText);
        }
        catch (System.Exception e)
        {
            Log($"ERROR parseando JSON: {e.Message}");
            State = LoadState.Failed;
            yield break;
        }

        if (trajectoryFile == null || trajectoryFile.trayectorias == null || trajectoryFile.trayectorias.Count == 0)
        {
            Log("ERROR: JSON vacío o mal formado.");
            State = LoadState.Failed;
            yield break;
        }

        // --- Buscar la trayectoria que coincide con el ID ---
        TrajectoryEntry match = trajectoryFile.trayectorias.Find(t => t.id == trajectoryId);

        if (match == null)
        {
            Log($"ERROR: No se encontró trayectoria con id='{trajectoryId}' en el JSON.");
            State = LoadState.NoData;
            yield break;
        }

        if (match.puntos == null || match.puntos.Count < 2)
        {
            Log($"ERROR: Trayectoria '{trajectoryId}' tiene menos de 2 puntos.");
            State = LoadState.NoData;
            yield break;
        }

        // --- Convertir a List<Vector3> ---
        List<Vector3> worldPoints = new List<Vector3>(match.puntos.Count);
        foreach (var p in match.puntos)
            worldPoints.Add(new Vector3(p.x, p.y, p.z));

        Log($"Trayectoria '{trajectoryId}' cargada: {worldPoints.Count} puntos.");

        // --- Resamplear y construir spline ---
        List<Vector3> resampled = ResampleByArcLength(worldPoints, knotCount);
        BuildSplineFromWorldKnots(resampled);

        // --- Configurar SplineExtrude ---
        if (splineExtrude != null)
        {
            splineExtrude.Radius = tubeRadius;
            splineExtrude.enabled = true;
            splineExtrude.Rebuild();
            radio = splineExtrude.Radius;
        }

        Log($"Spline construido | Knots: {resampled.Count} | Radio: {radio:0.000} m");

        State = LoadState.Ready;
        var mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;
        StartCoroutine(PlaceOnTableAfterSplineReady());
    }

    private IEnumerator PlaceOnTableAfterSplineReady() // Posicionar en mesa una vez listo el spline
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        TableInitialPlacement tp = GetComponent<TableInitialPlacement>();
        if (tp != null)
            tp.SetTrajectoryOnTable();
        else
            Log("ERROR: No existe TableInitialPlacement en este GameObject.");
    }

    // Resampleo y construcción del spline

    private List<Vector3> ResampleByArcLength(List<Vector3> pts, int targetCount)
    {
        List<Vector3> result = new List<Vector3>();
        if (pts == null || pts.Count < 2) return result;
        if (targetCount < 2) targetCount = 2;

        float[] cum = new float[pts.Count];
        cum[0] = 0f;
        for (int i = 1; i < pts.Count; i++)
            cum[i] = cum[i - 1] + Vector3.Distance(pts[i - 1], pts[i]);

        float total = cum[cum.Length - 1];
        if (total < 1e-6f)
        {
            result.Add(pts[0]);
            result.Add(pts[pts.Count - 1]);
            return result;
        }

        for (int k = 0; k < targetCount; k++)
        {
            float d = total * k / (targetCount - 1);
            int idx = 0;
            while (idx < cum.Length - 1 && cum[idx + 1] < d) idx++;

            float segStart = cum[idx];
            float segEnd = cum[Mathf.Min(idx + 1, cum.Length - 1)];
            float alpha = (segEnd > segStart) ? (d - segStart) / (segEnd - segStart) : 0f;

            result.Add(Vector3.Lerp(pts[idx], pts[Mathf.Min(idx + 1, pts.Count - 1)], alpha));
        }

        return result;
    }

    private void BuildSplineFromWorldKnots(List<Vector3> knotsWorld)
    {
        if (KnotsSpline == null) KnotsSpline = GetComponent<SplineContainer>();

        var spline = KnotsSpline.Spline;
        spline.Clear();

        foreach (var worldPt in knotsWorld)
        {
            Vector3 local = KnotsSpline.transform.InverseTransformPoint(worldPt);
            float3 posL = new float3(local.x, local.y, local.z);
            spline.Add(new BezierKnot(posL, float3.zero, float3.zero, quaternion.identity));
        }

        try
        {
            for (int i = 0; i < spline.Count; i++)
                spline.SetTangentMode(i, TangentMode.AutoSmooth);
        }
        catch {  }
    }

    private void Log(string msg)
    {
        Debug.Log($"[TrajectoryDataLoad] {msg}");
        if (debugText != null) debugText.text += "\n" + msg;
    }
}