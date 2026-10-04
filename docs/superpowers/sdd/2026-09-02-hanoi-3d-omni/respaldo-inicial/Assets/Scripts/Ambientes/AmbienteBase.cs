// AmbienteBase.cs -- Lo que comparten los tres ambientes de la Actividad 4.
//
// Cada ambiente es un ensayo cronometrado: se prepara, arranca cuando el
// usuario hace la primera accion, mide mientras dura y guarda dos CSV al
// terminar. Aqui vive esa maquina de estados, el cronometro, el muestreo de la
// traza y los atajos de teclado comunes; lo especifico de cada escenario esta
// en Ambiente1/2/3.
using UnityEngine;
using UnityEngine.InputSystem;

namespace HapticOmni
{
    public enum EstadoEnsayo { Preparado, EnCurso, Terminado }

    public abstract class AmbienteBase : MonoBehaviour
    {
        [Header("Comun")]
        public HapticDevice dispositivo;
        [Tooltip("Muestras por segundo de la traza. 100 Hz basta para dibujar la " +
                 "trayectoria y las curvas de fuerza; el detalle fino ya lo tiene " +
                 "el CSV nativo de 1 kHz.")]
        public float frecuenciaTraza = 100f;
        [Tooltip("Graba tambien el CSV nativo a 1 kHz mientras dura el ensayo.")]
        public bool grabarCsvNativo = true;

        public EstadoEnsayo Estado { get; protected set; } = EstadoEnsayo.Preparado;
        /// <summary>Segundos transcurridos desde que arranco el ensayo.</summary>
        public float Tiempo { get; private set; }

        /// <summary>Nombre corto que se muestra en el HUD.</summary>
        public abstract string Nombre { get; }
        /// <summary>Una linea por renglon del panel de estado.</summary>
        public abstract string[] LineasHUD();
        /// <summary>Ayuda de controles que se muestra abajo del panel.</summary>
        public abstract string[] LineasAyuda();

        protected RegistroCsv traza;
        protected RegistroCsv resumen;
        private float acumTraza;

        protected abstract string PrefijoArchivos { get; }
        protected abstract string CabeceraTraza { get; }
        protected abstract string CabeceraResumen { get; }

        /// <summary>Se llama una vez por muestra de traza mientras el ensayo corre.</summary>
        protected abstract void MuestrearTraza(float t);
        /// <summary>Se llama al terminar; aqui cada ambiente escribe su fila de resumen.</summary>
        protected abstract void EscribirResumen();
        /// <summary>Deja el escenario como al principio.</summary>
        protected abstract void PrepararEscenario();

        protected virtual void Awake()
        {
            if (dispositivo == null) dispositivo = FindFirstObjectByType<HapticDevice>();
            traza   = new RegistroCsv(CabeceraTraza);
            resumen = new RegistroCsv(CabeceraResumen);
        }

        protected virtual void Start() { Reiniciar(); }

        /// <summary>Vuelve al estado inicial sin guardar nada.</summary>
        public virtual void Reiniciar()
        {
            Estado = EstadoEnsayo.Preparado;
            Tiempo = 0f;
            acumTraza = 0f;
            traza.Limpiar();
            PrepararEscenario();
        }

        /// <summary>Arranca el cronometro. Los ambientes la llaman en la primera
        /// accion real del usuario, no al cargar la escena: si el reloj empezara
        /// al abrir la escena el tiempo medido incluiria la lectura de las
        /// instrucciones.</summary>
        public virtual void Iniciar()
        {
            if (Estado == EstadoEnsayo.EnCurso) return;
            Estado = EstadoEnsayo.EnCurso;
            Tiempo = 0f;
            acumTraza = 0f;
            traza.Limpiar();
            if (grabarCsvNativo && dispositivo != null && dispositivo.Connected)
                dispositivo.StartRecording(300);
            Debug.Log("[" + Nombre + "] Ensayo iniciado.");
        }

        /// <summary>Cierra el ensayo y guarda los archivos.</summary>
        public virtual void Terminar()
        {
            if (Estado != EstadoEnsayo.EnCurso) return;
            Estado = EstadoEnsayo.Terminado;

            EscribirResumen();
            traza.Guardar(PrefijoArchivos + "_traza.csv", false);
            resumen.Guardar(PrefijoArchivos + "_resumen.csv", true);
            resumen.Limpiar();

            if (dispositivo != null && dispositivo.Recording)
                dispositivo.StopRecording(PrefijoArchivos + "_1khz.csv");

            Debug.Log("[" + Nombre + "] Ensayo terminado en " + Tiempo.ToString("F2") + " s.");
        }

        protected virtual void Update()
        {
            LeerAtajosComunes();

            if (Estado != EstadoEnsayo.EnCurso) return;
            Tiempo += Time.deltaTime;

            acumTraza += Time.deltaTime;
            float paso = 1f / Mathf.Max(1f, frecuenciaTraza);
            while (acumTraza >= paso)
            {
                acumTraza -= paso;
                MuestrearTraza(Tiempo);
            }
        }

        private void LeerAtajosComunes()
        {
            var t = Keyboard.current;
            if (t == null) return;

            if (t.rKey.wasPressedThisFrame) Reiniciar();
            if (t.fKey.wasPressedThisFrame && dispositivo != null)
                dispositivo.forcesEnabled = !dispositivo.forcesEnabled;
            if (t.escapeKey.wasPressedThisFrame) SelectorAmbientes.IrAlMenu();

            // Botón 2 del stylus: tambien reinicia, para no soltar el equipo.
            if (dispositivo != null && dispositivo.Button2Down) Reiniciar();
        }

        /// <summary>Texto mm:ss.cs del cronometro.</summary>
        public string TiempoTexto
        {
            get
            {
                int m = Mathf.FloorToInt(Tiempo / 60f);
                float s = Tiempo - m * 60f;
                return string.Format("{0:00}:{1:00.00}", m, s);
            }
        }
    }
}
