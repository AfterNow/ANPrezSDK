using UnityEngine;

namespace AfterNow.PrezSDK
{
    internal class Rotate : MonoBehaviour
    {
        internal bool shouldUpdate;
        internal float speed;
        [HideInInspector]
        internal bool paused = false;
        const float speedFactor = 72f;
        // Update is called once per frame
        void Update()
        {
            if (shouldUpdate)
            {
                if (!paused)
                {
                    transform.Rotate(Vector3.up * speed * Time.deltaTime * speedFactor, Space.World);
                }
            }
        }
    }
}

