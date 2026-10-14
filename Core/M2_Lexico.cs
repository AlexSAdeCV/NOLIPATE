using NOPILATE.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NolipateSimulator.Core
{
    // ============================================================
    // 13 CATEGORÍAS TERMINALES (T) DE LA GRAMÁTICA BNF (SECCIÓN 11)
    // ============================================================
    public enum TipoToken
    {
        RONDA,        // Ej: "12."
        COLOR,        // "A", "Z", "R", "V"
        DADO,         // "d1" a "d6"
        BONO,         // "b10", "b20"
        FICHA,        // "A1"-"A4", "Z1"-"Z4", "R1"-"R4", "V1"-"V4"
        CASILLA,      // "1" a "68" (Aceptado por AFD)
        PASILLO,      // "p1" a "p7"
        CASA,         // "c"
        META,         // "m"
        OPERADOR,     // "-", "x", "="
        PASO,         // "*"
        BARRERA,      // "!"
        CIERRE,       // "#"
        EOF           // Fin de entrada
    }

    public class Token
    {
        public TipoToken Tipo { get; }
        public string Lexema { get; }
        public int Posicion { get; } // Posición base 1 (columna en la entrada)

        public Token(TipoToken tipo, string lexema, int posicion)
        {
            Tipo = tipo;
            Lexema = lexema;
            Posicion = posicion;
        }

        public override string ToString() => $"<{Tipo}, '{Lexema}', pos {Posicion}>";
    }

    public class ResultadoLexico
    {
        // Debe iniciar en true; solo pasa a false si se detecta un error léxico
        public bool EsValido { get; set; } = true;
        public List<Token> Tokens { get; set; } = new List<Token>();
        public string MensajeError { get; set; } = string.Empty;
        public int PosicionError { get; set; }
    }

    // ============================================================
    // M2 - ANALIZADOR LÉXICO Y AFD
    // ============================================================
    public class M2_Lexico
    {
        private readonly string _fuente;
        private int _cursor;
        private readonly int _longitud;

        public M2_Lexico(string fuente)
        {
            _fuente = fuente ?? string.Empty;
            _cursor = 0;
            _longitud = _fuente.Length;
        }

        public ResultadoLexico Analizar()
        {
            var resultado = new ResultadoLexico();
            _cursor = 0;

            while (_cursor < _longitud)
            {
                char actual = _fuente[_cursor];

                // 1. Manejo y normalización de espacios y tabuladores (Sección 10)
                if (actual == ' ' || actual == '\t' || actual == '\r' || actual == '\n')
                {
                    _cursor++;
                    continue;
                }

                // 2. Validación de pertenencia al alfabeto canónico Σ (27 caracteres)
                if (!Constantes.AlfabetoCanonico.Contains(actual))
                {
                    resultado.EsValido = false;
                    resultado.PosicionError = _cursor + 1;
                    resultado.MensajeError = $"Error léxico: símbolo '{actual}' no pertenece al alfabeto canónico en pos. {resultado.PosicionError}.";
                    return resultado;
                }

                int posInicio = _cursor + 1;

                // 3. Reconocimiento de números (RONDA o CASILLA mediante AFD)
                if (char.IsDigit(actual))
                {
                    var tokenNum = ReconocerNumeroOTransicionAFD(resultado);
                    // Si tokenNum es null, el AFD falló y ya asignó el error correspondiente
                    if (tokenNum == null)
                    {
                        return resultado;
                    }
                    resultado.Tokens.Add(tokenNum);
                    continue;
                }

                // 4. Reconocimiento de COLORES o FICHAS (A, Z, R, V)
                if (actual == 'A' || actual == 'Z' || actual == 'R' || actual == 'V')
                {
                    _cursor++;
                    if (_cursor < _longitud && char.IsDigit(_fuente[_cursor]))
                    {
                        char num = _fuente[_cursor];
                        if (num >= '1' && num <= '4')
                        {
                            _cursor++;
                            resultado.Tokens.Add(new Token(TipoToken.FICHA, $"{actual}{num}", posInicio));
                        }
                        else
                        {
                            resultado.EsValido = false;
                            resultado.PosicionError = _cursor + 1;
                            resultado.MensajeError = $"Error léxico: ficha '{actual}{num}' inválida. Índice debe ser 1-4 en pos. {resultado.PosicionError}.";
                            return resultado;
                        }
                    }
                    else
                    {
                        resultado.Tokens.Add(new Token(TipoToken.COLOR, actual.ToString(), posInicio));
                    }
                    continue;
                }

                // 5. Prefijo 'd' (DADO: d1 a d6)
                if (actual == 'd')
                {
                    _cursor++;
                    if (_cursor < _longitud && _fuente[_cursor] >= '1' && _fuente[_cursor] <= '6')
                    {
                        char valor = _fuente[_cursor++];
                        resultado.Tokens.Add(new Token(TipoToken.DADO, $"d{valor}", posInicio));
                    }
                    else
                    {
                        char erroneo = _cursor < _longitud ? _fuente[_cursor] : ' ';
                        resultado.EsValido = false;
                        resultado.PosicionError = _cursor + 1;
                        resultado.MensajeError = $"Error léxico: dado 'd{erroneo}' inválido. Valores permitidos 1-6 en pos. {resultado.PosicionError}.";
                        return resultado;
                    }
                    continue;
                }

                // 6. Prefijo 'b' (BONO: b10 o b20)
                if (actual == 'b')
                {
                    _cursor++;
                    int posBono = _cursor;
                    string resto = "";
                    while (_cursor < _longitud && char.IsDigit(_fuente[_cursor]))
                    {
                        resto += _fuente[_cursor++];
                    }

                    if (resto == "10" || resto == "20")
                    {
                        resultado.Tokens.Add(new Token(TipoToken.BONO, $"b{resto}", posInicio));
                    }
                    else
                    {
                        resultado.EsValido = false;
                        resultado.PosicionError = posBono + 1;
                        resultado.MensajeError = $"Error léxico: bono 'b{resto}' inválido. Solo se admiten b10 o b20 en pos. {resultado.PosicionError}.";
                        return resultado;
                    }
                    continue;
                }

                // 7. Prefijo 'p' (PASILLO: p1 a p7)
                if (actual == 'p')
                {
                    _cursor++;
                    if (_cursor < _longitud && _fuente[_cursor] >= '1' && _fuente[_cursor] <= '7')
                    {
                        char casillaP = _fuente[_cursor++];
                        resultado.Tokens.Add(new Token(TipoToken.PASILLO, $"p{casillaP}", posInicio));
                    }
                    else
                    {
                        char erroneo = _cursor < _longitud ? _fuente[_cursor] : ' ';
                        resultado.EsValido = false;
                        resultado.PosicionError = _cursor + 1;
                        resultado.MensajeError = $"Error léxico: pasillo 'p{erroneo}' inválido. Valores permitidos p1-p7 en pos. {resultado.PosicionError}.";
                        return resultado;
                    }
                    continue;
                }

                // 8. Símbolos simples de un carácter
                switch (actual)
                {
                    case 'c':
                        _cursor++;
                        resultado.Tokens.Add(new Token(TipoToken.CASA, "c", posInicio));
                        break;
                    case 'm':
                        _cursor++;
                        resultado.Tokens.Add(new Token(TipoToken.META, "m", posInicio));
                        break;
                    case '-':
                    case 'x':
                    case '=':
                        _cursor++;
                        resultado.Tokens.Add(new Token(TipoToken.OPERADOR, actual.ToString(), posInicio));
                        break;
                    case '*':
                        _cursor++;
                        resultado.Tokens.Add(new Token(TipoToken.PASO, "*", posInicio));
                        break;
                    case '!':
                        _cursor++;
                        resultado.Tokens.Add(new Token(TipoToken.BARRERA, "!", posInicio));
                        break;
                    case '#':
                        _cursor++;
                        resultado.Tokens.Add(new Token(TipoToken.CIERRE, "#", posInicio));
                        break;
                    default:
                        resultado.EsValido = false;
                        resultado.PosicionError = posInicio;
                        resultado.MensajeError = $"Error léxico: símbolo inesperado '{actual}' en pos. {posInicio}.";
                        return resultado;
                }
            }

            resultado.Tokens.Add(new Token(TipoToken.EOF, string.Empty, _longitud + 1));
            resultado.EsValido = true;
            return resultado;
        }

        // =========================================================================
        // IMPLEMENTACIÓN DEL AFD DE CASILLAS (FIGURA 4) Y DETECCIÓN DE RONDA
        // Estados: q0 (inicio), q1 (1-5), q2 (6), q3 (7-9), q4 (10-68), qE (error)
        // =========================================================================
        private Token ReconocerNumeroOTransicionAFD(ResultadoLexico resultado)
        {
            int posInicio = _cursor + 1;
            var sb = new StringBuilder();

            while (_cursor < _longitud && char.IsDigit(_fuente[_cursor]))
            {
                sb.Append(_fuente[_cursor++]);
            }

            // Si viene seguido de '.', es una RONDA (ej: "12.")
            if (_cursor < _longitud && _fuente[_cursor] == '.')
            {
                _cursor++;
                string lexemaRonda = sb.ToString() + ".";
                return new Token(TipoToken.RONDA, lexemaRonda, posInicio);
            }

            string lexema = sb.ToString();

            // Simulación del AFD de Casilla (Figura 4)
            int estado = 0; // q0
            foreach (char c in lexema)
            {
                switch (estado)
                {
                    case 0: // q0
                        if (c >= '1' && c <= '5') estado = 1;      // q1
                        else if (c == '6') estado = 2;             // q2
                        else if (c >= '7' && c <= '9') estado = 3; // q3
                        else estado = -1;                          // qE (dígito 0)
                        break;

                    case 1: // q1 (leyó 1-5, admite 0-9 para formar 10-59)
                        if (c >= '0' && c <= '9') estado = 4;      // q4
                        else estado = -1;                          // qE
                        break;

                    case 2: // q2 (leyó 6, admite 0-8 para formar 60-68)
                        if (c >= '0' && c <= '8') estado = 4;      // q4
                        else estado = -1;                          // qE (ej: 69)
                        break;

                    case 3: // q3 (leyó 7-9, no admite segundo dígito para casilla)
                        estado = -1;                               // qE
                        break;

                    case 4: // q4 (ya tiene dos dígitos, no admite más)
                        estado = -1;                               // qE
                        break;

                    default:
                        estado = -1;
                        break;
                }

                if (estado == -1) break;
            }

            // Estados de aceptación: q1 (1-5), q2 (6), q3 (7-9), q4 (10-68)
            if (estado == 1 || estado == 2 || estado == 3 || estado == 4)
            {
                return new Token(TipoToken.CASILLA, lexema, posInicio);
            }

            // Si llegó a qE o terminó en estado no de aceptación (como 0 solo)
            resultado.EsValido = false;
            resultado.PosicionError = posInicio;
            resultado.MensajeError = $"Error léxico: casilla '{lexema}' fuera de rango. El circuito solo admite casillas de 1 a 68 en pos. {posInicio}.";
            return null;
        }
    }
}
