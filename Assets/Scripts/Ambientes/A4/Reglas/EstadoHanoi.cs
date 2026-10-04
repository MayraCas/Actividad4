// EstadoHanoi.cs -- El arbitro de las Torres de Hanoi.
//
// Es C# puro: ni UnityEngine, ni transformadas, ni fuerzas. Solo tres pilas de
// enteros y las reglas. Esa separacion es deliberada -- toda la logica de juego
// del ambiente 4 vive aqui y se puede probar en frio, mientras que DiscoHanoi y
// TorreHanoi se limitan a obedecer.
//
// Convenio: las torres son 0, 1 y 2; los discos van del 0 (el mas pequeno) al
// n-1 (el mas grande). El indice 0 de cada pila es la base y el ultimo es la
// cima. El juego empieza con todo en la torre 0 y se resuelve en la torre 2.
using System.Collections.Generic;

namespace HapticOmni
{
    /// <summary>Un movimiento de una torre a otra. `valido` en false significa
    /// "no hay movimiento", que es distinto de un movimiento ilegal.</summary>
    public struct MovimientoHanoi
    {
        public int desde;
        public int hacia;
        public bool valido;

        public MovimientoHanoi(int desde, int hacia)
        {
            this.desde = desde;
            this.hacia = hacia;
            this.valido = true;
        }

        public static MovimientoHanoi Ninguno
        {
            get { return new MovimientoHanoi { desde = -1, hacia = -1, valido = false }; }
        }
    }

    public class EstadoHanoi
    {
        public const int Torres = 3;
        public const int MinDiscos = 3;
        public const int MaxDiscos = 7;

        private readonly List<int>[] pilas = new List<int>[Torres];

        public int Discos { get; private set; }
        public int Movimientos { get; private set; }

        public EstadoHanoi(int discos)
        {
            for (int i = 0; i < Torres; i++) pilas[i] = new List<int>(MaxDiscos);
            Reiniciar(discos);
        }

        public void Reiniciar() { Reiniciar(Discos); }

        public void Reiniciar(int discos)
        {
            if (discos < MinDiscos) discos = MinDiscos;
            if (discos > MaxDiscos) discos = MaxDiscos;

            Discos = discos;
            Movimientos = 0;
            for (int i = 0; i < Torres; i++) pilas[i].Clear();
            // El mayor va primero: queda en la base y el 0 acaba en la cima.
            for (int d = Discos - 1; d >= 0; d--) pilas[0].Add(d);
        }

        private static bool TorreValida(int t) { return t >= 0 && t < Torres; }

        /// <summary>Disco de la cima, o -1 si la torre esta vacia.</summary>
        public int Cima(int torre)
        {
            if (!TorreValida(torre) || pilas[torre].Count == 0) return -1;
            return pilas[torre][pilas[torre].Count - 1];
        }

        public int Altura(int torre)
        {
            return TorreValida(torre) ? pilas[torre].Count : 0;
        }

        /// <summary>Torre en la que esta un disco, o -1 si ese disco no existe.</summary>
        public int TorreDe(int disco)
        {
            for (int t = 0; t < Torres; t++)
                if (pilas[t].Contains(disco)) return t;
            return -1;
        }

        /// <summary>Nivel (0 = base) que ocupa un disco dentro de su torre.</summary>
        public int NivelDe(int disco)
        {
            for (int t = 0; t < Torres; t++)
            {
                int i = pilas[t].IndexOf(disco);
                if (i >= 0) return i;
            }
            return -1;
        }

        /// <summary>True si esa torre puede recibir ese disco: o esta vacia, o su
        /// cima es un disco mayor.</summary>
        public bool PuedeRecibir(int torre, int disco)
        {
            if (!TorreValida(torre) || disco < 0 || disco >= Discos) return false;
            int cima = Cima(torre);
            return cima < 0 || disco < cima;
        }

        public bool EsLegal(int desde, int hacia)
        {
            if (!TorreValida(desde) || !TorreValida(hacia) || desde == hacia) return false;
            int d = Cima(desde);
            if (d < 0) return false;
            return PuedeRecibir(hacia, d);
        }

        /// <summary>Aplica el movimiento si es legal. Devuelve false y no toca nada
        /// si no lo es, de modo que el contador nunca cuenta intentos fallidos.</summary>
        public bool Mover(int desde, int hacia)
        {
            if (!EsLegal(desde, hacia)) return false;
            int d = Cima(desde);
            pilas[desde].RemoveAt(pilas[desde].Count - 1);
            pilas[hacia].Add(d);
            Movimientos++;
            return true;
        }

        public bool Resuelto
        {
            get { return pilas[Torres - 1].Count == Discos && Discos > 0; }
        }

        /// <summary>El minimo teorico: 2^n - 1.</summary>
        public int MovimientosOptimos { get { return (1 << Discos) - 1; } }

        /// <summary>1.0 es perfecto; por debajo, cuantos movimientos de mas.</summary>
        public float Eficiencia
        {
            get { return Movimientos > 0 ? (float)MovimientosOptimos / Movimientos : 0f; }
        }

        /// <summary>El siguiente movimiento de un camino mas corto hacia la solucion,
        /// calculado desde el estado ACTUAL y no desde una secuencia precomputada.
        /// Por eso la guia haptica sigue sirviendo aunque el sujeto se salga del
        /// camino optimo: lo reencamina desde donde este.</summary>
        public MovimientoHanoi SiguienteMovimientoOptimo()
        {
            return Optimo(Discos, Torres - 1);
        }

        /// <summary>Siguiente paso para llevar los discos 0..k-1 al poste destino.
        /// Si el disco k-1 ya esta en destino, el problema se reduce a los k-1 de
        /// arriba; si no, primero hay que despejar los k-1 al poste auxiliar.</summary>
        private MovimientoHanoi Optimo(int k, int destino)
        {
            if (k <= 0) return MovimientoHanoi.Ninguno;

            int mayor = k - 1;
            int t = TorreDe(mayor);
            if (t == destino) return Optimo(k - 1, destino);

            int aux = Torres * (Torres - 1) / 2 - t - destino;   // 3 - t - destino
            var previo = Optimo(k - 1, aux);
            if (previo.valido) return previo;
            return new MovimientoHanoi(t, destino);
        }
    }
}
