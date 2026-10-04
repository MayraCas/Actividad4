// ConfiguracionLaberinto.cs -- Los parametros del ambiente 2, fuera del codigo.
//
// La dificultad del laberinto no es una constante compilada: vive en
// StreamingAssets/laberinto.json para que el protocolo sea reproducible (el
// archivo dice exactamente con que se corrio) y ajustable sin reabrir Unity.
//
// Regla de oro, la misma que en el ambiente 4: el ambiente NUNCA deja de
// arrancar por culpa del archivo. Si falta, no parsea o trae valores absurdos,
// se avisa en consola y se usan estos mismos valores compilados.
//
// Las dos medidas de dificultad crecen juntas a lo largo de la escalera: el
// camino optimo se alarga (6.9 -> 16.2 u) y la holgura de la bola dentro del
// pasillo se estrecha (3.2 -> 1.6 mm reales). Las semillas no son arbitrarias:
// se eligieron barriendo un rango y quedandose con la que daba un camino
// optimo creciente, porque con una semilla cualquiera mas celdas NO garantiza
// un recorrido mas largo.
using System.IO;
using UnityEngine;

namespace HapticOmni
{
    [System.Serializable]
    public class NivelLaberinto
    {
        public string nombre = "Normal";
        public int columnas = 9;
        public int filas = 6;
        [Tooltip("Ancho del pasillo en unidades de Unity.")]
        public float anchoPasillo = 0.34f;
        public float grosorMuro = 0.06f;
        [Tooltip("Radio de la bola. La dificultad es (anchoPasillo - 2*radioBola)/2.")]
        public float radioBola = 0.11f;
        [Tooltip("Fija por nivel: todos los sujetos recorren el mismo mapa.")]
        public int semilla = 1024;
    }

    [System.Serializable]
    public class ConfiguracionLaberinto
    {
        public const string NombreArchivo = "laberinto.json";

        /// <summary>Limites de tamano del tablero. El maximo no es caprichoso: un
        /// laberinto mas ancho que el espacio util del Touch tendria esquinas a las
        /// que el brazo no llega.</summary>
        public const int MinCeldas = 4;
        public const int MaxCeldas = 20;

        /// <summary>Holgura minima por lado, en unidades. 0.02 u son 0.8 mm reales:
        /// por debajo de eso el temblor de la mano domina la medida y el nivel deja
        /// de medir destreza.</summary>
        public const float HolguraMinima = 0.02f;

        public NivelLaberinto[] niveles;
        public float rigidezMuro = 0.20f;
        public float friccionMuro = 0.12f;
        public float rigidezRanura = 0.22f;
        [Tooltip("Los muros son altos a proposito: la ranura que sujeta la bola es " +
                 "fuerza, no pared, y tirando del brazo se le pueden sacar unos 15 mm " +
                 "a la punta antes de saturar el motor.")]
        public float profundidadMuro = 0.60f;
        [Tooltip("Aire que la camara deja alrededor del tablero. 1.12 = 12 %.")]
        public float margenCamara = 1.12f;
        public float limiteTiempoS = 0f;

        public int Cuantos { get { return niveles != null ? niveles.Length : 0; } }

        public static ConfiguracionLaberinto PorDefecto()
        {
            return new ConfiguracionLaberinto
            {
                niveles = new[]
                {
                    new NivelLaberinto { nombre = "Amplio",   columnas =  7, filas =  5, anchoPasillo = 0.420f, grosorMuro = 0.070f, radioBola = 0.130f, semilla = 3088 },
                    new NivelLaberinto { nombre = "Normal",   columnas =  9, filas =  6, anchoPasillo = 0.340f, grosorMuro = 0.060f, radioBola = 0.110f, semilla = 1024 },
                    new NivelLaberinto { nombre = "Estrecho", columnas = 11, filas =  8, anchoPasillo = 0.270f, grosorMuro = 0.050f, radioBola = 0.085f, semilla = 1016 },
                    new NivelLaberinto { nombre = "Fino",     columnas = 12, filas =  9, anchoPasillo = 0.260f, grosorMuro = 0.045f, radioBola = 0.085f, semilla = 1052 },
                    new NivelLaberinto { nombre = "Extremo",  columnas = 13, filas = 10, anchoPasillo = 0.250f, grosorMuro = 0.045f, radioBola = 0.085f, semilla = 1060 },
                }
            };
        }

        public static ConfiguracionLaberinto Cargar()
        {
            string ruta = Path.Combine(Application.streamingAssetsPath, NombreArchivo);
            try
            {
                if (!File.Exists(ruta))
                {
                    Debug.LogWarning("[Laberinto] No encuentro " + ruta + ". Uso los valores por defecto.");
                    return PorDefecto();
                }
                var c = JsonUtility.FromJson<ConfiguracionLaberinto>(File.ReadAllText(ruta));
                if (c == null || c.niveles == null || c.niveles.Length == 0)
                {
                    Debug.LogWarning("[Laberinto] " + NombreArchivo +
                                     " no trae niveles. Uso los valores por defecto.");
                    return PorDefecto();
                }
                c.Sanear();
                return c;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Laberinto] No pude leer " + NombreArchivo + " (" + e.Message +
                                 "). Uso los valores por defecto.");
                return PorDefecto();
            }
        }

        /// <summary>Holgura por lado de la bola dentro del pasillo, en unidades.
        /// Es LA medida de dificultad de este ambiente.</summary>
        public static float HolguraU(NivelLaberinto n)
        {
            if (n == null) return 0f;
            return (n.anchoPasillo - 2f * n.radioBola) * 0.5f;
        }

        /// <summary>Recorta todo a rangos en los que el dispositivo y el brazo tienen
        /// sentido. Un JSON mal puesto degrada la configuracion, no rompe el ambiente.</summary>
        public void Sanear()
        {
            if (niveles == null || niveles.Length == 0) niveles = PorDefecto().niveles;
            for (int i = 0; i < niveles.Length; i++)
            {
                // Reponer en vez de saltar: si se dejase el hueco nulo, Nivel(i) se lo
                // devolveria al llamador y reventaria al leer sus campos.
                if (niveles[i] == null) niveles[i] = new NivelLaberinto();
                var n = niveles[i];

                if (string.IsNullOrEmpty(n.nombre)) n.nombre = "Nivel";
                n.columnas = Mathf.Clamp(n.columnas, MinCeldas, MaxCeldas);
                n.filas    = Mathf.Clamp(n.filas,    MinCeldas, MaxCeldas);
                n.anchoPasillo = Mathf.Clamp(n.anchoPasillo, 0.10f, 1.00f);
                n.grosorMuro   = Mathf.Clamp(n.grosorMuro,   0.02f, 0.30f);
                n.radioBola    = Mathf.Clamp(n.radioBola,    0.02f, 0.40f);

                // Si el pasillo no deja hueco suficiente, se encoge la BOLA y no el
                // pasillo: el trazado es lo que define el nivel, y cambiarlo aqui
                // haria que el laberinto no coincidiese con el que dice la semilla.
                float maxRadio = (n.anchoPasillo - 2f * HolguraMinima) * 0.5f;
                if (maxRadio < 0.02f) maxRadio = 0.02f;
                if (n.radioBola > maxRadio)
                {
                    Debug.LogWarning("[Laberinto] El nivel '" + n.nombre + "' deja la bola sin " +
                                     "hueco en el pasillo; le bajo el radio de " +
                                     n.radioBola.ToString("F3") + " a " + maxRadio.ToString("F3") + ".");
                    n.radioBola = maxRadio;
                }
            }

            rigidezMuro     = Mathf.Clamp(rigidezMuro,     0.01f, 0.25f);
            friccionMuro    = Mathf.Clamp01(friccionMuro);
            rigidezRanura   = Mathf.Clamp(rigidezRanura,   0.01f, 0.25f);
            profundidadMuro = Mathf.Clamp(profundidadMuro, 0.10f, 1.50f);
            margenCamara    = Mathf.Clamp(margenCamara,    1.00f, 2.00f);
            limiteTiempoS   = Mathf.Max(0f, limiteTiempoS);
        }

        /// <summary>El nivel en el indice dado, saturado. OJO: `i` es 0-based,
        /// mientras que el numero de nivel que ve el usuario es 1-based.</summary>
        public NivelLaberinto Nivel(int i)
        {
            if (niveles == null || niveles.Length == 0) return new NivelLaberinto();
            return niveles[Mathf.Clamp(i, 0, niveles.Length - 1)];
        }
    }
}
