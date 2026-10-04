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

