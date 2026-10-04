# Paquete de revision - Tarea 1

## Archivos nuevos y modificados (vs snapshot previo)
```
Only in Assets/Scripts/Ambientes: A4
--- Assets/Tests (arbol nuevo completo) ---
Assets/Tests/EditMode/EstadoHanoiTests.cs
Assets/Tests/EditMode/HapticOmni.Tests.EditMode.asmdef
```

## Diff completo
```diff
Only in Assets/Scripts/Ambientes: A4
```

## Contenido de los archivos nuevos

### Assets/Scripts/Ambientes/A4/Reglas/HapticOmni.Reglas.asmdef
```
{
    "name": "HapticOmni.Reglas",
    "rootNamespace": "HapticOmni",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": true
}
```

### Assets/Scripts/Ambientes/A4/Reglas/EstadoHanoi.cs
```
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
```

### Assets/Tests/EditMode/HapticOmni.Tests.EditMode.asmdef
```
{
    "name": "HapticOmni.Tests.EditMode",
    "rootNamespace": "HapticOmni.Tests",
    "references": [
        "HapticOmni.Reglas",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "versionDefines": [],
    "noEngineReferences": false
}
```

### Assets/Tests/EditMode/EstadoHanoiTests.cs
```
// EstadoHanoiTests.cs -- Las reglas del ambiente 4, probadas en frio.
//
// EstadoHanoi no depende de Unity, asi que se puede ejercitar entero sin abrir
// una escena ni conectar el dispositivo. Todo lo que se puede verificar
// automaticamente de este ambiente esta aqui; lo haptico se prueba con la mano.
using NUnit.Framework;

namespace HapticOmni.Tests
{
    public class EstadoHanoiTests
    {
        [Test]
        public void EstadoInicial_TodosLosDiscosEnLaTorreCero_OrdenadosDeMayorAMenor()
        {
            var e = new EstadoHanoi(4);

            Assert.AreEqual(4, e.Altura(0));
            Assert.AreEqual(0, e.Altura(1));
            Assert.AreEqual(0, e.Altura(2));
            Assert.AreEqual(0, e.Cima(0), "el disco mas pequeno debe quedar arriba");
            Assert.AreEqual(0, e.Movimientos);
            Assert.IsFalse(e.Resuelto);
        }

        [Test]
        public void EsLegal_SoloPermiteApoyarUnDiscoSobreOtroMayorOSobreTorreVacia()
        {
            var e = new EstadoHanoi(3);

            Assert.IsTrue(e.EsLegal(0, 1), "torre vacia acepta cualquier disco");
            e.Mover(0, 1);                      // disco 0 -> torre 1

            Assert.IsFalse(e.EsLegal(0, 1), "el disco 1 no cabe sobre el disco 0");
            Assert.IsTrue(e.EsLegal(0, 2), "torre vacia sigue aceptando");
            Assert.IsTrue(e.EsLegal(1, 2), "el disco 0 cabe en una torre vacia");
        }

        [Test]
        public void EsLegal_RechazaTorreVaciaDeOrigenYMovimientosDegenerados()
        {
            var e = new EstadoHanoi(3);

            Assert.IsFalse(e.EsLegal(1, 2), "no hay nada que mover desde la torre 1");
            Assert.IsFalse(e.EsLegal(0, 0), "origen y destino iguales");
            Assert.IsFalse(e.EsLegal(-1, 0));
            Assert.IsFalse(e.EsLegal(0, 3));
        }

        [Test]
        public void Mover_NoAlteraElEstadoNiElContadorCuandoElMovimientoEsIlegal()
        {
            var e = new EstadoHanoi(3);
            e.Mover(0, 1);

            Assert.IsFalse(e.Mover(0, 1));
            Assert.AreEqual(1, e.Movimientos);
            Assert.AreEqual(2, e.Altura(0));
            Assert.AreEqual(1, e.Altura(1));
        }

        [Test]
        public void PuedeRecibir_CoincideConLaLegalidadDelMovimiento()
        {
            var e = new EstadoHanoi(3);
            e.Mover(0, 2);                      // disco 0 -> torre 2

            Assert.IsTrue(e.PuedeRecibir(1, 1), "torre vacia");
            Assert.IsFalse(e.PuedeRecibir(2, 1), "el disco 1 no cabe sobre el 0");
            Assert.IsFalse(e.PuedeRecibir(9, 0), "torre inexistente");
        }

        [Test]
        public void TorreDe_LocalizaCadaDisco()
        {
            var e = new EstadoHanoi(3);
            e.Mover(0, 1);

            Assert.AreEqual(1, e.TorreDe(0));
            Assert.AreEqual(0, e.TorreDe(1));
            Assert.AreEqual(0, e.TorreDe(2));
            Assert.AreEqual(-1, e.TorreDe(7), "disco que no existe");
        }

        [Test]
        public void MovimientosOptimos_EsDosElevadoANMenosUno()
        {
            Assert.AreEqual(7,  new EstadoHanoi(3).MovimientosOptimos);
            Assert.AreEqual(15, new EstadoHanoi(4).MovimientosOptimos);
            Assert.AreEqual(31, new EstadoHanoi(5).MovimientosOptimos);
            Assert.AreEqual(63, new EstadoHanoi(6).MovimientosOptimos);
            Assert.AreEqual(127, new EstadoHanoi(7).MovimientosOptimos);
        }

        [Test]
        public void Resuelto_SoloCuandoTodosLosDiscosEstanEnLaTorreDos()
        {
            var e = new EstadoHanoi(3);
            Assert.IsFalse(e.Resuelto);

            SeguirElOptimoHastaElFinal(e);

            Assert.IsTrue(e.Resuelto);
            Assert.AreEqual(3, e.Altura(2));
        }

        [Test]
        public void Reiniciar_DevuelveElEstadoInicialExacto()
        {
            var e = new EstadoHanoi(4);
            e.Mover(0, 1);
            e.Mover(0, 2);

            e.Reiniciar();

            Assert.AreEqual(4, e.Altura(0));
            Assert.AreEqual(0, e.Altura(1));
            Assert.AreEqual(0, e.Movimientos);
        }

        [Test]
        public void Reiniciar_ConOtroNumeroDeDiscosCambiaElTamanoDelJuego()
        {
            var e = new EstadoHanoi(3);
            e.Reiniciar(6);

            Assert.AreEqual(6, e.Discos);
            Assert.AreEqual(6, e.Altura(0));
            Assert.AreEqual(63, e.MovimientosOptimos);
        }

        [Test]
        public void SiguienteMovimientoOptimo_ResuelveEnElNumeroMinimoDeMovimientos()
        {
            for (int n = EstadoHanoi.MinDiscos; n <= EstadoHanoi.MaxDiscos; n++)
            {
                var e = new EstadoHanoi(n);
                SeguirElOptimoHastaElFinal(e);

                Assert.IsTrue(e.Resuelto, "no resolvio con " + n + " discos");
                Assert.AreEqual(e.MovimientosOptimos, e.Movimientos,
                                "no fue optimo con " + n + " discos");
            }
        }

        [Test]
        public void SiguienteMovimientoOptimo_ReencaminaDesdeUnEstadoFueraDelCaminoOptimo()
        {
            var e = new EstadoHanoi(4);
            // Tres movimientos legales pero deliberadamente torpes.
            e.Mover(0, 1);
            e.Mover(0, 2);
            e.Mover(1, 2);

            SeguirElOptimoHastaElFinal(e);

            Assert.IsTrue(e.Resuelto, "la guia debe reencaminar desde cualquier estado legal");
        }

        [Test]
        public void SiguienteMovimientoOptimo_DevuelveNingunoCuandoYaEstaResuelto()
        {
            var e = new EstadoHanoi(3);
            SeguirElOptimoHastaElFinal(e);

            Assert.IsFalse(e.SiguienteMovimientoOptimo().valido);
        }

        /// <summary>Aplica el movimiento optimo hasta resolver, con un tope que
        /// evita colgar la suite si el algoritmo entra en ciclo.</summary>
        private static void SeguirElOptimoHastaElFinal(EstadoHanoi e)
        {
            int tope = e.MovimientosOptimos * 4 + 16;
            while (!e.Resuelto && tope-- > 0)
            {
                var m = e.SiguienteMovimientoOptimo();
                Assert.IsTrue(m.valido, "se quedo sin movimiento optimo antes de resolver");
                Assert.IsTrue(e.Mover(m.desde, m.hacia),
                              "el movimiento optimo debe ser siempre legal");
            }
            Assert.Greater(tope, 0, "el algoritmo optimo no termino");
        }
    }
}
```
