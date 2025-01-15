using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class SkyController : MonoBehaviour
    {
        [SerializeField] private Material normalSky;
        [SerializeField] private Material bloodySky;

        public void SetNormalSky()
        {
            RenderSettings.skybox = normalSky;
        }

        public void SetBloodySky()
        {
            RenderSettings.skybox = bloodySky;
        }
    }
}