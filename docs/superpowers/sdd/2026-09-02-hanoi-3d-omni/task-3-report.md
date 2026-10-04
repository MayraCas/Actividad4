# Reporte — Tarea 3: nombres de archivo por sujeto y ensayo en `AmbienteBase`

## Archivos modificados

1. `Assets/Scripts/Ambientes/AmbienteBase.cs`:
   - Añadido `using System.IO;`.
   - Insertado, justo después de `protected abstract string CabeceraResumen { get; }`,
     el bloque completo del brief: `CabeceraSujeto`, `Ensayo`,
     `NombreArchivoEnsayo(string sufijo)`, `NombreArchivoResumen()`,
     `RutaRelativa1kHz()`, `SiguienteEnsayo()` y `FilaResumen(params object[] valores)`.
     Transcrito verbatim.
   - `Awake()`: añadida la línea `Ensayo = SiguienteEnsayo();` después de
     `resumen = new RegistroCsv(CabeceraResumen);`.
   - `Iniciar()`: añadida la línea `Ensayo = SiguienteEnsayo();` después de
     `Estado = EstadoEnsayo.EnCurso;`.
   - `Terminar()`: el bloque de guardado ahora usa
     `NombreArchivoEnsayo("traza.csv")`, `NombreArchivoResumen()` y
     `RutaRelativa1kHz()` en vez de `PrefijoArchivos + "_traza.csv"`,
     `PrefijoArchivos + "_resumen.csv"` y `PrefijoArchivos + "_1khz.csv"`.

2. `Assets/Scripts/Ambientes/A1/Ambiente1Manipulacion.cs`:
   - `CabeceraResumen` (antiguas líneas 75-77) con `CabeceraSujeto +` delante.
   - `EscribirResumen` (antigua línea 274): `resumen.Fila(` → `FilaResumen(`.
   - Nada más tocado en este archivo.

3. `Assets/Scripts/Ambientes/A2/Ambiente2Laberinto.cs`:
   - `CabeceraResumen` (antiguas líneas 76-77) con `CabeceraSujeto +` delante.
   - `EscribirResumen` (antigua línea 211): `resumen.Fila(` → `FilaResumen(`.
   - Nada más tocado en este archivo.

4. `Assets/Scripts/Ambientes/A3/Ambiente3Fuerza.cs`:
   - `CabeceraResumen` (antiguas líneas 60-61) con `CabeceraSujeto +` delante.
   - `EscribirResumen` (antigua línea 226), dentro del bucle por estación:
     `resumen.Fila(` → `FilaResumen(`.
   - Nada más tocado en este archivo.

Antes de editar A1/A2/A3 se leyeron las líneas exactas indicadas por el
brief y coincidían al carácter con el código citado; los diffs aplicados
son exactamente los que pide el brief, sin tocar métricas ni lógica.

## Verificación realizada

### Compilación

Unity MCP no detectó el editor en el primer intento
(`"Unity not detected (no fresh discovery files found)"`); en el segundo
intento sí. Se forzó `AssetDatabase.Refresh()` vía `Unity_RunCommand`:
`isCompilationSuccessful: true`. `Unity_GetConsoleLogs` con
`logTypes: "error,warning"` justo después de aplicar los cuatro cambios
devolvió `errorCount: 0` (un solo warning ajeno, de
`Unity.AI.Toolkit.Accounts`, sin relación con el proyecto).

### Paso 6 del brief, sustituido por ejecución programática (sin ratón físico)

Siguiendo la instrucción del encargo, no se jugó con ratón. En su lugar:

1. `EditorSceneManager.OpenScene("Assets/Scenes/Ambiente1_Manipulacion.unity")`
   y `EditorApplication.isPlaying = true` vía `Unity_RunCommand`.
2. Confirmado `isPlaying = True` y `Object.FindFirstObjectByType<Ambiente1Manipulacion>()`
   devolvió el componente (`EspacioHaptico`).
3. Llamado `amb.Iniciar(); amb.Terminar();` → log `Estado = Terminado`.

Salida de disco tras el primer ensayo:

```
ls -R Analysis/datos
Analysis/datos/ANON:
ANON_a1_e01_traza.csv
ANON_a1_resumen.csv

head -2 Analysis/datos/ANON/ANON_a1_resumen.csv
sujeto,sesion,ensayo,condicion,fecha,guia,tiempo_s,piezas,agarres,caidas,recorrido_u,impulso_ns,fuerza_pico_n,err_medio_u,detalle_por_pieza
ANON,0,1,sin_guia,2026-09-02 16:33:12,sin_guia,0.00000,4,0,0,0.00000,0.00000,0.00000,0.00000,"Pieza ligera:0.49N/0ag/0ca/0.0s/0.000u | Pieza media:1.18N/0ag/0ca/0.0s/0.000u | Pieza pesada:1.96N/0ag/0ca/0.0s/0.000u | Pieza muy pesada:2.75N/0ag/0ca/0.0s/0.000u"
```

Coincide exactamente con lo esperado: existe `datos/ANON/` con
`ANON_a1_e01_traza.csv` y `ANON_a1_resumen.csv`; la primera línea del
resumen empieza por `sujeto,sesion,ensayo,condicion,fecha,guia,` y la
segunda por `ANON,0,1,sin_guia,`.

Repetido `amb.Iniciar(); amb.Terminar();` una segunda vez (con
`Reiniciar()` antes, sin efecto sobre la numeración porque `Ensayo` se
recalcula dentro de `Iniciar()`):

```
ls Analysis/datos/ANON
ANON_a1_e01_traza.csv
ANON_a1_e02_traza.csv
ANON_a1_resumen.csv

cat Analysis/datos/ANON/ANON_a1_resumen.csv
sujeto,sesion,ensayo,condicion,fecha,guia,tiempo_s,piezas,agarres,caidas,recorrido_u,impulso_ns,fuerza_pico_n,err_medio_u,detalle_por_pieza
ANON,0,1,sin_guia,2026-09-02 16:33:12,sin_guia,0.00000,4,0,0,0.00000,0.00000,0.00000,0.00000,"Pieza ligera:0.49N/0ag/0ca/0.0s/0.000u | Pieza media:1.18N/0ag/0ca/0.0s/0.000u | Pieza pesada:1.96N/0ag/0ca/0.0s/0.000u | Pieza muy pesada:2.75N/0ag/0ca/0.0s/0.000u"
ANON,0,2,sin_guia,2026-09-02 16:33:19,sin_guia,0.00000,4,0,0,0.00000,0.00000,0.00000,0.00000,"Pieza ligera:0.49N/0ag/0ca/0.0s/0.000u | Pieza media:1.18N/0ag/0ca/0.0s/0.000u | Pieza pesada:1.96N/0ag/0ca/0.0s/0.000u | Pieza muy pesada:2.75N/0ag/0ca/0.0s/0.000u"
```

Apareció `ANON_a1_e02_traza.csv` (sin pisar `_e01_`) y el resumen quedó con
dos filas de datos (`ensayo=1` y `ensayo=2`), confirmando que la
numeración de ensayo se recalcula por conteo de archivos en disco y no
pisa nada al repetir sin salir de la escena.

Se salió de Play Mode (`EditorApplication.isPlaying = false`).
`Unity_GetConsoleLogs` con `logTypes: "error"` tras salir: `totalCount: 0`.

### Paso 7 del brief — punto de control

- Los cuatro nombres de archivo salen con el patrón `<ID>_<amb>_e<NN>_<sufijo>`.
  Confirmado (`ANON_a1_e01_traza.csv`, `ANON_a1_e02_traza.csv`; el resumen
  usa `<ID>_<amb>_resumen.csv` como especifica `NombreArchivoResumen()`).
- El número de ensayo se incrementa sin pisar archivos. Confirmado con dos
  ensayos consecutivos en la misma sesión de Play.
- Las tres cabeceras de A1/A2/A3 empiezan por `sujeto,sesion,ensayo,condicion`.
  Confirmado literalmente para A1 (ver `head -2` arriba); A2 y A3 se
  transcribieron con el mismo patrón `CabeceraSujeto + ",fecha,..."` pero
  no se ejecutaron sus escenas (fuera del alcance del paso 6, que sólo
  pide A1) — su compilación limpia sí quedó verificada.
- Borrado `Analysis/datos/ANON/` al terminar (`rm -rf`). Confirmado con
  `ls Analysis/datos` → sólo queda `_pruebas_previas/`.

## Decisiones tomadas

- Transcripción verbatim del brief en los cinco puntos de código (bloque
  nuevo en `AmbienteBase`, las dos líneas de `Ensayo = SiguienteEnsayo();`,
  el bloque de guardado en `Terminar()`, y los cuatro cambios de
  `CabeceraResumen`/`resumen.Fila(` → `FilaResumen(` en A1/A2/A3).
- No se tocó ninguna otra línea de A1/A2/A3: ni métricas, ni lógica de
  ensayo, ni HUD, tal como exige el encargo.
- Verificación del paso 6 sustituida por llamadas programáticas a
  `Iniciar()`/`Terminar()` vía `Unity_RunCommand` en Play Mode, como
  indicó el encargo, en vez de manipular el ratón físico.
- No se creó ningún `.meta` a mano; no hay repositorio git en el
  proyecto, así que no se ejecutó ningún `git init/add/commit`.

## Fix round 1: verificacion de A2 y A3

Sin cambios de código: sólo se ejecutó el mismo procedimiento del paso 6
(Play Mode + `Iniciar()`/`Terminar()` por código vía `Unity_RunCommand`)
para las escenas `Ambiente2_Laberinto` y `Ambiente3_Fuerza`, que en la
entrega original de la tarea sólo habían quedado verificadas por
compilación.

### A2 — `Ambiente2Laberinto`

`EditorSceneManager.OpenScene("Assets/Scenes/Ambiente2_Laberinto.unity")`,
`EditorApplication.isPlaying = true`. `FindFirstObjectByType<Ambiente2Laberinto>()`
encontró el componente (`EspacioHaptico`). `amb.Iniciar()` → `Estado = EnCurso`;
`amb.Terminar()` → `Estado = Terminado`. Único log de consola durante el
ciclo: un warning esperado de `HapticDevice` (`No se pudo abrir el
dispositivo... Se usara el modo simulacion con el raton`), sin errores.

En disco:

```
ls Analysis/datos/ANON
ANON_a2_e01_traza.csv
ANON_a2_resumen.csv

cat Analysis/datos/ANON/ANON_a2_resumen.csv
sujeto,sesion,ensayo,condicion,fecha,nivel,semilla,tiempo_s,choques,t_contacto_s,f_choque_pico_n,recorrido_u,optimo_u,eficiencia,desv_rms_u,desv_max_u
ANON,0,1,sin_guia,2026-09-02 16:38:52,2,20250825,0.00000,0,0.00000,0.00000,0.00000,6.80000,0.00000,0.00000,0.00000
```

Confirmado: existe `ANON_a2_e01_traza.csv`; la cabecera de
`ANON_a2_resumen.csv` empieza por
`sujeto,sesion,ensayo,condicion,fecha,nivel,semilla,` y la fila de datos
empieza por `ANON,0,1,sin_guia,`.

### A3 — `Ambiente3Fuerza`

Salida de Play Mode de A2 primero. Luego
`EditorSceneManager.OpenScene("Assets/Scenes/Ambiente3_Fuerza.unity")`,
`EditorApplication.isPlaying = true`.
`FindFirstObjectByType<Ambiente3Fuerza>()` encontró el componente
(`EspacioHaptico`). `amb.Iniciar()` → `Estado = EnCurso`; `amb.Terminar()`
→ `Estado = Terminado`. Sin precondiciones adicionales: ambas escenas
arrancaron y `Iniciar()` dejó el ensayo en curso a la primera.

En disco:

```
ls Analysis/datos/ANON
ANON_a2_e01_traza.csv
ANON_a2_resumen.csv
ANON_a3_e01_traza.csv
ANON_a3_resumen.csv

cat Analysis/datos/ANON/ANON_a3_resumen.csv
sujeto,sesion,ensayo,condicion,fecha,estacion,nombre,k_nmm,b_nmms,fs_n,fk_n,tiempo_s,f_media_n,f_pico_n,f_arranque_n,impulso_ns,trabajo_nmm,completada,fuente
ANON,0,1,sin_guia,2026-09-02 16:39:16,1,Libre,0.00000,0.00000,0.00000,0.00000,0.00000,0.00000,0.00000,0.00000,0.00000,0.00000,0,modelo_unity
ANON,0,1,sin_guia,2026-09-02 16:39:16,2,Viscosa,0.00000,0.00400,0.00000,0.00000,0.00000,0.00000,0.00000,0.00000,0.00000,0.00000,0,modelo_unity
ANON,0,1,sin_guia,2026-09-02 16:39:16,3,Friccion seca,0.00000,0.00000,1.60000,0.70000,0.00000,0.00000,0.00000,0.00000,0.00000,0.00000,0,modelo_unity
ANON,0,1,sin_guia,2026-09-02 16:39:16,4,Resorte,0.03000,0.00000,0.00000,0.00000,0.00000,0.00000,0.00000,0.00000,0.00000,0.00000,0,modelo_unity
ANON,0,1,sin_guia,2026-09-02 16:39:16,5,Piston,0.01200,0.00250,0.70000,0.40000,0.00000,0.00000,0.00000,0.00000,0.00000,0.00000,0,modelo_unity
```

Confirmado: existe `ANON_a3_e01_traza.csv`; la cabecera de
`ANON_a3_resumen.csv` empieza por
`sujeto,sesion,ensayo,condicion,fecha,estacion,nombre,`; hay **cinco**
filas de datos, las cinco empiezan por `ANON,0,1,sin_guia,`, y la columna
`estacion` (6ª columna) va 1, 2, 3, 4, 5 en orden.

Conteo de campos (separados por `,`, vía `awk -F',' '{print NF}'`):
cabecera → **19 campos**; cada una de las cinco filas de datos → **19
campos** también. Cuadran exactamente: ninguna fila está descuadrada
respecto a la cabecera. (`FilaResumen` dentro del bucle de A3 antepone
las mismas cuatro columnas de sujeto a cada una de las cinco filas, tal
como preveía el brief, y aquí queda confirmado en disco en vez de
deducido.)

Salida de Play Mode de A3. `Unity_GetConsoleLogs` con `logTypes: "error"`
tras salir: `totalCount: 0`.

### Limpieza

`rm -rf Analysis/datos/ANON`. Confirmado con `ls Analysis/datos` → sólo
queda `_pruebas_previas/`.

Ninguna escena falló al arrancar y ningún `Iniciar()` dejó el ensayo en
un estado distinto de `EnCurso`; no hubo que resolver nada, sólo
verificar.

## Pendiente / dudas

- **El recorrido jugado a mano (agarrar las cuatro piezas con el ratón en
  Play Mode) queda pendiente de verificación manual.** La verificación de
  esta tarea cubrió el mismo camino de código (nombres de archivo,
  cabeceras, numeración de ensayo) llamando a `Iniciar()`/`Terminar()`
  por código, pero no ejercitó la interacción real del jugador con
  `ObjetoAgarrable`/`ZonaDestino` ni el dispositivo háptico. Esto aplica
  igual a A2 y A3 tras el fix round 1: se verificó el guardado en
  ejecución, no la interacción manual con laberinto/estaciones.
