# Revisión de la tarea 5 — `ConfiguracionHanoi` y `hanoi.json`

**Modo:** autorrevisión (a partir de aquí el usuario pidió no usar subagentes).

## Alcance del cambio

`diff -rq` contra `task-5-antes/` sobre los tres subárboles versionados:

```
### Assets/Scripts
Only in Assets/Scripts/Ambientes/A4: ConfiguracionHanoi.cs
Only in Assets/Scripts/Ambientes/A4: ConfiguracionHanoi.cs.meta
### Assets/Editor
### Analysis
```

Más `Assets/StreamingAssets/hanoi.json` (+ `.meta`), fuera del snapshot.
Cero modificaciones a archivos existentes. Sin daños colaterales.

## Cumplimiento del spec

| Exigido por el brief | Estado |
|---|---|
| `NivelHanoi {nombre, discos, guiaPermitida}` | ✓ |
| `Cargar()`, `PorDefecto()`, `Sanear()`, `Nivel(int)` | ✓ |
| `MasaDe(int,int)`, `RadioDe(int,int)` | ✓ |
| `RadioMin=0.13`, `RadioMax=0.38`, `GrosorDisco=0.095`, `MasaTope=0.30` | ✓ |
| Consume `EstadoHanoi.MinDiscos/MaxDiscos` | ✓ (línea de `Sanear`) |
| `hanoi.json` con 4 niveles y los 10 parámetros | ✓ |

Ambos archivos son **byte a byte** lo que el brief dictaba. `EstadoHanoi` vive en
el namespace `HapticOmni` (assembly `HapticOmni.Reglas`) y `ConfiguracionHanoi`
en el mismo namespace desde `Assembly-CSharp`, que auto-referencia los asmdef:
compila sin referencia explícita.

## Evidencia de ejecución (Unity_RunCommand, proyecto confirmado)

`Application.dataPath = C:/Users/76905/Downloads/ReportesOMNI/Actividad4/Assets` — proyecto correcto.

| Caso | Entrada | Salida | Veredicto |
|---|---|---|---|
| Orden de saturación | `masaMin=0.22`, `masaMax=0.05` | `0.22 / 0.22`; `MasaDe(0,5)=MasaDe(4,5)=0.22` | correcto: nunca invierte el rango; degrada a "todos pesan igual" |
| Mín. absurdo | `masaMin=9`, `masaMax=0.1` | `0.30 / 0.30` | correcto (tope duro del dispositivo) |
| Negativas | `masaMin=-5`, `masaMax=-1` | `0.02 / 0.02` | correcto |
| Índices fuera de rango | `MasaDe(-3,5)`, `MasaDe(99,5)` | `0.06`, `0.28` | `Clamp01` los recoge |
| Degenerados | `MasaDe(0,1)`, `MasaDe(0,0)`, `RadioDe(0,1)` | `0.28`, `0.28`, `0.38` | rama `discos<=1` |
| `niveles` nulo | `new ConfiguracionHanoi().Nivel(0)` | `discos=3`, `nombre=Facil` | no revienta |
| `Sanear` con `niveles` nulo | — | 4 niveles por defecto | correcto |
| `Nivel` fuera de rango | `Nivel(-1)`, `Nivel(99)` | `Facil`, `Experto` | saturado |

Sumado a lo que ya verificó la implementación: 4 niveles cargados del archivo real,
`MasaDe(0,3)=0.06` / `MasaDe(2,3)=0.28`, `RadioDe(0,3)=0.13` / `RadioDe(2,3)=0.38`,
y los tres caminos de degradación (archivo ausente / JSON corrupto / sin niveles)
devolviendo los valores por defecto con su aviso.

## Hallazgos

### Menor (diferido al review final) — `Nivel()` puede devolver `null` si un elemento del array lo es

`Sanear()` tolera un nivel nulo (`if (n == null) continue;`) pero `Nivel(i)` lo
propaga tal cual al llamador, que hará `nivel.discos` → NullReferenceException.
Es una inconsistencia interna: la clase promete "nunca deja de arrancar por culpa
del archivo" y esta rama la rompería.

**Alcance real: inalcanzable desde el archivo.** Verificado que `JsonUtility`
nunca produce un elemento nulo dentro de un array de clases serializables:

- `"niveles":[{...},null,{...}]` → largo 3, `[1] == null` → **False**
- `"niveles":[{...},{},{...}]` → largo 3, `[1].nombre="Facil"`, `[1].discos=3`

Materializa siempre una instancia por defecto. Como `Cargar()` es la única vía de
entrada y sólo lee ese JSON, no hay camino vivo hasta el `null`. Queda como código
defensivo muerto e incoherente, con arreglo de una línea (`continue` →
`niveles[i] = new NivelHanoi()`), que por disciplina no entra al fix loop.

### Sin hallazgos (comprobado explícitamente)

- **`Cargar()` no puede lanzar hacia fuera.** El `try` cubre `File.Exists`,
  `ReadAllText`, `FromJson` y `Sanear`; el `catch (System.Exception)` es total y
  `PorDefecto()` es asignación pura. Lo único fuera del `try` es `Path.Combine`
  sobre `Application.streamingAssetsPath` (nunca nulo) y una `const`.
- **`PorDefecto()` no llama a `Sanear()`, y no le hace falta:** los diez valores
  compilados caen dentro de sus propios rangos (`0.008 ∈ [0,0.05]`,
  `0.35 ∈ [0.05,2]`, `0.20/0.18/0.22 ∈ [0.01,0.25]`, `0.25 ∈ [0,1]`, `0`).
- **Orden de saturación de masas:** `masaMin` se satura antes de usarse como cota
  inferior de `masaMax`. Es el orden correcto; el inverso dejaría pasar un
  `masaMax` por debajo del tope.

## Nota para la tarea 9 (geometría, aparcada)

De las constantes de esta tarea salen dos restricciones que la construcción de la
escena tiene que respetar:

- `RadioMax = 0.38` ⇒ separación mínima entre postes **0.76** para que los discos
  más grandes de dos torres vecinas no se solapen.
- Pila de 7 discos = `7 × 0.095` = **0.665** de alto ⇒ el poste tiene que ser más
  alto que eso, con holgura para levantar un disco por encima.

## Veredicto

**Spec: cumple. Calidad: aprobada** — cero Crítico, cero Importante, 1 Menor
diferido. Sin fix loop.
