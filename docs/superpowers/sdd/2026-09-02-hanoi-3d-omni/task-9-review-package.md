# Revisión de la tarea 9 — la escena y su entrada en el menú

**Modo:** implementación y revisión propias (sin subagentes).

## Alcance del cambio

```
### Scripts
  SelectorAmbientes.cs                 (los tres arreglos, 3 -> 4 entradas)
### Editor
  ConstruirAmbientes.cs                (cabecera, Todo(), ConstruirA4, DejarEnNivelInicial)
  CapturarAmbientes.cs                 (las DOS listas paralelas)
### Scenes
  Ambiente4_Hanoi.unity                (nueva)
  Menu / A1 / A2 / A3 .unity           (regeneradas por Todo())
### Analysis                           (sin cambios)
```

Las cuatro escenas anteriores cambian porque `Todo()` las vuelve a generar, que
es el modelo del proyecto ("las escenas son desechables: no se editan a mano, se
regeneran"). **Comprobado que la regeneración es equivalente, no un cambio de
comportamiento:**

| escena | objetos antes | objetos después | nombres |
|---|---|---|---|
| Ambiente1_Manipulacion | 74 | 74 | idénticos |
| Ambiente2_Laberinto | 92 | 92 | idénticos |
| Ambiente3_Fuerza | 66 | 66 | idénticos |
| Menu | 27 | 27 | idénticos |

Mismo número de líneas en cada archivo (5195/5195, 4841/4841, 2033/2033) y el
mismo multiconjunto de `m_Name`. Lo que cambia son los `fileID`, que Unity
reasigna en cada serialización y que reordenan los bloques.

## Cumplimiento del spec

| Exigido | Estado |
|---|---|
| Escena `Assets/Scenes/Ambiente4_Hanoi.unity` | ✓ |
| Entrada `Haptics/Escenas/Ambiente 4 - Torres de Hanoi` | ✓ |
| Cuarta entrada en `Escenas`/`Titulos`/`Descripciones` | ✓ los tres arreglos con 4 |
| `ConstruirA4` en `Todo()` | ✓ |
| Cabecera y mensaje final actualizados | ✓ "cinco escenas" |
| `CapturarAmbientes` con la quinta escena | ✓ (ver desviación 1) |

## Desviaciones respecto al brief

### 1. `CapturarAmbientes` tiene DOS listas paralelas — **el brief mandaba tocar sólo una**

El paso 3 decía cambiar únicamente el arreglo `escenas` (línea 31). Pero el
archivo tiene:

```csharp
string[] escenas = { "Menu", "Ambiente1_...", "Ambiente2_...", "Ambiente3_Fuerza" };
string[] salidas = { "a4_menu", "a4_ambiente1", "a4_ambiente2", "a4_ambiente3" };
...
for (int i = 0; i < escenas.Length; i++)      // indexa LOS DOS con la misma i
```

Seguir el brief al pie de la letra habría dejado `escenas` con 5 y `salidas` con
4: **`IndexOutOfRangeException` en la quinta vuelta**, justo al generar las
figuras del reporte. Actualicé las dos y dejé escrita la razón en un comentario,
para que el próximo que añada una escena vea el acoplamiento.

### 2. `DejarEnNivelInicial` — la escena guardada tiene que ser presentable

La tarea 7 ya corrigió el arranque en Play (`Start()` → `AplicarNivel`), pero la
escena **guardada en disco** seguía teniendo los siete discos activos con su
escala de plantilla (0.2) amontonados junto al primer poste. Eso importa por dos
motivos concretos:

- es lo que se ve al abrir la escena en el editor, y
- **`CapturarAmbientes` fotografía en modo edición**, sin pasar por Play: la
  figura del reporte habría salido con siete cilindros grises apilados.

Así que el constructor deja los discos como se verán al empezar, calculándolo con
las mismas clases que usa el juego (`ConfiguracionHanoi.MasaDe`/`RadioDe`,
`EstadoHanoi.NivelDe`, `TorreHanoi.PosicionDisco`) en vez de repetir constantes.

## Evidencia

### La escena se genera sin errores

`ConstruirA4()` y `Todo()` corrieron limpios. **Consola:** las únicas cuatro
entradas son del propio andamiaje MCP (una llamada mía a `Capture2DScene` con
los parámetros mal, dos avisos de *render texture* de la herramienta de captura y
un aviso de la cuenta de Unity AI). **Ninguna procede de la construcción.**

### Cableado completo

```
Ambiente4Hanoi: dispositivo=True discos=7 torres=3 guia=True alturaMesa=-1.02
                panel=True (apunta a este ambiente)  hud=True
```

### El invariante que la tarea 7 dejó como contrato

```
Invariante torres[i].indice == i : SE CUMPLE
  torres[0].indice=0 x=-1.250 alturaBase=-1.02 radioAceptacion=0.46 barrera=Semiespacio activa=False
  torres[1].indice=1 x= 0.000 ...
  torres[2].indice=2 x= 1.250 ...
```

Era el Menor diferido de la tarea 7: `Soltar()` indexa `torres[]` por `indice`.
Queda comprobado sobre la escena real, no supuesto.

### Geometría — las cotas que la tarea 5 dejó aparcadas

| magnitud | valor | requisito | margen |
|---|---|---|---|
| separación entre postes | 1.250 | ≥ 0.760 para que no solapen dos discos de radio máximo | holgado |
| ídem | 1.250 | ≥ 0.920 para que no se toquen los círculos de aceptación (r=0.46) | holgado |
| altura del poste | 0.950 | > 0.665 que mide una pila de 7 discos | 0.285 |

Las tres restricciones que quedaron anotadas en las tareas 5 y 6 se cumplen con
margen. Las barreras nacen apagadas ✓.

### Los discos, tal como quedan **guardados** en la escena

```
  disco 0 ACTIVO  y=-0.7825  radio=0.130  masa=0.060  escala=(0.2600, 0.0475, 0.2600)
  disco 1 ACTIVO  y=-0.8775  radio=0.255  masa=0.170  escala=(0.5100, 0.0475, 0.5100)
  disco 2 ACTIVO  y=-0.9725  radio=0.380  masa=0.280  escala=(0.7600, 0.0475, 0.7600)
  disco 3..6 apagados
```

contra lo esperado para el nivel por defecto:

```
  disco 0 nivel 2 -> y esperada -0.7825
  disco 1 nivel 1 -> y esperada -0.8775
  disco 2 nivel 0 -> y esperada -0.9725
```

Coinciden exactamente. Diámetros 0.26 / 0.51 / 0.76 = 2·radio, grosor 0.095 en los
tres.

### Captura de la escena

Inspeccionada visualmente: los tres postes con sus bases de color, las marcas de
1, 2 y 3 puntos delante de cada torre, el marco del espacio de trabajo, la mesa
con su rejilla, y **la pila de tres discos en la torre 1 con el ámbar (grande)
abajo, el verde en medio y el cian (pequeño) arriba**. Es la corrección de la
desviación 2 vista con los ojos.

### Registro en Build Settings y menú

```
  0: Assets/Scenes/Menu.unity                  (activa)
  1: Assets/Scenes/Ambiente1_Manipulacion.unity(activa)
  2: Assets/Scenes/Ambiente2_Laberinto.unity   (activa)
  3: Assets/Scenes/Ambiente3_Fuerza.unity      (activa)
  4: Assets/Scenes/Ambiente4_Hanoi.unity       (activa)
  5: Assets/Scenes/SampleScene.unity           (activa)
```

Los tres arreglos del selector cuadran (4, 4, 4) y las cuatro escenas que puede
cargar existen en disco. `SampleScene` es la de plantilla de Unity, ya estaba y
no se ha tocado.

### Los ambientes anteriores siguen en pie

```
  Ambiente1_Manipulacion: ambiente=Ambiente1Manipulacion 'AMBIENTE 1 · MANIPULACION'      dispositivo=True hud=True camara=True
  Ambiente2_Laberinto:    ambiente=Ambiente2Laberinto    'AMBIENTE 2 · PRECISION MOTRIZ'  dispositivo=True hud=True camara=True
  Ambiente3_Fuerza:       ambiente=Ambiente3Fuerza       'AMBIENTE 3 · INTERACCION CON FUERZA' dispositivo=True hud=True camara=True
  Ambiente4_Hanoi:        ambiente=Ambiente4Hanoi        'AMBIENTE 4 · TORRES DE HANOI'   dispositivo=True hud=True camara=True
  Menu: selector=True
```

## Hallazgos

### Menor (diferido) — el collider del disco es una cápsula, no un disco

`Primitiva(PrimitiveType.Cylinder, ..., conCollider: true)` deja el
`CapsuleCollider` que Unity pone por defecto en un cilindro. Para un disco
aplastado (escala Y = 0.0475) es una aproximación mala. **Sólo interviene mientras
un disco cae**, porque el resto del tiempo es cinemático y su posición se fija a
mano; y la caída dura como mucho `tiempoDeRegreso` = 1 s antes de que el ambiente
lo devuelva a su torre. El efecto visible sería que un disco caído rueda o se
apoya raro durante ese segundo.

### No verificado (y por qué)

- **`CapturarAmbientes` no se ha ejecutado.** Escribe una copia de los PNG en
  `../../src/imagenes`, que es la carpeta del reporte LaTeX, **declarado fuera de
  alcance** para este trabajo. La corrección es de longitud de arreglos (5 y 5) y
  se comprueba leyendo el archivo; ejecutarlo tocaría material que no me toca.
  Queda para cuando el usuario quiera regenerar las figuras.
- **El recorrido jugado** del paso 4 del brief (los ocho puntos: agarrar la cima,
  la barrera al intentar el movimiento ilegal, la caída y el regreso, terminar el
  juego, cambiar a 5 discos). Necesita frames avanzando y ratón o stylus. Va a la
  lista de verificación manual.

## Veredicto

**Spec: cumple.** De los cuatro puntos de control del paso 6, tres están
verificados aquí (las cinco escenas se generan sin errores y quedan en Build
Settings; los ambientes anteriores siguen intactos; no hubo datos de prueba que
borrar porque no se jugó). El cuarto — las tres reglas del juego jugadas — es el
recorrido manual pendiente.

**Calidad: aprobada** — 0 Crítico, 0 Importante, 1 Menor diferido.
