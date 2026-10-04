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
        public void NivelDe_DevuelveLaPosicionDentroDeLaPila()
        {
            var e = new EstadoHanoi(3);

            Assert.AreEqual(0, e.NivelDe(2), "el disco mayor es la base");
            Assert.AreEqual(1, e.NivelDe(1));
            Assert.AreEqual(2, e.NivelDe(0), "el mas pequeno es la cima");
            Assert.AreEqual(-1, e.NivelDe(9), "disco que no existe");

            e.Mover(0, 1);

            Assert.AreEqual(0, e.NivelDe(0), "recien movido a una torre vacia, es la base");
            Assert.AreEqual(1, e.NivelDe(1), "y el que era segundo sigue en su nivel");
        }

        [Test]
        public void Eficiencia_ValeUnoEnElOptimoYBajaConCadaMovimientoDeMas()
        {
            var limpio = new EstadoHanoi(3);
            Assert.AreEqual(0f, limpio.Eficiencia, "sin movimientos todavia no hay eficiencia");

            SeguirElOptimoHastaElFinal(limpio);
            Assert.AreEqual(1f, limpio.Eficiencia, 1e-5f, "resolverlo en 2^n-1 es eficiencia 1");

            // Dos movimientos que se cancelan: el disco 0 va y vuelve. El estado
            // queda igual que al principio, pero el contador ya marca dos.
            var torpe = new EstadoHanoi(3);
            torpe.Mover(0, 1);
            torpe.Mover(1, 0);
            SeguirElOptimoHastaElFinal(torpe);

            Assert.IsTrue(torpe.Resuelto);
            Assert.AreEqual(9, torpe.Movimientos, "7 optimos mas los 2 desperdiciados");
            Assert.AreEqual(7f / 9f, torpe.Eficiencia, 1e-5f);
            Assert.Less(torpe.Eficiencia, limpio.Eficiencia);
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
