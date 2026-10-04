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

