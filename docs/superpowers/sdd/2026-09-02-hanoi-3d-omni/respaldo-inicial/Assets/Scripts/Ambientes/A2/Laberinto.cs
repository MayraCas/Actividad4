// Laberinto.cs -- Generador y solucionador del laberinto del ambiente 2.
//
// Es codigo puro, sin GameObjects: lo usa el generador de escena del editor
// para construir los muros, y lo usa el ambiente para conocer el camino optimo
// contra el que se compara la trayectoria del usuario.
//
// Representacion: una rejilla de (2*columnas+1) x (2*filas+1) casillas donde
// las de indice impar son pasillo y las de indice par son muro. Es la forma
// clasica y hace trivial fusionar muros contiguos en barras largas: un
// laberinto de 9x6 pasa de ~45 cubos sueltos a ~20 barras, que se ve mejor y
// deja mas holgura en las 16 ranuras del dispositivo.
//
// La rejilla NO es uniforme: los pasillos miden 'anchoPasillo' y los muros
// miden 'grosorMuro'. Un laberinto con muros tan gruesos como los pasillos
// desperdiciaria la mitad del espacio de trabajo del Touch.
using System.Collections.Generic;
using UnityEngine;

namespace HapticOmni
{
    /// <summary>Un muro ya fusionado, en coordenadas locales del tablero.</summary>
    public struct BarraMuro
    {
        public Vector2 centro;   // unidades de Unity, relativo al centro del tablero
        public Vector2 tamano;   // ancho x alto
    }

    public class Laberinto
    {
        public readonly int columnas, filas;
        public readonly float anchoPasillo, grosorMuro;

        /// <summary>true = solido. Indices [0..2*columnas] x [0..2*filas].</summary>
        private readonly bool[,] solido;

        public int AnchoRejilla { get { return 2 * columnas + 1; } }
        public int AltoRejilla  { get { return 2 * filas    + 1; } }

        private float Paso { get { return anchoPasillo + grosorMuro; } }
        public float AnchoTablero { get { return columnas * Paso + grosorMuro; } }
        public float AltoTablero  { get { return filas    * Paso + grosorMuro; } }

        public Laberinto(int columnas, int filas, float anchoPasillo, float grosorMuro)
        {
            this.columnas     = Mathf.Max(2, columnas);
            this.filas        = Mathf.Max(2, filas);
            this.anchoPasillo = anchoPasillo;
            this.grosorMuro   = grosorMuro;
            solido = new bool[AnchoRejilla, AltoRejilla];
        }

        // ------------------------------------------------------------ geometria --
        /// <summary>Centro y tamano de una casilla de la rejilla sobre el eje X.</summary>
        public void RangoX(int gx, out float centro, out float tamano)
        {
            if ((gx & 1) == 0) { int i = gx / 2;      tamano = grosorMuro;   centro = i * Paso + grosorMuro * 0.5f; }
            else               { int c = (gx - 1) / 2; tamano = anchoPasillo; centro = c * Paso + grosorMuro + anchoPasillo * 0.5f; }
            centro -= AnchoTablero * 0.5f;
        }

        /// <summary>Centro y tamano de una casilla de la rejilla sobre el eje Y.</summary>
        public void RangoY(int gy, out float centro, out float tamano)
        {
            if ((gy & 1) == 0) { int i = gy / 2;      tamano = grosorMuro;   centro = i * Paso + grosorMuro * 0.5f; }
            else               { int c = (gy - 1) / 2; tamano = anchoPasillo; centro = c * Paso + grosorMuro + anchoPasillo * 0.5f; }
            centro -= AltoTablero * 0.5f;
        }

        /// <summary>Centro de la celda (cx,cy) en coordenadas locales del tablero.</summary>
        public Vector2 CentroCelda(int cx, int cy)
        {
            float x, y, a, b;
            RangoX(2 * cx + 1, out x, out a);
            RangoY(2 * cy + 1, out y, out b);
            return new Vector2(x, y);
        }

        // ----------------------------------------------------------- generacion --
        /// <summary>Laberinto perfecto por backtracking recursivo: entre dos celdas
        /// cualesquiera hay exactamente un camino, sin ciclos ni zonas aisladas.</summary>
        public static Laberinto Generar(int columnas, int filas, float anchoPasillo,
                                        float grosorMuro, int semilla)
        {
            var lab = new Laberinto(columnas, filas, anchoPasillo, grosorMuro);
            for (int x = 0; x < lab.AnchoRejilla; x++)
                for (int y = 0; y < lab.AltoRejilla; y++)
                    lab.solido[x, y] = true;

            var rnd = new System.Random(semilla);
            var visitada = new bool[lab.columnas, lab.filas];
            var pila = new Stack<Vector2Int>();

            Vector2Int actual = new Vector2Int(0, 0);
            visitada[0, 0] = true;
            lab.solido[1, 1] = false;
            pila.Push(actual);

            var vecinas = new List<Vector2Int>(4);
            while (pila.Count > 0)
            {
                actual = pila.Peek();
                vecinas.Clear();
                if (actual.x > 0                && !visitada[actual.x - 1, actual.y]) vecinas.Add(new Vector2Int(-1, 0));
                if (actual.x < lab.columnas - 1 && !visitada[actual.x + 1, actual.y]) vecinas.Add(new Vector2Int( 1, 0));
                if (actual.y > 0                && !visitada[actual.x, actual.y - 1]) vecinas.Add(new Vector2Int(0, -1));
                if (actual.y < lab.filas    - 1 && !visitada[actual.x, actual.y + 1]) vecinas.Add(new Vector2Int(0,  1));

                if (vecinas.Count == 0) { pila.Pop(); continue; }

                Vector2Int d = vecinas[rnd.Next(vecinas.Count)];
                Vector2Int sig = actual + d;
                visitada[sig.x, sig.y] = true;
                lab.solido[2 * sig.x + 1, 2 * sig.y + 1] = false;             // la celda
                lab.solido[2 * actual.x + 1 + d.x, 2 * actual.y + 1 + d.y] = false;  // el muro entre ambas
                pila.Push(sig);
            }
            return lab;
        }

        public bool EsSolido(int gx, int gy)
        {
            if (gx < 0 || gy < 0 || gx >= AnchoRejilla || gy >= AltoRejilla) return true;
            return solido[gx, gy];
        }

        /// <summary>True si se puede pasar de la celda (cx,cy) en la direccion d.</summary>
        private bool HayPaso(int cx, int cy, Vector2Int d)
        {
            int mx = 2 * cx + 1 + d.x, my = 2 * cy + 1 + d.y;
            return !EsSolido(mx, my);
        }

        // ------------------------------------------------------------- solucion --
        /// <summary>Camino mas corto en celdas entre dos celdas, por anchura.</summary>
        public List<Vector2Int> Resolver(Vector2Int inicio, Vector2Int fin)
        {
            var previo = new Dictionary<Vector2Int, Vector2Int>();
            var cola = new Queue<Vector2Int>();
            cola.Enqueue(inicio);
            previo[inicio] = inicio;

            Vector2Int[] dirs = { new Vector2Int(1,0), new Vector2Int(-1,0),
                                  new Vector2Int(0,1), new Vector2Int(0,-1) };

            while (cola.Count > 0)
            {
                var c = cola.Dequeue();
                if (c == fin) break;
                foreach (var d in dirs)
                {
                    var s = c + d;
                    if (s.x < 0 || s.y < 0 || s.x >= columnas || s.y >= filas) continue;
                    if (previo.ContainsKey(s)) continue;
                    if (!HayPaso(c.x, c.y, d)) continue;
                    previo[s] = c;
                    cola.Enqueue(s);
                }
            }

            var camino = new List<Vector2Int>();
            if (!previo.ContainsKey(fin)) return camino;
            var p = fin;
            while (p != inicio) { camino.Add(p); p = previo[p]; }
            camino.Add(inicio);
            camino.Reverse();
            return camino;
        }

        // -------------------------------------------------------------- muros ---
        /// <summary>Fusiona las casillas solidas en barras rectangulares: primero
        /// las carreras horizontales de dos o mas, luego las verticales de lo que
        /// quede, y por ultimo las casillas sueltas.</summary>
        public List<BarraMuro> Barras()
        {
            int W = AnchoRejilla, H = AltoRejilla;
            var usada = new bool[W, H];
            var barras = new List<BarraMuro>();

            for (int y = 0; y < H; y++)
            {
                int x = 0;
                while (x < W)
                {
                    if (!solido[x, y] || usada[x, y]) { x++; continue; }
                    int x2 = x;
                    while (x2 + 1 < W && solido[x2 + 1, y] && !usada[x2 + 1, y]) x2++;
                    if (x2 > x)
                    {
                        for (int i = x; i <= x2; i++) usada[i, y] = true;
                        barras.Add(Barra(x, y, x2, y));
                    }
                    x = x2 + 1;
                }
            }

            for (int x = 0; x < W; x++)
            {
                int y = 0;
                while (y < H)
                {
                    if (!solido[x, y] || usada[x, y]) { y++; continue; }
                    int y2 = y;
                    while (y2 + 1 < H && solido[x, y2 + 1] && !usada[x, y2 + 1]) y2++;
                    for (int i = y; i <= y2; i++) usada[x, i] = true;
                    barras.Add(Barra(x, y, x, y2));
                    y = y2 + 1;
                }
            }

            return barras;
        }

        private BarraMuro Barra(int gx0, int gy0, int gx1, int gy1)
        {
            float cx0, tx0, cx1, tx1, cy0, ty0, cy1, ty1;
            RangoX(gx0, out cx0, out tx0);
            RangoX(gx1, out cx1, out tx1);
            RangoY(gy0, out cy0, out ty0);
            RangoY(gy1, out cy1, out ty1);

            float izq = cx0 - tx0 * 0.5f, der = cx1 + tx1 * 0.5f;
            float aba = cy0 - ty0 * 0.5f, arr = cy1 + ty1 * 0.5f;

            return new BarraMuro
            {
                centro = new Vector2((izq + der) * 0.5f, (aba + arr) * 0.5f),
                tamano = new Vector2(der - izq, arr - aba),
            };
        }
    }
}
