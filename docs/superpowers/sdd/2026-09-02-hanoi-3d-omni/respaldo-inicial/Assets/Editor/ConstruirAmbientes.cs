// ConstruirAmbientes.cs -- Genera las cuatro escenas de la Actividad 4.
//
//   Haptics / Construir TODOS los ambientes
//   Haptics / Escenas / Menu
//   Haptics / Escenas / Ambiente 1 - Manipulacion
//   Haptics / Escenas / Ambiente 2 - Laberinto (nivel 1 / 2 / 3)
//   Haptics / Escenas / Ambiente 3 - Fuerza
//
// Las escenas son desechables: no se editan a mano, se regeneran. Asi el
// escenario que aparece en el reporte esta descrito por codigo revisable en vez
// de por un archivo .unity binario que nadie puede leer.
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HapticOmni.EditorTools
{
    public static class ConstruirAmbientes
    {
        private const string RUTA = "Assets/Scenes/";

        // =====================================================================
        [MenuItem("Haptics/Construir TODOS los ambientes", priority = 0)]
        public static void Todo()
        {
            ConstruirMenu();
            ConstruirA1();
            ConstruirA2Nivel2();
            ConstruirA3();
            EditorSceneManager.OpenScene(RUTA + "Menu.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("[Actividad 4] Las cuatro escenas estan listas. Abre Menu y pulsa Play.");
        }

        // =====================================================================
        //  MENU
        // =====================================================================
        [MenuItem("Haptics/Escenas/Menu", priority = 20)]
        public static void ConstruirMenu()
        {
            var escena = NuevaEscena();
            EstiloAmbientes.Iluminacion();
            EstiloAmbientes.PostProceso("VP_Ambientes");
            EstiloAmbientes.Camara(new Vector3(0f, 0f, -6f), new Vector3(0f, 0f, 0f), 50f);

            GameObject cursor;
            var dev = EstiloAmbientes.EspacioHaptico(out cursor, 0.05f, EstiloAmbientes.Cian);
            dev.shapesEnabled = false;
            cursor.SetActive(true);

            // Telon de fondo: tres tarjetas que insinuan los tres ambientes.
            var matPanel = EstiloAmbientes.MatTexturizado("M_PanelMenu", new Color(0.24f, 0.29f, 0.38f),
                                                          EstiloAmbientes.TexturaRejillaClara(), new Vector2(4, 3));
            var raiz = new GameObject("Decorado");
            for (int i = 0; i < 3; i++)
            {
                var c = EstiloAmbientes.PaletaPiezas[i];
                EstiloAmbientes.Primitiva(PrimitiveType.Cube, "Tarjeta " + (i + 1), raiz.transform,
                                          new Vector3(-2.6f + i * 2.6f, -1.9f, 1.2f),
                                          new Vector3(2.1f, 1.3f, 0.06f), matPanel);
                EstiloAmbientes.Numeral(i + 1, "Numero " + (i + 1), raiz.transform,
                                        new Vector3(-2.6f + i * 2.6f, -1.9f, 1.1f), c, 0.06f, 0.14f);
            }

            var sel = new GameObject("Selector");
            sel.AddComponent<SelectorAmbientes>();

            Guardar(escena, "Menu", 0);
        }

        // =====================================================================
        //  AMBIENTE 1 - MANIPULACION DE OBJETOS
        // =====================================================================
        [MenuItem("Haptics/Escenas/Ambiente 1 - Manipulacion", priority = 21)]
        public static void ConstruirA1()
        {
            var escena = NuevaEscena();
            EstiloAmbientes.Iluminacion();
            EstiloAmbientes.PostProceso("VP_Ambientes");
            EstiloAmbientes.Camara(new Vector3(0f, 0.75f, -2.95f), new Vector3(25f, 0f, 0f), 52f);

            const float radioPunta = 0.05f;
            GameObject cursor;
            var dev = EstiloAmbientes.EspacioHaptico(out cursor, radioPunta, EstiloAmbientes.Cian);
            dev.attractorsEnabled = false;

            var matLinea = EstiloAmbientes.Mat("M_Marco", new Color(0.20f, 0.42f, 0.55f), 0.35f);
            EstiloAmbientes.MarcoEspacioDeTrabajo(dev.transform, dev.unitsPerMillimeter, matLinea);

            // ---- la mesa -----------------------------------------------------
            // Es una caja haptica prioritaria: siempre se envia al dispositivo,
            // aunque la mano este lejos, porque es la superficie de referencia
            // contra la que se apoya todo el ejercicio.
            const float alturaMesa = -1.02f;
            var matMesa = EstiloAmbientes.MatTexturizado("M_Mesa", new Color(0.30f, 0.35f, 0.45f),
                                                         EstiloAmbientes.TexturaRejillaClara(), new Vector2(9, 5), 0f, 0.15f);
            var mesa = EstiloAmbientes.Primitiva(PrimitiveType.Cube, "Mesa de trabajo", null,
                                                 new Vector3(0f, alturaMesa - 0.07f, 0f),
                                                 new Vector3(4.4f, 0.14f, 2.0f), matMesa, true);
            var formaMesa = mesa.AddComponent<HapticShape>();
            formaMesa.tipo = TipoForma.Caja;
            formaMesa.rigidez = 0.18f;
            formaMesa.friccion = 0.30f;
            formaMesa.prioritaria = true;
            formaMesa.colorContacto = new Color(0, 0, 0, 0);   // la mesa no se resalta

            // Reborde trasero, para que las piezas no se pierdan por detras.
            var matBorde = EstiloAmbientes.Mat("M_Borde", EstiloAmbientes.GrisMedio, 0f, 0.25f);
            EstiloAmbientes.Primitiva(PrimitiveType.Cube, "Reborde", null,
                                      new Vector3(0f, alturaMesa + 0.09f, 1.02f),
                                      new Vector3(4.4f, 0.18f, 0.06f), matBorde, true);

            // ---- piezas y receptaculos ---------------------------------------
            // Cuatro masas muy separadas: 0.05 kg se sienten como una pluma y
            // 0.28 kg (2.75 N) llegan casi al maximo que el Touch puede dar.
            string[] nombres = { "Pieza ligera", "Pieza media", "Pieza pesada", "Pieza muy pesada" };
            float[]  masas   = { 0.05f, 0.12f, 0.20f, 0.28f };
            float[]  lados   = { 0.19f, 0.25f, 0.30f, 0.35f };
            // El destino de cada pieza no esta enfrente: obliga a cruzar la mesa.
            int[]    destinoDe = { 2, 0, 3, 1 };

            float[] xPieza   = { -1.55f, -0.52f, 0.52f, 1.55f };
            float[] xDestino = { -1.55f, -0.52f, 0.52f, 1.55f };
            const float zPieza = 0.42f, zDestino = -0.48f;

            var piezas   = new List<ObjetoAgarrable>();
            var destinos = new List<ZonaDestino>();

            var raizP = new GameObject("Piezas");
            var raizD = new GameObject("Receptaculos");

            for (int i = 0; i < 4; i++)
            {
                Color c = EstiloAmbientes.PaletaPiezas[i];
                var matP = EstiloAmbientes.Mat("M_Pieza" + i, c, 0.35f, 0.45f, 0.15f);

                var go = EstiloAmbientes.Primitiva(PrimitiveType.Cube, nombres[i], raizP.transform,
                                                   new Vector3(xPieza[i], alturaMesa + lados[i] * 0.5f, zPieza),
                                                   Vector3.one * lados[i], matP, true);
                var forma = go.AddComponent<HapticShape>();
                forma.tipo = TipoForma.Caja;
                forma.rigidez = 0.20f;
                forma.friccion = 0.25f;
                forma.colorContacto = Color.Lerp(c, Color.white, 0.55f);

                var cuerpo = go.AddComponent<Rigidbody>();
                cuerpo.mass = masas[i];
                cuerpo.linearDamping = 0.5f;
                cuerpo.angularDamping = 0.9f;

                var pieza = go.AddComponent<ObjetoAgarrable>();
                pieza.nombrePieza = nombres[i];
                pieza.masaKg = masas[i];
                pieza.idDestino = destinoDe[i];
                piezas.Add(pieza);

                EstiloAmbientes.Numeral(i + 1, "Marca " + (i + 1), raizP.transform,
                                        new Vector3(xPieza[i], alturaMesa + 0.01f, zPieza - lados[i] * 1.1f),
                                        c, 0.042f, 0.085f);
            }

            for (int d = 0; d < 4; d++)
            {
                // El color del receptaculo es el de la pieza que le toca.
                int piezaDeEsteDestino = System.Array.IndexOf(destinoDe, d);
                Color c = EstiloAmbientes.PaletaPiezas[Mathf.Max(0, piezaDeEsteDestino)];
                var matD = EstiloAmbientes.Mat("M_Destino" + d, c * 0.55f, 1.4f, 0.5f);

                // Aro exterior mas apagado + disco interior brillante: le da al
                // receptaculo un borde reconocible en vez de leerse como una mancha.
                var matAro = EstiloAmbientes.Mat("M_DestinoAro" + d, c * 0.30f, 0f, 0.3f, 0.4f);
                EstiloAmbientes.Primitiva(PrimitiveType.Cylinder, "Aro " + (d + 1), raizD.transform,
                                          new Vector3(xDestino[d], alturaMesa + 0.008f, zDestino),
                                          new Vector3(0.62f, 0.008f, 0.62f), matAro);

                var bandeja = EstiloAmbientes.Primitiva(PrimitiveType.Cylinder, "Receptaculo " + (d + 1),
                                                        raizD.transform,
                                                        new Vector3(xDestino[d], alturaMesa + 0.014f, zDestino),
                                                        new Vector3(0.50f, 0.010f, 0.50f), matD);
                var zona = bandeja.AddComponent<ZonaDestino>();
                zona.id = d;
                zona.radio = 0.26f;
                zona.color = c;
                destinos.Add(zona);

                EstiloAmbientes.Numeral(piezaDeEsteDestino + 1, "Marca destino " + (d + 1), raizD.transform,
                                        new Vector3(xDestino[d], alturaMesa + 0.02f, zDestino - 0.40f),
                                        c, 0.042f, 0.085f);
            }

            // ---- guia haptica opcional ---------------------------------------
            var guiaGO = new GameObject("Guia haptica");
            var guia = guiaGO.AddComponent<HapticAttractor>();
            guia.stiffness = 0.03f;
            guia.radius = 2.5f;
            guiaGO.SetActive(false);

            // ---- logica y HUD -------------------------------------------------
            var amb = dev.gameObject.AddComponent<Ambiente1Manipulacion>();
            amb.dispositivo = dev;
            amb.piezas = piezas.ToArray();
            amb.destinos = destinos.ToArray();
            amb.alturaMesa = alturaMesa;
            amb.guia = guia;

            var hud = dev.gameObject.AddComponent<HUDAmbiente>();
            hud.dispositivo = dev;
            hud.ambiente = amb;

            Guardar(escena, "Ambiente1_Manipulacion", 1);
        }

        // =====================================================================
        //  AMBIENTE 2 - PRECISION MOTRIZ (LABERINTO)
        // =====================================================================
        [MenuItem("Haptics/Escenas/Ambiente 2 - Laberinto (nivel 1 facil)", priority = 22)]
        public static void ConstruirA2Nivel1() { ConstruirA2(1, 20250825); }

        [MenuItem("Haptics/Escenas/Ambiente 2 - Laberinto (nivel 2 medio)", priority = 23)]
        public static void ConstruirA2Nivel2() { ConstruirA2(2, 20250825); }

        [MenuItem("Haptics/Escenas/Ambiente 2 - Laberinto (nivel 3 dificil)", priority = 24)]
        public static void ConstruirA2Nivel3() { ConstruirA2(3, 20250825); }

        public static void ConstruirA2(int nivel, int semilla)
        {
            // columnas, filas, ancho de pasillo, grosor de muro, radio de la bola.
            // La holgura por lado (pasillo - diametro)/2 baja de 8 a 5 cm de Unity
            // (3.2 a 2.0 mm reales del dispositivo): ahi esta la dificultad.
            int[]   cols   = { 7, 9, 11 };
            int[]   filas  = { 5, 6, 8 };
            float[] pasillo= { 0.42f, 0.34f, 0.27f };
            float[] muro   = { 0.07f, 0.06f, 0.05f };
            float[] bola   = { 0.13f, 0.11f, 0.085f };
            int n = Mathf.Clamp(nivel, 1, 3) - 1;

            var escena = NuevaEscena();
            EstiloAmbientes.Iluminacion();
            EstiloAmbientes.PostProceso("VP_Ambientes");
            // Camara mas lejos y con mucho menos angulo que en los otros ambientes:
            // el encuadre es el mismo (media altura visible ~1.50 u, y el tablero
            // mas alto mide 1.31 u de media altura) pero la vista queda casi
            // ortogonal. Hace falta: con muros de 0.60 u de alto, una vista abierta
            // esconderia la bola detras de las paredes en los pasillos del borde.
            EstiloAmbientes.Camara(new Vector3(0f, 0f, -6.5f), Vector3.zero, 26f);

            float radioBola = bola[n];
            GameObject cursor;
            var dev = EstiloAmbientes.EspacioHaptico(out cursor, radioBola, EstiloAmbientes.Cian);
            dev.attractorsEnabled = false;
            cursor.name = "Esfera (bola del laberinto)";

            var lab = Laberinto.Generar(cols[n], filas[n], pasillo[n], muro[n], semilla);

            // ---- perfil en Z del tablero ---------------------------------------
            // Los muros son ALTOS a proposito. Lo que sujeta la bola en el plano
            // del tablero es la ranura, y una ranura es fuerza, no pared: el Touch
            // satura a ~3.3 N y la ranura tiene rigidez 0.22 N/mm, asi que tirando
            // del brazo se le pueden sacar a la punta ~15 mm reales -- unas 0.38
            // unidades de Unity con la escala de estas escenas. Con los muros de
            // 0.20 u de antes solo habia 0.10 u de muro por delante del plano de
            // la bola (0.21 u contando el radio de sonda): la bola se montaba
            // encima de las paredes y se salia del laberinto. Con 0.60 u la cara
            // frontal queda en -0.42 u y la barrera efectiva pasa de 0.50 u, por
            // encima de lo que el usuario puede ganar antes de saturar el motor.
            const float profundidadMuro = 0.60f;
            const float zTraseraMuro    = 0.18f;   // cara trasera, pegada a la placa
            const float zCentroMuro     = zTraseraMuro - profundidadMuro * 0.5f;
            const float zPlaca          = zTraseraMuro + 0.03f;   // placa de 0.06 de canto

            var tablero = new GameObject("Tablero");
            var matPlaca = EstiloAmbientes.MatTexturizado("M_PlacaLaberinto", new Color(0.24f, 0.29f, 0.38f),
                                                          EstiloAmbientes.TexturaRejilla(),
                                                          new Vector2(cols[n], filas[n]), 0f, 0.12f);
            EstiloAmbientes.Primitiva(PrimitiveType.Cube, "Placa de fondo", tablero.transform,
                                      new Vector3(0f, 0f, zPlaca),
                                      new Vector3(lab.AnchoTablero + 0.12f, lab.AltoTablero + 0.12f, 0.06f),
                                      matPlaca);

            // ---- muros --------------------------------------------------------
            var matMuro = EstiloAmbientes.MatTexturizado("M_MuroLaberinto", new Color(0.42f, 0.50f, 0.66f),
                                                         EstiloAmbientes.TexturaRuido(), new Vector2(3, 3), 0f, 0.30f);
            var raizMuros = new GameObject("Muros");
            raizMuros.transform.SetParent(tablero.transform, false);

            var barras = lab.Barras();
            var formasMuro = new List<HapticShape>();
            for (int i = 0; i < barras.Count; i++)
            {
                var b = barras[i];
                var go = EstiloAmbientes.Primitiva(PrimitiveType.Cube, "Muro " + i, raizMuros.transform,
                                                   new Vector3(b.centro.x, b.centro.y, zCentroMuro),
                                                   new Vector3(b.tamano.x, b.tamano.y, profundidadMuro), matMuro);
                var f = go.AddComponent<HapticShape>();
                f.tipo = TipoForma.Caja;
                f.rigidez = 0.20f;
                f.friccion = 0.12f;
                f.colorContacto = new Color(1f, 0.45f, 0.35f);
                formasMuro.Add(f);
            }

            // ---- la ranura que mantiene la bola en el plano del tablero -------
            // Dos semiespacios enfrentados. Con el radio de sonda, el hueco libre
            // para el centro de la bola queda en +-0.02 unidades alrededor de z=0:
            // la bola puede rodar por el laberinto pero no salirse hacia el frente
            // ni hundirse en la placa.
            float sep = radioBola + 0.02f;
            var raizRanura = new GameObject("Ranura del tablero");
            raizRanura.transform.SetParent(tablero.transform, false);
            CrearSemiespacio(raizRanura.transform, "Fondo de la ranura", new Vector3(0, 0, -sep), Vector3.forward);
            CrearSemiespacio(raizRanura.transform, "Tapa de la ranura",  new Vector3(0, 0,  sep), Vector3.back);

            // ---- salida y meta ------------------------------------------------
            Vector2 cIni = lab.CentroCelda(0, 0);
            Vector2 cFin = lab.CentroCelda(lab.columnas - 1, lab.filas - 1);

            // Emision moderada: por encima de ~1.2 el bloom convierte el color en un
            // borron blanco y salida y meta dejan de distinguirse.
            var matSalida = EstiloAmbientes.Mat("M_Salida", EstiloAmbientes.Verde, 1.0f);
            var matMeta   = EstiloAmbientes.Mat("M_Meta",   EstiloAmbientes.Rojo,  1.1f);
            // Las marcas van adelantadas, cerca de la boca del canal: al fondo del
            // pasillo los muros altos las taparian justo en las esquinas, que es
            // donde caen la salida y la meta. Su Z ya no importa para la logica,
            // porque las distancias de salida y meta se miden en el plano.
            const float zMarca = -0.20f;
            var salida = EstiloAmbientes.Primitiva(PrimitiveType.Cube, "Salida", tablero.transform,
                                                   new Vector3(cIni.x, cIni.y, zMarca),
                                                   new Vector3(pasillo[n] * 0.8f, pasillo[n] * 0.8f, 0.02f), matSalida);
            var meta = EstiloAmbientes.Primitiva(PrimitiveType.Cube, "Meta", tablero.transform,
                                                 new Vector3(cFin.x, cFin.y, zMarca),
                                                 new Vector3(pasillo[n] * 0.8f, pasillo[n] * 0.8f, 0.02f), matMeta);

            // ---- camino optimo -------------------------------------------------
            var celdas = lab.Resolver(new Vector2Int(0, 0), new Vector2Int(lab.columnas - 1, lab.filas - 1));
            var camino = new Vector3[celdas.Count];
            float longitud = 0f;
            for (int i = 0; i < celdas.Count; i++)
            {
                Vector2 c = lab.CentroCelda(celdas[i].x, celdas[i].y);
                camino[i] = tablero.transform.TransformPoint(new Vector3(c.x, c.y, 0f));
                if (i > 0) longitud += Vector3.Distance(camino[i], camino[i - 1]);
            }

            // ---- logica y HUD --------------------------------------------------
            var amb = dev.gameObject.AddComponent<Ambiente2Laberinto>();
            amb.dispositivo = dev;
            amb.bola = cursor.transform;
            amb.salida = salida.transform;
            amb.meta = meta.transform;
            amb.radioMeta = pasillo[n] * 0.45f;
            amb.muros = formasMuro.ToArray();
            amb.caminoOptimo = camino;
            amb.longitudOptima = longitud;
            amb.nivel = nivel;
            amb.semilla = semilla;

            var hud = dev.gameObject.AddComponent<HUDAmbiente>();
            hud.dispositivo = dev;
            hud.ambiente = amb;

            Debug.Log(string.Format("[Ambiente 2] Nivel {0}: {1}x{2} celdas, {3} barras de muro, " +
                                    "tablero {4:F2}x{5:F2} u, camino optimo {6:F2} u, holgura por lado {7:F3} u",
                                    nivel, cols[n], filas[n], barras.Count,
                                    lab.AnchoTablero, lab.AltoTablero, longitud,
                                    (pasillo[n] - 2f * radioBola) * 0.5f));

            Guardar(escena, "Ambiente2_Laberinto", 2);
        }

        private static void CrearSemiespacio(Transform padre, string nombre, Vector3 pos, Vector3 normal)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.localPosition = pos;
            var f = go.AddComponent<HapticShape>();
            f.tipo = TipoForma.Semiespacio;
            f.normalLocal = normal;
            f.rigidez = 0.22f;
            f.friccion = 0f;
            f.prioritaria = true;
            f.colorContacto = new Color(0, 0, 0, 0);
        }

        // =====================================================================
        //  AMBIENTE 3 - INTERACCION CON FUERZA
        // =====================================================================
        [MenuItem("Haptics/Escenas/Ambiente 3 - Fuerza", priority = 25)]
        public static void ConstruirA3()
        {
            var escena = NuevaEscena();
            EstiloAmbientes.Iluminacion();
            EstiloAmbientes.PostProceso("VP_Ambientes");
            EstiloAmbientes.Camara(new Vector3(0f, 0f, -3.15f), Vector3.zero, 50f);

            GameObject cursor;
            var dev = EstiloAmbientes.EspacioHaptico(out cursor, 0.05f, EstiloAmbientes.Cian);
            dev.shapesEnabled = false;        // aqui la haptica la pone el riel, no los contactos
            dev.attractorsEnabled = false;

            var matPanel = EstiloAmbientes.MatTexturizado("M_PanelBanco", new Color(0.26f, 0.31f, 0.40f),
                                                          EstiloAmbientes.TexturaRejillaClara(),
                                                          new Vector2(5, 5), 0f, 0.12f);
            EstiloAmbientes.Primitiva(PrimitiveType.Cube, "Panel del banco", null,
                                      new Vector3(0.02f, 0f, 0.58f), new Vector3(2.45f, 2.50f, 0.08f), matPanel);

            // Las cinco estaciones se ven identicas a proposito: lo unico que las
            // distingue es lo que se siente en la mano.
            string[] nombres = { "Libre", "Viscosa", "Friccion seca", "Resorte", "Piston" };
            string[] descr =
            {
                "Sin resistencia. Es la referencia contra la que se comparan las demas.",
                "F = b·v. Cuesta mas cuanto mas rapido se mueve, como empujar dentro de miel.",
                "Fs = 1.6 N, Fk = 0.7 N. Hay que vencer la adherencia y luego cede de golpe.",
                "F = k·s. Cuesta mas cuanto mas lejos se lleva, y empuja de vuelta.",
                "Viscosa + friccion + resorte suave: el comportamiento de un amortiguador real.",
            };
            float[] ks  = { 0f,     0f,      0f,    0.030f, 0.012f };
            float[] bs  = { 0f,     0.0040f, 0f,    0f,     0.0025f };
            float[] fss = { 0f,     0f,      1.60f, 0f,     0.70f };
            float[] fks = { 0f,     0f,      0.70f, 0f,     0.40f };

            const float longitud = 1.40f;
            const float x0 = -0.70f;
            float[] ys = { 1.02f, 0.51f, 0f, -0.51f, -1.02f };

            var matRiel   = EstiloAmbientes.Mat("M_Riel", new Color(0.22f, 0.26f, 0.34f), 0f, 0.4f, 0.3f);
            var matTope   = EstiloAmbientes.Mat("M_Tope", EstiloAmbientes.GrisClaro, 0.6f);
            var estaciones = new List<EstacionResistencia>();
            var raiz = new GameObject("Estaciones");

            for (int i = 0; i < 5; i++)
            {
                Color c = EstiloAmbientes.PaletaPiezas[i];
                var nodo = new GameObject(nombres[i]);
                nodo.transform.SetParent(raiz.transform, false);
                nodo.transform.localPosition = new Vector3(x0, ys[i], 0f);

                // El riel se dibuja desde el origen de la estacion hacia +X.
                EstiloAmbientes.Primitiva(PrimitiveType.Cube, "Riel", nodo.transform,
                                          new Vector3(longitud * 0.5f, 0f, 0.04f),
                                          new Vector3(longitud + 0.10f, 0.05f, 0.05f), matRiel);
                EstiloAmbientes.Primitiva(PrimitiveType.Cube, "Tope inicial", nodo.transform,
                                          new Vector3(-0.05f, 0f, 0.02f), new Vector3(0.05f, 0.20f, 0.09f), matTope);
                var metaMat = EstiloAmbientes.Mat("M_Meta" + i, c, 1.0f);
                EstiloAmbientes.Primitiva(PrimitiveType.Cube, "Tope final", nodo.transform,
                                          new Vector3(longitud + 0.05f, 0f, 0.02f),
                                          new Vector3(0.05f, 0.20f, 0.09f), metaMat);

                var matCorr = EstiloAmbientes.Mat("M_Corredera" + i, c, 0.6f, 0.5f, 0.2f);
                var corr = EstiloAmbientes.Primitiva(PrimitiveType.Cube, "Corredera", nodo.transform,
                                                     Vector3.zero, new Vector3(0.20f, 0.16f, 0.16f), matCorr);

                EstiloAmbientes.Numeral(i + 1, "Numero", nodo.transform,
                                        new Vector3(-0.24f, 0.16f, 0f), c, 0.040f, 0.070f);

                var est = nodo.AddComponent<EstacionResistencia>();
                est.nombre = nombres[i];
                est.descripcion = descr[i];
                est.color = c;
                est.corredera = corr.transform;
                est.ejeLocal = Vector3.right;
                est.longitud = longitud;
                est.k = ks[i];
                est.b = bs[i];
                est.Fs = fss[i];
                est.Fk = fks[i];
                estaciones.Add(est);
            }

            var amb = dev.gameObject.AddComponent<Ambiente3Fuerza>();
            amb.dispositivo = dev;
            amb.estaciones = estaciones.ToArray();

            var hud = dev.gameObject.AddComponent<HUDAmbiente>();
            hud.dispositivo = dev;
            hud.ambiente = amb;

            Guardar(escena, "Ambiente3_Fuerza", 3);
        }

        // =====================================================================
        private static UnityEngine.SceneManagement.Scene NuevaEscena()
        {
            EstiloAmbientes.AsegurarCarpetas();
            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static void Guardar(UnityEngine.SceneManagement.Scene escena, string nombre, int posicion)
        {
            string ruta = RUTA + nombre + ".unity";
            EditorSceneManager.SaveScene(escena, ruta);
            EstiloAmbientes.RegistrarEscena(ruta, posicion);
            AssetDatabase.SaveAssets();
            Debug.Log("[Actividad 4] Escena guardada: " + ruta);
        }
    }
}
