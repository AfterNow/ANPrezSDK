using UnityEngine;

namespace AfterNow.PrezSDK
{
    internal class Rotate : MonoBehaviour
    {
        public bool shouldUpdate;
        public float speed;
        [HideInInspector]
        public bool paused = false;
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