// SesionSujeto.cs -- Quien esta haciendo la prueba.
//
// Hasta la Actividad 4 todos los ensayos caian en los mismos archivos sin decir
// quien los habia hecho, lo que impide comparar entre personas o entre sesiones
// de la misma persona. Este componente cierra ese hueco: se da de alta una vez
// en el menu, sobrevive a los cambios de escena y redirige la escritura de CSV
// a Analysis/datos/<ID>/.
//
// No se guardan datos personales identificables. El ID lo asigna quien conduce
// la prueba y la correspondencia con la persona, si hace falta, vive fuera del
// proyecto.
using System.Globalization;
using System.IO;
using UnityEngine;

namespace HapticOmni
{
    [AddComponentMenu("Haptics/Sesion del sujeto")]
    public class SesionSujeto : MonoBehaviour
    {
        public const string IdAnonimo = "ANON";
        public const string ArchivoSujetos = "sujetos.csv";
        private const string CabeceraSujetos = "id,fecha,edad,mano,experiencia,sesion,notas";

        public static SesionSujeto Actual { get; private set; }

        [Header("Identidad (anonima)")]
        public string id = IdAnonimo;
        public int edad;
        public string mano = "diestro";
        public string experiencia = "ninguna";

        [Header("Sesion")]
        public int sesion = 1;
        public string condicion = "sin_guia";
        [TextArea] public string notas = "";

        public static string IdActual
        {
            get
            {
                return Actual != null && EsIdValido(Actual.id) ? Actual.id.Trim() : IdAnonimo;
            }
        }

        public static string CondicionActual
        {
            get { return Actual != null ? Actual.condicion : "sin_guia"; }
        }

        public static int SesionActual
        {
            get { return Actual != null ? Actual.sesion : 0; }
        }

        /// <summary>Nombres que Windows no deja usar como carpeta, pase lo que
        /// pase con las letras que los componen.</summary>
        private static readonly string[] ReservadosWindows =
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
        };

        /// <summary>Un ID sirve si no esta vacio y puede ser un nombre de carpeta:
        /// letras, digitos, guion y guion bajo, hasta 24 caracteres. Se rechazan
        /// ademas los nombres reservados de Windows: pasarian este filtro pero
        /// Directory.CreateDirectory fallaria al dar de alta al sujeto, con el
        /// participante ya sentado delante.</summary>
        public static bool EsIdValido(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            s = s.Trim();
            if (s.Length == 0 || s.Length > 24) return false;
            foreach (char c in s)
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-') return false;
            foreach (string r in ReservadosWindows)
                if (string.Equals(s, r, System.StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }

        /// <summary>Da de alta al sujeto, calcula el numero de sesion y anexa la fila
        /// a sujetos.csv. Devuelve la sesion viva.</summary>
        public static SesionSujeto Registrar(string id, int edad, string mano,
                                             string experiencia, string condicion, string notas)
        {
            var s = Asegurar();
            s.id          = EsIdValido(id) ? id.Trim() : IdAnonimo;
            s.edad        = Mathf.Clamp(edad, 0, 120);
            s.mano        = string.IsNullOrEmpty(mano) ? "diestro" : mano;
            s.experiencia = string.IsNullOrEmpty(experiencia) ? "ninguna" : experiencia;
            s.condicion   = string.IsNullOrEmpty(condicion) ? "sin_guia" : condicion;
            s.notas       = notas ?? "";
            s.sesion      = SesionesPrevias(s.id) + 1;
            s.AnexarAlta();
            Debug.Log("[Sujeto] " + s.id + " · sesion " + s.sesion + " · " + s.condicion);
            return s;
        }

        /// <summary>Modo libre: los datos van a datos/ANON/ y el analisis los excluye
        /// por omision. Es para probar el equipo sin ensuciar el conjunto real.</summary>
        public static SesionSujeto RegistrarAnonimo()
        {
            var s = Asegurar();
            s.id = IdAnonimo;
            s.sesion = 0;
            s.condicion = "sin_guia";
            s.notas = "modo libre";
            Debug.Log("[Sujeto] modo libre (ANON): los datos no entran en el analisis.");
            return s;
        }

        private static SesionSujeto Asegurar()
        {
            if (Actual != null) return Actual;
            var go = new GameObject("SesionSujeto");
            var s = go.AddComponent<SesionSujeto>();
            DontDestroyOnLoad(go);
            Actual = s;
            return s;
        }

        void Awake()
        {
            // Si alguien deja un SesionSujeto en una escena, gana el que ya vivia.
            if (Actual != null && Actual != this) { Destroy(gameObject); return; }
            Actual = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>Cuenta cuantas veces aparece este ID en sujetos.csv. Contar el
        /// archivo en vez de llevar un contador en memoria es lo que hace que
        /// reanudar el trabajo otro dia numere la sesion correctamente.</summary>
        public static int SesionesPrevias(string id)
        {
            string ruta = Path.Combine(RegistroCsv.CarpetaRaiz, ArchivoSujetos);
            if (!File.Exists(ruta)) return 0;
            int n = 0;
            bool primera = true;
            foreach (string linea in File.ReadAllLines(ruta))
            {
                if (primera) { primera = false; continue; }   // la cabecera no es un sujeto
                int coma = linea.IndexOf(',');
                if (coma <= 0) continue;
                if (linea.Substring(0, coma).Trim() == id) n++;
            }
            return n;
        }

        private void AnexarAlta()
        {
            if (id == IdAnonimo) return;      // el modo libre no se da de alta
            var registro = new RegistroCsv(CabeceraSujetos);
            registro.Fila(id,
                          System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                          edad,
                          mano,
                          experiencia,
                          sesion,
                          RegistroCsv.EscaparTexto(notas));
            registro.GuardarEn(RegistroCsv.CarpetaRaiz, ArchivoSujetos, true);
        }

        /// <summary>Linea corta para el HUD y el menu.</summary>
        public string Etiqueta
        {
            get
            {
                return id + " · sesion " + sesion.ToString(CultureInfo.InvariantCulture) +
                       " · " + condicion;
            }
        }
    }
}
