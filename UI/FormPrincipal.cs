using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NolipateSimulator.Core;
using NOPILATE.Core;

namespace NOPILATE
{
    public partial class FormPrincipal : Form
    {
        // Controles de las 4 zonas (Sección 7)
        private TextBox txtTablero;         // Zona 1: Tablero
        private TextBox txtBitacora;        // Zona 2: Bitácora
        private TextBox txtEstado;          // Zona 3: Estado y retroalimentación
        private TextBox txtEntrada;         // Zona 4: Caja de captura
        private Label lblPrompt;

        // Botones de control (Fila 1)
        private Button btnValidar;
        private Button btnAyuda;
        private Button btnDeshacer;
        private Button btnExportar;
        private Button btnNueva;

        // Botones de pruebas controladas (Fila 2)
        private Button btnForzar5;
        private Button btnForzar6;
        private Button btnTest3x6;
        private Button btnBarrera;
        private Button btnTestPasillo;
        private Button btnTestMeta;
        private Button btnAuditoria;

        // Módulos del núcleo
        private M5_Estado _estado;
        private M0_Dado _dado;
        private string _ultimoMensaje = "OK: Partida iniciada. Ingrese una jugada legal.";

        public FormPrincipal()
        {
            // Activar doble búfer para evitar parpadeos al redimensionar
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw, true);

            InicializarComponentesPersonalizados();
            IniciarPartidaLimpia();
        }

        private void IniciarPartidaLimpia()
        {
            var dlg = MessageBox.Show("¿Desea iniciar con 4 jugadores (A, Z, R, V)?\nSeleccione 'No' para 2 jugadores (A, Z).",
                                      "Configuración de Partida", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            var jugadores = (dlg == DialogResult.Yes) ? new[] { 'A', 'Z', 'R', 'V' } : new[] { 'A', 'Z' };

            _estado = new M5_Estado(jugadores);
            _dado = new M0_Dado();
            _dado.Lanzar(_estado);

            _ultimoMensaje = $"OK: Partida con {jugadores.Length} jugadores ({string.Join("-", jugadores)}). Fichas en casa.";
            RefrescarInterfaz();
        }

        private void RefrescarInterfaz()
        {
            txtTablero.Text = M6_Salida.GenerarVistaTablero(_estado);

            txtBitacora.Text = "[ BITÁCORA DE LA PARTIDA ]" + Environment.NewLine + Environment.NewLine +
                               string.Join(Environment.NewLine, _estado.Bitacora);

            txtEstado.Text = M6_Salida.GenerarVistaEstado(_estado, _ultimoMensaje);
            lblPrompt.Text = $"{_estado.ColorActivo} >";
            txtEntrada.Clear();
            txtEntrada.Focus();
        }

        private void InicializarComponentesPersonalizados()
        {
            this.Text = "NOLIPATE - Notación Libre de Parchís por Texto";
            this.BackColor = Color.FromArgb(24, 24, 37);
            this.ForeColor = Color.White;
            Font monoFont = new Font("Consolas", 10.5f, FontStyle.Regular);

            // Ajustar el tamaño inicial relativo al área de trabajo del monitor (pantalla)
            Rectangle areaMonitor = Screen.PrimaryScreen.WorkingArea;
            int anchoInicial = Math.Min(1200, (int)(areaMonitor.Width * 0.92));
            int altoInicial = Math.Min(730, (int)(areaMonitor.Height * 0.90));
            this.Size = new Size(Math.Max(1020, anchoInicial), Math.Max(620, altoInicial));
            this.MinimumSize = new Size(980, 580);
            this.StartPosition = FormStartPosition.CenterScreen;

            // ZONA 1: Tablero
            txtTablero = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Font = monoFont,
                BackColor = Color.FromArgb(17, 17, 27),
                ForeColor = Color.FromArgb(137, 180, 250),
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = ScrollBars.Vertical
            };

            // ZONA 2: Bitácora
            txtBitacora = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Font = monoFont,
                BackColor = Color.FromArgb(17, 17, 27),
                ForeColor = Color.FromArgb(166, 227, 161),
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = ScrollBars.Vertical
            };

            // ZONA 3: Estado y Retroalimentación
            txtEstado = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Font = monoFont,
                BackColor = Color.FromArgb(17, 17, 27),
                ForeColor = Color.FromArgb(249, 226, 175),
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = ScrollBars.Vertical
            };

            // ZONA 4: Entrada y Controles
            lblPrompt = new Label
            {
                Font = new Font("Consolas", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(243, 139, 168),
                TextAlign = ContentAlignment.MiddleRight
            };

            txtEntrada = new TextBox
            {
                Font = new Font("Consolas", 12f, FontStyle.Bold),
                BackColor = Color.FromArgb(30, 30, 46),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtEntrada.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) EjecutarValidacion(); };

            // Instanciación de Botones - Fila 1
            btnValidar = CrearBoton("Validar", Point.Empty, (s, e) => EjecutarValidacion());
            btnAyuda = CrearBoton("Ayuda", Point.Empty, (s, e) => MostrarAyuda());
            btnDeshacer = CrearBoton("Deshacer", Point.Empty, (s, e) => EjecutarDeshacer());
            btnExportar = CrearBoton("Exportar", Point.Empty, (s, e) => ExportarBitacora());
            btnNueva = CrearBoton("Nueva", Point.Empty, (s, e) => IniciarPartidaLimpia());

            // Instanciación de Botones - Fila 2
            btnForzar5 = CrearBoton("Dado 5", Point.Empty, (s, e) => ForzarDadoValor(5));
            btnForzar6 = CrearBoton("Dado 6", Point.Empty, (s, e) => ForzarDadoValor(6));
            btnTest3x6 = CrearBoton("3 Seises", Point.Empty, (s, e) => PrepararEscenarioTresSeises());
            btnBarrera = CrearBoton("Barrera", Point.Empty, (s, e) => PrepararEscenarioBloqueoBarrera());
            btnTestPasillo = CrearBoton("Pasillo", Point.Empty, (s, e) => PrepararEscenarioPasillo());
            btnTestMeta = CrearBoton("Victoria", Point.Empty, (s, e) => PrepararEscenarioMeta());
            btnAuditoria = CrearBoton("Auditoría", Point.Empty, (s, e) => EjecutarAuditoriaSeccion16());

            // Agregar a los controles del formulario
            this.Controls.Add(txtTablero);
            this.Controls.Add(txtBitacora);
            this.Controls.Add(txtEstado);
            this.Controls.Add(lblPrompt);
            this.Controls.Add(txtEntrada);

            this.Controls.Add(btnValidar);
            this.Controls.Add(btnAyuda);
            this.Controls.Add(btnDeshacer);
            this.Controls.Add(btnExportar);
            this.Controls.Add(btnNueva);

            this.Controls.Add(btnForzar5);
            this.Controls.Add(btnForzar6);
            this.Controls.Add(btnTest3x6);
            this.Controls.Add(btnBarrera);
            this.Controls.Add(btnTestPasillo);
            this.Controls.Add(btnTestMeta);
            this.Controls.Add(btnAuditoria);

            // Reajustar dinámicamente cuando cambie el tamaño de la ventana
            this.Resize += (s, e) => AjustarLayout();
            AjustarLayout();
        }

        // =========================================================================
        // MOTOR DE ADAPTACIÓN RESPONSIVA AL TAMAÑO DE PANTALLA Y VENTANA
        // =========================================================================
        private void AjustarLayout()
        {
            if (txtTablero == null || txtBitacora == null || txtEstado == null || txtEntrada == null)
                return;

            int pad = 15;
            int anchoCliente = this.ClientSize.Width;
            int altoCliente = this.ClientSize.Height;

            if (anchoCliente < 600 || altoCliente < 400) return;

            // Espacio vertical para entrada y las 2 filas de botones
            int altoZonaInferior = 96;
            int altoDisponibleSuperior = altoCliente - (pad * 4) - altoZonaInferior;

            // 65% para tableros, 35% para estado
            int altoTableros = (int)(altoDisponibleSuperior * 0.65);
            int altoEstado = altoDisponibleSuperior - altoTableros;

            if (altoEstado < 120)
            {
                altoEstado = 120;
                altoTableros = Math.Max(120, altoDisponibleSuperior - altoEstado);
            }

            // 1. ZONA 1 (Tablero) y ZONA 2 (Bitácora): Cada una al 50% exacto
            int anchoDisponibleColumnas = anchoCliente - (pad * 3);
            int anchoColumna = anchoDisponibleColumnas / 2;

            txtTablero.Location = new Point(pad, pad);
            txtTablero.Size = new Size(anchoColumna, altoTableros);

            txtBitacora.Location = new Point(pad * 2 + anchoColumna, pad);
            txtBitacora.Size = new Size(anchoCliente - pad - (pad * 2 + anchoColumna), altoTableros);

            // 2. ZONA 3 (Estado): 100% del ancho
            int yEstado = txtTablero.Bottom + pad;
            txtEstado.Location = new Point(pad, yEstado);
            txtEstado.Size = new Size(anchoCliente - (pad * 2), altoEstado);

            // 3. ZONA 4: Entrada y filas de botones
            int yFila1 = txtEstado.Bottom + 12;
            int yFila2 = yFila1 + 42;
            int altoBoton = 34;

            Button[] fila1 = { btnValidar, btnAyuda, btnDeshacer, btnExportar, btnNueva };
            Button[] fila2 = { btnForzar5, btnForzar6, btnTest3x6, btnBarrera, btnTestPasillo, btnTestMeta, btnAuditoria };

            int anchoBtn = 86;
            int gap = 8;
            int totalAnchoFila2 = (fila2.Length * anchoBtn) + ((fila2.Length - 1) * gap);

            // Bloque de botones alineado a la derecha
            int xInicioBotones = Math.Max(pad + 380, anchoCliente - pad - totalAnchoFila2);

            // Fila 1 de botones
            int xF1 = xInicioBotones;
            foreach (var b in fila1)
            {
                b.Location = new Point(xF1, yFila1);
                b.Size = new Size(anchoBtn, altoBoton);
                xF1 += anchoBtn + gap;
            }

            // Fila 2 de botones
            int xF2 = xInicioBotones;
            foreach (var b in fila2)
            {
                b.Location = new Point(xF2, yFila2);
                b.Size = new Size(anchoBtn, altoBoton);
                xF2 += anchoBtn + gap;
            }

            // Prompt y Caja de entrada (se estiran adaptándose al espacio libre)
            lblPrompt.Location = new Point(pad, yFila1 + 2);
            lblPrompt.Size = new Size(50, 30);

            int xEntrada = lblPrompt.Right + 6;
            int anchoEntrada = Math.Max(180, (xInicioBotones - 15) - xEntrada);
            txtEntrada.Location = new Point(xEntrada, yFila1);
            txtEntrada.Size = new Size(anchoEntrada, 30);
        }

        private Button CrearBoton(string texto, Point ubicacion, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = texto,
                Location = ubicacion,
                Size = new Size(86, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(49, 50, 68),
                ForeColor = Color.White,
                Font = new Font("Consolas", 9.5f)
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(69, 71, 90);
            btn.Click += onClick;
            return btn;
        }

        private void EjecutarValidacion()
        {
            string entrada = txtEntrada.Text.Trim();
            if (string.IsNullOrEmpty(entrada)) return;

            if (_estado.Ganador.HasValue)
            {
                _ultimoMensaje = $"ERR {entrada} -> Error semántico: la partida ha finalizado. Ganador: {_estado.Ganador.Value} (#{_estado.Ganador.Value}). No se admiten más jugadas.";
                RefrescarInterfaz();
                return;
            }

            // FASE 1: LÉXICO (M2)
            var lexico = new M2_Lexico(entrada);
            var resLexico = lexico.Analizar();
            if (!resLexico.EsValido)
            {
                _ultimoMensaje = $"ERR {entrada} -> {resLexico.MensajeError}";
                RefrescarInterfaz();
                return;
            }

            // FASE 2: SINTÁCTICO LL(1) (M3)
            var sintactico = new M3_Sintactico(resLexico.Tokens);
            var resSintactico = sintactico.AnalizarTirada();
            if (!resSintactico.EsValido)
            {
                _ultimoMensaje = $"ERR {entrada} -> {resSintactico.MensajeError}";
                RefrescarInterfaz();
                return;
            }

            _estado.PilaDeshacer.Push(_estado.Clonar());

            char colorTurnoActual = _estado.ColorActivo;
            int rondaTurnoActual = _estado.RondaActual;

            // FASE 3: SEMÁNTICO (M4)
            var resSemantico = M4_Semantico.ValidarYAplicar(resSintactico.Arbol, _estado, _dado);

            if (!resSemantico.EsValido)
            {
                _estado = _estado.PilaDeshacer.Pop();
                _ultimoMensaje = $"ERR {entrada} -> {resSemantico.Mensaje}";
                RefrescarInterfaz();
                return;
            }

            string prefijoRonda = $"{rondaTurnoActual}.";
            string jugadaFormato = $"{colorTurnoActual} {resSemantico.JugadaRegistrada}";

            if (_estado.Bitacora.Count > 0 && _estado.Bitacora.Last().StartsWith(prefijoRonda))
            {
                _estado.Bitacora[_estado.Bitacora.Count - 1] += $"  {jugadaFormato}";
            }
            else
            {
                _estado.Bitacora.Add($"{prefijoRonda} {jugadaFormato}");
            }

            if (_estado.Ganador.HasValue)
            {
                txtEntrada.Enabled = false;
                btnValidar.Enabled = false;
            }

            _ultimoMensaje = resSemantico.Mensaje;
            RefrescarInterfaz();
        }

        private void ForzarDadoValor(int valor)
        {
            if (_estado.BonoPendiente.HasValue)
            {
                _ultimoMensaje = "ERR: Hay un bono pendiente. Debe resolverlo antes de forzar un dado.";
                RefrescarInterfaz();
                return;
            }

            _dado.Forzar(valor);
            _dado.Lanzar(_estado);
            _ultimoMensaje = $"Prueba controlada: dado forzado a d{valor} para el jugador {_estado.ColorActivo}.";
            RefrescarInterfaz();
        }

        private void PrepararEscenarioTresSeises()
        {
            _estado = new M5_Estado(new[] { 'A', 'Z' });
            _dado = new M0_Dado();

            _estado.Fichas["A1"] = "20";
            _estado.Fichas["A2"] = "40";

            _estado.SeisesConsecutivos = 3;
            _estado.DadoPublicado = 6;

            _ultimoMensaje = "Escenario preparado: Amarillo (A) tiene 3 seises consecutivos.\nFicha más adelantada: A2 en casilla 40. Escriba 'd6 A2 40-c'.";
            RefrescarInterfaz();
        }

        private void EjecutarDeshacer()
        {
            if (_estado.PilaDeshacer.Count > 0)
            {
                var anterior = _estado.PilaDeshacer.Pop();
                _dado.Restaurar(anterior.DadoPublicado);
                _estado = anterior;
                _ultimoMensaje = "OK: Última jugada deshecha. Se restauró el tablero y el recurso publicado.";
                RefrescarInterfaz();
            }
            else
            {
                _ultimoMensaje = "ERR: No hay acciones previas en la pila de deshacer.";
                RefrescarInterfaz();
            }
        }

        private void MostrarAyuda()
        {
            string textoAyuda =
                "=== GUÍA RÁPIDA DE COMANDOS Y SINTAXIS NOLIPATE ===\n\n" +
                "Estructura general:\n" +
                "  [Recurso] [Ficha] [Origen][Operador][Destino][Anotación]\n\n" +
                "1. Salir de casa (requiere dado d5):\n" +
                "   d5 A1 c-5    |  d5 Z1 c-22  |  d5 R1 c-39  |  d5 V1 c-56\n\n" +
                "2. Avance regular en el circuito:\n" +
                "   d4 A1 5-9\n\n" +
                "3. Captura de ficha rival:\n" +
                "   d6 R2 39x45  (genera bono b20 prioritario)\n\n" +
                "4. Entrada al pasillo propio:\n" +
                "   d4 A1 66-p2  (Amarillo entra en 68, Azul en 17, Rojo en 34, Verde en 51)\n\n" +
                "5. Llegada exacta a meta:\n" +
                "   d3 V1 p5=m   (genera bono b10 si quedan fichas en juego)\n\n" +
                "6. Formación de barrera (con anotación '!'):\n" +
                "   d2 A1 12-14!\n\n" +
                "7. Consumo de bonos:\n" +
                "   b20 R2 45-65  o  b10 A1 p2-p4\n\n" +
                "8. Paso forzado (* cuando no hay movimientos posibles):\n" +
                "   d4*  o  b20*\n\n" +
                "9. Penalización por tercer 6 (Regla 10):\n" +
                "   d6 A2 40-c   (la ficha más adelantada regresa a casa)";

            MessageBox.Show(textoAyuda, "Ayuda de Notación NOLIPATE", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExportarBitacora()
        {
            try
            {
                string ruta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bitacora_nolipate.txt");
                File.WriteAllLines(ruta, _estado.Bitacora);
                MessageBox.Show($"Bitácora exportada correctamente en:\n{ruta}", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar bitácora: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrepararEscenarioMeta()
        {
            _estado = new M5_Estado(new[] { 'A', 'Z' });
            _dado = new M0_Dado();

            _estado.Fichas["A1"] = "m";
            _estado.Fichas["A2"] = "m";
            _estado.Fichas["A3"] = "m";
            _estado.Fichas["A4"] = "p5";

            _dado.Forzar(3);
            _dado.Lanzar(_estado);

            _ultimoMensaje = "Escenario Victoria preparado: A1, A2, A3 en meta. A4 en p5.\nDado d3 publicado. Llegada exacta: escriba 'd3 A4 p5=m'.";
            RefrescarInterfaz();
        }

        private void PrepararEscenarioBloqueoBarrera()
        {
            _estado = new M5_Estado(new[] { 'A', 'Z' });
            _dado = new M0_Dado();

            _estado.Fichas["A1"] = "8";
            _estado.Fichas["A2"] = "c";
            _estado.Fichas["A3"] = "c";
            _estado.Fichas["A4"] = "c";

            _estado.Fichas["Z1"] = "14";
            _estado.Fichas["Z2"] = "14";

            _dado.Forzar(6);
            _dado.Lanzar(_estado);

            _ultimoMensaje = "Escenario Barrera preparado: A1 en 8. Barrera rival en 14 (Z1, Z2).\nDado d6 publicado. A1 caería justo en la barrera (ilegal). Pruebe 'd6*'.";
            RefrescarInterfaz();
        }

        private void EjecutarAuditoriaSeccion16()
        {
            var reporte = new System.Text.StringBuilder();
            reporte.AppendLine("AUDITORÍA DE PRUEBAS COMPLETAS (SECCIÓN 16 - PÁG. 32):\n");

            // 1. Salida legal
            var est1 = new M5_Estado(new[] { 'A', 'Z' });
            var dado1 = new M0_Dado();
            dado1.Forzar(5); dado1.Lanzar(est1);
            var sin1 = new M3_Sintactico(new M2_Lexico("d5 A3 c-5").Analizar().Tokens).AnalizarTirada();
            var sem1 = M4_Semantico.ValidarYAplicar(sin1.Arbol, est1, dado1);
            reporte.AppendLine($"1. 'd5 A3 c-5' (Dado 5, salida libre) -> {(sem1.EsValido ? "OK (Salida aceptada)" : "FALLÓ")}");

            // 2. Captura y bonos
            var est2 = new M5_Estado(new[] { 'R', 'V' });
            var dado2 = new M0_Dado();
            est2.Fichas["R2"] = "39";
            est2.Fichas["V1"] = "45";
            dado2.Forzar(6); dado2.Lanzar(est2);
            var sin2 = new M3_Sintactico(new M2_Lexico("d6 R2 39x45").Analizar().Tokens).AnalizarTirada();
            var sem2 = M4_Semantico.ValidarYAplicar(sin2.Arbol, est2, dado2);
            bool capturaOk = sem2.EsValido && est2.BonoPendiente == 20 && est2.Fichas["V1"] == "c";
            reporte.AppendLine($"2. 'd6 R2 39x45' (Captura con rival en 45) -> {(capturaOk ? "OK (Captura, rival a casa, b20 pendiente)" : "FALLÓ")}");

            // 3. Error Léxico
            var lex3 = new M2_Lexico("d7 R2 39-46").Analizar();
            reporte.AppendLine($"3. 'd7 R2 39-46' (Dado fuera de notación) -> {(!lex3.EsValido ? "OK (Error Léxico detectado)" : "FALLÓ")}");

            // 4. Error Sintáctico
            var lex4 = new M2_Lexico("d6 R2 39-").Analizar();
            var sin4 = new M3_Sintactico(lex4.Tokens).AnalizarTirada();
            reporte.AppendLine($"4. 'd6 R2 39-' (Falta destino) -> {(!sin4.EsValido ? "OK (Error Sintáctico detectado)" : "FALLÓ")}");

            // 5. Error Semántico
            var est5 = new M5_Estado(new[] { 'A', 'Z' });
            var dado5 = new M0_Dado();
            dado5.Forzar(3); dado5.Lanzar(est5);
            var sin5 = new M3_Sintactico(new M2_Lexico("d3 A3 c-5").Analizar().Tokens).AnalizarTirada();
            var sem5 = M4_Semantico.ValidarYAplicar(sin5.Arbol, est5, dado5);
            reporte.AppendLine($"5. 'd3 A3 c-5' (Dado 3 en salida de casa) -> {(!sem5.EsValido ? "OK (Error Semántico detectado)" : "FALLÓ")}");

            // 6. Consumo de Bono
            var est6 = new M5_Estado(new[] { 'A', 'Z' });
            var dado6 = new M0_Dado();
            est6.BonoPendiente = 20;
            var sin6 = new M3_Sintactico(new M2_Lexico("b20*").Analizar().Tokens).AnalizarTirada();
            var sem6 = M4_Semantico.ValidarYAplicar(sin6.Arbol, est6, dado6);
            reporte.AppendLine($"6. 'b20*' (Bono sin jugadas posibles) -> {(sem6.EsValido && est6.BonoPendiente == null ? "OK (Bono consumido)" : "FALLÓ")}");

            MessageBox.Show(reporte.ToString(), "Auditoría Completa - Sección 16", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void PrepararEscenarioPasillo()
        {
            _estado = new M5_Estado(new[] { 'A', 'Z' });
            _dado = new M0_Dado();

            _estado.Fichas["A1"] = "66";
            _estado.Fichas["A2"] = "c";
            _estado.Fichas["A3"] = "c";
            _estado.Fichas["A4"] = "c";

            _dado.Forzar(4);
            _dado.Lanzar(_estado);

            _ultimoMensaje = "Escenario Pasillo: A1 en casilla 66. Dado d4 publicado.\n" +
                             "Ruta esperada: 66 -> 67 -> 68 -> p1 -> p2.\n" +
                             "Pruebe jugada errónea 'd4 A1 66-2' o la correcta 'd4 A1 66-p2'.";
            RefrescarInterfaz();
        }
    }
}