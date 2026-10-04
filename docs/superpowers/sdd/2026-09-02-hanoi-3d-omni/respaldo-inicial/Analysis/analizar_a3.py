# analizar_a3.py -- Ambiente 3: interaccion con fuerza.
#
# Registra lo que pide la actividad -- FUERZA APLICADA y TIEMPO REQUERIDO -- y
# ademas cierra el ciclo software -> hardware -> medicion: a partir de las
# curvas medidas se ESTIMAN por minimos cuadrados los parametros k, b y Fk que
# se programaron en cada estacion, y se comparan con los del inspector de Unity.
# Si la estimacion coincide, el modelo que se programo es el que sintio la mano.
#
#   python Analysis/analizar_a3.py
import numpy as np
import matplotlib.pyplot as plt

from comun import (estilo, cargar, num, txt, guardar, falta, tabla,
                   encabezado, imprimir_1khz, COLORES, comprobar_datos)


def estimar_parametros(s_mm, v_mms, f_n):
    """Ajusta F = k*s + b*|v| + Fk por minimos cuadrados.

    Es el mismo procedimiento de identificacion de la Actividad 3: se plantea
    la matriz de diseno con las tres contribuciones y se resuelve el sistema
    sobredeterminado. El termino independiente Fk recoge la friccion seca."""
    if len(s_mm) < 20:
        return None
    A = np.column_stack([s_mm, np.abs(v_mms), np.ones_like(s_mm)])
    sol, *_ = np.linalg.lstsq(A, f_n, rcond=None)
    pred = A @ sol
    ss_res = float(np.sum((f_n - pred) ** 2))
    ss_tot = float(np.sum((f_n - np.mean(f_n)) ** 2))
    r2 = 1.0 - ss_res / ss_tot if ss_tot > 1e-12 else 0.0
    return {"k": float(sol[0]), "b": float(sol[1]), "F0": float(sol[2]), "r2": r2}


def analizar():
    encabezado("AMBIENTE 3 - INTERACCION CON FUERZA")

    traza = cargar("a3_traza.csv")
    if traza is None:
        falta("a3_traza.csv")
        return
    estilo()

    t      = num(traza, "t")
    est    = num(traza, "estacion")
    nombre = txt(traza, "nombre")
    avance = num(traza, "avance_mm")
    vel    = num(traza, "velocidad_mms")
    fuerza = num(traza, "fuerza_n")
    desliz = num(traza, "deslizando") > 0.5
    fuente = txt(traza, "fuente")

    origen = fuente[0] if len(fuente) else "?"
    print()
    print("Origen de la fuerza registrada: %s" %
          ("servo loop a 1 kHz (hardware conectado)" if origen == "servo_1khz"
           else "modelo evaluado en Unity (sin hardware)"))

    ids = sorted({int(e) for e in est if e > 0})
    if not ids:
        print("  (no hay ninguna estacion enganchada en la traza)")
        return

    # ---- curvas F(s) y F(t) -------------------------------------------------
    fig, ejes = plt.subplots(1, 2, figsize=(12, 4.2))
    for i, e in enumerate(ids):
        sel = est == e
        etiqueta = str(nombre[np.flatnonzero(sel)[0]])
        c = COLORES[(e - 1) % len(COLORES)]
        ejes[0].plot(avance[sel], fuerza[sel], lw=1.2, color=c, label="%d · %s" % (e, etiqueta))
        ejes[1].plot(t[sel], fuerza[sel], lw=1.2, color=c, label="%d · %s" % (e, etiqueta))

    ejes[0].set_xlabel("avance de la corredera (mm)")
    ejes[0].set_ylabel("fuerza aplicada (N)")
    ejes[0].set_title("Curva fuerza–desplazamiento de cada resistencia")
    ejes[0].legend(fontsize=7)

    ejes[1].set_xlabel("t (s)")
    ejes[1].set_ylabel("fuerza aplicada (N)")
    ejes[1].set_title("Fuerza en el tiempo")
    ejes[1].legend(fontsize=7)
    guardar(fig, "a3_resistencias.png")

    # ---- identificacion de parametros --------------------------------------
    filas = []
    for e in ids:
        sel = (est == e) & (fuerza > 0)
        etiqueta = str(nombre[np.flatnonzero(est == e)[0]])
        r = estimar_parametros(avance[sel], vel[sel], fuerza[sel])
        if r is None:
            filas.append([e, etiqueta, "-", "-", "-", "-"])
            continue
        filas.append([e, etiqueta,
                      "%.4f" % r["k"], "%.5f" % r["b"], "%.2f" % r["F0"],
                      "%.2f" % r["r2"]])
    tabla("Parametros identificados a partir de la fuerza medida\n"
          "(comparar con los del inspector: k N/mm, b N/(mm/s), F0 = friccion seca N)",
          ["#", "estacion", "k est.", "b est.", "F0 est.", "R²"], filas)

    # ---- resumen de estaciones ----------------------------------------------
    resumen = cargar("a3_resumen.csv")
    if resumen is None:
        falta("a3_resumen.csv")
        imprimir_1khz("a3_1khz.csv")
        return

    idx    = num(resumen, "estacion")
    nom    = txt(resumen, "nombre")
    k_prog = num(resumen, "k_nmm")
    b_prog = num(resumen, "b_nmms")
    fs_pr  = num(resumen, "fs_n")
    fk_pr  = num(resumen, "fk_n")
    tiempo = num(resumen, "tiempo_s")
    fmed   = num(resumen, "f_media_n")
    fpico  = num(resumen, "f_pico_n")
    farr   = num(resumen, "f_arranque_n")
    trab   = num(resumen, "trabajo_nmm")

    # Si hay varias sesiones acumuladas se queda con la ultima pasada completa.
    n_est = int(np.max(idx)) if len(idx) else 0
    if n_est > 0 and len(idx) > n_est:
        idx, nom = idx[-n_est:], nom[-n_est:]
        k_prog, b_prog = k_prog[-n_est:], b_prog[-n_est:]
        fs_pr, fk_pr = fs_pr[-n_est:], fk_pr[-n_est:]
        tiempo, fmed, fpico = tiempo[-n_est:], fmed[-n_est:], fpico[-n_est:]
        farr, trab = farr[-n_est:], trab[-n_est:]

    tabla("Resultados por estacion (programado -> medido)",
          ["#", "estacion", "k", "b", "Fs", "Fk",
           "t (s)", "F media", "F pico", "F arranque", "trabajo N·mm"],
          [[int(idx[i]), nom[i], "%.3f" % k_prog[i], "%.4f" % b_prog[i],
            "%.2f" % fs_pr[i], "%.2f" % fk_pr[i],
            "%.1f" % tiempo[i], "%.2f" % fmed[i], "%.2f" % fpico[i],
            "%.2f" % farr[i], "%.0f" % trab[i]] for i in range(len(idx))])

    # La comprobacion clave del stick-slip: la fuerza que hubo que aplicar para
    # arrancar deberia parecerse a la Fs programada, y ser mayor que la media.
    print()
    print("Friccion estatica: programada frente a la que hizo falta de verdad")
    for i in range(len(idx)):
        if fs_pr[i] <= 0.01:
            continue
        print("  %-16s Fs programada %.2f N   F de arranque medida %.2f N   "
              "F media en marcha %.2f N"
              % (nom[i], fs_pr[i], farr[i], fmed[i]))

    n = np.arange(1, len(idx) + 1)
    colores = [COLORES[(int(idx[i]) - 1) % len(COLORES)] for i in range(len(idx))]
    fig2, ejes = plt.subplots(1, 4, figsize=(13, 3.2))
    for ax, valores, titulo in ((ejes[0], tiempo, "Tiempo requerido (s)"),
                                (ejes[1], fmed,   "Fuerza media (N)"),
                                (ejes[2], fpico,  "Fuerza pico (N)"),
                                (ejes[3], trab,   "Trabajo (N·mm)")):
        ax.bar(n, valores, color=colores)
        ax.set_title(titulo)
        ax.set_xticks(n)
        ax.set_xticklabels([str(nom[i])[:9] for i in range(len(idx))],
                           rotation=30, ha="right", fontsize=7)
    guardar(fig2, "a3_estaciones.png")

    # ---- detalle del stick-slip a 1 kHz -------------------------------------
    mil = cargar("a3_1khz.csv")
    if mil is not None:
        tm  = num(mil, "t")
        tag = num(mil, "tag")
        rf  = num(mil, "rail_f")
        rs  = num(mil, "rail_s")
        # La estacion de friccion seca es la que mas se nota a 1 kHz.
        candidatas = sorted({int(v) for v in tag if v > 0})
        if candidatas:
            fig3, ejes = plt.subplots(len(candidatas), 1,
                                      figsize=(9, 1.9 * len(candidatas)), sharex=False)
            if len(candidatas) == 1:
                ejes = [ejes]
            for ax, e in zip(ejes, candidatas):
                sel = tag == e
                c = COLORES[(e - 1) % len(COLORES)]
                ax.plot(tm[sel] - tm[sel][0], rf[sel], lw=0.7, color=c)
                ax.set_ylabel("F (N)")
                etiqueta = nom[e - 1] if e - 1 < len(nom) else ("estacion %d" % e)
                ax.set_title("%d · %s  —  resistencia a 1 kHz" % (e, etiqueta),
                             fontsize=9, loc="left")
            ejes[-1].set_xlabel("t (s)")
            guardar(fig3, "a3_stickslip_1khz.png")

    imprimir_1khz("a3_1khz.csv")


if __name__ == "__main__":
    comprobar_datos()
    analizar()
