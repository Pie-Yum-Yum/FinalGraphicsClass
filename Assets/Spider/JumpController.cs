using System.Collections;
using UnityEngine;

// Moves the spider body using WS/AD for translation and Q/E for rotation.
// WS: forward/back, AD: strafe left/right, Q/E: rotate in place.
// Keeps aim points in-sync (smoothly) with the body. Attach this script to the spider `body` GameObject.
public class JumpController : MonoBehaviour
{
    [Tooltip("Aim point transforms (one per leg). These will be moved smoothly to maintain their offsets from the body.")]
    public Transform[] aimPoints;
    public AimAtPoint AimAtPoint;

    [Header("Movement")]
    public float moveSpeed = 2f;
    [Tooltip("How quickly the body reaches the target position")]
    public float positionSmoothTime = 0.12f;
    [Tooltip("Rotation speed (deg/sec) when turning toward movement direction")]
    public float rotationSpeed = 720f;

    [Header("Aim points smoothing")]
    [Tooltip("How quickly aim points follow updated body position")]
    public float aimPointSmoothTime = 0.08f;

    [Header("Body Float (ground following)")]
    [Tooltip("Enable body float that follows terrain under probes.")]
    public bool enableBodyFloat = true;
    [Tooltip("Vertical offset above the ground average (meters)")]
    public float bodyHeightOffset = 0.5f;
    [Tooltip("How quickly the body Y position smooths to the desired height (seconds)")]
    public float bodyHeightSmoothTime = 0.12f;
    [Tooltip("How quickly the body rotates to level with the ground normal")]
    public float bodyRotationSmoothSpeed = 8f;
    [Tooltip("Transforms used to sample the ground below the spider (assign same anchors used for legs)")]
    public Transform[] groundProbes;
    [Tooltip("How far above each probe to start the ground raycast")]
    public float probeRayStartHeight = 1.0f;
    [Tooltip("Maximum ray distance when sampling the ground")]
    public float probeRayDistance = 2.0f;
    [Tooltip("Layer mask used when raycasting for ground")]
    public LayerMask groundLayerMask = ~0;
    [Tooltip("Layer mask used to detect floor hits from anchor->aim rays. Set this to your 'Floor' layer.")]
    private LayerMask UILayerMask = 5; // Layer 5 is UI unless changed 
    public LayerMask floorLayerMask = 0;
    [Header("Jump / Teleport")]
    [Tooltip("Enable clicking to jump the spider to the floor hit point")]
    public bool enableJumpToFloor = true;
    [Tooltip("Duration of the jump (seconds)")]
    public float jumpDuration = 0.6f;
    [Tooltip("Arc height of the jump in meters")]
    public float jumpArcHeight = 0.6f;
    [Tooltip("Enable wall-climbing collision handling to prevent phasing through walls")]
    public bool enableWallClimb = true;
    [Tooltip("Approximate horizontal radius of the spider body used to avoid penetrating geometry")]
    public float bodyRadius = 0.5f;
    [Tooltip("Layers considered for collision when preventing penetration (walls, obstacles)")]
    public LayerMask collisionMask = ~0;
    [Tooltip("If the averaged surface normal has a Y component below this threshold, treat it as a wall for climbing.")]
    [Range(0f,1f)]
    public float climbNormalYThreshold = 0.6f;
    [Header("Anchor-based rotation (optional)")]
    [Tooltip("When enabled, use the average 'up' direction of these anchor transforms as the body's normal for leveling.")]
    public bool useAnchorNormals = false;
    [Tooltip("Anchor transforms whose up vectors will be averaged to produce the body's normal when 'useAnchorNormals' is on.")]
    public Transform[] anchorNormals;

    Vector3 bodyVelocity = Vector3.zero;
    Vector3[] aimVelocities;
    Vector3[] aimLocalOffsets;
    // body float state
    float bodyVerticalVelocity = 0f;
    Quaternion bodyRotationTarget;
    // jump state
    public bool isJumping = false;
    Vector3 jumpStartPos;
    Vector3 jumpTargetPos;
    Quaternion jumpStartRot;
    Quaternion jumpTargetRot;
    float jumpElapsed = 0f;

    void Start()
    {
        // allocate arrays
        if (aimPoints != null)
        {
            aimVelocities = new Vector3[aimPoints.Length];
            aimLocalOffsets = new Vector3[aimPoints.Length];
            for (int i = 0; i < aimPoints.Length; i++)
            {
                if (aimPoints[i] != null)
                {
                    // store offset in body's local space so we can reapply as the body moves/rotates
                    aimLocalOffsets[i] = transform.InverseTransformPoint(aimPoints[i].position);
                    aimVelocities[i] = Vector3.zero;
                }
            }
        }
    }

    void StartJumpToFloor(Vector3 point, Vector3 normal)
    {
        isJumping = true;
        jumpElapsed = 0f;
        jumpStartPos = transform.position;

        // target position sits offset from hit point along normal by bodyHeightOffset
        jumpTargetPos = point + normal.normalized * bodyHeightOffset;
        jumpStartRot = transform.rotation;
        // compute forward projected onto plane of normal to choose a sensible forward direction
        Vector3 forwardProj = Vector3.ProjectOnPlane(transform.forward, normal);
        if (forwardProj.sqrMagnitude < 0.0001f) forwardProj = Vector3.ProjectOnPlane(Vector3.right, normal);
        jumpTargetRot = Quaternion.LookRotation(forwardProj.normalized, normal.normalized);

        Vector3 jumpTargetForward = Vector3.ProjectOnPlane(transform.forward, normal.normalized);
        if(jumpTargetForward.magnitude == 0f)
        {
            jumpTargetForward = Vector3.ProjectOnPlane(Vector3.Lerp(transform.forward, transform.up, 0.1f), normal.normalized);
        }
        jumpTargetForward.Normalize();
        jumpTargetRot = Quaternion.LookRotation(jumpTargetForward.normalized, normal.normalized);
        
    }
    
    void Update()
    {
        // Mouse click -> jump to floor hit
        // Block jump if pointer is over UI (using EventSystem)
        bool isOverUI = UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        if (enableJumpToFloor && Input.GetMouseButtonDown(0) && Camera.main != null && !isOverUI)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 200f, floorLayerMask))
            {
                
                // start jump to hit.point and align to its normal
                StartJumpToFloor(hit.point, hit.normal);
            }
        }

        
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0f, v);
        if (input.sqrMagnitude > 1f) input.Normalize();

        Vector3 desiredMove = transform.TransformDirection(input) * moveSpeed;
        Vector3 targetPos = transform.position + desiredMove * Time.deltaTime;

        // If currently jumping, handle jump motion and skip usual body float/ground logic
        if (isJumping)
        {
            jumpElapsed += Time.deltaTime;
            float p = Mathf.Clamp01(jumpElapsed / Mathf.Max(0.0001f, jumpDuration));
            // horizontal interpolation
            Vector3 horiz = Vector3.Lerp(jumpStartPos, jumpTargetPos, p);
            // arc
            float arc = Mathf.Sin(p * Mathf.PI) * jumpArcHeight;

            transform.position = horiz + Vector3.up * arc;
            // rotate toward target
            transform.rotation = Quaternion.Slerp(jumpStartRot, jumpTargetRot, Mathf.SmoothStep(0f, 1f, p));

            // keep aim points following body during jump
            if (aimPoints != null && aimLocalOffsets != null)
            {
                for (int i = 0; i < aimPoints.Length; i++)
                {
                    if (aimPoints[i] == null) continue;
                    Vector3 desiredWorld = transform.TransformPoint(aimLocalOffsets[i]);
                    aimPoints[i].position = Vector3.SmoothDamp(aimPoints[i].position, desiredWorld, ref aimVelocities[i], aimPointSmoothTime);
                }
            }

            if (p >= 1f)
            {
                isJumping = false;
                bodyVerticalVelocity = 0f;
                transform.position = jumpTargetPos;
                transform.rotation = jumpTargetRot;
            }
            else
            {
                return; // mid-jump: skip ground follow and XZ smoothing to avoid jitter
            }
        }

        // Smoothly move the body position in XZ




        //Vector3 targetPosXZ = new Vector3(targetPos.x, transform.position.y, targetPos.z);
        Vector3 desiredXZ = Vector3.SmoothDamp(transform.position, targetPos, ref bodyVelocity, positionSmoothTime);

        // Prevent penetrating walls: spherecast from current to desired and clamp if hit
        if (enableWallClimb)
        {
            Vector3 moveDelta = desiredXZ - transform.position;
            float moveDist = moveDelta.magnitude;
            if (moveDist > 0.0001f)
            {
                RaycastHit hit;
                if (Physics.SphereCast(transform.position, 0.1f, moveDelta.normalized, out hit, moveDist + 0.05f, collisionMask, QueryTriggerInteraction.Ignore))
                {
                    // clamp position to be outside the hit surface, offset by bodyRadius
                    Vector3 hitPos = hit.point + hit.normal * (bodyRadius + 0.01f);
                    desiredXZ = new Vector3(hitPos.x, hitPos.y, hitPos.z);

                    if(!isJumping) StartJumpToFloor(hit.point, hit.normal);
                }
            }
        }

        transform.position = desiredXZ;

        // Rotation via Q/E keys (rotate in place). This keeps translation independent (WS = forward/back, AD = strafe).
        float rotInput = 0f;
        if (Input.GetKey(KeyCode.Q)) rotInput -= 1f;
        if (Input.GetKey(KeyCode.E)) rotInput += 1f;
        if (Mathf.Abs(rotInput) > 0.001f)
        {
            transform.Rotate(transform.up, rotInput * rotationSpeed * Time.deltaTime, Space.World);
        }

        // --- Body float: sample ground under probes and adjust body height + leveling rotation ---
        if (enableBodyFloat && groundProbes != null && groundProbes.Length > 0)
        {
            Vector3 avgPoint = Vector3.zero;
            Vector3 avgNormal = Vector3.up;
            int hitCount = 0;
            for (int i = 0; i < groundProbes.Length; i++)
            {
                var p = groundProbes[i];
                if (p == null) continue;
                Vector3 rayStart = p.position + transform.up * probeRayStartHeight;
                RaycastHit hit;
                if (Physics.Raycast(rayStart, -transform.up, out hit, probeRayDistance + probeRayStartHeight, groundLayerMask))
                {
                    avgPoint += hit.point;
                    avgNormal += hit.normal;
                    hitCount++;
                }
            }

            if (hitCount > 0)
            {
                // **code piece from chatgpt**
                // Average the contact point and normal
                avgPoint /= hitCount;
                avgNormal.Normalize();

                // Compute where the spider body should be along the surface normal
                Vector3 currentPos = transform.position;

                // Find how far the current body is from the average plane
                float dist = Vector3.Dot(avgNormal, currentPos - avgPoint);

                // Compute target position by correcting along the normal
                Vector3 desiredPos = currentPos - avgNormal * (dist - bodyHeightOffset);

                // Smooth the movement along the normal direction
                Vector3 smoothVelocity = Vector3.zero;  // make this persistent in your class!
                transform.position = Vector3.SmoothDamp(
                    currentPos,
                    desiredPos,
                    ref smoothVelocity,
                    bodyHeightSmoothTime
                );
                // end code piece

                // First try: cast rays from each groundProbe toward its corresponding aimPoint and collect normals when they hit the Floor layer
                // First: try to detect walls by casting from probe -> aim against groundLayerMask (or collisionMask) to find steep surfaces
                Vector3 sumGeneralNormals = Vector3.zero;
                Vector3 sumGeneralPoints = Vector3.zero;
                int generalHitCount = 0;
                if (aimPoints != null)
                {
                    int probeCount = Mathf.Min(groundProbes.Length, aimPoints.Length);
                    for (int i = 0; i < probeCount; i++)
                    {
                        var origin = groundProbes[i];
                        var aim = aimPoints[i];
                        if (origin == null || aim == null) continue;
                        Vector3 dir = aim.position - origin.position;
                        float d = dir.magnitude;
                        if (d < 0.001f) continue;
                        Vector3 dirNorm = dir / d;
                        RaycastHit hit;
                        float maxDist = d + probeRayDistance;
                        // use groundLayerMask for general surface detection
                        if (Physics.Raycast(origin.position, dirNorm, out hit, maxDist, groundLayerMask))
                        {
                            sumGeneralNormals += hit.normal;
                            sumGeneralPoints += hit.point;
                            generalHitCount++;
                        }
                    }
                }

                Vector3 normalToUse = avgNormal;
                Vector3 avgGeneralPoint = Vector3.zero;
                if (generalHitCount > 0)
                {
                    Vector3 avgGeneralNormal = (sumGeneralNormals / generalHitCount).normalized;
                    avgGeneralPoint = sumGeneralPoints / generalHitCount;
                    // if the averaged general normal is steep, prefer it (wall)
                    if (Mathf.Abs(avgGeneralNormal.y) < climbNormalYThreshold)
                    {
                        normalToUse = avgGeneralNormal;
                        // position body offset from the wall surface
                        float desiredYWall = avgGeneralPoint.y + Vector3.Dot(normalToUse, Vector3.up) * bodyHeightOffset;
                        // compute desired position on plane using average point projected along normal
                        Vector3 desiredOnPlane = avgGeneralPoint + normalToUse * bodyHeightOffset;
                        // directly set vertical to desiredOnPlane.y (will be smoothed below)
                        Vector3 cur = transform.position;
                        //transform.position = new Vector3(cur.x, Mathf.SmoothDamp(cur.y, desiredOnPlane.y, ref bodyVerticalVelocity, bodyHeightSmoothTime), cur.z);//////////////////////////
                    }
                    else if (useAnchorNormals)
                    {
                        // fallback: average anchor up vectors (if provided), otherwise keep avgNormal
                        Transform[] source = (anchorNormals != null && anchorNormals.Length > 0) ? anchorNormals : groundProbes;
                        if (source != null && source.Length > 0)
                        {
                            Vector3 sumN = Vector3.zero;
                            int ncount = 0;
                            for (int i = 0; i < source.Length; i++)
                            {
                                if (source[i] == null) continue;
                                sumN += source[i].up;
                                ncount++;
                            }
                            if (ncount > 0)
                            {
                                normalToUse = (sumN / ncount).normalized;
                            }
                        }
                    }
                }

                // compute leveling rotation preserving forward direction projected onto plane defined by normalToUse
                Vector3 forwardProj = Vector3.ProjectOnPlane(transform.forward, normalToUse);
                if (forwardProj.sqrMagnitude < 0.0001f) forwardProj = Vector3.ProjectOnPlane(transform.up, normalToUse);
                Quaternion targetRot = Quaternion.LookRotation(forwardProj.normalized, normalToUse);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-bodyRotationSmoothSpeed * Time.deltaTime));
            }
        }

        // Move aim points to keep their body-relative offsets (smoothly)
        if (aimPoints != null && aimLocalOffsets != null)
        {
            for (int i = 0; i < aimPoints.Length; i++)
            {
                if (aimPoints[i] == null) continue;
                Vector3 desiredWorld = transform.TransformPoint(aimLocalOffsets[i]);
                aimPoints[i].position = Vector3.SmoothDamp(aimPoints[i].position, desiredWorld, ref aimVelocities[i], aimPointSmoothTime);
            }
        }
    }

    // Capture current aim positions as offsets relative to the body. Use this after placing aim points in the scene.
    [ContextMenu("Capture Aim Offsets (store current aimPoints local offsets)")]
    void CaptureAimOffsets()
    {
        if (aimPoints == null) return;
        if (aimLocalOffsets == null || aimLocalOffsets.Length != aimPoints.Length)
        {
            aimLocalOffsets = new Vector3[aimPoints.Length];
            aimVelocities = new Vector3[aimPoints.Length];
        }
        for (int i = 0; i < aimPoints.Length; i++)
        {
            if (aimPoints[i] == null) continue;
            aimLocalOffsets[i] = transform.InverseTransformPoint(aimPoints[i].position);
            aimVelocities[i] = Vector3.zero;
        }
        Debug.Log($"Captured {aimPoints.Length} aim offsets.");
    }
}
