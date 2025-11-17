using UnityEngine;

public class IKSolver : MonoBehaviour
{
    [SerializeField] Transform shoulder, bone1, elbow, bone2, end;
    Vector3 storedEndPos;

    void Update()
    {
        if (end.position != storedEndPos)
        {
            doIK();
            storedEndPos = end.position;
        }
    }

    public void doIK()
    {
        //Initial variables
        float d = Vector3.Distance(shoulder.position, end.position);
        float r1 = bone1.localScale.z, r2 = bone2.localScale.z;

        //Check for endpoint out of range
        if(d >= r1 + r2)
        {
            shoulder.LookAt(end);
            elbow.transform.position = shoulder.transform.position + (shoulder.forward * r1);
            elbow.LookAt(end);
            return;
        }

        //Find 2D rotation values
        float fracNum = (r2 * r2) - (d * d) - (r1 * r1);
        float fracDen = -2f * d * r1;
        float theta = Mathf.Acos(fracNum / fracDen);

        //Perform 2D rotation
        shoulder.LookAt(end);
        Vector3 axis1 = Vector3.Cross((end.position - shoulder.position).normalized, shoulder.right).normalized;
        shoulder.Rotate(axis1, Mathf.Abs(Mathf.Rad2Deg * theta));

        elbow.transform.position = shoulder.transform.position + (shoulder.forward * r1);
        elbow.LookAt(end);
    }
}
