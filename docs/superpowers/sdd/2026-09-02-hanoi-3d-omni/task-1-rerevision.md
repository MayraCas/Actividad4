# Re-revision scoped - Tarea 1, fix round 1

## EstadoHanoi.cs: cambios respecto a lo ya aprobado
```diff
(SIN CAMBIOS - identico a lo aprobado)
```

## EstadoHanoiTests.cs: diff del fix
```diff
--- C:/Users/76905/Downloads/ReportesOMNI/Actividad4/docs/superpowers/sdd/2026-09-02-hanoi-3d-omni/tmp-revisado/EstadoHanoiTests.cs.revisado	2026-09-02 16:20:29.568455800 -0600
+++ Assets/Tests/EditMode/EstadoHanoiTests.cs	2026-09-02 16:19:10.129236400 -0600
@@ -82,6 +82,44 @@
         }
 
         [Test]
+        public void NivelDe_DevuelveLaPosicionDentroDeLaPila()
+        {
+            var e = new EstadoHanoi(3);
+
+            Assert.AreEqual(0, e.NivelDe(2), "el disco mayor es la base");
+            Assert.AreEqual(1, e.NivelDe(1));
+            Assert.AreEqual(2, e.NivelDe(0), "el mas pequeno es la cima");
+            Assert.AreEqual(-1, e.NivelDe(9), "disco que no existe");
+
+            e.Mover(0, 1);
+
+            Assert.AreEqual(0, e.NivelDe(0), "recien movido a una torre vacia, es la base");
+            Assert.AreEqual(1, e.NivelDe(1), "y el que era segundo sigue en su nivel");
+        }
+
+        [Test]
+        public void Eficiencia_ValeUnoEnElOptimoYBajaConCadaMovimientoDeMas()
+        {
+            var limpio = new EstadoHanoi(3);
+            Assert.AreEqual(0f, limpio.Eficiencia, "sin movimientos todavia no hay eficiencia");
+
+            SeguirElOptimoHastaElFinal(limpio);
+            Assert.AreEqual(1f, limpio.Eficiencia, 1e-5f, "resolverlo en 2^n-1 es eficiencia 1");
+
+            // Dos movimientos que se cancelan: el disco 0 va y vuelve. El estado
+            // queda igual que al principio, pero el contador ya marca dos.
+            var torpe = new EstadoHanoi(3);
+            torpe.Mover(0, 1);
+            torpe.Mover(1, 0);
+            SeguirElOptimoHastaElFinal(torpe);
+
+            Assert.IsTrue(torpe.Resuelto);
+            Assert.AreEqual(9, torpe.Movimientos, "7 optimos mas los 2 desperdiciados");
+            Assert.AreEqual(7f / 9f, torpe.Eficiencia, 1e-5f);
+            Assert.Less(torpe.Eficiencia, limpio.Eficiencia);
+        }
+
+        [Test]
         public void MovimientosOptimos_EsDosElevadoANMenosUno()
         {
             Assert.AreEqual(7,  new EstadoHanoi(3).MovimientosOptimos);
```
