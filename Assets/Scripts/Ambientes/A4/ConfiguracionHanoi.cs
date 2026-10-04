// ConfiguracionHanoi.cs -- Los parametros del ambiente 4, fuera del codigo.
//
// La dificultad de este ambiente no es una constante compilada: vive en
// StreamingAssets/hanoi.json para que el protocolo sea reproducible (el archivo
// dice exactamente con que se corrio) y ajustable sin reabrir Unity.
//
// Regla de oro: el ambiente NUNCA deja de arrancar por culpa del archivo. Si
// falta, no parsea o trae valores absurdos, se avisa en consola y se usan estos
// mismos valores compilados.
using System.IO;
using UnityEngine;

namespace HapticOmni
{
    [System.Serializable]
    public class NivelHanoi
    {
        public string nombre = "Facil";
        public int discos = 3;
        public bool guiaPermitida = true;
    }

    [System.Serializable]
    public class ConfiguracionHanoi
    {
        public const string NombreArchivo = "hanoi.json";

        /// <summary>Radio del disco mas pequeno y del mas grande, en unidades de
        /// Unity. Fijos: son geometria del escenario, no del protocolo.</summary>
        public const float RadioMin = 0.13f;
        public const float RadioMax = 0.38f;
        /// <summary>Grosor de un disco; tambien el paso vertical de la pila.</summary>
        public const float GrosorDisco = 0.095f;
        /// <summary>Tope duro por debajo de los 3 N que puede dar el Touch.</summary>
        public const float MasaTope = 0.30f;

        public NivelHanoi[] niveles;
        public float masaMin = 0.06f;
        public float masaMax = 0.28f;
        public float amortiguamientoPorKg = 0.008f;
        public float retardoPorKg = 0.35f;
        public float rigidezDisco = 0.20f;
        public float friccionDisco = 0.25f;
        public float rigidezPoste = 0.18f;
        public bool  bloqueoHaptico = true;
        public float rigidezRechazo = 0.22f;
        public float limiteTiempoS = 0f;

        public static ConfiguracionHanoi PorDefecto()
        {
            return new ConfiguracionHanoi
            {
                niveles = new[]
                {
                    new NivelHanoi { nombre = "Facil",   discos = 3, guiaPermitida = true  },
                    new NivelHanoi { nombre = "Medio",   discos = 4, guiaPermitida = true  },
                    new NivelHanoi { nombre = "Dificil", discos = 5, guiaPermitida = false },
                    new NivelHanoi { nombre = "Experto", discos = 6, guiaPermitida = false },
                }
            };
        }

        public static ConfiguracionHanoi Cargar()
        {
            string ruta = Path.Combine(Application.streamingAssetsPath, NombreArchivo);
            try
            {
                if (!File.Exists(ruta))
                {
                    Debug.LogWarning("[Hanoi] No encuentro " + ruta + ". Uso los valores por defecto.");
                    return PorDefecto();
                }
                var c = JsonUtility.FromJson<ConfiguracionHanoi>(File.ReadAllText(ruta));
                if (c == null || c.niveles == null || c.niveles.Length == 0)
                {
                    Debug.LogWarning("[Hanoi] " + NombreArchivo +
                                     " no trae niveles. Uso los valores por defecto.");
                    return PorDefecto();
                }
                c.Sanear();
                return c;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Hanoi] No pude leer " + NombreArchivo + " (" + e.Message +
                                 "). Uso los valores por defecto.");
                return PorDefecto();
            }
        }

        /// <summary>Recorta todo a rangos en los que el dispositivo y el escenario
        /// tienen sentido. Un JSON mal puesto degrada la configuracion, no rompe
        /// el ambiente.</summary>
        public void Sanear()
        {
            if (niveles == null || niveles.Length == 0) niveles = PorDefecto().niveles;
            for (int i = 0; i < niveles.Length; i++)
            {
                // Reponer en vez de saltar: si se dejase el hueco nulo, Nivel(i) se
                // lo devolveria al llamador y reventaria en `nivel.discos`, que es
                // justo lo contrario de lo que promete esta clase. Hoy JsonUtility
                // nunca deja nulos en un arreglo de clases serializables, asi que
                // esto es cinturon y tirantes, no un camino vivo.
                if (niveles[i] == null) niveles[i] = new NivelHanoi();
                var n = niveles[i];
                if (string.IsNullOrEmpty(n.nombre)) n.nombre = "Nivel";
                n.discos = Mathf.Clamp(n.discos, EstadoHanoi.MinDiscos, EstadoHanoi.MaxDiscos);
            }
            masaMin = Mathf.Clamp(masaMin, 0.02f, MasaTope);
            masaMax = Mathf.Clamp(masaMax, masaMin, MasaTope);
            amortiguamientoPorKg = Mathf.Clamp(amortiguamientoPorKg, 0f, 0.05f);
            retardoPorKg    = Mathf.Clamp(retardoPorKg, 0.05f, 2f);
            rigidezDisco    = Mathf.Clamp(rigidezDisco, 0.01f, 0.25f);
            friccionDisco   = Mathf.Clamp01(friccionDisco);
            rigidezPoste    = Mathf.Clamp(rigidezPoste, 0.01f, 0.25f);
            rigidezRechazo  = Mathf.Clamp(rigidezRechazo, 0.01f, 0.25f);
            limiteTiempoS   = Mathf.Max(0f, limiteTiempoS);
        }

        public NivelHanoi Nivel(int i)
        {
            if (niveles == null || niveles.Length == 0) return new NivelHanoi();
            return niveles[Mathf.Clamp(i, 0, niveles.Length - 1)];
        }

        /// <summary>Masa del disco `indice` (0 = el mas pequeno) en un juego de
        /// `discos` piezas. Reparto lineal entre masaMin y masaMax: con 3 discos los
        /// saltos son groseros y se distinguen sin pensar; con 7 son finos y
        /// discriminar el peso pasa a ser parte de la dificultad.</summary>
        public float MasaDe(int indice, int discos)
        {
            if (discos <= 1) return masaMax;
            float t = Mathf.Clamp01((float)indice / (discos - 1));
            return Mathf.Lerp(masaMin, masaMax, t);
        }

        public float RadioDe(int indice, int discos)
        {
            if (discos <= 1) return RadioMax;
            float t = Mathf.Clamp01((float)indice / (discos - 1));
            return Mathf.Lerp(RadioMin, RadioMax, t);
        }
    }
}
