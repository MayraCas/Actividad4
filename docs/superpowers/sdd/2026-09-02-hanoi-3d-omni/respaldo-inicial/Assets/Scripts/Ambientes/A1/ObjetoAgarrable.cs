// ObjetoAgarrable.cs -- Una pieza que se puede tomar, mover y soltar.
//
// La pieza tiene tres vidas distintas segun su estado:
//
//   Suelta    la mueve la fisica de Unity (Rigidbody). Si se suelta en el aire,
//             cae sobre la mesa: soltar tiene consecuencias.
//   Agarrada  es cinematica y sigue a la punta con un resorte amortiguado cuyo
//             retardo crece con la masa. Ese retardo es la parte visual de la
//             inercia; la parte que se siente en la mano la pone el puente con
//             la fuerza constante del peso.
//   Colocada  queda fija en su receptaculo y ya no se puede tomar.
//
// Mientras esta agarrada se le apaga su HapticShape. Si no, el servo loop
// intentaria expulsar la punta del interior de la pieza justo cuando el usuario
// la esta sujetando, y la mano sentiria dos fuerzas peleandose.
using UnityEngine;

namespace HapticOmni
{
    [AddComponentMenu("Haptics/A1 - Objeto agarrable")]
    [RequireComponent(typeof(Rigidbody))]
    public class ObjetoAgarrable : MonoBehaviour
    {
        [Header("Identidad")]
        public string nombrePieza = "Pieza";
        [Tooltip("Receptaculo que le corresponde a esta pieza.")]
        public int idDestino = 0;

        [Header("Fisica")]
        [Tooltip("Masa en kg. El peso que siente la mano es m*g, saturado a 3 N por " +
                 "el puente: 0.05 kg se sienten como una pluma y 0.28 kg llegan casi " +
                 "al maximo que el Touch puede entregar.")]
        [Range(0.02f, 0.32f)] public float masaKg = 0.15f;

        [Tooltip("Retardo del seguimiento cuando esta agarrada, en segundos por kg. " +
                 "Es lo que hace que una pieza pesada se vea 'costar' mas.")]
        public float retardoPorKg = 0.35f;

        // ---- estado ----------------------------------------------------------
        public bool Agarrada { get; private set; }
        public bool Colocada { get; private set; }
        public int  Agarres  { get; private set; }
        public int  Caidas   { get; private set; }
        /// <summary>Segundos que la pieza ha estado en la mano.</summary>
        public float TiempoEnMano { get; private set; }
        /// <summary>Distancia al centro del receptaculo en el momento de soltarla.</summary>
        public float ErrorColocacion { get; private set; }

        /// <summary>Peso en newtons: lo que el puente aplica como fuerza constante.</summary>
        public float PesoN { get { return masaKg * 9.81f; } }

        private HapticShape forma;
        private Rigidbody cuerpo;
        private Vector3 posicionInicial;
        private Quaternion rotacionInicial;
        private Vector3 desfaseAgarre;
        private Vector3 velSuavizado;

        void Awake()
        {
            forma  = GetComponent<HapticShape>();
            cuerpo = GetComponent<Rigidbody>();
            cuerpo.mass = masaKg;
            posicionInicial = transform.position;
            rotacionInicial = transform.rotation;
        }

        /// <summary>Devuelve la pieza a su sitio de salida y borra su historial.</summary>
        public void Reiniciar()
        {
            Agarrada = false;
            Colocada = false;
            Agarres  = 0;
            Caidas   = 0;
            TiempoEnMano = 0f;
            ErrorColocacion = 0f;
            velSuavizado = Vector3.zero;

            transform.SetPositionAndRotation(posicionInicial, rotacionInicial);
            cuerpo.isKinematic = false;
            cuerpo.linearVelocity = Vector3.zero;
            cuerpo.angularVelocity = Vector3.zero;
            cuerpo.mass = masaKg;
            if (forma != null) forma.enabled = true;
        }

        /// <summary>Distancia de la punta a la superficie de la pieza.</summary>
        public float DistanciaA(Vector3 punta)
        {
            return forma != null ? forma.DistanciaA(punta)
                                 : Vector3.Distance(punta, transform.position);
        }

        public void Agarrar(Vector3 puntaMundo)
        {
            if (Colocada || Agarrada) return;
            Agarrada = true;
            Agarres++;
            desfaseAgarre = transform.position - puntaMundo;
            velSuavizado  = Vector3.zero;
            cuerpo.isKinematic = true;
            if (forma != null) forma.enabled = false;   // que no pelee con la mano
        }

        /// <summary>Suelta la pieza. Si va a un receptaculo se fija ahi; si no, cae.</summary>
        public void Soltar(ZonaDestino zona)
        {
            if (!Agarrada) return;
            Agarrada = false;
            if (forma != null) forma.enabled = true;

            if (zona != null && zona.id == idDestino)
            {
                ErrorColocacion = Vector3.Distance(
                    new Vector3(transform.position.x, 0f, transform.position.z),
                    new Vector3(zona.transform.position.x, 0f, zona.transform.position.z));
                Colocada = true;
                cuerpo.isKinematic = true;
                transform.position = zona.PuntoDeApoyo(transform.localScale.y * 0.5f);
                zona.Ocupante = this;
            }
            else
            {
                Caidas++;
                cuerpo.isKinematic = false;
                cuerpo.linearVelocity = Vector3.zero;
                cuerpo.angularVelocity = Vector3.zero;
            }
        }

        /// <summary>Seguimiento de la punta mientras esta en la mano.</summary>
        public void SeguirPunta(Vector3 puntaMundo, float dt)
        {
            if (!Agarrada) return;
            TiempoEnMano += dt;
            float retardo = Mathf.Max(0.01f, masaKg * retardoPorKg);
            transform.position = Vector3.SmoothDamp(transform.position,
                                                    puntaMundo + desfaseAgarre,
                                                    ref velSuavizado, retardo);
        }

        /// <summary>True si la pieza se salio de la mesa (se considera caida grave).</summary>
        public bool SeCayoFuera(float alturaMesa)
        {
            return !Colocada && !Agarrada && transform.position.y < alturaMesa - 1.5f;
        }

        public void DevolverAlOrigen()
        {
            transform.SetPositionAndRotation(posicionInicial, rotacionInicial);
            cuerpo.linearVelocity = Vector3.zero;
            cuerpo.angularVelocity = Vector3.zero;
        }
    }
}
