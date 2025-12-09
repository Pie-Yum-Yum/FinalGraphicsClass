using UnityEngine;

public class AimAtPoint : MonoBehaviour
{
    [Header("References")]
    [Tooltip("One anchor transform per leg. E.g. 6 anchors for 6 legs.")]
    public TNode[] anchorPoints;
    [Tooltip("Corresponding aim point transform per leg. Must match length of anchorPoints.")]
    public TNode[] aimPoints;

    public JumpController JumpController;

    [Header("Raycast")]
    public float maxRayDistance = 2f;
    public LayerMask layerMask = ~0;

    [Header("Smoothing")]
    public float smoothTime = 0.08f;
    [Tooltip("How quickly feet tuck toward anchors during a jump")]
    public float tuckSmoothTime = 0.05f;
    [Tooltip("Local-space offset from each anchor used while tucking during a jump (e.g., pull under the body)")]
    public Vector3 tuckOffsetLocal = new Vector3(0f, -0.1f, 0f);
    [Tooltip("Blend time used right after landing so feet settle naturally instead of snapping")]
    public float landingBlendTime = 0.15f;

    [Header("Stepping")]
    [Tooltip("Distance the aim point must move before the foot takes a step")]
    public float stepThreshold = 0.35f;
    [Tooltip("Height of the stepping arc")]
    public float stepHeight = 0.15f;
    [Tooltip("Speed of the step (larger = faster)")]
    public float stepSpeed = 4f;
    [Tooltip("Gait cycle frequency in cycles per second. Used to phase legs so they step in groups.")]
    public float gaitCycleFrequency = 1f;
    [Tooltip("Fraction of the gait cycle during which a leg is allowed to start a step (0-1)")]
    [Range(0.01f, 1f)]
    public float stepWindow = 0.25f;
    [Tooltip("Per-leg phase offsets (0-1). If empty, all legs default to 0. Use the context menu to auto-fill a tripod gait when you have 6 legs.")]
    public float[] phaseOffsets;

    [Header("Visuals (optional)")]
    public GameObject footPrefab;    // optional visual marker for the computed foot/target
    [Header("Debug")]
    public bool enableDebugLogs = true;

    Vector3[] footPositions;
    Vector3[] footVelocities;
    [SerializeField] TNode[] footMarkers = new TNode[6];
    // stepping state
    bool[] isStepping;
    Vector3[] stepStarts;
    Vector3[] stepTargets;
    float[] stepProgress;
    float[] pendingStepTime;
    bool wasMoving = false;
    bool wasJumpingLast = false;
    float landingBlendRemaining = 0f;
    Vector3[] landingStartPositions;

    int totalLegs => Mathf.Min((anchorPoints != null) ? anchorPoints.Length : 0, (aimPoints != null) ? aimPoints.Length : 0);

    TNode tnode;

    void Awake()
    {
        tnode = GetComponent<TNode>();
    }

    void OnValidate()
    {
        // Editor-time sanity checks to help the user
        if (anchorPoints == null || aimPoints == null) return;
        if (anchorPoints.Length != aimPoints.Length)
        {
            Debug.LogWarning($"AimAtPoint: anchorPoints length ({anchorPoints.Length}) != aimPoints length ({aimPoints.Length}). Make them match.");
        }
        if (phaseOffsets != null && phaseOffsets.Length != 0 && totalLegs != 0 && phaseOffsets.Length != totalLegs)
        {
            Debug.LogWarning($"AimAtPoint: phaseOffsets length ({phaseOffsets.Length}) does not match totalLegs ({totalLegs}).");
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
        //footMarkers = new Transform[totalLegs];
        // stepping arrays
        isStepping = new bool[totalLegs];
        stepStarts = new Vector3[totalLegs];
        stepTargets = new Vector3[totalLegs];
        stepProgress = new float[totalLegs];
        pendingStepTime = new float[totalLegs];

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
            pendingStepTime[i] = -1f;

            if (footPrefab != null)
            {
                //GameObject go = Instantiate(footPrefab, desired, Quaternion.identity, transform);
                //go.name = "aim" + i;
                //footMarkers[i] = go.transform;
            }
        }
    }

    void Update()
    {
        if (totalLegs == 0) return;

        bool isJumpingNow = (JumpController != null && JumpController.isJumping);

        // Precompute desired positions for all legs so we can detect movement start
        Vector3[] desiredPositions = new Vector3[totalLegs];
        for (int i = 0; i < totalLegs; i++)
        {
            // If jumping, tuck toward anchor position plus optional local offset
            if (isJumpingNow && anchorPoints != null && i < anchorPoints.Length && anchorPoints[i] != null)
            {
                // Determine side by anchor index: assume first half = one side, second half = other side
                // For 6 legs: 0,1,2 are one side and 3,4,5 are the other
                Vector3 offset = tuckOffsetLocal;
                
                // If anchor is in the second half, flip the x offset
                if (i >= totalLegs / 2)
                {
                    offset.x *= -1f;
                }
                
                // Debug first frame of jump
                if ((i == 0 || i == 3) && !wasJumpingLast)
                {
                    //Debug.Log($"Leg {i} ({anchorPoints[i].name}): offset={offset}, tuckOffsetLocal={tuckOffsetLocal}");
                }
                
                // Convert offset from body local space to world space and apply to anchor position
                Vector3 worldOffset = tnode.TransformVector(offset);
                desiredPositions[i] = anchorPoints[i].GetWorldPosition() + worldOffset;
            }
            else
            {
                desiredPositions[i] = ComputeDesiredPositionForIndex(i);
            }
        }

        // On landing: start a blend so feet settle naturally
        if (!isJumpingNow && wasJumpingLast)
        {
            landingBlendRemaining = landingBlendTime;
            if (landingStartPositions == null || landingStartPositions.Length != totalLegs)
                landingStartPositions = new Vector3[totalLegs];
            for (int i = 0; i < totalLegs; i++)
            {
                landingStartPositions[i] = footPositions[i];
                stepTargets[i] = desiredPositions[i];
                stepStarts[i] = desiredPositions[i];
                footVelocities[i] = Vector3.zero;
                isStepping[i] = false;
                stepProgress[i] = 0f;
                pendingStepTime[i] = -1f;
            }
            wasMoving = false; // force movement detection to restart cleanly
        }

        // decrement landing blend timer
        if (landingBlendRemaining > 0f)
        {
            landingBlendRemaining = Mathf.Max(0f, landingBlendRemaining - Time.deltaTime);
        }

        // detect transition from stopped to moving: any leg wants to move beyond threshold (skip while jumping)
        if (!isJumpingNow)
        {
            bool movingNow = false;
            for (int i = 0; i < totalLegs; i++)
            {
                float dcheck = Vector3.Distance(desiredPositions[i], footPositions[i]);
                if (dcheck > stepThreshold * 0.5f) // smaller sensitivity to detect start
                {
                    movingNow = true;
                    break;
                }
            }

            if (movingNow && !wasMoving)
            {
                // movement just started: schedule staggered steps for alternating groups
                OnMovementStart(desiredPositions);
            }

            wasMoving = movingNow;
        }

        for (int i = 0; i < totalLegs; i++)
        {
            Vector3 desired = desiredPositions[i];

            // If jumping: force tuck toward anchor, disable stepping
            if (isJumpingNow)
            {
                isStepping[i] = false;
                footPositions[i] = Vector3.SmoothDamp(footPositions[i], desired, ref footVelocities[i], tuckSmoothTime);
                stepProgress[i] = 0f;
                pendingStepTime[i] = -1f;
            }
            // If recently landed, blend feet back to desired smoothly without stepping
            else if (landingBlendRemaining > 0f)
            {
                float t = 1f - (landingBlendRemaining / Mathf.Max(0.0001f, landingBlendTime));
                footPositions[i] = Vector3.Lerp(landingStartPositions[i], desired, t);
                footVelocities[i] = Vector3.zero;
                isStepping[i] = false;
                stepProgress[i] = 0f;
                pendingStepTime[i] = -1f;
            }
            // if currently stepping, advance the step
            else if (isStepping[i])
            {
                stepProgress[i] += Time.deltaTime * stepSpeed;
                float p = Mathf.Clamp01(stepProgress[i]);

                // horizontal interpolation
                Vector3 horizontal = Vector3.Lerp(stepStarts[i], stepTargets[i], p);
                // vertical arc (sin curve)
                float arc = Mathf.Sin(p * Mathf.PI) * stepHeight;
                footPositions[i] = horizontal + tnode.parent.GetUp() * arc;

                if (p >= 1f)
                {
                    isStepping[i] = false;
                    footPositions[i] = stepTargets[i];
                    footVelocities[i] = Vector3.zero;
                }
            }
            else
            {
                // decide whether to take a step:
                // separate horizontal (lateral) and forward components relative to the leg anchor.
                // trigger a step if lateral displacement exceeds half the step threshold OR
                // forward displacement exceeds the full step threshold. This prevents clipping while strafing.
                float d = Vector3.Distance(desired, footPositions[i]);
                float lateral = 0f;
                float forward = 0f;
                if (anchorPoints != null && i < anchorPoints.Length && anchorPoints[i] != null)
                {
                    TNode a = anchorPoints[i];
                    Vector3 delta = desired - footPositions[i];
                    lateral = Vector3.Dot(delta, a.GetRight());
                    forward = Vector3.Dot(delta, a.GetForward());
                }
                bool lateralTooFar = Mathf.Abs(lateral) > (stepThreshold * 0.5f);
                bool forwardTooFar = Mathf.Abs(forward) > stepThreshold;
                if (d > stepThreshold || lateralTooFar || forwardTooFar || JumpController.isJumping)
                {
                    bool startNow = false;
                    if(lateralTooFar) pendingStepTime[i] = 0f;
                    // start if pending time reached
                    if (pendingStepTime[i] > 0f && Time.time >= pendingStepTime[i]) startNow = true;
                    // or if allowed by phase window
                    if (IsInPhaseWindow(i)) startNow = true;

                    if (startNow)
                    {
                        isStepping[i] = true;
                        stepStarts[i] = footPositions[i];
                        stepTargets[i] = desired;
                        stepProgress[i] = 0f;
                        pendingStepTime[i] = -1f;
                    }
                }
                else
                {
                    // optionally allow small smoothing to gently follow small movements
                    footPositions[i] = Vector3.SmoothDamp(footPositions[i], desired, ref footVelocities[i], smoothTime);
                }
            }

            if (footMarkers[i] != null)
            {
                footMarkers[i].SetWorldPosition(footPositions[i]);
                // orient marker to face its matching aim point for clarity (if available)
                if (aimPoints != null && i < aimPoints.Length && aimPoints[i] != null)
                {
                    Quaternion q = Quaternion.LookRotation((aimPoints[i].GetWorldPosition() - footPositions[i]).normalized, tnode.GetUp());
                    footMarkers[i].SetRotation(q);
                }
            }
        }

        // record jump state for next frame transition detection
        wasJumpingLast = isJumpingNow;
    }

    // Compute desired target position for a given leg index by raycasting from the per-leg anchor toward its matching aim point.
    Vector3 ComputeDesiredPositionForIndex(int index)
    {
        if (anchorPoints == null || aimPoints == null) return Vector3.zero;
        if (index < 0 || index >= totalLegs) return Vector3.zero;

        TNode a = anchorPoints[index];
        TNode t = aimPoints[index];
        if (a == null || t == null) return Vector3.zero;

        Vector3 worldOrigin = a.GetWorldPosition();
        Vector3 dir = (t.GetWorldPosition() - worldOrigin);
        float dist = dir.magnitude;
        if (dist <= 0.0001f) dir = a.GetForward(); else dir /= dist;

        RaycastHit hit;
        if (Physics.Raycast(worldOrigin, dir, out hit, maxRayDistance, layerMask))
        {
            return hit.point;
        }

        // fallback: point along the ray at max distance
        return worldOrigin + dir * maxRayDistance;
    }

    // Return whether the given leg index is currently inside its allowed phase window to start a step.
    bool IsInPhaseWindow(int index)
    {
        if (phaseOffsets == null || phaseOffsets.Length == 0) return true;
        if (index < 0 || index >= totalLegs) return true;

        float offset = Mathf.Clamp01((index < phaseOffsets.Length) ? phaseOffsets[index] : 0f);
        float cyclePos = Mathf.Repeat(Time.time * gaitCycleFrequency, 1f);
        float halfWindow = stepWindow * 0.5f;

        // compute wrapped distance on circular phase [0,1]
        float delta = Mathf.Abs(Mathf.DeltaAngle(cyclePos * 360f, offset * 360f)) / 360f;
        return delta <= halfWindow;
    }

    [ContextMenu("Auto-Fill Tripod Phases (alternating)")]
    void AutoFillTripodPhases()
    {
        if (totalLegs == 0)
        {
            Debug.LogWarning("Cannot autofill phases: no legs assigned.");
            return;
        }
        phaseOffsets = new float[totalLegs];
        for (int i = 0; i < totalLegs; i++)
        {
            // alternating phases: even legs = 0, odd legs = 0.5 (two groups)
            phaseOffsets[i] = (i % 2 == 0) ? 0f : 0.5f;
        }
        Debug.Log($"Auto-filled tripod phases for {totalLegs} legs (alternating 0/0.5).");
    }

    // Called when movement begins to stagger initial steps so legs oscillate immediately.
    void OnMovementStart(Vector3[] desiredPositions)
    {
        if (totalLegs == 0) return;

        // Determine groups: use phaseOffsets if available, otherwise even/odd
        int[] groups = new int[totalLegs];
        for (int i = 0; i < totalLegs; i++)
        {
            if (phaseOffsets != null && phaseOffsets.Length == totalLegs)
                groups[i] = (phaseOffsets[i] < 0.5f) ? 0 : 1;
            else
                groups[i] = i % 2; // 0 or 1 alternating
        }

        // start group 0 immediately (if they need to move), schedule group 1 half a cycle later
        float halfCycleDelay = 0.5f / Mathf.Max(0.0001f, gaitCycleFrequency);
        for (int i = 0; i < totalLegs; i++) pendingStepTime[i] = -1f;

        for (int i = 0; i < totalLegs; i++)
        {
            float d = Vector3.Distance(desiredPositions[i], footPositions[i]);
            if (d <= stepThreshold) continue;

            if (groups[i] == 0)
            {
                // start immediately
                isStepping[i] = true;
                stepStarts[i] = footPositions[i];
                stepTargets[i] = desiredPositions[i];
                stepProgress[i] = 0f;
                pendingStepTime[i] = -1f;
            }
            else
            {
                // schedule to start after half cycle so groups alternate
                pendingStepTime[i] = Time.time + halfCycleDelay;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (anchorPoints == null || aimPoints == null) return;
        if (totalLegs == 0) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < totalLegs; i++)
        {
            TNode a = anchorPoints[i];
            TNode t = aimPoints[i];
            if (a == null || t == null) continue;
            Gizmos.DrawSphere(a.GetWorldPosition(), 0.02f);
            Gizmos.DrawLine(a.GetWorldPosition(), t.GetWorldPosition());
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
