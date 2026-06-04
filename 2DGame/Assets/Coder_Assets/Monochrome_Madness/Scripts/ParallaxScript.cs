using UnityEngine;


namespace Coder_Assets.Monochrome_Madness

{
    public class ParallaxScript : MonoBehaviour
    {
        [System.Serializable]
        public class ParallaxLayer
        {
            public Transform layerTransform;
            public float xParallaxFactor = 0.5f;
            public float yParallaxFactor = 0.0f;
            [HideInInspector] public Vector3 initialOffset;
        }

        public Transform cameraTarget;
        public ParallaxLayer[] layers;

        private Vector3 initialCameraPosition;

        void Start()
        {
            if (cameraTarget == null)
            {
                cameraTarget = Camera.main.transform;
            }

            initialCameraPosition = cameraTarget.position;

            foreach (var layer in layers)
            {
                if (layer.layerTransform != null)
                {
                    layer.initialOffset = layer.layerTransform.position - cameraTarget.position;
                }
            }
        }

        void LateUpdate()
        {
            Vector3 cameraDelta = cameraTarget.position - initialCameraPosition;

            foreach (var layer in layers)
            {
                if (layer.layerTransform != null)
                {
                    Vector3 offset = new Vector3(
                        cameraDelta.x * layer.xParallaxFactor,
                        cameraDelta.y * layer.yParallaxFactor,
                        0f
                    );

                    layer.layerTransform.position = initialCameraPosition + layer.initialOffset + offset;
                }
            }
        }
    }

}