# Task 5 -- Reporte: `ConfiguracionHanoi` y `hanoi.json`

## Estado: DONE

## Archivos creados

- `Assets/StreamingAssets/hanoi.json` (carpeta `StreamingAssets` creada, no existia)
- `Assets/Scripts/Ambientes/A4/ConfiguracionHanoi.cs`

Ambos transcritos verbatim del brief `task-5-brief.md`, sin cambios de valores.

## Paso 1: compilacion

`AssetDatabase.Refresh()` ejecutado por MCP contra el proyecto `Actividad4`
(confirmado por `Application.dataPath` en la respuesta:
`C:/Users/76905/Downloads/ReportesOMNI/Actividad4/Assets`).

`Unity_GetConsoleLogs` (logTypes=Error) tras el refresh: `errorCount: 0`. Sin
errores de compilacion.

## Paso 2: `Cargar()` con el JSON del brief

```
niveles.Length = 4
MasaDe(0,3) = 0.06
MasaDe(2,3) = 0.28
MasaDe(0,7) = 0.06
MasaDe(6,7) = 0.28
RadioDe(0,3) = 0.13
RadioDe(2,3) = 0.38
```

Coincide con lo esperado: extremos correctos en indice 0 y n-1, sin warnings
en consola.

## Paso 3: prueba de degradacion (los tres caminos)

### a) Sin archivo

Se renombro `hanoi.json` -> `hanoi.json.off`, se llamo `Cargar()`, se
restauro el nombre.

Resultado: `niveles.Length = 4` (valores por defecto).

Warning exacto en consola:
```
[Hanoi] No encuentro C:/Users/76905/Downloads/ReportesOMNI/Actividad4/Assets/StreamingAssets\hanoi.json. Uso los valores por defecto.
```

### b) JSON corrupto

Se escribio temporalmente `{{{no soy json` en `hanoi.json`, se llamo
`Cargar()`, se restauro el contenido correcto.

Resultado: `niveles.Length = 4` (valores por defecto).

Warning exacto en consola:
```
[Hanoi] No pude leer hanoi.json (JSON parse error: Missing a name for object member.). Uso los valores por defecto.
```

### c) JSON valido sin niveles

Se escribio temporalmente `{"niveles": []}`, se llamo `Cargar()`, se
restauro el contenido correcto.

Resultado: `niveles.Length = 4` (valores por defecto).

Warning exacto en consola:
```
[Hanoi] hanoi.json no trae niveles. Uso los valores por defecto.
```

Los tres caminos degradan a `PorDefecto()` (4 niveles) y avisan en consola,
tal como exige la regla de oro: el ambiente nunca deja de arrancar por culpa
del archivo.

## Paso 4: prueba de saturacion

JSON temporal:
```json
{
  "niveles": [
    {"nombre": "Extremo", "discos": 99, "guiaPermitida": true},
    {"nombre": "Minimo",  "discos": 1,  "guiaPermitida": false}
  ],
  "masaMin": 0.5,
  "masaMax": 0.9,
  ...
}
```

Resultado tras `Cargar()`:
```
niveles.Length = 2
nivel[0] nombre=Extremo discos=7
nivel[1] nombre=Minimo discos=3
masaMin = 0.3
masaMax = 0.3
```

- `discos: 99` -> saturado a `7` (`EstadoHanoi.MaxDiscos`).
- `discos: 1` -> saturado a `3` (`EstadoHanoi.MinDiscos`).
- `masaMin: 0.5` -> saturado a `0.3` (`MasaTope`).
- `masaMax: 0.9` -> saturado a `Clamp(0.9, masaMin=0.3, MasaTope=0.3) = 0.3`.

Todo dentro de 3..7 discos y masas `<= 0.30` kg (por debajo del limite de 3 N
del Touch, ~2.94 N). Contenido correcto restaurado despues de la prueba.

## Paso 5: contenido final de `Assets/StreamingAssets/hanoi.json`

Verificado byte a byte contra el brief tras la ultima restauracion; consola
sin warnings ni errores en la carga final (`niveles.Length=4, masaMin=0.06,
masaMax=0.28`).

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

## Dudas / observaciones

- Ninguna duda tecnica pendiente. Nota de proceso: el tool `Unity_RunCommand`
  reporta como "error" (`UNEXPECTED_ERROR`) cualquier ejecucion que emita un
  `LogWarning`, aun cuando la ejecucion en si fue exitosa (`isExecutionSuccessful`
  implicito, logs presentes). Esto es esperado para los tres casos de
  degradacion del paso 3, que deliberadamente disparan `Debug.LogWarning`; no
  indica un fallo real del codigo.
- No se creo ningun `.meta` a mano; Unity los genera solo al hacer
  `AssetDatabase.Refresh()` sobre los archivos nuevos.
- No hay repositorio git en este proyecto (confirmado por el entorno), asi
  que no se ejecuto ningun comando de git.
