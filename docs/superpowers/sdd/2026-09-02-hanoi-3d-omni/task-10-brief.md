### Task 10: Lectura multi-sujeto en Python

**Files:**
- Modify: `Analysis/comun.py` (añadir funciones; sustituir `cargar` por `cargar_todos`; `resumen_1khz` pasa a trabajar por patrón)
- Modify: `Analysis/analizar_a1.py:10-11, 17, 90, 93, 140`
- Modify: `Analysis/analizar_a2.py:11-12, 18, 105, 135`
- Modify: `Analysis/analizar_a3.py:13-14, 37, 100, 103, 161, 185`
- Modify: `Analysis/analizar_todo.py`

**Interfaces:**
- Consumes: la estructura de carpetas de las tareas 2 y 3.
- Produces:
  - `comun.carpetas_sujeto(incluir_anon=False)` → `list[tuple[str, str]]`
  - `comun.archivos(patron, incluir_anon=False)` → `list[tuple[str, str]]` (sujeto, ruta)
  - `comun.cargar_todos(patron, incluir_anon=False)` → `dict | None`, con columna `sujeto` garantizada
  - `comun.cargar_ruta(ruta)` → `dict | None`
  - `comun.sujetos()` → `dict | None`
  - `comun.resumen_1khz(patron)` y `comun.imprimir_1khz(patron)` aceptan patrón glob

- [ ] **Paso 1: Reescribir la carga en `comun.py`**

Añadir `import fnmatch` junto a los otros `import`, y sustituir la función
`cargar` (líneas 57-69) por este bloque:

```python
# Carpetas que no son de un sujeto real.
IGNORAR = ("_pruebas_previas", "figuras")


def carpetas_sujeto(incluir_anon=False):
    """Las carpetas de Analysis/datos que corresponden a un sujeto.

    Una carpeta por sujeto es la fuente unica de los datos: no hay tabla
    maestra que mantener sincronizada. ANON es el modo libre de desarrollo y se
    excluye por omision para que las pruebas del equipo no entren al analisis."""
    if not os.path.isdir(DATOS):
        return []
    salida = []
    for nombre in sorted(os.listdir(DATOS)):
        ruta = os.path.join(DATOS, nombre)
        if not os.path.isdir(ruta):
            continue
        if nombre in IGNORAR or nombre.startswith("_"):
            continue
        if nombre == "ANON" and not incluir_anon:
            continue
        salida.append((nombre, ruta))
    return salida


def archivos(patron, incluir_anon=False):
    """Rutas que casan con un glob, en todas las carpetas de sujeto.
    Devuelve pares (sujeto, ruta) ordenados."""
    salida = []
    for sid, carpeta in carpetas_sujeto(incluir_anon):
        for nombre in sorted(os.listdir(carpeta)):
            if fnmatch.fnmatch(nombre, patron):
                salida.append((sid, os.path.join(carpeta, nombre)))
    return salida


def cargar_ruta(ruta):
    """Lee un CSV concreto como diccionario de columnas (listas de texto)."""
    if not os.path.exists(ruta):
        return None
    with open(ruta, "r", encoding="utf-8-sig", newline="") as f:
        filas = list(csv.DictReader(f))
    if not filas:
        return None
    cols = {k: [fila.get(k, "") or "" for fila in filas] for k in filas[0].keys()}
    cols["__n__"] = len(filas)
    return cols


def cargar_todos(patron, incluir_anon=False):
    """Concatena todos los archivos que casan con el glob, de todos los sujetos.

    Devuelve un diccionario de columnas con la union de las claves encontradas;
    las celdas que un archivo no traiga quedan vacias. Siempre hay columna
    'sujeto': si el CSV no la trae, se rellena con el nombre de la carpeta.
    Devuelve None si no hay nada que leer todavia."""
    filas = []
    for sid, ruta in archivos(patron, incluir_anon):
        with open(ruta, "r", encoding="utf-8-sig", newline="") as f:
            for fila in csv.DictReader(f):
                if not fila.get("sujeto"):
                    fila["sujeto"] = sid
                fila["archivo"] = os.path.basename(ruta)
                filas.append(fila)
    if not filas:
        return None

    claves = []
    for fila in filas:
        for k in fila:
            if k not in claves:
                claves.append(k)
    cols = {k: [fila.get(k, "") or "" for fila in filas] for k in claves}
    cols["__n__"] = len(filas)
    return cols


def sujetos():
    """La tabla de participantes, o None si nadie se ha dado de alta."""
    return cargar_ruta(os.path.join(DATOS, "sujetos.csv"))
```

- [ ] **Paso 2: Hacer que el análisis del bucle de 1 kHz trabaje por patrón**

Sustituir `resumen_1khz` y `imprimir_1khz` por:

```python
def resumen_1khz(patron):
    """Comprueba la calidad del bucle haptico en el CSV nativo mas reciente que
    case con el patron: frecuencia real, jitter y ciclos largos.

    Se mira UN archivo y no la concatenacion de todos porque el salto de tiempo
    entre dos grabaciones distintas no es un ciclo del servo loop y falsearia el
    jitter."""
    encontrados = archivos(patron)
    if not encontrados:
        return None
    ruta = max((r for _, r in encontrados), key=os.path.getmtime)
    cols = cargar_ruta(ruta)
    if cols is None:
        return None
    t = num(cols, "t")
    if len(t) < 10:
        return None
    dt = np.diff(t)
    dt = dt[(dt > 0) & (dt < 0.1)]
    if len(dt) == 0:
        return None
    return {
        "archivo": os.path.basename(ruta),
        "muestras": len(t),
        "duracion": float(t[-1] - t[0]),
        "hz_medio": float(1.0 / np.mean(dt)),
        "jitter_ms": float(np.std(dt) * 1000.0),
        "pct_lentos": float(100.0 * np.mean(dt > 0.002)),
    }


def imprimir_1khz(patron):
    r = resumen_1khz(patron)
    if r is None:
        return
    print()
    print("Bucle haptico (%s)" % r["archivo"])
    print("  muestras          %d en %.1f s" % (r["muestras"], r["duracion"]))
    print("  frecuencia real   %.1f Hz" % r["hz_medio"])
    print("  jitter            sigma = %.3f ms" % r["jitter_ms"])
    print("  ciclos > 2 ms     %.2f %%" % r["pct_lentos"])
```

Actualizar también el comentario de cabecera del archivo (líneas 1-6):

```python
# comun.py -- Utilidades compartidas por los analisis de la Actividad 4.
#
# Los CSV los escribe Unity en Analysis/datos/<ID del sujeto>/. Se leen con el
# modulo csv de la biblioteca estandar (no pandas) para que el unico requisito
# real sean numpy y matplotlib, igual que en la Actividad 3:
#
#     pip install numpy matplotlib
#
# La lectura es siempre multi-sujeto: cargar_todos recorre las carpetas de
# sujeto y concatena, anadiendo la columna 'sujeto' si el CSV no la trae.
```

- [ ] **Paso 3: Actualizar `analizar_a1.py`**

- Línea 10-11: cambiar `cargar` por `cargar_todos` en la lista de importaciones.
- Línea 17: `traza = cargar("a1_traza.csv")` → `traza = cargar_todos("*_a1_e*_traza.csv")`
- Línea 18-19 (`falta("a1_traza.csv")`) → `falta("*_a1_e*_traza.csv")`
- Línea 90: `resumen = cargar("a1_resumen.csv")` → `resumen = cargar_todos("*_a1_resumen.csv")`
- Líneas 93 y 140: `imprimir_1khz("a1_1khz.csv")` → `imprimir_1khz("*_a1_e*_1khz.csv")`

Y en la rama en que `resumen` es `None`, si el mensaje menciona `a1_resumen.csv`,
cambiarlo a `*_a1_resumen.csv`.

- [ ] **Paso 4: Actualizar `analizar_a2.py`**

- Línea 11-12: `cargar` → `cargar_todos`.
- Línea 18: `cargar("a2_traza.csv")` → `cargar_todos("*_a2_e*_traza.csv")` (y el `falta` correspondiente).
- Línea 105: `cargar("a2_resumen.csv")` → `cargar_todos("*_a2_resumen.csv")`.
- Línea 135: `imprimir_1khz("a2_1khz.csv")` → `imprimir_1khz("*_a2_e*_1khz.csv")`.

- [ ] **Paso 5: Actualizar `analizar_a3.py`**

- Línea 13-14: `cargar` → `cargar_todos` (y añadir `cargar_ruta` si hiciera falta; no lo hace).
- Línea 37: `cargar("a3_traza.csv")` → `cargar_todos("*_a3_e*_traza.csv")` (y el `falta`).
- Línea 100: `cargar("a3_resumen.csv")` → `cargar_todos("*_a3_resumen.csv")`.
- Líneas 103 y 185: `imprimir_1khz("a3_1khz.csv")` → `imprimir_1khz("*_a3_e*_1khz.csv")`.
- Línea 161: `mil = cargar("a3_1khz.csv")` → `mil = cargar_todos("*_a3_e*_1khz.csv")`.

- [ ] **Paso 6: Añadir Hanói a `analizar_todo.py`**

Sustituir el archivo entero por:

```python
# analizar_todo.py -- Ejecuta los cuatro analisis de una vez.
#
#   pip install numpy matplotlib      (solo la primera vez)
#   python Analysis/analizar_todo.py
#
# Las figuras quedan en Analysis/figuras/ y las tablas se imprimen en la
# consola listas para copiarse al reporte.
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from comun import comprobar_datos, encabezado, tabla, sujetos, FIGURAS
import analizar_a1
import analizar_a2
import analizar_a3
import analizar_hanoi


def listar_sujetos():
    s = sujetos()
    if s is None:
        print("  (todavia no hay nadie dado de alta en sujetos.csv)")
        return
    filas = list(zip(s["id"], s["fecha"], s["edad"], s["mano"],
                     s["experiencia"], s["sesion"]))
    tabla("PARTICIPANTES",
          ["id", "fecha", "edad", "mano", "experiencia", "sesion"],
          filas)


def main():
    comprobar_datos()
    encabezado("SUJETOS")
    listar_sujetos()
    analizar_a1.analizar()
    analizar_a2.analizar()
    analizar_a3.analizar()
    analizar_hanoi.analizar()
    encabezado("LISTO")
    print("Figuras en: %s" % FIGURAS)


if __name__ == "__main__":
    main()
```

(Este archivo importa `analizar_hanoi`, que se escribe en la tarea 11. Ejecutar
`analizar_todo.py` fallará hasta entonces; los tres análisis individuales sí se
pueden correr sueltos.)

- [ ] **Paso 7: Verificar la lectura multi-sujeto**

Generar datos de dos sujetos: abrir `Menu`, registrar `S01`, hacer un ensayo del
ambiente 1, volver al menú, *Cambiar de sujeto*, registrar `S02` y hacer otro.

```bash
cd "C:/Users/76905/Downloads/ReportesOMNI"
python Actividad4/Analysis/analizar_a1.py
```

Esperado: corre sin error y las tablas incluyen filas de los dos sujetos. Además:

```bash
python -c "import sys; sys.path.insert(0,'Actividad4/Analysis'); import comun; c=comun.cargar_todos('*_a1_resumen.csv'); print(sorted(set(c['sujeto'])))"
```

Esperado: `['S01', 'S02']`.

- [ ] **Paso 8: Punto de control**

- `cargar_todos` devuelve `None` limpio cuando no hay datos, y no revienta con carpetas vacías.
- `_pruebas_previas/` y `ANON/` quedan fuera del análisis.
- Los tres análisis existentes corren con el formato nuevo.

---

