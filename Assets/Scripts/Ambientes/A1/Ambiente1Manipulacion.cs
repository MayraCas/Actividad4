// Ambiente1Manipulacion.cs -- "Banco de trabajo haptico".
//
// Ejercicio de la actividad: el usuario debe TOMAR, MOVER y SOLTAR objetos
// virtuales. Aqui hay cuatro piezas de masas muy distintas y cuatro
// receptaculos; cada pieza va al suyo.
//
// Que se siente en cada fase:
//
//   Tomar    la punta choca con la pieza (F = k*penetracion, mas friccion) y el
//            boton 1 la engancha.
//   Mover    el puente aplica una fuerza constante hacia abajo de m*g. Es una
//            fuerza que no depende de la posicion, asi que actualizarla a 60 Hz
//            no introduce ningun problema de estabilidad: lo que exige el
//            kilohercio son los contactos, no la gravedad. Se anade
//            amortiguamiento proporcional a la masa, que es la inercia.
//   Soltar   si la pieza esta sobre su receptaculo queda colocada; si no, cae a
//            la mesa y se cuenta como error.
//
// La guia haptica (tecla G) enciende un resorte de atraccion hacia el
// receptaculo correcto. Es el mismo principio que las guias de los entrenadores
// quirurgicos, y comparar el tiempo y la precision con guia y sin ella da una
// medida directa de para que sirve.
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HapticOmni
{
    [AddComponentMenu("Haptics/A1 - Ambiente de manipulacion")]
    public class Ambiente1Manipulacion : AmbienteBase
    {
        [Header("Escenario")]
        public ObjetoAgarrable[] piezas;
        public ZonaDestino[] destinos;
        public float alturaMesa = 0f;

        [Header("Agarre")]
        [Tooltip("Margen extra sobre la superficie de la pieza para poder engancharla.")]
        public float alcanceAgarre = 0.10f;
        [Tooltip("Con el boton 1 mantenido se sujeta; al soltarlo se suelta la pieza. " +
                 "Desactivalo para que el boton alterne entre tomar y soltar.")]
        public bool modoMantener = true;
        [Tooltip("Amortiguamiento de acarreo por kg, en N/(mm/s). Es la inercia que " +
                 "se siente al frenar una pieza pesada.")]
        public float amortiguamientoPorKg = 0.008f;

        [Header("Guia haptica (tecla G)")]
        public bool guiaActiva = false;
        public HapticAttractor guia;
        [Range(0.01f, 0.12f)] public float rigidezGuia = 0.03f;
        public float radioGuia = 2.5f;

        // ---- metricas --------------------------------------------------------
        private int totalAgarres, totalCaidas;
        private float impulso;          // integral de |F| dt, N*s
        private float fuerzaPico;
        private float recorrido;        // longitud del camino de la punta
        private Vector3 puntaPrevia;
        private bool primeraMuestra = true;

        private ObjetoAgarrable enMano;
        private ObjetoAgarrable candidata;

        public override string Nombre { get { return "AMBIENTE 1 · MANIPULACION"; } }
        protected override string PrefijoArchivos { get { return "a1"; } }

        protected override string CabeceraTraza
        {
            get { return "t,x,y,z,fx,fy,fz,fmag,agarrada,pieza,peso_n,guia"; }
        }
        protected override string CabeceraResumen
        {
            get
            {
                return CabeceraSujeto +
                       ",fecha,guia,tiempo_s,piezas,agarres,caidas,recorrido_u," +
                       "impulso_ns,fuerza_pico_n,err_medio_u,detalle_por_pieza";
            }
        }

        // ---------------------------------------------------------------------
        protected override void PrepararEscenario()
        {
            enMano = null;
            totalAgarres = totalCaidas = 0;
            impulso = fuerzaPico = recorrido = 0f;
            primeraMuestra = true;

            if (piezas != null)
                foreach (var p in piezas) if (p != null) p.Reiniciar();
            if (destinos != null)
                foreach (var d in destinos) if (d != null) d.Reiniciar();

            if (dispositivo != null)
            {
                dispositivo.CarryEnabled = false;
                dispositivo.attractorsEnabled = false;
            }
            if (guia != null) guia.gameObject.SetActive(false);
        }

        protected override void Update()
        {
            base.Update();

            var t = Keyboard.current;
            if (t != null && t.gKey.wasPressedThisFrame) guiaActiva = !guiaActiva;

            if (dispositivo == null) return;
            Vector3 punta = dispositivo.WorldPosition;

            BuscarCandidata(punta);
            LeerBotonAgarre(punta);

            if (enMano != null) enMano.SeguirPunta(punta, Time.deltaTime);

            AplicarFuerzasDeAcarreo();
            ActualizarGuia();
            VigilarCaidas();
            AcumularMetricas(punta);

            if (Estado == EstadoEnsayo.EnCurso && TodasColocadas()) Terminar();
        }

        /// <summary>La pieza suelta mas cercana que esta al alcance de la punta.</summary>
        private void BuscarCandidata(Vector3 punta)
        {
            candidata = null;
            if (enMano != null || piezas == null) return;
            float mejor = alcanceAgarre;
            foreach (var p in piezas)
            {
                if (p == null || p.Colocada || p.Agarrada) continue;
                float d = p.DistanciaA(punta);
                if (d < mejor) { mejor = d; candidata = p; }
            }
        }

        private void LeerBotonAgarre(Vector3 punta)
        {
            bool teclado = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
            bool sujeta  = dispositivo.Button1 || teclado;
            bool pulso   = dispositivo.Button1Down ||
                           (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame);

            if (modoMantener)
            {
                if (enMano == null && sujeta && candidata != null) Tomar(candidata, punta);
                else if (enMano != null && !sujeta)                Soltar();
            }
            else if (pulso)
            {
                if (enMano == null) { if (candidata != null) Tomar(candidata, punta); }
                else                Soltar();
            }
        }

        private void Tomar(ObjetoAgarrable p, Vector3 punta)
        {
            if (Estado == EstadoEnsayo.Preparado) Iniciar();
            if (Estado != EstadoEnsayo.EnCurso) return;
            p.Agarrar(punta);
            enMano = p;
            totalAgarres++;
        }

        private void Soltar()
        {
            if (enMano == null) return;
            ZonaDestino zona = ZonaBajoLaPieza(enMano);
            int caidasAntes = enMano.Caidas;
            enMano.Soltar(zona);
            if (enMano.Caidas > caidasAntes) totalCaidas++;
            enMano = null;
            dispositivo.CarryEnabled = false;
        }

        private ZonaDestino ZonaBajoLaPieza(ObjetoAgarrable p)
        {
            if (destinos == null) return null;
            foreach (var d in destinos)
                if (d != null && d.Libre && d.Contiene(p.transform.position)) return d;
            return null;
        }

        /// <summary>Peso e inercia de lo que se lleva en la mano.</summary>
        private void AplicarFuerzasDeAcarreo()
        {
            if (enMano == null) { dispositivo.CarryEnabled = false; return; }
            dispositivo.CarryEnabled    = true;
            dispositivo.CarryForceWorld = Vector3.down * enMano.PesoN;
            dispositivo.CarryDamping    = enMano.masaKg * amortiguamientoPorKg;
        }

        /// <summary>El atractor se coloca sobre el receptaculo de la pieza que se
        /// lleva; solo tiene efecto si la guia esta encendida.</summary>
        private void ActualizarGuia()
        {
            bool usar = guiaActiva && enMano != null && guia != null;
            dispositivo.attractorsEnabled = usar;
            if (guia != null) guia.gameObject.SetActive(usar);
            if (!usar) return;

            ZonaDestino destino = null;
            foreach (var d in destinos)
                if (d != null && d.id == enMano.idDestino) { destino = d; break; }
            if (destino == null) { dispositivo.attractorsEnabled = false; return; }

            guia.transform.position = destino.transform.position + Vector3.up * 0.35f;
            guia.stiffness = rigidezGuia;
            guia.radius    = radioGuia;
        }

        /// <summary>Una pieza que se cae de la mesa vuelve a su sitio de salida.</summary>
        private void VigilarCaidas()
        {
            if (piezas == null) return;
            foreach (var p in piezas)
                if (p != null && p.SeCayoFuera(alturaMesa)) p.DevolverAlOrigen();
        }

        private void AcumularMetricas(Vector3 punta)
        {
            if (Estado != EstadoEnsayo.EnCurso) return;

            float mag = dispositivo.ForceN.magnitude;
            impulso += mag * Time.deltaTime;
            if (mag > fuerzaPico) fuerzaPico = mag;

            if (primeraMuestra) { puntaPrevia = punta; primeraMuestra = false; }
            recorrido += Vector3.Distance(punta, puntaPrevia);
            puntaPrevia = punta;
        }

        private bool TodasColocadas()
        {
            if (piezas == null || piezas.Length == 0) return false;
            foreach (var p in piezas) if (p != null && !p.Colocada) return false;
            return true;
        }

        // ---------------------------------------------------------------------
        protected override void MuestrearTraza(float t)
        {
            Vector3 p = dispositivo.WorldPosition;
            Vector3 f = dispositivo.ForceN;
            traza.Fila(t, p.x, p.y, p.z, f.x, f.y, f.z, f.magnitude,
                       enMano != null,
                       enMano != null ? enMano.nombrePieza : "-",
                       enMano != null ? enMano.PesoN : 0f,
                       guiaActiva);
        }

        protected override void EscribirResumen()
        {
            var det = new StringBuilder();
            float sumaError = 0f;
            int n = 0;
            if (piezas != null)
            {
                foreach (var p in piezas)
                {
                    if (p == null) continue;
                    if (det.Length > 0) det.Append(" | ");
                    det.Append(p.nombrePieza).Append(':')
                       .Append(p.PesoN.ToString("F2")).Append("N/")
                       .Append(p.Agarres).Append("ag/")
                       .Append(p.Caidas).Append("ca/")
                       .Append(p.TiempoEnMano.ToString("F1")).Append("s/")
                       .Append(p.ErrorColocacion.ToString("F3")).Append('u');
                    if (p.Colocada) { sumaError += p.ErrorColocacion; n++; }
                }
            }

            FilaResumen(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                         guiaActiva ? "con_guia" : "sin_guia",
                         Tiempo,
                         piezas != null ? piezas.Length : 0,
                         totalAgarres,
                         totalCaidas,
                         recorrido,
                         impulso,
                         fuerzaPico,
                         n > 0 ? sumaError / n : 0f,
                         "\"" + det.ToString() + "\"");
        }

        // ---------------------------------------------------------------------
        public override string[] LineasHUD()
        {
            int colocadas = 0;
            if (piezas != null) foreach (var p in piezas) if (p != null && p.Colocada) colocadas++;

            string enManoTxt = enMano != null
                ? enMano.nombrePieza + "  (" + enMano.PesoN.ToString("F2") + " N)"
                : (candidata != null ? "al alcance: " + candidata.nombrePieza : "mano vacia");

            return new[]
            {
                "Piezas colocadas   " + colocadas + " / " + (piezas != null ? piezas.Length : 0),
                "En la mano         " + enManoTxt,
                "Agarres            " + totalAgarres,
                "Caidas             " + totalCaidas,
                "Recorrido          " + recorrido.ToString("F2") + " u",
                "Impulso  ∫|F|dt    " + impulso.ToString("F2") + " N·s",
                "Fuerza pico        " + fuerzaPico.ToString("F2") + " N",
                "Guia haptica       " + (guiaActiva ? "ENCENDIDA" : "apagada"),
            };
        }

        public override string[] LineasAyuda()
        {
            return new[]
            {
                modoMantener ? "Boton 1 del stylus (o ESPACIO): manten para sujetar, suelta para dejar"
                             : "Boton 1 del stylus (o ESPACIO): alterna tomar / soltar",
                "G guia haptica · R reiniciar · F fuerzas on/off",
                "1/2/3 cambian de ambiente · ESC menu",
            };
        }
    }
}
