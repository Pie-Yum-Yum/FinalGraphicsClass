using UnityEngine;

// Moves the spider body using WASD and keeps aim points in-sync (smoothly) with the body.
// Attach this script to the spider `body` GameObject.
public class SpiderController : MonoBehaviour
{
    [Tooltip("Aim point transforms (one per leg). These will be moved smoothly to maintain their offsets from the body.")]
    public Transform[] aimPoints;

    [Header("Movement")]
    public float moveSpeed = 2f;
    [Tooltip("How quickly the body reaches the target position")]
    public float positionSmoothTime = 0.12f;
    [Tooltip("Rotation speed (deg/sec) when turning toward movement direction")]
    public float rotationSpeed = 720f;

    [Header("Aim points smoothing")]
    [Tooltip("How quickly aim points follow updated body position")]
    public float aimPointSmoothTime = 0.08f;

    Vector3 bodyVelocity = Vector3.zero;
    Vector3[] aimVelocities;
    Vector3[] aimLocalOffsets;

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

    void Update()
    {
        // Read WASD / arrow keys using the old input axes (works in most projects)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0f, v);
        if (input.sqrMagnitude > 1f) input.Normalize();

        // desired movement in world space (body-forward movement)
        Vector3 desiredMove = transform.TransformDirection(input) * moveSpeed;
        Vector3 targetPos = transform.position + desiredMove * Time.deltaTime;

        // Smoothly move the body
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref bodyVelocity, positionSmoothTime);

        // Rotate body to face movement direction when there is input
        if (input.sqrMagnitude > 0.001f)
        {
            Vector3 forwardDir = transform.TransformDirection(input).normalized;
            Quaternion targetRot = Quaternion.LookRotation(forwardDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
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
