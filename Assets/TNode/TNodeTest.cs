using UnityEngine;

public class TNodeTest : MonoBehaviour
{
    [SerializeField] TNode node1, node2;
    
    void Update()
    {
        node1.LookAt(node2.GetWorldPosition());
    }
}
