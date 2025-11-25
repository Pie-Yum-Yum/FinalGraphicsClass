using UnityEngine;

public class AimAtPoint : MonoBehaviour
{
    [Header("References")]
    [Tooltip("One anchor transform per leg. E.g. 6 anchors for 6 legs.")]
    public Transform[] anchorPoints;
    [Tooltip("Corresponding aim point transform per leg. Must match length of anchorPoints.")]
    public Transform[] aimPoints;

    [Header("Raycast")]
    public float maxRayDistance = 2f;
    public LayerMask layerMask = ~0;

    [Header("Smoothing")]
    public float smoothTime = 0.08f;

    [Header("Stepping")]
    [Tooltip("Distance the aim point must move before the foot takes a step")]
    public float stepThreshold = 0.35f;
    [Tooltip("Height of the stepping arc")]
    public float stepHeight = 0.15f;
    [Tooltip("Speed of the step (larger = faster)")]
    public float stepSpeed = 4f;

    [Header("Visuals (optional)")]
    public GameObject footPrefab;    // optional visual marker for the computed foot/target
    [Header("Debug")]
    public bool enableDebugLogs = true;

    Vector3[] footPositions;
    Vector3[] footVelocities;
    Transform[] footMarkers;
    // stepping state
    bool[] isStepping;
    Vector3[] stepStarts;
    Vector3[] stepTargets;
    float[] stepProgress;

    int totalLegs => Mathf.Min((anchorPoints != null) ? anchorPoints.Length : 0, (aimPoints != null) ? aimPoints.Length : 0);

    void OnValidate()
    {
        // Editor-time sanity checks to help the user
        if (anchorPoints == null || aimPoints == null) return;
        if (anchorPoints.Length != aimPoints.Length)
        {
            Debug.LogWarning($"AimAtPoint: anchorPoints length ({anchorPoints.Length}) != aimPoints length ({aimPoints.Length}). Make them match.");
        }
    }

    void Start()
    {
        if (enableDebugLogs)
        {
            int aCount = (anchorPoints != null) ? anchorPoints.Length : 0;
            int tCount = (aimPoints != null) ? aimPoints.Length : 0;
            Debug.Log($"AimAtPoint Start: anchorPoints={aCount}, aimPoints={tCount}, totalLegs={totalLegs}");
            if (totalLegs == 0)
            {
                Debug.LogWarning("AimAtPoint: Please assign matching `anchorPoints` and `aimPoints` (e.g. length 6). Use the context menu helpers to auto-fill from children.");
                return;
            }
        }
        else
        {
            if (totalLegs == 0) return;
        }

        footPositions = new Vector3[totalLegs];
        footVelocities = new Vector3[totalLegs];
        footMarkers = new Transform[totalLegs];
        // stepping arrays
        isStepping = new bool[totalLegs];
        stepStarts = new Vector3[totalLegs];
        stepTargets = new Vector3[totalLegs];
        stepProgress = new float[totalLegs];

        // Initialize foot positions to current raycast results to avoid popping
        for (int i = 0; i < totalLegs; i++)
        {
            Vector3 desired = ComputeDesiredPositionForIndex(i);
            footPositions[i] = desired;
            footVelocities[i] = Vector3.zero;
            isStepping[i] = false;
            stepStarts[i] = desired;
            stepTargets[i] = desired;
            stepProgress[i] = 0f;

            if (footPrefab != null)
            {
                GameObject go = Instantiate(footPrefab, desired, Quaternion.identity, transform);
                footMarkers[i] = go.transform;
            }
        }
    }

    // Helper functions accessible from the component's context menu in the inspector
    [ContextMenu("Auto-Fill Anchors From Children (name contains 'Anchor')")]
    void AutoFillAnchorsFromChildren()
    {
        var children = GetComponentsInChildren<Transform>(true);
        var list = new System.Collections.Generic.List<Transform>();
        foreach (var c in children)
        {
            if (c == this.transform) continue;
            if (c.name.ToLower().Contains("anchor")) list.Add(c);
        }
        anchorPoints = list.ToArray();
        Debug.Log($"Auto-filled {anchorPoints.Length} anchorPoints from children.");
    }

    [ContextMenu("Auto-Fill Aims From Children (name contains 'Aim')")]
    void AutoFillAimsFromChildren()
    {
        var children = GetComponentsInChildren<Transform>(true);
        var list = new System.Collections.Generic.List<Transform>();
        foreach (var c in children)
        {
            if (c == this.transform) continue;
            if (c.name.ToLower().Contains("aim")) list.Add(c);
        }
        aimPoints = list.ToArray();
        Debug.Log($"Auto-filled {aimPoints.Length} aimPoints from children.");
    }

    void Update()
    {
        if (totalLegs == 0) return;

        for (int i = 0; i < totalLegs; i++)
        {
            Vector3 desired = ComputeDesiredPositionForIndex(i);

            // if currently stepping, advance the step
            if (isStepping[i])
            {
                stepProgress[i] += Time.deltaTime * stepSpeed;
                float p = Mathf.Clamp01(stepProgress[i]);

                // horizontal interpolation
                Vector3 horizontal = Vector3.Lerp(stepStarts[i], stepTargets[i], p);
                // vertical arc (sin curve)
                float arc = Mathf.Sin(p * Mathf.PI) * stepHeight;
                footPositions[i] = horizontal + Vector3.up * arc;

                if (p >= 1f)
                {
                    isStepping[i] = false;
                    footPositions[i] = stepTargets[i];
                    footVelocities[i] = Vector3.zero;
                }
            }
            else
            {
                // decide whether to take a step: if desired is far from current foot position
                float d = Vector3.Distance(desired, footPositions[i]);
                if (d > stepThreshold)
                {
                    // start step
                    isStepping[i] = true;
                    stepStarts[i] = footPositions[i];
                    stepTargets[i] = desired;
                    stepProgress[i] = 0f;
                }
                else
                {
                    // optionally allow small smoothing to gently follow small movements
                    footPositions[i] = Vector3.SmoothDamp(footPositions[i], desired, ref footVelocities[i], smoothTime);
                }
            }

            if (footMarkers[i] != null)
            {
                footMarkers[i].position = footPositions[i];
                // orient marker to face its matching aim point for clarity (if available)
                if (aimPoints != null && i < aimPoints.Length && aimPoints[i] != null)
                    footMarkers[i].rotation = Quaternion.LookRotation((aimPoints[i].position - footPositions[i]).normalized, Vector3.up);
            }
        }
    }

    // Compute desired target position for a given leg index by raycasting from the per-leg anchor toward its matching aim point.
    Vector3 ComputeDesiredPositionForIndex(int index)
    {
        if (anchorPoints == null || aimPoints == null) return Vector3.zero;
        if (index < 0 || index >= totalLegs) return Vector3.zero;

        Transform a = anchorPoints[index];
        Transform t = aimPoints[index];
        if (a == null || t == null) return Vector3.zero;

        Vector3 worldOrigin = a.position;
        Vector3 dir = (t.position - worldOrigin);
        float dist = dir.magnitude;
        if (dist <= 0.0001f) dir = a.forward; else dir /= dist;

        RaycastHit hit;
        if (Physics.Raycast(worldOrigin, dir, out hit, maxRayDistance, layerMask))
        {
            return hit.point;
        }

        // fallback: point along the ray at max distance
        return worldOrigin + dir * maxRayDistance;
    }

    void OnDrawGizmosSelected()
    {
        if (anchorPoints == null || aimPoints == null) return;
        if (totalLegs == 0) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < totalLegs; i++)
        {
            Transform a = anchorPoints[i];
            Transform t = aimPoints[i];
            if (a == null || t == null) continue;
            Gizmos.DrawSphere(a.position, 0.02f);
            Gizmos.DrawLine(a.position, t.position);
        }

        // draw computed foot positions
        if (footPositions != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < footPositions.Length; i++)
            {
                Gizmos.DrawSphere(footPositions[i], 0.04f);
            }
        }
    }
}
