using UnityEngine;
public class NodeIK : MonoBehaviour
{
    [SerializeField] TNode shoulder, elbow;
    [SerializeField] float r1, r2;
    [SerializeField] Transform beginTran, endTran;

    void Update()
    {
        //if (end.position != storedEndPos)
        //{
            doIK();
            //storedEndPos = end.position;
        //}
    }
    
    public void doIK()
    {
        //Initial variables
        float d = Vector3.Distance(beginTran.position, endTran.position);

        //Move shoulder to position
        shoulder.SetWorldPosition(beginTran.position);

        //Check for endpoint out of range
        if(d >= r1 + r2)
        {
            shoulder.LookAt(endTran.position);
            elbow.SetWorldPosition(beginTran.position + (shoulder.GetForward() * r1));
            elbow.LookAt(endTran.position);
            return;
        }

        //Find 2D rotation values
        float fracNum = (r2 * r2) - (d * d) - (r1 * r1);
        float fracDen = -2f * d * r1;
        float theta = Mathf.Acos(fracNum / fracDen);

        //Perform 2D rotation
        shoulder.LookAt(endTran.position);
        //Vector3.up is the pole vector here
        Vector3 axis1 = Vector3.Cross((endTran.position - beginTran.position).normalized, Vector3.up).normalized;
        //MUST rotate in world space, otherwise LookAt messes everything up since the calculations are done before that (and other things?)
        Quaternion q = Quaternion.AngleAxis(Mathf.Abs(Mathf.Rad2Deg * theta), axis1);

        shoulder.RotateWorld(q);

        elbow.SetWorldPosition(beginTran.position + (shoulder.GetForward() * r1));
        elbow.LookAt(endTran.position);
    }
}