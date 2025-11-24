using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class TNode : MonoBehaviour
{
    public TNode parent;
    public List<TNode> children = new List<TNode>();
    Matrix4x4 m = Matrix4x4.identity;

    [SerializeField] Vector3 localPosition;
    [SerializeField] BoxCollider col;

    void Update()
    {
        SetLocalPosition();
        if(col) col.center = GetWorldPosition();

        if(GetComponent<Renderer>()) GetComponent<Renderer>().material.SetMatrix("MyTRSMatrix", GetWorldMatrix());
    }

    public Matrix4x4 GetWorldMatrix()
    {
        if(parent != null)
        {
            return parent.GetWorldMatrix() * m;
        }
        else
        {
            return m;
        }
    }

    public Vector3 GetRight()
    {
        return ((Vector3)(GetWorldMatrix().GetColumn(0))).normalized;
    }

    public Vector3 GetUp()
    {
        return ((Vector3)(GetWorldMatrix().GetColumn(1))).normalized;
    }

    public Vector3 GetForward()
    {
        return ((Vector3)(GetWorldMatrix().GetColumn(2))).normalized;
    }

    public Vector3 GetLocalPosition()
    {
        return localPosition;
    }

    void SetLocalPosition()
    {
        m.m03 = localPosition.x;
        m.m13 = localPosition.y;
        m.m23 = localPosition.z;
    }

    public Vector3 GetWorldPosition()
    {
        Matrix4x4 finalM = GetWorldMatrix();
        return finalM.GetColumn(3);
    }

    public void SetWorldPosition(Vector3 worldPosition)
    {
        Vector3 current = GetWorldPosition();
        Translate(worldPosition - current);
    }

    public void Translate(Vector3 translation)
    {
        localPosition.x += translation.x;
        localPosition.y += translation.y;
        localPosition.z += translation.z;
        SetLocalPosition();
    }

    public void Rotate(Quaternion q)
    {
        m *= Matrix4x4.Rotate(q);
    }

    public Quaternion GetRotation()
    {
        return Quaternion.LookRotation(GetForward(), GetUp());
    }
}
