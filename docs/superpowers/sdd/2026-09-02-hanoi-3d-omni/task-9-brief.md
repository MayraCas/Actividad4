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

