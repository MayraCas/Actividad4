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
        /// Devuelve la altura que consumio, para que quien lo llama coloque lo que
        /// sigue justo debajo: el alto de este panel cambia segun la rama y segun
        /// si hay aviso de ID invalido, y reservarle una altura fija hacia que se
        /// solapara con los botones de abajo.</summary>
        public static float Dibujar(Rect caja, GUIStyle titulo, GUIStyle texto,
                                    GUIStyle chico, GUIStyle boton)
        {
            float y0 = caja.y;
            var r = new Rect(caja.x, caja.y, caja.width, 22);

            if (HaySujeto)
            {
                GUI.Label(r, "SUJETO", texto); r.y += 24;
                GUI.Label(r, SesionSujeto.Actual.Etiqueta, chico); r.y += 22;
                if (GUI.Button(new Rect(r.x, r.y, 160, 26), "Cambiar de sujeto", boton))
                {
                    Object.Destroy(SesionSujeto.Actual.gameObject);
                    Limpiar();
                }
                r.y += 30;
                return r.y - y0;
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
            {
                GUI.Label(new Rect(r.x, r.y, caja.width, 20),
                          "El ID solo admite letras, digitos, guion y guion bajo (max. 24).", chico);
                r.y += 22;
            }

            return r.y - y0;
        }

        /// <summary>Vacia el formulario al cambiar de sujeto. Sin esto, el siguiente
        /// participante heredaria el ID, la edad y sobre todo las notas del anterior,
        /// que es exactamente la mezcla de datos que este registro existe para evitar.</summary>
        private static void Limpiar()
        {
            id = "";
            edad = "";
            notas = "";
            mano = 0;
            experiencia = 0;
            condicion = 0;
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
