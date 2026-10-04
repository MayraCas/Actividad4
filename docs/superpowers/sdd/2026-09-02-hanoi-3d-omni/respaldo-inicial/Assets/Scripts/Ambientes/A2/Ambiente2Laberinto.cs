// Ambiente2Laberinto.cs -- "Laberinto de precision".
//
// Ejercicio de la actividad: mover una esfera dentro de un laberinto y evaluar
// TIEMPO, ERRORES y TRAYECTORIA.
//
//   Tiempo      arranca cuando la bola sale de la casilla de salida, no al
//               cargar la escena: asi no se cuenta el rato que el usuario pasa
//               leyendo las instrucciones.
//   Errores     cada vez que la bola toca un muro se cuenta un choque. Se
//               cuentan flancos, no frames: rozar una pared durante medio
//               segundo es UN error, no treinta. Ademas se acumula el tiempo
//               total en contacto y la fuerza de choque maxima.
//   Trayectoria longitud recorrida frente a la del camino optimo (BFS sobre el
//               laberinto), y desviacion RMS respecto al eje del pasillo.
//
// La bola es la propia punta del dispositivo: se le da un radio con
// probeRadius y el puente infla los muros por ese radio, de modo que la esfera
// rueda contra las paredes en vez de atravesarlas como un punto. Dos
// semiespacios enfrentados la mantienen en el plano del tablero.
using UnityEngine;

namespace HapticOmni
{
    [AddComponentMenu("Haptics/A2 - Ambiente de laberinto")]
    public class Ambiente2Laberinto : AmbienteBase
    {
        [Header("Escenario")]
        public Transform bola;
        public Transform salida;
        public Transform meta;
        [Tooltip("Radio de aceptacion de la meta y de la salida, en unidades.")]
        public float radioMeta = 0.16f;

        [Header("Laberinto (lo rellena el generador de escena)")]
        public HapticShape[] muros;
        [Tooltip("Eje del pasillo: centros de las celdas del camino mas corto.")]
        public Vector3[] caminoOptimo;
        public float longitudOptima = 1f;
        public int nivel = 2;
        public int semilla = 12345;

        // ---- metricas --------------------------------------------------------
        private int choques;
        private float tiempoEnContacto;
        private float fuerzaChoquePico;
        private float recorrido;
        private float sumaDesviacion2;
        private int muestrasDesviacion;
        private float desviacionMax;

        private Vector3 puntaPrevia;
        private bool primeraMuestra = true;
        private bool tocandoMuro, tocandoPrevio;
        /// <summary>True cuando la bola ya ha pasado por la casilla de salida. El
        /// cronometro no puede arrancar antes: al cargar la escena la punta esta
        /// donde este el stylus, que casi nunca es la salida.</summary>
        private bool enSalida;
        private System.Collections.Generic.HashSet<HapticShape> setMuros;

        public int   Choques        { get { return choques; } }
        public float Recorrido      { get { return recorrido; } }
        public float DesviacionRms  { get { return muestrasDesviacion > 0 ? Mathf.Sqrt(sumaDesviacion2 / muestrasDesviacion) : 0f; } }
        public float Eficiencia     { get { return recorrido > 1e-4f ? longitudOptima / recorrido : 0f; } }

        public override string Nombre { get { return "AMBIENTE 2 · PRECISION MOTRIZ"; } }
        protected override string PrefijoArchivos { get { return "a2"; } }

        protected override string CabeceraTraza
        {
            get { return "t,x,y,z,fx,fy,fz,fmag,desviacion_u,tocando_muro,choques"; }
        }
        protected override string CabeceraResumen
        {
            get
            {
                return "fecha,nivel,semilla,tiempo_s,choques,t_contacto_s,f_choque_pico_n," +
                       "recorrido_u,optimo_u,eficiencia,desv_rms_u,desv_max_u";
            }
        }

        protected override void Awake()
        {
            base.Awake();
            setMuros = new System.Collections.Generic.HashSet<HapticShape>();
            if (muros != null)
                foreach (var m in muros) if (m != null) setMuros.Add(m);
        }

        // ---------------------------------------------------------------------
        protected override void PrepararEscenario()
        {
            choques = 0;
            tiempoEnContacto = 0f;
            fuerzaChoquePico = 0f;
            recorrido = 0f;
            sumaDesviacion2 = 0f;
            muestrasDesviacion = 0;
            desviacionMax = 0f;
            primeraMuestra = true;
            tocandoMuro = tocandoPrevio = false;
            enSalida = false;

            if (dispositivo != null)
            {
                dispositivo.CarryEnabled = false;
                dispositivo.RailEnabled = false;
                dispositivo.attractorsEnabled = false;
            }
        }

        protected override void Update()
        {
            base.Update();
            if (dispositivo == null) return;

            Vector3 punta = dispositivo.WorldPosition;
            if (bola != null) bola.position = punta;

            DetectarChoques();

            // Arranque en dos tiempos: primero hay que colocar la bola en la salida,
            // y el cronometro empieza al abandonarla.
            if (Estado == EstadoEnsayo.Preparado && salida != null)
            {
                float d = DistanciaEnPlano(punta, salida.position);
                if (!enSalida && d < radioMeta) enSalida = true;
                else if (enSalida && d > radioMeta * 1.6f) Iniciar();
            }

            if (Estado != EstadoEnsayo.EnCurso) return;

            if (primeraMuestra) { puntaPrevia = punta; primeraMuestra = false; }
            recorrido += Vector3.Distance(punta, puntaPrevia);
            puntaPrevia = punta;

            if (tocandoMuro) tiempoEnContacto += Time.deltaTime;

            if (meta != null && DistanciaEnPlano(punta, meta.position) < radioMeta)
                Terminar();
        }

        /// <summary>Distancia medida solo en el plano del tablero (XY). La Z de las
        /// marcas de salida y meta es decorativa: van adelantadas dentro del canal
        /// para que los muros altos no las tapen, mientras que la bola vive en
        /// z ~ 0. Contar esa diferencia se comeria casi todo el radio de meta.</summary>
        private static float DistanciaEnPlano(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dy = a.y - b.y;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>Un choque es un flanco: se cuenta al ENTRAR en contacto con un
        /// muro, no mientras dura el roce.</summary>
        private void DetectarChoques()
        {
            tocandoMuro = false;
            var lista = dispositivo.EnContacto;
            for (int i = 0; i < lista.Count; i++)
            {
                if (lista[i] == null || !setMuros.Contains(lista[i])) continue;
                tocandoMuro = true;
                break;
            }

            if (tocandoMuro && !tocandoPrevio && Estado == EstadoEnsayo.EnCurso) choques++;
            if (tocandoMuro)
            {
                float f = dispositivo.Connected ? dispositivo.ContactForceN
                                                : dispositivo.ForceN.magnitude;
                if (f > fuerzaChoquePico) fuerzaChoquePico = f;
            }
            tocandoPrevio = tocandoMuro;
        }

        /// <summary>Distancia de un punto al eje del pasillo (la polilinea del camino
        /// optimo). Es la desviacion que pide la actividad.</summary>
        public float DesviacionDelEje(Vector3 p)
        {
            if (caminoOptimo == null || caminoOptimo.Length == 0) return 0f;
            if (caminoOptimo.Length == 1) return Vector3.Distance(p, caminoOptimo[0]);

            float mejor = float.MaxValue;
            for (int i = 0; i < caminoOptimo.Length - 1; i++)
            {
                Vector3 a = caminoOptimo[i], b = caminoOptimo[i + 1];
                Vector3 ab = b - a;
                float len2 = ab.sqrMagnitude;
                float t = len2 > 1e-8f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / len2) : 0f;
                float d = Vector3.Distance(p, a + ab * t);
                if (d < mejor) mejor = d;
            }
            return mejor;
        }

        // ---------------------------------------------------------------------
        protected override void MuestrearTraza(float t)
        {
            Vector3 p = dispositivo.WorldPosition;
            Vector3 f = dispositivo.ForceN;
            float desv = DesviacionDelEje(p);

            sumaDesviacion2 += desv * desv;
            muestrasDesviacion++;
            if (desv > desviacionMax) desviacionMax = desv;

            traza.Fila(t, p.x, p.y, p.z, f.x, f.y, f.z, f.magnitude, desv, tocandoMuro, choques);
        }

        protected override void EscribirResumen()
        {
            resumen.Fila(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                         nivel, semilla, Tiempo, choques, tiempoEnContacto, fuerzaChoquePico,
                         recorrido, longitudOptima, Eficiencia, DesviacionRms, desviacionMax);
        }

        // ---------------------------------------------------------------------
        public override string[] LineasHUD()
        {
            return new[]
            {
                "Nivel              " + nivel + "   (semilla " + semilla + ")",
                "Errores (choques)  " + choques,
                "Tiempo en contacto " + tiempoEnContacto.ToString("F2") + " s",
                "F de choque pico   " + fuerzaChoquePico.ToString("F2") + " N",
                "Recorrido          " + recorrido.ToString("F2") + " u",
                "Camino optimo      " + longitudOptima.ToString("F2") + " u",
                "Eficiencia         " + (Eficiencia * 100f).ToString("F0") + " %",
                "Desviacion RMS     " + DesviacionRms.ToString("F3") + " u",
                tocandoMuro ? ">> TOCANDO MURO <<"
                            : Estado == EstadoEnsayo.Preparado
                              ? (enSalida ? "listo: sal de la casilla verde" : "coloca la bola en la casilla verde")
                              : "",
            };
        }

        public override string[] LineasAyuda()
        {
            return new[]
            {
                "Lleva la esfera del cuadro verde al rojo sin rozar los muros",
                "R reiniciar · F fuerzas on/off",
                "1/2/3 cambian de ambiente · ESC menu",
            };
        }

        void OnDrawGizmosSelected()
        {
            if (caminoOptimo == null) return;
            Gizmos.color = new Color(0.4f, 1f, 0.6f, 0.8f);
            for (int i = 0; i < caminoOptimo.Length - 1; i++)
                Gizmos.DrawLine(caminoOptimo[i], caminoOptimo[i + 1]);
        }
    }
}
