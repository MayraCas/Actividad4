# Revisión de la tarea 8 — `PanelConfigHanoi`

**Modo:** implementación y revisión propias (sin subagentes).

## Alcance del cambio

```
### Scripts
Only in Assets/Scripts/UI: PanelConfigHanoi.cs   (+ .meta)
### Editor    (sin cambios)
### Analysis  (sin cambios)
```

Un archivo nuevo. La carpeta vacía `datos/ANON/` que dejó la prueba en Play mode
está borrada; `Analysis` vuelve idéntico al snapshot.

## Cumplimiento del spec

| Exigido | Estado |
|---|---|
| `PanelConfigHanoi : MonoBehaviour` con campo público `ambiente` | ✓ |
| Consume `Config`, `AplicarNivel`, `ConfigAbierta`, `nivel`, `discosElegidos` | ✓ |
| Compila sin errores | ✓ |
| No puede dejar `discosSel` fuera de 3–7 | ✓ medido |
| Elegir nivel actualiza los discos sugeridos, y luego se pueden ajustar a mano | ✓ |

## Desviaciones respecto al brief

### 1. El alto del párrafo se pregunta, no se supone — **corrige un recorte real**

El brief dibujaba las instrucciones en un rect de 28 px de alto. Medido:

```
texto de 103 caracteres en 468 px de ancho
  alto de una linea = 15.5 px
  alto del parrafo  = 31.0 px  (~2.0 lineas)
  el brief reservaba 28 px -> recorte de 3.0 px
```

Se comía la base de la segunda línea. Ahora:

```csharp
float altoInstr = chico.CalcHeight(new GUIContent(Instrucciones), r.width);
GUI.Label(new Rect(r.x, r.y, r.width, altoInstr), Instrucciones, chico);
r.y += altoInstr + 10f;
```

Es la lección de la tarea 4 aplicada antes de que duela: en IMGUI el alto de un
texto que se envuelve se calcula, no se estima. Además el texto pasa a una
constante, para que lo que se mide y lo que se dibuja no puedan separarse.

**Salvedad honesta sobre la medida:** `GUI.skin` no es accesible fuera de `OnGUI`,
así que medí con `EditorStyles.label`, que usa otra fuente. Los 31.0 px son una
**cota, no la cifra exacta** en tiempo de ejecución. Lo que sí queda establecido
es lo cualitativo: el texto ocupa dos líneas y dos líneas no caben en 28 px. Con
`CalcHeight` la corrección es correcta sea cual sea la fuente, que es justamente
la gracia de calcularlo.

### 2. `Sincronizar()` al abrir el panel — cierra un desajuste entre lo que se ve y lo que se juega

En el brief, cerrar el panel con `C` sin pulsar "Empezar" dejaba `ConfigAbierta =
false` **sin aplicar la selección**: el sujeto jugaba el nivel anterior mientras
el panel había mostrado otro marcado, y al reabrirlo seguía viendo el que nunca
se aplicó. Ahora, al abrir, la selección se relee del ambiente:

```csharp
private void Sincronizar()
{
    nivelSel  = ambiente.nivel;
    discosSel = Mathf.Clamp(ambiente.DiscosActuales, EstadoHanoi.MinDiscos, EstadoHanoi.MaxDiscos);
}
```

`C` pasa a comportarse como "cancelar" de forma visible, y "Empezar" sigue siendo
lo único que aplica. `Start()` usa la misma función, así que hay un solo sitio
donde se decide qué se muestra.

## Evidencia

### Recorrido vertical dentro de la caja de 300 px

```
  titulo         +22    (28)
  instrucciones  +54    (31.0)
  'Nivel'        +95.0
  botones nivel  +115.0 (28)
  discos -/+     +151.0 (24)
  aviso de guia  +185.0 (26)
  'Empezar'      +211.0 (30)  -> pie en +241.0
  holgura = 59.0 px
```

Nada se sale ni se pisa, y sobran 59 px. (Con el rect del brief el pie caía en
+238: la corrección cuesta 3 px y la caja no necesita crecer.)

### Botones de nivel — la fórmula del brief es correcta

Con 4 niveles sobre 468 px:

```
    boton 0:   0.0 .. 112.5
    boton 1: 118.5 .. 231.0
    boton 2: 237.0 .. 349.5
    boton 3: 355.5 .. 468.0     <- justo el borde derecho
    separacion entre botones = 6.0 px
```

Sin solapes, sin desbordar, repartido exacto. La aritmética `x = i·(W+6)/n`,
`ancho = (W−6(n−1))/n` es la buena.

### Punto de control: saturación de discos

```
  tres '-' desde 3 -> 3
  tres '+' desde 7 -> 7
```

Nunca sale de `[EstadoHanoi.MinDiscos, EstadoHanoi.MaxDiscos]` = [3, 7].

### El mínimo que anuncia el panel es el mismo que mide el juego

| discos | panel `(1<<k)-1` | `EstadoHanoi.MovimientosOptimos` |
|---|---|---|
| 3 | 7 | 7 ✓ |
| 4 | 15 | 15 ✓ |
| 5 | 31 | 31 ✓ |
| 6 | 63 | 63 ✓ |
| 7 | 127 | 127 ✓ |

Importa porque es el número contra el que el sujeto compara su resultado en el
HUD y en el resumen: si el panel prometiese otro mínimo, la eficiencia parecería
mal calculada.

### Cableado (Play mode)

```
Awake del panel encontro el ambiente? True          (estando en otro GameObject)
ConfigAbierta nada mas crear el ambiente = True
tras AplicarNivel(2,5): nivel=2 DiscosActuales=5    (de ahi relee Sincronizar)
```

Detalle que refuerza el bloqueo del agarre: **`ConfigAbierta` arranca en `true`
por inicializador de campo del propio `Ambiente4Hanoi`**, no por el `Start()` del
panel. Aunque el orden de `Start` entre componentes no está definido, el ambiente
nunca puede empezar leyendo el botón de agarre.

## Hallazgos

Ninguno Crítico, Importante ni Menor más allá de las dos desviaciones ya
corregidas.

### Sin hallazgos (comprobado explícitamente)

- **`Config` no puede ser null cuando el panel lo usa.** Se asigna en
  `Ambiente4Hanoi.Awake()`, y todos los `Awake` corren antes que cualquier `Start`
  u `OnGUI`.
- **`ambiente == null` está cubierto en los tres puntos de entrada** (`Start`,
  `Update`, `OnGUI`): el panel simplemente no se dibuja.
- **`GUI.color` se guarda y se restaura** dentro del bucle de botones, así que el
  tinte de selección no se derrama al resto de la interfaz.
- **La textura de fondo sigue el patrón ya usado en `HUDAmbiente`**
  (`Texture2D` 1×1 con `HideFlags.HideAndDontSave`, sin `OnDestroy`). No es una
  fuga nueva ni una incoherencia con el proyecto.
- **`n = 0` no divide por cero**: el bucle no se ejecuta y los `Mathf.Max(1, n)`
  cubren el resto. Además `Cargar()` garantiza al menos un nivel.

## Pendiente de verificación manual

Todo lo visual y lo interactivo: que la caja quede centrada y legible sobre la
escena, que el nivel marcado se distinga, que `C` abra y cierre, y que "Empezar"
cambie la torre en pantalla. Los frames no avanzan en este entorno, así que la
aritmética es la evidencia y el ojo es la comprobación que falta.

## Veredicto

**Spec: cumple** (los tres puntos de control del brief).
**Calidad: aprobada** — 0 Crítico, 0 Importante, 0 Menor. Sin fix loop.
