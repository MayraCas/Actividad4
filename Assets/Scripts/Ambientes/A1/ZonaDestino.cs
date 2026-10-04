// ZonaDestino.cs -- Receptaculo donde debe acabar cada pieza del ambiente 1.
//
// No usa triggers de fisica a proposito: la comprobacion es una distancia
// horizontal contra el centro, evaluada solo en el instante de soltar. Asi el
// criterio de "colocada" es explicito y medible -- la distancia al centro es
// justamente el error de colocacion que se registra en el CSV -- en vez de
// depender de si dos colliders se estaban solapando ese frame.
using UnityEngine;

namespace HapticOmni
{
    [AddComponentMenu("Haptics/A1 - Zona destino")]
    public class ZonaDestino : MonoBehaviour
    {
        [Tooltip("Debe coincidir con el idDestino de la pieza que le corresponde.")]
        public int id = 0;

        [Tooltip("Radio de aceptacion en unidades de Unity, medido en horizontal.")]
        public float radio = 0.26f;

        [Tooltip("Margen vertical por encima y por debajo del borde del receptaculo.")]
        public float margenVertical = 0.45f;

        public Color color = new Color(0.4f, 0.85f, 1f);

        /// <summary>Pieza que ya quedo colocada aqui.</summary>
        public ObjetoAgarrable Ocupante { get; set; }

        public bool Libre { get { return Ocupante == null; } }

        /// <summary>True si el punto cae dentro del volumen de aceptacion.</summary>
        public bool Contiene(Vector3 p)
        {
            Vector3 c = transform.position;
            float dh = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(c.x, c.z));
            return dh <= radio && Mathf.Abs(p.y - c.y) <= margenVertical;
        }

        /// <summary>Donde queda apoyada una pieza de media altura dada.</summary>
        public Vector3 PuntoDeApoyo(float mediaAltura)
        {
            return transform.position + Vector3.up * mediaAltura;
        }

        public void Reiniciar() { Ocupante = null; }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(color.r, color.g, color.b, 0.6f);
            Vector3 c = transform.position;
            const int n = 32;
            Vector3 prev = c + new Vector3(radio, 0, 0);
            for (int i = 1; i <= n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                Vector3 p = c + new Vector3(Mathf.Cos(a) * radio, 0, Mathf.Sin(a) * radio);
                Gizmos.DrawLine(prev, p);
                prev = p;
            }
        }
    }
}
