using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LaplasPuason.MathCore
{
    public sealed class PolyXY
    {
        private readonly Dictionary<MonomKey, double> _terms = new Dictionary<MonomKey, double>();

        public IReadOnlyDictionary<MonomKey, double> Terms => _terms;
        public bool IsZero => _terms.Count == 0;

        public PolyXY() { }

        private PolyXY(Dictionary<MonomKey, double> terms)
        {
            _terms = terms;
        }

        public static PolyXY Zero => new PolyXY();

        public static PolyXY Const(double c)
        {
            var p = Zero;
            p.AddTerm(0, 0, c);
            return p;
        }

        public static PolyXY X
        {
            get {
                var p = new PolyXY();
                p.AddTerm(1, 0, 1.0);
                return p;
            }
        }

        public static PolyXY Y
        {
            get {
                var p = new PolyXY();
                p.AddTerm(0, 1, 1.0);
                return p;
            }
        }

        public PolyXY Clone()
        {
            return new PolyXY(new Dictionary<MonomKey, double>(_terms));
        }

        private void AddTerm(int p, int q, double c)
        {
            if (Math.Abs(c) < 1e-13) return;
            var k = new MonomKey(p, q);
            if (_terms.TryGetValue(k, out var cur))
            {
                var n = cur + c;
                if (Math.Abs(n) < 1e-13) _terms.Remove(k);
                else _terms[k] = n;
            }
            else
            {
                _terms[k] = c;
            }
        }

        public static PolyXY operator +(PolyXY a, PolyXY b)
        {
            var r = a.Clone();
            foreach (var t in b._terms) r.AddTerm(t.Key.P, t.Key.Q, t.Value);
            return r;
        }

        public static PolyXY operator -(PolyXY a, PolyXY b)
        {
            var r = a.Clone();
            foreach (var t in b._terms) r.AddTerm(t.Key.P, t.Key.Q, -t.Value);
            return r;
        }

        public static PolyXY operator -(PolyXY a)
        {
            var r = new PolyXY();
            foreach (var t in a._terms) r.AddTerm(t.Key.P, t.Key.Q, -t.Value);
            return r;
        }

        public static PolyXY operator *(PolyXY a, PolyXY b)
        {
            var r = new PolyXY();
            foreach (var ta in a._terms)
                foreach (var tb in b._terms)
                    r.AddTerm(ta.Key.P + tb.Key.P, ta.Key.Q + tb.Key.Q, ta.Value * tb.Value);
            return r;
        }

        public static PolyXY operator *(double s, PolyXY a)
        {
            var r = new PolyXY();
            foreach (var t in a._terms) r.AddTerm(t.Key.P, t.Key.Q, s * t.Value);
            return r;
        }

        public static PolyXY operator /(PolyXY a, double s)
        {
            return (1.0 / s) * a;
        }

        public PolyXY Pow(int n)
        {
            if (n < 0) throw new InvalidOperationException("Отрицательная степень в полиноме");
            var r = Const(1.0);
            for (int i = 0; i < n; i++) r = r * this;
            return r;
        }

        public double Evaluate(double x, double y)
        {
            double s = 0;
            foreach (var t in _terms)
                s += t.Value * Pow(x, t.Key.P) * Pow(y, t.Key.Q);
            return s;
        }

        public int Degree
        {
            get
            {
                int d = 0;
                foreach (var k in _terms.Keys)
                    if (k.P + k.Q > d) d = k.P + k.Q;
                return d;
            }
        }

        public bool IsConstant(out double value)
        {
            value = 0;
            foreach (var t in _terms)
            {
                if (t.Key.P != 0 || t.Key.Q != 0) return false;
                value = t.Value;
            }
            return true;
        }

        public double Coefficient(int p, int q)
        {
            return _terms.TryGetValue(new MonomKey(p, q), out var v) ? v : 0.0;
        }

        public PolyXY Substitute(PolyXY xExpr, PolyXY yExpr)
        {
            var result = Zero;
            foreach (var term in _terms)
            {
                var t = Const(term.Value) * xExpr.Pow(term.Key.P) * yExpr.Pow(term.Key.Q);
                result = result + t;
            }
            return result;
        }

        public PolyXY ShiftBy(double dx, double dy)
        {
            return Substitute(X + Const(dx), Y + Const(dy));
        }

        private static double Pow(double x, int n)
        {
            if (n == 0) return 1.0;
            double r = 1.0;
            for (int i = 0; i < n; i++) r *= x;
            return r;
        }

        public override string ToString()
        {
            if (IsZero) return "0";
            var ordered = _terms.OrderByDescending(t => t.Key.P + t.Key.Q).ThenByDescending(t => t.Key.P);
            var sb = new StringBuilder();
            bool first = true;
            foreach (var t in ordered)
            {
                double c = t.Value;
                string sign = c < 0 ? "-" : (first ? "" : "+");
                double a = Math.Abs(c);
                string body;
                if (t.Key.P == 0 && t.Key.Q == 0)
                {
                    body = a.ToString("G", CultureInfo.InvariantCulture);
                }
                else
                {
                    var partsList = new List<string>();
                    if (Math.Abs(a - 1.0) > 1e-13) partsList.Add(a.ToString("G", CultureInfo.InvariantCulture));
                    if (t.Key.P == 1) partsList.Add("x");
                    else if (t.Key.P > 1) partsList.Add("x^" + t.Key.P);
                    if (t.Key.Q == 1) partsList.Add("y");
                    else if (t.Key.Q > 1) partsList.Add("y^" + t.Key.Q);
                    body = string.Join("*", partsList);
                }
                if (first) sb.Append(sign).Append(body);
                else sb.Append(' ').Append(sign).Append(' ').Append(body);
                first = false;
            }
            return sb.ToString();
        }
    }

    public readonly struct MonomKey : IEquatable<MonomKey>
    {
        public readonly int P;
        public readonly int Q;
        public MonomKey(int p, int q) { P = p; Q = q; }
        public bool Equals(MonomKey o) => P == o.P && Q == o.Q;
        public override bool Equals(object obj) => obj is MonomKey m && Equals(m);
        public override int GetHashCode() => unchecked(P * 397 ^ Q);
    }

    public sealed class ParseException : Exception
    {
        public ParseException(string message) : base(message) { }
    }

    public static class PolyParser
    {
        public static PolyXY Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ParseException("Пустое выражение");
            var tokens = Tokenize(input);
            int pos = 0;
            var result = ParseAddSub(tokens, ref pos);
            if (pos < tokens.Count)
                throw new ParseException("Ожидался конец выражения, найден символ " + tokens[pos].Text);
            return result;
        }

        public static (PolyXY left, PolyXY right) ParseEquation(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ParseException("Пустое уравнение");
            int eqIndex = input.IndexOf('=');
            if (eqIndex < 0)
                throw new ParseException("Ожидался знак '=' в уравнении окружности");
            var left = Parse(input.Substring(0, eqIndex));
            var right = Parse(input.Substring(eqIndex + 1));
            return (left, right);
        }

        private enum Tk { Number, Ident, Plus, Minus, Star, Slash, Caret, LParen, RParen }

        private struct Token
        {
            public Tk Type;
            public string Text;
            public double Number;
        }

        private static List<Token> Tokenize(string s)
        {
            var list = new List<Token>();
            int i = 0;
            while (i < s.Length)
            {
                char c = s[i];
                if (char.IsWhiteSpace(c)) { i++; continue; }
                if (char.IsDigit(c) || c == '.')
                {
                    int start = i;
                    while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.')) i++;
                    var lit = s.Substring(start, i - start);
                    if (!double.TryParse(lit, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                        throw new ParseException("Некорректное число " + lit);
                    list.Add(new Token { Type = Tk.Number, Number = v, Text = lit });
                    continue;
                }
                if (char.IsLetter(c))
                {
                    int start = i;
                    while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
                    list.Add(new Token { Type = Tk.Ident, Text = s.Substring(start, i - start) });
                    continue;
                }
                switch (c)
                {
                    case '+': list.Add(new Token { Type = Tk.Plus, Text = "+" }); i++; break;
                    case '-': list.Add(new Token { Type = Tk.Minus, Text = "-" }); i++; break;
                    case '*': list.Add(new Token { Type = Tk.Star, Text = "*" }); i++; break;
                    case '/': list.Add(new Token { Type = Tk.Slash, Text = "/" }); i++; break;
                    case '^': list.Add(new Token { Type = Tk.Caret, Text = "^" }); i++; break;
                    case '(': list.Add(new Token { Type = Tk.LParen, Text = "(" }); i++; break;
                    case ')': list.Add(new Token { Type = Tk.RParen, Text = ")" }); i++; break;
                    default:
                        throw new ParseException("Недопустимый символ " + c);
                }
            }
            return list;
        }

        private static PolyXY ParseAddSub(List<Token> t, ref int pos)
        {
            var left = ParseMulDiv(t, ref pos);
            while (pos < t.Count && (t[pos].Type == Tk.Plus || t[pos].Type == Tk.Minus))
            {
                var op = t[pos].Type;
                pos++;
                var right = ParseMulDiv(t, ref pos);
                left = (op == Tk.Plus) ? left + right : left - right;
            }
            return left;
        }

        private static PolyXY ParseMulDiv(List<Token> t, ref int pos)
        {
            var left = ParseUnary(t, ref pos);
            while (pos < t.Count && (t[pos].Type == Tk.Star || t[pos].Type == Tk.Slash))
            {
                var op = t[pos].Type;
                pos++;
                var right = ParseUnary(t, ref pos);
                if (op == Tk.Star)
                {
                    left = left * right;
                }
                else
                {
                    if (!right.IsConstant(out var divisor))
                        throw new ParseException("Делитть можно только на численную константу");
                    if (Math.Abs(divisor) < 1e-15)
                        throw new ParseException("Деление на ноль");
                    left = left / divisor;
                }
            }
            return left;
        }

        private static PolyXY ParseUnary(List<Token> t, ref int pos)
        {
            if (pos < t.Count && t[pos].Type == Tk.Minus)
            {
                pos++;
                return -ParseUnary(t, ref pos);
            }
            if (pos < t.Count && t[pos].Type == Tk.Plus)
            {
                pos++;
                return ParseUnary(t, ref pos);
            }
            return ParsePower(t, ref pos);
        }

        private static PolyXY ParsePower(List<Token> t, ref int pos)
        {
            var baseExpr = ParseAtom(t, ref pos);
            if (pos < t.Count && t[pos].Type == Tk.Caret)
            {
                pos++;
                var exp = ParseUnary(t, ref pos);
                if (!exp.IsConstant(out var exponentVal))
                    throw new ParseException("Показател степени должен быть целым числом");
                if (Math.Abs(exponentVal - Math.Round(exponentVal)) > 1e-9)
                    throw new ParseException("Показател степени должен быть целым числом");
                int n = (int)Math.Round(exponentVal);
                return baseExpr.Pow(n);
            }
            return baseExpr;
        }

        private static PolyXY ParseAtom(List<Token> t, ref int pos)
        {
            if (pos >= t.Count) throw new ParseException("Неожиданный конец выражения.");
            var tok = t[pos];
            if (tok.Type == Tk.Number)
            {
                pos++;
                return PolyXY.Const(tok.Number);
            }
            if (tok.Type == Tk.LParen)
            {
                pos++;
                var inner = ParseAddSub(t, ref pos);
                if (pos >= t.Count || t[pos].Type != Tk.RParen)
                    throw new ParseException("Ожидалась закрывающая скобка.");
                pos++;
                return inner;
            }
            if (tok.Type == Tk.Ident)
            {
                pos++;
                if (tok.Text == "x") return PolyXY.X;
                if (tok.Text == "y") return PolyXY.Y;

                if (tok.Text == "log" || tok.Text == "ln")
                {
                    if (pos >= t.Count || t[pos].Type != Tk.LParen)
                        throw new ParseException($"Функция {tok.Text} требует аргумента в скобках");
                    pos++;
                    var inner = ParseAddSub(t, ref pos);
                    if (pos >= t.Count || t[pos].Type != Tk.RParen)
                        throw new ParseException("Ожидалась закрывающая скобка для логарифма");
                    pos++;

                    if (!inner.IsConstant(out var val))
                        throw new ParseException("Аналитическое решение поддерживает логарифм только от численных констант");
                    if (val <= 0)
                        throw new ParseException("Аргумент логарифма должен быть строго больше нуля");

                    return PolyXY.Const(Math.Log(val));
                }

                throw new ParseException("Неизвестный идентификатор " + tok.Text);
            }
            throw new ParseException("Неожиданный символ " + tok.Text);
        }
    }
}
