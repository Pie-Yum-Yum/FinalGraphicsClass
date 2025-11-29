using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class TNode : MonoBehaviour
{
    public TNode parent;
    //public List<TNode> children = new List<TNode>();
    Quaternion localR;

    [SerializeField] Vector3 localT, localS =  Vector3.one;
    [SerializeField] BoxCollider col;

    void Update()
    {
        if(col) col.center = GetWorldPosition();

        if(GetComponent<Renderer>())
        {
            GetComponent<Renderer>().material.SetMatrix("MyTRSMatrix", GetWorldMatrix());
        }
    }

    public Matrix4x4 GetWorldMatrix()
    {
        if(parent != null)
        {
            return parent.GetWorldMatrix() * GetLocalMatrix();
        }
        else
        {
            return GetLocalMatrix();
        }
    }

    Matrix4x4 GetLocalMatrix()
    {
        return Matrix4x4.Translate(localT) * Matrix4x4.Rotate(localR) * Matrix4x4.Scale(localS);
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
        return localT;
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
        localT.x += translation.x;
        localT.y += translation.y;
        localT.z += translation.z;
    }

    public void SetRotation(Quaternion q)
    {
        localR = q;

    }

    public void RotateLocal(Quaternion q)
    {
        localR = localR * q;
    }

    public void RotateWorld(Quaternion q)
    {
        localR = q * localR;
    }

    public Quaternion GetRotation()
    {
        return Quaternion.LookRotation(GetForward(), GetUp());
    }

    public void LookAt(Vector3 position)
    {
        Vector3 forward = position - GetWorldPosition();
        Quaternion q = Quaternion.LookRotation(forward, Vector3.up);
        SetRotation(q);
    }
}
