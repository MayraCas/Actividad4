# Ambiente 2: niveles seleccionables, mapa mayor y datos por nivel — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que el laberinto tenga cinco niveles elegibles dentro de la escena, con un mapa que llega a 13×10 celdas, y que cada ensayo guarde la geometría con la que se jugó.

**Architecture:** El tablero deja de estar cocido en el archivo de escena y pasa a construirse en tiempo de ejecución. `ConstruirA2` crea sólo el armazón (dispositivo, cámara, materiales, HUD, panel) y un `ConstructorLaberinto` levanta muros, ranura, salida y meta cada vez que se aplica un nivel. Los cinco niveles viven en `Assets/StreamingAssets/laberinto.json`. Es el patrón que ya pasó revisión en el ambiente 4.

**Tech Stack:** Unity 6, URP 17.3, Input System 1.18, `JsonUtility`, IMGUI (`OnGUI`); Python 3 con numpy y matplotlib (sin pandas).

**Spec:** `docs/superpowers/specs/2026-09-03-laberinto-niveles-design.md`

## Global Constraints

- **El proyecto Unity correcto es `C:\Users\76905\Downloads\ReportesOMNI\Actividad4`.** NUNCA tocar `My project (3)`, que es la Actividad 3 ya entregada. Confirmar con `Application.dataPath` antes de escribir nada.
- **`ReportesOMNI` no es un repositorio git.** Donde este plan diría «commit», hágase en su lugar un **punto de control**: `diff` contra el snapshot previo a la tarea para confirmar que sólo cambiaron los archivos declarados. No hay `git add` ni `git commit`.
- **No hay tests unitarios posibles para este trabajo.** `Laberinto` y `ConfiguracionLaberinto` usan `UnityEngine` (`Vector2`, `Mathf`), así que viven en `Assembly-CSharp`, y una asmdef de tests no puede referenciar un ensamblado predefinido. La verificación es por **llamada directa vía `Unity_RunCommand`** y por **archivos en disco**, igual que en las tareas 2–11 del trabajo anterior.
- **En Play mode vía MCP no avanzan los frames** (`Application.runInBackground = false`, el editor sin foco). Evidencia del tipo «no hubo excepciones en `OnGUI`» es **débil**. Lo que vale: llamadas directas a métodos, archivos en disco, aritmética de layout y razonamiento estático.
- **`Unity_RunCommand` reporta `UNEXPECTED_ERROR` ante cualquier `Debug.LogWarning`.** Es esperable en las pruebas de degradación; no es un fallo real.
- **La reflexión está prohibida** en los scripts de `Unity_RunCommand`: el puente la rechaza. Verificar por API pública.
- **Techo del dispositivo:** 3 N de fuerza y 0.25 N/mm de rigidez. Por encima, el lazo a 1 kHz entra en ciclo límite.
- **Escala:** `unitsPerMillimeter = 0.025`. El espacio útil del Touch es ±2.00 × ±1.50 × ±0.875 unidades.
- **`nivel` es 1-based en todo lo que ve una persona** (el campo público, el HUD, el panel y la columna del CSV) y **0-based en `ConfiguracionLaberinto.Nivel(i)`**. La conversión ocurre en un único sitio, la propiedad `NivelActual` de `Ambiente2Laberinto`. Ningún otro código resta uno.
- **No tocar** los ambientes 1, 3 y 4, `Laberinto.cs`, `comun.py`, el reporte LaTeX (`src/`), ni ejecutar `Haptics > Capturar imagenes`.
- **Codificación:** los `.cs`, `.json` y `.py` van en UTF-8 sin BOM y sin acentos en el código (el proyecto usa identificadores y comentarios sin tildes). Los `.md` sí llevan acentos.

## Estructura de archivos

| Archivo | Responsabilidad |
|---|---|
| `Assets/StreamingAssets/laberinto.json` | Los cinco niveles y los parámetros hápticos. Editable sin abrir Unity. |
| `Assets/Scripts/Ambientes/A2/ConfiguracionLaberinto.cs` | Leer, sanear y servir esa configuración. Nada de geometría. |
| `Assets/Scripts/Ambientes/A2/ConstructorLaberinto.cs` | Construir y destruir el tablero. Nada de reglas ni de métricas. |
| `Assets/Scripts/Ambientes/A2/Ambiente2Laberinto.cs` | Reglas, métricas y CSV. Delega la geometría al constructor. |
| `Assets/Scripts/UI/PanelConfigLaberinto.cs` | Elegir el nivel. Nada más. |
| `Assets/Editor/ConstruirAmbientes.cs` | Crear el armazón de la escena. Ya no construye el laberinto. |
| `Analysis/analizar_a2.py` | Agrupar por nivel y describir la dificultad. |

---

### Task 1: `ConfiguracionLaberinto` y `laberinto.json`

**Files:**
- Create: `Assets/StreamingAssets/laberinto.json`
- Create: `Assets/Scripts/Ambientes/A2/ConfiguracionLaberinto.cs`

**Interfaces:**
- Consumes: nada de tareas anteriores.
- Produces:
  - `class NivelLaberinto { string nombre; int columnas, filas; float anchoPasillo, grosorMuro, radioBola; int semilla; }`
  - `class ConfiguracionLaberinto` con:
    - `static ConfiguracionLaberinto Cargar()`
    - `static ConfiguracionLaberinto PorDefecto()`
    - `void Sanear()`
    - `NivelLaberinto Nivel(int i)` — **i es 0-based**
    - `int Cuantos { get; }`
    - `static float HolguraU(NivelLaberinto n)` — holgura por lado, en unidades
    - campos `rigidezMuro`, `friccionMuro`, `rigidezRanura`, `profundidadMuro`, `margenCamara`, `limiteTiempoS`
    - `const int MinCeldas = 4, MaxCeldas = 20`
    - `const float HolguraMinima = 0.02f`

- [ ] **Paso 1: Escribir el JSON**

`Assets/StreamingAssets/laberinto.json`:

```json
{
  "niveles": [
    {"nombre": "Amplio",   "columnas":  7, "filas":  5, "anchoPasillo": 0.420, "grosorMuro": 0.070, "radioBola": 0.130, "semilla": 3088},
    {"nombre": "Normal",   "columnas":  9, "filas":  6, "anchoPasillo": 0.340, "grosorMuro": 0.060, "radioBola": 0.110, "semilla": 1024},
    {"nombre": "Estrecho", "columnas": 11, "filas":  8, "anchoPasillo": 0.270, "grosorMuro": 0.050, "radioBola": 0.085, "semilla": 1016},
    {"nombre": "Fino",     "columnas": 12, "filas":  9, "anchoPasillo": 0.260, "grosorMuro": 0.045, "radioBola": 0.085, "semilla": 1052},
    {"nombre": "Extremo",  "columnas": 13, "filas": 10, "anchoPasillo": 0.250, "grosorMuro": 0.045, "radioBola": 0.085, "semilla": 1060}
  ],
  "rigidezMuro": 0.20,
  "friccionMuro": 0.12,
  "rigidezRanura": 0.22,
  "profundidadMuro": 0.60,
  "margenCamara": 1.12,
  "limiteTiempoS": 0
}
```

- [ ] **Paso 2: Escribir `ConfiguracionLaberinto`**

`Assets/Scripts/Ambientes/A2/ConfiguracionLaberinto.cs`:

```csharp
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
```

- [ ] **Paso 3: Verificar la carga y la escalera**

Ejecutar por MCP (`Unity_RunCommand`):

```csharp
using UnityEngine;
using UnityEditor;
using HapticOmni;
using System.Text;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        result.Log("dataPath = " + Application.dataPath);
        var c = ConfiguracionLaberinto.Cargar();
        var sb = new StringBuilder();
        for (int i = 0; i < c.Cuantos; i++)
        {
            var n = c.Nivel(i);
            var lab = Laberinto.Generar(n.columnas, n.filas, n.anchoPasillo, n.grosorMuro, n.semilla);
            var cam = lab.Resolver(new Vector2Int(0, 0),
                                   new Vector2Int(lab.columnas - 1, lab.filas - 1));
            float largo = 0f;
            for (int k = 1; k < cam.Count; k++)
                largo += Vector2.Distance(lab.CentroCelda(cam[k].x, cam[k].y),
                                          lab.CentroCelda(cam[k - 1].x, cam[k - 1].y));
            sb.Append(string.Format("  nivel {0} {1,-9} {2,2}x{3,-2} holgura {4:F2} mm  camino {5,5:F2} u  {6,2} barras  tablero {7:F2}x{8:F2}\n",
                      i + 1, n.nombre, n.columnas, n.filas,
                      ConfiguracionLaberinto.HolguraU(n) / 0.025f, largo,
                      lab.Barras().Count, lab.AnchoTablero, lab.AltoTablero));
        }
        result.Log(c.Cuantos + " niveles:\n" + sb);
    }
}
```

Esperado, exactamente:

```
  nivel 1 Amplio     7x5  holgura 3.20 mm  camino  6.86 u  22 barras  tablero 3.50x2.52
  nivel 2 Normal     9x6  holgura 2.40 mm  camino  8.40 u  37 barras  tablero 3.66x2.46
  nivel 3 Estrecho  11x8  holgura 2.00 mm  camino 11.20 u  41 barras  tablero 3.57x2.61
  nivel 4 Fino      12x9  holgura 1.80 mm  camino 13.73 u  58 barras  tablero 3.71x2.79
  nivel 5 Extremo   13x10 holgura 1.60 mm  camino 16.23 u  73 barras  tablero 3.88x3.00
```

Si alguna cifra no coincide, **el fallo está en el JSON, no en el esperado**: estas cifras se midieron sobre `Laberinto.Generar` antes de escribir el plan.

- [ ] **Paso 4: Verificar la degradación y la saturación**

Ejecutar tres veces, renombrando el archivo entre medias:

Entre cada `mv`/`echo` del shell, ejecutar por MCP esta comprobacion:

```csharp
using UnityEngine;
using UnityEditor;
using HapticOmni;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var c = ConfiguracionLaberinto.Cargar();
        result.Log("niveles=" + c.Cuantos + " primero='" + c.Nivel(0).nombre +
                   "' ultimo='" + c.Nivel(c.Cuantos - 1).nombre + "'");
    }
}
```

Los tres estados del archivo:

```bash
cd "C:/Users/76905/Downloads/ReportesOMNI/Actividad4/Assets/StreamingAssets"

mv laberinto.json laberinto.json.off        # 1) ausente      -> ejecutar la comprobacion
printf '{ "niveles": [' > laberinto.json    # 2) JSON corrupto -> ejecutar la comprobacion
printf '{"niveles":[]}' > laberinto.json    # 3) sin niveles   -> ejecutar la comprobacion

rm -f laberinto.json
mv laberinto.json.off laberinto.json        # restaurar
```

En los tres casos la salida debe ser `niveles=5 primero='Amplio' ultimo='Extremo'`,
acompanada del `LogWarning` correspondiente:

1. `[Laberinto] No encuentro ...laberinto.json. Uso los valores por defecto.`
2. `[Laberinto] No pude leer laberinto.json (...). Uso los valores por defecto.`
3. `[Laberinto] laberinto.json no trae niveles. Uso los valores por defecto.`

Los tres casos deben devolver los 5 niveles por defecto con su `LogWarning`.
(`Unity_RunCommand` devolverá `UNEXPECTED_ERROR` por el warning: es lo esperado.)

Y la saturación, sobre instancias en memoria:

```csharp
var a = ConfiguracionLaberinto.PorDefecto();
a.niveles[0].columnas = 99; a.niveles[0].filas = 1;
a.niveles[1].radioBola = 0.30f;      // no cabe en un pasillo de 0.34
a.niveles[2] = null;
a.Sanear();
result.Log("columnas 99->" + a.Nivel(0).columnas + " filas 1->" + a.Nivel(0).filas);
result.Log("radioBola 0.30 en pasillo 0.34 -> " + a.Nivel(1).radioBola.ToString("F3") +
           " (esperado 0.150, que deja la holgura minima de 0.02)");
result.Log("nivel nulo repuesto: Nivel(2)==null? " + (a.Nivel(2) == null) +
           " nombre='" + a.Nivel(2).nombre + "'");
```

Esperado: `columnas 99->20`, `filas 1->4`, `radioBola -> 0.150`, `Nivel(2)==null? False`.

- [ ] **Paso 5: Punto de control**

- `Application.dataPath` apunta a `Actividad4`.
- Los cinco niveles cargan del archivo y sus cifras coinciden con la tabla.
- Los tres caminos de fallo degradan a los valores por defecto con aviso.
- `Sanear` satura tamaños y corrige una bola que no cabe encogiendo la bola, no el pasillo.
- `diff` contra el snapshot: sólo `ConfiguracionLaberinto.cs` (+`.meta`) y `laberinto.json` (+`.meta`).

---

### Task 2: `ConstructorLaberinto`

**Files:**
- Create: `Assets/Scripts/Ambientes/A2/ConstructorLaberinto.cs`

**Interfaces:**
- Consumes: `NivelLaberinto`, `ConfiguracionLaberinto` (tarea 1); `Laberinto`, `BarraMuro`, `HapticShape`, `TipoForma`, `HapticDevice` (ya existen).
- Produces:
  - `class ConstructorLaberinto : MonoBehaviour` con campos públicos `raizTablero`, `camara`, `dispositivo`, `cursor`, `matPlaca`, `matMuro`, `matSalida`, `matMeta`
  - `void Construir(NivelLaberinto n, ConfiguracionLaberinto cfg)`
  - propiedades de sólo lectura `Laberinto Actual`, `Transform Salida`, `Transform Meta`, `HapticShape[] Muros`, `Vector3[] CaminoOptimo`, `float LongitudOptima`

- [ ] **Paso 1: Escribir `ConstructorLaberinto`**

`Assets/Scripts/Ambientes/A2/ConstructorLaberinto.cs`:

```csharp
// ConstructorLaberinto.cs -- Levanta el tablero en tiempo de ejecucion.
//
// Antes esto lo hacia el generador de escena del editor y el laberinto quedaba
// cocido en el archivo .unity, de modo que la escena solo podia contener un
// nivel. Al traerlo a runtime, una sola escena sirve para los cinco y la
// geometria pasa a ser configurable desde laberinto.json.
//
// El detalle que decide el diseno: EstiloAmbientes vive en Assets/Editor y NO
// existe en runtime. Por eso esta clase no puede pedirle materiales; el armazon
// que construye el editor se los deja cableados en los campos de abajo. Esos
// materiales si son assets del proyecto (EstiloAmbientes.Mat termina en
// Guardar), asi que la referencia sobrevive a guardar y recargar la escena.
//
// Esta clase no sabe nada de reglas ni de metricas: construye, mide el camino
// optimo y devuelve las piezas. Quien juega es Ambiente2Laberinto.
using System.Collections.Generic;
using UnityEngine;

namespace HapticOmni
{
    [AddComponentMenu("Haptics/A2 - Constructor del laberinto")]
    public class ConstructorLaberinto : MonoBehaviour
    {
        [Header("Cableado por el generador de escena")]
        [Tooltip("Padre de todo lo que se destruye y se recrea al cambiar de nivel.")]
        public Transform raizTablero;
        public Camera camara;
        public HapticDevice dispositivo;
        [Tooltip("La bola es la propia punta del stylus: aqui solo se le ajusta el tamano.")]
        public Transform cursor;

        [Header("Materiales")]
        public Material matPlaca, matMuro, matSalida, matMeta;

        // ---- perfil en Z del tablero ---------------------------------------
        // Los muros son ALTOS a proposito. Lo que sujeta la bola en el plano del
        // tablero es la ranura, y una ranura es fuerza, no pared: el Touch satura
        // a ~3.3 N y la ranura tiene rigidez 0.22 N/mm, asi que tirando del brazo
        // se le pueden sacar a la punta ~15 mm reales -- unas 0.38 unidades con la
        // escala de estas escenas. Con muros bajos la bola se monta encima de las
        // paredes y se sale del laberinto.
        private const float ZTraseraMuro = 0.18f;
        private const float ZMarca       = -0.20f;
        private const float HuecoRanura  = 0.02f;

        public Laberinto Actual        { get; private set; }
        public Transform Salida        { get; private set; }
        public Transform Meta          { get; private set; }
        public HapticShape[] Muros     { get; private set; }
        public Vector3[] CaminoOptimo  { get; private set; }
        public float LongitudOptima    { get; private set; }

        /// <summary>Destruye el tablero anterior y levanta el del nivel dado.</summary>
        public void Construir(NivelLaberinto n, ConfiguracionLaberinto cfg)
        {
            if (n == null || cfg == null || raizTablero == null) return;

            Limpiar();

            Actual = Laberinto.Generar(n.columnas, n.filas, n.anchoPasillo, n.grosorMuro, n.semilla);

            float zCentroMuro = ZTraseraMuro - cfg.profundidadMuro * 0.5f;
            float zPlaca      = ZTraseraMuro + 0.03f;

            CrearPlaca(n, zPlaca);
            Muros = CrearMuros(n, cfg, zCentroMuro);
            CrearRanura(n, cfg);
            CrearMarcas(n);
            MedirCamino();
            AjustarBola(n);
            AjustarCamara(cfg);
        }

        /// <summary>Vacia el tablero. Las formas se apagan ANTES de destruirlas: si
        /// solo se llamase a Destroy, seguirian registradas en el dispositivo hasta
        /// el final del frame y durante ese frame convivirian los muros del nivel
        /// viejo con los del nuevo.</summary>
        private void Limpiar()
        {
            for (int i = raizTablero.childCount - 1; i >= 0; i--)
            {
                var hijo = raizTablero.GetChild(i);
                foreach (var f in hijo.GetComponentsInChildren<HapticShape>(true))
                    f.enabled = false;
                hijo.SetParent(null, false);   // sale del arbol ya, no al final del frame
                if (Application.isPlaying) Destroy(hijo.gameObject);
                else DestroyImmediate(hijo.gameObject);
            }
            Muros = new HapticShape[0];
            Salida = Meta = null;
            CaminoOptimo = new Vector3[0];
            LongitudOptima = 1f;
        }

        private GameObject Caja(string nombre, Vector3 pos, Vector3 escala, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                // No hay fisica en este ambiente: la bola es la punta del stylus y
                // quien la detiene es el servo loop, no un collider.
                if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            }
            go.transform.SetParent(raizTablero, false);
            go.transform.localPosition = pos;
            go.transform.localScale = escala;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private void CrearPlaca(NivelLaberinto n, float zPlaca)
        {
            var placa = Caja("Placa de fondo", new Vector3(0f, 0f, zPlaca),
                             new Vector3(Actual.AnchoTablero + 0.12f,
                                         Actual.AltoTablero + 0.12f, 0.06f), matPlaca);

            // El tiling depende del nivel, y matPlaca es un ASSET compartido:
            // escribirselo encima ensuciaria un archivo del proyecto cada vez que se
            // juega. Va por MaterialPropertyBlock, el mismo patron que HapticShape
            // usa para el color de contacto.
            var rend = placa.GetComponent<Renderer>();
            var bloque = new MaterialPropertyBlock();
            rend.GetPropertyBlock(bloque);
            bloque.SetVector("_BaseMap_ST", new Vector4(n.columnas, n.filas, 0f, 0f));
            bloque.SetVector("_MainTex_ST", new Vector4(n.columnas, n.filas, 0f, 0f));
            rend.SetPropertyBlock(bloque);
        }

        private HapticShape[] CrearMuros(NivelLaberinto n, ConfiguracionLaberinto cfg, float zCentro)
        {
            var barras = Actual.Barras();
            var formas = new List<HapticShape>(barras.Count);
            for (int i = 0; i < barras.Count; i++)
            {
                var b = barras[i];
                var go = Caja("Muro " + i,
                              new Vector3(b.centro.x, b.centro.y, zCentro),
                              new Vector3(b.tamano.x, b.tamano.y, cfg.profundidadMuro), matMuro);
                var f = go.AddComponent<HapticShape>();
                f.tipo = TipoForma.Caja;
                f.rigidez = cfg.rigidezMuro;
                f.friccion = cfg.friccionMuro;
                f.colorContacto = new Color(1f, 0.45f, 0.35f);
                formas.Add(f);
            }
            return formas.ToArray();
        }

        /// <summary>Dos semiespacios enfrentados que mantienen la bola en el plano del
        /// tablero: puede rodar por el laberinto pero no salirse hacia el frente ni
        /// hundirse en la placa.</summary>
        private void CrearRanura(NivelLaberinto n, ConfiguracionLaberinto cfg)
        {
            float sep = n.radioBola + HuecoRanura;
            Semiespacio("Fondo de la ranura", new Vector3(0, 0, -sep), Vector3.forward, cfg);
            Semiespacio("Tapa de la ranura",  new Vector3(0, 0,  sep), Vector3.back,    cfg);
        }

        private void Semiespacio(string nombre, Vector3 pos, Vector3 normal, ConfiguracionLaberinto cfg)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(raizTablero, false);
            go.transform.localPosition = pos;
            var f = go.AddComponent<HapticShape>();
            f.tipo = TipoForma.Semiespacio;
            f.normalLocal = normal;
            f.rigidez = cfg.rigidezRanura;
            f.friccion = 0f;
            f.prioritaria = true;   // siempre se envia: es la superficie de referencia
            f.colorContacto = new Color(0, 0, 0, 0);
        }

        /// <summary>Salida y meta. Van adelantadas dentro del canal (ZMarca) porque al
        /// fondo del pasillo los muros altos las taparian justo en las esquinas. Su Z
        /// no afecta a la logica: las distancias se miden en el plano.</summary>
        private void CrearMarcas(NivelLaberinto n)
        {
            Vector2 cIni = Actual.CentroCelda(0, 0);
            Vector2 cFin = Actual.CentroCelda(Actual.columnas - 1, Actual.filas - 1);
            float lado = n.anchoPasillo * 0.8f;

            Salida = Caja("Salida", new Vector3(cIni.x, cIni.y, ZMarca),
                          new Vector3(lado, lado, 0.02f), matSalida).transform;
            Meta   = Caja("Meta",   new Vector3(cFin.x, cFin.y, ZMarca),
                          new Vector3(lado, lado, 0.02f), matMeta).transform;
        }

        private void MedirCamino()
        {
            var celdas = Actual.Resolver(new Vector2Int(0, 0),
                                         new Vector2Int(Actual.columnas - 1, Actual.filas - 1));
            var camino = new Vector3[celdas.Count];
            float largo = 0f;
            for (int i = 0; i < celdas.Count; i++)
            {
                Vector2 c = Actual.CentroCelda(celdas[i].x, celdas[i].y);
                camino[i] = raizTablero.TransformPoint(new Vector3(c.x, c.y, 0f));
                if (i > 0) largo += Vector3.Distance(camino[i], camino[i - 1]);
            }
            CaminoOptimo = camino;
            LongitudOptima = Mathf.Max(1e-4f, largo);
        }

        private void AjustarBola(NivelLaberinto n)
        {
            if (dispositivo != null) dispositivo.probeRadius = n.radioBola;
            if (cursor != null) cursor.localScale = Vector3.one * Mathf.Max(0.06f, n.radioBola * 2f);
        }

        /// <summary>Coloca la camara para que el tablero quepa con el margen pedido.
        /// Se comprueban los DOS ejes y se toma el que obligue a alejarse mas: con una
        /// ventana estrecha manda el ancho, y con una apaisada manda el alto.</summary>
        private void AjustarCamara(ConfiguracionLaberinto cfg)
        {
            if (camara == null) return;
            float mediaV = Mathf.Tan(camara.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float aspecto = Mathf.Max(0.1f, camara.aspect);
            float mediaH = mediaV * aspecto;

            float zV = (Actual.AltoTablero  * 0.5f * cfg.margenCamara) / Mathf.Max(1e-4f, mediaV);
            float zH = (Actual.AnchoTablero * 0.5f * cfg.margenCamara) / Mathf.Max(1e-4f, mediaH);

            camara.transform.position = new Vector3(0f, 0f, -Mathf.Max(zV, zH));
            camara.transform.rotation = Quaternion.identity;
        }
    }
}
```

- [ ] **Paso 2: Verificar la construcción de los cinco niveles**

Ejecutar por MCP, **en modo edición** (el constructor funciona en los dos):

```csharp
using UnityEngine;
using UnityEditor;
using HapticOmni;
using System.Text;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var raiz = new GameObject("PruebaConstructor");
        var tablero = new GameObject("Tablero");
        tablero.transform.SetParent(raiz.transform, false);
        var camGO = new GameObject("Cam");
        camGO.transform.SetParent(raiz.transform, false);
        var cam = camGO.AddComponent<Camera>();
        cam.fieldOfView = 26f;
        var devGO = new GameObject("Dev");
        devGO.transform.SetParent(raiz.transform, false);
        var dev = devGO.AddComponent<HapticDevice>();
        var cursor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cursor.transform.SetParent(raiz.transform, false);

        var con = raiz.AddComponent<ConstructorLaberinto>();
        con.raizTablero = tablero.transform;
        con.camara = cam;
        con.dispositivo = dev;
        con.cursor = cursor.transform;

        var cfg = ConfiguracionLaberinto.Cargar();
        var sb = new StringBuilder();
        for (int i = 0; i < cfg.Cuantos; i++)
        {
            var n = cfg.Nivel(i);
            con.Construir(n, cfg);
            sb.Append(string.Format(
                "  nivel {0} {1,-9}: {2,2} muros  {3,2} hijos  camino {4,5:F2} u ({5,2} puntos)  " +
                "probeRadius {6:F3}  cursor {7:F3}  camara z {8,6:F2}\n",
                i + 1, n.nombre, con.Muros.Length, tablero.transform.childCount,
                con.LongitudOptima, con.CaminoOptimo.Length,
                dev.probeRadius, cursor.transform.localScale.x, cam.transform.position.z));
        }
        result.Log("Construccion de los cinco niveles:\n" + sb);

        // Volver al nivel 1 tras haber pasado por el 5: no deben quedar huerfanos
        con.Construir(cfg.Nivel(0), cfg);
        result.Log("De vuelta al nivel 1: " + con.Muros.Length + " muros, " +
                   tablero.transform.childCount + " hijos del tablero " +
                   "(esperado 22 muros y 26 hijos: 22 muros + placa + 2 de ranura + salida + meta)");

        DestroyImmediate(raiz);
    }
}
```

Esperado: los muros por nivel deben ser 22, 37, 41, 58, 73, y los hijos del tablero
siempre `muros + 5` (placa, dos semiespacios de ranura, salida y meta). El camino
debe dar 6.86, 8.40, 11.20, 13.73 y 16.23 u. `probeRadius` y el diámetro del cursor
deben seguir al nivel. La cámara debe ir de −6.11 a −7.26.

**Lo que este paso prueba de verdad:** que volver a un nivel corto después de uno
largo deja el tablero exactamente como si se hubiera construido de cero. Es el
fallo típico de la construcción en runtime.

- [ ] **Paso 3: Verificar que el material de la placa NO se ensucia**

Reutilizando el mismo montaje del paso 2, pero midiendo el asset antes y despues:

```csharp
using UnityEngine;
using UnityEditor;
using HapticOmni;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_PlacaLaberinto.mat");
        if (mat == null) { result.Log("Todavia no existe el asset: ejecuta antes la tarea 5."); return; }

        Vector2 antes = mat.GetTextureScale("_BaseMap");
        result.Log("tiling del asset ANTES = " + antes.ToString("F2"));

        var raiz = new GameObject("PruebaTiling");
        var tablero = new GameObject("Tablero");
        tablero.transform.SetParent(raiz.transform, false);
        var con = raiz.AddComponent<ConstructorLaberinto>();
        con.raizTablero = tablero.transform;
        con.matPlaca = mat;

        var cfg = ConfiguracionLaberinto.Cargar();
        for (int i = 0; i < cfg.Cuantos; i++) con.Construir(cfg.Nivel(i), cfg);

        Vector2 despues = mat.GetTextureScale("_BaseMap");
        result.Log("tiling del asset DESPUES = " + despues.ToString("F2"));
        result.Log("intacto? " + (antes == despues) +
                   " | marcado como sucio? " + EditorUtility.IsDirty(mat));

        DestroyImmediate(raiz);
    }
}
```

Esperado: el tiling del asset **no cambia** y el material no queda marcado como
sucio. Si cambiara, el `MaterialPropertyBlock` no se está aplicando y se estaría
modificando un archivo del proyecto al jugar.

- [ ] **Paso 4: Punto de control**

- Los cinco niveles se construyen sin errores ni warnings en consola.
- Muros, camino y cámara coinciden con las cifras esperadas.
- Cambiar de nivel no deja hijos huérfanos en `raizTablero`.
- El asset del material de la placa no se modifica.
- `diff`: sólo `ConstructorLaberinto.cs` (+`.meta`).

---

### Task 3: `Ambiente2Laberinto` con niveles

**Files:**
- Modify: `Assets/Scripts/Ambientes/A2/Ambiente2Laberinto.cs`

**Interfaces:**
- Consumes: `ConfiguracionLaberinto`, `NivelLaberinto` (tarea 1); `ConstructorLaberinto` (tarea 2); `AmbienteBase`, `CabeceraSujeto`, `FilaResumen` (ya existen).
- Produces:
  - `Ambiente2Laberinto` gana los campos públicos `constructor` y conserva `nivel` (**1-based**)
  - `void AplicarNivel(int nivelUno)` — el argumento es 1-based
  - `bool ConfigAbierta { get; set; }` — arranca en `true`
  - `ConfiguracionLaberinto Config { get; }`
  - `NivelLaberinto NivelActual { get; }` — **el único sitio que convierte 1-based a 0-based**
  - `int NumeroDeNivel { get; }` — el 1-based, para el panel y el HUD
  - `float HolguraMm { get; }`

- [ ] **Paso 1: Sustituir la cabecera de campos y propiedades**

Reemplazar el bloque que va desde `[Header("Escenario")]` hasta la línea
`public float Eficiencia ...` por:

```csharp
        [Header("Escenario")]
        public Transform bola;
        [Tooltip("Quien levanta el tablero. Lo cablea el generador de escena.")]
        public ConstructorLaberinto constructor;
        [Tooltip("Los rellena AplicarNivel; no se tocan a mano.")]
        public Transform salida;
        public Transform meta;
        [Tooltip("Radio de aceptacion de la meta y de la salida, en unidades.")]
        public float radioMeta = 0.16f;

        [Header("Laberinto (lo rellena AplicarNivel)")]
        public HapticShape[] muros;
        [Tooltip("Eje del pasillo: centros de las celdas del camino mas corto.")]
        public Vector3[] caminoOptimo;
        public float longitudOptima = 1f;

        [Header("Dificultad")]
        [Tooltip("Numero de nivel tal como lo ve el usuario: 1 es el mas amplio. " +
                 "OJO: ConfiguracionLaberinto.Nivel() indexa desde 0; la conversion " +
                 "ocurre SOLO en la propiedad NivelActual.")]
        public int nivel = 2;
        public int semilla = 1024;

        public ConfiguracionLaberinto Config { get; private set; }

        /// <summary>El nivel en juego. Unico punto de conversion entre el numero que
        /// ve el usuario (1..N) y el indice del arreglo (0..N-1).</summary>
        public NivelLaberinto NivelActual
        {
            get { return Config != null ? Config.Nivel(nivel - 1) : new NivelLaberinto(); }
        }

        public int NumeroDeNivel { get { return nivel; } }

        /// <summary>Holgura de la bola dentro del pasillo, en milimetros reales. Es la
        /// medida de dificultad de este ambiente. Se convierte con la escala del
        /// dispositivo en vez de con una constante, para que no se descuadre si
        /// alguien cambia unitsPerMillimeter.</summary>
        public float HolguraMm
        {
            get
            {
                float upm = dispositivo != null ? dispositivo.unitsPerMillimeter : 0.025f;
                return ConfiguracionLaberinto.HolguraU(NivelActual) / Mathf.Max(1e-5f, upm);
            }
        }

        /// <summary>Mientras el panel de dificultad esta abierto el ambiente no arranca
        /// el cronometro: elegir el nivel no debe iniciar el ensayo.</summary>
        public bool ConfigAbierta { get; set; } = true;
```

Y dejar las métricas privadas y las propiedades `Choques`, `Recorrido`,
`DesviacionRms` y `Eficiencia` exactamente como estaban.

- [ ] **Paso 2: Cabecera del resumen con las tres columnas nuevas**

Reemplazar `CabeceraResumen` por:

```csharp
        protected override string CabeceraResumen
        {
            get
            {
                return CabeceraSujeto +
                       ",fecha,nivel,semilla,columnas,filas,holgura_mm,tiempo_s,choques," +
                       "t_contacto_s,f_choque_pico_n,recorrido_u,optimo_u,eficiencia," +
                       "desv_rms_u,desv_max_u";
            }
        }
```

Y `EscribirResumen`:

```csharp
        protected override void EscribirResumen()
        {
            var n = NivelActual;
            FilaResumen(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        nivel, semilla, n.columnas, n.filas, HolguraMm,
                        Tiempo, choques, tiempoEnContacto, fuerzaChoquePico,
                        recorrido, longitudOptima, Eficiencia, DesviacionRms, desviacionMax);
        }
```

Son 15 valores más las 4 columnas de sujeto = 19, contra 19 nombres en la cabecera.

- [ ] **Paso 3: `Awake`, `Start` y `AplicarNivel`**

Reemplazar `Awake` por:

```csharp
        protected override void Awake()
        {
            Config = ConfiguracionLaberinto.Cargar();
            nivel = Mathf.Clamp(nivel, 1, Mathf.Max(1, Config.Cuantos));
            if (constructor == null) constructor = FindFirstObjectByType<ConstructorLaberinto>();
            setMuros = new System.Collections.Generic.HashSet<HapticShape>();
            base.Awake();
        }

        /// <summary>El arranque pasa por AplicarNivel y no por el Reiniciar() de la
        /// base: cuando corre Start todavia no hay tablero que preparar, porque ahora
        /// lo levanta el constructor en tiempo de ejecucion. AplicarNivel termina
        /// llamando a Reiniciar(), asi que hace lo mismo que la base y ademas en el
        /// orden correcto.</summary>
        protected override void Start()
        {
            AplicarNivel(nivel);
        }

        /// <summary>Cambia de nivel en caliente: reconstruye el tablero, recoge las
        /// piezas nuevas y pone las metricas a cero. El argumento es el numero que ve
        /// el usuario (1 = el mas amplio).</summary>
        public void AplicarNivel(int nivelUno)
        {
            if (Config == null) Config = ConfiguracionLaberinto.Cargar();
            nivel = Mathf.Clamp(nivelUno, 1, Mathf.Max(1, Config.Cuantos));

            var n = NivelActual;
            semilla = n.semilla;

            if (constructor != null)
            {
                constructor.Construir(n, Config);
                muros          = constructor.Muros;
                caminoOptimo   = constructor.CaminoOptimo;
                longitudOptima = constructor.LongitudOptima;
                salida         = constructor.Salida;
                meta           = constructor.Meta;
            }
            radioMeta = n.anchoPasillo * 0.45f;

            // El conjunto de muros ya NO se puede construir en Awake: cuando Awake
            // corre, los muros todavia no existen.
            if (setMuros == null) setMuros = new System.Collections.Generic.HashSet<HapticShape>();
            setMuros.Clear();
            if (muros != null)
                foreach (var m in muros) if (m != null) setMuros.Add(m);

            Reiniciar();
        }
```

- [ ] **Paso 4: Bloquear el arranque mientras el panel está abierto**

En `Update`, envolver el bloque de arranque en dos tiempos:

```csharp
            // Arranque en dos tiempos: primero hay que colocar la bola en la salida,
            // y el cronometro empieza al abandonarla. Con el panel abierto no se
            // arranca: elegir el nivel no debe iniciar el ensayo.
            if (!ConfigAbierta && Estado == EstadoEnsayo.Preparado && salida != null)
            {
                float d = DistanciaEnPlano(punta, salida.position);
                if (!enSalida && d < radioMeta) enSalida = true;
                else if (enSalida && d > radioMeta * 1.6f) Iniciar();
            }
```

Y añadir el límite de tiempo justo antes del cierre por meta:

```csharp
            if (Config != null && Config.limiteTiempoS > 0f && Tiempo >= Config.limiteTiempoS)
            {
                Terminar();
                return;
            }

            if (meta != null && DistanciaEnPlano(punta, meta.position) < radioMeta)
                Terminar();
```

- [ ] **Paso 5: Proteger `DetectarChoques` de un conjunto todavía vacío**

Primera línea del método, después de `tocandoMuro = false;`:

```csharp
            if (setMuros == null || setMuros.Count == 0) { tocandoPrevio = false; return; }
```

Sin esto, `Update` corre un frame antes de que `Start` haya aplicado el nivel y
`setMuros.Contains` reventaría.

- [ ] **Paso 6: HUD y ayuda**

Reemplazar `LineasHUD` y `LineasAyuda` por:

```csharp
        public override string[] LineasHUD()
        {
            var n = NivelActual;
            return new[]
            {
                "Nivel              " + nivel + " · " + n.nombre + "   (" +
                                        n.columnas + "x" + n.filas + ", semilla " + semilla + ")",
                "Holgura de la bola " + HolguraMm.ToString("F2") + " mm por lado",
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
                "C configuracion · R reiniciar · F fuerzas on/off",
                "1/2/3/4 cambian de ambiente · ESC menu",
            };
        }
```

- [ ] **Paso 7: Verificar el cuadre de columnas y dos ensayos seguidos**

En **Play mode**, con un sujeto de prueba:

```csharp
using UnityEngine;
using UnityEditor;
using HapticOmni;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        if (!Application.isPlaying) { result.LogError("Hace falta Play mode."); return; }

        // Registrar exige Play mode: SesionSujeto usa DontDestroyOnLoad.
        SesionSujeto.Registrar("T3LAB", 30, "diestro", "ninguna", "sin_guia", "prueba tarea 3");
        result.Log("sujeto=" + SesionSujeto.IdActual + " sesion=" + SesionSujeto.SesionActual);

        var raiz = new GameObject("RigT3");
        var tablero = new GameObject("Tablero");
        tablero.transform.SetParent(raiz.transform, false);

        var camGO = new GameObject("Cam");
        camGO.transform.SetParent(raiz.transform, false);
        var cam = camGO.AddComponent<Camera>();
        cam.fieldOfView = 26f;

        var cursor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cursor.transform.SetParent(raiz.transform, false);

        var devGO = new GameObject("Dispositivo");
        devGO.transform.SetParent(raiz.transform, false);
        var dev = devGO.AddComponent<HapticDevice>();

        var con = devGO.AddComponent<ConstructorLaberinto>();
        con.raizTablero = tablero.transform;
        con.camara = cam;
        con.dispositivo = dev;
        con.cursor = cursor.transform;

        var amb = devGO.AddComponent<Ambiente2Laberinto>();   // Awake corre aqui
        amb.dispositivo = dev;
        amb.bola = cursor.transform;
        amb.constructor = con;
        amb.grabarCsvNativo = false;      // sin dispositivo real no hay grabacion a 1 kHz

        amb.AplicarNivel(1);
        result.Log("nivel 1: " + amb.NivelActual.nombre + " " +
                   amb.NivelActual.columnas + "x" + amb.NivelActual.filas +
                   " holgura " + amb.HolguraMm.ToString("F2") + " mm" +
                   " muros " + amb.muros.Length +
                   " optimo " + amb.longitudOptima.ToString("F2"));
        amb.Iniciar();
        amb.Terminar();

        amb.AplicarNivel(5);
        result.Log("nivel 5: " + amb.NivelActual.nombre + " " +
                   amb.NivelActual.columnas + "x" + amb.NivelActual.filas +
                   " holgura " + amb.HolguraMm.ToString("F2") + " mm" +
                   " muros " + amb.muros.Length +
                   " optimo " + amb.longitudOptima.ToString("F2"));
        amb.Iniciar();
        amb.Terminar();

        result.Log("carpeta = " + RegistroCsv.CarpetaDeSujeto());
        Object.Destroy(raiz);
    }
}
```

Esperado en consola: `nivel 1: Amplio 7x5 holgura 3.20 mm muros 22 optimo 6.86` y
`nivel 5: Extremo 13x10 holgura 1.60 mm muros 73 optimo 16.23`.

Luego, desde el shell:

```bash
cd "C:/Users/76905/Downloads/ReportesOMNI/Actividad4/Analysis/datos/T3LAB"
ls
head -1 T3LAB_a2_resumen.csv | tr ',' '\n' | wc -l    # esperado 19
awk -F, 'NR>1{print NF}' T3LAB_a2_resumen.csv        # esperado 19 en cada fila
cat T3LAB_a2_resumen.csv
```

Esperado: `T3LAB_a2_e01_traza.csv`, `T3LAB_a2_e02_traza.csv` y un resumen con
cabecera de 19 campos y dos filas de 19, la primera con `nivel=1,columnas=7,filas=5,holgura_mm=3.20`
y la segunda con `nivel=5,columnas=13,filas=10,holgura_mm=1.60`.

**Borrar los datos de prueba al terminar**: `rm -rf datos/T3LAB` y quitar la fila
`T3LAB` de `datos/sujetos.csv`.

- [ ] **Paso 8: Punto de control**

- Compila sin errores.
- `NivelActual` es el único sitio que hace `nivel - 1` (verificar con `grep -n "nivel - 1" Assets/Scripts`).
- Cabecera de 19 campos contra 19 valores escritos.
- Dos ensayos de niveles distintos producen `e01` y `e02` sin pisarse, con la geometría correcta en cada fila.
- Datos de prueba borrados.
- `diff`: sólo `Ambiente2Laberinto.cs`.

---

### Task 4: `PanelConfigLaberinto`

**Files:**
- Create: `Assets/Scripts/UI/PanelConfigLaberinto.cs`

**Interfaces:**
- Consumes: `Ambiente2Laberinto.Config`, `.AplicarNivel`, `.ConfigAbierta`, `.NumeroDeNivel` (tarea 3); `ConfiguracionLaberinto.HolguraU` (tarea 1).
- Produces: `class PanelConfigLaberinto : MonoBehaviour` con campo público `ambiente`.

- [ ] **Paso 1: Escribir el panel**

`Assets/Scripts/UI/PanelConfigLaberinto.cs`:

```csharp
// PanelConfigLaberinto.cs -- Elegir la dificultad sin salir de la escena.
//
// Se abre al entrar y se puede volver a abrir con C. Mientras esta abierto el
// ambiente no arranca el cronometro, de modo que elegir el nivel no cuenta como
// empezar el ensayo.
//
// Muestra las tres cifras que describen la dificultad de cada nivel: el tamano
// del mapa, la holgura que le queda a la bola dentro del pasillo, y la longitud
// del camino optimo. Las tres crecen o decrecen a la vez a lo largo de la
// escalera, que es lo que hace que el numero de nivel signifique algo.
using UnityEngine;
using UnityEngine.InputSystem;

namespace HapticOmni
{
    [AddComponentMenu("Haptics/A2 - Panel de dificultad")]
    public class PanelConfigLaberinto : MonoBehaviour
    {
        public Ambiente2Laberinto ambiente;

        private const string Instrucciones =
            "Lleva la bola del cuadro verde al rojo sin rozar los muros. Cuanto mas " +
            "alto es el nivel, mas estrecho es el pasillo y menos margen tiene la mano.";

        private GUIStyle titulo, chico, boton;
        private Texture2D fondo;
        private int nivelSel = 1;

        void Awake()
        {
            if (ambiente == null) ambiente = FindFirstObjectByType<Ambiente2Laberinto>();

            fondo = new Texture2D(1, 1);
            fondo.SetPixel(0, 0, new Color(0.05f, 0.07f, 0.12f, 0.94f));
            fondo.Apply();
            fondo.hideFlags = HideFlags.HideAndDontSave;
        }

        void Start()
        {
            if (ambiente == null) return;
            Sincronizar();
            ambiente.ConfigAbierta = true;
        }

        /// <summary>Deja la seleccion mostrada igual a la que el ambiente esta jugando
        /// de verdad. Se llama al abrir el panel para que no ensene una dificultad que
        /// no se llego a aplicar.</summary>
        private void Sincronizar()
        {
            nivelSel = ambiente.NumeroDeNivel;
        }

        void Update()
        {
            var t = Keyboard.current;
            if (ambiente == null || t == null || !t.cKey.wasPressedThisFrame) return;

            bool abrir = !ambiente.ConfigAbierta;
            // Al abrir, la seleccion vuelve a la dificultad realmente en juego: si el
            // panel se cerro con C sin pulsar "Empezar", lo elegido no se aplico y
            // ensenarlo marcado haria creer que si.
            if (abrir) Sincronizar();
            ambiente.ConfigAbierta = abrir;
        }

        private void Estilos()
        {
            if (titulo != null) return;
            titulo = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            titulo.normal.textColor = new Color(0.60f, 0.88f, 1f);
            chico = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            chico.normal.textColor = new Color(0.72f, 0.78f, 0.88f);
            boton = new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold };
        }

        void OnGUI()
        {
            if (ambiente == null || !ambiente.ConfigAbierta) return;
            var cfg = ambiente.Config;
            if (cfg == null) return;
            Estilos();

            int w = 560, h = 330;
            var caja = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.DrawTexture(caja, fondo);

            var r = new Rect(caja.x + 26, caja.y + 22, caja.width - 52, 28);
            GUI.Label(r, "LABERINTO DE PRECISION", titulo); r.y += 32;

            // El texto se envuelve solo, asi que su alto se pregunta en vez de
            // suponerse: con un rectangulo fijo la ultima linea se recortaria.
            float altoInstr = chico.CalcHeight(new GUIContent(Instrucciones), r.width);
            GUI.Label(new Rect(r.x, r.y, r.width, altoInstr), Instrucciones, chico);
            r.y += altoInstr + 10f;

            GUI.Label(r, "Nivel", chico); r.y += 20;

            int n = Mathf.Max(1, cfg.Cuantos);
            for (int i = 0; i < n; i++)
            {
                var rb = new Rect(r.x + i * ((r.width + 6) / n), r.y,
                                  (r.width - 6 * (n - 1)) / n, 28);
                bool sel = (i + 1) == nivelSel;
                var color = GUI.color;
                if (sel) GUI.color = new Color(0.6f, 0.9f, 1f);
                if (GUI.Button(rb, (i + 1) + " " + cfg.Nivel(i).nombre, boton)) nivelSel = i + 1;
                GUI.color = color;
            }
            r.y += 36;

            var elegido = cfg.Nivel(nivelSel - 1);
            float upm = ambiente.dispositivo != null ? ambiente.dispositivo.unitsPerMillimeter : 0.025f;
            float holgura = ConfiguracionLaberinto.HolguraU(elegido) / Mathf.Max(1e-5f, upm);
            var lab = Laberinto.Generar(elegido.columnas, elegido.filas,
                                        elegido.anchoPasillo, elegido.grosorMuro, elegido.semilla);
            var celdas = lab.Resolver(new Vector2Int(0, 0),
                                      new Vector2Int(lab.columnas - 1, lab.filas - 1));
            float largo = 0f;
            for (int k = 1; k < celdas.Count; k++)
                largo += Vector2.Distance(lab.CentroCelda(celdas[k].x, celdas[k].y),
                                          lab.CentroCelda(celdas[k - 1].x, celdas[k - 1].y));

            GUI.Label(r, "Mapa " + elegido.columnas + "x" + elegido.filas +
                         "   ·   holgura " + holgura.ToString("F2") + " mm por lado" +
                         "   ·   camino optimo " + largo.ToString("F1") + " u", chico);
            r.y += 26;

            if (GUI.Button(new Rect(r.x, r.y, 210, 30), "Empezar", boton))
            {
                ambiente.AplicarNivel(nivelSel);
                ambiente.ConfigAbierta = false;
            }
            GUI.Label(new Rect(r.x + 220, r.y + 6, r.width - 220, 22),
                      "C abre y cierra este panel", chico);
        }
    }
}
```

- [ ] **Paso 2: Verificar la aritmética de layout**

En este entorno los frames no avanzan, así que «no hubo excepciones en `OnGUI`»
no prueba nada. Lo que se comprueba es el recorrido vertical y el reparto
horizontal, con `EditorStyles.label` como cota (la fuente de runtime es otra,
pero lo que interesa es si el texto cabe):

```csharp
var chico = new GUIStyle(EditorStyles.label) { fontSize = 13, wordWrap = true };
string instr = "Lleva la bola del cuadro verde al rojo sin rozar los muros. Cuanto mas " +
               "alto es el nivel, mas estrecho es el pasillo y menos margen tiene la mano.";
float ancho = 560 - 52;
float alto = chico.CalcHeight(new GUIContent(instr), ancho);
float y = 22f;
y += 32;                    // titulo
y += alto + 10f;            // instrucciones
y += 20;                    // "Nivel"
y += 36;                    // botones de nivel
y += 26;                    // linea de cifras
result.Log("alto de las instrucciones = " + alto.ToString("F1") +
           " | 'Empezar' en +" + y.ToString("F1") + " termina en +" + (y + 30).ToString("F1") +
           " | holgura al pie = " + (330 - y - 30).ToString("F1"));

int n = 5;
for (int i = 0; i < n; i++)
{
    float x = i * ((ancho + 6) / n);
    float bw = (ancho - 6 * (n - 1)) / n;
    result.Log(string.Format("  boton {0}: {1:F1} .. {2:F1}", i, x, x + bw));
}
```

Esperado: el pie cae dentro de los 330 px con holgura, y los cinco botones
reparten los 508 px sin solaparse, con 6 px entre ellos y el último terminando
justo en 508.

- [ ] **Paso 3: Verificar el cableado en Play mode**

```csharp
// ambiente y panel en GameObjects distintos: el Awake del panel debe encontrarlo
result.Log("Awake encontro el ambiente? " + (pan.ambiente == amb));
result.Log("ConfigAbierta al crear el ambiente = " + amb.ConfigAbierta + " (esperado True)");
amb.AplicarNivel(4);
result.Log("tras AplicarNivel(4): NumeroDeNivel=" + amb.NumeroDeNivel +
           " nombre=" + amb.NivelActual.nombre + " (esperado 4 / Fino)");
```

- [ ] **Paso 4: Punto de control**

- Compila sin errores.
- El pie del panel cabe en la caja y los botones de nivel no se solapan.
- `ConfigAbierta` vale `true` por inicializador de campo del ambiente, sin depender de que corra el `Start` del panel.
- `diff`: sólo `PanelConfigLaberinto.cs` (+`.meta`).

---

### Task 5: `ConstruirA2` reducido al armazón

**Files:**
- Modify: `Assets/Editor/ConstruirAmbientes.cs` (sustituir `ConstruirA2Nivel1/2/3` y `ConstruirA2`; `CrearSemiespacio` se queda sin usar y se borra)

**Interfaces:**
- Consumes: todo lo anterior.
- Produces: escena `Assets/Scenes/Ambiente2_Laberinto.unity` con el armazón; entrada de menú única `Haptics/Escenas/Ambiente 2 - Laberinto`.

- [ ] **Paso 1: Sustituir los cuatro métodos por uno**

Borrar `ConstruirA2Nivel1()`, `ConstruirA2Nivel2()`, `ConstruirA2Nivel3()`,
`ConstruirA2(int, int)` y `CrearSemiespacio(...)`, y poner en su lugar:

```csharp
        // =====================================================================
        //  AMBIENTE 2 - LABERINTO DE PRECISION
        // =====================================================================
        // Este metodo ya NO construye el laberinto: construye el armazon. El
        // tablero lo levanta ConstructorLaberinto en tiempo de ejecucion, de modo
        // que una sola escena sirve para los cinco niveles y la geometria vive en
        // StreamingAssets/laberinto.json en vez de cocida en el .unity.
        [MenuItem("Haptics/Escenas/Ambiente 2 - Laberinto", priority = 22)]
        public static void ConstruirA2()
        {
            var escena = NuevaEscena();
            EstiloAmbientes.Iluminacion();
            EstiloAmbientes.PostProceso("VP_Ambientes");
            // Camara casi ortogonal: con muros de 0.60 u de alto, una vista abierta
            // esconderia la bola detras de las paredes en los pasillos del borde. La
            // DISTANCIA la fija el constructor segun el tamano del tablero; aqui solo
            // se elige el angulo.
            var cam = EstiloAmbientes.Camara(new Vector3(0f, 0f, -6.5f), Vector3.zero, 26f);

            GameObject cursor;
            var dev = EstiloAmbientes.EspacioHaptico(out cursor, 0.11f, EstiloAmbientes.Cian);
            dev.attractorsEnabled = false;
            cursor.name = "Esfera (bola del laberinto)";

            var matLinea = EstiloAmbientes.Mat("M_Marco", new Color(0.20f, 0.42f, 0.55f), 0.35f);
            EstiloAmbientes.MarcoEspacioDeTrabajo(dev.transform, dev.unitsPerMillimeter, matLinea);

            // Los materiales se crean aqui, en el editor, y se le entregan al
            // constructor: EstiloAmbientes no existe en runtime. Son assets del
            // proyecto, asi que la referencia sobrevive a recargar la escena.
            var matPlaca = EstiloAmbientes.MatTexturizado("M_PlacaLaberinto",
                                                          new Color(0.24f, 0.29f, 0.38f),
                                                          EstiloAmbientes.TexturaRejilla(),
                                                          Vector2.one, 0f, 0.12f);
            var matMuro = EstiloAmbientes.MatTexturizado("M_MuroLaberinto",
                                                         new Color(0.42f, 0.50f, 0.66f),
                                                         EstiloAmbientes.TexturaRuido(),
                                                         new Vector2(3, 3), 0f, 0.30f);
            // Emision moderada: por encima de ~1.2 el bloom convierte el color en un
            // borron blanco y salida y meta dejan de distinguirse.
            var matSalida = EstiloAmbientes.Mat("M_Salida", EstiloAmbientes.Verde, 1.0f);
            var matMeta   = EstiloAmbientes.Mat("M_Meta",   EstiloAmbientes.Rojo,  1.1f);

            var tablero = new GameObject("Tablero");

            var con = dev.gameObject.AddComponent<ConstructorLaberinto>();
            con.raizTablero = tablero.transform;
            con.camara = cam;
            con.dispositivo = dev;
            con.cursor = cursor.transform;
            con.matPlaca = matPlaca;
            con.matMuro = matMuro;
            con.matSalida = matSalida;
            con.matMeta = matMeta;

            var amb = dev.gameObject.AddComponent<Ambiente2Laberinto>();
            amb.dispositivo = dev;
            amb.bola = cursor.transform;
            amb.constructor = con;
            amb.nivel = 2;

            var panel = dev.gameObject.AddComponent<PanelConfigLaberinto>();
            panel.ambiente = amb;

            var hud = dev.gameObject.AddComponent<HUDAmbiente>();
            hud.dispositivo = dev;
            hud.ambiente = amb;

            // Se deja el nivel por defecto ya construido para que la escena guardada
            // sea presentable: es la que se abre en el editor y la que fotografia
            // CapturarAmbientes, que trabaja en modo edicion y no pasa por Play.
            var cfg = ConfiguracionLaberinto.Cargar();
            con.Construir(cfg.Nivel(amb.nivel - 1), cfg);

            Debug.Log(string.Format("[Ambiente 2] Armazon listo. Nivel por defecto {0} ({1}): " +
                                    "{2} muros, camino optimo {3:F2} u",
                                    amb.nivel, cfg.Nivel(amb.nivel - 1).nombre,
                                    con.Muros.Length, con.LongitudOptima));

            Guardar(escena, "Ambiente2_Laberinto", 2);
        }
```

- [ ] **Paso 2: Actualizar el comentario de cabecera del archivo**

Cambiar la línea

```
//   Haptics / Escenas / Ambiente 2 - Laberinto (nivel 1 / 2 / 3)
```

por

```
//   Haptics / Escenas / Ambiente 2 - Laberinto
```

- [ ] **Paso 3: Actualizar `Todo()`**

Cambiar `ConstruirA2Nivel2();` por `ConstruirA2();`.

- [ ] **Paso 4: Actualizar la descripción del selector**

En `Assets/Scripts/Ambientes/SelectorAmbientes.cs`, sustituir la segunda entrada
de `Descripciones` por:

```csharp
            "Llevar la esfera por el laberinto sin tocar los muros. Cinco niveles, " +
            "de 7x5 a 13x10 celdas: cuanto mas alto, mas estrecho el pasillo. " +
            "Se evaluan el tiempo, los errores (choques) y la trayectoria.",
```

- [ ] **Paso 5: Generar la escena y comprobarla**

```csharp
HapticOmni.EditorTools.ConstruirAmbientes.ConstruirA2();
// luego, sobre la escena abierta:
var amb = Object.FindFirstObjectByType<Ambiente2Laberinto>();
var con = Object.FindFirstObjectByType<ConstructorLaberinto>();
var pan = Object.FindFirstObjectByType<PanelConfigLaberinto>();
result.Log("cableado: constructor=" + (amb.constructor == con) +
           " panel->ambiente=" + (pan.ambiente == amb) +
           " raizTablero=" + (con.raizTablero != null) +
           " materiales=" + (con.matPlaca != null && con.matMuro != null &&
                             con.matSalida != null && con.matMeta != null));
result.Log("nivel por defecto=" + amb.NumeroDeNivel + " " + amb.NivelActual.nombre +
           " muros=" + con.Muros.Length + " camino=" + con.LongitudOptima.ToString("F2"));
result.Log("hijos del tablero=" + con.raizTablero.childCount + " (esperado 42: 37 muros + 5)");
```

Después, `Haptics > Construir TODOS los ambientes` y comprobar que las cinco
escenas quedan en Build Settings y que los otros tres ambientes conservan su
componente, dispositivo, HUD y cámara.

- [ ] **Paso 6: Punto de control**

- Las cinco escenas se generan sin errores en consola.
- El armazón queda cableado y la escena guardada muestra ya el nivel 2 construido.
- Los ambientes 1, 3 y 4 siguen intactos (mismo número de objetos y mismos nombres que antes de regenerar).
- `diff`: sólo `ConstruirAmbientes.cs` y `SelectorAmbientes.cs`, más la escena `Ambiente2_Laberinto.unity` (y las otras cuatro, que `Todo()` regenera).

---

### Task 6: `analizar_a2.py` por nivel

**Files:**
- Modify: `Analysis/analizar_a2.py`

**Interfaces:**
- Consumes: `comun.cargar_todos`, `num`, `txt`, `guardar`, `falta`, `tabla`, `encabezado`, `imprimir_1khz`, `estilo`, `COLORES`, `comprobar_datos`.
- Produces: `analizar_a2.analizar()` y cuatro figuras: `a2_laberinto.png`, `a2_permanencia.png`, `a2_aprendizaje.png`, `a2_dificultad.png`.

- [ ] **Paso 1: Que las figuras de traza usen UN ensayo**

Hoy la sección de traza concatena las trazas de todos los sujetos y las trata
como un recorrido continuo: `t[-1] - t[0]`, `x[0]` como salida y `x[-1]` como
meta. Con varios sujetos ya estaba mal, y con cinco niveles superpondría mapas de
tamaños distintos en la misma figura.

Insertar justo después de `estilo()`:

```python
    # Las figuras de traza describen UN recorrido, no una concatenacion: con
    # varios sujetos y cinco niveles, pegar las trazas superpondria laberintos de
    # tamanos distintos y el "tiempo total" seria la suma de ensayos ajenos. Se
    # elige el ensayo con mas muestras, que es el mas informativo.
    archivo = txt(traza, "archivo")
    if len(archivo):
        cuales = list(dict.fromkeys(archivo))
        elegido = max(cuales, key=lambda a: int(np.sum(archivo == a)))
        sel = archivo == elegido
        traza = {k: (list(np.array(v, dtype=object)[sel]) if isinstance(v, list) else v)
                 for k, v in traza.items() if k != "__n__"}
        traza["__n__"] = int(np.sum(sel))
        print()
        print("Recorrido mostrado: %s  (%d muestras de %d ensayo(s) disponibles)"
              % (elegido, traza["__n__"], len(cuales)))
```

y añadir `txt` a la lista de importaciones de `comun`:

```python
from comun import (estilo, cargar_todos, num, txt, guardar, falta, tabla,
                   encabezado, imprimir_1khz, COLORES, comprobar_datos)
```

- [ ] **Paso 2: Tabla de ensayos con la geometría**

Sustituir el bloque `tabla("Ensayos registrados", ...)` por:

```python
        # Las tres columnas de geometria pueden faltar en CSV anteriores al
        # trabajo de niveles: en ese caso se muestran como "-" en vez de romper.
        cols = resumen.get("columnas", [])
        fils = resumen.get("filas", [])
        holg = resumen.get("holgura_mm", [])

        def celda(v, i, fmt="%s"):
            if i >= len(v) or v[i] in ("", None):
                return "-"
            try:
                return fmt % float(v[i])
            except (TypeError, ValueError):
                return str(v[i])

        suj = resumen.get("sujeto", [])
        tabla("Ensayos registrados",
              ["#", "sujeto", "nivel", "mapa", "holgura mm", "t (s)", "choques",
               "eficiencia %", "desv RMS u", "F pico N"],
              [[i + 1, suj[i] if i < len(suj) else "", int(nivel[i]),
                ("%s x %s" % (celda(cols, i, "%.0f"), celda(fils, i, "%.0f"))
                 if i < len(cols) and cols[i] not in ("", None) else "-"),
                celda(holg, i, "%.2f"),
                "%.1f" % tiempo[i], int(choques[i]),
                "%.0f" % (100 * efic[i]), "%.3f" % rms[i], "%.2f" % pico[i]]
               for i in range(len(tiempo))])
```

- [ ] **Paso 3: Curva de aprendizaje agrupada por nivel**

Sustituir el bloque `if len(tiempo) >= 2:` por:

```python
        # Una linea por NIVEL: mezclar dificultades en una sola curva no significa
        # nada, porque el tiempo de un 13x10 y el de un 7x5 no son comparables.
        if len(tiempo) >= 2:
            fig3, (ax, ax2) = plt.subplots(1, 2, figsize=(11, 3.6))
            for k, lv in enumerate(sorted(set(int(v) for v in nivel))):
                sel = nivel == lv
                n = np.arange(1, int(np.sum(sel)) + 1)
                c = COLORES[k % len(COLORES)]
                ax.plot(n, tiempo[sel], "o-", color=c, label="nivel %d" % lv)
                ax2.plot(n, choques[sel], "s--", color=c, label="nivel %d" % lv)
            ax.set_xlabel("ensayo dentro del nivel"); ax.set_ylabel("tiempo (s)")
            ax.set_title("Curva de aprendizaje · tiempo")
            ax.legend(fontsize=7)
            ax2.set_xlabel("ensayo dentro del nivel"); ax2.set_ylabel("choques")
            ax2.set_title("Curva de aprendizaje · errores")
            ax2.legend(fontsize=7)
            guardar(fig3, "a2_aprendizaje.png")
```

- [ ] **Paso 4: Figura nueva de dificultad**

Añadir justo después, dentro del mismo `if resumen is not None:`:

```python
        # La lectura que justifica la escalera: al estrecharse el pasillo, la
        # eficiencia deberia caer y los choques subir. Una serie por sujeto.
        if len(holg) and any(v not in ("", None) for v in holg):
            hh = num(resumen, "holgura_mm")
            valido = hh > 0
            if np.any(valido):
                suj_a = txt(resumen, "sujeto")
                fig4, (a1, a2) = plt.subplots(1, 2, figsize=(11, 3.6))
                for k, s in enumerate(dict.fromkeys(suj_a[valido])):
                    m = valido & (suj_a == s)
                    orden = np.argsort(-hh[m])          # de mas holgura a menos
                    c = COLORES[k % len(COLORES)]
                    a1.plot(hh[m][orden], 100 * efic[m][orden], "o-", color=c, label=str(s))
                    a2.plot(hh[m][orden], choques[m][orden], "s-", color=c, label=str(s))
                a1.invert_xaxis(); a2.invert_xaxis()    # dificultad creciente hacia la derecha
                a1.set_xlabel("holgura por lado (mm)"); a1.set_ylabel("eficiencia (%)")
                a1.set_title("Cuanto cuesta el pasillo estrecho")
                a1.legend(fontsize=7)
                a2.set_xlabel("holgura por lado (mm)"); a2.set_ylabel("choques")
                a2.set_title("Errores frente a la holgura")
                a2.legend(fontsize=7)
                guardar(fig4, "a2_dificultad.png")
```

- [ ] **Paso 5: Verificar con datos de varios niveles**

Generar datos sintéticos leyendo la cabecera **del propio C#**, para que un
desajuste entre lo que Unity escribe y lo que Python lee salte aquí:

Guardar esto como `Analysis/_generar_prueba_a2.py` y ejecutarlo. Lee las
cabeceras **del propio C#** en vez de copiarlas a mano, de modo que un desajuste
entre lo que Unity escribe y lo que Python lee salta aqui y no en el laboratorio:

```python
"""Datos sinteticos de a2 para probar el ANALISIS. Las cabeceras salen del C#."""
import csv, io, math, os, random, re

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
src  = io.open(os.path.join(RAIZ, "Assets/Scripts/Ambientes/A2/Ambiente2Laberinto.cs"),
               encoding="utf-8").read()
base = io.open(os.path.join(RAIZ, "Assets/Scripts/Ambientes/AmbienteBase.cs"),
               encoding="utf-8").read()

def literales(bloque):
    return "".join(re.findall(r'"([^"]*)"', bloque))

cab_suj = re.search(r'CabeceraSujeto = "([^"]*)"', base).group(1)
cab_res = cab_suj + literales(
    re.search(r"CabeceraResumen\s*\{\s*get\s*\{(.*?)\}\s*\}", src, re.S).group(1))
cab_tra = literales(
    re.search(r"CabeceraTraza\s*\{\s*get\s*\{(.*?)\}\s*\}", src, re.S).group(1))
print("resumen: %d campos ; traza: %d campos"
      % (len(cab_res.split(",")), len(cab_tra.split(","))))

DATOS = os.path.join(RAIZ, "Analysis", "datos")
# nivel: (columnas, filas, holgura_mm, camino_optimo)
NIVELES = {1: (7, 5, 3.20, 6.86), 3: (11, 8, 2.00, 11.20), 5: (13, 10, 1.60, 16.23)}

def ensayo(sid, ens, lv, sem, rnd):
    cols, fils, holg, optimo = NIVELES[lv]
    # Cuanto mas estrecho el pasillo, mas lento y mas choques: es lo que la
    # figura de dificultad debe poder leer.
    dureza = 3.2 / holg
    t = optimo * (0.55 + 0.30 * dureza) * (1.0 + 0.15 * rnd.random())
    t *= 1.0 - 0.10 * (ens - 1)                      # algo de aprendizaje
    choques = int(round(2 * dureza * dureza * (0.7 + 0.6 * rnd.random())))
    recorrido = optimo * (1.10 + 0.25 * dureza * rnd.random())
    efic = optimo / recorrido
    rms = 0.010 * dureza * (0.8 + 0.4 * rnd.random())

    carpeta = os.path.join(DATOS, sid)
    os.makedirs(carpeta, exist_ok=True)

    filas = []
    tt, choq = 0.0, 0
    while tt < t:
        fase = tt / max(1e-6, t)
        x = -1.6 + 3.2 * fase
        y = 0.9 * math.sin(fase * 9)
        toca = 1 if (rnd.random() < 0.05 * dureza) else 0
        if toca: choq = min(choques, choq + 1)
        fmag = 0.3 + (2.4 if toca else 0.0) * rnd.random()
        filas.append(["%.5f" % tt, "%.5f" % x, "%.5f" % y, "%.5f" % 0.0,
                      "%.5f" % 0.0, "%.5f" % -fmag, "%.5f" % 0.0, "%.5f" % fmag,
                      "%.5f" % (rms * (0.5 + rnd.random())), toca, choq])
        tt += 0.01
    with io.open(os.path.join(carpeta, "%s_a2_e%02d_traza.csv" % (sid, ens)),
                 "w", encoding="utf-8", newline="") as f:
        w = csv.writer(f, lineterminator="
")
        w.writerow(cab_tra.split(","))
        w.writerows(filas)

    return [sid, 1, ens, "sin_guia", "2026-09-03 17:0%d:00" % ens, lv, sem,
            cols, fils, "%.2f" % holg, "%.5f" % t, choques,
            "%.5f" % (t * 0.18), "%.5f" % (2.6 + 0.6 * rnd.random()),
            "%.5f" % recorrido, "%.5f" % optimo, "%.5f" % efic,
            "%.5f" % rms, "%.5f" % (rms * 2.6)]

plan = [("S01", 1, 1, 3088), ("S01", 2, 1, 3088), ("S01", 3, 3, 1016), ("S01", 4, 5, 1060),
        ("S02", 1, 1, 3088), ("S02", 2, 3, 1016), ("S02", 3, 5, 1060)]
por_sujeto = {}
for sid, ens, lv, sem in plan:
    rnd = random.Random(hash((sid, ens)) & 0xffff)
    por_sujeto.setdefault(sid, []).append(ensayo(sid, ens, lv, sem, rnd))

for sid, filas in por_sujeto.items():
    with io.open(os.path.join(DATOS, sid, "%s_a2_resumen.csv" % sid),
                 "w", encoding="utf-8", newline="") as f:
        w = csv.writer(f, lineterminator="
")
        w.writerow(cab_res.split(","))
        w.writerows(filas)

with io.open(os.path.join(DATOS, "sujetos.csv"), "w", encoding="utf-8", newline="") as f:
    w = csv.writer(f, lineterminator="
")
    w.writerow(["id", "fecha", "edad", "mano", "experiencia", "sesion", "notas"])
    w.writerow(["S01", "2026-09-03 17:00:00", 23, "diestro", "ninguna", 1, "prueba tarea 6"])
    w.writerow(["S02", "2026-09-03 17:30:00", 31, "zurdo", "videojuegos", 1, "prueba tarea 6"])

print("generados %d ensayos" % len(plan))
```

Luego:

```bash
cd "C:/Users/76905/Downloads/ReportesOMNI/Actividad4"
python Analysis/_generar_prueba_a2.py
python Analysis/analizar_a2.py
python Analysis/analizar_todo.py
```

Esperado: `resumen: 19 campos`, el análisis corre con **exit 0**, la tabla muestra
la columna `mapa` y `holgura mm`, la curva de aprendizaje tiene una línea por
nivel y se genera `a2_dificultad.png`.

Comprobar además los dos caminos degradados:
- **Sin columnas de geometría** (un CSV con la cabecera de 16 campos): la tabla
  muestra `-` en `mapa` y `holgura mm`, no se genera `a2_dificultad.png`, y no
  hay excepción.
- **Un solo nivel**: la curva de aprendizaje sale con una sola línea.

**Borrar los datos de prueba al terminar.**

- [ ] **Paso 6: Punto de control**

- `python -m py_compile Analysis/analizar_a2.py` pasa.
- `analizar_a2.py` corre solo y desde `analizar_todo.py`, ambos con exit 0.
- Las figuras usan el estilo oscuro común.
- CSV antiguos sin las tres columnas nuevas no rompen nada.
- Datos de prueba borrados, **incluido `Analysis/_generar_prueba_a2.py`**; `Analysis/datos` vuelve a contener sólo `_pruebas_previas`.
- `diff`: sólo `analizar_a2.py`.

---

## Verificación final

1. Los tests de EditMode siguen en verde (15/15 de `EstadoHanoiTests`): este trabajo no los toca, pero un cambio que rompa la compilación los tiraría.
2. `Haptics > Construir TODOS los ambientes` — cinco escenas sin errores.
3. Un recorrido con el equipo conectado: registrar sujeto, abrir el laberinto, hacer un ensayo del nivel 1 y otro del 5, y comprobar que `Analysis/datos/<ID>/` tiene los dos con la geometría correcta en cada fila.
4. `python Actividad4/Analysis/analizar_todo.py` sin excepciones.
5. A mano, con el Touch: que la bola no se salga del tablero en el nivel 5, que la esquina más lejana se alcance sin forzar el brazo, y que el HUD no marque `Unstable` durante el ensayo.
6. Medir el coste de construir el nivel 5 en runtime (`System.Diagnostics.Stopwatch` alrededor de `Construir`). Si pasara de unos pocos milisegundos, moverlo a mientras el panel está en pantalla.
