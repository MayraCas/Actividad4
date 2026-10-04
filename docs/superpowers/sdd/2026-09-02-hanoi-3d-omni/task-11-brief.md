### Task 11: `analizar_hanoi.py`

**Files:**
- Create: `Analysis/analizar_hanoi.py`

**Interfaces:**
- Consumes: `comun.cargar_todos`, `comun.num`, `comun.txt`, `comun.guardar`, `comun.falta`, `comun.tabla`, `comun.encabezado`, `comun.imprimir_1khz`, `comun.estilo`, `comun.COLORES` (tarea 10); los CSV de la tarea 7.
- Produces: `analizar_hanoi.analizar()` y cuatro figuras en `Analysis/figuras/`.

- [ ] **Paso 1: Escribir el análisis**

`Analysis/analizar_hanoi.py`:

```python
# analizar_hanoi.py -- Ambiente 4: Torres de Hanoi hapticas.
#
# Lee las trazas, las bitacoras de movimientos y los resumenes de todos los
# sujetos y responde a lo que pide la actividad para este ambiente: cuanto se
# alejo cada persona del optimo, si los discos pesados costaron mas, y cuanto
# empujaron contra la barrera de rechazo.
#
#   python Analysis/analizar_hanoi.py
import numpy as np
import matplotlib.pyplot as plt

from comun import (estilo, cargar_todos, num, txt, guardar, falta, tabla,
                   encabezado, imprimir_1khz, COLORES, comprobar_datos)


def analizar():
    encabezado("AMBIENTE 4 - TORRES DE HANOI")

    resumen = cargar_todos("*_hanoi_resumen.csv")
    if resumen is None:
        falta("*_hanoi_resumen.csv")
        return
    estilo()

    tabla_de_ensayos(resumen)
    figura_eficiencia(resumen)

    movs = cargar_todos("*_hanoi_e*_movs.csv")
    if movs is not None:
        figura_aprendizaje(movs)
        figura_por_disco(movs)
        tabla_de_rechazo(movs)

    traza = cargar_todos("*_hanoi_e*_traza.csv")
    if traza is not None:
        figura_trayectoria(traza)

    imprimir_1khz("*_hanoi_e*_1khz.csv")


# ---------------------------------------------------------------------------
def tabla_de_ensayos(r):
    filas = []
    for i in range(r["__n__"]):
        filas.append([
            r["sujeto"][i],
            r["sesion"][i],
            r["ensayo"][i],
            r["nivel"][i],
            r["discos"][i],
            "si" if r["completado"][i] in ("1", "True", "true") else "no",
            "%.1f" % float(r["tiempo_s"][i] or 0),
            "%s/%s" % (r["movimientos"][i], r["movimientos_optimos"][i]),
            "%.2f" % float(r["eficiencia"][i] or 0),
            r["intentos_ilegales"][i],
            r["caidas"][i],
            "%.2f" % float(r["fuerza_rechazo_pico_n"][i] or 0),
            r["guia"][i],
        ])
    tabla("ENSAYOS",
          ["sujeto", "ses", "ens", "nivel", "n", "fin", "t (s)", "mov/opt",
           "efic", "ileg", "caid", "rechazo (N)", "guia"],
          filas)


def figura_eficiencia(r):
    """Eficiencia frente al numero de discos, una serie por sujeto. Es la lectura
    central del ambiente: 1.0 es resolverlo en el minimo teorico."""
    discos = num(r, "discos")
    efic = num(r, "eficiencia")
    sujeto = txt(r, "sujeto")

    fig, ax = plt.subplots(figsize=(7, 4))
    for k, s in enumerate(dict.fromkeys(sujeto)):
        sel = sujeto == s
        orden = np.argsort(discos[sel])
        ax.plot(discos[sel][orden], efic[sel][orden], "o-",
                color=COLORES[k % len(COLORES)], label=str(s))
    ax.axhline(1.0, color="#8899AA", linestyle="--", linewidth=1)
    ax.set_xlabel("discos")
    ax.set_ylabel("eficiencia  (2^n-1) / movimientos")
    ax.set_title("Cercania al optimo segun la dificultad")
    ax.set_ylim(0, 1.1)
    ax.legend()
    guardar(fig, "hanoi_eficiencia.png")


def figura_aprendizaje(m):
    """Duracion de cada movimiento a lo largo del ensayo. Si la curva baja, el
    sujeto se esta soltando con el equipo."""
    n = num(m, "n_mov")
    dur = num(m, "t_fin") - num(m, "t_inicio")
    sujeto = txt(m, "sujeto")

    fig, ax = plt.subplots(figsize=(7, 4))
    for k, s in enumerate(dict.fromkeys(sujeto)):
        sel = sujeto == s
        orden = np.argsort(n[sel])
        ax.plot(n[sel][orden], dur[sel][orden], "-", alpha=0.55,
                color=COLORES[k % len(COLORES)], label=str(s))
    ax.set_xlabel("numero de movimiento")
    ax.set_ylabel("duracion del movimiento (s)")
    ax.set_title("Curva de aprendizaje")
    ax.legend()
    guardar(fig, "hanoi_aprendizaje.png")


def figura_por_disco(m):
    """Tiempo e impulso medios por disco. El disco 1 es el mas pequeno y ligero;
    el ultimo, el mas grande y pesado. Aqui se ve si el peso costo."""
    disco = num(m, "disco").astype(int)
    dur = num(m, "t_fin") - num(m, "t_inicio")
    imp = num(m, "impulso_ns")

    indices = sorted(set(disco.tolist()))
    tmed = [float(np.mean(dur[disco == d])) for d in indices]
    imed = [float(np.mean(imp[disco == d])) for d in indices]
    etiquetas = [str(d + 1) for d in indices]

    fig, (a1, a2) = plt.subplots(1, 2, figsize=(9, 4))
    a1.bar(etiquetas, tmed, color=COLORES[0])
    a1.set_xlabel("disco (1 = el mas pequeno)")
    a1.set_ylabel("tiempo medio por movimiento (s)")
    a1.set_title("Cuanto cuesta cada disco")

    a2.bar(etiquetas, imed, color=COLORES[2])
    a2.set_xlabel("disco (1 = el mas pequeno)")
    a2.set_ylabel("impulso medio  integral|F|dt  (N s)")
    a2.set_title("Esfuerzo por disco")
    guardar(fig, "hanoi_por_disco.png")

    tabla("COSTE POR DISCO",
          ["disco", "movimientos", "t medio (s)", "impulso medio (N s)"],
          [[e, int(np.sum(disco == d)), "%.2f" % t, "%.2f" % i]
           for e, d, t, i in zip(etiquetas, indices, tmed, imed)])


def tabla_de_rechazo(m):
    """Cuantos intentos ilegales hubo por movimiento y con cuanta fuerza pico. Es
    la metrica que solo existe porque la barrera es haptica."""
    ilegales = num(m, "intentos_ilegales")
    pico = num(m, "fuerza_pico_n")
    sujeto = txt(m, "sujeto")

    filas = []
    for s in dict.fromkeys(sujeto):
        sel = sujeto == s
        filas.append([s,
                      int(np.sum(sel)),
                      int(np.sum(ilegales[sel])),
                      "%.2f" % float(np.mean(ilegales[sel])),
                      "%.2f" % float(np.max(pico[sel]) if np.any(sel) else 0.0)])
    tabla("BARRERA DE RECHAZO",
          ["sujeto", "movimientos", "intentos ilegales", "por movimiento", "F pico (N)"],
          filas)


def figura_trayectoria(t):
    """La trayectoria de la punta del ensayo mas largo disponible, con los tramos
    de rechazo marcados en rojo."""
    archivo = txt(t, "archivo")
    cuales = list(dict.fromkeys(archivo))
    elegido = max(cuales, key=lambda a: int(np.sum(archivo == a)))
    sel = archivo == elegido

    x, y, z = num(t, "x")[sel], num(t, "y")[sel], num(t, "z")[sel]
    ilegal = num(t, "ilegal")[sel] > 0.5

    fig = plt.figure(figsize=(6.5, 5))
    ax = fig.add_subplot(111, projection="3d")
    ax.plot(x, z, y, color=COLORES[0], linewidth=0.8, alpha=0.85)
    if np.any(ilegal):
        ax.scatter(x[ilegal], z[ilegal], y[ilegal], s=4,
                   color=COLORES[3], label="contra la barrera")
        ax.legend()
    ax.set_xlabel("x (u)")
    ax.set_ylabel("z (u)")
    ax.set_zlabel("y (u)")
    ax.set_title("Trayectoria de la punta · %s" % elegido)
    guardar(fig, "hanoi_trayectoria.png")


if __name__ == "__main__":
    comprobar_datos()
    analizar()
```

- [ ] **Paso 2: Correr el análisis con datos reales**

Hacer dos ensayos de Hanói con `S01` (uno fácil, uno de 5 discos) y uno con
`S02`, provocando a propósito algún intento ilegal y alguna caída. Luego:

```bash
cd "C:/Users/76905/Downloads/ReportesOMNI"
python Actividad4/Analysis/analizar_hanoi.py
```

Esperado: cuatro figuras en `Actividad4/Analysis/figuras/`
(`hanoi_eficiencia.png`, `hanoi_aprendizaje.png`, `hanoi_por_disco.png`,
`hanoi_trayectoria.png`) y tres tablas en consola. Comprobar en la tabla de
ensayos que `mov/opt` y `efic` cuadran con lo que decía el HUD al terminar.

- [ ] **Paso 3: Correr el análisis completo**

```bash
python Actividad4/Analysis/analizar_todo.py
```

Esperado: la tabla de participantes seguida de los cuatro ambientes, sin
excepciones. Los ambientes sin datos deben decir "(falta ...)" en vez de fallar.

- [ ] **Paso 4: Punto de control**

- `analizar_hanoi.py` corre solo y desde `analizar_todo.py`.
- Con un único sujeto y sin intentos ilegales tampoco falla (la figura de rechazo simplemente no marca puntos).
- Las figuras usan el estilo oscuro común, para que peguen con las capturas de las escenas en el reporte.

---

## Verificación final

Antes de dar el trabajo por terminado:

1. `Window > General > Test Runner > EditMode > Run All` — los 13 tests de `EstadoHanoiTests` en verde.
2. `Haptics > Construir TODOS los ambientes` — cinco escenas sin errores en consola.
3. `Haptics > Capturar imagenes de los ambientes` — cinco PNG en `Capturas/`.
4. Un recorrido completo con el equipo conectado: registrar sujeto, un ensayo de cada ambiente, comprobar que `Analysis/datos/<ID>/` tiene los archivos de los cuatro.
5. `python Actividad4/Analysis/analizar_todo.py` sin excepciones.
6. Comprobar a mano, con el Touch en la mano, lo que ningún test puede: que el disco grande se siente claramente más pesado que el pequeño, que la barrera de rechazo se lee como una pared y no como algo blando, y que el HUD no marca `Unstable` durante el ensayo.
