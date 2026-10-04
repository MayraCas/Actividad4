# comun.py -- Utilidades compartidas por los tres analisis de la Actividad 4.
#
# Los CSV los escribe Unity en Analysis/datos/. Se leen con el modulo csv de la
# biblioteca estandar (no pandas) para que el unico requisito real sean numpy y
# matplotlib, igual que en la Actividad 3:
#
#     pip install numpy matplotlib
import csv
import os
import sys

import numpy as np
import matplotlib
matplotlib.use("Agg")            # sin ventana: solo genera PNG
import matplotlib.pyplot as plt

AQUI   = os.path.dirname(os.path.abspath(__file__))
DATOS  = os.path.join(AQUI, "datos")
FIGURAS = os.path.join(AQUI, "figuras")

# Paleta consistente con la de las escenas de Unity.
COLORES = ["#5CCCFF", "#73E68C", "#FFB847", "#F26B72", "#AE8CF9"]
FONDO   = "#0E1219"
TINTA   = "#E6EBF2"
REJILLA = "#2A3340"


def estilo():
    """Estilo oscuro comun a todas las figuras, para que peguen con las capturas
    de las escenas dentro del reporte."""
    plt.rcParams.update({
        "figure.facecolor": FONDO,
        "axes.facecolor":   FONDO,
        "savefig.facecolor": FONDO,
        "axes.edgecolor":   REJILLA,
        "axes.labelcolor":  TINTA,
        "axes.titlecolor":  TINTA,
        "axes.titlesize":   11,
        "axes.titleweight": "bold",
        "text.color":       TINTA,
        "xtick.color":      TINTA,
        "ytick.color":      TINTA,
        "grid.color":       REJILLA,
        "grid.linestyle":   "-",
        "grid.linewidth":   0.6,
        "axes.grid":        True,
        "legend.framealpha": 0.0,
        "font.size":        9,
        "figure.dpi":       120,
    })


def ruta_datos(nombre):
    return os.path.join(DATOS, nombre)


def cargar(nombre):
    """Lee un CSV como diccionario de columnas (listas de texto).
    Devuelve None si el archivo no existe todavia."""
    ruta = ruta_datos(nombre)
    if not os.path.exists(ruta):
        return None
    with open(ruta, "r", encoding="utf-8-sig", newline="") as f:
        filas = list(csv.DictReader(f))
    if not filas:
        return None
    cols = {k: [fila[k] for fila in filas] for k in filas[0].keys()}
    cols["__n__"] = len(filas)
    return cols


def num(cols, clave, por_defecto=0.0):
    """Columna como arreglo de numpy de flotantes, tolerando celdas vacias."""
    salida = []
    for v in cols.get(clave, []):
        try:
            salida.append(float(v))
        except (TypeError, ValueError):
            salida.append(por_defecto)
    return np.array(salida, dtype=float)


def txt(cols, clave):
    return np.array(cols.get(clave, []), dtype=object)


def guardar(fig, nombre):
    os.makedirs(FIGURAS, exist_ok=True)
    ruta = os.path.join(FIGURAS, nombre)
    fig.tight_layout()
    fig.savefig(ruta, bbox_inches="tight")
    plt.close(fig)
    print("  figura -> " + os.path.relpath(ruta, AQUI))
    return ruta


def falta(nombre):
    print("  (falta %s: ejecuta el ambiente en Unity y termina un ensayo)" % nombre)


def tabla(titulo, cabeceras, filas):
    """Imprime una tabla alineada en la consola. Es lo que se copia al reporte."""
    anchos = [len(str(c)) for c in cabeceras]
    for f in filas:
        for i, c in enumerate(f):
            anchos[i] = max(anchos[i], len(str(c)))
    linea = "  ".join("-" * a for a in anchos)
    print()
    print(titulo)
    print(linea)
    print("  ".join(str(c).ljust(anchos[i]) for i, c in enumerate(cabeceras)))
    print(linea)
    for f in filas:
        print("  ".join(str(c).ljust(anchos[i]) for i, c in enumerate(f)))
    print(linea)


def resumen_1khz(nombre):
    """Comprueba la calidad del bucle haptico en el CSV nativo: frecuencia real,
    jitter y ciclos largos. Es la misma verificacion de la Actividad 3, repetida
    aqui porque los ambientes cargan mas el servo loop (16 primitivas por ciclo
    en vez de 8 esferas)."""
    cols = cargar(nombre)
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
        "muestras": len(t),
        "duracion": float(t[-1] - t[0]),
        "hz_medio": float(1.0 / np.mean(dt)),
        "jitter_ms": float(np.std(dt) * 1000.0),
        "pct_lentos": float(100.0 * np.mean(dt > 0.002)),
    }


def imprimir_1khz(nombre):
    r = resumen_1khz(nombre)
    if r is None:
        return
    print()
    print("Bucle haptico (%s)" % nombre)
    print("  muestras          %d en %.1f s" % (r["muestras"], r["duracion"]))
    print("  frecuencia real   %.1f Hz" % r["hz_medio"])
    print("  jitter            sigma = %.3f ms" % r["jitter_ms"])
    print("  ciclos > 2 ms     %.2f %%" % r["pct_lentos"])


def encabezado(titulo):
    print()
    print("=" * 72)
    print(titulo)
    print("=" * 72)


def comprobar_datos():
    if not os.path.isdir(DATOS):
        print("No existe %s todavia." % DATOS)
        print("Ejecuta las escenas en Unity y termina al menos un ensayo de cada ambiente.")
        sys.exit(1)
