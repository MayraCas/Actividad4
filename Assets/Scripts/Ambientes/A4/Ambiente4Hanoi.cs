// Ambiente4Hanoi.cs -- "Torres de Hanoi hapticas".
//
// Ejercicio de la actividad: resolver el rompecabezas moviendo discos de uno en
// uno, sin apoyar nunca uno grande sobre uno pequeno. Lo que lo hace distinto de
// una version con raton es que cada disco pesa: el mas pequeno se siente como
// una pluma y el mas grande llega casi al maximo que el Touch puede entregar.
// El sujeto planifica sabiendo que el disco de abajo va a costarle mas.
//
// Que se siente en cada fase:
//
//   Tomar    la punta choca con el disco (F = k*penetracion, mas friccion) y el
//            boton 1 lo engancha. Solo se puede tomar la cima de una torre.
//   Mover    el puente aplica una fuerza constante hacia abajo de m*g y un
//            amortiguamiento proporcional a la masa, que es la inercia. Es una
//            fuerza que no depende de la posicion, asi que actualizarla a 60 Hz
//            no compromete la estabilidad: lo que exige el kilohercio son los
//            contactos, no la gravedad.
//   Rechazo  si el disco no cabe en la torre bajo la punta, se enciende un
//            semiespacio sobre la cima y la mano topa con un techo. Ese "no" es
//            la metrica propia del ambiente: cuantas veces se intento y con
//            cuanta fuerza se empujo contra la barrera.
//   Soltar   sobre una torre valida, el disco se posa; fuera de toda torre, cae
//            a la mesa, cuenta como caida y vuelve a su sitio.
//
// La guia haptica (tecla G) atrae hacia el poste del siguiente movimiento
// optimo, recalculado desde el estado actual. Comparar tiempo y eficiencia con
// guia y sin ella es la misma medida que hace el ambiente 1.
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HapticOmni
{
    [AddComponentMenu("Haptics/A4 - Torres de Hanoi")]
    public class Ambiente4Hanoi : AmbienteBase
    {
        [Header("Escenario")]
        [Tooltip("Los siete discos que crea la escena; el nivel decide cuantos se usan.")]
        public DiscoHanoi[] discos;
        [Tooltip("Las tres torres, EN ORDEN: torres[i].indice tiene que valer i. " +
                 "El estado del juego habla de torres por su indice y aqui se usa " +
                 "ese indice para entrar al arreglo; si la escena las montase " +
                 "desordenadas, un disco se posaria sobre la torre equivocada.")]
        public TorreHanoi[] torres;
        public float alturaMesa = -1.02f;

        [Header("Agarre")]
        [Tooltip("Margen extra sobre la superficie del disco para poder engancharlo.")]
        public float alcanceAgarre = 0.10f;
        [Tooltip("Con el boton 1 mantenido se sujeta; al soltarlo se suelta el disco.")]
        public bool modoMantener = true;

        [Header("Guia haptica (tecla G)")]
        public bool guiaActiva = false;
        public HapticAttractor guia;
        [Range(0.01f, 0.12f)] public float rigidezGuia = 0.03f;
        public float radioGuia = 2.5f;

        [Header("Dificultad")]
        public int nivel = 0;
        public int discosElegidos = 3;

        public ConfiguracionHanoi Config { get; private set; }
        public EstadoHanoi Estado4 { get; private set; }
        public int NivelActual { get { return nivel; } }
        public int DiscosActuales { get { return discosElegidos; } }
        /// <summary>Mientras el panel de configuracion esta abierto el ambiente no
        /// lee el boton de agarre: elegir el nivel no debe arrancar el ensayo.</summary>
        public bool ConfigAbierta { get; set; } = true;

        // ---- metricas del ensayo --------------------------------------------
        private int totalAgarres, totalCaidas, intentosIlegales;
        private float impulso, fuerzaPico, recorrido, fuerzaRechazoPico;
        private Vector3 puntaPrevia;
        private bool primeraMuestra = true;

        // ---- metricas del movimiento en curso -------------------------------
        private RegistroCsv movs;
        private float tMovInicio, recorridoMov, impulsoMov, picoMov;
        private int ilegalesMov;

        // ---- estado de la mano ----------------------------------------------
        private DiscoHanoi enMano, candidato;
        private int torreOrigen = -1;
        private int torreSobre = -1;
        private bool rechazando;

        public override string Nombre { get { return "AMBIENTE 4 · TORRES DE HANOI"; } }
        protected override string PrefijoArchivos { get { return "hanoi"; } }

        protected override string CabeceraTraza
        {
            get
            {
                return "t,x,y,z,fx,fy,fz,fmag,agarrado,disco,radio,peso_n," +
                       "torre_origen,torre_sobre,ilegal";
            }
        }

        protected override string CabeceraResumen
        {
            get
            {
                return CabeceraSujeto +
                       ",fecha,nivel,discos,completado,tiempo_s,movimientos," +
                       "movimientos_optimos,eficiencia,intentos_ilegales,caidas,agarres," +
                       "recorrido_u,impulso_ns,fuerza_pico_n,fuerza_rechazo_pico_n,guia";
            }
        }

        private const string CabeceraMovs =
            "n_mov,t_inicio,t_fin,disco,desde,hacia,intentos_ilegales," +
            "recorrido_u,impulso_ns,fuerza_pico_n";

        // ---------------------------------------------------------------------
        protected override void Awake()
        {
            Config = ConfiguracionHanoi.Cargar();
            var n = Config.Nivel(nivel);
            discosElegidos = Mathf.Clamp(n.discos, EstadoHanoi.MinDiscos, EstadoHanoi.MaxDiscos);
            Estado4 = new EstadoHanoi(discosElegidos);
            movs = new RegistroCsv(CabeceraMovs);
            base.Awake();
        }

        /// <summary>Cambia de nivel en caliente. La escena crea siempre siete discos;
        /// aqui se decide cuantos se activan y de que tamano son, de modo que no hace
        /// falta generar geometria en tiempo de ejecucion.</summary>
        public void AplicarNivel(int nivelNuevo, int discosNuevos)
        {
            if (Config == null) Config = ConfiguracionHanoi.Cargar();
            nivel = Mathf.Clamp(nivelNuevo, 0,
                                Config.niveles != null ? Config.niveles.Length - 1 : 0);
            discosElegidos = Mathf.Clamp(discosNuevos, EstadoHanoi.MinDiscos, EstadoHanoi.MaxDiscos);

            if (!Config.Nivel(nivel).guiaPermitida) guiaActiva = false;

            if (Estado4 == null) Estado4 = new EstadoHanoi(discosElegidos);
            else Estado4.Reiniciar(discosElegidos);

            if (discos != null)
            {
                for (int i = 0; i < discos.Length; i++)
                {
                    if (discos[i] == null) continue;
                    bool enJuego = i < discosElegidos;
                    discos[i].gameObject.SetActive(enJuego);
                    if (!enJuego) continue;
                    discos[i].Configurar(i,
                                         Config.MasaDe(i, discosElegidos),
                                         Config.RadioDe(i, discosElegidos),
                                         Config.retardoPorKg,
                                         Config.rigidezDisco,
                                         Config.friccionDisco);
                }
            }

            Reiniciar();
        }

        /// <summary>La escena guarda los siete discos activos, porque el nivel no se
        /// conoce hasta que se carga la configuracion. Por eso el arranque pasa por
        /// AplicarNivel y no por el Reiniciar() de la base: si colocasemos antes de
        /// activar y dimensionar, los discos que sobran no estarian en EstadoHanoi,
        /// su NivelDe seria -1 y acabarian todos apilados en la base de la torre 0,
        /// con su tamano de plantilla. AplicarNivel termina llamando a Reiniciar(),
        /// asi que esto hace lo mismo que la base y ademas en el orden correcto.</summary>
        protected override void Start()
        {
            AplicarNivel(nivel, discosElegidos);
        }

        // ---------------------------------------------------------------------
        protected override void PrepararEscenario()
        {
            enMano = null;
            candidato = null;
            torreOrigen = -1;
            torreSobre = -1;
            rechazando = false;

            totalAgarres = totalCaidas = intentosIlegales = 0;
            impulso = fuerzaPico = recorrido = fuerzaRechazoPico = 0f;
            primeraMuestra = true;
            if (movs != null) movs.Limpiar();

            if (Estado4 != null) Estado4.Reiniciar();

            // Cada disco a su nivel en la torre 0: el mas grande abajo.
            if (discos != null && torres != null && torres.Length > 0 && torres[0] != null)
            {
                for (int i = 0; i < discos.Length; i++)
                {
                    if (discos[i] == null || !discos[i].gameObject.activeSelf) continue;
                    discos[i].Reiniciar();
                    int nivelEnPila = Estado4.NivelDe(i);
                    discos[i].PosarEn(torres[0].PosicionDisco(Mathf.Max(0, nivelEnPila)));
                }
            }

            if (torres != null)
                foreach (var t in torres)
                    if (t != null) t.MostrarBarrera(false, 0, Config != null ? Config.rigidezRechazo : 0.22f);

            if (dispositivo != null)
            {
                dispositivo.CarryEnabled = false;
                dispositivo.attractorsEnabled = false;
            }
            if (guia != null) guia.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------------
        protected override void Update()
        {
            base.Update();

            var t = Keyboard.current;
            if (t != null && t.gKey.wasPressedThisFrame && Config.Nivel(nivel).guiaPermitida)
                guiaActiva = !guiaActiva;

            if (dispositivo == null) return;
            Vector3 punta = dispositivo.WorldPosition;

            torreSobre = TorreBajoLaPunta(punta);
            ActualizarRechazo();

            if (!ConfigAbierta)
            {
                BuscarCandidato(punta);
                LeerBotonAgarre(punta);
            }

            if (enMano != null) enMano.SeguirPunta(punta, Time.deltaTime);

            AplicarFuerzasDeAcarreo();
            ActualizarGuia();
            VigilarCaidas();
            AcumularMetricas(punta);

            if (Estado == EstadoEnsayo.EnCurso)
            {
                if (Estado4.Resuelto) Terminar();
                else if (Config.limiteTiempoS > 0f && Tiempo >= Config.limiteTiempoS) Terminar();
            }
        }

        /// <summary>La torre cuyo eje esta mas cerca de la punta, si esta dentro de su
        /// radio de aceptacion.</summary>
        private int TorreBajoLaPunta(Vector3 punta)
        {
            if (torres == null) return -1;
            int mejor = -1;
            float mejorD = float.MaxValue;
            foreach (var t in torres)
            {
                if (t == null) continue;
                float d = t.DistanciaHorizontal(punta);
                if (d <= t.radioAceptacion && d < mejorD) { mejorD = d; mejor = t.indice; }
            }
            return mejor;
        }

        /// <summary>Enciende la barrera de la torre bajo la punta si el disco que se
        /// lleva no cabe ahi, y la apaga en todas las demas.</summary>
        private void ActualizarRechazo()
        {
            rechazando = enMano != null && torreSobre >= 0 && torreSobre != torreOrigen &&
                         !Estado4.PuedeRecibir(torreSobre, enMano.indice);

            if (torres == null) return;
            bool bloquear = rechazando && Config.bloqueoHaptico;
            foreach (var t in torres)
            {
                if (t == null) continue;
                bool suya = bloquear && t.indice == torreSobre;
                t.MostrarBarrera(suya, Estado4.Altura(t.indice), Config.rigidezRechazo);
            }
        }

        /// <summary>El disco suelto mas cercano que ademas es cima de su torre: solo
        /// la cima se puede tomar, igual que en el juego de mesa.</summary>
        private void BuscarCandidato(Vector3 punta)
        {
            candidato = null;
            if (enMano != null || discos == null) return;
            float mejor = alcanceAgarre;
            for (int i = 0; i < discos.Length; i++)
            {
                var d = discos[i];
                if (d == null || !d.gameObject.activeSelf || d.Agarrado || d.Cayendo) continue;
                int torre = Estado4.TorreDe(i);
                if (torre < 0 || Estado4.Cima(torre) != i) continue;
                float dist = d.DistanciaA(punta);
                if (dist < mejor) { mejor = dist; candidato = d; }
            }
        }

        private void LeerBotonAgarre(Vector3 punta)
        {
            bool teclado = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
            bool sujeta = dispositivo.Button1 || teclado;
            bool pulso  = dispositivo.Button1Down ||
                          (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame);

            if (modoMantener)
            {
                if (enMano == null && sujeta && candidato != null) Tomar(candidato, punta);
                else if (enMano != null && !sujeta) Soltar();
            }
            else if (pulso)
            {
                if (enMano == null) { if (candidato != null) Tomar(candidato, punta); }
                else Soltar();
            }
        }

        private void Tomar(DiscoHanoi d, Vector3 punta)
        {
            if (Estado == EstadoEnsayo.Preparado) Iniciar();
            if (Estado != EstadoEnsayo.EnCurso) return;

            torreOrigen = Estado4.TorreDe(d.indice);
            if (torreOrigen < 0) return;

            d.Agarrar(punta);
            enMano = d;
            totalAgarres++;

            tMovInicio = Tiempo;
            recorridoMov = impulsoMov = picoMov = 0f;
            ilegalesMov = 0;
        }

        private void Soltar()
        {
            if (enMano == null) return;

            if (torreSobre < 0)
            {
                // Fuera de toda torre: se lo lleva la fisica.
                enMano.Caer();
                totalCaidas++;
                enMano = null;
                dispositivo.CarryEnabled = false;
                return;
            }

            if (torreSobre == torreOrigen)
            {
                // Devuelto a su sitio: ni movimiento ni error.
                enMano.PosarEn(torres[torreOrigen].PosicionDisco(
                                   Estado4.Altura(torreOrigen) - 1));
                enMano = null;
                dispositivo.CarryEnabled = false;
                return;
            }

            if (!Estado4.PuedeRecibir(torreSobre, enMano.indice))
            {
                intentosIlegales++;
                ilegalesMov++;
                enMano.PosarEn(torres[torreOrigen].PosicionDisco(
                                   Estado4.Altura(torreOrigen) - 1));
                enMano = null;
                dispositivo.CarryEnabled = false;
                return;
            }

            // Movimiento valido. El nivel se calcula ANTES de mover: es la altura
            // que tiene la torre destino justo ahora.
            int nivelDestino = Estado4.Altura(torreSobre);
            int idDisco = enMano.indice;
            int desde = torreOrigen, hacia = torreSobre;
            Estado4.Mover(desde, hacia);
            enMano.PosarEn(torres[hacia].PosicionDisco(nivelDestino));

            movs.Fila(Estado4.Movimientos, tMovInicio, Tiempo, idDisco, desde, hacia,
                      ilegalesMov, recorridoMov, impulsoMov, picoMov);

            enMano = null;
            torreOrigen = -1;
            dispositivo.CarryEnabled = false;
        }

        /// <summary>Peso e inercia de lo que se lleva en la mano.</summary>
        private void AplicarFuerzasDeAcarreo()
        {
            if (enMano == null) { dispositivo.CarryEnabled = false; return; }
            dispositivo.CarryEnabled = true;
            dispositivo.CarryForceWorld = Vector3.down * enMano.PesoN;
            dispositivo.CarryDamping = enMano.masaKg * Config.amortiguamientoPorKg;
        }

        /// <summary>El atractor se coloca sobre el poste del siguiente movimiento
        /// optimo: el de destino si ya llevas el disco correcto, el de origen si aun
        /// no has tomado nada.</summary>
        private void ActualizarGuia()
        {
            bool usar = guiaActiva && guia != null && Config.Nivel(nivel).guiaPermitida &&
                        Estado == EstadoEnsayo.EnCurso;
            if (usar)
            {
                var m = Estado4.SiguienteMovimientoOptimo();
                int destino = -1;
                if (m.valido)
                    destino = (enMano != null && enMano.indice == Estado4.Cima(m.desde))
                            ? m.hacia : m.desde;
                if (destino >= 0 && torres != null && destino < torres.Length && torres[destino] != null)
                {
                    guia.transform.position = torres[destino].PosicionDisco(
                                                  Estado4.Altura(destino)) + Vector3.up * 0.35f;
                    guia.stiffness = rigidezGuia;
                    guia.radius = radioGuia;
                }
                else usar = false;
            }

            dispositivo.attractorsEnabled = usar;
            if (guia != null && guia.gameObject.activeSelf != usar)
                guia.gameObject.SetActive(usar);
        }

        /// <summary>Un disco caido vuelve a su torre pasado un momento, o de inmediato
        /// si se salio de la mesa.</summary>
        private void VigilarCaidas()
        {
            if (discos == null || torres == null) return;
            for (int i = 0; i < discos.Length; i++)
            {
                var d = discos[i];
                if (d == null || !d.gameObject.activeSelf || !d.Cayendo) continue;

                bool fuera = d.transform.position.y < alturaMesa - 1.5f;
                if (!d.ActualizarCaida(Time.deltaTime) && !fuera) continue;

                int torre = Estado4.TorreDe(i);
                if (torre < 0) torre = 0;
                d.PosarEn(torres[torre].PosicionDisco(Mathf.Max(0, Estado4.NivelDe(i))));
            }
        }

        private void AcumularMetricas(Vector3 punta)
        {
            if (Estado != EstadoEnsayo.EnCurso) return;

            float mag = dispositivo.ForceN.magnitude;
            impulso += mag * Time.deltaTime;
            if (mag > fuerzaPico) fuerzaPico = mag;
            if (rechazando && mag > fuerzaRechazoPico) fuerzaRechazoPico = mag;

            if (primeraMuestra) { puntaPrevia = punta; primeraMuestra = false; }
            float paso = Vector3.Distance(punta, puntaPrevia);
            recorrido += paso;
            puntaPrevia = punta;

            if (enMano != null)
            {
                recorridoMov += paso;
                impulsoMov += mag * Time.deltaTime;
                if (mag > picoMov) picoMov = mag;
            }
        }

        // ---------------------------------------------------------------------
        protected override void MuestrearTraza(float t)
        {
            Vector3 p = dispositivo.WorldPosition;
            Vector3 f = dispositivo.ForceN;
            traza.Fila(t, p.x, p.y, p.z, f.x, f.y, f.z, f.magnitude,
                       enMano != null,
                       enMano != null ? enMano.indice : -1,
                       enMano != null ? enMano.radio : 0f,
                       enMano != null ? enMano.PesoN : 0f,
                       torreOrigen,
                       torreSobre,
                       rechazando);
        }

        protected override void EscribirResumen()
        {
            FilaResumen(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Config.Nivel(nivel).nombre,
                        discosElegidos,
                        Estado4.Resuelto,
                        Tiempo,
                        Estado4.Movimientos,
                        Estado4.MovimientosOptimos,
                        Estado4.Eficiencia,
                        intentosIlegales,
                        totalCaidas,
                        totalAgarres,
                        recorrido,
                        impulso,
                        fuerzaPico,
                        fuerzaRechazoPico,
                        guiaActiva ? "con_guia" : "sin_guia");
        }

        /// <summary>La bitacora de movimientos es propia de este ambiente, asi que se
        /// guarda aqui, despues de que la base haya cerrado el ensayo y fijado el
        /// numero de ensayo con el que se nombran los archivos.</summary>
        public override void Terminar()
        {
            if (Estado != EstadoEnsayo.EnCurso) return;
            base.Terminar();
            movs.Guardar(NombreArchivoEnsayo("movs.csv"), false);
            movs.Limpiar();
        }

        // ---------------------------------------------------------------------
        public override string[] LineasHUD()
        {
            var pilas = new StringBuilder();
            for (int t = 0; t < EstadoHanoi.Torres; t++)
            {
                if (t > 0) pilas.Append("  ");
                pilas.Append(t + 1).Append(':').Append(Estado4 != null ? Estado4.Altura(t) : 0);
            }

            string enManoTxt = enMano != null
                ? "disco " + (enMano.indice + 1) + "  (" + enMano.PesoN.ToString("F2") + " N)"
                : (candidato != null ? "al alcance: disco " + (candidato.indice + 1) : "mano vacia");

            return new[]
            {
                "Nivel              " + Config.Nivel(nivel).nombre + "  ·  " +
                                        discosElegidos + " discos",
                "Torres             " + pilas,
                "En la mano         " + enManoTxt,
                "Movimientos        " + Estado4.Movimientos + " / " + Estado4.MovimientosOptimos +
                                        " optimos",
                "Eficiencia         " + Estado4.Eficiencia.ToString("F2"),
                "Intentos ilegales  " + intentosIlegales,
                "Caidas             " + totalCaidas,
                "Recorrido          " + recorrido.ToString("F2") + " u",
                "Impulso  ∫|F|dt    " + impulso.ToString("F2") + " N·s",
                "Fuerza pico        " + fuerzaPico.ToString("F2") + " N",
                "Rechazo pico       " + fuerzaRechazoPico.ToString("F2") + " N",
                "Guia haptica       " + (!Config.Nivel(nivel).guiaPermitida ? "no disponible"
                                       : guiaActiva ? "ENCENDIDA" : "apagada"),
            };
        }

        public override string[] LineasAyuda()
        {
            return new[]
            {
                modoMantener ? "Boton 1 del stylus (o ESPACIO): manten para sujetar, suelta para dejar"
                             : "Boton 1 del stylus (o ESPACIO): alterna tomar / soltar",
                "C configuracion · G guia haptica · R reiniciar · F fuerzas on/off",
                "1/2/3/4 cambian de ambiente · ESC menu",
            };
        }
    }
}
