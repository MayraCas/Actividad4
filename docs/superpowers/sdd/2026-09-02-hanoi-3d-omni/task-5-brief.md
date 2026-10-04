### Task 5: `ConfiguracionHanoi` y `hanoi.json`

**Files:**
- Create: `Assets/Scripts/Ambientes/A4/ConfiguracionHanoi.cs`
- Create: `Assets/StreamingAssets/hanoi.json`

**Interfaces:**
- Consumes: `EstadoHanoi.MinDiscos`, `EstadoHanoi.MaxDiscos` (tarea 1).
- Produces:
  - `class NivelHanoi { string nombre; int discos; bool guiaPermitida; }`
  - `class ConfiguracionHanoi` con los campos del JSON y:
    - `static ConfiguracionHanoi Cargar()`
    - `static ConfiguracionHanoi PorDefecto()`
    - `void Sanear()`
    - `float MasaDe(int indice, int discos)`
    - `float RadioDe(int indice, int discos)`
    - `NivelHanoi Nivel(int i)`
    - `const float RadioMin = 0.13f`, `const float RadioMax = 0.38f`, `const float GrosorDisco = 0.095f`

- [ ] **Paso 1: Escribir el JSON**

`Assets/StreamingAssets/hanoi.json`:

```json
{
  "niveles": [
    {"nombre": "Facil",   "discos": 3, "guiaPermitida": true},
    {"nombre": "Medio",   "discos": 4, "guiaPermitida": true},
    {"nombre": "Dificil", "discos": 5, "guiaPermitida": false},
    {"nombre": "Experto", "discos": 6, "guiaPermitida": false}
  ],
  "masaMin": 0.06,
  "masaMax": 0.28,
  "amortiguamientoPorKg": 0.008,
  "retardoPorKg": 0.35,
  "rigidezDisco": 0.20,
  "friccionDisco": 0.25,
  "rigidezPoste": 0.18,
  "bloqueoHaptico": true,
  "rigidezRechazo": 0.22,
  "limiteTiempoS": 0
}
```

- [ ] **Paso 2: Escribir `ConfiguracionHanoi`**

`Assets/Scripts/Ambientes/A4/ConfiguracionHanoi.cs`:

```csharp
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
            foreach (var n in niveles)
            {
                if (n == null) continue;
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
```

- [ ] **Paso 3: Verificar la carga y la degradación**

Ejecutar por MCP (`Unity_RunCommand`):

```csharp
var c = HapticOmni.ConfiguracionHanoi.Cargar();
UnityEngine.Debug.Log(c.niveles.Length + " niveles · masa disco 0 de 3 = " +
                      c.MasaDe(0,3) + " · masa disco 2 de 3 = " + c.MasaDe(2,3));
```

Esperado en consola: `4 niveles · masa disco 0 de 3 = 0.06 · masa disco 2 de 3 = 0.28`.

Luego renombrar temporalmente el JSON y repetir:

```bash
cd "C:/Users/76905/Downloads/ReportesOMNI/Actividad4/Assets/StreamingAssets"
mv hanoi.json hanoi.json.off
```

Esperado: un `LogWarning` con "No encuentro ... Uso los valores por defecto" y
los mismos 4 niveles. Restaurar:

```bash
mv hanoi.json.off hanoi.json
```

- [ ] **Paso 4: Punto de control**

- `Application.streamingAssetsPath` resuelve dentro del proyecto y el JSON se lee.
- Sin archivo, con archivo corrupto y con archivo sin niveles, la carga devuelve los valores por defecto y avisa.
- `MasaDe` y `RadioDe` dan los extremos correctos en los índices 0 y `n−1`.

---

