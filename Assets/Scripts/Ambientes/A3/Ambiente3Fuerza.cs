// Ambiente3Fuerza.cs -- "Banco de resistencias".
//
// Ejercicio de la actividad: mover objetos con diferentes resistencias y
// REGISTRAR LA FUERZA APLICADA y el TIEMPO REQUERIDO.
//
// Hay cinco correderas identicas en aspecto y recorrido, y distintas solo en el
// modelo de resistencia que el servo loop evalua. Que se vean iguales es
// deliberado: la unica informacion que distingue una de otra llega por la mano.
//
// Sobre "la fuerza aplicada": el Touch no lleva sensor de fuerza, asi que no se
// mide lo que el usuario empuja. Lo que se registra es la resistencia que el
// dispositivo opone a lo largo del riel, que en movimiento cuasiestatico es
// igual y opuesta a la que aplica la mano. Con el equipo conectado ese valor lo
// calcula el servo loop a 1 kHz y lo publica en el estado; sin equipo se evalua
// el mismo modelo en Unity para que las graficas sigan teniendo sentido. El CSV
// marca cual de los dos casos fue.
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HapticOmni
{
    [AddComponentMenu("Haptics/A3 - Ambiente de fuerza")]
    public class Ambiente3Fuerza : AmbienteBase
    {
        [Header("Escenario")]
        public EstacionResistencia[] estaciones;

        [Header("Enganche")]
        [Tooltip("Con el boton 1 mantenido se sujeta la corredera. Es el mismo gesto " +
                 "que en el ambiente 1.")]
        public bool modoMantener = true;
        [Tooltip("Si la punta se aleja mas de esto del eje del riel, la corredera se " +
                 "escapa de la mano.")]
        public float distanciaDeEscape = 0.35f;

        [Header("Ajuste del riel")]
        [Range(0.02f, 0.25f)] public float rigidezLateral = 0.12f;
        [Range(0.02f, 0.25f)] public float rigidezTopes = 0.20f;
        [Range(0.02f, 0.25f)] public float rigidezAdherencia = 0.12f;

        // ---- estado ----------------------------------------------------------
        private EstacionResistencia activa;
        private EstacionResistencia cercana;
        private int indiceActiva = -1;
        private float avancePrevio;
        private float fuerzaActual;

        public override string Nombre { get { return "AMBIENTE 3 · INTERACCION CON FUERZA"; } }
        protected override string PrefijoArchivos { get { return "a3"; } }

        protected override string CabeceraTraza
        {
            get { return "t,estacion,nombre,avance_mm,velocidad_mms,fuerza_n,deslizando,fuente"; }
        }
        protected override string CabeceraResumen
        {
            get
            {
                return CabeceraSujeto +
                       ",fecha,estacion,nombre,k_nmm,b_nmms,fs_n,fk_n,tiempo_s," +
                       "f_media_n,f_pico_n,f_arranque_n,impulso_ns,trabajo_nmm,completada,fuente";
            }
        }

        private string Fuente { get { return dispositivo != null && dispositivo.Connected ? "servo_1khz" : "modelo_unity"; } }

        // ---------------------------------------------------------------------
        protected override void PrepararEscenario()
        {
            activa = null;
            cercana = null;
            indiceActiva = -1;
            fuerzaActual = 0f;

            if (estaciones != null)
                foreach (var e in estaciones) if (e != null) e.Reiniciar();

            if (dispositivo != null)
            {
                dispositivo.RailEnabled = false;
                dispositivo.CarryEnabled = false;
                dispositivo.attractorsEnabled = false;
                dispositivo.UserTag = 0;
            }
        }

        protected override void Update()
        {
            base.Update();
            if (dispositivo == null || estaciones == null) return;

            Vector3 punta = dispositivo.WorldPosition;
            BuscarCercana(punta);
            LeerBotonEnganche(punta);
            ActualizarEstacionActiva(punta);

            if (Estado == EstadoEnsayo.EnCurso && TodasCompletadas()) Terminar();
        }

        private void BuscarCercana(Vector3 punta)
        {
            cercana = null;
            if (activa != null) { cercana = activa; return; }
            float mejor = float.MaxValue;
            foreach (var e in estaciones)
            {
                if (e == null) continue;
                float d = e.DistanciaAsidero(punta);
                if (d < e.radioEnganche && d < mejor) { mejor = d; cercana = e; }
            }
        }

        private void LeerBotonEnganche(Vector3 punta)
        {
            bool teclado = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
            bool sujeta  = dispositivo.Button1 || teclado;
            bool pulso   = dispositivo.Button1Down ||
                           (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame);

            if (modoMantener)
            {
                if (activa == null && sujeta && cercana != null) Enganchar(cercana, punta);
                else if (activa != null && !sujeta)              Soltar();
            }
            else if (pulso)
            {
                if (activa == null) { if (cercana != null) Enganchar(cercana, punta); }
                else                Soltar();
            }

            // Si la mano se aleja demasiado del eje, la corredera se escapa sola.
            if (activa != null)
            {
                float s = activa.Proyectar(punta);
                Vector3 sobreElEje = activa.Origen + activa.EjeMundo * s;
                if (Vector3.Distance(punta, sobreElEje) > distanciaDeEscape) Soltar();
            }
        }

        private void Enganchar(EstacionResistencia e, Vector3 punta)
        {
            if (Estado == EstadoEnsayo.Preparado) Iniciar();
            if (Estado != EstadoEnsayo.EnCurso) return;

            activa = e;
            activa.Enganchada = true;
            indiceActiva = System.Array.IndexOf(estaciones, e);
            avancePrevio = e.Avance;

            dispositivo.RailEnabled     = true;
            dispositivo.RailOriginWorld = e.Origen;
            dispositivo.RailAxisWorld   = e.EjeMundo;
            dispositivo.RailLength      = e.longitud;
            dispositivo.RailK           = e.k;
            dispositivo.RailB           = e.b;
            dispositivo.RailFs          = e.Fs;
            dispositivo.RailFk          = e.Fk;
            dispositivo.RailStickK      = rigidezAdherencia;
            dispositivo.RailLateralK    = rigidezLateral;
            dispositivo.RailEndK        = rigidezTopes;
            dispositivo.UserTag         = indiceActiva + 1;   // marca del CSV de 1 kHz
        }

        private void Soltar()
        {
            if (activa != null) activa.Enganchada = false;
            activa = null;
            indiceActiva = -1;
            fuerzaActual = 0f;
            dispositivo.RailEnabled = false;
            dispositivo.UserTag = 0;
        }

        private void ActualizarEstacionActiva(Vector3 punta)
        {
            if (activa == null) return;

            float avance = Mathf.Clamp(activa.Proyectar(punta), 0f, activa.longitud);
            float dt = Mathf.Max(1e-4f, Time.deltaTime);
            float velocidad = (avance - avancePrevio) / dt;
            avancePrevio = avance;

            fuerzaActual = dispositivo.Connected
                         ? Mathf.Abs(dispositivo.RailForceN)
                         : activa.FuerzaModelo(avance, velocidad, dispositivo.unitsPerMillimeter);

            activa.Actualizar(avance, fuerzaActual, dt);
        }

        private bool TodasCompletadas()
        {
            if (estaciones == null || estaciones.Length == 0) return false;
            foreach (var e in estaciones) if (e != null && !e.Completada) return false;
            return true;
        }

        // ---------------------------------------------------------------------
        protected override void MuestrearTraza(float t)
        {
            float upm = dispositivo.unitsPerMillimeter;
            float avanceMm = activa != null ? activa.Avance / Mathf.Max(1e-5f, upm) : 0f;
            float velMm = dispositivo.Connected
                        ? Vector3.Dot(dispositivo.VelocityMm,
                                      dispositivo.WorldToDeviceDirection(
                                          activa != null ? activa.EjeMundo : Vector3.right))
                        : 0f;

            traza.Fila(t,
                       indiceActiva + 1,
                       activa != null ? activa.nombre : "-",
                       avanceMm,
                       velMm,
                       fuerzaActual,
                       dispositivo.Connected && dispositivo.RailSliding,
                       Fuente);
        }

        protected override void EscribirResumen()
        {
            if (estaciones == null) return;
            float upm = Mathf.Max(1e-5f, dispositivo != null ? dispositivo.unitsPerMillimeter : 0.025f);
            for (int i = 0; i < estaciones.Length; i++)
            {
                var e = estaciones[i];
                if (e == null) continue;
                FilaResumen(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                             i + 1, e.nombre,
                             e.k, e.b, e.Fs, e.Fk,
                             e.TiempoActivo,
                             e.FuerzaMedia, e.FuerzaPico, e.FuerzaDeArranque,
                             e.Impulso,
                             e.Trabajo / upm,          // N·mm
                             e.Completada,
                             Fuente);
            }
        }

        // ---------------------------------------------------------------------
        public override string[] LineasHUD()
        {
            var lineas = new System.Collections.Generic.List<string>();
            int hechas = 0;
            if (estaciones != null) foreach (var e in estaciones) if (e != null && e.Completada) hechas++;

            lineas.Add("Estaciones         " + hechas + " / " + (estaciones != null ? estaciones.Length : 0));
            lineas.Add("En la mano         " + (activa != null ? activa.nombre
                                              : cercana != null ? "al alcance: " + cercana.nombre
                                              : "ninguna"));
            lineas.Add("Fuerza actual      " + fuerzaActual.ToString("F2") + " N" +
                       (activa != null && dispositivo.Connected
                        ? (dispositivo.RailSliding ? "  (deslizando)" : "  (adherida)") : ""));
            lineas.Add("");

            if (estaciones != null)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < estaciones.Length; i++)
                {
                    var e = estaciones[i];
                    if (e == null) continue;
                    sb.Clear();
                    sb.Append(e.Completada ? "✔ " : (e == activa ? "→ " : "  "));
                    sb.Append(e.nombre.PadRight(16).Substring(0, Mathf.Min(16, e.nombre.Length)).PadRight(16));
                    sb.Append((e.Avance / Mathf.Max(1e-4f, e.longitud) * 100f).ToString("F0").PadLeft(3)).Append("% ");
                    sb.Append(e.FuerzaMedia.ToString("F2")).Append("N ");
                    sb.Append(e.TiempoActivo.ToString("F1")).Append("s");
                    lineas.Add(sb.ToString());
                }
            }
            return lineas.ToArray();
        }

        public override string[] LineasAyuda()
        {
            return new[]
            {
                "Acerca la punta a una corredera y manten el boton 1 (o ESPACIO)",
                "Llevala hasta el final del riel; repite con las cinco",
                "R reiniciar · F fuerzas on/off · 1/2/3 ambientes · ESC menu",
            };
        }
    }
}
