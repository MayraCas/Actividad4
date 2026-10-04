# Revisión de la tarea 11 — `analizar_hanoi.py`

**Modo:** implementación y revisión propias (sin subagentes).

## Alcance del cambio

Un archivo nuevo, `Analysis/analizar_hanoi.py` (244 líneas). Los cinco `.py` de
la tarea 10 quedan **intactos**, verificado con `diff`.

## Cumplimiento del spec

| Exigido | Estado |
|---|---|
| `analizar_hanoi.analizar()` | ✓ |
| Cuatro figuras en `Analysis/figuras/` | ✓ `hanoi_eficiencia`, `hanoi_aprendizaje`, `hanoi_por_disco`, `hanoi_trayectoria` |
| Tres tablas en consola | ✓ ENSAYOS, COSTE POR DISCO, BARRERA DE RECHAZO |
| Consume sólo lo declarado de `comun` | ✓ |
| Corre solo y desde `analizar_todo.py` | ✓ ambos con exit 0 |
| Con un sujeto y sin ilegales tampoco falla | ✓ probado |
| Estilo oscuro común | ✓ inspeccionado |

## Desviaciones respecto al brief

### 1. La eficiencia se agrega sólo sobre ensayos terminados — **cumple el requisito vinculante de la tarea 7**

La tarea 7 dejó esto escrito como obligación para la 11, tras medir un ensayo
incompleto con `eficiencia = 3.50000`. El código del brief no filtraba nada y
`figura_eficiencia` fijaba `ylim(0, 1.1)`: un ensayo cortado **se habría salido
del gráfico sin dejar rastro**, ni visible ni contado.

Ahora:

- `completados(r)` da la máscara desde la columna `completado`.
- `figura_eficiencia` filtra por ella y el título dice cuántos quedaron fuera.
- `tabla_de_ensayos` muestra `-` en vez de una eficiencia sin sentido, y explica
  por qué debajo de la tabla.
- Si **ningún** ensayo está terminado, la figura se omite con un mensaje en vez
  de dibujar un lienzo vacío.

Los ensayos incompletos siguen apareciendo en la tabla con todo lo demás
(tiempo, movimientos, ilegales, caídas): no se ocultan, se marcan.

### 2. La curva de aprendizaje va por ensayo, no por sujeto — **la del brief no se leía**

El brief trazaba una línea por sujeto sobre `n_mov`. Pero `n_mov` **vuelve a 1 en
cada ensayo**: juntando los tres ensayos de S01, `argsort` intercala el
movimiento 1 del ensayo A, el 1 del B, el 2 del A… y la línea va y vuelve. No es
una curva de aprendizaje, es un zigzag.

Ahora hay una línea por `archivo` (un ensayo), coloreada por sujeto y con una
sola entrada de leyenda por persona. Se ve en la figura: cada trazo arranca en el
movimiento 1 y llega hasta donde llegó su ensayo (7, 15, 21 y 31 movimientos).

### 3. Aviso cuando las medias por disco mezclan dificultades

`figura_por_disco` promedia por índice de disco a través de todos los ensayos.
Pero el índice **no fija la masa**: el disco 2 de un juego de 3 es el mayor, y el
disco 2 de un juego de 6 es de los pequeños. Con niveles mezclados las barras
intermedias mezclan masas.

No reestructuro el gráfico — los extremos sí son comparables siempre (el disco 1
pesa `masaMin` en todos los niveles, y el último de cada ensayo pesa `masaMax`) —
pero el análisis lo dice en voz alta:

```
  Aviso: estas medias mezclan ensayos de 3 y 4 y 5 y 6 discos.
  El disco 1 (masa minima) y el ultimo de cada ensayo (masa maxima)
  son comparables entre niveles; los intermedios no lo son.
```

El aviso sólo aparece si hay más de un número de discos; con un solo nivel calla.

## Evidencia

### Los datos de prueba salen de las cabeceras del propio C#

Generé cinco ensayos de dos sujetos leyendo `CabeceraTraza`, `CabeceraResumen` y
`CabeceraMovs` **del código fuente de `Ambiente4Hanoi.cs`** y `CabeceraSujeto` de
`AmbienteBase.cs`, no copiándolas a mano. Así, si Python esperase una columna que
Unity no escribe, saltaría aquí:

```
cabeceras leidas del C#:  traza 15 campos · resumen 20 campos · movs 10 campos
```

El plan de ensayos incluye a propósito los casos que importan: un ensayo
**incompleto** (S01 e03, 21 de 63 movimientos, eficiencia cruda 3.00) y otro con
**cero intentos ilegales** (S02 e02).

### Ejecución con los cinco ensayos

```
ENSAYOS
sujeto  ses  ens  nivel    n  fin  t (s)  mov/opt  efic  ileg  caid  rechazo (N)  guia
S01     1    1    Facil    3  si   10.6   7/7      1.00  6     2     2.40         sin_guia
S01     1    2    Dificil  5  si   38.5   31/31    1.00  6     3     2.40         sin_guia
S01     1    3    Experto  6  NO   28.5   21/63    -     14    2     2.40         sin_guia
S02     1    1    Facil    3  si   10.2   7/7      1.00  3     0     2.40         con_guia
S02     1    2    Medio    4  si   18.8   15/15    1.00  0     0     0.00         con_guia

  1 de 5 ensayos quedaron sin terminar; su eficiencia se omite
```

`21/63` con `efic = -` es exactamente el caso que la tarea 7 pidió cerrar.

La tabla de la barrera separa por sujeto: S01 con 26 intentos ilegales en 59
movimientos (0.44 por movimiento) frente a S02 con 3 en 22 (0.14).

### Punto de control: los tres caminos degradados

| caso | resultado |
|---|---|
| **Sin datos de Hanói** | `(falta *_hanoi_resumen.csv: ejecuta el ambiente en Unity y termina un ensayo)` y vuelve. Sin excepción |
| **Un solo sujeto, cero ilegales** | Corre entero; la tabla de rechazo muestra `0` y `0.00`, la figura de trayectoria no marca puntos rojos; el aviso de dificultades mezcladas **calla**, como debe |
| **Todos los ensayos sin terminar** | `efic` en `-`, nota explicando, `(ningun ensayo terminado todavia: sin figura de eficiencia)`, y el resto del análisis sigue funcionando |

Ninguno lanza excepción.

### `analizar_todo.py` de punta a punta

Exit 0. Imprime la tabla de PARTICIPANTES (S01 y S02 con edad, mano y
experiencia) y luego los cuatro ambientes. **Las doce figuras** quedan en
`Analysis/figuras/`, las cuatro de Hanói entre ellas.

### Estilo

Inspeccionadas `hanoi_eficiencia.png` y `hanoi_aprendizaje.png`: fondo oscuro,
rejilla tenue, tipografía clara y la paleta de `COLORES` — el mismo estilo que las
figuras de a1/a2/a3 y que las capturas de las escenas.

## Hallazgos

Ninguno pendiente. Las tres desviaciones de arriba son correcciones aplicadas.

### Sin hallazgos (comprobado explícitamente)

- **`figura_trayectoria` elige el archivo con más filas**, no el primero, así que
  la figura muestra el recorrido más rico disponible.
- **La proyección 3D funciona** sin importar `mpl_toolkits` a mano (matplotlib
  moderno la registra sola): la figura se generó, 139 KB.
- **`dict.fromkeys` sobre el arreglo de objetos de numpy** conserva el orden de
  aparición y deduplica bien; las leyendas no repiten sujeto.

## No verificado (y por qué)

- **Los datos son sintéticos.** Las cabeceras salen del C# real y las reglas del
  juego se simulan con el mismo algoritmo óptimo, pero **las cifras no las
  produjo una persona jugando**. Lo que esto demuestra es que el análisis lee el
  formato correcto, agrupa bien y no revienta en los bordes; **no** demuestra que
  la eficiencia o el impulso que medirá el ambiente sean plausibles. Eso llega
  con el recorrido manual del paso 2 del brief.
- Por lo mismo, el "comprobar que `mov/opt` y `efic` cuadran con lo que decía el
  HUD" no se puede cerrar aquí: hace falta jugar y mirar el HUD.

## Veredicto

**Spec: cumple**, los tres puntos de control del paso 4 verificados.
**Calidad: aprobada** — 0 Crítico, 0 Importante, 0 Menor.
