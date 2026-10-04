# Ambiente 2 — niveles seleccionables, mapa mayor y datos por nivel

**Fecha:** 2026-09-03
**Estado:** diseño aprobado, pendiente de plan de implementación
**Proyecto:** `ReportesOMNI/Actividad4` (Unity 6, URP 17.3)

---

## 1. Qué se construye

El ambiente 2 (laberinto de precisión) pasa a tener **cinco niveles seleccionables
dentro de la escena**, con un mapa que llega a 13×10 celdas, y guarda cada ensayo
identificando la geometría con la que se jugó.

Es el mismo tratamiento que recibió el ambiente 4 en el trabajo del 2026-09-02:
configuración en un JSON de `StreamingAssets`, panel de dificultad en escena, y
construcción del escenario en tiempo de ejecución en vez de cocida en el archivo
de escena.

### El problema que resuelve

Los tres niveles que existían hoy son **tres entradas de menú del editor que
sobrescriben el mismo archivo de escena**. En consecuencia:

- `Ambiente2_Laberinto.unity` sólo puede contener un nivel a la vez, y
  `ConstruirAmbientes.Todo()` siempre construye el 2.
- El sujeto no puede cambiar de dificultad sin volver a Unity y regenerar la escena.
- La columna `nivel` del CSV vale siempre `2`, así que no hay forma de comparar
  dificultades aunque el formato de datos lo permita.

## 2. Decisiones tomadas

| Decisión | Elegida | Por qué |
|---|---|---|
| Cuánto crecer el mapa | Escalera de 5 niveles hasta 13×10 | El tablero ya ocupa el 82–92 % del espacio alcanzable del Touch: **no puede crecer en tamaño físico**, sólo en número de celdas, encogiendo bola y pasillos |
| Semilla | Fija por nivel, escrita en el JSON | Todos los sujetos recorren el mismo mapa: comparables entre sí y entre ensayos. Es lo que asume la curva de aprendizaje del análisis |
| Cómo se cambia de nivel | Construcción en tiempo de ejecución | Una sola escena para los cinco niveles, y la geometría queda configurable sin abrir Unity. El alternativo (precocinar los cinco y encender uno) mete ~240 muros en la escena y congela la geometría |
| Semillas concretas | Elegidas para que el camino óptimo crezca | Con la semilla actual la escalera **no es monótona**: el nivel 2 tiene camino más corto que el 1, y el 4 más corto que el 3 |
| Cámara | Colocada desde el tamaño del tablero | La cámara fija de hoy (z = −6.5, fov 26) da media altura 1.50 y el nivel 5 necesita exactamente 1.50: no cabría |
| Columnas nuevas en el CSV | `columnas`, `filas`, `holgura_mm` | Sin ellas, si algún día se edita `laberinto.json`, un `nivel=3` viejo y uno nuevo significarían cosas distintas y nada en el archivo lo delataría |

## 3. Los cinco niveles

Medidos ya sobre `Laberinto.Generar` con las semillas elegidas:

| nivel | nombre | celdas | bola r | pasillo | grosor muro | holgura/lado | semilla | camino óptimo | barras |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Amplio | 7×5 | 0.130 | 0.420 | 0.070 | 3.2 mm | 3088 | 6.86 u | 22 |
| 2 | Normal | 9×6 | 0.110 | 0.340 | 0.060 | 2.4 mm | 1024 | 8.40 u | 37 |
| 3 | Estrecho | 11×8 | 0.085 | 0.270 | 0.050 | 2.0 mm | 1016 | 11.20 u | 41 |
| 4 | Fino | 12×9 | 0.085 | 0.260 | 0.045 | 1.8 mm | 1052 | 13.73 u | 58 |
| 5 | Extremo | 13×10 | 0.085 | 0.250 | 0.045 | 1.6 mm | 1060 | 16.23 u | 73 |

«Holgura» es `(anchoPasillo − 2·radioBola) / 2` convertida a milímetros reales del
dispositivo (`unitsPerMillimeter = 0.025`). Es la medida de dificultad que
importa: cuánto margen tiene la bola a cada lado dentro del pasillo.

El camino óptimo crece 6.86 → 8.40 → 11.20 → 13.73 → 16.23 u y la holgura baja
3.2 → 2.4 → 2.0 → 1.8 → 1.6 mm. Las dos escaleras son monótonas: el nivel 4 usa
pasillo 0.260 y no 0.250 justamente para que su holgura caiga entre la del 3 y
la del 5 en vez de empatar con esta última. Las semillas se
buscaron barriendo `[1000, 6000)` y quedándose con la que acercaba el camino a un
objetivo creciente por nivel.

### Alcance del brazo

Lo que el sujeto debe poder alcanzar no es el borde del tablero sino el **centro
del pasillo de la esquina**. Con el espacio útil del Touch en ±2.00 × ±1.50 u:

| nivel | tablero | centro más lejano | % del espacio | margen sobrante |
|---|---|---|---|---|
| 1 | 3.50 × 2.52 | x=1.47, y=0.98 | 73 % × 65 % | 20.8 mm |
| 2 | 3.66 × 2.46 | x=1.60, y=1.00 | 80 % × 67 % | 20.0 mm |
| 3 | 3.57 × 2.61 | x=1.60, y=1.12 | 80 % × 75 % | 15.2 mm |
| 4 | 3.71 × 2.79 | x=1.68, y=1.22 | 84 % × 81 % | 11.2 mm |
| 5 | 3.88 × 3.00 | x=1.77, y=1.33 | 89 % × 89 % | 6.9 mm |

Los cinco caben. El 5 es el más justo y aún deja 6.9 mm.

### Ranuras del dispositivo

El nivel 5 tiene 73 barras de muro (el 4, con pasillo más ancho, tiene 58) y el Touch acepta 16 primitivas por ciclo. No
es un problema: `HapticDevice.SeleccionarFormas` ordena **todas** las formas
registradas por distancia a la punta en cada frame y envía las 16 más cercanas,
con las `prioritaria` primero. Lo que sí sube es el coste de ese ordenamiento
(ver §9).

## 4. Arquitectura

Cuatro piezas nuevas o modificadas, calcadas del patrón del ambiente 4.

```
Assets/StreamingAssets/laberinto.json          (nuevo)  los cinco niveles
Assets/Scripts/Ambientes/A2/
    ConfiguracionLaberinto.cs                  (nuevo)  lee y sanea el JSON
    ConstructorLaberinto.cs                    (nuevo)  construye el tablero en runtime
    Ambiente2Laberinto.cs                      (modif)  AplicarNivel, ConfigAbierta
    Laberinto.cs                               (intacto)
Assets/Scripts/UI/PanelConfigLaberinto.cs      (nuevo)  panel de dificultad
Assets/Editor/ConstruirAmbientes.cs            (modif)  ConstruirA2 crea sólo el armazón
Analysis/analizar_a2.py                        (modif)  agrupa por nivel
```

### 4.1 `ConfiguracionLaberinto`

Gemelo de `ConfiguracionHanoi`, con la misma regla de oro: **el ambiente nunca
deja de arrancar por culpa del archivo**. Si falta, no parsea o trae valores
absurdos, se avisa por consola y se usan los valores compilados.

```csharp
[System.Serializable] public class NivelLaberinto
{
    public string nombre = "Normal";
    public int   columnas = 9, filas = 6;
    public float anchoPasillo = 0.34f, grosorMuro = 0.06f, radioBola = 0.11f;
    public int   semilla = 1024;
}

[System.Serializable] public class ConfiguracionLaberinto
{
    public const string NombreArchivo = "laberinto.json";
    public const int MinCeldas = 4, MaxCeldas = 20;

    public NivelLaberinto[] niveles;
    public float rigidezMuro = 0.20f, friccionMuro = 0.12f;
    public float rigidezRanura = 0.22f;
    public float profundidadMuro = 0.60f;
    public float margenCamara = 1.12f;
    public float limiteTiempoS = 0f;

    public static ConfiguracionLaberinto Cargar();
    public static ConfiguracionLaberinto PorDefecto();
    public void Sanear();
    public NivelLaberinto Nivel(int i);
    public float HolguraMm(int i);          // (anchoPasillo - 2*radioBola)/2 / 0.025
}
```

`Sanear()` recorta `columnas`/`filas` a `[MinCeldas, MaxCeldas]`, obliga a que
`anchoPasillo > 2·radioBola` (si no, la bola no cabría en el pasillo y el nivel
sería imposible) y satura las rigideces al techo de 0.25 N/mm que el puente
admite sin entrar en ciclo límite.

### 4.2 `ConstructorLaberinto`

Un `MonoBehaviour` en la escena, con las referencias que el editor le deja
puestas y un solo método público útil:

```csharp
public class ConstructorLaberinto : MonoBehaviour
{
    // Lo que el generador del editor deja cableado
    public Transform raizTablero;      // padre de todo lo que se destruye y recrea
    public Camera camara;
    public HapticDevice dispositivo;
    public Transform cursor;           // la bola: es la punta del stylus
    public Material matPlaca, matMuro, matSalida, matMeta;

    public Laberinto Actual { get; private set; }
    public Transform Salida { get; private set; }
    public Transform Meta   { get; private set; }
    public HapticShape[] Muros { get; private set; }
    public Vector3[] CaminoOptimo { get; private set; }
    public float LongitudOptima  { get; private set; }

    public void Construir(NivelLaberinto nivel, ConfiguracionLaberinto cfg);
}
```

`Construir` hace, en este orden:

1. Destruye los hijos de `raizTablero`. Es lo que evita que queden muros
   huérfanos al cambiar de nivel.
2. `Laberinto.Generar(columnas, filas, anchoPasillo, grosorMuro, semilla)`.
3. Placa de fondo, escalada al tablero.
4. Muros: una caja por barra de `lab.Barras()`, cada una con su `HapticShape`
   de tipo `Caja`.
5. Ranura: los dos semiespacios enfrentados a `±(radioBola + 0.02)`.
6. Salida y meta en los centros de las celdas `(0,0)` y `(columnas−1, filas−1)`.
7. Camino óptimo por BFS, convertido a puntos de mundo, y su longitud.
8. `dispositivo.probeRadius = radioBola` y escala del cursor a `2·radioBola`.
9. Cámara: `z = −(altoTablero/2 · margenCamara) / tan(fov/2)`, mirando al origen.

**El detalle que decide este diseño.** `EstiloAmbientes` vive en `Assets/Editor`
y no existe en runtime, así que el constructor no puede llamarlo. Crea las
primitivas con `GameObject.CreatePrimitive` y les asigna los materiales que el
armazón le dejó cableados. Esos materiales **sí son assets del proyecto**
(`EstiloAmbientes.Mat` termina en `Guardar(m, nombre)`), de modo que la
referencia sobrevive a guardar y recargar la escena.

**Cuidado con el tiling de la placa.** Hoy el material de la placa se crea con
`SetTextureScale(new Vector2(columnas, filas))`, que depende del nivel. Como el
material es un asset compartido, escribir el tiling en él **ensuciaría un archivo
del proyecto cada vez que se juega**. El tiling se aplica con un
`MaterialPropertyBlock` sobre el `Renderer` de la placa, que es el mismo patrón
que ya usa `HapticShape.LateUpdate` para el color de contacto.

### 4.3 `PanelConfigLaberinto`

Gemelo de `PanelConfigHanoi`, con las lecciones que dejó su revisión ya
aplicadas de salida:

- El alto de los textos que se envuelven se calcula con `GUIStyle.CalcHeight`,
  no se estima con un número mágico.
- Al abrir el panel, la selección se **resincroniza** desde el ambiente, para que
  cerrarlo con `C` sin pulsar «Empezar» no deje mostrada una dificultad que no se
  aplicó.
- Muestra por nivel: nombre, celdas, holgura en milímetros y longitud del camino
  óptimo. Son las tres cifras que describen la dificultad.
- Mientras está abierto, `Ambiente2Laberinto.ConfigAbierta` es `true` y el
  ambiente no arranca el cronómetro.

### 4.4 Cambios en `Ambiente2Laberinto`

Gana `AplicarNivel(int)`, `ConfigAbierta`, `Config` y `NivelActual`, y pierde la
construcción del `HashSet` de muros en `Awake` — cuando `Awake` corre, los muros
todavía no existen. El conjunto se rehace al final de cada `AplicarNivel`.

`AplicarNivel(int)`:

1. Satura el índice al rango de niveles cargados.
2. Llama a `constructor.Construir(Config.Nivel(nivel), Config)`.
3. Recoge de vuelta `muros`, `caminoOptimo`, `longitudOptima`, `salida`, `meta`,
   y fija `radioMeta = anchoPasillo · 0.45`.
4. Rehace el `HashSet` de muros.
5. Llama a `Reiniciar()`, que pone las métricas a cero.

El arranque pasa por `AplicarNivel` y no por el `Reiniciar()` de la base, por la
misma razón por la que el ambiente 4 tuvo que hacerlo: si se preparase el
escenario antes de construirlo, no habría nada que preparar.

### 4.5 Cambios en `ConstruirAmbientes`

`ConstruirA2Nivel1/2/3` desaparecen y se funden en un solo
`ConstruirA2()`. El método deja de construir el laberinto y construye el
**armazón**: iluminación, post-proceso, cámara, espacio háptico y cursor, marco
del espacio de trabajo, los cuatro materiales, un `GameObject` vacío llamado
«Tablero» como `raizTablero`, y los componentes `ConstructorLaberinto`,
`Ambiente2Laberinto`, `PanelConfigLaberinto` y `HUDAmbiente` con sus referencias
cruzadas.

## 5. Los datos

**El formato de carpetas y las columnas de sujeto no cambian.** Siguen siendo
`Analysis/datos/<ID>/<ID>_a2_e<NN>_{traza,1khz}.csv` y `<ID>_a2_resumen.csv`, con
`sujeto,sesion,ensayo,condicion` al frente, tal como quedó en el trabajo anterior.

`CabeceraTraza` no cambia.

`CabeceraResumen` añade tres columnas después de `semilla`:

```
sujeto,sesion,ensayo,condicion,fecha,nivel,semilla,columnas,filas,holgura_mm,
tiempo_s,choques,t_contacto_s,f_choque_pico_n,recorrido_u,optimo_u,eficiencia,
desv_rms_u,desv_max_u
```

Con eso, cada fila se explica sola: dice con qué geometría se jugó, no sólo qué
número de nivel se eligió.

Los CSV que existan de antes tienen 16 columnas y los nuevos 19. `cargar_todos`
ya une claves de archivos heterogéneos rellenando con vacío, así que el análisis
lee ambos sin romperse; las filas viejas simplemente no traen las tres columnas
nuevas.

## 6. El análisis en Python

`analizar_a2.py` tiene hoy el mismo defecto que se encontró en el ambiente 4: la
curva de aprendizaje traza todos los ensayos seguidos, y con cinco niveles
mezclaría dificultades en una sola línea que no significa nada.

Cambios:

- **La curva de aprendizaje agrupa por nivel**: una línea por nivel, coloreada
  con la paleta común, y los ensayos ordenados dentro de cada nivel.
- **La tabla de ensayos** muestra `nivel`, `celdas` (`columnas×filas`) y
  `holgura_mm` junto al resto, con `-` cuando la fila es de un CSV antiguo que no
  las trae.
- **Figura nueva `a2_dificultad.png`**: eficiencia y choques por milímetro de
  holgura, que es la lectura que justifica la escalera. Una serie por sujeto.

`comun.py` no cambia.

## 7. Lo que NO entra

- **Guía háptica para el laberinto.** El ambiente 1 y el 4 la tienen; éste no, y
  añadirla no es lo que se pidió.
- **Cambiar las métricas.** Tiempo, choques, tiempo en contacto, fuerza de choque
  pico, recorrido, eficiencia y desviación RMS se quedan como están.
- **El reporte LaTeX** (`src/actividad4.tex`), fuera de alcance como en el
  trabajo anterior. Tampoco se ejecuta `Haptics > Capturar imagenes`, que escribe
  en `src/imagenes`.
- **Los ambientes 1, 3 y 4.** No se tocan.

## 8. Verificación

Sin dispositivo háptico y sin frames avanzando en Play mode se puede comprobar,
por llamada directa y por lectura de archivos:

1. Los cinco niveles se construyen sin errores en consola.
2. Barras, camino óptimo y celdas de cada nivel coinciden con la tabla de §3.
3. Cambiar de nivel no deja muros huérfanos: el número de hijos de `raizTablero`
   vuelve al esperado, y el `HashSet` de muros del ambiente apunta a los vivos.
4. `probeRadius` y la escala del cursor siguen al nivel.
5. La cámara encuadra el tablero con el margen pedido, en los cinco niveles.
6. `ConfiguracionLaberinto.Cargar()` degrada bien: archivo ausente, JSON corrupto
   y sin niveles devuelven los valores compilados con su aviso.
7. `Sanear()` corrige un nivel con `anchoPasillo ≤ 2·radioBola`.
8. Un ensayo cerrado programáticamente escribe el resumen con las 19 columnas y
   el mismo número de campos.
9. `analizar_a2.py` corre con datos de varios niveles y agrupa bien.
10. El material de la placa **no queda modificado** en disco después de jugar.

Queda pendiente de verificación manual, porque exige el equipo y frames:
recorrer un laberinto de cada extremo de la escalera, comprobar que la bola no se
sale del tablero en el nivel 5, que la esquina más lejana se alcanza sin forzar el
brazo, y que el panel se ve y responde.

## 9. Riesgos

**Coste de construir en runtime.** El nivel 5 son 567 casillas de rejilla, un BFS
y ~73 barras que instanciar. Se espera del orden de milisegundos, pero hay que
medirlo. Si sorprendiera, la salida es construir mientras el panel está en
pantalla, donde un tirón no molesta a nadie.

**Coste del ordenamiento de formas.** `SeleccionarFormas` ordena todas las formas
registradas por distancia en cada frame, y el comparador llama a `DistanciaA`.
Pasar de 34 barras (nivel 2 de hoy) a 73 (nivel 5) más que duplica esa lista. Es un
ordenamiento de ~75 elementos por frame: no debería notarse, pero se mide junto a
lo anterior.

**El nivel 5 usa el 89 % del espacio alcanzable.** Si al probarlo con el equipo
resulta incómodo llegar a la esquina, se baja a 13×9 tocando sólo el JSON, sin
recompilar.

**Materiales compartidos.** Ya cubierto con `MaterialPropertyBlock` para el
tiling de la placa, pero conviene comprobarlo explícitamente (punto 10 de §8):
un asset del proyecto modificado al jugar es el tipo de daño que no se nota hasta
que alguien mira el control de versiones, y aquí no hay control de versiones.

**La escena vieja se reemplaza.** `Ambiente2_Laberinto.unity` se regenera. Como
las escenas del proyecto son desechables y se generan desde código, no es
pérdida, pero conviene decirlo.

## 10. Orden de trabajo

1. `ConfiguracionLaberinto` + `laberinto.json`.
2. `ConstructorLaberinto`.
3. Cambios en `Ambiente2Laberinto` (`AplicarNivel`, `ConfigAbierta`, cabecera del
   resumen).
4. `PanelConfigLaberinto`.
5. `ConstruirA2` reducido al armazón, y las tres entradas de menú fundidas en una.
6. `analizar_a2.py`.
