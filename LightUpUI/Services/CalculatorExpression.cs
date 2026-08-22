using System;
using System.Globalization;

namespace LightUpUI.Services;

public static class CalculatorExpression
{
    public static bool TryEvaluate(string expression, out decimal result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(expression))
            return false;

        var parser = new Parser(expression.Replace('×', '*').Replace('÷', '/'));
        return parser.TryParse(out result);
    }

    public static string Format(decimal result) => result.ToString("G29", CultureInfo.InvariantCulture);

    private sealed class Parser(string text)
    {
        private int _position;
        private bool _hasOperation;

        public bool TryParse(out decimal result)
        {
            result = 0;
            try
            {
                result = ParseExpression();
                SkipWhitespace();
                return _hasOperation && _position == text.Length;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }
            catch (DivideByZeroException)
            {
                return false;
            }
        }

        private decimal ParseExpression()
        {
            var value = ParseTerm();
            while (true)
            {
                if (TryRead('+'))
                {
                    _hasOperation = true;
                    value += ParseTerm();
                }
                else if (TryRead('-'))
                {
                    _hasOperation = true;
                    value -= ParseTerm();
                }
                else
                {
                    return value;
                }
            }
        }

        private decimal ParseTerm()
        {
            var value = ParseFactor();
            while (true)
            {
                if (TryRead('*'))
                {
                    _hasOperation = true;
                    value *= ParseFactor();
                }
                else if (TryRead('/'))
                {
                    _hasOperation = true;
                    var divisor = ParseFactor();
                    if (divisor == 0)
                        throw new DivideByZeroException();

                    value /= divisor;
                }
                else if (TryRead('%'))
                {
                    _hasOperation = true;
                    var divisor = ParseFactor();
                    if (divisor == 0)
                        throw new DivideByZeroException();

                    value %= divisor;
                }
                else
                {
                    return value;
                }
            }
        }

        private decimal ParseFactor()
        {
            if (TryRead('+'))
                return ParseFactor();
            if (TryRead('-'))
                return -ParseFactor();
            if (TryRead('('))
            {
                var value = ParseExpression();
                if (!TryRead(')'))
                    throw new FormatException();

                return value;
            }

            return ParseNumber();
        }

        private decimal ParseNumber()
        {
            SkipWhitespace();
            var start = _position;
            var sawDecimalPoint = false;
            var digitCount = 0;
            while (_position < text.Length)
            {
                var character = text[_position];
                if (char.IsDigit(character))
                {
                    digitCount++;
                    _position++;
                    continue;
                }

                if (character == '.' && !sawDecimalPoint)
                {
                    sawDecimalPoint = true;
                    _position++;
                    continue;
                }

                break;
            }

            if (digitCount == 0
                || !decimal.TryParse(
                    text[start.._position],
                    NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out var value))
            {
                throw new FormatException();
            }

            return value;
        }

        private bool TryRead(char expected)
        {
            SkipWhitespace();
            if (_position >= text.Length || text[_position] != expected)
                return false;

            _position++;
            return true;
        }

        private void SkipWhitespace()
        {
            while (_position < text.Length && char.IsWhiteSpace(text[_position]))
                _position++;
        }
    }
}
