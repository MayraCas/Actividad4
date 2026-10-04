# Revisión de la tarea 6 — `DiscoHanoi` y `TorreHanoi`

**Modo:** implementación y revisión propias (sin subagentes, por petición del usuario).

## Alcance del cambio

`diff -rq` contra `task-6-antes/`:

```
### Scripts
Only in Assets/Scripts/Ambientes/A4: DiscoHanoi.cs   (+ .meta)
Only in Assets/Scripts/Ambientes/A4: TorreHanoi.cs   (+ .meta)
### Editor        (sin cambios)
### Analysis      (sin cambios)
### StreamingAssets: hanoi.json intacto
```

Dos archivos nuevos, cero modificaciones a lo ya entregado.

## Cumplimiento del spec

| Exigido | Estado |
|---|---|
| `DiscoHanoi`: `indice`, `masaKg`, `radio`, `retardoPorKg` | ✓ |
| `Agarrado`, `Cayendo`, `Agarres`, `Caidas`, `TiempoEnMano`, `PesoN` | ✓ |
| `Configurar(int,float,float,float,float,float)` | ✓ firma exacta |
| `Agarrar`, `SeguirPunta`, `PosarEn`, `Caer`, `DistanciaA`, `Reiniciar`, `ActualizarCaida`→`bool` | ✓ |
| `TorreHanoi`: `indice`, `radioAceptacion`, `alturaBase`, `barrera`, `margenBarrera` | ✓ |
| `Contiene`, `DistanciaHorizontal`, `PosicionDisco(int)`, `MostrarBarrera(bool,int,float)` | ✓ |
| Consume `ConfiguracionHanoi.GrosorDisco`, `HapticShape`, `TipoForma` | ✓ |
| Ninguna regla del juego dentro de `DiscoHanoi` | ✓ verificado por lectura: no hay ni una comparación de radios ni de índices entre discos |

## Desviaciones respecto al código literal del brief

Tres, todas documentadas. Las dos primeras son la misma corrección:

### 1. `HacerQuieto()`: frenar antes de congelar — **necesaria, probada**

El brief hacía `isKinematic = true;` y *después* `linearVelocity = zero`. Sonda
deliberada en Unity 6:

```
[Warning] Setting linear velocity of a kinematic body is not supported.
[Warning] Setting angular velocity of a kinematic body is not supported.
```

`HacerQuieto()` se llama desde `PosarEn()`, es decir **en cada colocación legal y
en cada devolución tras un intento ilegal**. Habría escupido dos warnings por
movimiento durante toda la sesión, justo en la consola que uso para detectar
errores reales de un ensayo. Ahora se ponen a cero las velocidades sólo si el
cuerpo aún es dinámico, y luego se congela. Prueba de que funciona: en toda la
verificación del disco los **únicos** warnings fueron los de la sonda.

### 2. `Agarrar()`: mismo orden — **defensiva, no probada como necesaria**

El brief no ponía las velocidades a cero aquí. Añadí el mismo bloque guardado,
por simetría y porque el estado es alcanzable: en la tarea 7 el jugador puede
agarrar un disco **mientras cae** (dinámico, con velocidad acumulada). Sin esto
la velocidad residual quedaría guardada hasta que `Caer()` la limpiase más tarde.
Lo digo sin adornos: no he demostrado que rompa nada sin el bloque; es coste cero
y cierra la ruta de forma explícita en vez de depender de una limpieza posterior.

### 3. `Contiene()` delega en `DistanciaHorizontal()`

El brief repetía el mismo cálculo de distancia en los dos métodos. Ahora
`Contiene` llama a `DistanciaHorizontal`. Semántica idéntica; elimina que los dos
criterios puedan divergir si algún día se toca uno.

## Evidencia de ejecución

`Application.dataPath = C:/Users/76905/Downloads/ReportesOMNI/Actividad4/Assets` — proyecto correcto.
Compilación limpia (`isCompilationSuccessful: true`, `compilationLogs` vacío).

### `TorreHanoi`

| Comprobación | Resultado |
|---|---|
| `PosicionDisco(0)` cara inferior | `-1.0200` = `alturaBase` exacto — **punto de control del brief cumplido** |
| Paso vertical `PosicionDisco(1)−(0)` | `0.0950` = `GrosorDisco` |
| `PosicionDisco(6)` conserva XZ del poste | x=1.5, z=0.3 ✓ |
| `Contiene` a 0.45 / 0.47 del eje (r=0.46) | `True` / `False` |
| `Contiene` ignora la Y | punto a 1000 de altura → `True` ✓ (es un cilindro infinito, como pretende el diseño) |
| `MostrarBarrera` sin barrera asignada | no lanza |
| Barrera con altura 0 | y = `-0.9600` = `alturaBase + margen` ✓ |
| Barrera con altura 3 | y = `-0.6750` = `alturaBase + 3·grosor + margen` ✓ |
| Rigidez aplicada | 0.22 → 0.19 ✓ |
| Apagar dos veces | idempotente, sin `SetActive` redundante |

### `DiscoHanoi`

| Comprobación | Resultado |
|---|---|
| `Configurar` con 5 discos | masas 0.0600 / 0.1150 / 0.1700 / 0.2250 / 0.2800; radios 0.1300 → 0.3800 |
| `localScale` | `(0.5100, 0.0475, 0.5100)` → diámetro 0.51 = 2·radio ✓, alto 2·0.0475 = 0.095 = `GrosorDisco` ✓ (cilindro primitivo: 1 de diámetro, **2** de alto) |
| `rb.mass` sigue a `masaKg` | 0.1700 ✓ |
| `SemiladosMundo` | `(0.2550, 0.0475, 0.2550)` = el override, **no** la escala. Importa: sin override habría salido `(0.255, 0.0238, 0.255)`, la mitad de alto |
| `PesoN` | 1.6677 N con el disco mayor de 5 → **por debajo del techo de 3 N** del Touch |
| `Reiniciar` | todo a cero, cinemático, forma encendida |
| `Agarrar` | `forma.enabled → False` (no pelea con la mano); `Agarres=1` |
| `Agarrar` dos veces | `Agarres` sigue en 1 ✓ |
| `SeguirPunta` conserva el desfase | objetivo `(1, 0.5, 0)` + desfase `(−0.05, −0.02, 0)` → `(0.9500, 0.4800, 0.0000)`. **El disco no se teletransporta al centro de la punta** |
| `TiempoEnMano` | 6.400 s tras 400×0.016 ✓ |
| Retardo por masa | disco 0 → 0.0210 s, disco 4 → 0.0980 s (el grande responde más lento) |
| `PosarEn` | posición exacta, rotación a identidad, cinemático, forma encendida |
| `Caer` | `Caidas=1`, cinemático off, `useGravity=True`, forma encendida |
| Temporizador de caída | 0.5 → `False`; +0.5 → `True` (con `tiempoDeRegreso=1.0`) |
| `PosarEn` cancela la caída | `ActualizarCaida(5)` → `False` |
| `SeguirPunta` sin agarrar | inerte: ni posición ni `TiempoEnMano` cambian |
| `DistanciaA` | centro → `−0.0475` (medio grosor, la cara más cercana); a 5 de distancia → `4.7450` = 5 − 0.255 ✓ |

## Hallazgos

### Menor (diferido) — `Agarrar`/`Caer` usan `cuerpo` sin la guarda que sí tiene `Configurar`

`Configurar` y `HacerQuieto` hacen `if (cuerpo == null) cuerpo = GetComponent<Rigidbody>();`,
pero `Agarrar` y `Caer` van directos a `cuerpo`. Si alguien los llamara sin que
`Awake` hubiese corrido (posible sólo en modo edición) sería
NullReferenceException. En Play mode `Awake` siempre corre antes que cualquier
llamada del ambiente, así que **no hay camino vivo**. Es una asimetría de estilo
heredada del brief, no un defecto operativo.

### Sin hallazgos (comprobado explícitamente)

- **El `Contiene` del borde exacto devuelve `False`** por representación en coma
  flotante (`1.5f + 0.46f − 1.5f` no es exactamente `0.46f`). Es el
  comportamiento esperado y sin consecuencia: un disco soltado exactamente sobre
  la circunferencia cae, y el criterio de aceptación no depende de esa cifra.
- **`forma.enabled = false` al agarrar** desregistra la forma del `HapticDevice`
  vía `OnDisable`, que es justo lo que el comentario promete.
- **`MostrarBarrera` coloca antes de activar**: el transform se puede mover con el
  GameObject inactivo, así que la barrera nunca aparece un frame en su posición
  vieja.

## Aparcado para la sesión manual (ajuste, no defecto)

Con `retardoPorKg = 0.35` los retardos reales van de **0.021 s a 0.098 s**: a esas
cifras `SmoothDamp` alcanza el objetivo casi al instante y el canal "inercia"
apenas se percibe. El peso se sentirá sobre todo por `PesoN` (la fuerza
constante), que es lo principal, pero conviene subir `retardoPorKg` en
`hanoi.json` si al probarlo con el Touch la diferencia entre disco pequeño y
grande resulta pobre. El parámetro es configurable y satura en `[0.05, 2]`, así
que se ajusta sin tocar código.

## Veredicto

**Spec: cumple** (incluido el punto de control de `PosicionDisco(0)`).
**Calidad: aprobada** — 0 Crítico, 0 Importante, 1 Menor diferido. Sin fix loop.

**Pendiente de verificación manual:** el tacto. Que la barrera se sienta como un
techo y no como un rebote, que el disco agarrado siga a la punta sin oscilar, y
que la diferencia de peso entre el disco 0 y el 6 se note con el Touch en la mano.
Nada de eso se puede comprobar sin el dispositivo.
