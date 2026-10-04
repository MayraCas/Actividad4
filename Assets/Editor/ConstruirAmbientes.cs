// ConstruirAmbientes.cs -- Genera las cinco escenas de la Actividad 4.
//
//   Haptics / Construir TODOS los ambientes
//   Haptics / Escenas / Menu
//   Haptics / Escenas / Ambiente 1 - Manipulacion
//   Haptics / Escenas / Ambiente 2 - Laberinto
//   Haptics / Escenas / Ambiente 3 - Fuerza
//   Haptics / Escenas / Ambiente 4 - Torres de Hanoi
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
            ConstruirA2();
            ConstruirA3();
            ConstruirA4();
            EditorSceneManager.OpenScene(RUTA + "Menu.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("[Actividad 4] Las cinco escenas estan listas. Abre Menu y pulsa Play.");
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
        //  AMBIENTE 2 - LABERINTO DE PRECISION
        // =====================================================================
        // Este metodo ya NO construye el laberinto: construye el armazon. El
        // tablero lo levanta ConstructorLaberinto en tiempo de ejecucion, de modo
        // que una sola escena sirve para los cinco niveles y la geometria vive en
        // StreamingAssets/laberinto.json en vez de cocida en el .unity.
        [MenuItem("Haptics/Escenas/Ambiente 2 - Laberinto", priority = 22)]
        public static void ConstruirA2()
        {
            var escena = NuevaEscena();
            EstiloAmbientes.Iluminacion();
            EstiloAmbientes.PostProceso("VP_Ambientes");
            // Camara casi ortogonal: con muros de 0.60 u de alto, una vista abierta
            // esconderia la bola detras de las paredes en los pasillos del borde. La
            // DISTANCIA la fija el constructor segun el tamano del tablero; aqui solo
            // se elige el angulo.
            var cam = EstiloAmbientes.Camara(new Vector3(0f, 0f, -6.5f), Vector3.zero, 26f);

            GameObject cursor;
            var dev = EstiloAmbientes.EspacioHaptico(out cursor, 0.11f, EstiloAmbientes.Cian);
            dev.attractorsEnabled = false;
            cursor.name = "Esfera (bola del laberinto)";

            var matLinea = EstiloAmbientes.Mat("M_Marco", new Color(0.20f, 0.42f, 0.55f), 0.35f);
            EstiloAmbientes.MarcoEspacioDeTrabajo(dev.transform, dev.unitsPerMillimeter, matLinea);

            // Los materiales se crean aqui, en el editor, y se le entregan al
            // constructor: EstiloAmbientes no existe en runtime. Son assets del
            // proyecto, asi que la referencia sobrevive a recargar la escena.
            var matPlaca = EstiloAmbientes.MatTexturizado("M_PlacaLaberinto",
                                                          new Color(0.24f, 0.29f, 0.38f),
                                                          EstiloAmbientes.TexturaRejilla(),
                                                          Vector2.one, 0f, 0.12f);
            var matMuro = EstiloAmbientes.MatTexturizado("M_MuroLaberinto",
                                                         new Color(0.42f, 0.50f, 0.66f),
                                                         EstiloAmbientes.TexturaRuido(),
                                                         new Vector2(3, 3), 0f, 0.30f);
            // Emision moderada: por encima de ~1.2 el bloom convierte el color en un
            // borron blanco y salida y meta dejan de distinguirse.
            var matSalida = EstiloAmbientes.Mat("M_Salida", EstiloAmbientes.Verde, 1.0f);
            var matMeta   = EstiloAmbientes.Mat("M_Meta",   EstiloAmbientes.Rojo,  1.1f);

            var tablero = new GameObject("Tablero");

            var con = dev.gameObject.AddComponent<ConstructorLaberinto>();
            con.raizTablero = tablero.transform;
            con.camara = cam;
            con.dispositivo = dev;
            con.cursor = cursor.transform;
            con.matPlaca = matPlaca;
            con.matMuro = matMuro;
            con.matSalida = matSalida;
            con.matMeta = matMeta;

            var amb = dev.gameObject.AddComponent<Ambiente2Laberinto>();
            amb.dispositivo = dev;
            amb.bola = cursor.transform;
            amb.constructor = con;
            amb.nivel = 2;

            var panel = dev.gameObject.AddComponent<PanelConfigLaberinto>();
            panel.ambiente = amb;

            var hud = dev.gameObject.AddComponent<HUDAmbiente>();
            hud.dispositivo = dev;
            hud.ambiente = amb;

            // Se deja el nivel por defecto ya construido para que la escena guardada
            // sea presentable: es la que se abre en el editor y la que fotografia
            // CapturarAmbientes, que trabaja en modo edicion y no pasa por Play.
            var cfg = ConfiguracionLaberinto.Cargar();
            con.Construir(cfg.Nivel(amb.nivel - 1), cfg);

            Debug.Log(string.Format("[Ambiente 2] Armazon listo. Nivel por defecto {0} ({1}): " +
                                    "{2} muros, camino optimo {3:F2} u",
                                    amb.nivel, cfg.Nivel(amb.nivel - 1).nombre,
                                    con.Muros.Length, con.LongitudOptima));

            Guardar(escena, "Ambiente2_Laberinto", 2);
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
                                                   Vector3.one * 0.2f, matD, false);
                go.AddComponent<HapticShape>();

                // El cilindro primitivo trae CapsuleCollider, que para un disco
                // aplastado sobresale por debajo y lo dejaria flotando sobre la
                // mesa durante la caida. Una caja del tamano del disco se ajusta
                // mucho mejor. Solo importa mientras cae: el resto del tiempo el
                // disco es cinematico y su posicion se fija a mano.
                go.AddComponent<BoxCollider>();

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

            DejarEnNivelInicial(discos, torres[0], amb.nivel, amb.discosElegidos);

            Guardar(escena, "Ambiente4_Hanoi", 4);
        }

        /// <summary>Deja los discos como se veran al empezar: los del nivel por
        /// defecto apilados en la torre 1 con su tamano real, y los que sobran
        /// desactivados. En Play lo rehace Ambiente4Hanoi.Start(), pero la escena
        /// guardada tambien tiene que ser presentable: es la que se abre en el
        /// editor y la que fotografia CapturarAmbientes, y si no, saldria con los
        /// siete discos del tamano de plantilla amontonados junto al primer poste.
        /// Se calcula con las mismas clases que usa el juego, no con constantes
        /// repetidas aqui.</summary>
        private static void DejarEnNivelInicial(List<DiscoHanoi> discos, TorreHanoi torre,
                                                int nivel, int cuantos)
        {
            var cfg = ConfiguracionHanoi.Cargar();
            cuantos = Mathf.Clamp(cuantos, EstadoHanoi.MinDiscos, EstadoHanoi.MaxDiscos);
            var estado = new EstadoHanoi(cuantos);

            for (int i = 0; i < discos.Count; i++)
            {
                bool enJuego = i < cuantos;
                discos[i].gameObject.SetActive(enJuego);
                if (!enJuego) continue;

                discos[i].Configurar(i, cfg.MasaDe(i, cuantos), cfg.RadioDe(i, cuantos),
                                     cfg.retardoPorKg, cfg.rigidezDisco, cfg.friccionDisco);
                discos[i].transform.SetPositionAndRotation(
                    torre.PosicionDisco(estado.NivelDe(i)), Quaternion.identity);
            }
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
