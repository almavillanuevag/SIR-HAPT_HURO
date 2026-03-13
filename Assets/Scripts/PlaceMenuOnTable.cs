using Meta.XR.MRUtilityKit;
using System.Collections;
using UnityEngine;

public class PlaceMenuOnTable : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Transform de la cámara del usuario (OVRCameraRig / CenterEyeAnchor).")]
    public Transform cameraTransform;
    public Transform referencePoint;

    // Variables internas
    float yOffset = 0.02f;
    //float forwardOffset = 0.15f;
    //float pitchX = 30f;
    int maxRetries = 120;
    float retryInterval = 0.05f;

    private void Start()
    {
        if (cameraTransform == null) return;

        StartCoroutine(WaitAndPlace());
    }
    private IEnumerator WaitAndPlace()
    {
        // Esperar MRUK
        while (MRUK.Instance == null) yield return null;

        // Esperar cuarto
        while (MRUK.Instance.GetCurrentRoom() == null) yield return null;
        MRUKRoom room = MRUK.Instance.GetCurrentRoom();

        // Esperar anchors
        while (room.Anchors == null || room.Anchors.Count == 0) yield return null;

        // Buscar anchor TABLE más cercano (con reintentos)
        MRUKAnchor table = null;
        for (int i = 0; i < maxRetries; i++)
        {
            table = FindClosestTable(room, referencePoint);
            if (table != null) break;
            yield return new WaitForSeconds(retryInterval);
        }

        if (table == null)
        {
            Debug.LogWarning("[PlaceMenuOnTable] No se encontró un anchor TABLE en la escena.");
            yield break;
        }

        PlaceOnTable(table);
        //if (cameraTransform != null)
        //{
        //    Vector3 direction = cameraTransform.position - transform.position;
        //    direction.y = 0f; 
        //    Quaternion yaw = Quaternion.LookRotation(-direction, Vector3.up);
        //    Quaternion pitch = Quaternion.Euler(pitchX, 0f, 0f);
        //    transform.rotation = yaw * pitch;
        //}
    }

    private MRUKAnchor FindClosestTable(MRUKRoom room, Transform refPoint)
    {
        MRUKAnchor best = null;
        float bestDist = float.PositiveInfinity;

        foreach (var anchor in room.Anchors)
        {
            if (anchor == null) continue;
            if (anchor.Label != MRUKAnchor.SceneLabels.TABLE) continue;

            float d = Vector3.Distance(refPoint.position, anchor.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = anchor;
            }
        }

        return best;
    }

    private void PlaceOnTable(MRUKAnchor anchor)
    {
        // Obtener bounds del anchor (Collider preferido)
        if (!TryGetAnchorBounds(anchor, out Bounds anchorBounds))
        {
            // Fallback: usar posición del anchor como "centro"
            anchorBounds = new Bounds(anchor.transform.position, Vector3.zero);
        }

        // Calcular punto destino en el anchor
        Vector3 targetPointWorld = new Vector3(anchorBounds.center.x, anchorBounds.max.y, anchorBounds.center.z); ;
        // Offset vertical final (signo depende de placement)
        float signedYOffset = Mathf.Abs(yOffset);

        targetPointWorld += Vector3.up * signedYOffset;

        // Obtener bounds del objeto a mover (Renderer preferido)
        if (!TryGetObjectBounds(out Bounds objBounds))
        {
            // Fallback: usa pivot como referencia
            transform.position = targetPointWorld;
            return;
        }

        // Elegir qué punto del objeto alinear al target
        Vector3 c = objBounds.center;
        Vector3 objectRefPoint = new Vector3(c.x, objBounds.min.y, c.z);

        // Calcular delta para mover
        Vector3 delta = targetPointWorld - objectRefPoint;
        transform.position += delta;
    }

    private bool TryGetObjectBounds(out Bounds b)
    {
        Renderer[] rends = GetComponentsInChildren<Renderer>();
        if (rends == null || rends.Length == 0)
        {
            b = default;
            return false;
        }

        b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
            b.Encapsulate(rends[i].bounds);

        return true;
    }

    private bool TryGetAnchorBounds(MRUKAnchor anchor, out Bounds b)
    {
        // Collider del anchor (si el building block lo genera)
        Collider col = anchor.GetComponentInChildren<Collider>();
        if (col != null)
        {
            b = col.bounds;
            return true;
        }

        Renderer rend = anchor.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            b = rend.bounds;
            return true;
        }

        b = default;
        return false;
    }
}