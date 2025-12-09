using UnityEngine;

public class TNodeFollow : MonoBehaviour
{
    [SerializeField] TNode toFollow;
    [SerializeField] Vector3 positionOffset;
    [SerializeField] Vector3 rotationOffsetEuler;

    void Update()
    {
        Quaternion rotationOffset = Quaternion.Euler(rotationOffsetEuler);
        transform.position = toFollow.GetWorldPosition() + toFollow.GetRotation() * positionOffset;
        transform.rotation = toFollow.GetRotation() * rotationOffset;
    }
}
