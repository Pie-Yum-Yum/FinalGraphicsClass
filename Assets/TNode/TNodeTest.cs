using UnityEngine;

public class TNodeTest : MonoBehaviour
{
    [SerializeField] bool rotate1, rotate2;
    [SerializeField] TNode node1, node2;
    [SerializeField] Vector3 axis;
    [SerializeField] float angleDeg;

    void Update()
    {
        if(rotate1)
        {
            rotate1 = false;
            Quaternion q = Quaternion.AngleAxis(angleDeg, axis);
            node1.Rotate(q);

            Debug.Log("FORWARD: " + node1.GetForward());
            Debug.Log("RIGHT: " + node1.GetRight());
            Debug.Log("UP: " + node1.GetUp());
        }
        if(rotate2)
        {
            rotate2 = false;
            Quaternion q = Quaternion.AngleAxis(angleDeg, axis);
            node2.Rotate(q);

            Debug.Log("FORWARD: " + node2.GetForward());
            Debug.Log("RIGHT: " + node2.GetRight());
            Debug.Log("UP: " + node2.GetUp());
        }
        
    }
}
