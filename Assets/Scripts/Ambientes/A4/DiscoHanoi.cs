// DiscoHanoi.cs -- Un disco: la parte que se ve y se siente.
//
// El disco no sabe las reglas del juego; de eso se encarga EstadoHanoi. Aqui
// solo vive el ciclo fisico, que es el mismo que ya funciona en ObjetoAgarrable
// del ambiente 1:
//
//   En la torre  cinematico y quieto, con su HapticShape encendido: se puede tocar.
//   Agarrado     sigue a la punta con un resorte amortiguado cuyo retardo crece
//                con la masa. Se le apaga la forma haptica, porque si no el servo
//                loop intentaria expulsar la punta del interior del disco justo
//                cuando el usuario lo esta sujetando y la mano sentiria dos
//                fuerzas peleandose.
//   Cayendo      lo mueve la fisica de Unity. Soltar fuera de una torre tiene
//                consecuencia: el disco cae a la mesa, cuenta como caida y vuelve
//                a su torre al cabo de un momento.
using UnityEngine;

namespace HapticOmni
{
    [AddComponentMenu("Haptics/A4 - Disco de Hanoi")]
    [RequireComponent(typeof(Rigidbody))]
    public class DiscoHanoi : MonoBehaviour
    {
        [Header("Identidad")]
        [Tooltip("0 es el disco mas pequeno.")]
        public int indice = 0;

        [Header("Fisica")]
        [Tooltip("Masa en kg. El peso que siente la mano es m*g, saturado a 3 N por " +
                 "el puente.")]
        public float masaKg = 0.10f;
        [Tooltip("Radio en unidades de Unity. Manda en la regla del juego: un disco " +
                 "solo se apoya sobre otro de radio mayor.")]
        public float radio = 0.2f;
        [Tooltip("Retardo del seguimiento cuando esta agarrado, en segundos por kg.")]
        public float retardoPorKg = 0.35f;
        [Tooltip("Segundos que tarda en volver a su torre despues de caerse.")]
        public float tiempoDeRegreso = 1.0f;

        public bool  Agarrado     { get; private set; }
        public bool  Cayendo      { get; private set; }
        public int   Agarres      { get; private set; }
        public int   Caidas       { get; private set; }
        public float TiempoEnMano { get; private set; }

        /// <summary>Peso en newtons: lo que el puente aplica como fuerza constante.</summary>
        public float PesoN { get { return masaKg * 9.81f; } }

        private HapticShape forma;
        private Rigidbody cuerpo;
        private Vector3 desfaseAgarre;
        private Vector3 velSuavizado;
        private float tCaida;

        void Awake()
        {
            forma  = GetComponent<HapticShape>();
            cuerpo = GetComponent<Rigidbody>();
            cuerpo.mass = masaKg;
        }

        /// <summary>Ajusta tamano, masa y aspecto haptico. La llama el ambiente cada
        /// vez que cambia el nivel: la escena crea siempre siete discos y el nivel
        /// decide cuantos se activan y como de grandes son.</summary>
        public void Configurar(int indice, float masaKg, float radio, float retardoPorKg,
                               float rigidez, float friccion)
        {
            this.indice = indice;
            this.masaKg = masaKg;
            this.radio = radio;
            this.retardoPorKg = retardoPorKg;

            // El cilindro primitivo de Unity mide 1 de diametro y 2 de alto.
            transform.localScale = new Vector3(radio * 2f,
                                               ConfiguracionHanoi.GrosorDisco * 0.5f,
                                               radio * 2f);

            if (cuerpo == null) cuerpo = GetComponent<Rigidbody>();
            cuerpo.mass = masaKg;

            if (forma == null) forma = GetComponent<HapticShape>();
            if (forma != null)
            {
                // No hay primitiva cilindro en el puente: una caja de semilados
                // (radio, medio grosor, radio) es la aproximacion util. La punta
                // toca las esquinas un poco antes que el borde real, y a este
                // tamano la diferencia no se percibe.
                forma.tipo = TipoForma.Caja;
                forma.semiladosOverride = new Vector3(radio, ConfiguracionHanoi.GrosorDisco * 0.5f, radio);
                forma.rigidez = rigidez;
                forma.friccion = friccion;
            }
        }

        public void Reiniciar()
        {
            Agarrado = false;
            Cayendo = false;
            Agarres = 0;
            Caidas = 0;
            TiempoEnMano = 0f;
            tCaida = 0f;
            velSuavizado = Vector3.zero;
            HacerQuieto();
        }

        private void HacerQuieto()
        {
            if (cuerpo == null) cuerpo = GetComponent<Rigidbody>();
            // El orden importa: poner a cero las velocidades de un cuerpo que ya es
            // cinematico es una operacion no soportada y Unity lo avisa por consola
            // en cada llamada. Se frena primero y se congela despues.
            if (!cuerpo.isKinematic)
            {
                cuerpo.linearVelocity = Vector3.zero;
                cuerpo.angularVelocity = Vector3.zero;
            }
            cuerpo.isKinematic = true;
            if (forma != null) forma.enabled = true;
        }

        /// <summary>Distancia de la punta a la superficie del disco.</summary>
        public float DistanciaA(Vector3 punta)
        {
            return forma != null ? forma.DistanciaA(punta)
                                 : Vector3.Distance(punta, transform.position) - radio;
        }

        public void Agarrar(Vector3 puntaMundo)
        {
            if (Agarrado) return;
            Agarrado = true;
            Cayendo = false;
            Agarres++;
            desfaseAgarre = transform.position - puntaMundo;
            velSuavizado = Vector3.zero;
            if (cuerpo == null) cuerpo = GetComponent<Rigidbody>();
            if (!cuerpo.isKinematic)
            {
                cuerpo.linearVelocity = Vector3.zero;
                cuerpo.angularVelocity = Vector3.zero;
            }
            cuerpo.isKinematic = true;
            if (forma != null) forma.enabled = false;   // que no pelee con la mano
        }

        public void SeguirPunta(Vector3 puntaMundo, float dt)
        {
            if (!Agarrado) return;
            TiempoEnMano += dt;
            float retardo = Mathf.Max(0.01f, masaKg * retardoPorKg);
            transform.position = Vector3.SmoothDamp(transform.position,
                                                    puntaMundo + desfaseAgarre,
                                                    ref velSuavizado, retardo);
        }

        /// <summary>Lo deja fijo en un sitio concreto: colocado en una torre, o
        /// devuelto a la suya tras un intento ilegal.</summary>
        public void PosarEn(Vector3 destino)
        {
            Agarrado = false;
            Cayendo = false;
            tCaida = 0f;
            transform.SetPositionAndRotation(destino, Quaternion.identity);
            HacerQuieto();
        }

        /// <summary>Soltado fuera de toda torre: se lo lleva la fisica.</summary>
        public void Caer()
        {
            Agarrado = false;
            Cayendo = true;
            Caidas++;
            tCaida = 0f;
            if (forma != null) forma.enabled = true;
            if (cuerpo == null) cuerpo = GetComponent<Rigidbody>();
            cuerpo.isKinematic = false;
            cuerpo.linearVelocity = Vector3.zero;
            cuerpo.angularVelocity = Vector3.zero;
        }

        /// <summary>Avanza el temporizador de la caida. Devuelve true cuando ya toca
        /// devolverlo a su torre.</summary>
        public bool ActualizarCaida(float dt)
        {
            if (!Cayendo) return false;
            tCaida += dt;
            return tCaida >= tiempoDeRegreso;
        }
    }
}
