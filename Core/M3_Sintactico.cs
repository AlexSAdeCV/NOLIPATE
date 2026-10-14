using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NolipateSimulator.Core
{
    // ============================================================
    // ESTRUCTURA DEL ÁRBOL SINTÁCTICO / ACCIÓN ESTRUCTURADA (SEC. 12.14)
    // ============================================================
    public class NodoTirada
    {
        public Token Recurso { get; set; }           // DADO ("d6") o BONO ("b20")
        public bool EsPaso { get; set; }              // true si la acción es '*'
        public Token Ficha { get; set; }              // ej: "R2"
        public Token Origen { get; set; }             // CASILLA ("39"), PASILLO ("p5"), CASA ("c")
        public Token Operador { get; set; }           // "-", "x", "="
        public Token Destino { get; set; }            // CASILLA ("45"), PASILLO ("p2"), META ("m"), CASA ("c")
        public Token AnotacionOpc { get; set; }       // "!" o null si es epsilon

        public override string ToString()
        {
            if (EsPaso)
                return $"{Recurso.Lexema}* (Paso)";

            string anot = AnotacionOpc != null ? AnotacionOpc.Lexema : "";
            return $"{Recurso.Lexema} {Ficha.Lexema} {Origen.Lexema}{Operador.Lexema}{Destino.Lexema}{anot}";
        }
    }

    public class ResultadoSintactico
    {
        public bool EsValido { get; set; }
        public NodoTirada Arbol { get; set; }
        public string MensajeError { get; set; }
        public int PosicionError { get; set; }
    }

    // ============================================================
    // M3 - PARSER DESCENDENTE RECURSIVO LL(1)
    // ============================================================
    public class M3_Sintactico
    {
        private readonly List<Token> _tokens;
        private int _indice;
        private Token _tokenActual => _indice < _tokens.Count ? _tokens[_indice] : _tokens[_tokens.Count - 1];

        public M3_Sintactico(List<Token> tokens)
        {
            _tokens = tokens ?? new List<Token>();
            _indice = 0;
        }

        public ResultadoSintactico AnalizarTirada()
        {
            var resultado = new ResultadoSintactico();
            _indice = 0;

            try
            {
                var arbol = ReglaTirada();

                // Debe consumirse toda la entrada hasta EOF
                if (_tokenActual.Tipo != TipoToken.EOF)
                {
                    resultado.EsValido = false;
                    resultado.PosicionError = _tokenActual.Posicion;
                    resultado.MensajeError = $"Error sintáctico: texto adicional inesperado '{_tokenActual.Lexema}' tras la tirada en pos. {_tokenActual.Posicion}.";
                    return resultado;
                }

                resultado.EsValido = true;
                resultado.Arbol = arbol;
                return resultado;
            }
            catch (SintacticoException ex)
            {
                resultado.EsValido = false;
                resultado.PosicionError = ex.Posicion;
                resultado.MensajeError = ex.Message;
                return resultado;
            }
        }

        // <TIRADA> ::= DADO <ACCION> | BONO <ACCION>
        private NodoTirada ReglaTirada()
        {
            var nodo = new NodoTirada();

            if (_tokenActual.Tipo == TipoToken.DADO || _tokenActual.Tipo == TipoToken.BONO)
            {
                nodo.Recurso = _tokenActual;
                Avanzar();
                ReglaAccion(nodo);
                return nodo;
            }

            throw new SintacticoException(
                $"Error sintáctico: se esperaba DADO o BONO al inicio de la jugada, pero se encontró '{_tokenActual.Lexema}' en pos. {_tokenActual.Posicion}.",
                _tokenActual.Posicion);
        }

        // <ACCION> ::= <MOVIMIENTO> | PASO
        private void ReglaAccion(NodoTirada nodo)
        {
            if (_tokenActual.Tipo == TipoToken.PASO)
            {
                nodo.EsPaso = true;
                Avanzar();
                return;
            }

            // Si no es paso, debe derivar en <MOVIMIENTO>
            ReglaMovimiento(nodo);
        }

        // <MOVIMIENTO> ::= FICHA <ORIGEN> OPERADOR <DESTINO> <ANOTACION_OPC>
        private void ReglaMovimiento(NodoTirada nodo)
        {
            nodo.EsPaso = false;

            // 1. FICHA
            if (_tokenActual.Tipo == TipoToken.FICHA)
            {
                nodo.Ficha = _tokenActual;
                Avanzar();
            }
            else
            {
                throw new SintacticoException(
                    $"Error sintáctico: se esperaba una FICHA (ej: A1, R2), pero se encontró '{_tokenActual.Lexema}' en pos. {_tokenActual.Posicion}.",
                    _tokenActual.Posicion);
            }

            // 2. <ORIGEN> ::= CASILLA | PASILLO | CASA
            nodo.Origen = ReglaOrigen();

            // 3. OPERADOR ::= '-' | 'x' | '='
            if (_tokenActual.Tipo == TipoToken.OPERADOR)
            {
                nodo.Operador = _tokenActual;
                Avanzar();
            }
            else
            {
                throw new SintacticoException(
                    $"Error sintáctico: se esperaba un operador ('-', 'x', '='), pero se encontró '{_tokenActual.Lexema}' en pos. {_tokenActual.Posicion}.",
                    _tokenActual.Posicion);
            }

            // 4. <DESTINO> ::= CASILLA | PASILLO | META | CASA
            nodo.Destino = ReglaDestino();

            // 5. <ANOTACION_OPC> ::= BARRERA | ε
            if (_tokenActual.Tipo == TipoToken.BARRERA)
            {
                nodo.AnotacionOpc = _tokenActual;
                Avanzar();
            }
            else
            {
                nodo.AnotacionOpc = null; // ε
            }
        }

        // <ORIGEN> ::= CASILLA | PASILLO | CASA
        private Token ReglaOrigen()
        {
            if (_tokenActual.Tipo == TipoToken.CASILLA ||
                _tokenActual.Tipo == TipoToken.PASILLO ||
                _tokenActual.Tipo == TipoToken.CASA)
            {
                var token = _tokenActual;
                Avanzar();
                return token;
            }

            throw new SintacticoException(
                $"Error sintáctico: origen inválido '{_tokenActual.Lexema}'. Se esperaba CASILLA (1-68), PASILLO (p1-p7) o CASA ('c') en pos. {_tokenActual.Posicion}.",
                _tokenActual.Posicion);
        }

        // <DESTINO> ::= CASILLA | PASILLO | META | CASA
        private Token ReglaDestino()
        {
            if (_tokenActual.Tipo == TipoToken.CASILLA ||
                _tokenActual.Tipo == TipoToken.PASILLO ||
                _tokenActual.Tipo == TipoToken.META ||
                _tokenActual.Tipo == TipoToken.CASA)
            {
                var token = _tokenActual;
                Avanzar();
                return token;
            }

            throw new SintacticoException(
                $"Error sintáctico: destino inválido '{_tokenActual.Lexema}'. Se esperaba CASILLA (1-68), PASILLO (p1-p7), META ('m') o CASA ('c') en pos. {_tokenActual.Posicion}.",
                _tokenActual.Posicion);
        }

        private void Avanzar()
        {
            if (_indice < _tokens.Count)
                _indice++;
        }
    }

    public class SintacticoException : Exception
    {
        public int Posicion { get; }
        public SintacticoException(string mensaje, int posicion) : base(mensaje)
        {
            Posicion = posicion;
        }
    }
}
