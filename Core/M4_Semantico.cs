using NOPILATE.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NolipateSimulator.Core
{
    public class ResultadoSemantico
    {
        public bool EsValido { get; set; }
        public string Mensaje { get; set; }
        public string JugadaRegistrada { get; set; }
    }

    public class M4_Semantico
    {
        public static List<string> ObtenerRutaCompleta(char color)
        {
            var ruta = new List<string>();
            int salida = Constantes.CasillasSalida[color];
            int finCircuito = Constantes.EntradasPasillo[color];

            int actual = salida;
            while (true)
            {
                ruta.Add(actual.ToString());
                if (actual == finCircuito) break;
                actual = (actual % 68) + 1;
            }

            for (int i = 1; i <= 7; i++)
                ruta.Add($"p{i}");
            ruta.Add("m");

            return ruta;
        }

        public static string ObtenerFichaMasAdelantada(char color, M5_Estado estado)
        {
            var ruta = ObtenerRutaCompleta(color);

            var candidatas = estado.Fichas
                .Where(kvp => kvp.Key.StartsWith(color.ToString()) && kvp.Value != "c" && kvp.Value != "m")
                .Select(kvp => new
                {
                    Ficha = kvp.Key,
                    Posicion = kvp.Value,
                    Progreso = ruta.IndexOf(kvp.Value),
                    IndiceFicha = int.Parse(kvp.Key.Substring(1))
                })
                .Where(x => x.Progreso >= 0)
                .OrderByDescending(x => x.Progreso)
                .ThenBy(x => x.IndiceFicha)
                .FirstOrDefault();

            return candidatas?.Ficha;
        }

        // REGLA 11: Comprobación estricta antes de autorizar Paso (*)
        public static bool TieneMovimientosLegales(char color, int valorRecurso, M5_Estado estado, out string sugerenciaJugada)
        {
            sugerenciaJugada = string.Empty;
            var ruta = ObtenerRutaCompleta(color);
            var fichasPropias = estado.Fichas.Where(f => f.Key.StartsWith(color.ToString())).ToList();

            foreach (var f in fichasPropias)
            {
                string fichaId = f.Key;
                string posActual = f.Value;

                if (posActual == "m") continue;

                // Caso 1: Salida de casa (requiere 5 y salida libre de rivales)
                if (posActual == "c")
                {
                    if (valorRecurso == 5)
                    {
                        string casillaSalida = Constantes.CasillasSalida[color].ToString();
                        var ocupantesSalida = estado.ObtenerOcupacion(casillaSalida);
                        int ocupantesPropios = ocupantesSalida.Count(x => x[0] == color);
                        bool rivalEnSalida = ocupantesSalida.Any(x => x[0] != color);

                        if (ocupantesPropios < 2 && !rivalEnSalida)
                        {
                            sugerenciaJugada = $"{fichaId} c-{casillaSalida}";
                            return true;
                        }
                    }
                    continue;
                }

                // Caso 2: Avance por la ruta
                int idxOrigen = ruta.IndexOf(posActual);
                if (idxOrigen == -1) continue;

                int idxDestino = idxOrigen + valorRecurso;
                if (idxDestino >= ruta.Count) continue; // Excede meta

                string destino = ruta[idxDestino];

                // Barreras intermedias solo en circuito
                bool barreraEnCamino = false;
                for (int step = idxOrigen + 1; step < idxDestino; step++)
                {
                    string posIntermedia = ruta[step];
                    if (!posIntermedia.StartsWith("p") && posIntermedia != "m")
                    {
                        var ocup = estado.ObtenerOcupacion(posIntermedia);
                        if (ocup.Count == 2 && ocup[0][0] == ocup[1][0])
                        {
                            barreraEnCamino = true;
                            break;
                        }
                    }
                }
                if (barreraEnCamino) continue;

                if (destino == "m")
                {
                    sugerenciaJugada = $"{fichaId} {posActual}=m";
                    return true;
                }

                var ocupDestino = estado.ObtenerOcupacion(destino);
                if (!destino.StartsWith("p") && ocupDestino.Count == 2 && ocupDestino[0][0] == ocupDestino[1][0])
                    continue;

                var rival = ocupDestino.FirstOrDefault(x => x[0] != color);
                if (rival != null)
                {
                    int.TryParse(destino, out int numCasilla);
                    if (!Constantes.CasillasSeguras.Contains(numCasilla))
                    {
                        sugerenciaJugada = $"{fichaId} {posActual}x{destino}";
                        return true;
                    }
                }
                else
                {
                    int propias = ocupDestino.Count(x => x[0] == color);
                    if (propias < 2)
                    {
                        sugerenciaJugada = $"{fichaId} {posActual}-{destino}";
                        return true;
                    }
                }
            }

            return false;
        }

        public static ResultadoSemantico ValidarYAplicar(NodoTirada tirada, M5_Estado estado, M0_Dado dado)
        {
            var res = new ResultadoSemantico();

            // 1. Partida finalizada (Regla 12)
            if (estado.Ganador.HasValue)
            {
                res.EsValido = false;
                res.Mensaje = $"Error semántico: la partida ha finalizado. Ganador: {estado.Ganador.Value} (#{estado.Ganador.Value}). No se admiten más jugadas.";
                return res;
            }

            // 2. Penalización por tres seises consecutivos (Regla 10)
            if (estado.SeisesConsecutivos == 3)
            {
                if (tirada.Recurso.Lexema != "d6")
                {
                    res.EsValido = false;
                    res.Mensaje = $"Error semántico: se requiere aplicar el tercer 6 ('d6'), se introdujo '{tirada.Recurso.Lexema}'.";
                    return res;
                }

                string fichaPenalizable = ObtenerFichaMasAdelantada(estado.ColorActivo, estado);

                if (fichaPenalizable != null)
                {
                    string posReal = estado.Fichas[fichaPenalizable];

                    if (tirada.EsPaso)
                    {
                        res.EsValido = false;
                        res.Mensaje = $"Error semántico: al tercer 6 debe penalizarse la ficha más adelantada ({fichaPenalizable} en '{posReal}'). Escriba 'd6 {fichaPenalizable} {posReal}-c'.";
                        return res;
                    }

                    if (tirada.Ficha.Lexema != fichaPenalizable ||
                        tirada.Origen.Lexema != posReal ||
                        tirada.Operador.Lexema != "-" ||
                        tirada.Destino.Lexema != "c")
                    {
                        res.EsValido = false;
                        res.Mensaje = $"Error semántico: penalización por tercer 6. La ficha más adelantada es {fichaPenalizable} en '{posReal}'. Formato obligatorio: 'd6 {fichaPenalizable} {posReal}-c'.";
                        return res;
                    }

                    estado.Fichas[fichaPenalizable] = "c";
                    estado.DadoPublicado = null;
                    estado.SeisesConsecutivos = 0;
                    estado.TiradaExtra = false;

                    res.EsValido = true;
                    res.JugadaRegistrada = $"d6 {fichaPenalizable} {posReal}-c";
                    res.Mensaje = $"OK: Penalización aplicada (Regla 10). {fichaPenalizable} regresa a casa desde {posReal}. Fin del turno.";

                    CambiarTurno(estado, dado);
                    return res;
                }
                else
                {
                    if (!tirada.EsPaso)
                    {
                        res.EsValido = false;
                        res.Mensaje = "Error semántico: penalización por tercer 6 sin fichas en juego. Debe registrar paso: 'd6*'.";
                        return res;
                    }

                    estado.DadoPublicado = null;
                    estado.SeisesConsecutivos = 0;
                    estado.TiradaExtra = false;

                    res.EsValido = true;
                    res.JugadaRegistrada = "d6*";
                    res.Mensaje = "OK: Penalización por 3 seises (Regla 10). Sin fichas penalizables en juego. Paso registrado (*). Fin del turno.";

                    CambiarTurno(estado, dado);
                    return res;
                }
            }

            // 3. Validación de recurso publicado
            int valorRecurso = 0;
            bool esBono = tirada.Recurso.Tipo == TipoToken.BONO;

            if (esBono)
            {
                int valBono = int.Parse(tirada.Recurso.Lexema.Substring(1));
                if (estado.BonoPendiente != valBono)
                {
                    res.EsValido = false;
                    res.Mensaje = $"Error semántico: se esperaba bono {(estado.BonoPendiente.HasValue ? "b" + estado.BonoPendiente.Value : "ninguno")}, pero se introdujo '{tirada.Recurso.Lexema}'.";
                    return res;
                }
                valorRecurso = valBono;
            }
            else
            {
                if (estado.BonoPendiente.HasValue)
                {
                    res.EsValido = false;
                    res.Mensaje = $"Error semántico: debe resolver primero el bono pendiente b{estado.BonoPendiente.Value} antes de usar un dado.";
                    return res;
                }

                int valDado = int.Parse(tirada.Recurso.Lexema.Substring(1));
                if (estado.DadoPublicado != valDado)
                {
                    res.EsValido = false;
                    res.Mensaje = $"Error semántico: el dado publicado es d{estado.DadoPublicado}, pero la jugada declara 'd{valDado}'.";
                    return res;
                }
                valorRecurso = valDado;
            }

            // 4. Paso estricto (*) (Regla 11)
            if (tirada.EsPaso)
            {
                if (TieneMovimientosLegales(estado.ColorActivo, valorRecurso, estado, out string sugerencia))
                {
                    res.EsValido = false;
                    res.Mensaje = $"Error semántico (Regla 11): no se permite pasar (*). Existen jugadas legales con {tirada.Recurso.Lexema} (ejemplo: {tirada.Recurso.Lexema} {sugerencia}).";
                    return res;
                }

                if (esBono) estado.BonoPendiente = null;
                else estado.DadoPublicado = null;

                res.EsValido = true;
                res.JugadaRegistrada = $"{tirada.Recurso.Lexema}*";
                res.Mensaje = $"Paso aceptado (*) (Regla 11): recurso consumido.";

                AvanzarTurnoORecurso(estado, dado, tirada.Recurso.Lexema == "d6");
                return res;
            }

            // 5. Pertenencia de la ficha
            string ficha = tirada.Ficha.Lexema;
            if (ficha[0] != estado.ColorActivo)
            {
                res.EsValido = false;
                res.Mensaje = $"Error semántico: la ficha {ficha} no pertenece al jugador activo ({estado.ColorActivo}).";
                return res;
            }

            // 6. Origen real de la ficha
            string origenReal = estado.Fichas[ficha];
            if (origenReal != tirada.Origen.Lexema)
            {
                res.EsValido = false;
                res.Mensaje = $"Error semántico: la ficha {ficha} está en '{origenReal}', pero se indicó origen '{tirada.Origen.Lexema}'.";
                return res;
            }

            // 7. Salida de casa (Regla 2 y Regla 6)
            if (origenReal == "c")
            {
                if (valorRecurso != 5)
                {
                    res.EsValido = false;
                    res.Mensaje = $"Error semántico: salir de casa exige un dado 5; recurso actual: {valorRecurso}.";
                    return res;
                }

                string salidaEsperada = Constantes.CasillasSalida[estado.ColorActivo].ToString();
                if (tirada.Destino.Lexema != salidaEsperada)
                {
                    res.EsValido = false;
                    res.Mensaje = $"Error semántico: la casilla de salida para {estado.ColorActivo} es {salidaEsperada}, no '{tirada.Destino.Lexema}'.";
                    return res;
                }

                var ocupantesSalida = estado.ObtenerOcupacion(salidaEsperada);
                bool rivalEnSalida = ocupantesSalida.Any(f => f[0] != estado.ColorActivo);
                if (rivalEnSalida)
                {
                    res.EsValido = false;
                    res.Mensaje = $"Error semántico: la casilla de salida {salidaEsperada} es segura y está ocupada por un rival; no se permite salir.";
                    return res;
                }

                int propiasEnSalida = ocupantesSalida.Count(f => f[0] == estado.ColorActivo);
                if (propiasEnSalida >= 2)
                {
                    res.EsValido = false;
                    res.Mensaje = $"Error semántico: la casilla de salida {salidaEsperada} ya tiene dos fichas propias.";
                    return res;
                }

                estado.Fichas[ficha] = salidaEsperada;
                CompletarJugada(estado, dado, tirada, esBono, false, false, res);
                return res;
            }

            // 8. Avance sobre la ruta
            var rutaNormal = ObtenerRutaCompleta(estado.ColorActivo);
            int idxOrigen = rutaNormal.IndexOf(origenReal);
            if (idxOrigen == -1)
            {
                res.EsValido = false;
                res.Mensaje = $"Error semántico: la posición '{origenReal}' no es válida en la ruta de {estado.ColorActivo}.";
                return res;
            }

            int idxDestinoCalculado = idxOrigen + valorRecurso;
            if (idxDestinoCalculado >= rutaNormal.Count)
            {
                res.EsValido = false;
                res.Mensaje = $"Error semántico: el recurso ({valorRecurso}) excede la llegada exacta a meta.";
                return res;
            }

            string destinoCalculado = rutaNormal[idxDestinoCalculado];
            if (destinoCalculado != tirada.Destino.Lexema)
            {
                res.EsValido = false;
                res.Mensaje = $"Error semántico: avance incorrecto. Con {valorRecurso} pasos desde '{origenReal}', el destino es '{destinoCalculado}', no '{tirada.Destino.Lexema}'.";
                return res;
            }

            // 9. Detección de barreras intermedias (solo en el circuito)
            for (int step = idxOrigen + 1; step < idxDestinoCalculado; step++)
            {
                string posIntermedia = rutaNormal[step];
                if (!posIntermedia.StartsWith("p") && posIntermedia != "m")
                {
                    var ocupantes = estado.ObtenerOcupacion(posIntermedia);
                    if (ocupantes.Count == 2 && ocupantes[0][0] == ocupantes[1][0])
                    {
                        res.EsValido = false;
                        res.Mensaje = $"Error semántico: barrera en casilla intermedia {posIntermedia} ({ocupantes[0]} y {ocupantes[1]}) bloquea el paso.";
                        return res;
                    }
                }
            }

            // 10. Destino y operadores
            var ocupantesDestino = estado.ObtenerOcupacion(destinoCalculado);
            bool esMeta = destinoCalculado == "m";
            bool esCaptura = false;
            string fichaCapturada = null;

            if (esMeta)
            {
                if (tirada.Operador.Lexema != "=")
                {
                    res.EsValido = false;
                    res.Mensaje = $"Error semántico: la llegada a meta debe usar el operador '=', no '{tirada.Operador.Lexema}'.";
                    return res;
                }
            }
            else
            {
                if (!destinoCalculado.StartsWith("p") && ocupantesDestino.Count == 2 && ocupantesDestino[0][0] == ocupantesDestino[1][0])
                {
                    res.EsValido = false;
                    res.Mensaje = $"Error semántico: la casilla {destinoCalculado} contiene una barrera; no se puede terminar allí.";
                    return res;
                }

                var rival = ocupantesDestino.FirstOrDefault(f => f[0] != estado.ColorActivo);
                if (rival != null)
                {
                    int.TryParse(destinoCalculado, out int numCasilla);
                    if (Constantes.CasillasSeguras.Contains(numCasilla))
                    {
                        res.EsValido = false;
                        res.Mensaje = $"Error semántico: la casilla {destinoCalculado} es un seguro; no se puede capturar ni compartir con rival.";
                        return res;
                    }

                    if (tirada.Operador.Lexema != "x")
                    {
                        res.EsValido = false;
                        res.Mensaje = $"Error semántico: la casilla {destinoCalculado} está ocupada por {rival}; se esperaba operador 'x'.";
                        return res;
                    }

                    esCaptura = true;
                    fichaCapturada = rival;
                }
                else
                {
                    if (tirada.Operador.Lexema == "x")
                    {
                        res.EsValido = false;
                        res.Mensaje = $"Error semántico: se indicó captura 'x', pero no hay rival en {destinoCalculado}.";
                        return res;
                    }

                    int propias = ocupantesDestino.Count(f => f[0] == estado.ColorActivo);
                    if (propias >= 2)
                    {
                        res.EsValido = false;
                        res.Mensaje = $"Error semántico: la casilla {destinoCalculado} ya tiene dos fichas propias.";
                        return res;
                    }
                }
            }

            // 11. Anotación de barrera '!' (Regla 7)
            if (tirada.AnotacionOpc != null && tirada.AnotacionOpc.Lexema == "!")
            {
                int propiasEnDestino = ocupantesDestino.Count(f => f[0] == estado.ColorActivo);
                if (propiasEnDestino != 1)
                {
                    res.EsValido = false;
                    res.Mensaje = $"Error semántico (Regla 7): se indicó anotación de barrera '!', pero la casilla {destinoCalculado} no quedará con dos fichas propias.";
                    return res;
                }
            }

            // Aplicar transición
            estado.Fichas[ficha] = destinoCalculado;

            if (esCaptura)
            {
                estado.Fichas[fichaCapturada] = "c";
                estado.BonoPendiente = 20;
            }
            else if (esMeta)
            {
                int fichasEnMeta = estado.Fichas.Count(f => f.Key.StartsWith(estado.ColorActivo.ToString()) && f.Value == "m");
                if (fichasEnMeta == 4)
                {
                    estado.Ganador = estado.ColorActivo;
                    estado.BonoPendiente = null;
                }
                else
                {
                    estado.BonoPendiente = 10;
                }
            }

            CompletarJugada(estado, dado, tirada, esBono, esCaptura, esMeta, res);
            return res;
        }

        private static void CompletarJugada(M5_Estado estado, M0_Dado dado, NodoTirada tirada, bool esBono, bool esCaptura, bool esMeta, ResultadoSemantico res)
        {
            res.EsValido = true;
            string anot = tirada.AnotacionOpc != null ? tirada.AnotacionOpc.Lexema : "";
            res.JugadaRegistrada = $"{tirada.Recurso.Lexema} {tirada.Ficha.Lexema} {tirada.Origen.Lexema}{tirada.Operador.Lexema}{tirada.Destino.Lexema}{anot}";

            bool obtuvoSeis = tirada.Recurso.Lexema == "d6";

            // Si fue un 6 y produjo bono (captura o meta), se difiere el tiro extra (Reglas 4 y 9)
            if (obtuvoSeis && (esCaptura || esMeta) && estado.SeisesConsecutivos < 3)
            {
                estado.TiradaExtra = true;
            }

            if (estado.Ganador.HasValue)
            {
                res.JugadaRegistrada += $" #{estado.Ganador.Value}";
                res.Mensaje = $"OK: ¡VICTORIA DECLARADA! {estado.Ganador.Value} completó sus cuatro fichas en meta (#{estado.Ganador.Value}). Partida finalizada.";
            }
            else if (esCaptura)
            {
                res.Mensaje = $"OK: Captura aceptada. {tirada.Ficha.Lexema} pasa a {tirada.Destino.Lexema}; rival devuelta a casa. Pendiente: Bono b20.";
            }
            else if (esMeta)
            {
                res.Mensaje = $"OK: Ficha en meta. Pendiente: Bono b10.";
            }
            else
            {
                res.Mensaje = $"OK: Movimiento aceptado ({res.JugadaRegistrada}).";
            }

            if (esBono) estado.BonoPendiente = null;
            else estado.DadoPublicado = null;

            AvanzarTurnoORecurso(estado, dado, obtuvoSeis && !esCaptura && !esMeta);
        }

        private static void AvanzarTurnoORecurso(M5_Estado estado, M0_Dado dado, bool tiradaSimpleSeis)
        {
            if (estado.Ganador.HasValue) return;

            // 1. Bono pendiente prioritario (Regla 9)
            if (estado.BonoPendiente.HasValue)
            {
                return;
            }

            // 2. Liquidación de tirada extra diferida de un 6 anterior (Reglas 4 y 9)
            if (estado.TiradaExtra)
            {
                estado.TiradaExtra = false;
                dado.Lanzar(estado);
                return;
            }

            // 3. Tirada simple de 6 sin bonos (Regla 4)
            if (tiradaSimpleSeis && estado.SeisesConsecutivos < 3)
            {
                dado.Lanzar(estado);
                return;
            }

            // 4. Fin de turno
            CambiarTurno(estado, dado);
        }

        private static void CambiarTurno(M5_Estado estado, M0_Dado dado)
        {
            estado.SeisesConsecutivos = 0;
            estado.TiradaExtra = false;
            estado.TurnoIndex = (estado.TurnoIndex + 1) % estado.Jugadores.Count;
            if (estado.TurnoIndex == 0)
            {
                estado.RondaActual++;
            }
            dado.Lanzar(estado);
        }
    }
}
