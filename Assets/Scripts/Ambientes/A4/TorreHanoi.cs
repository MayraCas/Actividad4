// TorreHanoi.cs -- Un poste, y la barrera que dice "aqui no".
//
// El poste es una caja haptica vertical: se puede tocar, y sirve de referencia
// tactil para localizar la torre sin mirar. La barrera es lo que hace que este
// ambiente sea haptico y no solo tridimensional: cuando el disco que llevas no
// cabe en esta torre, se enciende un semiespacio justo por encima de la cima y
// la mano topa con un techo invisible. El "no" se siente antes de intentar
// soltar, que es exactamente lo que un guante o un raton no pueden dar.
//
// La aceptacion no usa triggers de fisica: es una distancia horizontal al eje
// del poste, evaluada en el instante de soltar. El criterio es explicito y
// medible en vez de depender de si dos colliders se solapaban ese frame.
using UnityEngine;

namespace HapticOmni
{
    [AddComponentMenu("Haptics/A4 - Torre de Hanoi")]
    public class TorreHanoi : MonoBehaviour
    {
        [Tooltip("0, 1 o 2. El juego empieza en la 0 y se resuelve en la 2.")]
        public int indice = 0;

        [Tooltip("Radio horizontal dentro del cual se considera que el disco esta " +
                 "sobre esta torre, en unidades de Unity.")]
        public float radioAceptacion = 0.46f;

        [Tooltip("Y de la mesa: donde se apoya la cara inferior del primer disco.")]
        public float alturaBase = -1.02f;

        [Tooltip("Semiespacio que se enciende para rechazar un disco que no cabe.")]
        public HapticShape barrera;

        [Tooltip("Margen sobre la cima al que se coloca la barrera.")]
        public float margenBarrera = 0.06f;

        /// <summary>True si el punto cae dentro del cilindro de aceptacion.</summary>
        public bool Contiene(Vector3 p)
        {
            return DistanciaHorizontal(p) <= radioAceptacion;
        }

        /// <summary>Distancia horizontal de un punto al eje del poste.</summary>
        public float DistanciaHorizontal(Vector3 p)
        {
            Vector3 c = transform.position;
            return Vector2.Distance(new Vector2(p.x, p.z), new Vector2(c.x, c.z));
        }

        /// <summary>Centro del disco que ocupe el nivel dado (0 = el de abajo).</summary>
        public Vector3 PosicionDisco(int nivel)
        {
            Vector3 c = transform.position;
            float y = alturaBase + ConfiguracionHanoi.GrosorDisco * (nivel + 0.5f);
            return new Vector3(c.x, y, c.z);
        }

        /// <summary>Enciende o apaga el techo de rechazo. `altura` es el numero de
        /// discos que ya hay en la torre.</summary>
        public void MostrarBarrera(bool visible, int altura, float rigidez)
        {
            if (barrera == null) return;
            if (visible)
            {
                Vector3 c = transform.position;
                float y = alturaBase + ConfiguracionHanoi.GrosorDisco * altura + margenBarrera;
                barrera.transform.position = new Vector3(c.x, y, c.z);
                barrera.rigidez = rigidez;
            }
            if (barrera.gameObject.activeSelf != visible)
                barrera.gameObject.SetActive(visible);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.4f, 0.85f, 1f, 0.5f);
            Vector3 c = new Vector3(transform.position.x, alturaBase + 0.01f, transform.position.z);
            const int n = 32;
            Vector3 prev = c + new Vector3(radioAceptacion, 0, 0);
            for (int i = 1; i <= n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                Vector3 p = c + new Vector3(Mathf.Cos(a) * radioAceptacion, 0,
                                            Mathf.Sin(a) * radioAceptacion);
                Gizmos.DrawLine(prev, p);
                prev = p;
            }
        }
    }
}
