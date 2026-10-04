# Revisión de la tarea 7 — `Ambiente4Hanoi`

**Modo:** implementación y revisión propias (sin subagentes).

## Alcance del cambio

```
### Scripts
Only in Assets/Scripts/Ambientes/A4: Ambiente4Hanoi.cs   (+ .meta)
### Editor    (sin cambios)
### Analysis  (sin cambios)
```

Un archivo nuevo. Los datos de prueba (`datos/T7TEST/`, la fila de `sujetos.csv`
y el `sujetos.csv` vacío que quedó) están borrados: el árbol vuelve exactamente
al estado del snapshot.

## Cumplimiento del spec

| Exigido | Estado |
|---|---|
| `Ambiente4Hanoi : AmbienteBase` | ✓ |
| Campos `discos`, `torres`, `alturaMesa`, `alcanceAgarre`, `modoMantener`, `guia`, `guiaActiva`, `rigidezGuia`, `radioGuia`, `nivel`, `discosElegidos` | ✓ |
| `AplicarNivel(int,int)` | ✓ |
| `ConfigAbierta { get; set; }` | ✓ arranca en `true` |
| `Config`, `Estado4`, `NivelActual`, `DiscosActuales` | ✓ |
| Consume `AmbienteBase`, `EstadoHanoi`, `ConfiguracionHanoi`, `DiscoHanoi`, `TorreHanoi`, `HapticDevice`, `HapticAttractor` | ✓ todas las firmas verificadas contra el código real |
| Ninguna regla del juego duplicada | ✓ toda la legalidad pasa por `Estado4.PuedeRecibir` / `Mover` / `Cima` |
| `Terminar()` comprueba el estado antes de `base.Terminar()` | ✓ |
| No lee el botón de agarre mientras `ConfigAbierta` | ✓ `BuscarCandidato` y `LeerBotonAgarre` están dentro del `if (!ConfigAbierta)` |

## Desviación: `Start()` — **corrección de un defecto real de mi plan**

### El defecto

La escena (tarea 9) crea los **siete** discos activos, porque el nivel no se
conoce hasta cargar la configuración. `AmbienteBase.Start()` llama a `Reiniciar()`
→ `PrepararEscenario()`, que **coloca** los discos. Pero quien los **activa y
dimensiona** es `AplicarNivel`, que en el plan no se llamaba nunca al arrancar.

Resultado medido con `EstadoHanoi(3)` y una torre real:

```
disco 0: NivelDe=2  -> y=-0.7825
disco 1: NivelDe=1  -> y=-0.8775
disco 2: NivelDe=0  -> y=-0.9725
disco 3: NivelDe=-1 -> Mathf.Max(0,-1)=0 -> y=-0.9725   <-- encima del disco 2
disco 4: NivelDe=-1 -> y=-0.9725
disco 5: NivelDe=-1 -> y=-0.9725
disco 6: NivelDe=-1 -> y=-0.9725
```

Cinco discos interpenetrados en la base de la torre 0, con su escala de plantilla
(0.2) porque nunca pasaron por `Configurar`. **El ambiente se abriría roto en cada
carga de escena**, hasta que el sujeto tocase el panel de configuración. El
`Mathf.Max(0, nivelEnPila)` del plan enmascaraba el `-1` en vez de evitarlo.

### La corrección

```csharp
protected override void Start()
{
    AplicarNivel(nivel, discosElegidos);
}
```

`AplicarNivel` activa, dimensiona y termina llamando a `Reiniciar()` — hace lo
mismo que la base y además en el orden correcto. Es una línea, y hace que la
aplicación del nivel sea el único camino de entrada al escenario.

## Evidencia de ejecución

Verificado en **Play mode** (`Awake` y las llamadas del ciclo de vida sí corren;
lo que no avanza en este entorno son los frames). Proyecto correcto:
`Analysis\datos` bajo `Actividad4`.

### El arranque, antes y después

```
Tras Awake: Estado4=3 discos · Config=4 niveles · discos activos=7/7   <-- el estado roto
AplicarNivel(0,3): activos=3/7
```

### Colocación y dimensionado

`AplicarNivel(0,3)` — nivel *Facil*:

| disco | torre · nivel | y real | y esperado | radio | masa | peso |
|---|---|---|---|---|---|---|
| 0 | 0 · 2 | −0.7825 | −0.7825 ✓ | 0.130 | 0.060 | **0.59 N** |
| 1 | 0 · 1 | −0.8775 | −0.8775 ✓ | 0.255 | 0.170 | 1.67 N |
| 2 | 0 · 0 | −0.9725 | −0.9725 ✓ | 0.380 | 0.280 | **2.75 N** |

El más pequeño se siente como una pluma y el mayor llega a 2.75 N, **por debajo
del techo de 3 N** del Touch. Es exactamente el efecto que el spec pedía.

`AplicarNivel(3,6)` — nivel *Experto*: 6 discos activos, todos colocados en su
nivel exacto, pesos 0.59 → 2.75 N en pasos finos, y **`guiaActiva` se apaga sola**
porque ese nivel no permite guía ✓.

Vuelta a 3 discos: los 3 que sobran se desactivan ✓.
Saturación: `AplicarNivel(99,99)` → nivel 3, 7 discos; `AplicarNivel(-5,1)` → nivel 0, 3 discos ✓.

### Archivos de un ensayo

Dos ensayos seguidos sin salir de la escena:

```
datos/T7TEST/T7TEST_hanoi_e01_traza.csv   T7TEST_hanoi_e01_movs.csv
             T7TEST_hanoi_e02_traza.csv   T7TEST_hanoi_e02_movs.csv
             T7TEST_hanoi_resumen.csv
```

- **La bitácora de movimientos lleva el mismo número de ensayo que la traza**
  (e01 con e01, e02 con e02). Era el riesgo declarado en el plan: `Terminar()`
  llama a `movs.Guardar(NombreArchivoEnsayo(...))` *después* de `base.Terminar()`,
  y `Ensayo` sólo se recalcula en `Iniciar()`, así que no puede desincronizarse.
- **Resumen: 20 columnas de cabecera, 20 campos por fila.**
  `CabeceraSujeto`(4) + 16 nombres, contra 16 argumentos de `FilaResumen` + 4 que
  antepone la base. Traza: 15 y 15. Movs: 10 y 10. Contado sobre el archivo, no a ojo.
- Filas obtenidas: `T7TEST,1,1,sin_guia,...,Facil,3,1,...,7,7,1.00000,...` y
  `T7TEST,1,2,sin_guia,...,Facil,3,0,...,2,7,3.50000,...` — el número de ensayo
  avanza y el segundo no pisa al primero.

## Hallazgos

### Importante — requisito vinculante para la tarea 11: la eficiencia de un ensayo incompleto es > 1

En el segundo ensayo, cerrado sin resolver, `eficiencia` salió **3.50000**
(óptimos 7 ÷ movimientos 2). Como *ratio* es correcto y así lo aprobó la tarea 1;
como *eficiencia* no significa nada, y promediar esa columna sin filtrar
inflaría el resultado de cualquier sujeto que no termine.

No toco el cálculo: falsear el dato crudo es peor, y la columna que resuelve el
problema ya está escrita al lado (`completado`, 1/0). Lo que queda es una
obligación para la tarea 11:

> **`analizar_hanoi.py` debe agregar `eficiencia` únicamente sobre las filas con
> `completado == 1`**, y decir en la figura cuántos ensayos quedaron fuera.

Camino de ocurrencia real: hoy sólo se cierra un ensayo incompleto si el operador
pone `limiteTiempoS > 0` en `hanoi.json` (por omisión vale 0 y está desactivado);
`R` y `ESC` reinician sin escribir resumen. Es decir, es una opción que el spec
ofrece, no un accidente.

### Menor (diferido) — `Soltar()` indexa `torres[]` por `indice`, no por posición

`TorreBajoLaPunta` devuelve `t.indice` y `Soltar()` lo usa como índice del array
(`torres[torreOrigen]`). Sólo es correcto si **`torres[i].indice == i`**.
`ActualizarGuia` sí comprueba `destino < torres.Length`; `Soltar` no.
La tarea 9 las construye en orden, así que el invariante se cumple, pero conviene
que quede escrito: **es un contrato que la construcción de la escena debe respetar**.

### Sin hallazgos (comprobado explícitamente)

- **Los niveles de la pila inicial son los correctos.** `EstadoHanoi.Reiniciar`
  apila `[Discos-1 … 1, 0]`, así que `NivelDe(0)` = el nivel más alto y el disco
  mayor queda en la base: el orden lógico y el visual coinciden.
- **`Soltar()` calcula el nivel destino antes de `Mover`.** Con la torre destino
  a altura *h*, el disco se posa en el nivel *h* y `Mover` la deja en *h+1* ✓.
  En los caminos de devolución usa `Altura(origen) - 1`, correcto porque el disco
  agarrado **nunca sale de `Estado4`** hasta que el movimiento es válido.
- **Un disco caído vuelve a su sitio exacto**: al no haberse retirado del estado,
  su hueco en la pila sigue reservado y nadie ha podido ocuparlo.
- **`Terminar()` es idempotente**: sale por el `if (Estado != EnCurso)` antes de
  tocar nada, así que la bitácora no puede escribirse dos veces.

## No verificado (y por qué)

- **El ciclo agarrar → mover → soltar**, y con él las *filas* del archivo de
  movimientos, el conteo de intentos ilegales, las caídas y la barrera de rechazo.
  Todo eso vive en `Update()` y necesita frames avanzando y el botón del stylus;
  en este entorno los frames no corren y no hay dispositivo. Los ensayos de prueba
  movieron las fichas llamando a `Estado4.Mover` directamente, que es lo que
  permite verificar los archivos pero **salta `Soltar()`**. Lo que sí está
  acotado: la fila de movimientos tiene 10 argumentos contra 10 columnas, y
  `RegistroCsv.Fila` ya se verificó en las tareas 2 y 3.
- **`tiempo_s`, `recorrido_u`, `impulso_ns` y las fuerzas salen en 0.00000** en los
  ensayos de prueba por la misma razón: se acumulan en `Update()`.
- Todo el tacto: peso, inercia, barrera, guía.

## Veredicto

**Spec: cumple** (los cuatro puntos de control del brief, verificados uno a uno).
**Calidad: aprobada con una obligación heredada** — 0 Crítico, 1 Importante que no
se arregla aquí sino en la tarea 11 (y queda anotado como requisito), 1 Menor
diferido.
