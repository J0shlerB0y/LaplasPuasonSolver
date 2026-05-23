using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;

namespace LaplasPuason.MathCore
{
    public readonly struct PolarKey : IEquatable<PolarKey>
    {
        public readonly int K;
        public readonly int N;
        public readonly bool IsSin;
        public readonly bool HasLog;

        public PolarKey(int k, int n, bool isSin, bool hasLog)
        {
            K = k; N = n; IsSin = isSin; HasLog = hasLog;
        }

        public bool Equals(PolarKey o) => K == o.K && N == o.N && IsSin == o.IsSin && HasLog == o.HasLog;
        public override bool Equals(object obj) => obj is PolarKey p && Equals(p);
        public override int GetHashCode()
        {
            unchecked
            {
                int h = K * 397;
                h = (h ^ N) * 397;
                h = (h ^ (IsSin ? 1 : 0)) * 397;
                h = h ^ (HasLog ? 1 : 0);
                return h;
            }
        }
    }

    public sealed class PolarPoly
    {
        private readonly Dictionary<PolarKey, double> _terms = new Dictionary<PolarKey, double>();

        public IReadOnlyDictionary<PolarKey, double> Terms => _terms;
        public bool IsZero => _terms.Count == 0;

        public PolarPoly() { }

        private PolarPoly(Dictionary<PolarKey, double> terms) { _terms = terms; }

        public PolarPoly Clone() => new PolarPoly(new Dictionary<PolarKey, double>(_terms));

        public void Add(PolarKey key, double coef)
        {
            if (Math.Abs(coef) < 1e-12) return;
            if (key.N == 0 && key.IsSin) return;
            if (_terms.TryGetValue(key, out var cur))
            {
                var n = cur + coef;
                if (Math.Abs(n) < 1e-12) _terms.Remove(key);
                else _terms[key] = n;
            }
            else _terms[key] = coef;
        }

        public static PolarPoly operator +(PolarPoly a, PolarPoly b)
        {
            var r = a.Clone();
            foreach (var t in b._terms) r.Add(t.Key, t.Value);
            return r;
        }

        public static PolarPoly operator -(PolarPoly a, PolarPoly b)
        {
            var r = a.Clone();
            foreach (var t in b._terms) r.Add(t.Key, -t.Value);
            return r;
        }

        public static PolarPoly operator -(PolarPoly a)
        {
            var r = new PolarPoly();
            foreach (var t in a._terms) r.Add(t.Key, -t.Value);
            return r;
        }

        public static PolarPoly operator *(double s, PolarPoly a)
        {
            var r = new PolarPoly();
            foreach (var t in a._terms) r.Add(t.Key, s * t.Value);
            return r;
        }

        public static PolarPoly FromPolyXY(PolyXY poly)
        {
            var result = new PolarPoly();
            foreach (var term in poly.Terms)
            {
                int p = term.Key.P;
                int q = term.Key.Q;
                double coef = term.Value;
                int k = p + q;
                Dictionary<(int n, bool isSin), double> fourier = FourierOfCosPSinQ(p, q);
                foreach (var f in fourier)
                {
                    int n = f.Key.n;
                    bool isSin = f.Key.isSin;
                    double c = coef * f.Value;
                    result.Add(new PolarKey(k, n, isSin, false), c);
                }
            }
            return result;
        }

        private static Dictionary<(int n, bool isSin), double> FourierOfCosPSinQ(int p, int q)
        {
            var z = new Dictionary<int, Complex>();
            z[0] = new Complex(1, 0);

            for (int i = 0; i < p; i++) z = MultByCos(z);
            for (int i = 0; i < q; i++) z = MultBySin(z);

            var result = new Dictionary<(int n, bool isSin), double>();
            foreach (var pair in z)
            {
                int m = pair.Key;
                Complex c = pair.Value;
                if (m < 0) continue;
                if (m == 0)
                {
                    if (Math.Abs(c.Real) > 1e-12)
                        result[(0, false)] = c.Real;
                }
                else
                {
                    z.TryGetValue(-m, out var cn);
                    double cosCoef = (c + cn).Real;
                    double sinCoef = (Complex.ImaginaryOne * (c - cn)).Real;
                    if (Math.Abs(cosCoef) > 1e-12) result[(m, false)] = cosCoef;
                    if (Math.Abs(sinCoef) > 1e-12) result[(m, true)] = sinCoef;
                }
            }
            return result;
        }

        private static Dictionary<int, Complex> MultByCos(Dictionary<int, Complex> a)
        {
            var r = new Dictionary<int, Complex>();
            foreach (var pair in a)
            {
                AddComplex(ref r, pair.Key + 1, pair.Value / 2.0);
                AddComplex(ref r, pair.Key - 1, pair.Value / 2.0);
            }
            return r;
        }

        private static Dictionary<int, Complex> MultBySin(Dictionary<int, Complex> a)
        {
            var r = new Dictionary<int, Complex>();
            var halfOverI = new Complex(0, -0.5);
            foreach (var pair in a)
            {
                AddComplex(ref r, pair.Key + 1, pair.Value * halfOverI);
                AddComplex(ref r, pair.Key - 1, -pair.Value * halfOverI);
            }
            return r;
        }

        private static void AddComplex(ref Dictionary<int, Complex> d, int k, Complex v)
        {
            if (d.TryGetValue(k, out var cur)) d[k] = cur + v;
            else d[k] = v;
            if (Complex.Abs(d[k]) < 1e-14) d.Remove(k);
        }

        public PolarPoly InverseLaplacian()
        {
            var r = new PolarPoly();
            foreach (var t in _terms)
            {
                if (t.Key.HasLog)
                    throw new InvalidOperationException("Обратный лапласиан для членов с логарифмом");
                int k = t.Key.K;
                int n = t.Key.N;
                int a = k + 2;
                double denom = (double)a * a - (double)n * n;
                if (Math.Abs(denom) < 1e-12)
                {
                    double resCoef = t.Value / (2.0 * a);
                    r.Add(new PolarKey(a, n, t.Key.IsSin, true), resCoef);
                }
                else
                {
                    r.Add(new PolarKey(a, n, t.Key.IsSin, false), t.Value / denom);
                }
            }
            return r;
        }

        public PolarPoly DerivativeRo()
        {
            var r = new PolarPoly();
            foreach (var t in _terms)
            {
                int k = t.Key.K;
                int n = t.Key.N;
                if (!t.Key.HasLog)
                {
                    if (k != 0)
                        r.Add(new PolarKey(k - 1, n, t.Key.IsSin, false), t.Value * k);
                }
                else
                {
                    if (k != 0)
                        r.Add(new PolarKey(k - 1, n, t.Key.IsSin, true), t.Value * k);
                    r.Add(new PolarKey(k - 1, n, t.Key.IsSin, false), t.Value);
                }
            }
            return r;
        }

        public double Evaluate(double Ro, double phi)
        {
            double s = 0;
            foreach (var t in _terms)
            {
                double RoPow;
                if (t.Key.K == 0) RoPow = 1.0;
                else if (t.Key.K > 0) RoPow = Math.Pow(Ro, t.Key.K);
                else RoPow = 1.0 / Math.Pow(Ro, -t.Key.K);

                double trig = t.Key.IsSin ? Math.Sin(t.Key.N * phi) : Math.Cos(t.Key.N * phi);
                double logFactor = t.Key.HasLog ? Math.Log(Ro) : 1.0;
                s += t.Value * RoPow * trig * logFactor;
            }
            return s;
        }

        public string Format(int decimals)
        {
            if (IsZero) return "0";
            var ordered = _terms
                .OrderBy(t => t.Key.HasLog ? 1 : 0)
                .ThenBy(t => t.Key.K)
                .ThenBy(t => t.Key.N)
                .ThenBy(t => t.Key.IsSin ? 1 : 0)
                .ToList();

            var sb = new StringBuilder();
            bool first = true;
            foreach (var t in ordered)
            {
                double c = Math.Round(t.Value, decimals);
                if (Math.Abs(c) < Math.Pow(10, -decimals) / 2.0) continue;
                string sign = c < 0 ? "-" : (first ? "" : "+");
                double a = Math.Abs(c);
                string body = FormatTerm(a, t.Key, decimals);
                if (first) sb.Append(sign).Append(body);
                else sb.Append(' ').Append(sign).Append(' ').Append(body);
                first = false;
            }
            return sb.Length == 0 ? "0" : sb.ToString();
        }

        private static string FormatTerm(double absCoef, PolarKey k, int decimals)
        {
            var parts = new List<string>();
            bool isPureConstant = (k.K == 0 && k.N == 0 && !k.HasLog);
            if (isPureConstant || Math.Abs(absCoef - 1.0) > Math.Pow(10, -decimals) / 2.0)
                parts.Add(absCoef.ToString("0." + new string('#', decimals), CultureInfo.InvariantCulture));

            if (k.K > 0)
            {
                if (k.K == 1) parts.Add("Ro");
                else parts.Add("Ro^" + k.K);
            }
            if (k.HasLog) parts.Add("ln(Ro)");
            if (k.N > 0)
            {
                string trigName = k.IsSin ? "sin" : "cos";
                string arg = k.N == 1 ? "φ" : (k.N + "·φ");
                parts.Add(trigName + "(" + arg + ")");
            }

            string numerator = parts.Count == 0 ? "1" : string.Join("·", parts);

            if (k.K < 0)
            {
                string denomBase;
                if (k.K == -1) denomBase = "Ro";
                else denomBase = "Ro^" + (-k.K);
                return "(" + numerator + ")/" + denomBase;
            }
            return numerator;
        }
    }
}
