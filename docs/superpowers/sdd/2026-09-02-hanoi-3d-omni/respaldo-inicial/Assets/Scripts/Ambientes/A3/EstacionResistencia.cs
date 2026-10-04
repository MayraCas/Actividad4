// EstacionResistencia.cs -- Una corredera con un modelo de resistencia propio.
//
// Las cinco estaciones del ambiente 3 usan el mismo riel del puente nativo con
// parametros distintos, de forma que se pueden comparar entre si tocandolas una
// detras de otra. El modelo completo que evalua el servo loop es:
//
//     F(s, v) = k*s  +  b*v  +  friccion(v)          [N]
//
//   k  resorte de retorno   N/mm   -- cuesta mas cuanto mas lejos se lleva
//   b  viscosidad           N/(mm/s) -- cuesta mas cuanto mas rapido se mueve
//   Fs friccion estatica    N      -- hay que vencerla para que arranque
//   Fk friccion dinamica    N      -- lo que cuesta mantenerlo en movimiento
//
// Fs > Fk es lo que produce el stick-slip: el objeto se resiste, se suelta de
// golpe y luego cuesta menos. Es la sensacion de un cajon atascado, y es la
// unica de las cuatro que no se puede imitar con una animacion.
using UnityEngine;

namespace HapticOmni
{
    [AddComponentMenu("Haptics/A3 - Estacion de resistencia")]
    public class EstacionResistencia : MonoBehaviour
    {
        [Header("Identidad")]
        public string nombre = "Libre";
        [TextArea(2, 3)] public string descripcion = "Sin resistencia: referencia.";
        public Color color = Color.white;

        [Header("Riel")]
        [Tooltip("Objeto que se desliza; se coloca sobre el riel segun el avance.")]
        public Transform corredera;
        [Tooltip("Direccion del riel en espacio local.")]
        public Vector3 ejeLocal = Vector3.right;
        [Tooltip("Recorrido util en unidades de Unity.")]
        public float longitud = 1.4f;
        [Tooltip("Margen para dar la estacion por completada, en unidades.")]
        public float tolerancia = 0.06f;
        [Tooltip("Distancia a la que la punta puede enganchar la corredera.")]
        public float radioEnganche = 0.16f;

        [Header("Modelo de resistencia")]
        [Tooltip("Resorte de retorno, N/mm.")]
        [Range(0f, 0.25f)] public float k = 0f;
        [Tooltip("Viscosidad, N por cada mm/s.")]
        [Range(0f, 0.005f)] public float b = 0f;
        [Tooltip("Friccion estatica en N: hay que vencerla para que se mueva.")]
        [Range(0f, 3f)] public float Fs = 0f;
        [Tooltip("Friccion dinamica en N: lo que cuesta mientras desliza.")]
        [Range(0f, 3f)] public float Fk = 0f;

        // ---- estado y metricas -----------------------------------------------
        /// <summary>Avance de la corredera sobre el riel, en unidades de Unity.</summary>
        public float Avance { get; private set; }
        public bool  Completada { get; private set; }
        public bool  Enganchada { get; set; }

        public float TiempoActivo { get; private set; }
        public float FuerzaPico { get; private set; }
        /// <summary>Integral de |F| dt: el "esfuerzo" total, en N·s.</summary>
        public float Impulso { get; private set; }
        /// <summary>Integral de |F| ds: el trabajo mecanico, en N·mm.</summary>
        public float Trabajo { get; private set; }
        public float FuerzaMedia { get { return TiempoActivo > 1e-4f ? Impulso / TiempoActivo : 0f; } }
        /// <summary>Fuerza maxima registrada antes de que la corredera se moviera:
        /// es la medida experimental de la friccion estatica.</summary>
        public float FuerzaDeArranque { get; private set; }

        public Vector3 Origen { get { return transform.position; } }
        public Vector3 EjeMundo { get { return transform.TransformDirection(ejeLocal).normalized; } }
        public Vector3 PuntoFinal { get { return Origen + EjeMundo * longitud; } }
        public Vector3 PosicionCorredera { get { return Origen + EjeMundo * Avance; } }

        private bool arranco;

        void Awake() { Reiniciar(); }

        public void Reiniciar()
        {
            Avance = 0f;
            Completada = false;
            Enganchada = false;
            TiempoActivo = 0f;
            FuerzaPico = 0f;
            Impulso = 0f;
            Trabajo = 0f;
            FuerzaDeArranque = 0f;
            arranco = false;
            ColocarCorredera();
        }

        /// <summary>Proyeccion de un punto sobre el riel, en unidades de Unity.</summary>
        public float Proyectar(Vector3 punto)
        {
            return Vector3.Dot(punto - Origen, EjeMundo);
        }

        /// <summary>Distancia de un punto a la corredera.</summary>
        public float DistanciaAsidero(Vector3 punto)
        {
            return Vector3.Distance(punto, PosicionCorredera);
        }

        /// <summary>Resistencia teorica del modelo, en N. Se usa para las metricas
        /// cuando no hay hardware; con el equipo conectado se usa la que reporta
        /// el servo loop, que es la que realmente sintio la mano.</summary>
        public float FuerzaModelo(float avanceU, float velocidadU, float unidadesPorMm)
        {
            float s  = avanceU / Mathf.Max(1e-5f, unidadesPorMm);       // mm
            float v  = velocidadU / Mathf.Max(1e-5f, unidadesPorMm);    // mm/s
            float f  = k * s + b * Mathf.Abs(v);
            // Por debajo de 0.5 mm/s la corredera se considera adherida: la
            // resistencia es la estatica. Al despegarse cae a la dinamica.
            f += Mathf.Abs(v) > 0.5f ? Fk : Fs;
            return f;
        }

        /// <summary>Avanza el estado de la estacion durante un frame.</summary>
        public void Actualizar(float nuevoAvance, float fuerza, float dt)
        {
            float anterior = Avance;
            Avance = Mathf.Clamp(nuevoAvance, 0f, longitud);
            ColocarCorredera();

            if (!Enganchada) return;

            TiempoActivo += dt;
            float f = Mathf.Abs(fuerza);
            Impulso += f * dt;
            Trabajo += f * Mathf.Abs(Avance - anterior);
            if (f > FuerzaPico) FuerzaPico = f;

            if (!arranco)
            {
                if (f > FuerzaDeArranque) FuerzaDeArranque = f;
                if (Mathf.Abs(Avance - anterior) > 0.002f) arranco = true;
            }

            if (!Completada && Avance >= longitud - tolerancia) Completada = true;
        }

        private void ColocarCorredera()
        {
            if (corredera != null) corredera.position = PosicionCorredera;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(color.r, color.g, color.b, 0.9f);
            Gizmos.DrawLine(Origen, PuntoFinal);
            Gizmos.DrawWireSphere(Origen, 0.05f);
            Gizmos.DrawWireSphere(PuntoFinal, 0.05f);
        }
    }
}
