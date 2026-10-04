// HapticCursor.cs -- Representa la punta del stylus en la escena.
// Ejercicio 1: el movimiento de la punta del dispositivo se refleja en la
// interfaz grafica. Ademas dibuja la estela del recorrido y un vector que
// muestra la fuerza que el dispositivo esta devolviendo a la mano.
using UnityEngine;

namespace HapticOmni
{
    [AddComponentMenu("Haptics/Haptic Cursor")]
    public class HapticCursor : MonoBehaviour
    {
        [Tooltip("Copiar tambien la orientacion del stylus.")]
        public bool seguirRotacion = false;

        [Header("Estela del recorrido")]
        public TrailRenderer estela;
        public bool estelaActiva = true;

        [Header("Vector de fuerza")]
        [Tooltip("Linea que dibuja la fuerza aplicada; 1 N equivale a esta longitud.")]
        public LineRenderer flechaFuerza;
        public float unidadesPorNewton = 0.35f;

        [Header("Color segun la fuerza")]
        public Color colorLibre   = new Color(0.35f, 0.85f, 1f);
        public Color colorContacto = new Color(1f, 0.35f, 0.3f);

        private Renderer rend;
        private MaterialPropertyBlock bloque;

        void Awake()
        {
            rend   = GetComponent<Renderer>();
            bloque = new MaterialPropertyBlock();
        }

        void LateUpdate()
        {
            var dev = HapticDevice.Instance;
            if (dev == null) return;

            transform.position = dev.WorldPosition;
            if (seguirRotacion) transform.rotation = dev.WorldRotation;

            float magnitud = dev.ForceN.magnitude;

            if (rend != null)
            {
                Color c = Color.Lerp(colorLibre, colorContacto, Mathf.Clamp01(magnitud / 2f));
                rend.GetPropertyBlock(bloque);
                bloque.SetColor("_BaseColor", c);
                bloque.SetColor("_Color", c);
                bloque.SetColor("_EmissionColor", c * 0.6f);
                rend.SetPropertyBlock(bloque);
            }

            if (estela != null) estela.emitting = estelaActiva;

            if (flechaFuerza != null)
            {
                bool hayFuerza = magnitud > 0.02f;
                flechaFuerza.enabled = hayFuerza;
                if (hayFuerza)
                {
                    // La fuerza esta en el marco del dispositivo: se lleva al mundo
                    // igual que la posicion (Z invertida).
                    Vector3 f = new Vector3(dev.ForceN.x, dev.ForceN.y, -dev.ForceN.z);
                    Vector3 dir = dev.transform.TransformDirection(f);
                    flechaFuerza.positionCount = 2;
                    flechaFuerza.SetPosition(0, transform.position);
                    flechaFuerza.SetPosition(1, transform.position + dir * unidadesPorNewton);
                }
            }
        }
    }
}
