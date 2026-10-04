// HapticAttractor.cs -- Punto que atrae la punta del stylus.
// Es un resorte lineal: F = k * (destino - punta), activo solo dentro del
// radio indicado. Es la base de las guias hapticas que se usan, por ejemplo,
// en entrenadores quirurgicos para conducir la mano del alumno.
using UnityEngine;

namespace HapticOmni
{
    [AddComponentMenu("Haptics/Haptic Attractor (pozo de gravedad)")]
    public class HapticAttractor : MonoBehaviour
    {
        [Tooltip("Rigidez del resorte en N/mm.")]
        [Range(0.005f, 0.15f)] public float stiffness = 0.05f;

        [Tooltip("Radio de accion en unidades de Unity. Fuera de el no se siente nada.")]
        public float radius = 1.5f;

        void OnEnable()  { HapticDevice.Register(this); }
        void OnDisable() { HapticDevice.Unregister(this); }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.4f, 1f, 0.6f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, radius);
            Gizmos.color = new Color(0.4f, 1f, 0.6f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, 0.06f);
        }
    }
}
