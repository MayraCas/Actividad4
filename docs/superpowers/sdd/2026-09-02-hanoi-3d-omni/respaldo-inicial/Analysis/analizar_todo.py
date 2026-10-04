# analizar_todo.py -- Ejecuta los tres analisis de una vez.
#
#   pip install numpy matplotlib      (solo la primera vez)
#   python Analysis/analizar_todo.py
#
# Las figuras quedan en Analysis/figuras/ y las tablas se imprimen en la
# consola listas para copiarse al reporte.
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from comun import comprobar_datos, encabezado, FIGURAS
import analizar_a1
import analizar_a2
import analizar_a3


def main():
    comprobar_datos()
    analizar_a1.analizar()
    analizar_a2.analizar()
    analizar_a3.analizar()
    encabezado("LISTO")
    print("Figuras en: %s" % FIGURAS)


if __name__ == "__main__":
    main()
