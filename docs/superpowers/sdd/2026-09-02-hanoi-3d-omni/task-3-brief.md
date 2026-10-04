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

