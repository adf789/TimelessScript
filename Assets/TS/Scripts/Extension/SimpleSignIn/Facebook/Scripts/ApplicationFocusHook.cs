using System;
using UnityEngine;

namespace Assets.SimpleSignIn.Facebook.Scripts
{
    public class ApplicationFocusHook : MonoBehaviour
    {
        private static ApplicationFocusHook _instance;
        private Action _callback;

        public static void Create(Action callback)
        {
            if (_instance == null)
            {
                _instance = new GameObject(nameof(ApplicationFocusHook)).AddComponent<ApplicationFocusHook>();
            }

            _instance._callback = callback;
        }

        public void OnApplicationFocus(bool focus)
        {
            if (focus && _callback != null)
            {
                _callback();
                _callback = null;
            }
        }
    }
}