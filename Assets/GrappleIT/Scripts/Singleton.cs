using UnityEngine;

namespace Rhinox.XR.Grapple.It
{
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        [HideInInspector] public Singleton<T> Instance;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(this);
            }
        }
    }
}