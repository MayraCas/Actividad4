# analizar_a1.py -- Ambiente 1: manipulacion de objetos.
#
# Lee Analysis/datos/a1_traza.csv y a1_resumen.csv y responde a lo que pide la
# actividad para este ambiente: cuanto costo tomar, mover y soltar cada pieza.
#
#   python Analysis/analizar_a1.py
import numpy as np
import matplotlib.pyplot as plt

from comun import (estilo, cargar, num, txt, guardar, falta, tabla,
                   encabezado, imprimir_1khz, COLORES, comprobar_datos)


def analizar():
    encabezado("AMBIENTE 1 - MANIPULACION DE OBJETOS")

    traza = cargar("a1_traza.csv")
    if traza is None:
        falta("a1_traza.csv")
        return
    estilo()

    t     = num(traza, "t")
    x, y, z = num(traza, "x"), num(traza, "y"), num(traza, "z")
    fmag  = num(traza, "fmag")
    agarr = num(traza, "agarrada") > 0.5
    pieza = txt(traza, "pieza")
    peso  = num(traza, "peso_n")

    dt = np.gradient(t) if len(t) > 1 else np.array([0.0])

    # ---- reparto del tiempo por pieza -------------------------------------
    nombres = [p for p in dict.fromkeys(pieza) if p != "-"]
    filas = []
    for i, nom in enumerate(nombres):
        sel = (pieza == nom) & agarr
        if not np.any(sel):
            continue
        filas.append([
            nom,
            "%.2f" % float(np.max(peso[sel])),
            "%.1f" % float(np.sum(dt[sel])),
            "%.2f" % float(np.mean(fmag[sel])),
            "%.2f" % float(np.max(fmag[sel])),
            "%.2f" % float(np.sum(fmag[sel] * dt[sel])),
        ])
    if filas:
        tabla("Tiempo y esfuerzo por pieza (solo mientras estuvo en la mano)",
              ["pieza", "peso N", "t (s)", "F media N", "F pico N", "impulso N·s"],
              filas)

    t_mano = float(np.sum(dt[agarr]))
    print()
    print("Duracion total del ensayo   %.1f s" % (t[-1] - t[0]))
    print("Tiempo con algo en la mano  %.1f s  (%.0f %%)"
          % (t_mano, 100.0 * t_mano / max(1e-6, t[-1] - t[0])))

    # ---- figura 1: recorrido de la punta ----------------------------------
    fig = plt.figure(figsize=(11, 4.2))

    ax = fig.add_subplot(1, 2, 1, projection="3d")
    ax.plot(x, z, y, lw=0.8, color="#3B4A5E", label="mano vacia")
    for i, nom in enumerate(nombres):
        sel = (pieza == nom) & agarr
        if np.any(sel):
            ax.plot(x[sel], z[sel], y[sel], lw=1.8,
                    color=COLORES[i % len(COLORES)], label=nom)
    ax.set_xlabel("X (u)"); ax.set_ylabel("Z (u)"); ax.set_zlabel("Y (u)")
    ax.set_title("Recorrido de la punta")
    ax.legend(fontsize=7, loc="upper left")
    ax.set_facecolor("#0E1219")

    # ---- figura 1b: fuerza en el tiempo ------------------------------------
    ax2 = fig.add_subplot(1, 2, 2)
    ax2.plot(t, fmag, lw=0.9, color="#5CCCFF", label="|F| aplicada")
    for i, nom in enumerate(nombres):
        sel = (pieza == nom) & agarr
        if np.any(sel):
            ax2.fill_between(t, 0, np.max(fmag) * 1.05, where=sel, alpha=0.16,
                             color=COLORES[i % len(COLORES)], step="mid")
    # El peso teorico de lo que se lleva: la referencia contra la que se compara.
    ax2.plot(t, peso, lw=1.0, ls="--", color="#FFB847", label="peso m·g de la pieza")
    ax2.set_xlabel("t (s)"); ax2.set_ylabel("N")
    ax2.set_title("Fuerza devuelta a la mano · bandas = pieza sujeta")
    ax2.legend(fontsize=7)

    guardar(fig, "a1_manipulacion.png")

    # ---- figura 2: resumen de ensayos --------------------------------------
    resumen = cargar("a1_resumen.csv")
    if resumen is None:
        falta("a1_resumen.csv")
        imprimir_1khz("a1_1khz.csv")
        return

    guia    = txt(resumen, "guia")
    tiempo  = num(resumen, "tiempo_s")
    agarres = num(resumen, "agarres")
    caidas  = num(resumen, "caidas")
    recorr  = num(resumen, "recorrido_u")
    errmed  = num(resumen, "err_medio_u")

    tabla("Ensayos registrados",
          ["#", "guia", "t (s)", "agarres", "caidas", "recorrido u", "error medio u"],
          [[i + 1, guia[i], "%.1f" % tiempo[i], int(agarres[i]), int(caidas[i]),
            "%.2f" % recorr[i], "%.3f" % errmed[i]] for i in range(len(tiempo))])

    # Si hay ensayos con y sin guia haptica, la comparacion es el resultado
    # interesante: mide para que sirve la asistencia.
    con = guia == "con_guia"
    sin = guia == "sin_guia"
    if np.any(con) and np.any(sin):
        print()
        print("Efecto de la guia haptica")
        for etiqueta, valores in (("tiempo (s)", tiempo),
                                  ("agarres", agarres),
                                  ("caidas", caidas),
                                  ("recorrido (u)", recorr),
                                  ("error de colocacion (u)", errmed)):
            a, b = float(np.mean(valores[sin])), float(np.mean(valores[con]))
            delta = 100.0 * (b - a) / a if abs(a) > 1e-9 else 0.0
            print("  %-26s sin guia %8.3f    con guia %8.3f    %+6.1f %%"
                  % (etiqueta, a, b, delta))

    n = np.arange(1, len(tiempo) + 1)
    fig2, ejes = plt.subplots(1, 4, figsize=(12, 3.0))
    for ax, valores, titulo, unidad in (
            (ejes[0], tiempo,  "Tiempo",              "s"),
            (ejes[1], agarres, "Agarres",             ""),
            (ejes[2], caidas,  "Caidas",              ""),
            (ejes[3], errmed,  "Error de colocacion", "u")):
        colores = ["#73E68C" if g == "con_guia" else "#5CCCFF" for g in guia]
        ax.bar(n, valores, color=colores)
        ax.set_title(titulo + ((" (" + unidad + ")") if unidad else ""))
        ax.set_xlabel("ensayo")
        ax.set_xticks(n)
    fig2.suptitle("Ambiente 1 · azul = sin guia haptica, verde = con guia", y=1.06)
    guardar(fig2, "a1_ensayos.png")

    imprimir_1khz("a1_1khz.csv")


if __name__ == "__main__":
    comprobar_datos()
    analizar()
