using System;

namespace AfterNow.AnPrez.SDK.Internal.Views
{
    [Serializable]
    public class ItemTransform
    {
        public PrezVector3 position;
        public PrezQuaternion rotation;
        public PrezVector3 localScale;

        public ItemTransform()
        {
            this.position = new PrezVector3(0f);
            this.rotation = new PrezQuaternion(0, 1, 0, 0);
            this.localScale = new PrezVector3(1f);
        }

        public void ResetTransform()
        {
            this.position = new PrezVector3(0f);
            this.rotation = new PrezQuaternion(0, 1, 0, 0);
            this.localScale = new PrezVector3(1f);
        }
    }

    [Serializable]
    public struct PrezVector3
    {
        public float x;
        public float y;
        public float z;

        public PrezVector3(float oneValue)
        {
            x = oneValue;
            y = oneValue;
            z = oneValue;
        }
    }

    [Serializable]
    public struct PrezQuaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public PrezQuaternion(float x,float y,float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
    }
}
