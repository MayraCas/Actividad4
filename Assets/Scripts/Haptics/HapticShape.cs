// HapticShape.cs -- Marca un objeto como cuerpo solido que se puede tocar.
//
// Sustituye al HapticSolid de la Actividad 3, que solo sabia de esferas. Aqui
// hay tres tipos, porque los tres ambientes los necesitan:
//
//   Esfera       las piezas del banco de trabajo, la bola del laberinto
//   Caja         los muros del laberinto, la mesa, los receptaculos
//   Semiespacio  el suelo y las paredes del recinto: un plano infinito
//
// El servo loop trata cualquiera de los tres igual: penetracion -> normal ->
// F = k*penetracion - b*v_normal, mas friccion tangencial. La normal de
// contacto se memoriza dentro del C++ para que empujar fuerte no invierta el
// signo de la fuerza.
using UnityEngine;

namespace HapticOmni
{
    public enum TipoForma
    {
        Esfera      = OmniShapeType.Sphere,
        Caja        = OmniShapeType.Box,
        Semiespacio = OmniShapeType.HalfSpace,
    }

    [AddComponentMenu("Haptics/Haptic Shape (solido tocable)")]
    public class HapticShape : MonoBehaviour
    {
        public TipoForma tipo = TipoForma.Caja;

        [Tooltip("Rigidez del contacto en N/mm. Valores altos = superficie mas dura. " +
                 "El puente satura en 0.25 N/mm: por encima el lazo a 1 kHz entra en " +
                 "ciclo limite y el equipo vibra en la mano.")]
        [Range(0.01f, 0.25f)] public float rigidez = 0.15f;

        [Tooltip("Friccion tangencial de Coulomb. 0 = superficie lisa (hielo); " +
                 "0.6 = aspera. Se aplica como fuerza opuesta al deslizamiento.")]
        [Range(0f, 1f)] public float friccion = 0.15f;

        [Tooltip("Radio en unidades de Unity para el tipo Esfera. Si es 0 se deduce " +
                 "de la escala del objeto.")]
        public float radioOverride = 0f;

        [Tooltip("Semilados en unidades de Unity para el tipo Caja. Si es cero se " +
                 "deducen de la escala del objeto (cubo primitivo).")]
        public Vector3 semiladosOverride = Vector3.zero;

        [Tooltip("Normal del lado libre para el tipo Semiespacio, en espacio local.")]
        public Vector3 normalLocal = Vector3.up;

        [Tooltip("Color al que cambia el objeto mientras se le esta tocando. " +
                 "Alfa 0 desactiva el resaltado.")]
        public Color colorContacto = new Color(1f, 0.85f, 0.3f);

        [Tooltip("Desactivalo en superficies grandes que siempre deben enviarse " +
                 "al dispositivo, como el suelo: se les da prioridad maxima.")]
        public bool prioritaria = false;

        /// <summary>True mientras el servo loop reporta contacto con esta forma.</summary>
        public bool EnContacto { get; internal set; }

        /// <summary>Identificador estable que el puente usa para no mezclar contactos.</summary>
        public int Id { get; private set; }

        private Renderer rend;
        private Color colorOriginal;
        private MaterialPropertyBlock bloque;
        private bool contactoPrevio;

        /// <summary>Radio efectivo en unidades de mundo (tipo Esfera).</summary>
        public float RadioMundo
        {
            get
            {
                if (radioOverride > 0f) return radioOverride;
                Vector3 e = transform.lossyScale;
                return 0.5f * Mathf.Max(Mathf.Abs(e.x), Mathf.Max(Mathf.Abs(e.y), Mathf.Abs(e.z)));
            }
        }

        /// <summary>Semilados efectivos en unidades de mundo (tipo Caja).</summary>
        public Vector3 SemiladosMundo
        {
            get
            {
                if (semiladosOverride != Vector3.zero) return semiladosOverride;
                Vector3 e = transform.lossyScale;
                return new Vector3(Mathf.Abs(e.x), Mathf.Abs(e.y), Mathf.Abs(e.z)) * 0.5f;
            }
        }

        /// <summary>Radio de la esfera que envuelve a la forma; sirve para ordenar
        /// por cercania cuando hay mas formas que ranuras en el dispositivo.</summary>
        public float RadioEnvolvente
        {
            get
            {
                switch (tipo)
                {
                    case TipoForma.Esfera: return RadioMundo;
                    case TipoForma.Caja:   return SemiladosMundo.magnitude;
                    default:               return 0f;   // el semiespacio es infinito
                }
            }
        }

        /// <summary>Distancia aproximada de un punto a la superficie. Negativa dentro.</summary>
        public float DistanciaA(Vector3 punto)
        {
            switch (tipo)
            {
                case TipoForma.Esfera:
                    return Vector3.Distance(punto, transform.position) - RadioMundo;

                case TipoForma.Caja:
                {
                    // Los ejes del transform son unitarios, asi que el producto punto
                    // da directamente la coordenada local en unidades de mundo; la
                    // escala ya esta contemplada en SemiladosMundo.
                    Vector3 h = SemiladosMundo;
                    Vector3 r = punto - transform.position;
                    Vector3 q = new Vector3(Vector3.Dot(r, transform.right),
                                            Vector3.Dot(r, transform.up),
                                            Vector3.Dot(r, transform.forward));
                    Vector3 d = new Vector3(Mathf.Abs(q.x) - h.x, Mathf.Abs(q.y) - h.y, Mathf.Abs(q.z) - h.z);
                    Vector3 fuera = Vector3.Max(d, Vector3.zero);
                    float dentro = Mathf.Min(Mathf.Max(d.x, Mathf.Max(d.y, d.z)), 0f);
                    return fuera.magnitude + dentro;
                }

                default:
                {
                    Vector3 n = transform.TransformDirection(normalLocal).normalized;
                    return Vector3.Dot(punto - transform.position, n);
                }
            }
        }

        void OnEnable()
        {
            Id = GetInstanceID();
            HapticDevice.Register(this);
            rend = GetComponent<Renderer>();
            if (rend != null)
            {
                bloque = new MaterialPropertyBlock();
                colorOriginal = rend.sharedMaterial != null ? rend.sharedMaterial.color : Color.white;
            }
        }

        void OnDisable()
        {
            HapticDevice.Unregister(this);
            EnContacto = false;
        }

        void LateUpdate()
        {
            if (rend == null || colorContacto.a <= 0f) return;
            if (EnContacto == contactoPrevio) return;
            contactoPrevio = EnContacto;

            Color c = EnContacto ? colorContacto : colorOriginal;
            rend.GetPropertyBlock(bloque);
            bloque.SetColor("_BaseColor", c);   // URP
            bloque.SetColor("_Color",     c);   // built-in
            rend.SetPropertyBlock(bloque);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.6f);
            switch (tipo)
            {
                case TipoForma.Esfera:
                    Gizmos.DrawWireSphere(transform.position, RadioMundo);
                    break;
                case TipoForma.Caja:
                    Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, SemiladosMundo * 2f);
                    Gizmos.matrix = Matrix4x4.identity;
                    break;
                default:
                    Vector3 n = transform.TransformDirection(normalLocal).normalized;
                    Gizmos.DrawRay(transform.position, n * 0.5f);
                    Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.LookRotation(n), Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, new Vector3(3f, 3f, 0.001f));
                    Gizmos.matrix = Matrix4x4.identity;
                    break;
            }
        }
    }
}
