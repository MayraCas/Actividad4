# analizar_a2.py -- Ambiente 2: precision motriz (laberinto).
#
# Responde exactamente a los tres puntos que pide la actividad -- TIEMPO,
# ERRORES y TRAYECTORIA -- y ademas mide con que fuerza se choco contra los
# muros, que es informacion que solo existe porque el ambiente es haptico.
#
#   python Analysis/analizar_a2.py
import numpy as np
import matplotlib.pyplot as plt

from comun import (estilo, cargar, num, guardar, falta, tabla,
                   encabezado, imprimir_1khz, comprobar_datos)


def analizar():
    encabezado("AMBIENTE 2 - PRECISION MOTRIZ (LABERINTO)")

    traza = cargar("a2_traza.csv")
    if traza is None:
        falta("a2_traza.csv")
        return
    estilo()

    t    = num(traza, "t")
    x, y = num(traza, "x"), num(traza, "y")
    fmag = num(traza, "fmag")
    desv = num(traza, "desviacion_u")
    toca = num(traza, "tocando_muro") > 0.5
    choq = num(traza, "choques")

    dt = np.gradient(t) if len(t) > 1 else np.array([0.0])
    recorrido = float(np.sum(np.hypot(np.diff(x), np.diff(y)))) if len(x) > 1 else 0.0
    velocidad = np.hypot(np.gradient(x), np.gradient(y)) / np.maximum(dt, 1e-6)

    print()
    print("Tiempo total            %.2f s" % (t[-1] - t[0]))
    print("Errores (choques)       %d" % int(choq[-1]))
    print("Tiempo en contacto      %.2f s  (%.1f %% del recorrido)"
          % (float(np.sum(dt[toca])), 100.0 * np.sum(dt[toca]) / max(1e-6, t[-1] - t[0])))
    print("Recorrido               %.2f u" % recorrido)
    print("Desviacion del eje      RMS %.3f u   maxima %.3f u"
          % (float(np.sqrt(np.mean(desv ** 2))), float(np.max(desv))))
    print("Velocidad de la mano    media %.2f u/s   maxima %.2f u/s"
          % (float(np.mean(velocidad)), float(np.max(velocidad))))
    if np.any(toca):
        print("Fuerza de choque        media %.2f N   pico %.2f N"
              % (float(np.mean(fmag[toca])), float(np.max(fmag[toca]))))

    # La pregunta interesante: cuando se equivoca uno, iba mas rapido?
    if np.any(toca) and np.any(~toca):
        v_choque = float(np.mean(velocidad[toca]))
        v_libre  = float(np.mean(velocidad[~toca]))
        print()
        print("Velocidad media al chocar   %.2f u/s" % v_choque)
        print("Velocidad media sin chocar  %.2f u/s" % v_libre)
        print("  -> los choques ocurren a %.0f %% de la velocidad del tramo limpio"
              % (100.0 * v_choque / max(1e-6, v_libre)))

    # ---- figuras -----------------------------------------------------------
    fig, ejes = plt.subplots(1, 3, figsize=(13, 4.0))

    ax = ejes[0]
    ax.plot(x, y, lw=1.2, color="#5CCCFF", label="trayectoria")
    if np.any(toca):
        ax.scatter(x[toca], y[toca], s=14, color="#F26B72", zorder=3, label="contacto con muro")
    ax.scatter([x[0]], [y[0]], s=60, marker="s", color="#73E68C", zorder=4, label="salida")
    ax.scatter([x[-1]], [y[-1]], s=60, marker="s", color="#F26B72", zorder=4, label="meta")
    ax.set_aspect("equal", adjustable="datalim")
    ax.set_xlabel("X (u)"); ax.set_ylabel("Y (u)")
    ax.set_title("Trayectoria en el tablero")
    ax.legend(fontsize=7)

    ax = ejes[1]
    ax.plot(t, desv, lw=1.0, color="#AE8CF9")
    ax.axhline(float(np.sqrt(np.mean(desv ** 2))), ls="--", lw=1.0, color="#FFB847",
               label="RMS")
    for i in np.flatnonzero(np.diff(choq) > 0):
        ax.axvline(t[i], lw=0.7, color="#F26B72", alpha=0.55)
    ax.set_xlabel("t (s)"); ax.set_ylabel("u")
    ax.set_title("Desviacion del eje del pasillo\n(lineas rojas = choques)")
    ax.legend(fontsize=7)

    ax = ejes[2]
    ax.plot(t, fmag, lw=0.9, color="#5CCCFF")
    ax.fill_between(t, 0, np.max(fmag) * 1.05 if np.max(fmag) > 0 else 1,
                    where=toca, alpha=0.20, color="#F26B72", step="mid")
    ax.set_xlabel("t (s)"); ax.set_ylabel("N")
    ax.set_title("Fuerza de reaccion de los muros")

    guardar(fig, "a2_laberinto.png")

    # ---- mapa de calor de la permanencia -----------------------------------
    # Donde se atasco la mano: las celdas mas calientes son las que costaron.
    if len(x) > 50:
        fig2, ax = plt.subplots(figsize=(6.0, 4.2))
        h = ax.hist2d(x, y, bins=(48, 34), cmap="magma")
        fig2.colorbar(h[3], ax=ax, label="muestras (~ tiempo de permanencia)")
        ax.set_aspect("equal", adjustable="datalim")
        ax.set_xlabel("X (u)"); ax.set_ylabel("Y (u)")
        ax.set_title("Donde se detuvo la mano")
        ax.grid(False)
        guardar(fig2, "a2_permanencia.png")

    # ---- resumen de ensayos -------------------------------------------------
    resumen = cargar("a2_resumen.csv")
    if resumen is not None:
        nivel = num(resumen, "nivel")
        tiempo = num(resumen, "tiempo_s")
        choques = num(resumen, "choques")
        efic = num(resumen, "eficiencia")
        rms = num(resumen, "desv_rms_u")
        pico = num(resumen, "f_choque_pico_n")

        tabla("Ensayos registrados",
              ["#", "nivel", "t (s)", "choques", "eficiencia %", "desv RMS u", "F pico N"],
              [[i + 1, int(nivel[i]), "%.1f" % tiempo[i], int(choques[i]),
                "%.0f" % (100 * efic[i]), "%.3f" % rms[i], "%.2f" % pico[i]]
               for i in range(len(tiempo))])

        # Curva de aprendizaje: si se repite el mismo nivel, tiempo y errores
        # deberian bajar juntos.
        if len(tiempo) >= 2:
            n = np.arange(1, len(tiempo) + 1)
            fig3, ax = plt.subplots(figsize=(6.4, 3.4))
            ax.plot(n, tiempo, "o-", color="#5CCCFF", label="tiempo (s)")
            ax.set_xlabel("ensayo"); ax.set_ylabel("s", color="#5CCCFF")
            ax.set_xticks(n)
            ax2 = ax.twinx()
            ax2.plot(n, choques, "s--", color="#F26B72", label="choques")
            ax2.set_ylabel("choques", color="#F26B72")
            ax2.grid(False)
            ax.set_title("Curva de aprendizaje")
            guardar(fig3, "a2_aprendizaje.png")

    imprimir_1khz("a2_1khz.csv")


if __name__ == "__main__":
    comprobar_datos()
    analizar()
