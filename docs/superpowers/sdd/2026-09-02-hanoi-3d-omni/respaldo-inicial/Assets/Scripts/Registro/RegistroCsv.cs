// RegistroCsv.cs -- Acumulador de filas CSV para el analisis posterior.
//
// Los tres ambientes producen dos archivos cada uno:
//
//   *_traza.csv    una fila por frame: posicion, fuerza y lo que este midiendo
//                  el ambiente. Sirve para dibujar la trayectoria y las curvas.
//   *_resumen.csv  una fila por ensayo (una pieza colocada, un recorrido del
//                  laberinto, una estacion de resistencia). Es la tabla que
//                  entra directa al reporte.
//
// Se escribe todo de golpe al terminar, no fila a fila: abrir el archivo en
// cada frame introduce pausas de I/O que se notarian en el render y, por
// rebote, en la sensacion de continuidad de la mano.
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace HapticOmni
{
    public class RegistroCsv
    {
        private readonly string cabecera;
        private readonly List<string> filas = new List<string>();
        private readonly StringBuilder sb = new StringBuilder(256);

        /// <summary>Numero de filas acumuladas.</summary>
        public int Filas { get { return filas.Count; } }

        public RegistroCsv(string cabecera)
        {
            this.cabecera = cabecera;
        }

        public void Limpiar() { filas.Clear(); }

        /// <summary>Anade una fila. Los numeros se formatean con punto decimal e
        /// invariante de cultura: si la maquina esta en espanol, la coma decimal
        /// romperia el CSV al leerlo desde Python.</summary>
        public void Fila(params object[] valores)
        {
            sb.Clear();
            for (int i = 0; i < valores.Length; i++)
            {
                if (i > 0) sb.Append(',');
                object v = valores[i];
                if (v is float f)       sb.Append(f.ToString("F5", CultureInfo.InvariantCulture));
                else if (v is double d) sb.Append(d.ToString("F5", CultureInfo.InvariantCulture));
                else if (v is Vector3 p) sb.Append(p.x.ToString("F5", CultureInfo.InvariantCulture)).Append(',')
                                           .Append(p.y.ToString("F5", CultureInfo.InvariantCulture)).Append(',')
                                           .Append(p.z.ToString("F5", CultureInfo.InvariantCulture));
                else if (v is bool b)   sb.Append(b ? '1' : '0');
                else                     sb.Append(v);
            }
            filas.Add(sb.ToString());
        }

        /// <summary>Carpeta donde viven los datos: Analysis/datos junto al proyecto.</summary>
        public static string Carpeta
        {
            get
            {
                string ruta = Path.GetFullPath(Path.Combine(Application.dataPath, "../Analysis/datos"));
                Directory.CreateDirectory(ruta);
                return ruta;
            }
        }

        /// <summary>Guarda el archivo y devuelve la ruta completa. Si el archivo ya
        /// existe se anaden las filas al final, para poder acumular varios
        /// ensayos de varias sesiones en la misma tabla.</summary>
        public string Guardar(string nombreArchivo, bool anexar = true)
        {
            string ruta = Path.Combine(Carpeta, nombreArchivo);
            bool nuevo = !File.Exists(ruta) || !anexar;
            using (var w = new StreamWriter(ruta, anexar && !nuevo, new UTF8Encoding(false)))
            {
                if (nuevo) w.WriteLine(cabecera);
                for (int i = 0; i < filas.Count; i++) w.WriteLine(filas[i]);
            }
            Debug.Log("[Registro] " + filas.Count + " filas -> " + ruta);
            return ruta;
        }
    }
}
