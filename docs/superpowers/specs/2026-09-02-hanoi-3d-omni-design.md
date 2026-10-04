# Ambiente 4 — Torres de Hanói 3D con el Geomagic Touch

Diseño validado · 2026-09-02 · Actividad 4

## 1. Qué se construye

Un cuarto ambiente virtual para la Actividad 4: las Torres de Hanói en 3D,
manipuladas con el stylus del Geomagic Touch. Cada disco tiene una masa que
crece con su tamaño y que la mano siente como peso real; el número de discos
define la dificultad y se configura sin recompilar; los movimientos ilegales se
bloquean con una barrera háptica.

Junto con el ambiente se introduce un subsistema que hoy no existe: la
**identidad del sujeto que hace la prueba**. Hasta ahora todos los ensayos de
los tres ambientes caían en los mismos archivos sin decir quién los había
hecho, lo que impide comparar entre personas o entre sesiones de la misma
persona. Este trabajo cierra ese hueco para los cuatro ambientes.

### Objetivos

1. Un ambiente jugable y medible que ejercite agarre, transporte con carga y
   planificación motora.
2. Que el peso de cada disco sea información háptica útil, no decoración.
3. Dificultad configurable en caliente (3–7 discos) y reproducible (archivo).
4. Que todo ensayo quede atribuido a un sujeto y una sesión, y que el análisis
   en Python pueda cruzar sujetos y ambientes.

### Fuera de alcance

- El reporte LaTeX `src/actividad4.tex`. Se actualizará aparte.
- Cualquier cambio a `My project (3)` (Actividad 3, ya entregada).
- Multijugador, red, persistencia en base de datos.

## 2. Decisiones tomadas

| Decisión | Elección | Motivo |
|---|---|---|
| Alcance del pipeline de sujeto | Global, los 4 ambientes | Permite comparar entre personas; un formato único de datos |
| Configuración de dificultad | JSON en `StreamingAssets` + panel en escena | Protocolo reproducible y ajustable sin recompilar |
| Movimiento ilegal | Bloqueo háptico + rebote | Aprovecha el OMNI y produce una métrica háptica propia |
| Datos ya existentes | Archivar en `_pruebas_previas/` | Son corridas de desarrollo, no de sujetos reales |
| Análisis en Python | Nuevo + adaptar los tres existentes | Deja el pipeline cerrado de punta a punta |
| Mecánica de los discos | Híbrida (estado discreto + física al caer) | Es el patrón ya depurado de `ObjetoAgarrable` |

### Mecánica: por qué la híbrida

Se consideraron tres opciones:

- **Máquina de estados pura.** Las pilas son listas y la física de Unity no
  participa. Robusto y medible, pero soltar un disco en el aire no tiene
  consecuencia: se siente a tablero de ajedrez, no a objeto.
- **Física real de apilamiento.** Rigidbodies apilados de verdad. Unity apila
  mal objetos pequeños agarrados de forma cinemática: aparece *jitter*, las
  torres se derrumban solas y el ruido del solver contamina las métricas.
- **Híbrida (elegida).** Estado discreto para las pilas — al colocar un disco
  hace *snap* y pasa a `isKinematic` — y física real sólo para el caso de
  soltar fuera de una torre: el disco cae a la mesa, se cuenta como caída y
  vuelve a su torre de origen. Es exactamente el ciclo
  `Agarrada / Colocada / Suelta` de `ObjetoAgarrable`, de modo que el ambiente
  4 se lee igual que el 1 y hereda un comportamiento ya probado.

## 3. Arquitectura

### 3.1 Unidades nuevas

```
Assets/Scripts/Registro/
  SesionSujeto.cs         identidad del sujeto y de la sesión, viva entre escenas
  RegistroCsv.cs          (modificado) carpeta por sujeto

Assets/Scripts/Ambientes/A4/
  EstadoHanoi.cs          reglas puras del juego, sin Unity
  DiscoHanoi.cs           un disco: masa, radio, agarre, seguimiento
  TorreHanoi.cs           un poste: pila, aceptación, barrera de rechazo
  Ambiente4Hanoi.cs       orquestación, métricas, HUD
  ConfiguracionHanoi.cs   carga y valida hanoi.json

Assets/Scripts/UI/
  PanelSujeto.cs          formulario de alta del sujeto (escena Menu)
  PanelConfigHanoi.cs     selección de nivel y nº de discos (escena Hanói)

Assets/StreamingAssets/
  hanoi.json              niveles y parámetros

Assets/Tests/EditMode/
  EstadoHanoiTests.cs     tests de las reglas

Analysis/
  analizar_hanoi.py       análisis del ambiente 4
  comun.py                (modificado) lectura multi-sujeto
```

### 3.2 Fronteras

Cada unidad tiene un propósito y se puede entender sin leer las otras:

- **`EstadoHanoi`** es el árbitro. No sabe nada de Unity, transformadas ni
  fuerzas: sólo tres pilas de enteros. Se le pregunta si un movimiento es
  legal, se le informa cuando ocurre, y sabe cuál sería el siguiente
  movimiento óptimo. Es la única pieza con lógica de juego, y por eso es la
  única que se prueba automáticamente.

**Convenio de índices.** En código las torres son `0`, `1` y `2`, y los discos
van de `0` (el más pequeño) a `n−1` (el más grande). En pantalla y en el HUD se
muestran como torres `1`, `2` y `3`. Los CSV guardan siempre los índices de
código. El juego empieza con todos los discos en la torre `0` y se resuelve
cuando están todos en la torre `2`.
- **`DiscoHanoi`** y **`TorreHanoi`** son la encarnación física y háptica del
  estado. No deciden nada; obedecen.
- **`Ambiente4Hanoi`** traduce entre la mano y el árbitro, y mide.
- **`SesionSujeto`** no sabe qué ambiente la está usando; sólo responde quién
  es el sujeto y dónde van sus archivos.
- **`ConfiguracionHanoi`** es un lector de archivo con valores por defecto;
  nadie más toca el JSON.

## 4. Pipeline del sujeto

### 4.1 Datos que se capturan

| Campo | Tipo | Notas |
|---|---|---|
| `id` | texto | Anónimo, formato `S01`. Nunca el nombre real |
| `edad` | entero | 0 si no se declara |
| `mano` | `diestro` / `zurdo` | |
| `experiencia` | `ninguna` / `poca` / `mucha` | Con dispositivos hápticos |
| `sesion` | entero | Autoincremental por sujeto |
| `condicion` | `sin_guia` / `con_guia` | Condición experimental de la sesión |
| `notas` | texto libre | Comas escapadas al escribir el CSV |

No se recogen datos personales identificables. El identificador lo asigna quien
conduce la prueba y la correspondencia con la persona, si hace falta, vive
fuera del proyecto.

### 4.2 Ciclo de vida

`SesionSujeto` es un `MonoBehaviour` con instancia estática y
`DontDestroyOnLoad`, creado por `PanelSujeto` en la escena `Menu`. Sobrevive a
los cambios de escena, de modo que el sujeto se registra una vez y vale para
todos los ambientes que recorra en esa sesión.

En `Menu`, **los botones de ambiente quedan deshabilitados hasta que hay un ID
válido** (no vacío, sin separadores de ruta). Así ningún ensayo puede quedar
huérfano. Un botón secundario, *"modo libre (sin registrar)"*, crea una sesión
con `id = ANON` para pruebas de desarrollo; sus datos van a `datos/ANON/` y el
análisis los excluye por omisión.

`sesion` se calcula al confirmar el alta: se cuentan las filas de `sujetos.csv`
con ese `id` y se suma uno. Al confirmar se anexa la fila nueva.

### 4.3 Estructura de archivos

```
Analysis/datos/
  sujetos.csv                    id,fecha,edad,mano,experiencia,sesion,notas
  S01/
    S01_hanoi_resumen.csv        una fila por ensayo (se anexa)
    S01_hanoi_e01_traza.csv      uno por ensayo
    S01_hanoi_e01_movs.csv       uno por ensayo
    S01_hanoi_e01_1khz.csv       uno por ensayo
    S01_a1_resumen.csv
    S01_a1_e01_traza.csv
    ...
  _pruebas_previas/              los a1_*/a2_*/a3_* actuales, archivados
```

Una carpeta por sujeto es la fuente única de verdad: no hay tabla maestra que
mantener sincronizada. El análisis recorre `datos/*/` y concatena.

El número de ensayo se deduce contando las filas que ya existen en el resumen
de ese sujeto para ese ambiente, así que reanudar una sesión no pisa archivos
anteriores.

### 4.4 Cambios en el código existente

**`RegistroCsv`**

- `Carpeta` (estática) pasa a ser `CarpetaDeSujeto()`, que devuelve
  `Analysis/datos/<id>/` y la crea. Si no hay sesión activa usa `ANON`.
- `Guardar` no cambia de firma.
- Se añade `EscaparTexto(string)` para las notas y los campos libres.

**`AmbienteBase`**

- `PrefijoArchivos` sigue siendo la clave corta del ambiente (`a1`, `hanoi`…).
  El nombre final lo compone la base: `<ID>_<prefijo>_e<NN>_traza.csv` para los
  archivos por ensayo y `<ID>_<prefijo>_resumen.csv` para el acumulado.
- Se añade `protected int Ensayo { get; }`, calculado en `Awake`.
- Se añade `protected string ColumnasSujeto()`, que devuelve
  `id,sesion,ensayo,condicion` ya formateado, y la constante
  `PREFIJO_CABECERA_SUJETO = "sujeto,sesion,ensayo,condicion"`.
- `Terminar()` antepone `ColumnasSujeto()` a la fila de resumen.

**A1 / A2 / A3**

Cambio mínimo: sus `CabeceraResumen` pasan a empezar con
`PREFIJO_CABECERA_SUJETO`. La lógica de cada ambiente no se toca. Este es el
único punto en que este trabajo entra en los ambientes ya entregados, y es
deliberadamente pequeño.

**`SelectorAmbientes`**

- Cuarta entrada en `Escenas`, `Titulos` y `Descripciones`.
- `LeerAtajosDeEscena` añade la tecla `4`.
- El texto del pie menciona `1/2/3/4`.

## 5. El ambiente

### 5.1 Escenario

Generado por `ConstruirAmbientes.ConstruirA4()`, siguiendo el estilo de los
otros tres: mesa háptica prioritaria, marco del espacio de trabajo, iluminación
y post-proceso de `EstiloAmbientes`.

- Una **mesa** (caja háptica prioritaria, rigidez 0.18, fricción 0.30) a la
  altura `-1.02`, igual que en el A1.
- **Tres postes** verticales equiespaciados en X, numerados con
  `EstiloAmbientes.Numeral`. Cada uno es un cilindro háptico: se puede tocar y
  sirve de referencia táctil para localizar la torre sin mirar.
- **N discos** cilíndricos, del más grande abajo al más pequeño arriba, en la
  torre `0`. Radio y color se derivan del índice; el color viene de
  `EstiloAmbientes.PaletaPiezas` extendida cíclicamente.
- Una **guía háptica** opcional (`HapticAttractor`, tecla `G`) que atrae hacia
  el poste destino del siguiente movimiento óptimo, que le pregunta a
  `EstadoHanoi.SiguienteMovimientoOptimo()`. Sólo disponible en los niveles
  cuyo `guiaPermitida` es `true`. Comparar con y sin guía es el mismo
  experimento que ya hace el A1.

  El movimiento óptimo se calcula desde el estado actual, no desde una
  secuencia precomputada: si el sujeto se sale del camino óptimo, la guía lo
  reencamina desde donde está en vez de quedarse desincronizada.

### 5.2 Los pesos

```
masa(i) = masaMin + (masaMax - masaMin) * i / (n - 1)
```

con `i = 0` el disco más pequeño. Con los valores por defecto (0.06 → 0.28 kg)
y `n = 3`, las masas son 0.06 / 0.17 / 0.28 kg: el disco chico pesa ≈0.6 N y el
grande ≈2.7 N, casi el tope de 3 N que el Touch puede entregar. Con `n = 7` los
saltos son más finos, lo que hace la discriminación de peso parte de la
dificultad.

El peso llega a la mano igual que en el A1:

- `dispositivo.CarryForceWorld = Vector3.down * disco.PesoN` — fuerza
  constante, independiente de la posición, así que actualizarla a 60 Hz no
  compromete la estabilidad del lazo.
- `dispositivo.CarryDamping = disco.masaKg * amortiguamientoPorKg` — la
  inercia que se siente al frenar un disco pesado.
- Visualmente, el retardo del `SmoothDamp` de seguimiento crece con la masa
  (`retardo = masaKg * retardoPorKg`), de modo que un disco grande "cuesta"
  también a la vista.

### 5.3 La barrera de rechazo

Cuando el disco en mano no puede posarse en la torre sobre la que está —porque
su radio es mayor que el del disco de la cima— `TorreHanoi` enciende un
`HapticShape` de tipo semiespacio (plano con normal `+Y`) situado a la altura
de la cima más un margen. La punta del stylus topa con un techo invisible: la
mano siente el "no" antes de que el usuario intente soltar.

Además, soltar en esa posición no coloca el disco: cuenta como
**intento ilegal** y el disco vuelve por interpolación a su torre de origen.

`rigidezRechazo` (0.22 por defecto) es más alta que la de los discos: la
barrera debe leerse como una pared, no como algo blando. Si `bloqueoHaptico`
está en `false` en el JSON, la barrera no se enciende y el intento ilegal se
señala sólo visualmente; queda así como condición experimental alternativa.

### 5.4 Ciclo del ensayo

1. **Preparado.** La escena carga con el nivel por defecto del JSON. El panel
   de configuración está abierto; el cronómetro no corre.
2. El sujeto elige nivel y número de discos y cierra el panel. La torre se
   reconstruye en caliente.
3. **En curso.** Arranca en el primer agarre real, no al cargar la escena — si
   el reloj empezara antes, el tiempo medido incluiría la lectura de las
   instrucciones. Es la misma regla que los otros tres ambientes.
4. Cada colocación válida se registra como un movimiento en la bitácora.
5. **Terminado** cuando los N discos están en el poste 3, o al agotarse
   `limiteTiempoS` si es distinto de cero. Se escriben los cuatro archivos.
6. `R` o el botón 2 del stylus reinician sin guardar, como en los demás.

## 6. Configuración

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

`ConfiguracionHanoi` lo lee con `JsonUtility` desde
`Application.streamingAssetsPath`. Reglas de robustez:

- Si el archivo falta, no parsea, o la lista de niveles está vacía, se usan
  estos mismos valores compilados y se emite un `Debug.LogWarning`. **El
  ambiente nunca deja de arrancar por culpa del archivo.**
- `discos` se satura al rango 3–7. Menos de 3 no es un problema; más de 7
  supera el espacio de trabajo útil del Touch.
- `masaMax` se satura a 0.30 kg (≈2.94 N), por debajo del límite del
  dispositivo.

El panel en escena (`PanelConfigHanoi`, `OnGUI` con el estilo del selector)
permite elegir nivel y ajustar el número de discos con `+`/`−` dentro del mismo
rango. El nivel elegido queda registrado en el resumen del ensayo, de modo que
la configuración usada siempre es recuperable desde los datos.

## 7. Datos que produce

Todas las columnas numéricas se escriben con `CultureInfo.InvariantCulture`,
como ya hace `RegistroCsv`.

### `<ID>_hanoi_e<NN>_traza.csv` — 100 Hz

```
t,x,y,z,fx,fy,fz,fmag,agarrado,disco,radio,peso_n,torre_origen,torre_sobre,ilegal
```

`torre_sobre` es el índice de la torre bajo la punta (−1 si ninguna); `ilegal`
vale 1 mientras la barrera de rechazo está activa.

### `<ID>_hanoi_e<NN>_movs.csv` — una fila por movimiento

```
n_mov,t_inicio,t_fin,disco,desde,hacia,intentos_ilegales,recorrido_u,impulso_ns,fuerza_pico_n
```

Es la bitácora fina: permite ver dónde se atascó el sujeto y con qué disco.

### `<ID>_hanoi_resumen.csv` — una fila por ensayo

```
sujeto,sesion,ensayo,condicion,fecha,nivel,discos,completado,tiempo_s,
movimientos,movimientos_optimos,eficiencia,intentos_ilegales,caidas,agarres,
recorrido_u,impulso_ns,fuerza_pico_n,fuerza_rechazo_pico_n,guia
```

`eficiencia = (2^n − 1) / movimientos`, con 1.0 como óptimo. Es la métrica
central del ambiente: cruzada con el peso de los discos y con la fuerza de
rechazo es lo que da material comparable para el reporte.

`condicion` y `guia` no son lo mismo y por eso van las dos: `condicion` es la
condición experimental declarada al dar de alta la sesión y vale igual para
todos los ambientes que el sujeto recorra; `guia` dice si la guía háptica
estaba encendida durante *este* ensayo concreto. Si el sujeto la enciende con
`G` en una sesión declarada `sin_guia`, la discrepancia queda visible en los
datos en vez de perderse.

### `<ID>_hanoi_e<NN>_1khz.csv`

El CSV nativo del puente, sin cambios respecto a los otros ambientes.

## 8. Análisis en Python

**`comun.py`** gana dos funciones y no pierde ninguna:

- `sujetos()` — lee `datos/sujetos.csv` y devuelve la tabla de participantes.
- `cargar_todos(nombre)` — recorre `datos/*/`, carga cada archivo que termine
  en `nombre`, añade la columna `sujeto` si no viene, y concatena. Ignora
  `_pruebas_previas/` y, por omisión, `ANON/`.

`cargar()` se mantiene para leer un archivo concreto.

**`analizar_a1.py`, `analizar_a2.py`, `analizar_a3.py`, `analizar_todo.py`**
pasan a `cargar_todos`. Con un solo sujeto el resultado es idéntico al actual;
con varios, las figuras ganan una serie por sujeto.

**`analizar_hanoi.py`** produce:

1. Eficiencia (movimientos / óptimo) frente al número de discos, por sujeto.
2. Curva de aprendizaje: tiempo por movimiento a lo largo del ensayo.
3. Reparto de tiempo y de impulso por peso de disco — responde directamente a
   si los discos pesados cuestan más.
4. Intentos ilegales y fuerza de rechazo por movimiento.
5. Trayectoria 3D de la punta, con los movimientos marcados.

## 9. Verificación

**Automática.** `Assets/Tests/EditMode/EstadoHanoiTests.cs`, con el Test
Framework 1.6.0 ya presente en el manifest. `EstadoHanoi` no tiene
dependencias de Unity, así que se prueba directo:

- El estado inicial tiene los N discos en la torre 0, ordenados.
- Mover un disco grande sobre uno pequeño es ilegal; sobre uno mayor o sobre
  una torre vacía es legal.
- Mover desde una torre vacía es ilegal.
- `MovimientosOptimos` es `2^n − 1` para n de 3 a 7.
- `Resuelto` sólo es cierto con los N discos en la torre 2.
- `Reiniciar` restaura el estado inicial exacto.
- `SiguienteMovimientoOptimo()` devuelve siempre un movimiento legal, y
  seguirlo repetidamente desde el estado inicial resuelve el juego en
  `2^n − 1` movimientos. Seguirlo desde un estado alcanzado por movimientos
  legales arbitrarios también lo resuelve.
- Una secuencia óptima generada recursivamente resuelve el juego en
  exactamente `2^n − 1` movimientos, todos legales.

**Manual.** Lo háptico no se puede probar sin la mano: la barrera de rechazo,
la diferencia de peso entre discos y la estabilidad del lazo se verifican con
el equipo conectado. El modo simulación con ratón permite recorrer el ambiente
y comprobar reglas, HUD y escritura de archivos sin hardware, igual que en los
otros tres ambientes.

**De datos.** Tras un ensayo de prueba, comprobar que los cuatro archivos
aparecen bajo `Analysis/datos/<ID>/`, que las cabeceras coinciden con las de
este documento y que `analizar_hanoi.py` los lee sin error.

## 10. Orden de trabajo

1. `EstadoHanoi` + sus tests. Es el corazón y lo único probable en frío.
2. `SesionSujeto`, `PanelSujeto` y los cambios en `RegistroCsv` /
   `AmbienteBase` / cabeceras de A1–A3. Archivar `_pruebas_previas/`.
3. `ConfiguracionHanoi` + `hanoi.json`.
4. `DiscoHanoi`, `TorreHanoi`, `Ambiente4Hanoi`, `PanelConfigHanoi`.
5. `ConstruirAmbientes.ConstruirA4()`, `SelectorAmbientes`,
   `CapturarAmbientes`.
6. `comun.py`, `analizar_hanoi.py`, adaptación de los tres análisis
   existentes.

Cada paso deja el proyecto compilando. Los pasos 1–3 no rompen nada de lo que
ya funciona; el paso 2 es el único que toca los ambientes entregados y se
limita a sus cabeceras.

## 11. Riesgos

| Riesgo | Mitigación |
|---|---|
| Las 16 ranuras hápticas del dispositivo se saturan con 7 discos + 3 postes + mesa + barrera | Son 12 primitivas como máximo; `HapticDevice` ya ordena por distancia y la mesa es prioritaria |
| La barrera de rechazo oscila por rigidez alta | `rigidezRechazo` es configurable y el puente ya tiene `AutoGain` y detección de inestabilidad |
| Con 7 discos el disco más pequeño es difícil de agarrar | El alcance de agarre es un margen sobre la superficie, no un contacto exacto; se ajusta en el JSON si hace falta |
| Cambiar las cabeceras rompe los análisis existentes | Los datos viejos se archivan y los cuatro análisis se adaptan en el mismo trabajo |
