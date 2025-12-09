using UnityEngine;

public class TNodeTest : MonoBehaviour
{
    [SerializeField] TNode node1, node2;
    
    void Update()
    {
        Quaternion q = Quaternion.AngleAxis(100f * Time.deltaTime, node1.GetUp());
        node1.RotateWorld(q);
    }
}
