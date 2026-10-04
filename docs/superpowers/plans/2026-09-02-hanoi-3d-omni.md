# Ambiente 4 — Torres de Hanói 3D con OMNI · Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Añadir a la Actividad 4 un cuarto ambiente — las Torres de Hanói en 3D con pesos hápticos por disco y dificultad configurable — y el subsistema de identidad de sujeto que hoy falta en los cuatro ambientes.

**Architecture:** El árbitro del juego (`EstadoHanoi`) es C# puro sin Unity y vive en su propio assembly para poder probarse con el Test Framework. Los discos y los postes son su encarnación física y háptica y no deciden nada. `Ambiente4Hanoi` hereda de `AmbienteBase` como los otros tres. La identidad del sujeto vive en un singleton que sobrevive a los cambios de escena y redirige toda la escritura de CSV a una carpeta por sujeto.

**Tech Stack:** Unity 6 (URP 17.3), C#, Input System 1.18, Test Framework 1.6, `JsonUtility`, Python 3 con numpy + matplotlib (sin pandas).

**Spec:** `docs/superpowers/specs/2026-09-02-hanoi-3d-omni-design.md`

## Global Constraints

- **El proyecto Unity es `C:\Users\76905\Downloads\ReportesOMNI\Actividad4`.** Nunca tocar `My project (3)` (Actividad 3, ya entregada). Confirmar con `Application.dataPath` vía el MCP de Unity antes de escribir nada.
- **No hay repositorio git.** Donde otros planes dirían "commit", este dice "Punto de control": una lista de comprobaciones concretas. El usuario versiona por su cuenta.
- **Convenio de índices:** torres `0`, `1`, `2` y discos `0` (el más pequeño) a `n−1` (el más grande) en código y en los CSV. En pantalla se muestran torres `1`, `2`, `3`. El juego empieza con todo en la torre `0` y se resuelve en la torre `2`.
- **Rango de discos:** 3 a 7. Saturado en todas partes.
- **Masas:** `masaMin = 0.06`, `masaMax = 0.28` kg por defecto; `masaMax` nunca por encima de `0.30` kg (≈2.94 N), por debajo del límite de 3 N del Touch.
- **Namespace:** todo el código C# va en `namespace HapticOmni` (los editores, en `HapticOmni.EditorTools`).
- **Idioma del código:** nombres, comentarios y textos de UI en español sin acentos en los identificadores, como el resto del proyecto. Los comentarios de cabecera de cada archivo explican el *porqué*, siguiendo el estilo existente.
- **CSV:** números siempre con `CultureInfo.InvariantCulture` (ya lo hace `RegistroCsv.Fila`). El texto libre se escapa entre comillas.
- **Los archivos `.meta`** los genera Unity al importar; no crearlos a mano.

---

### Task 1: `EstadoHanoi` — las reglas del juego, con tests

Es el corazón y lo único que se puede probar sin hardware ni escena. Va en su propio assembly porque un `.asmdef` de tests no puede referenciar `Assembly-CSharp` (la asamblea predefinida), pero sí otro `.asmdef`. Los assemblies definidos por `.asmdef` sí son referenciados automáticamente desde `Assembly-CSharp`, así que el resto del proyecto lo verá sin cambios.

**Files:**
- Create: `Assets/Scripts/Ambientes/A4/Reglas/EstadoHanoi.cs`
- Create: `Assets/Scripts/Ambientes/A4/Reglas/HapticOmni.Reglas.asmdef`
- Create: `Assets/Tests/EditMode/HapticOmni.Tests.EditMode.asmdef`
- Test: `Assets/Tests/EditMode/EstadoHanoiTests.cs`

**Interfaces:**
- Consumes: nada.
- Produces:
  - `struct MovimientoHanoi { int desde; int hacia; bool valido; }` con `MovimientoHanoi.Ninguno`
  - `class EstadoHanoi`:
    - `const int Torres = 3`, `const int MinDiscos = 3`, `const int MaxDiscos = 7`
    - `EstadoHanoi(int discos)`
    - `int Discos { get; }`, `int Movimientos { get; }`
    - `void Reiniciar()`, `void Reiniciar(int discos)`
    - `int Cima(int torre)` → índice del disco de arriba, `-1` si vacía
    - `int Altura(int torre)`
    - `int TorreDe(int disco)` → `-1` si no existe
    - `int NivelDe(int disco)` → posición dentro de su pila (0 = la base), `-1` si no existe
    - `bool EsLegal(int desde, int hacia)`
    - `bool PuedeRecibir(int torre, int disco)`
    - `bool Mover(int desde, int hacia)`
    - `bool Resuelto { get; }`
    - `int MovimientosOptimos { get; }` = `2^n − 1`
    - `float Eficiencia { get; }`
    - `MovimientoHanoi SiguienteMovimientoOptimo()`

- [ ] **Paso 1: Crear el `.asmdef` del assembly de reglas**

`Assets/Scripts/Ambientes/A4/Reglas/HapticOmni.Reglas.asmdef`:

```json
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

`noEngineReferences: true` porque `EstadoHanoi` no usa nada de `UnityEngine`. Eso obliga a no colar `Mathf` ahí dentro por descuido.

- [ ] **Paso 2: Crear el `.asmdef` de los tests**

`Assets/Tests/EditMode/HapticOmni.Tests.EditMode.asmdef`:

```json
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

- [ ] **Paso 3: Escribir los tests, que fallarán**

`Assets/Tests/EditMode/EstadoHanoiTests.cs`:

```csharp
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

- [ ] **Paso 4: Correr los tests y ver que fallan**

En Unity: `Window > General > Test Runner > EditMode > Run All`.
Esperado: FALLA con errores de compilación — `EstadoHanoi` no existe todavía.

(Alternativa por MCP: `Unity_RunCommand` con
`UnityEditor.TestTools.TestRunner.Api.TestRunnerApi` — o simplemente leer la
consola con `Unity_GetConsoleLogs` tras lanzar la corrida desde la ventana.)

- [ ] **Paso 5: Escribir `EstadoHanoi`**

`Assets/Scripts/Ambientes/A4/Reglas/EstadoHanoi.cs`:

```csharp
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

- [ ] **Paso 6: Correr los tests y ver que pasan**

`Window > General > Test Runner > EditMode > Run All`.
Esperado: 13 tests en verde.

- [ ] **Paso 7: Punto de control**

- La consola de Unity no tiene errores ni warnings nuevos.
- Los 13 tests de `EstadoHanoiTests` pasan.
- `Assets/Scripts/Ambientes/A4/Reglas/` tiene el `.cs`, el `.asmdef` y sus `.meta` generados por Unity.
- Los tres ambientes existentes siguen compilando (el nuevo assembly es auto-referenciado, no debería romper nada).

---

### Task 2: `SesionSujeto` — quién está haciendo la prueba

**Files:**
- Create: `Assets/Scripts/Registro/SesionSujeto.cs`
- Modify: `Assets/Scripts/Registro/RegistroCsv.cs` (líneas 59-84: `Carpeta` y `Guardar`)
- Modify (mover archivos): `Analysis/datos/`

**Interfaces:**
- Consumes: nada de tareas anteriores.
- Produces:
  - `RegistroCsv.CarpetaRaiz` → `string` (`Analysis/datos`)
  - `RegistroCsv.CarpetaDeSujeto()` → `string` (`Analysis/datos/<id>`)
  - `RegistroCsv.GuardarEn(string carpeta, string nombreArchivo, bool anexar)` → `string`
  - `RegistroCsv.Guardar(string nombreArchivo, bool anexar = true)` → `string` (ahora escribe en la carpeta del sujeto)
  - `RegistroCsv.EscaparTexto(string)` → `string`
  - `SesionSujeto.Actual` → `SesionSujeto` (puede ser `null`)
  - `SesionSujeto.IdActual` → `string` (`"ANON"` si no hay sesión)
  - `SesionSujeto.CondicionActual` → `string`
  - `SesionSujeto.SesionActual` → `int`
  - `SesionSujeto.EsIdValido(string)` → `bool`
  - `SesionSujeto.Registrar(string id, int edad, string mano, string experiencia, string condicion, string notas)` → `SesionSujeto`
  - `SesionSujeto.RegistrarAnonimo()` → `SesionSujeto`
  - `SesionSujeto.IdAnonimo` → `const string "ANON"`
  - `SesionSujeto.Etiqueta` → `string` (línea corta `id · sesion N · condicion` para el HUD y el menú)

- [ ] **Paso 1: Archivar los datos de desarrollo**

```bash
cd "C:/Users/76905/Downloads/ReportesOMNI/Actividad4/Analysis/datos"
mkdir -p _pruebas_previas
mv a1_*.csv a2_*.csv a3_*.csv _pruebas_previas/ 2>/dev/null
ls -la . _pruebas_previas
```

Esperado: `datos/` queda con sólo `_pruebas_previas/` dentro, y ahí los nueve CSV.

- [ ] **Paso 2: Reescribir la sección de carpetas y guardado de `RegistroCsv`**

Sustituir el bloque que va desde el comentario `/// <summary>Carpeta donde viven los datos...` hasta el final del método `Guardar` (líneas 59-84 del archivo actual) por:

```csharp
        /// <summary>Raiz de los datos: Analysis/datos junto al proyecto.</summary>
        public static string CarpetaRaiz
        {
            get
            {
                string ruta = Path.GetFullPath(Path.Combine(Application.dataPath, "../Analysis/datos"));
                Directory.CreateDirectory(ruta);
                return ruta;
            }
        }

        /// <summary>Carpeta del sujeto que esta haciendo la prueba. Una carpeta por
        /// sujeto es la fuente unica: no hay tabla maestra que mantener
        /// sincronizada, y el analisis en Python recorre datos/*/ y concatena.</summary>
        public static string CarpetaDeSujeto()
        {
            string ruta = Path.Combine(CarpetaRaiz, SesionSujeto.IdActual);
            Directory.CreateDirectory(ruta);
            return ruta;
        }

        /// <summary>Envuelve un texto libre entre comillas y duplica las que lleve
        /// dentro. Sin esto, una coma en las notas partiria la fila en dos.</summary>
        public static string EscaparTexto(string s)
        {
            if (s == null) s = "";
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }

        /// <summary>Guarda en la carpeta del sujeto actual.</summary>
        public string Guardar(string nombreArchivo, bool anexar = true)
        {
            return GuardarEn(CarpetaDeSujeto(), nombreArchivo, anexar);
        }

        /// <summary>Guarda en una carpeta concreta y devuelve la ruta completa. Si el
        /// archivo ya existe se anaden las filas al final, para poder acumular
        /// varios ensayos de varias sesiones en la misma tabla.</summary>
        public string GuardarEn(string carpeta, string nombreArchivo, bool anexar = true)
        {
            Directory.CreateDirectory(carpeta);
            string ruta = Path.Combine(carpeta, nombreArchivo);
            bool nuevo = !File.Exists(ruta) || !anexar;
            using (var w = new StreamWriter(ruta, anexar && !nuevo, new UTF8Encoding(false)))
            {
                if (nuevo) w.WriteLine(cabecera);
                for (int i = 0; i < filas.Count; i++) w.WriteLine(filas[i]);
            }
            Debug.Log("[Registro] " + filas.Count + " filas -> " + ruta);
            return ruta;
        }
```

Actualizar también el comentario de cabecera del archivo (líneas 1-13) para que
describa la estructura nueva:

```csharp
// RegistroCsv.cs -- Acumulador de filas CSV para el analisis posterior.
//
// Cada ambiente produce, por ensayo, una traza y una fila de resumen; el
// ambiente 4 anade ademas una bitacora de movimientos. Todo se escribe bajo
// Analysis/datos/<ID del sujeto>/ para que cada ensayo quede atribuido a quien
// lo hizo:
//
//   <ID>_<amb>_e<NN>_traza.csv   una fila por muestra: posicion, fuerza y lo
//                                que este midiendo el ambiente
//   <ID>_<amb>_resumen.csv       una fila por ensayo, acumulada entre sesiones
//
// Se escribe todo de golpe al terminar, no fila a fila: abrir el archivo en
// cada frame introduce pausas de I/O que se notarian en el render y, por
// rebote, en la sensacion de continuidad de la mano.
```

- [ ] **Paso 3: Escribir `SesionSujeto`**

`Assets/Scripts/Registro/SesionSujeto.cs`:

```csharp
// SesionSujeto.cs -- Quien esta haciendo la prueba.
//
// Hasta la Actividad 4 todos los ensayos caian en los mismos archivos sin decir
// quien los habia hecho, lo que impide comparar entre personas o entre sesiones
// de la misma persona. Este componente cierra ese hueco: se da de alta una vez
// en el menu, sobrevive a los cambios de escena y redirige la escritura de CSV
// a Analysis/datos/<ID>/.
//
// No se guardan datos personales identificables. El ID lo asigna quien conduce
// la prueba y la correspondencia con la persona, si hace falta, vive fuera del
// proyecto.
using System.Globalization;
using System.IO;
using UnityEngine;

namespace HapticOmni
{
    [AddComponentMenu("Haptics/Sesion del sujeto")]
    public class SesionSujeto : MonoBehaviour
    {
        public const string IdAnonimo = "ANON";
        public const string ArchivoSujetos = "sujetos.csv";
        private const string CabeceraSujetos = "id,fecha,edad,mano,experiencia,sesion,notas";

        public static SesionSujeto Actual { get; private set; }

        [Header("Identidad (anonima)")]
        public string id = IdAnonimo;
        public int edad;
        public string mano = "diestro";
        public string experiencia = "ninguna";

        [Header("Sesion")]
        public int sesion = 1;
        public string condicion = "sin_guia";
        [TextArea] public string notas = "";

        public static string IdActual
        {
            get
            {
                return Actual != null && EsIdValido(Actual.id) ? Actual.id.Trim() : IdAnonimo;
            }
        }

        public static string CondicionActual
        {
            get { return Actual != null ? Actual.condicion : "sin_guia"; }
        }

        public static int SesionActual
        {
            get { return Actual != null ? Actual.sesion : 0; }
        }

        /// <summary>Un ID sirve si no esta vacio y puede ser un nombre de carpeta:
        /// letras, digitos, guion y guion bajo, hasta 24 caracteres.</summary>
        public static bool EsIdValido(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            s = s.Trim();
            if (s.Length == 0 || s.Length > 24) return false;
            foreach (char c in s)
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-') return false;
            return true;
        }

        /// <summary>Da de alta al sujeto, calcula el numero de sesion y anexa la fila
        /// a sujetos.csv. Devuelve la sesion viva.</summary>
        public static SesionSujeto Registrar(string id, int edad, string mano,
                                             string experiencia, string condicion, string notas)
        {
            var s = Asegurar();
            s.id          = EsIdValido(id) ? id.Trim() : IdAnonimo;
            s.edad        = Mathf.Clamp(edad, 0, 120);
            s.mano        = string.IsNullOrEmpty(mano) ? "diestro" : mano;
            s.experiencia = string.IsNullOrEmpty(experiencia) ? "ninguna" : experiencia;
            s.condicion   = string.IsNullOrEmpty(condicion) ? "sin_guia" : condicion;
            s.notas       = notas ?? "";
            s.sesion      = SesionesPrevias(s.id) + 1;
            s.AnexarAlta();
            Debug.Log("[Sujeto] " + s.id + " · sesion " + s.sesion + " · " + s.condicion);
            return s;
        }

        /// <summary>Modo libre: los datos van a datos/ANON/ y el analisis los excluye
        /// por omision. Es para probar el equipo sin ensuciar el conjunto real.</summary>
        public static SesionSujeto RegistrarAnonimo()
        {
            var s = Asegurar();
            s.id = IdAnonimo;
            s.sesion = 0;
            s.condicion = "sin_guia";
            s.notas = "modo libre";
            Debug.Log("[Sujeto] modo libre (ANON): los datos no entran en el analisis.");
            return s;
        }

        private static SesionSujeto Asegurar()
        {
            if (Actual != null) return Actual;
            var go = new GameObject("SesionSujeto");
            var s = go.AddComponent<SesionSujeto>();
            DontDestroyOnLoad(go);
            Actual = s;
            return s;
        }

        void Awake()
        {
            // Si alguien deja un SesionSujeto en una escena, gana el que ya vivia.
            if (Actual != null && Actual != this) { Destroy(gameObject); return; }
            Actual = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>Cuenta cuantas veces aparece este ID en sujetos.csv. Contar el
        /// archivo en vez de llevar un contador en memoria es lo que hace que
        /// reanudar el trabajo otro dia numere la sesion correctamente.</summary>
        public static int SesionesPrevias(string id)
        {
            string ruta = Path.Combine(RegistroCsv.CarpetaRaiz, ArchivoSujetos);
            if (!File.Exists(ruta)) return 0;
            int n = 0;
            foreach (string linea in File.ReadAllLines(ruta))
            {
                int coma = linea.IndexOf(',');
                if (coma <= 0) continue;
                if (linea.Substring(0, coma).Trim() == id) n++;
            }
            return n;
        }

        private void AnexarAlta()
        {
            if (id == IdAnonimo) return;      // el modo libre no se da de alta
            var registro = new RegistroCsv(CabeceraSujetos);
            registro.Fila(id,
                          System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                          edad,
                          mano,
                          experiencia,
                          sesion,
                          RegistroCsv.EscaparTexto(notas));
            registro.GuardarEn(RegistroCsv.CarpetaRaiz, ArchivoSujetos, true);
        }

        /// <summary>Linea corta para el HUD y el menu.</summary>
        public string Etiqueta
        {
            get
            {
                return id + " · sesion " + sesion.ToString(CultureInfo.InvariantCulture) +
                       " · " + condicion;
            }
        }
    }
}
```

- [ ] **Paso 4: Comprobar que compila y que el alta escribe el archivo**

Con Unity abierto, comprobar en la consola que no hay errores de compilación.
Luego, para verificar la escritura sin montar todavía el panel, ejecutar por MCP
(`Unity_RunCommand`) o desde una ventana de Test Runner improvisada:

```csharp
HapticOmni.SesionSujeto.Registrar("S99", 30, "diestro", "poca", "sin_guia", "prueba, con coma");
```

Después:

```bash
cat "C:/Users/76905/Downloads/ReportesOMNI/Actividad4/Analysis/datos/sujetos.csv"
```

Esperado exactamente:

```
id,fecha,edad,mano,experiencia,sesion,notas
S99,<fecha>,30,diestro,poca,1,"prueba, con coma"
```

Repetir la llamada y comprobar que la segunda fila dice `sesion` = `2`.

- [ ] **Paso 5: Limpiar la prueba**

```bash
rm -f "C:/Users/76905/Downloads/ReportesOMNI/Actividad4/Analysis/datos/sujetos.csv"
rm -rf "C:/Users/76905/Downloads/ReportesOMNI/Actividad4/Analysis/datos/S99"
```

- [ ] **Paso 6: Punto de control**

- `Analysis/datos/` contiene sólo `_pruebas_previas/`.
- `RegistroCsv` compila y expone `CarpetaRaiz`, `CarpetaDeSujeto`, `GuardarEn` y `EscaparTexto`.
- El alta calcula la sesión contando el archivo y escapa las comas de las notas.
- Los tres ambientes siguen compilando (aún guardan con los nombres viejos; eso lo arregla la tarea 3).

---

### Task 3: Nombres de archivo por sujeto y ensayo en `AmbienteBase`

Aquí es donde el pipeline llega a los cuatro ambientes. El cambio en A1/A2/A3
es deliberadamente pequeño: sus cabeceras de resumen y su llamada de escritura.
La lógica de cada ambiente no se toca.

**Files:**
- Modify: `Assets/Scripts/Ambientes/AmbienteBase.cs`
- Modify: `Assets/Scripts/Ambientes/A1/Ambiente1Manipulacion.cs:75-77` (cabecera) y `:274` (llamada)
- Modify: `Assets/Scripts/Ambientes/A2/Ambiente2Laberinto.cs:76-77` (cabecera) y `:211` (llamada)
- Modify: `Assets/Scripts/Ambientes/A3/Ambiente3Fuerza.cs:60-61` (cabecera) y `:226` (llamada)

**Interfaces:**
- Consumes: `SesionSujeto.IdActual`, `SesionSujeto.SesionActual`, `SesionSujeto.CondicionActual`, `RegistroCsv.CarpetaDeSujeto()` (tarea 2).
- Produces:
  - `AmbienteBase.CabeceraSujeto` → `const string "sujeto,sesion,ensayo,condicion"`
  - `AmbienteBase.Ensayo` → `protected int`
  - `AmbienteBase.NombreArchivoEnsayo(string sufijo)` → `protected string`
  - `AmbienteBase.NombreArchivoResumen()` → `protected string`
  - `AmbienteBase.FilaResumen(params object[] valores)` → `protected void`

- [ ] **Paso 1: Añadir los miembros nuevos a `AmbienteBase`**

Añadir `using System.IO;` arriba, junto a los otros `using`.

Justo después de la línea `protected abstract string CabeceraResumen { get; }`, insertar:

```csharp
        /// <summary>Columnas que anteceden a toda fila de resumen, en los cuatro
        /// ambientes. Cada ambiente las antepone a su propia cabecera.</summary>
        public const string CabeceraSujeto = "sujeto,sesion,ensayo,condicion";

        /// <summary>Numero de ensayo de este sujeto en este ambiente. Se recalcula
        /// al arrancar cada ensayo, no una sola vez al cargar la escena: asi hacer
        /// varios ensayos seguidos sin salir de la escena no pisa archivos.</summary>
        protected int Ensayo { get; private set; } = 1;

        /// <summary>Nombre de un archivo propio de este ensayo, del estilo
        /// S01_hanoi_e03_traza.csv.</summary>
        protected string NombreArchivoEnsayo(string sufijo)
        {
            return SesionSujeto.IdActual + "_" + PrefijoArchivos +
                   "_e" + Ensayo.ToString("00") + "_" + sufijo;
        }

        /// <summary>Nombre del acumulado del sujeto para este ambiente.</summary>
        protected string NombreArchivoResumen()
        {
            return SesionSujeto.IdActual + "_" + PrefijoArchivos + "_resumen.csv";
        }

        /// <summary>Ruta que espera StopRecording: es relativa a Analysis/datos y el
        /// puente crea la subcarpeta por su cuenta.</summary>
        private string RutaRelativa1kHz()
        {
            return SesionSujeto.IdActual + "/" + NombreArchivoEnsayo("1khz.csv");
        }

        /// <summary>Cuenta las trazas ya guardadas por este sujeto en este ambiente.
        /// Se cuentan archivos de traza y no filas de resumen porque el ambiente 3
        /// escribe cinco filas por ensayo (una por estacion) y el conteo saldria mal.</summary>
        private int SiguienteEnsayo()
        {
            try
            {
                string carpeta = RegistroCsv.CarpetaDeSujeto();
                string patron = SesionSujeto.IdActual + "_" + PrefijoArchivos + "_e*_traza.csv";
                return Directory.GetFiles(carpeta, patron).Length + 1;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[" + Nombre + "] No pude contar ensayos previos: " + e.Message);
                return 1;
            }
        }

        /// <summary>Anade una fila de resumen con las columnas del sujeto delante.</summary>
        protected void FilaResumen(params object[] valores)
        {
            var completa = new object[valores.Length + 4];
            completa[0] = SesionSujeto.IdActual;
            completa[1] = SesionSujeto.SesionActual;
            completa[2] = Ensayo;
            completa[3] = SesionSujeto.CondicionActual;
            System.Array.Copy(valores, 0, completa, 4, valores.Length);
            resumen.Fila(completa);
        }
```

- [ ] **Paso 2: Recalcular el ensayo al arrancar y usar los nombres nuevos al guardar**

En `Awake`, después de `resumen = new RegistroCsv(CabeceraResumen);`, añadir:

```csharp
            Ensayo = SiguienteEnsayo();
```

En `Iniciar()`, justo después de `Estado = EstadoEnsayo.EnCurso;`, añadir:

```csharp
            Ensayo = SiguienteEnsayo();
```

En `Terminar()`, sustituir el bloque de guardado

```csharp
            traza.Guardar(PrefijoArchivos + "_traza.csv", false);
            resumen.Guardar(PrefijoArchivos + "_resumen.csv", true);
            resumen.Limpiar();

            if (dispositivo != null && dispositivo.Recording)
                dispositivo.StopRecording(PrefijoArchivos + "_1khz.csv");
```

por

```csharp
            traza.Guardar(NombreArchivoEnsayo("traza.csv"), false);
            resumen.Guardar(NombreArchivoResumen(), true);
            resumen.Limpiar();

            if (dispositivo != null && dispositivo.Recording)
                dispositivo.StopRecording(RutaRelativa1kHz());
```

- [ ] **Paso 3: Anteponer las columnas del sujeto en A1**

En `Ambiente1Manipulacion.cs`, cambiar `CabeceraResumen` (líneas 75-77) de

```csharp
                return "fecha,guia,tiempo_s,piezas,agarres,caidas,recorrido_u," +
                       "impulso_ns,fuerza_pico_n,err_medio_u,detalle_por_pieza";
```

a

```csharp
                return CabeceraSujeto +
                       ",fecha,guia,tiempo_s,piezas,agarres,caidas,recorrido_u," +
                       "impulso_ns,fuerza_pico_n,err_medio_u,detalle_por_pieza";
```

y en `EscribirResumen` (línea 274) cambiar `resumen.Fila(` por `FilaResumen(`.

- [ ] **Paso 4: Lo mismo en A2**

En `Ambiente2Laberinto.cs`, cambiar `CabeceraResumen` (líneas 76-77) a

```csharp
                return CabeceraSujeto +
                       ",fecha,nivel,semilla,tiempo_s,choques,t_contacto_s,f_choque_pico_n," +
                       "recorrido_u,optimo_u,eficiencia,desv_rms_u,desv_max_u";
```

y en la línea 211 cambiar `resumen.Fila(` por `FilaResumen(`.

- [ ] **Paso 5: Lo mismo en A3**

En `Ambiente3Fuerza.cs`, cambiar `CabeceraResumen` (líneas 60-61) a

```csharp
                return CabeceraSujeto +
                       ",fecha,estacion,nombre,k_nmm,b_nmms,fs_n,fk_n,tiempo_s," +
                       "f_media_n,f_pico_n,f_arranque_n,impulso_ns,trabajo_nmm,completada,fuente";
```

y en la línea 226 cambiar `resumen.Fila(` por `FilaResumen(`. Ojo: aquí la
llamada está dentro de un bucle que escribe una fila por estación; `FilaResumen`
funciona igual y le pone las mismas cuatro columnas a cada fila, que es lo
correcto — todas pertenecen al mismo ensayo.

- [ ] **Paso 6: Verificar de punta a punta con un ambiente existente**

Abrir `Assets/Scenes/Ambiente1_Manipulacion.unity` y darle a Play. Sin panel de
sujeto todavía, la sesión será `ANON`. Colocar las cuatro piezas para terminar
el ensayo y salir de Play.

```bash
ls -R "C:/Users/76905/Downloads/ReportesOMNI/Actividad4/Analysis/datos"
head -2 "C:/Users/76905/Downloads/ReportesOMNI/Actividad4/Analysis/datos/ANON/ANON_a1_resumen.csv"
```

Esperado: existe `datos/ANON/` con `ANON_a1_e01_traza.csv` y
`ANON_a1_resumen.csv`, y la primera línea del resumen empieza por
`sujeto,sesion,ensayo,condicion,fecha,guia,...` y la segunda por `ANON,0,1,sin_guia,`.

Repetir el ensayo (tecla `R` y volver a colocarlas) y comprobar que aparece
`ANON_a1_e02_traza.csv` y que el resumen tiene ahora dos filas de datos.

- [ ] **Paso 7: Punto de control**

- Los cuatro nombres de archivo salen con el patrón `<ID>_<amb>_e<NN>_<sufijo>`.
- El número de ensayo se incrementa sin pisar archivos.
- Las tres cabeceras de A1/A2/A3 empiezan por `sujeto,sesion,ensayo,condicion`.
- Borrar `Analysis/datos/ANON/` antes de seguir.

---

### Task 4: `PanelSujeto` — el alta en el menú

**Files:**
- Create: `Assets/Scripts/UI/PanelSujeto.cs`
- Modify: `Assets/Scripts/Ambientes/SelectorAmbientes.cs` (método `OnGUI`, y `Escenas`/`Titulos`/`Descripciones` en la tarea 9)

**Interfaces:**
- Consumes: `SesionSujeto.Registrar`, `SesionSujeto.RegistrarAnonimo`, `SesionSujeto.EsIdValido`, `SesionSujeto.Actual` (tarea 2).
- Produces:
  - `PanelSujeto.HaySujeto` → `static bool`
  - `PanelSujeto.Dibujar(Rect caja, GUIStyle titulo, GUIStyle texto, GUIStyle chico, GUIStyle boton)` → `static void`

- [ ] **Paso 1: Escribir `PanelSujeto`**

`Assets/Scripts/UI/PanelSujeto.cs`:

```csharp
// PanelSujeto.cs -- Alta del sujeto antes de entrar a cualquier ambiente.
//
// Se dibuja dentro del menu y bloquea los botones de los ambientes hasta que hay
// un ID valido. Es una decision de protocolo, no de interfaz: si se pudiera
// entrar sin registrarse, tarde o temprano habria un ensayo sin dueno en los
// datos y no habria forma de saber de quien era.
//
// Es OnGUI como el resto de la interfaz de la Actividad 4: no depende de
// prefabs ni de Canvas, asi que las escenas se pueden seguir regenerando desde
// codigo sin arrastrar assets.
using UnityEngine;

namespace HapticOmni
{
    public static class PanelSujeto
    {
        private static string id = "S01";
        private static string edad = "";
        private static string notas = "";
        private static int mano = 0;          // 0 diestro, 1 zurdo
        private static int experiencia = 0;   // 0 ninguna, 1 poca, 2 mucha
        private static int condicion = 0;     // 0 sin guia, 1 con guia

        private static readonly string[] Manos        = { "diestro", "zurdo" };
        private static readonly string[] Experiencias = { "ninguna", "poca", "mucha" };
        private static readonly string[] Condiciones  = { "sin_guia", "con_guia" };

        public static bool HaySujeto
        {
            get { return SesionSujeto.Actual != null; }
        }

        /// <summary>Dibuja el formulario, o la ficha del sujeto ya registrado.
        /// Devuelve la altura consumida a traves del Rect que recibe.</summary>
        public static void Dibujar(Rect caja, GUIStyle titulo, GUIStyle texto,
                                   GUIStyle chico, GUIStyle boton)
        {
            var r = new Rect(caja.x, caja.y, caja.width, 22);

            if (HaySujeto)
            {
                GUI.Label(r, "SUJETO", texto); r.y += 24;
                GUI.Label(r, SesionSujeto.Actual.Etiqueta, chico); r.y += 22;
                if (GUI.Button(new Rect(r.x, r.y, 160, 26), "Cambiar de sujeto", boton))
                {
                    Object.Destroy(SesionSujeto.Actual.gameObject);
                }
                return;
            }

            GUI.Label(r, "REGISTRO DEL SUJETO", titulo); r.y += 30;
            GUI.Label(r, "El ID es anonimo. No escribas nombres reales.", chico); r.y += 22;

            Campo(ref r, chico, "ID", ref id, 120);
            Campo(ref r, chico, "Edad", ref edad, 60);

            GUI.Label(new Rect(r.x, r.y, 90, 22), "Mano", chico);
            mano = GUI.SelectionGrid(new Rect(r.x + 95, r.y, 200, 22), mano, Manos, 2);
            r.y += 26;

            GUI.Label(new Rect(r.x, r.y, 90, 22), "Experiencia", chico);
            experiencia = GUI.SelectionGrid(new Rect(r.x + 95, r.y, 280, 22), experiencia,
                                            Experiencias, 3);
            r.y += 26;

            GUI.Label(new Rect(r.x, r.y, 90, 22), "Condicion", chico);
            condicion = GUI.SelectionGrid(new Rect(r.x + 95, r.y, 200, 22), condicion,
                                          Condiciones, 2);
            r.y += 26;

            Campo(ref r, chico, "Notas", ref notas, caja.width - 100);

            bool valido = SesionSujeto.EsIdValido(id);
            GUI.enabled = valido;
            if (GUI.Button(new Rect(r.x, r.y, 190, 28), "Registrar y continuar", boton))
            {
                int n;
                if (!int.TryParse(edad, out n)) n = 0;
                SesionSujeto.Registrar(id, n, Manos[mano], Experiencias[experiencia],
                                       Condiciones[condicion], notas);
            }
            GUI.enabled = true;

            if (GUI.Button(new Rect(r.x + 200, r.y, 190, 28), "Modo libre (sin registrar)", boton))
            {
                SesionSujeto.RegistrarAnonimo();
            }
            r.y += 32;

            if (!valido)
                GUI.Label(new Rect(r.x, r.y, caja.width, 20),
                          "El ID solo admite letras, digitos, guion y guion bajo (max. 24).", chico);
        }

        private static void Campo(ref Rect r, GUIStyle chico, string etiqueta,
                                  ref string valor, float ancho)
        {
            GUI.Label(new Rect(r.x, r.y, 90, 22), etiqueta, chico);
            valor = GUI.TextField(new Rect(r.x + 95, r.y, ancho, 22), valor, 64);
            r.y += 26;
        }
    }
}
```

- [ ] **Paso 2: Bloquear los botones del menú hasta que haya sujeto**

En `SelectorAmbientes.OnGUI`, sustituir el cuerpo completo del método por:

```csharp
        void OnGUI()
        {
            Estilos();
            // La caja crece: ahora carga el formulario del sujeto y un cuarto ambiente.
            int w = 660, h = 700;
            var caja = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.DrawTexture(caja, fondo);

            var r = new Rect(caja.x + 30, caja.y + 24, caja.width - 60, 34);
            GUI.Label(r, "ACTIVIDAD 4", titulo); r.y += 36;
            GUI.Label(r, "Desarrollo de ambientes virtuales simples", chico); r.y += 26;

            var dev = HapticDevice.Instance;
            string estado = dev == null ? "sin HapticDevice en la escena"
                          : dev.Connected ? "dispositivo conectado: " + dev.DeviceInfo
                                          : "modo simulacion (raton): sin hardware";
            GUI.Label(r, (dev != null && dev.Connected ? "● " : "○ ") + estado, chico); r.y += 30;

            // El alta del sujeto va primero y decide si se puede entrar a un ambiente.
            var cajaSujeto = new Rect(r.x, r.y, r.width, 220);
            PanelSujeto.Dibujar(cajaSujeto, titulo, texto, chico, boton);
            r.y += PanelSujeto.HaySujeto ? 80f : 220f;

            GUI.enabled = PanelSujeto.HaySujeto;
            for (int i = 0; i < Escenas.Length; i++)
            {
                var rb = new Rect(r.x, r.y, r.width, 32);
                if (GUI.Button(rb, Titulos[i], boton)) Cargar(i);
                r.y += 34;
                GUI.Label(new Rect(r.x + 8, r.y, r.width - 16, 34), Descripciones[i], chico);
                r.y += 38;
            }
            GUI.enabled = true;

            string pie = PanelSujeto.HaySujeto
                ? "Teclas 1/2/3/4 cambian de ambiente en cualquier momento · ESC vuelve aqui · " +
                  "R reinicia el ensayo · F activa o desactiva las fuerzas"
                : "Registra al sujeto para habilitar los ambientes.";
            GUI.Label(new Rect(r.x, caja.yMax - 34, r.width, 20), pie, chico);
        }
```

- [ ] **Paso 3: Impedir también el salto por teclado sin sujeto**

En `SelectorAmbientes.LeerAtajosDeEscena`, añadir como primera línea del cuerpo,
después de la comprobación del teclado:

```csharp
            if (!PanelSujeto.HaySujeto) return;
```

de modo que el método quede:

```csharp
        /// <summary>Atajos globales 1/2/3/4 disponibles en cualquier escena. No hacen
        /// nada mientras no haya un sujeto registrado: entrar a un ambiente por
        /// teclado no debe ser una puerta trasera al bloqueo del menu.</summary>
        public static void LeerAtajosDeEscena()
        {
            var t = Keyboard.current;
            if (t == null) return;
            if (!PanelSujeto.HaySujeto) return;
            if (t.digit1Key.wasPressedThisFrame) Cargar(0);
            if (t.digit2Key.wasPressedThisFrame) Cargar(1);
            if (t.digit3Key.wasPressedThisFrame) Cargar(2);
            if (t.digit4Key.wasPressedThisFrame) Cargar(3);
        }
```

(La entrada 3 del arreglo `Escenas` la añade la tarea 9; hasta entonces `Cargar(3)`
no hace nada porque comprueba el rango.)

- [ ] **Paso 4: Verificar el menú**

Abrir `Assets/Scenes/Menu.unity`, Play.

Esperado:
- El formulario aparece y los botones de ambiente están grises.
- Escribir un ID con un espacio o un `/` deja el botón *Registrar* deshabilitado y muestra el aviso.
- Con `S01` el botón se habilita; al pulsarlo el formulario se sustituye por la ficha `S01 · sesion 1 · sin_guia` y los botones de ambiente se habilitan.
- Salir de Play y volver a entrar: como `sujetos.csv` ya tiene una fila para `S01`, registrarlo otra vez muestra `sesion 2`.

```bash
cat "C:/Users/76905/Downloads/ReportesOMNI/Actividad4/Analysis/datos/sujetos.csv"
```

- [ ] **Paso 5: Punto de control**

- No se puede entrar a un ambiente sin registrar, ni con el ratón ni con las teclas.
- El modo libre entra como `ANON` sin escribir en `sujetos.csv`.
- La numeración de sesión sobrevive a cerrar y reabrir Play.
- Borrar `Analysis/datos/sujetos.csv` y `datos/S01/` antes de seguir.

---

### Task 5: `ConfiguracionHanoi` y `hanoi.json`

**Files:**
- Create: `Assets/Scripts/Ambientes/A4/ConfiguracionHanoi.cs`
- Create: `Assets/StreamingAssets/hanoi.json`

**Interfaces:**
- Consumes: `EstadoHanoi.MinDiscos`, `EstadoHanoi.MaxDiscos` (tarea 1).
- Produces:
  - `class NivelHanoi { string nombre; int discos; bool guiaPermitida; }`
  - `class ConfiguracionHanoi` con los campos del JSON y:
    - `static ConfiguracionHanoi Cargar()`
    - `static ConfiguracionHanoi PorDefecto()`
    - `void Sanear()`
    - `float MasaDe(int indice, int discos)`
    - `float RadioDe(int indice, int discos)`
    - `NivelHanoi Nivel(int i)`
    - `const float RadioMin = 0.13f`, `const float RadioMax = 0.38f`, `const float GrosorDisco = 0.095f`

- [ ] **Paso 1: Escribir el JSON**

`Assets/StreamingAssets/hanoi.json`:

```json
{
  "niveles": [
    {"nombre": "Facil",   "discos": 3, "guiaPermitida": true},
    {"nombre": "Medio",   "discos": 4, "guiaPermitida": true},
    {"nombre": "Dificil", "discos": 5, "guiaPermitida": false},
    {"nombre": "Experto", "discos": 6, "guiaPermitida": false}
  ],
  "masaMin": 0.06,
  "masaMax": 0.28,
  "amortiguamientoPorKg": 0.008,
  "retardoPorKg": 0.35,
  "rigidezDisco": 0.20,
  "friccionDisco": 0.25,
  "rigidezPoste": 0.18,
  "bloqueoHaptico": true,
  "rigidezRechazo": 0.22,
  "limiteTiempoS": 0
}
```

- [ ] **Paso 2: Escribir `ConfiguracionHanoi`**

`Assets/Scripts/Ambientes/A4/ConfiguracionHanoi.cs`:

```csharp
// ConfiguracionHanoi.cs -- Los parametros del ambiente 4, fuera del codigo.
//
// La dificultad de este ambiente no es una constante compilada: vive en
// StreamingAssets/hanoi.json para que el protocolo sea reproducible (el archivo
// dice exactamente con que se corrio) y ajustable sin reabrir Unity.
//
// Regla de oro: el ambiente NUNCA deja de arrancar por culpa del archivo. Si
// falta, no parsea o trae valores absurdos, se avisa en consola y se usan estos
// mismos valores compilados.
using System.IO;
using UnityEngine;

namespace HapticOmni
{
    [System.Serializable]
    public class NivelHanoi
    {
        public string nombre = "Facil";
        public int discos = 3;
        public bool guiaPermitida = true;
    }

    [System.Serializable]
    public class ConfiguracionHanoi
    {
        public const string NombreArchivo = "hanoi.json";

        /// <summary>Radio del disco mas pequeno y del mas grande, en unidades de
        /// Unity. Fijos: son geometria del escenario, no del protocolo.</summary>
        public const float RadioMin = 0.13f;
        public const float RadioMax = 0.38f;
        /// <summary>Grosor de un disco; tambien el paso vertical de la pila.</summary>
        public const float GrosorDisco = 0.095f;
        /// <summary>Tope duro por debajo de los 3 N que puede dar el Touch.</summary>
        public const float MasaTope = 0.30f;

        public NivelHanoi[] niveles;
        public float masaMin = 0.06f;
        public float masaMax = 0.28f;
        public float amortiguamientoPorKg = 0.008f;
        public float retardoPorKg = 0.35f;
        public float rigidezDisco = 0.20f;
        public float friccionDisco = 0.25f;
        public float rigidezPoste = 0.18f;
        public bool  bloqueoHaptico = true;
        public float rigidezRechazo = 0.22f;
        public float limiteTiempoS = 0f;

        public static ConfiguracionHanoi PorDefecto()
        {
            return new ConfiguracionHanoi
            {
                niveles = new[]
                {
                    new NivelHanoi { nombre = "Facil",   discos = 3, guiaPermitida = true  },
                    new NivelHanoi { nombre = "Medio",   discos = 4, guiaPermitida = true  },
                    new NivelHanoi { nombre = "Dificil", discos = 5, guiaPermitida = false },
                    new NivelHanoi { nombre = "Experto", discos = 6, guiaPermitida = false },
                }
            };
        }

        public static ConfiguracionHanoi Cargar()
        {
            string ruta = Path.Combine(Application.streamingAssetsPath, NombreArchivo);
            try
            {
                if (!File.Exists(ruta))
                {
                    Debug.LogWarning("[Hanoi] No encuentro " + ruta + ". Uso los valores por defecto.");
                    return PorDefecto();
                }
                var c = JsonUtility.FromJson<ConfiguracionHanoi>(File.ReadAllText(ruta));
                if (c == null || c.niveles == null || c.niveles.Length == 0)
                {
                    Debug.LogWarning("[Hanoi] " + NombreArchivo +
                                     " no trae niveles. Uso los valores por defecto.");
                    return PorDefecto();
                }
                c.Sanear();
                return c;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Hanoi] No pude leer " + NombreArchivo + " (" + e.Message +
                                 "). Uso los valores por defecto.");
                return PorDefecto();
            }
        }

        /// <summary>Recorta todo a rangos en los que el dispositivo y el escenario
        /// tienen sentido. Un JSON mal puesto degrada la configuracion, no rompe
        /// el ambiente.</summary>
        public void Sanear()
        {
            if (niveles == null || niveles.Length == 0) niveles = PorDefecto().niveles;
            foreach (var n in niveles)
            {
                if (n == null) continue;
                if (string.IsNullOrEmpty(n.nombre)) n.nombre = "Nivel";
                n.discos = Mathf.Clamp(n.discos, EstadoHanoi.MinDiscos, EstadoHanoi.MaxDiscos);
            }
            masaMin = Mathf.Clamp(masaMin, 0.02f, MasaTope);
            masaMax = Mathf.Clamp(masaMax, masaMin, MasaTope);
            amortiguamientoPorKg = Mathf.Clamp(amortiguamientoPorKg, 0f, 0.05f);
            retardoPorKg    = Mathf.Clamp(retardoPorKg, 0.05f, 2f);
            rigidezDisco    = Mathf.Clamp(rigidezDisco, 0.01f, 0.25f);
            friccionDisco   = Mathf.Clamp01(friccionDisco);
            rigidezPoste    = Mathf.Clamp(rigidezPoste, 0.01f, 0.25f);
            rigidezRechazo  = Mathf.Clamp(rigidezRechazo, 0.01f, 0.25f);
            limiteTiempoS   = Mathf.Max(0f, limiteTiempoS);
        }

        public NivelHanoi Nivel(int i)
        {
            if (niveles == null || niveles.Length == 0) return new NivelHanoi();
            return niveles[Mathf.Clamp(i, 0, niveles.Length - 1)];
        }

        /// <summary>Masa del disco `indice` (0 = el mas pequeno) en un juego de
        /// `discos` piezas. Reparto lineal entre masaMin y masaMax: con 3 discos los
        /// saltos son groseros y se distinguen sin pensar; con 7 son finos y
        /// discriminar el peso pasa a ser parte de la dificultad.</summary>
        public float MasaDe(int indice, int discos)
        {
            if (discos <= 1) return masaMax;
            float t = Mathf.Clamp01((float)indice / (discos - 1));
            return Mathf.Lerp(masaMin, masaMax, t);
        }

        public float RadioDe(int indice, int discos)
        {
            if (discos <= 1) return RadioMax;
            float t = Mathf.Clamp01((float)indice / (discos - 1));
            return Mathf.Lerp(RadioMin, RadioMax, t);
        }
    }
}
```

- [ ] **Paso 3: Verificar la carga y la degradación**

Ejecutar por MCP (`Unity_RunCommand`):

```csharp
var c = HapticOmni.ConfiguracionHanoi.Cargar();
UnityEngine.Debug.Log(c.niveles.Length + " niveles · masa disco 0 de 3 = " +
                      c.MasaDe(0,3) + " · masa disco 2 de 3 = " + c.MasaDe(2,3));
```

Esperado en consola: `4 niveles · masa disco 0 de 3 = 0.06 · masa disco 2 de 3 = 0.28`.

Luego renombrar temporalmente el JSON y repetir:

```bash
cd "C:/Users/76905/Downloads/ReportesOMNI/Actividad4/Assets/StreamingAssets"
mv hanoi.json hanoi.json.off
```

Esperado: un `LogWarning` con "No encuentro ... Uso los valores por defecto" y
los mismos 4 niveles. Restaurar:

```bash
mv hanoi.json.off hanoi.json
```

- [ ] **Paso 4: Punto de control**

- `Application.streamingAssetsPath` resuelve dentro del proyecto y el JSON se lee.
- Sin archivo, con archivo corrupto y con archivo sin niveles, la carga devuelve los valores por defecto y avisa.
- `MasaDe` y `RadioDe` dan los extremos correctos en los índices 0 y `n−1`.

---

### Task 6: `DiscoHanoi` y `TorreHanoi`

**Files:**
- Create: `Assets/Scripts/Ambientes/A4/DiscoHanoi.cs`
- Create: `Assets/Scripts/Ambientes/A4/TorreHanoi.cs`

**Interfaces:**
- Consumes: `ConfiguracionHanoi.GrosorDisco` (tarea 5), `HapticShape`, `TipoForma`.
- Produces:
  - `DiscoHanoi`: campos públicos `indice`, `masaKg`, `radio`, `retardoPorKg`; propiedades `Agarrado`, `Cayendo`, `Agarres`, `Caidas`, `TiempoEnMano`, `PesoN`; métodos `Configurar(int, float, float, float, float, float)`, `Agarrar(Vector3)`, `SeguirPunta(Vector3, float)`, `PosarEn(Vector3)`, `Caer()`, `DistanciaA(Vector3)`, `Reiniciar()`, `ActualizarCaida(float)` → `bool`
  - `TorreHanoi`: campos `indice`, `radioAceptacion`, `alturaBase`, `barrera`, `margenBarrera`; métodos `Contiene(Vector3)`, `DistanciaHorizontal(Vector3)`, `PosicionDisco(int nivel)`, `MostrarBarrera(bool visible, int altura, float rigidez)`

- [ ] **Paso 1: Escribir `DiscoHanoi`**

`Assets/Scripts/Ambientes/A4/DiscoHanoi.cs`:

```csharp
// DiscoHanoi.cs -- Un disco: la parte que se ve y se siente.
//
// El disco no sabe las reglas del juego; de eso se encarga EstadoHanoi. Aqui
// solo vive el ciclo fisico, que es el mismo que ya funciona en ObjetoAgarrable
// del ambiente 1:
//
//   En la torre  cinematico y quieto, con su HapticShape encendido: se puede tocar.
//   Agarrado     sigue a la punta con un resorte amortiguado cuyo retardo crece
//                con la masa. Se le apaga la forma haptica, porque si no el servo
//                loop intentaria expulsar la punta del interior del disco justo
//                cuando el usuario lo esta sujetando y la mano sentiria dos
//                fuerzas peleandose.
//   Cayendo      lo mueve la fisica de Unity. Soltar fuera de una torre tiene
//                consecuencia: el disco cae a la mesa, cuenta como caida y vuelve
//                a su torre al cabo de un momento.
using UnityEngine;

namespace HapticOmni
{
    [AddComponentMenu("Haptics/A4 - Disco de Hanoi")]
    [RequireComponent(typeof(Rigidbody))]
    public class DiscoHanoi : MonoBehaviour
    {
        [Header("Identidad")]
        [Tooltip("0 es el disco mas pequeno.")]
        public int indice = 0;

        [Header("Fisica")]
        [Tooltip("Masa en kg. El peso que siente la mano es m*g, saturado a 3 N por " +
                 "el puente.")]
        public float masaKg = 0.10f;
        [Tooltip("Radio en unidades de Unity. Manda en la regla del juego: un disco " +
                 "solo se apoya sobre otro de radio mayor.")]
        public float radio = 0.2f;
        [Tooltip("Retardo del seguimiento cuando esta agarrado, en segundos por kg.")]
        public float retardoPorKg = 0.35f;
        [Tooltip("Segundos que tarda en volver a su torre despues de caerse.")]
        public float tiempoDeRegreso = 1.0f;

        public bool  Agarrado     { get; private set; }
        public bool  Cayendo      { get; private set; }
        public int   Agarres      { get; private set; }
        public int   Caidas       { get; private set; }
        public float TiempoEnMano { get; private set; }

        /// <summary>Peso en newtons: lo que el puente aplica como fuerza constante.</summary>
        public float PesoN { get { return masaKg * 9.81f; } }

        private HapticShape forma;
        private Rigidbody cuerpo;
        private Vector3 desfaseAgarre;
        private Vector3 velSuavizado;
        private float tCaida;

        void Awake()
        {
            forma  = GetComponent<HapticShape>();
            cuerpo = GetComponent<Rigidbody>();
            cuerpo.mass = masaKg;
        }

        /// <summary>Ajusta tamano, masa y aspecto haptico. La llama el ambiente cada
        /// vez que cambia el nivel: la escena crea siempre siete discos y el nivel
        /// decide cuantos se activan y como de grandes son.</summary>
        public void Configurar(int indice, float masaKg, float radio, float retardoPorKg,
                               float rigidez, float friccion)
        {
            this.indice = indice;
            this.masaKg = masaKg;
            this.radio = radio;
            this.retardoPorKg = retardoPorKg;

            // El cilindro primitivo de Unity mide 1 de diametro y 2 de alto.
            transform.localScale = new Vector3(radio * 2f,
                                               ConfiguracionHanoi.GrosorDisco * 0.5f,
                                               radio * 2f);

            if (cuerpo == null) cuerpo = GetComponent<Rigidbody>();
            cuerpo.mass = masaKg;

            if (forma == null) forma = GetComponent<HapticShape>();
            if (forma != null)
            {
                // No hay primitiva cilindro en el puente: una caja de semilados
                // (radio, medio grosor, radio) es la aproximacion util. La punta
                // toca las esquinas un poco antes que el borde real, y a este
                // tamano la diferencia no se percibe.
                forma.tipo = TipoForma.Caja;
                forma.semiladosOverride = new Vector3(radio, ConfiguracionHanoi.GrosorDisco * 0.5f, radio);
                forma.rigidez = rigidez;
                forma.friccion = friccion;
            }
        }

        public void Reiniciar()
        {
            Agarrado = false;
            Cayendo = false;
            Agarres = 0;
            Caidas = 0;
            TiempoEnMano = 0f;
            tCaida = 0f;
            velSuavizado = Vector3.zero;
            HacerQuieto();
        }

        private void HacerQuieto()
        {
            if (cuerpo == null) cuerpo = GetComponent<Rigidbody>();
            cuerpo.isKinematic = true;
            cuerpo.linearVelocity = Vector3.zero;
            cuerpo.angularVelocity = Vector3.zero;
            if (forma != null) forma.enabled = true;
        }

        /// <summary>Distancia de la punta a la superficie del disco.</summary>
        public float DistanciaA(Vector3 punta)
        {
            return forma != null ? forma.DistanciaA(punta)
                                 : Vector3.Distance(punta, transform.position) - radio;
        }

        public void Agarrar(Vector3 puntaMundo)
        {
            if (Agarrado) return;
            Agarrado = true;
            Cayendo = false;
            Agarres++;
            desfaseAgarre = transform.position - puntaMundo;
            velSuavizado = Vector3.zero;
            cuerpo.isKinematic = true;
            if (forma != null) forma.enabled = false;   // que no pelee con la mano
        }

        public void SeguirPunta(Vector3 puntaMundo, float dt)
        {
            if (!Agarrado) return;
            TiempoEnMano += dt;
            float retardo = Mathf.Max(0.01f, masaKg * retardoPorKg);
            transform.position = Vector3.SmoothDamp(transform.position,
                                                    puntaMundo + desfaseAgarre,
                                                    ref velSuavizado, retardo);
        }

        /// <summary>Lo deja fijo en un sitio concreto: colocado en una torre, o
        /// devuelto a la suya tras un intento ilegal.</summary>
        public void PosarEn(Vector3 destino)
        {
            Agarrado = false;
            Cayendo = false;
            tCaida = 0f;
            transform.SetPositionAndRotation(destino, Quaternion.identity);
            HacerQuieto();
        }

        /// <summary>Soltado fuera de toda torre: se lo lleva la fisica.</summary>
        public void Caer()
        {
            Agarrado = false;
            Cayendo = true;
            Caidas++;
            tCaida = 0f;
            if (forma != null) forma.enabled = true;
            cuerpo.isKinematic = false;
            cuerpo.linearVelocity = Vector3.zero;
            cuerpo.angularVelocity = Vector3.zero;
        }

        /// <summary>Avanza el temporizador de la caida. Devuelve true cuando ya toca
        /// devolverlo a su torre.</summary>
        public bool ActualizarCaida(float dt)
        {
            if (!Cayendo) return false;
            tCaida += dt;
            return tCaida >= tiempoDeRegreso;
        }
    }
}
```

- [ ] **Paso 2: Escribir `TorreHanoi`**

`Assets/Scripts/Ambientes/A4/TorreHanoi.cs`:

```csharp
// TorreHanoi.cs -- Un poste, y la barrera que dice "aqui no".
//
// El poste es una caja haptica vertical: se puede tocar, y sirve de referencia
// tactil para localizar la torre sin mirar. La barrera es lo que hace que este
// ambiente sea haptico y no solo tridimensional: cuando el disco que llevas no
// cabe en esta torre, se enciende un semiespacio justo por encima de la cima y
// la mano topa con un techo invisible. El "no" se siente antes de intentar
// soltar, que es exactamente lo que un guante o un raton no pueden dar.
//
// La aceptacion no usa triggers de fisica: es una distancia horizontal al eje
// del poste, evaluada en el instante de soltar. El criterio es explicito y
// medible en vez de depender de si dos colliders se solapaban ese frame.
using UnityEngine;

namespace HapticOmni
{
    [AddComponentMenu("Haptics/A4 - Torre de Hanoi")]
    public class TorreHanoi : MonoBehaviour
    {
        [Tooltip("0, 1 o 2. El juego empieza en la 0 y se resuelve en la 2.")]
        public int indice = 0;

        [Tooltip("Radio horizontal dentro del cual se considera que el disco esta " +
                 "sobre esta torre, en unidades de Unity.")]
        public float radioAceptacion = 0.46f;

        [Tooltip("Y de la mesa: donde se apoya la cara inferior del primer disco.")]
        public float alturaBase = -1.02f;

        [Tooltip("Semiespacio que se enciende para rechazar un disco que no cabe.")]
        public HapticShape barrera;

        [Tooltip("Margen sobre la cima al que se coloca la barrera.")]
        public float margenBarrera = 0.06f;

        /// <summary>True si el punto cae dentro del cilindro de aceptacion.</summary>
        public bool Contiene(Vector3 p)
        {
            Vector3 c = transform.position;
            float dh = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(c.x, c.z));
            return dh <= radioAceptacion;
        }

        /// <summary>Distancia horizontal de un punto al eje del poste.</summary>
        public float DistanciaHorizontal(Vector3 p)
        {
            Vector3 c = transform.position;
            return Vector2.Distance(new Vector2(p.x, p.z), new Vector2(c.x, c.z));
        }

        /// <summary>Centro del disco que ocupe el nivel dado (0 = el de abajo).</summary>
        public Vector3 PosicionDisco(int nivel)
        {
            Vector3 c = transform.position;
            float y = alturaBase + ConfiguracionHanoi.GrosorDisco * (nivel + 0.5f);
            return new Vector3(c.x, y, c.z);
        }

        /// <summary>Enciende o apaga el techo de rechazo. `altura` es el numero de
        /// discos que ya hay en la torre.</summary>
        public void MostrarBarrera(bool visible, int altura, float rigidez)
        {
            if (barrera == null) return;
            if (visible)
            {
                Vector3 c = transform.position;
                float y = alturaBase + ConfiguracionHanoi.GrosorDisco * altura + margenBarrera;
                barrera.transform.position = new Vector3(c.x, y, c.z);
                barrera.rigidez = rigidez;
            }
            if (barrera.gameObject.activeSelf != visible)
                barrera.gameObject.SetActive(visible);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.4f, 0.85f, 1f, 0.5f);
            Vector3 c = new Vector3(transform.position.x, alturaBase + 0.01f, transform.position.z);
            const int n = 32;
            Vector3 prev = c + new Vector3(radioAceptacion, 0, 0);
            for (int i = 1; i <= n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                Vector3 p = c + new Vector3(Mathf.Cos(a) * radioAceptacion, 0,
                                            Mathf.Sin(a) * radioAceptacion);
                Gizmos.DrawLine(prev, p);
                prev = p;
            }
        }
    }
}
```

- [ ] **Paso 3: Punto de control**

- Ambos archivos compilan sin errores ni warnings.
- `DiscoHanoi` no contiene ninguna regla del juego (ni comparaciones de radio entre discos): esa lógica es de `EstadoHanoi`.
- `TorreHanoi.PosicionDisco(0)` deja la cara inferior del disco exactamente en `alturaBase`.

---

### Task 7: `Ambiente4Hanoi` — la lógica y las métricas

**Files:**
- Create: `Assets/Scripts/Ambientes/A4/Ambiente4Hanoi.cs`

**Interfaces:**
- Consumes: `AmbienteBase` (tarea 3), `EstadoHanoi` (tarea 1), `ConfiguracionHanoi` (tarea 5), `DiscoHanoi` y `TorreHanoi` (tarea 6), `HapticDevice`, `HapticAttractor`.
- Produces:
  - `Ambiente4Hanoi : AmbienteBase` con campos públicos `discos`, `torres`, `alturaMesa`, `alcanceAgarre`, `modoMantener`, `guia`, `guiaActiva`, `rigidezGuia`, `radioGuia`, `nivel`, `discosElegidos`
  - `void AplicarNivel(int nivel, int discos)`
  - `bool ConfigAbierta { get; set; }`
  - `ConfiguracionHanoi Config { get; }`
  - `EstadoHanoi Estado4 { get; }`
  - `int NivelActual { get; }`, `int DiscosActuales { get; }`

- [ ] **Paso 1: Escribir `Ambiente4Hanoi`**

`Assets/Scripts/Ambientes/A4/Ambiente4Hanoi.cs`:

```csharp
// Ambiente4Hanoi.cs -- "Torres de Hanoi hapticas".
//
// Ejercicio de la actividad: resolver el rompecabezas moviendo discos de uno en
// uno, sin apoyar nunca uno grande sobre uno pequeno. Lo que lo hace distinto de
// una version con raton es que cada disco pesa: el mas pequeno se siente como
// una pluma y el mas grande llega casi al maximo que el Touch puede entregar.
// El sujeto planifica sabiendo que el disco de abajo va a costarle mas.
//
// Que se siente en cada fase:
//
//   Tomar    la punta choca con el disco (F = k*penetracion, mas friccion) y el
//            boton 1 lo engancha. Solo se puede tomar la cima de una torre.
//   Mover    el puente aplica una fuerza constante hacia abajo de m*g y un
//            amortiguamiento proporcional a la masa, que es la inercia. Es una
//            fuerza que no depende de la posicion, asi que actualizarla a 60 Hz
//            no compromete la estabilidad: lo que exige el kilohercio son los
//            contactos, no la gravedad.
//   Rechazo  si el disco no cabe en la torre bajo la punta, se enciende un
//            semiespacio sobre la cima y la mano topa con un techo. Ese "no" es
//            la metrica propia del ambiente: cuantas veces se intento y con
//            cuanta fuerza se empujo contra la barrera.
//   Soltar   sobre una torre valida, el disco se posa; fuera de toda torre, cae
//            a la mesa, cuenta como caida y vuelve a su sitio.
//
// La guia haptica (tecla G) atrae hacia el poste del siguiente movimiento
// optimo, recalculado desde el estado actual. Comparar tiempo y eficiencia con
// guia y sin ella es la misma medida que hace el ambiente 1.
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HapticOmni
{
    [AddComponentMenu("Haptics/A4 - Torres de Hanoi")]
    public class Ambiente4Hanoi : AmbienteBase
    {
        [Header("Escenario")]
        [Tooltip("Los siete discos que crea la escena; el nivel decide cuantos se usan.")]
        public DiscoHanoi[] discos;
        public TorreHanoi[] torres;
        public float alturaMesa = -1.02f;

        [Header("Agarre")]
        [Tooltip("Margen extra sobre la superficie del disco para poder engancharlo.")]
        public float alcanceAgarre = 0.10f;
        [Tooltip("Con el boton 1 mantenido se sujeta; al soltarlo se suelta el disco.")]
        public bool modoMantener = true;

        [Header("Guia haptica (tecla G)")]
        public bool guiaActiva = false;
        public HapticAttractor guia;
        [Range(0.01f, 0.12f)] public float rigidezGuia = 0.03f;
        public float radioGuia = 2.5f;

        [Header("Dificultad")]
        public int nivel = 0;
        public int discosElegidos = 3;

        public ConfiguracionHanoi Config { get; private set; }
        public EstadoHanoi Estado4 { get; private set; }
        public int NivelActual { get { return nivel; } }
        public int DiscosActuales { get { return discosElegidos; } }
        /// <summary>Mientras el panel de configuracion esta abierto el ambiente no
        /// lee el boton de agarre: elegir el nivel no debe arrancar el ensayo.</summary>
        public bool ConfigAbierta { get; set; } = true;

        // ---- metricas del ensayo --------------------------------------------
        private int totalAgarres, totalCaidas, intentosIlegales;
        private float impulso, fuerzaPico, recorrido, fuerzaRechazoPico;
        private Vector3 puntaPrevia;
        private bool primeraMuestra = true;

        // ---- metricas del movimiento en curso -------------------------------
        private RegistroCsv movs;
        private float tMovInicio, recorridoMov, impulsoMov, picoMov;
        private int ilegalesMov;

        // ---- estado de la mano ----------------------------------------------
        private DiscoHanoi enMano, candidato;
        private int torreOrigen = -1;
        private int torreSobre = -1;
        private bool rechazando;

        public override string Nombre { get { return "AMBIENTE 4 · TORRES DE HANOI"; } }
        protected override string PrefijoArchivos { get { return "hanoi"; } }

        protected override string CabeceraTraza
        {
            get
            {
                return "t,x,y,z,fx,fy,fz,fmag,agarrado,disco,radio,peso_n," +
                       "torre_origen,torre_sobre,ilegal";
            }
        }

        protected override string CabeceraResumen
        {
            get
            {
                return CabeceraSujeto +
                       ",fecha,nivel,discos,completado,tiempo_s,movimientos," +
                       "movimientos_optimos,eficiencia,intentos_ilegales,caidas,agarres," +
                       "recorrido_u,impulso_ns,fuerza_pico_n,fuerza_rechazo_pico_n,guia";
            }
        }

        private const string CabeceraMovs =
            "n_mov,t_inicio,t_fin,disco,desde,hacia,intentos_ilegales," +
            "recorrido_u,impulso_ns,fuerza_pico_n";

        // ---------------------------------------------------------------------
        protected override void Awake()
        {
            Config = ConfiguracionHanoi.Cargar();
            var n = Config.Nivel(nivel);
            discosElegidos = Mathf.Clamp(n.discos, EstadoHanoi.MinDiscos, EstadoHanoi.MaxDiscos);
            Estado4 = new EstadoHanoi(discosElegidos);
            movs = new RegistroCsv(CabeceraMovs);
            base.Awake();
        }

        /// <summary>Cambia de nivel en caliente. La escena crea siempre siete discos;
        /// aqui se decide cuantos se activan y de que tamano son, de modo que no hace
        /// falta generar geometria en tiempo de ejecucion.</summary>
        public void AplicarNivel(int nivelNuevo, int discosNuevos)
        {
            if (Config == null) Config = ConfiguracionHanoi.Cargar();
            nivel = Mathf.Clamp(nivelNuevo, 0,
                                Config.niveles != null ? Config.niveles.Length - 1 : 0);
            discosElegidos = Mathf.Clamp(discosNuevos, EstadoHanoi.MinDiscos, EstadoHanoi.MaxDiscos);

            if (!Config.Nivel(nivel).guiaPermitida) guiaActiva = false;

            if (Estado4 == null) Estado4 = new EstadoHanoi(discosElegidos);
            else Estado4.Reiniciar(discosElegidos);

            if (discos != null)
            {
                for (int i = 0; i < discos.Length; i++)
                {
                    if (discos[i] == null) continue;
                    bool enJuego = i < discosElegidos;
                    discos[i].gameObject.SetActive(enJuego);
                    if (!enJuego) continue;
                    discos[i].Configurar(i,
                                         Config.MasaDe(i, discosElegidos),
                                         Config.RadioDe(i, discosElegidos),
                                         Config.retardoPorKg,
                                         Config.rigidezDisco,
                                         Config.friccionDisco);
                }
            }

            Reiniciar();
        }

        // ---------------------------------------------------------------------
        protected override void PrepararEscenario()
        {
            enMano = null;
            candidato = null;
            torreOrigen = -1;
            torreSobre = -1;
            rechazando = false;

            totalAgarres = totalCaidas = intentosIlegales = 0;
            impulso = fuerzaPico = recorrido = fuerzaRechazoPico = 0f;
            primeraMuestra = true;
            if (movs != null) movs.Limpiar();

            if (Estado4 != null) Estado4.Reiniciar();

            // Cada disco a su nivel en la torre 0: el mas grande abajo.
            if (discos != null && torres != null && torres.Length > 0 && torres[0] != null)
            {
                for (int i = 0; i < discos.Length; i++)
                {
                    if (discos[i] == null || !discos[i].gameObject.activeSelf) continue;
                    discos[i].Reiniciar();
                    int nivelEnPila = Estado4.NivelDe(i);
                    discos[i].PosarEn(torres[0].PosicionDisco(Mathf.Max(0, nivelEnPila)));
                }
            }

            if (torres != null)
                foreach (var t in torres)
                    if (t != null) t.MostrarBarrera(false, 0, Config != null ? Config.rigidezRechazo : 0.22f);

            if (dispositivo != null)
            {
                dispositivo.CarryEnabled = false;
                dispositivo.attractorsEnabled = false;
            }
            if (guia != null) guia.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------------
        protected override void Update()
        {
            base.Update();

            var t = Keyboard.current;
            if (t != null && t.gKey.wasPressedThisFrame && Config.Nivel(nivel).guiaPermitida)
                guiaActiva = !guiaActiva;

            if (dispositivo == null) return;
            Vector3 punta = dispositivo.WorldPosition;

            torreSobre = TorreBajoLaPunta(punta);
            ActualizarRechazo();

            if (!ConfigAbierta)
            {
                BuscarCandidato(punta);
                LeerBotonAgarre(punta);
            }

            if (enMano != null) enMano.SeguirPunta(punta, Time.deltaTime);

            AplicarFuerzasDeAcarreo();
            ActualizarGuia();
            VigilarCaidas();
            AcumularMetricas(punta);

            if (Estado == EstadoEnsayo.EnCurso)
            {
                if (Estado4.Resuelto) Terminar();
                else if (Config.limiteTiempoS > 0f && Tiempo >= Config.limiteTiempoS) Terminar();
            }
        }

        /// <summary>La torre cuyo eje esta mas cerca de la punta, si esta dentro de su
        /// radio de aceptacion.</summary>
        private int TorreBajoLaPunta(Vector3 punta)
        {
            if (torres == null) return -1;
            int mejor = -1;
            float mejorD = float.MaxValue;
            foreach (var t in torres)
            {
                if (t == null) continue;
                float d = t.DistanciaHorizontal(punta);
                if (d <= t.radioAceptacion && d < mejorD) { mejorD = d; mejor = t.indice; }
            }
            return mejor;
        }

        /// <summary>Enciende la barrera de la torre bajo la punta si el disco que se
        /// lleva no cabe ahi, y la apaga en todas las demas.</summary>
        private void ActualizarRechazo()
        {
            rechazando = enMano != null && torreSobre >= 0 && torreSobre != torreOrigen &&
                         !Estado4.PuedeRecibir(torreSobre, enMano.indice);

            if (torres == null) return;
            bool bloquear = rechazando && Config.bloqueoHaptico;
            foreach (var t in torres)
            {
                if (t == null) continue;
                bool suya = bloquear && t.indice == torreSobre;
                t.MostrarBarrera(suya, Estado4.Altura(t.indice), Config.rigidezRechazo);
            }
        }

        /// <summary>El disco suelto mas cercano que ademas es cima de su torre: solo
        /// la cima se puede tomar, igual que en el juego de mesa.</summary>
        private void BuscarCandidato(Vector3 punta)
        {
            candidato = null;
            if (enMano != null || discos == null) return;
            float mejor = alcanceAgarre;
            for (int i = 0; i < discos.Length; i++)
            {
                var d = discos[i];
                if (d == null || !d.gameObject.activeSelf || d.Agarrado || d.Cayendo) continue;
                int torre = Estado4.TorreDe(i);
                if (torre < 0 || Estado4.Cima(torre) != i) continue;
                float dist = d.DistanciaA(punta);
                if (dist < mejor) { mejor = dist; candidato = d; }
            }
        }

        private void LeerBotonAgarre(Vector3 punta)
        {
            bool teclado = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
            bool sujeta = dispositivo.Button1 || teclado;
            bool pulso  = dispositivo.Button1Down ||
                          (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame);

            if (modoMantener)
            {
                if (enMano == null && sujeta && candidato != null) Tomar(candidato, punta);
                else if (enMano != null && !sujeta) Soltar();
            }
            else if (pulso)
            {
                if (enMano == null) { if (candidato != null) Tomar(candidato, punta); }
                else Soltar();
            }
        }

        private void Tomar(DiscoHanoi d, Vector3 punta)
        {
            if (Estado == EstadoEnsayo.Preparado) Iniciar();
            if (Estado != EstadoEnsayo.EnCurso) return;

            torreOrigen = Estado4.TorreDe(d.indice);
            if (torreOrigen < 0) return;

            d.Agarrar(punta);
            enMano = d;
            totalAgarres++;

            tMovInicio = Tiempo;
            recorridoMov = impulsoMov = picoMov = 0f;
            ilegalesMov = 0;
        }

        private void Soltar()
        {
            if (enMano == null) return;

            if (torreSobre < 0)
            {
                // Fuera de toda torre: se lo lleva la fisica.
                enMano.Caer();
                totalCaidas++;
                enMano = null;
                dispositivo.CarryEnabled = false;
                return;
            }

            if (torreSobre == torreOrigen)
            {
                // Devuelto a su sitio: ni movimiento ni error.
                enMano.PosarEn(torres[torreOrigen].PosicionDisco(
                                   Estado4.Altura(torreOrigen) - 1));
                enMano = null;
                dispositivo.CarryEnabled = false;
                return;
            }

            if (!Estado4.PuedeRecibir(torreSobre, enMano.indice))
            {
                intentosIlegales++;
                ilegalesMov++;
                enMano.PosarEn(torres[torreOrigen].PosicionDisco(
                                   Estado4.Altura(torreOrigen) - 1));
                enMano = null;
                dispositivo.CarryEnabled = false;
                return;
            }

            // Movimiento valido. El nivel se calcula ANTES de mover: es la altura
            // que tiene la torre destino justo ahora.
            int nivelDestino = Estado4.Altura(torreSobre);
            int idDisco = enMano.indice;
            int desde = torreOrigen, hacia = torreSobre;
            Estado4.Mover(desde, hacia);
            enMano.PosarEn(torres[hacia].PosicionDisco(nivelDestino));

            movs.Fila(Estado4.Movimientos, tMovInicio, Tiempo, idDisco, desde, hacia,
                      ilegalesMov, recorridoMov, impulsoMov, picoMov);

            enMano = null;
            torreOrigen = -1;
            dispositivo.CarryEnabled = false;
        }

        /// <summary>Peso e inercia de lo que se lleva en la mano.</summary>
        private void AplicarFuerzasDeAcarreo()
        {
            if (enMano == null) { dispositivo.CarryEnabled = false; return; }
            dispositivo.CarryEnabled = true;
            dispositivo.CarryForceWorld = Vector3.down * enMano.PesoN;
            dispositivo.CarryDamping = enMano.masaKg * Config.amortiguamientoPorKg;
        }

        /// <summary>El atractor se coloca sobre el poste del siguiente movimiento
        /// optimo: el de destino si ya llevas el disco correcto, el de origen si aun
        /// no has tomado nada.</summary>
        private void ActualizarGuia()
        {
            bool usar = guiaActiva && guia != null && Config.Nivel(nivel).guiaPermitida &&
                        Estado == EstadoEnsayo.EnCurso;
            if (usar)
            {
                var m = Estado4.SiguienteMovimientoOptimo();
                int destino = -1;
                if (m.valido)
                    destino = (enMano != null && enMano.indice == Estado4.Cima(m.desde))
                            ? m.hacia : m.desde;
                if (destino >= 0 && torres != null && destino < torres.Length && torres[destino] != null)
                {
                    guia.transform.position = torres[destino].PosicionDisco(
                                                  Estado4.Altura(destino)) + Vector3.up * 0.35f;
                    guia.stiffness = rigidezGuia;
                    guia.radius = radioGuia;
                }
                else usar = false;
            }

            dispositivo.attractorsEnabled = usar;
            if (guia != null && guia.gameObject.activeSelf != usar)
                guia.gameObject.SetActive(usar);
        }

        /// <summary>Un disco caido vuelve a su torre pasado un momento, o de inmediato
        /// si se salio de la mesa.</summary>
        private void VigilarCaidas()
        {
            if (discos == null || torres == null) return;
            for (int i = 0; i < discos.Length; i++)
            {
                var d = discos[i];
                if (d == null || !d.gameObject.activeSelf || !d.Cayendo) continue;

                bool fuera = d.transform.position.y < alturaMesa - 1.5f;
                if (!d.ActualizarCaida(Time.deltaTime) && !fuera) continue;

                int torre = Estado4.TorreDe(i);
                if (torre < 0) torre = 0;
                d.PosarEn(torres[torre].PosicionDisco(Mathf.Max(0, Estado4.NivelDe(i))));
            }
        }

        private void AcumularMetricas(Vector3 punta)
        {
            if (Estado != EstadoEnsayo.EnCurso) return;

            float mag = dispositivo.ForceN.magnitude;
            impulso += mag * Time.deltaTime;
            if (mag > fuerzaPico) fuerzaPico = mag;
            if (rechazando && mag > fuerzaRechazoPico) fuerzaRechazoPico = mag;

            if (primeraMuestra) { puntaPrevia = punta; primeraMuestra = false; }
            float paso = Vector3.Distance(punta, puntaPrevia);
            recorrido += paso;
            puntaPrevia = punta;

            if (enMano != null)
            {
                recorridoMov += paso;
                impulsoMov += mag * Time.deltaTime;
                if (mag > picoMov) picoMov = mag;
            }
        }

        // ---------------------------------------------------------------------
        protected override void MuestrearTraza(float t)
        {
            Vector3 p = dispositivo.WorldPosition;
            Vector3 f = dispositivo.ForceN;
            traza.Fila(t, p.x, p.y, p.z, f.x, f.y, f.z, f.magnitude,
                       enMano != null,
                       enMano != null ? enMano.indice : -1,
                       enMano != null ? enMano.radio : 0f,
                       enMano != null ? enMano.PesoN : 0f,
                       torreOrigen,
                       torreSobre,
                       rechazando);
        }

        protected override void EscribirResumen()
        {
            FilaResumen(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Config.Nivel(nivel).nombre,
                        discosElegidos,
                        Estado4.Resuelto,
                        Tiempo,
                        Estado4.Movimientos,
                        Estado4.MovimientosOptimos,
                        Estado4.Eficiencia,
                        intentosIlegales,
                        totalCaidas,
                        totalAgarres,
                        recorrido,
                        impulso,
                        fuerzaPico,
                        fuerzaRechazoPico,
                        guiaActiva ? "con_guia" : "sin_guia");
        }

        /// <summary>La bitacora de movimientos es propia de este ambiente, asi que se
        /// guarda aqui, despues de que la base haya cerrado el ensayo y fijado el
        /// numero de ensayo con el que se nombran los archivos.</summary>
        public override void Terminar()
        {
            if (Estado != EstadoEnsayo.EnCurso) return;
            base.Terminar();
            movs.Guardar(NombreArchivoEnsayo("movs.csv"), false);
            movs.Limpiar();
        }

        // ---------------------------------------------------------------------
        public override string[] LineasHUD()
        {
            var pilas = new StringBuilder();
            for (int t = 0; t < EstadoHanoi.Torres; t++)
            {
                if (t > 0) pilas.Append("  ");
                pilas.Append(t + 1).Append(':').Append(Estado4 != null ? Estado4.Altura(t) : 0);
            }

            string enManoTxt = enMano != null
                ? "disco " + (enMano.indice + 1) + "  (" + enMano.PesoN.ToString("F2") + " N)"
                : (candidato != null ? "al alcance: disco " + (candidato.indice + 1) : "mano vacia");

            return new[]
            {
                "Nivel              " + Config.Nivel(nivel).nombre + "  ·  " +
                                        discosElegidos + " discos",
                "Torres             " + pilas,
                "En la mano         " + enManoTxt,
                "Movimientos        " + Estado4.Movimientos + " / " + Estado4.MovimientosOptimos +
                                        " optimos",
                "Eficiencia         " + Estado4.Eficiencia.ToString("F2"),
                "Intentos ilegales  " + intentosIlegales,
                "Caidas             " + totalCaidas,
                "Recorrido          " + recorrido.ToString("F2") + " u",
                "Impulso  ∫|F|dt    " + impulso.ToString("F2") + " N·s",
                "Fuerza pico        " + fuerzaPico.ToString("F2") + " N",
                "Rechazo pico       " + fuerzaRechazoPico.ToString("F2") + " N",
                "Guia haptica       " + (!Config.Nivel(nivel).guiaPermitida ? "no disponible"
                                       : guiaActiva ? "ENCENDIDA" : "apagada"),
            };
        }

        public override string[] LineasAyuda()
        {
            return new[]
            {
                modoMantener ? "Boton 1 del stylus (o ESPACIO): manten para sujetar, suelta para dejar"
                             : "Boton 1 del stylus (o ESPACIO): alterna tomar / soltar",
                "C configuracion · G guia haptica · R reiniciar · F fuerzas on/off",
                "1/2/3/4 cambian de ambiente · ESC menu",
            };
        }
    }
}
```

- [ ] **Paso 2: Punto de control**

- Compila sin errores.
- `Terminar()` comprueba el estado antes de llamar a `base.Terminar()`, para que la bitácora no se escriba dos veces si algo llama al método de más.
- Ninguna regla del juego está duplicada aquí: todas las comprobaciones de legalidad pasan por `Estado4`.
- El ambiente no lee el botón de agarre mientras `ConfigAbierta` es `true`.

---

### Task 8: `PanelConfigHanoi` — elegir la dificultad en la escena

**Files:**
- Create: `Assets/Scripts/UI/PanelConfigHanoi.cs`

**Interfaces:**
- Consumes: `Ambiente4Hanoi.Config`, `Ambiente4Hanoi.AplicarNivel`, `Ambiente4Hanoi.ConfigAbierta`, `Ambiente4Hanoi.nivel`, `Ambiente4Hanoi.discosElegidos` (tarea 7).
- Produces: `class PanelConfigHanoi : MonoBehaviour` con campo público `ambiente`.

- [ ] **Paso 1: Escribir el panel**

`Assets/Scripts/UI/PanelConfigHanoi.cs`:

```csharp
// PanelConfigHanoi.cs -- Nivel y numero de discos, sin salir de la escena.
//
// Se abre al entrar y se puede volver a abrir con C. Mientras esta abierto el
// ambiente ignora el boton de agarre, de modo que elegir la dificultad no
// arranca el cronometro por accidente.
//
// Los niveles vienen de hanoi.json; el numero de discos se puede ajustar aparte
// dentro del rango 3-7, porque en la practica se acaba queriendo una corrida
// intermedia que el archivo no contempla.
using UnityEngine;
using UnityEngine.InputSystem;

namespace HapticOmni
{
    [AddComponentMenu("Haptics/A4 - Panel de configuracion")]
    public class PanelConfigHanoi : MonoBehaviour
    {
        public Ambiente4Hanoi ambiente;

        private GUIStyle titulo, chico, boton;
        private Texture2D fondo;
        private int nivelSel;
        private int discosSel = 3;

        void Awake()
        {
            if (ambiente == null) ambiente = FindFirstObjectByType<Ambiente4Hanoi>();

            fondo = new Texture2D(1, 1);
            fondo.SetPixel(0, 0, new Color(0.05f, 0.07f, 0.12f, 0.94f));
            fondo.Apply();
            fondo.hideFlags = HideFlags.HideAndDontSave;
        }

        void Start()
        {
            if (ambiente == null) return;
            nivelSel = ambiente.nivel;
            discosSel = ambiente.Config.Nivel(nivelSel).discos;
            ambiente.ConfigAbierta = true;
        }

        void Update()
        {
            var t = Keyboard.current;
            if (ambiente != null && t != null && t.cKey.wasPressedThisFrame)
                ambiente.ConfigAbierta = !ambiente.ConfigAbierta;
        }

        private void Estilos()
        {
            if (titulo != null) return;
            titulo = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            titulo.normal.textColor = new Color(0.60f, 0.88f, 1f);
            chico = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            chico.normal.textColor = new Color(0.72f, 0.78f, 0.88f);
            boton = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold };
        }

        void OnGUI()
        {
            if (ambiente == null || !ambiente.ConfigAbierta) return;
            Estilos();

            int w = 520, h = 300;
            var caja = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.DrawTexture(caja, fondo);

            var r = new Rect(caja.x + 26, caja.y + 22, caja.width - 52, 28);
            GUI.Label(r, "TORRES DE HANOI", titulo); r.y += 32;
            GUI.Label(r, "Lleva los discos a la torre 3. Nunca apoyes uno grande sobre " +
                         "uno pequeno: la mano sentira que no entra.", chico); r.y += 38;

            var cfg = ambiente.Config;
            int n = cfg.niveles != null ? cfg.niveles.Length : 0;
            GUI.Label(r, "Nivel", chico); r.y += 20;

            for (int i = 0; i < n; i++)
            {
                var rb = new Rect(r.x + i * ((r.width + 6) / Mathf.Max(1, n)), r.y,
                                  (r.width - 6 * (n - 1)) / Mathf.Max(1, n), 28);
                bool sel = i == nivelSel;
                string etiqueta = cfg.Nivel(i).nombre + " (" + cfg.Nivel(i).discos + ")";
                var color = GUI.color;
                if (sel) GUI.color = new Color(0.6f, 0.9f, 1f);
                if (GUI.Button(rb, etiqueta, boton))
                {
                    nivelSel = i;
                    discosSel = cfg.Nivel(i).discos;
                }
                GUI.color = color;
            }
            r.y += 36;

            GUI.Label(new Rect(r.x, r.y, 150, 24), "Discos: " + discosSel, chico);
            if (GUI.Button(new Rect(r.x + 150, r.y, 34, 24), "-", boton))
                discosSel = Mathf.Max(EstadoHanoi.MinDiscos, discosSel - 1);
            if (GUI.Button(new Rect(r.x + 190, r.y, 34, 24), "+", boton))
                discosSel = Mathf.Min(EstadoHanoi.MaxDiscos, discosSel + 1);
            GUI.Label(new Rect(r.x + 236, r.y, r.width - 236, 24),
                      "minimo " + ((1 << discosSel) - 1) + " movimientos", chico);
            r.y += 34;

            GUI.Label(r, cfg.Nivel(nivelSel).guiaPermitida
                         ? "Guia haptica disponible en este nivel (tecla G)."
                         : "Sin guia haptica en este nivel.", chico);
            r.y += 26;

            if (GUI.Button(new Rect(r.x, r.y, 210, 30), "Empezar", boton))
            {
                ambiente.AplicarNivel(nivelSel, discosSel);
                ambiente.ConfigAbierta = false;
            }
            GUI.Label(new Rect(r.x + 220, r.y + 6, r.width - 220, 22),
                      "C abre y cierra este panel", chico);
        }
    }
}
```

- [ ] **Paso 2: Punto de control**

- Compila sin errores.
- El panel no puede dejar `discosSel` fuera de 3–7.
- Elegir un nivel actualiza el número de discos sugerido, pero después se puede ajustar a mano.

---

### Task 9: Generar la escena e integrarla en el menú

**Files:**
- Modify: `Assets/Scripts/Ambientes/SelectorAmbientes.cs:16-39` (`Escenas`, `Titulos`, `Descripciones`)
- Modify: `Assets/Editor/ConstruirAmbientes.cs` (comentario de cabecera, método `Todo`, y un método nuevo `ConstruirA4`)
- Modify: `Assets/Editor/CapturarAmbientes.cs:31` (lista de escenas)

**Interfaces:**
- Consumes: todo lo anterior.
- Produces: escena `Assets/Scenes/Ambiente4_Hanoi.unity`, entrada de menú `Haptics/Escenas/Ambiente 4 - Torres de Hanoi`.

- [ ] **Paso 1: Añadir la cuarta entrada al selector**

En `SelectorAmbientes.cs`, sustituir los tres arreglos (líneas 16-39) por:

```csharp
        public static readonly string[] Escenas =
        {
            "Ambiente1_Manipulacion",
            "Ambiente2_Laberinto",
            "Ambiente3_Fuerza",
            "Ambiente4_Hanoi",
        };
        public static readonly string[] Titulos =
        {
            "1 - MANIPULACION DE OBJETOS",
            "2 - PRECISION MOTRIZ",
            "3 - INTERACCION CON FUERZA",
            "4 - TORRES DE HANOI",
        };
        public static readonly string[] Descripciones =
        {
            "Tomar, mover y soltar piezas de distinto peso en su receptaculo. " +
            "Se registra el numero de agarres, las caidas y el tiempo por pieza.",

            "Llevar la esfera por el laberinto sin tocar los muros. " +
            "Se evaluan el tiempo, los errores (choques) y la trayectoria.",

            "Mover cinco correderas con resistencias distintas: libre, viscosa, " +
            "friccion seca, resorte y resorte con tope. Se registran la fuerza " +
            "aplicada y el tiempo requerido.",

            "Resolver las Torres de Hanoi con discos que pesan: el pequeno es una " +
            "pluma y el grande casi el maximo del equipo. Dificultad de 3 a 7 " +
            "discos. Se miden los movimientos frente al optimo, los intentos " +
            "ilegales y la fuerza contra la barrera.",
        };
```

- [ ] **Paso 2: Escribir `ConstruirA4`**

En `ConstruirAmbientes.cs`, insertar antes de la línea
`private static UnityEngine.SceneManagement.Scene NuevaEscena()`:

```csharp
        // =====================================================================
        //  AMBIENTE 4 - TORRES DE HANOI
        // =====================================================================
        [MenuItem("Haptics/Escenas/Ambiente 4 - Torres de Hanoi", priority = 26)]
        public static void ConstruirA4()
        {
            var escena = NuevaEscena();
            EstiloAmbientes.Iluminacion();
            EstiloAmbientes.PostProceso("VP_Ambientes");
            EstiloAmbientes.Camara(new Vector3(0f, 0.85f, -3.15f), new Vector3(24f, 0f, 0f), 54f);

            const float radioPunta = 0.05f;
            GameObject cursor;
            var dev = EstiloAmbientes.EspacioHaptico(out cursor, radioPunta, EstiloAmbientes.Cian);
            dev.attractorsEnabled = false;

            var matLinea = EstiloAmbientes.Mat("M_Marco", new Color(0.20f, 0.42f, 0.55f), 0.35f);
            EstiloAmbientes.MarcoEspacioDeTrabajo(dev.transform, dev.unitsPerMillimeter, matLinea);

            // ---- la mesa -----------------------------------------------------
            const float alturaMesa = -1.02f;
            var matMesa = EstiloAmbientes.MatTexturizado("M_Mesa", new Color(0.30f, 0.35f, 0.45f),
                                                         EstiloAmbientes.TexturaRejillaClara(),
                                                         new Vector2(9, 5), 0f, 0.15f);
            var mesa = EstiloAmbientes.Primitiva(PrimitiveType.Cube, "Mesa de trabajo", null,
                                                 new Vector3(0f, alturaMesa - 0.07f, 0f),
                                                 new Vector3(4.4f, 0.14f, 2.0f), matMesa, true);
            var formaMesa = mesa.AddComponent<HapticShape>();
            formaMesa.tipo = TipoForma.Caja;
            formaMesa.rigidez = 0.18f;
            formaMesa.friccion = 0.30f;
            formaMesa.prioritaria = true;
            formaMesa.colorContacto = new Color(0, 0, 0, 0);

            // ---- los tres postes ---------------------------------------------
            // El poste es tocable a proposito: da una referencia tactil para
            // localizar la torre sin apartar la vista del disco que se lleva.
            const float alturaPoste = 0.95f;
            float[] xTorre = { -1.25f, 0f, 1.25f };
            var raizT = new GameObject("Torres");
            var torres = new List<TorreHanoi>();

            for (int i = 0; i < 3; i++)
            {
                Color c = EstiloAmbientes.PaletaPiezas[i];
                var matPoste = EstiloAmbientes.Mat("M_Poste" + i, c * 0.6f, 0.25f, 0.45f);
                var matBase  = EstiloAmbientes.Mat("M_BasePoste" + i, c * 0.35f, 0f, 0.35f, 0.4f);

                var nodo = new GameObject("Torre " + (i + 1));
                nodo.transform.SetParent(raizT.transform, false);
                nodo.transform.position = new Vector3(xTorre[i], alturaMesa, 0f);

                EstiloAmbientes.Primitiva(PrimitiveType.Cylinder, "Base " + (i + 1), nodo.transform,
                                          new Vector3(0f, 0.008f, 0f),
                                          new Vector3(0.96f, 0.008f, 0.96f), matBase);

                var poste = EstiloAmbientes.Primitiva(PrimitiveType.Cylinder, "Poste " + (i + 1),
                                                      nodo.transform,
                                                      new Vector3(0f, alturaPoste * 0.5f, 0f),
                                                      new Vector3(0.07f, alturaPoste * 0.5f, 0.07f),
                                                      matPoste);
                var formaPoste = poste.AddComponent<HapticShape>();
                formaPoste.tipo = TipoForma.Caja;
                formaPoste.semiladosOverride = new Vector3(0.035f, alturaPoste * 0.5f, 0.035f);
                formaPoste.rigidez = 0.18f;
                formaPoste.friccion = 0.20f;
                formaPoste.colorContacto = Color.Lerp(c, Color.white, 0.5f);

                // La barrera de rechazo: apagada casi siempre, la enciende el
                // ambiente cuando el disco en mano no cabe en esta torre.
                var barreraGO = new GameObject("Barrera " + (i + 1));
                barreraGO.transform.SetParent(nodo.transform, false);
                var barrera = barreraGO.AddComponent<HapticShape>();
                barrera.tipo = TipoForma.Semiespacio;
                barrera.normalLocal = Vector3.up;
                barrera.rigidez = 0.22f;
                barrera.friccion = 0.05f;
                barrera.colorContacto = new Color(1f, 0.35f, 0.32f);
                barreraGO.SetActive(false);

                var torre = nodo.AddComponent<TorreHanoi>();
                torre.indice = i;
                torre.radioAceptacion = 0.46f;
                torre.alturaBase = alturaMesa;
                torre.barrera = barrera;
                torres.Add(torre);

                EstiloAmbientes.Numeral(i + 1, "Marca torre " + (i + 1), nodo.transform,
                                        new Vector3(0f, 0.02f, -0.62f), c, 0.042f, 0.085f);
            }

            // ---- los discos ---------------------------------------------------
            // Se crean siempre los siete del nivel maximo; el ambiente activa y
            // redimensiona los que pida el nivel. Asi cambiar de dificultad en
            // caliente no exige generar geometria en tiempo de ejecucion.
            var raizD = new GameObject("Discos");
            var discos = new List<DiscoHanoi>();

            for (int i = 0; i < EstadoHanoi.MaxDiscos; i++)
            {
                Color c = EstiloAmbientes.PaletaPiezas[i % EstiloAmbientes.PaletaPiezas.Length];
                var matD = EstiloAmbientes.Mat("M_Disco" + i, c, 0.35f, 0.45f, 0.15f);

                var go = EstiloAmbientes.Primitiva(PrimitiveType.Cylinder, "Disco " + (i + 1),
                                                   raizD.transform,
                                                   new Vector3(xTorre[0], alturaMesa + 0.05f + i * 0.1f, 0f),
                                                   Vector3.one * 0.2f, matD, true);
                go.AddComponent<HapticShape>();

                var cuerpo = go.AddComponent<Rigidbody>();
                cuerpo.linearDamping = 0.5f;
                cuerpo.angularDamping = 0.9f;
                cuerpo.isKinematic = true;

                var disco = go.AddComponent<DiscoHanoi>();
                disco.indice = i;
                discos.Add(disco);
            }

            // ---- guia haptica opcional ---------------------------------------
            var guiaGO = new GameObject("Guia haptica");
            var guia = guiaGO.AddComponent<HapticAttractor>();
            guia.stiffness = 0.03f;
            guia.radius = 2.5f;
            guiaGO.SetActive(false);

            // ---- logica, configuracion y HUD ----------------------------------
            var amb = dev.gameObject.AddComponent<Ambiente4Hanoi>();
            amb.dispositivo = dev;
            amb.discos = discos.ToArray();
            amb.torres = torres.ToArray();
            amb.alturaMesa = alturaMesa;
            amb.guia = guia;
            amb.nivel = 0;
            amb.discosElegidos = 3;

            var panel = dev.gameObject.AddComponent<PanelConfigHanoi>();
            panel.ambiente = amb;

            var hud = dev.gameObject.AddComponent<HUDAmbiente>();
            hud.dispositivo = dev;
            hud.ambiente = amb;

            Guardar(escena, "Ambiente4_Hanoi", 4);
        }
```

- [ ] **Paso 3: Añadir A4 a la construcción completa y a las capturas**

En `ConstruirAmbientes.Todo()`, después de `ConstruirA3();`, añadir:

```csharp
            ConstruirA4();
```

y cambiar el mensaje final:

```csharp
            Debug.Log("[Actividad 4] Las cinco escenas estan listas. Abre Menu y pulsa Play.");
```

Actualizar el comentario de cabecera del archivo (líneas 1-11) añadiendo la línea

```
//   Haptics / Escenas / Ambiente 4 - Torres de Hanoi
```

y cambiando "Genera las cuatro escenas" por "Genera las cinco escenas".

En `CapturarAmbientes.cs:31`, cambiar el arreglo por:

```csharp
            string[] escenas = { "Menu", "Ambiente1_Manipulacion", "Ambiente2_Laberinto",
                                 "Ambiente3_Fuerza", "Ambiente4_Hanoi" };
```

- [ ] **Paso 4: Generar la escena y jugarla**

En Unity: `Haptics > Escenas > Ambiente 4 - Torres de Hanoi`.

Esperado: la consola dice `[Actividad 4] Escena guardada: Assets/Scenes/Ambiente4_Hanoi.unity` sin errores.

Abrir la escena y darle a Play (modo simulación con ratón basta).

Verificar, en este orden:
1. El panel de configuración aparece y el cronómetro está a `00:00.00`.
2. Elegir *Facil* y pulsar *Empezar*: aparecen 3 discos apilados en la torre 1, del más grande abajo.
3. Con el ratón sobre el disco de arriba y ESPACIO mantenido, el disco sigue al cursor. El HUD dice `disco 1 (0.59 N)`.
4. Llevarlo a la torre 3 y soltar: se posa y el contador de movimientos pasa a 1.
5. Tomar el disco 2 y llevarlo sobre la torre 3 (que ya tiene el disco 1, más pequeño): el HUD marca la barrera; al soltar, el disco vuelve a la torre 1 y `Intentos ilegales` pasa a 1.
6. Soltar un disco lejos de toda torre: cae, `Caidas` pasa a 1 y al cabo de un segundo vuelve a su torre.
7. Resolver el juego: el ensayo termina solo y la consola muestra las líneas de `[Registro]`.
8. Pulsar `C`, subir a 5 discos, *Empezar*: se reconstruye la torre con 5 discos y las métricas vuelven a cero.

```bash
ls "C:/Users/76905/Downloads/ReportesOMNI/Actividad4/Analysis/datos"/*/
head -2 "C:/Users/76905/Downloads/ReportesOMNI/Actividad4/Analysis/datos"/*/*_hanoi_resumen.csv
head -3 "C:/Users/76905/Downloads/ReportesOMNI/Actividad4/Analysis/datos"/*/*_hanoi_e01_movs.csv
```

Esperado: `<ID>_hanoi_e01_traza.csv`, `<ID>_hanoi_e01_movs.csv` y
`<ID>_hanoi_resumen.csv`, con las cabeceras exactas de la sección 7 del spec.

- [ ] **Paso 5: Regenerar todo y comprobar el menú**

`Haptics > Construir TODOS los ambientes`. Abrir `Menu`, registrar `S01` y
comprobar que el cuarto botón entra al ambiente y que la tecla `4` también.

- [ ] **Paso 6: Punto de control**

- Las cinco escenas se generan sin errores y quedan registradas en Build Settings.
- Los tres ambientes anteriores siguen funcionando y escribiendo en la carpeta del sujeto.
- Las tres reglas del juego se comportan como en el spec: sólo la cima es agarrable, el disco grande no se posa sobre el pequeño, y el juego termina en la torre 3.
- Borrar los datos de esta prueba antes de seguir.

---

### Task 10: Lectura multi-sujeto en Python

**Files:**
- Modify: `Analysis/comun.py` (añadir funciones; sustituir `cargar` por `cargar_todos`; `resumen_1khz` pasa a trabajar por patrón)
- Modify: `Analysis/analizar_a1.py:10-11, 17, 90, 93, 140`
- Modify: `Analysis/analizar_a2.py:11-12, 18, 105, 135`
- Modify: `Analysis/analizar_a3.py:13-14, 37, 100, 103, 161, 185`
- Modify: `Analysis/analizar_todo.py`

**Interfaces:**
- Consumes: la estructura de carpetas de las tareas 2 y 3.
- Produces:
  - `comun.carpetas_sujeto(incluir_anon=False)` → `list[tuple[str, str]]`
  - `comun.archivos(patron, incluir_anon=False)` → `list[tuple[str, str]]` (sujeto, ruta)
  - `comun.cargar_todos(patron, incluir_anon=False)` → `dict | None`, con columna `sujeto` garantizada
  - `comun.cargar_ruta(ruta)` → `dict | None`
  - `comun.sujetos()` → `dict | None`
  - `comun.resumen_1khz(patron)` y `comun.imprimir_1khz(patron)` aceptan patrón glob

- [ ] **Paso 1: Reescribir la carga en `comun.py`**

Añadir `import fnmatch` junto a los otros `import`, y sustituir la función
`cargar` (líneas 57-69) por este bloque:

```python
# Carpetas que no son de un sujeto real.
IGNORAR = ("_pruebas_previas", "figuras")


def carpetas_sujeto(incluir_anon=False):
    """Las carpetas de Analysis/datos que corresponden a un sujeto.

    Una carpeta por sujeto es la fuente unica de los datos: no hay tabla
    maestra que mantener sincronizada. ANON es el modo libre de desarrollo y se
    excluye por omision para que las pruebas del equipo no entren al analisis."""
    if not os.path.isdir(DATOS):
        return []
    salida = []
    for nombre in sorted(os.listdir(DATOS)):
        ruta = os.path.join(DATOS, nombre)
        if not os.path.isdir(ruta):
            continue
        if nombre in IGNORAR or nombre.startswith("_"):
            continue
        if nombre == "ANON" and not incluir_anon:
            continue
        salida.append((nombre, ruta))
    return salida


def archivos(patron, incluir_anon=False):
    """Rutas que casan con un glob, en todas las carpetas de sujeto.
    Devuelve pares (sujeto, ruta) ordenados."""
    salida = []
    for sid, carpeta in carpetas_sujeto(incluir_anon):
        for nombre in sorted(os.listdir(carpeta)):
            if fnmatch.fnmatch(nombre, patron):
                salida.append((sid, os.path.join(carpeta, nombre)))
    return salida


def cargar_ruta(ruta):
    """Lee un CSV concreto como diccionario de columnas (listas de texto)."""
    if not os.path.exists(ruta):
        return None
    with open(ruta, "r", encoding="utf-8-sig", newline="") as f:
        filas = list(csv.DictReader(f))
    if not filas:
        return None
    cols = {k: [fila.get(k, "") or "" for fila in filas] for k in filas[0].keys()}
    cols["__n__"] = len(filas)
    return cols


def cargar_todos(patron, incluir_anon=False):
    """Concatena todos los archivos que casan con el glob, de todos los sujetos.

    Devuelve un diccionario de columnas con la union de las claves encontradas;
    las celdas que un archivo no traiga quedan vacias. Siempre hay columna
    'sujeto': si el CSV no la trae, se rellena con el nombre de la carpeta.
    Devuelve None si no hay nada que leer todavia."""
    filas = []
    for sid, ruta in archivos(patron, incluir_anon):
        with open(ruta, "r", encoding="utf-8-sig", newline="") as f:
            for fila in csv.DictReader(f):
                if not fila.get("sujeto"):
                    fila["sujeto"] = sid
                fila["archivo"] = os.path.basename(ruta)
                filas.append(fila)
    if not filas:
        return None

    claves = []
    for fila in filas:
        for k in fila:
            if k not in claves:
                claves.append(k)
    cols = {k: [fila.get(k, "") or "" for fila in filas] for k in claves}
    cols["__n__"] = len(filas)
    return cols


def sujetos():
    """La tabla de participantes, o None si nadie se ha dado de alta."""
    return cargar_ruta(os.path.join(DATOS, "sujetos.csv"))
```

- [ ] **Paso 2: Hacer que el análisis del bucle de 1 kHz trabaje por patrón**

Sustituir `resumen_1khz` y `imprimir_1khz` por:

```python
def resumen_1khz(patron):
    """Comprueba la calidad del bucle haptico en el CSV nativo mas reciente que
    case con el patron: frecuencia real, jitter y ciclos largos.

    Se mira UN archivo y no la concatenacion de todos porque el salto de tiempo
    entre dos grabaciones distintas no es un ciclo del servo loop y falsearia el
    jitter."""
    encontrados = archivos(patron)
    if not encontrados:
        return None
    ruta = max((r for _, r in encontrados), key=os.path.getmtime)
    cols = cargar_ruta(ruta)
    if cols is None:
        return None
    t = num(cols, "t")
    if len(t) < 10:
        return None
    dt = np.diff(t)
    dt = dt[(dt > 0) & (dt < 0.1)]
    if len(dt) == 0:
        return None
    return {
        "archivo": os.path.basename(ruta),
        "muestras": len(t),
        "duracion": float(t[-1] - t[0]),
        "hz_medio": float(1.0 / np.mean(dt)),
        "jitter_ms": float(np.std(dt) * 1000.0),
        "pct_lentos": float(100.0 * np.mean(dt > 0.002)),
    }


def imprimir_1khz(patron):
    r = resumen_1khz(patron)
    if r is None:
        return
    print()
    print("Bucle haptico (%s)" % r["archivo"])
    print("  muestras          %d en %.1f s" % (r["muestras"], r["duracion"]))
    print("  frecuencia real   %.1f Hz" % r["hz_medio"])
    print("  jitter            sigma = %.3f ms" % r["jitter_ms"])
    print("  ciclos > 2 ms     %.2f %%" % r["pct_lentos"])
```

Actualizar también el comentario de cabecera del archivo (líneas 1-6):

```python
# comun.py -- Utilidades compartidas por los analisis de la Actividad 4.
#
# Los CSV los escribe Unity en Analysis/datos/<ID del sujeto>/. Se leen con el
# modulo csv de la biblioteca estandar (no pandas) para que el unico requisito
# real sean numpy y matplotlib, igual que en la Actividad 3:
#
#     pip install numpy matplotlib
#
# La lectura es siempre multi-sujeto: cargar_todos recorre las carpetas de
# sujeto y concatena, anadiendo la columna 'sujeto' si el CSV no la trae.
```

- [ ] **Paso 3: Actualizar `analizar_a1.py`**

- Línea 10-11: cambiar `cargar` por `cargar_todos` en la lista de importaciones.
- Línea 17: `traza = cargar("a1_traza.csv")` → `traza = cargar_todos("*_a1_e*_traza.csv")`
- Línea 18-19 (`falta("a1_traza.csv")`) → `falta("*_a1_e*_traza.csv")`
- Línea 90: `resumen = cargar("a1_resumen.csv")` → `resumen = cargar_todos("*_a1_resumen.csv")`
- Líneas 93 y 140: `imprimir_1khz("a1_1khz.csv")` → `imprimir_1khz("*_a1_e*_1khz.csv")`

Y en la rama en que `resumen` es `None`, si el mensaje menciona `a1_resumen.csv`,
cambiarlo a `*_a1_resumen.csv`.

- [ ] **Paso 4: Actualizar `analizar_a2.py`**

- Línea 11-12: `cargar` → `cargar_todos`.
- Línea 18: `cargar("a2_traza.csv")` → `cargar_todos("*_a2_e*_traza.csv")` (y el `falta` correspondiente).
- Línea 105: `cargar("a2_resumen.csv")` → `cargar_todos("*_a2_resumen.csv")`.
- Línea 135: `imprimir_1khz("a2_1khz.csv")` → `imprimir_1khz("*_a2_e*_1khz.csv")`.

- [ ] **Paso 5: Actualizar `analizar_a3.py`**

- Línea 13-14: `cargar` → `cargar_todos` (y añadir `cargar_ruta` si hiciera falta; no lo hace).
- Línea 37: `cargar("a3_traza.csv")` → `cargar_todos("*_a3_e*_traza.csv")` (y el `falta`).
- Línea 100: `cargar("a3_resumen.csv")` → `cargar_todos("*_a3_resumen.csv")`.
- Líneas 103 y 185: `imprimir_1khz("a3_1khz.csv")` → `imprimir_1khz("*_a3_e*_1khz.csv")`.
- Línea 161: `mil = cargar("a3_1khz.csv")` → `mil = cargar_todos("*_a3_e*_1khz.csv")`.

- [ ] **Paso 6: Añadir Hanói a `analizar_todo.py`**

Sustituir el archivo entero por:

```python
# analizar_todo.py -- Ejecuta los cuatro analisis de una vez.
#
#   pip install numpy matplotlib      (solo la primera vez)
#   python Analysis/analizar_todo.py
#
# Las figuras quedan en Analysis/figuras/ y las tablas se imprimen en la
# consola listas para copiarse al reporte.
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from comun import comprobar_datos, encabezado, tabla, sujetos, FIGURAS
import analizar_a1
import analizar_a2
import analizar_a3
import analizar_hanoi


def listar_sujetos():
    s = sujetos()
    if s is None:
        print("  (todavia no hay nadie dado de alta en sujetos.csv)")
        return
    filas = list(zip(s["id"], s["fecha"], s["edad"], s["mano"],
                     s["experiencia"], s["sesion"]))
    tabla("PARTICIPANTES",
          ["id", "fecha", "edad", "mano", "experiencia", "sesion"],
          filas)


def main():
    comprobar_datos()
    encabezado("SUJETOS")
    listar_sujetos()
    analizar_a1.analizar()
    analizar_a2.analizar()
    analizar_a3.analizar()
    analizar_hanoi.analizar()
    encabezado("LISTO")
    print("Figuras en: %s" % FIGURAS)


if __name__ == "__main__":
    main()
```

(Este archivo importa `analizar_hanoi`, que se escribe en la tarea 11. Ejecutar
`analizar_todo.py` fallará hasta entonces; los tres análisis individuales sí se
pueden correr sueltos.)

- [ ] **Paso 7: Verificar la lectura multi-sujeto**

Generar datos de dos sujetos: abrir `Menu`, registrar `S01`, hacer un ensayo del
ambiente 1, volver al menú, *Cambiar de sujeto*, registrar `S02` y hacer otro.

```bash
cd "C:/Users/76905/Downloads/ReportesOMNI"
python Actividad4/Analysis/analizar_a1.py
```

Esperado: corre sin error y las tablas incluyen filas de los dos sujetos. Además:

```bash
python -c "import sys; sys.path.insert(0,'Actividad4/Analysis'); import comun; c=comun.cargar_todos('*_a1_resumen.csv'); print(sorted(set(c['sujeto'])))"
```

Esperado: `['S01', 'S02']`.

- [ ] **Paso 8: Punto de control**

- `cargar_todos` devuelve `None` limpio cuando no hay datos, y no revienta con carpetas vacías.
- `_pruebas_previas/` y `ANON/` quedan fuera del análisis.
- Los tres análisis existentes corren con el formato nuevo.

---

### Task 11: `analizar_hanoi.py`

**Files:**
- Create: `Analysis/analizar_hanoi.py`

**Interfaces:**
- Consumes: `comun.cargar_todos`, `comun.num`, `comun.txt`, `comun.guardar`, `comun.falta`, `comun.tabla`, `comun.encabezado`, `comun.imprimir_1khz`, `comun.estilo`, `comun.COLORES` (tarea 10); los CSV de la tarea 7.
- Produces: `analizar_hanoi.analizar()` y cuatro figuras en `Analysis/figuras/`.

- [ ] **Paso 1: Escribir el análisis**

`Analysis/analizar_hanoi.py`:

```python
# analizar_hanoi.py -- Ambiente 4: Torres de Hanoi hapticas.
#
# Lee las trazas, las bitacoras de movimientos y los resumenes de todos los
# sujetos y responde a lo que pide la actividad para este ambiente: cuanto se
# alejo cada persona del optimo, si los discos pesados costaron mas, y cuanto
# empujaron contra la barrera de rechazo.
#
#   python Analysis/analizar_hanoi.py
import numpy as np
import matplotlib.pyplot as plt

from comun import (estilo, cargar_todos, num, txt, guardar, falta, tabla,
                   encabezado, imprimir_1khz, COLORES, comprobar_datos)


def analizar():
    encabezado("AMBIENTE 4 - TORRES DE HANOI")

    resumen = cargar_todos("*_hanoi_resumen.csv")
    if resumen is None:
        falta("*_hanoi_resumen.csv")
        return
    estilo()

    tabla_de_ensayos(resumen)
    figura_eficiencia(resumen)

    movs = cargar_todos("*_hanoi_e*_movs.csv")
    if movs is not None:
        figura_aprendizaje(movs)
        figura_por_disco(movs)
        tabla_de_rechazo(movs)

    traza = cargar_todos("*_hanoi_e*_traza.csv")
    if traza is not None:
        figura_trayectoria(traza)

    imprimir_1khz("*_hanoi_e*_1khz.csv")


# ---------------------------------------------------------------------------
def tabla_de_ensayos(r):
    filas = []
    for i in range(r["__n__"]):
        filas.append([
            r["sujeto"][i],
            r["sesion"][i],
            r["ensayo"][i],
            r["nivel"][i],
            r["discos"][i],
            "si" if r["completado"][i] in ("1", "True", "true") else "no",
            "%.1f" % float(r["tiempo_s"][i] or 0),
            "%s/%s" % (r["movimientos"][i], r["movimientos_optimos"][i]),
            "%.2f" % float(r["eficiencia"][i] or 0),
            r["intentos_ilegales"][i],
            r["caidas"][i],
            "%.2f" % float(r["fuerza_rechazo_pico_n"][i] or 0),
            r["guia"][i],
        ])
    tabla("ENSAYOS",
          ["sujeto", "ses", "ens", "nivel", "n", "fin", "t (s)", "mov/opt",
           "efic", "ileg", "caid", "rechazo (N)", "guia"],
          filas)


def figura_eficiencia(r):
    """Eficiencia frente al numero de discos, una serie por sujeto. Es la lectura
    central del ambiente: 1.0 es resolverlo en el minimo teorico."""
    discos = num(r, "discos")
    efic = num(r, "eficiencia")
    sujeto = txt(r, "sujeto")

    fig, ax = plt.subplots(figsize=(7, 4))
    for k, s in enumerate(dict.fromkeys(sujeto)):
        sel = sujeto == s
        orden = np.argsort(discos[sel])
        ax.plot(discos[sel][orden], efic[sel][orden], "o-",
                color=COLORES[k % len(COLORES)], label=str(s))
    ax.axhline(1.0, color="#8899AA", linestyle="--", linewidth=1)
    ax.set_xlabel("discos")
    ax.set_ylabel("eficiencia  (2^n-1) / movimientos")
    ax.set_title("Cercania al optimo segun la dificultad")
    ax.set_ylim(0, 1.1)
    ax.legend()
    guardar(fig, "hanoi_eficiencia.png")


def figura_aprendizaje(m):
    """Duracion de cada movimiento a lo largo del ensayo. Si la curva baja, el
    sujeto se esta soltando con el equipo."""
    n = num(m, "n_mov")
    dur = num(m, "t_fin") - num(m, "t_inicio")
    sujeto = txt(m, "sujeto")

    fig, ax = plt.subplots(figsize=(7, 4))
    for k, s in enumerate(dict.fromkeys(sujeto)):
        sel = sujeto == s
        orden = np.argsort(n[sel])
        ax.plot(n[sel][orden], dur[sel][orden], "-", alpha=0.55,
                color=COLORES[k % len(COLORES)], label=str(s))
    ax.set_xlabel("numero de movimiento")
    ax.set_ylabel("duracion del movimiento (s)")
    ax.set_title("Curva de aprendizaje")
    ax.legend()
    guardar(fig, "hanoi_aprendizaje.png")


def figura_por_disco(m):
    """Tiempo e impulso medios por disco. El disco 1 es el mas pequeno y ligero;
    el ultimo, el mas grande y pesado. Aqui se ve si el peso costo."""
    disco = num(m, "disco").astype(int)
    dur = num(m, "t_fin") - num(m, "t_inicio")
    imp = num(m, "impulso_ns")

    indices = sorted(set(disco.tolist()))
    tmed = [float(np.mean(dur[disco == d])) for d in indices]
    imed = [float(np.mean(imp[disco == d])) for d in indices]
    etiquetas = [str(d + 1) for d in indices]

    fig, (a1, a2) = plt.subplots(1, 2, figsize=(9, 4))
    a1.bar(etiquetas, tmed, color=COLORES[0])
    a1.set_xlabel("disco (1 = el mas pequeno)")
    a1.set_ylabel("tiempo medio por movimiento (s)")
    a1.set_title("Cuanto cuesta cada disco")

    a2.bar(etiquetas, imed, color=COLORES[2])
    a2.set_xlabel("disco (1 = el mas pequeno)")
    a2.set_ylabel("impulso medio  integral|F|dt  (N s)")
    a2.set_title("Esfuerzo por disco")
    guardar(fig, "hanoi_por_disco.png")

    tabla("COSTE POR DISCO",
          ["disco", "movimientos", "t medio (s)", "impulso medio (N s)"],
          [[e, int(np.sum(disco == d)), "%.2f" % t, "%.2f" % i]
           for e, d, t, i in zip(etiquetas, indices, tmed, imed)])


def tabla_de_rechazo(m):
    """Cuantos intentos ilegales hubo por movimiento y con cuanta fuerza pico. Es
    la metrica que solo existe porque la barrera es haptica."""
    ilegales = num(m, "intentos_ilegales")
    pico = num(m, "fuerza_pico_n")
    sujeto = txt(m, "sujeto")

    filas = []
    for s in dict.fromkeys(sujeto):
        sel = sujeto == s
        filas.append([s,
                      int(np.sum(sel)),
                      int(np.sum(ilegales[sel])),
                      "%.2f" % float(np.mean(ilegales[sel])),
                      "%.2f" % float(np.max(pico[sel]) if np.any(sel) else 0.0)])
    tabla("BARRERA DE RECHAZO",
          ["sujeto", "movimientos", "intentos ilegales", "por movimiento", "F pico (N)"],
          filas)


def figura_trayectoria(t):
    """La trayectoria de la punta del ensayo mas largo disponible, con los tramos
    de rechazo marcados en rojo."""
    archivo = txt(t, "archivo")
    cuales = list(dict.fromkeys(archivo))
    elegido = max(cuales, key=lambda a: int(np.sum(archivo == a)))
    sel = archivo == elegido

    x, y, z = num(t, "x")[sel], num(t, "y")[sel], num(t, "z")[sel]
    ilegal = num(t, "ilegal")[sel] > 0.5

    fig = plt.figure(figsize=(6.5, 5))
    ax = fig.add_subplot(111, projection="3d")
    ax.plot(x, z, y, color=COLORES[0], linewidth=0.8, alpha=0.85)
    if np.any(ilegal):
        ax.scatter(x[ilegal], z[ilegal], y[ilegal], s=4,
                   color=COLORES[3], label="contra la barrera")
        ax.legend()
    ax.set_xlabel("x (u)")
    ax.set_ylabel("z (u)")
    ax.set_zlabel("y (u)")
    ax.set_title("Trayectoria de la punta · %s" % elegido)
    guardar(fig, "hanoi_trayectoria.png")


if __name__ == "__main__":
    comprobar_datos()
    analizar()
```

- [ ] **Paso 2: Correr el análisis con datos reales**

Hacer dos ensayos de Hanói con `S01` (uno fácil, uno de 5 discos) y uno con
`S02`, provocando a propósito algún intento ilegal y alguna caída. Luego:

```bash
cd "C:/Users/76905/Downloads/ReportesOMNI"
python Actividad4/Analysis/analizar_hanoi.py
```

Esperado: cuatro figuras en `Actividad4/Analysis/figuras/`
(`hanoi_eficiencia.png`, `hanoi_aprendizaje.png`, `hanoi_por_disco.png`,
`hanoi_trayectoria.png`) y tres tablas en consola. Comprobar en la tabla de
ensayos que `mov/opt` y `efic` cuadran con lo que decía el HUD al terminar.

- [ ] **Paso 3: Correr el análisis completo**

```bash
python Actividad4/Analysis/analizar_todo.py
```

Esperado: la tabla de participantes seguida de los cuatro ambientes, sin
excepciones. Los ambientes sin datos deben decir "(falta ...)" en vez de fallar.

- [ ] **Paso 4: Punto de control**

- `analizar_hanoi.py` corre solo y desde `analizar_todo.py`.
- Con un único sujeto y sin intentos ilegales tampoco falla (la figura de rechazo simplemente no marca puntos).
- Las figuras usan el estilo oscuro común, para que peguen con las capturas de las escenas en el reporte.

---

## Verificación final

Antes de dar el trabajo por terminado:

1. `Window > General > Test Runner > EditMode > Run All` — los 13 tests de `EstadoHanoiTests` en verde.
2. `Haptics > Construir TODOS los ambientes` — cinco escenas sin errores en consola.
3. `Haptics > Capturar imagenes de los ambientes` — cinco PNG en `Capturas/`.
4. Un recorrido completo con el equipo conectado: registrar sujeto, un ensayo de cada ambiente, comprobar que `Analysis/datos/<ID>/` tiene los archivos de los cuatro.
5. `python Actividad4/Analysis/analizar_todo.py` sin excepciones.
6. Comprobar a mano, con el Touch en la mano, lo que ningún test puede: que el disco grande se siente claramente más pesado que el pequeño, que la barrera de rechazo se lee como una pared y no como algo blando, y que el HUD no marca `Unstable` durante el ensayo.
