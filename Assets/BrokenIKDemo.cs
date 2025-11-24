using UnityEngine;

public class BrokenIKDemo : MonoBehaviour
{
    [SerializeField] Transform shoulder, bone1, elbow, bone2, end;
    Vector3 storedEndPos;

    void Update()
    {
        doIK();
        if (end.position != storedEndPos)
        {
            doIK();
            storedEndPos = end.position;
        }
    }

    /*
        1 - get desired angle between shoulderToTarget and shoulderToElbow (theta)
        2 - set shoulder to lookk at the endpoint
        3 - rotate it along the 
    */

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
        //PlaneNormal
        Vector3 axis1 = Vector3.Cross((end.position - shoulder.position).normalized, Vector3.up).normalized;

        /*
        The issue was that I was doing:
        shoulder.Rotate(axis1, Mathf.Abs(Mathf.Rad2Deg * theta));

        When I should have been doing the below.
        Essentially uses the angle to multiply by a plane-specific x and y axis
        The plane contains all parts of the limb
        ShoulderToTarget and planTangent(elbowToShoulder roughly?) are the axes
        
        The core, essential difference seems to be that axis1 is not always
        perpindicular to the actual desired axis of rotation



        NOTE:
        Normal of the plane defined by target and pole
        Vector3 planeNormal = Vector3.Cross(shoulderToTarget, shoulderToPole).normalized;
        Vector3 planeTangent = Vector3.Cross(planeNormal, shoulderToTarget).normalized;
        */
        Vector3 shoulderToTarget = (end.position - shoulder.position).normalized;
        Vector3 planeTangent = Vector3.Cross(axis1, end.position - shoulder.position).normalized;
        Vector3 elbowDir = shoulderToTarget * Mathf.Cos(theta) + planeTangent * Mathf.Sin(theta);

        Vector3 axis2 = Vector3.Cross((end.position - shoulder.position).normalized, Vector3.Cross(axis1, (end.position - shoulder.position).normalized).normalized).normalized;

        //shoulder.rotation = Quaternion.LookRotation(elbowDir, axis1);
        shoulder.Rotate(axis2, Mathf.Abs(Mathf.Rad2Deg * theta));

        elbow.transform.position = shoulder.transform.position + (shoulder.forward * r1);
        elbow.LookAt(end);

        Debug.DrawLine(shoulder.position, shoulder.position + (axis1 * 4));
    }
}