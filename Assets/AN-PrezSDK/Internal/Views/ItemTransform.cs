using System;

[Serializable]
public class ItemTransform
{
    public PrezVector3 position;
    public PrezQuaternion rotation;
    public PrezVector3 localScale;

    public ItemTransform()
    {
        //this.position = new PrezVector3(0f);
        this.position = new PrezVector3(0,1.75f,0);
        this.rotation = new PrezQuaternion(0, 1, 0, 0);
        this.localScale = new PrezVector3(1, 1, 1);
    }

    public void ResetTransform()
    {
        //        this.position = new PrezVector3(0f);
        this.position = new PrezVector3(0, 1.75f, 0);
        this.rotation = new PrezQuaternion(0, 1, 0, 0);
        this.localScale = new PrezVector3(1, 1, 1);
    }
}

[Serializable]
public struct PrezVector3
{
    public float x;
    public float y;
    public float z;

    public PrezVector3(float _x, float _y, float _z)
    {
        x = _x;
        y = _y;
        z = _z;
    }
}

[Serializable]
public struct PrezQuaternion
{
    public float x;
    public float y;
    public float z;
    public float w;

    public PrezQuaternion(float x, float y, float z, float w)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }
}
