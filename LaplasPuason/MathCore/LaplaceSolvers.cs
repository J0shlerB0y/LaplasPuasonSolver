using System;
using System.Collections.Generic;
using System.Linq;

namespace LaplasPuason.MathCore
{
    public enum BoundaryType { Dirichlet, Neumann }

    public sealed class SolutionResult
    {
        public PolarPoly FullSolution { get; set; }
        public PolarPoly ParticularSolution { get; set; }
        public PolarPoly HomogeneousSolution { get; set; }
        public bool IsSolvable { get; set; }
        public string Diagnostics { get; set; }
    }

    public abstract class LaplaceSolver
    {
        public CircularDomain Domain { get; }

        protected LaplaceSolver(CircularDomain domain)
        {
            Domain = domain;
        }

        public abstract SolutionResult Solve(BoundaryType type, PolyXY source, PolyXY[] boundary);

        public static LaplaceSolver Create(CircularDomain domain)
        {
            if (domain is RingDomain r) return new RingSolver(r);
            if (domain is InnerDiskDomain i) return new InnerDiskSolver(i);
            if (domain is OuterDiskDomain o) return new OuterDiskSolver(o);
            throw new ArgumentException("не поддерживаемый тип сущности");
        }

        protected static PolarPoly ShiftToPolar(PolyXY p, CircularDomain d)
            => PolarPoly.FromPolyXY(p.ShiftBy(d.CenterX, d.CenterY));

        protected static FourierSeries EvaluateAtRo(PolarPoly p, double Ro)
        {
            var fs = new FourierSeries();
            foreach (var t in p.Terms)
            {
                double RoPow = Math.Pow(Ro, t.Key.K);
                double logFactor = t.Key.HasLog ? Math.Log(Ro) : 1.0;
                double c = t.Value * RoPow * logFactor;
                fs.Add(t.Key.N, t.Key.IsSin, c);
            }
            return fs;
        }
    }

    public sealed class FourierSeries
    {
        private readonly Dictionary<(int n, bool isSin), double> _coefs = new Dictionary<(int n, bool isSin), double>();

        public IReadOnlyDictionary<(int n, bool isSin), double> Coefficients => _coefs;

        public void Add(int n, bool isSin, double coef)
        {
            if (Math.Abs(coef) < 1e-12) return;
            if (n == 0 && isSin) return;
            var k = (n, isSin);
            if (_coefs.TryGetValue(k, out var cur))
            {
                var v = cur + coef;
                if (Math.Abs(v) < 1e-12) _coefs.Remove(k);
                else _coefs[k] = v;
            }
            else _coefs[k] = coef;
        }

        public double Get(int n, bool isSin)
            => _coefs.TryGetValue((n, isSin), out var v) ? v : 0.0;

        public int MaxFrequency
        {
            get
            {
                int m = 0;
                foreach (var k in _coefs.Keys) if (k.n > m) m = k.n;
                return m;
            }
        }

        public static FourierSeries operator -(FourierSeries a, FourierSeries b)
        {
            var r = new FourierSeries();
            foreach (var p in a._coefs) r.Add(p.Key.n, p.Key.isSin, p.Value);
            foreach (var p in b._coefs) r.Add(p.Key.n, p.Key.isSin, -p.Value);
            return r;
        }

        public static FourierSeries operator -(FourierSeries a)
        {
            var r = new FourierSeries();
            foreach (var p in a._coefs) r.Add(p.Key.n, p.Key.isSin, -p.Value);
            return r;
        }
    }

    public sealed class RingSolver : LaplaceSolver
    {
        public new RingDomain Domain => (RingDomain)base.Domain;

        public RingSolver(RingDomain d) : base(d) { }

        public override SolutionResult Solve(BoundaryType type, PolyXY source, PolyXY[] boundary)
        {
            if (boundary == null || boundary.Length != 2)
                throw new ArgumentException("Для кольца требуются две граничные функции");

            var srcPolar = ShiftToPolar(source ?? PolyXY.Zero, Domain);
            var b1Polar = ShiftToPolar(boundary[0] ?? PolyXY.Zero, Domain);
            var b2Polar = ShiftToPolar(boundary[1] ?? PolyXY.Zero, Domain);

            var up = srcPolar.InverseLaplacian();
            var v = new PolarPoly();
            string diag = string.Empty;
            bool solvable = true;

            double R1 = Domain.RInner;
            double R2 = Domain.ROuter;

            if (type == BoundaryType.Dirichlet)
            {
                var H1 = EvaluateAtRo(b1Polar, R1) - EvaluateAtRo(up, R1);
                var H2 = EvaluateAtRo(b2Polar, R2) - EvaluateAtRo(up, R2);

                double lnR1 = Math.Log(R1);
                double lnR2 = Math.Log(R2);
                double a01 = H1.Get(0, false);
                double a02 = H2.Get(0, false);
                double b0 = (a02 - a01) / (lnR2 - lnR1);
                double a0 = a01 - b0 * lnR1;

                v.Add(new PolarKey(0, 0, false, false), a0);
                v.Add(new PolarKey(0, 0, false, true), b0);

                int nMax = Math.Max(H1.MaxFrequency, H2.MaxFrequency);
                for (int n = 1; n <= nMax; n++)
                {
                    foreach (bool sn in new[] { false, true })
                    {
                        double A1 = H1.Get(n, sn);
                        double A2 = H2.Get(n, sn);
                        if (Math.Abs(A1) < 1e-13 && Math.Abs(A2) < 1e-13) continue;

                        double r1n = Math.Pow(R1, n);
                        double r2n = Math.Pow(R2, n);
                        double r1mn = 1.0 / r1n;
                        double r2mn = 1.0 / r2n;
                        double det = r1n * r2mn - r2n * r1mn;
                        double an = (A1 * r2mn - A2 * r1mn) / det;
                        double bn = (r1n * A2 - r2n * A1) / det;

                        v.Add(new PolarKey(n, n, sn, false), an);
                        v.Add(new PolarKey(-n, n, sn, false), bn);
                    }
                }
            }
            else
            {
                var upDr = up.DerivativeRo();
                var b1F = EvaluateAtRo(b1Polar, R1);
                var b2F = EvaluateAtRo(b2Polar, R2);
                var up1 = EvaluateAtRo(upDr, R1);
                var up2 = EvaluateAtRo(upDr, R2);

                var dV1 = (-b1F) - up1;
                var dV2 = b2F - up2;

                double v1_0 = dV1.Get(0, false);
                double v2_0 = dV2.Get(0, false);
                double b0From1 = R1 * v1_0;
                double b0From2 = R2 * v2_0;
                double residual = Math.Abs(b0From2 - b0From1);
                if (residual > 1e-6)
                {
                    solvable = false;
                    diag = "Условие разрешимости задачи Неймана не выполнено: невязка примерно равно " + residual.ToString("G4");
                }
                double b0 = (b0From1 + b0From2) / 2.0;
                v.Add(new PolarKey(0, 0, false, true), b0);

                int nMax = Math.Max(dV1.MaxFrequency, dV2.MaxFrequency);
                for (int n = 1; n <= nMax; n++)
                {
                    foreach (bool sn in new[] { false, true })
                    {
                        double H1 = dV1.Get(n, sn);
                        double H2 = dV2.Get(n, sn);
                        if (Math.Abs(H1) < 1e-13 && Math.Abs(H2) < 1e-13) continue;

                        double r1nm1 = Math.Pow(R1, n - 1);
                        double r2nm1 = Math.Pow(R2, n - 1);
                        double r1nm = Math.Pow(R1, -n - 1);
                        double r2nm = Math.Pow(R2, -n - 1);
                        double a11 = n * r1nm1, a12 = -n * r1nm;
                        double a21 = n * r2nm1, a22 = -n * r2nm;
                        double det = a11 * a22 - a12 * a21;
                        double an = (H1 * a22 - H2 * a12) / det;
                        double bn = (a11 * H2 - a21 * H1) / det;

                        v.Add(new PolarKey(n, n, sn, false), an);
                        v.Add(new PolarKey(-n, n, sn, false), bn);
                    }
                }
            }

            return new SolutionResult
            {
                FullSolution = v + up,
                ParticularSolution = up,
                HomogeneousSolution = v,
                IsSolvable = solvable,
                Diagnostics = diag
            };
        }
    }

    public sealed class InnerDiskSolver : LaplaceSolver
    {
        public new InnerDiskDomain Domain => (InnerDiskDomain)base.Domain;

        public InnerDiskSolver(InnerDiskDomain d) : base(d) { }

        public override SolutionResult Solve(BoundaryType type, PolyXY source, PolyXY[] boundary)
        {
            if (boundary == null || boundary.Length < 1)
                throw new ArgumentException("Требуется одна граничная функция");

            var srcPolar = ShiftToPolar(source ?? PolyXY.Zero, Domain);
            var bPolar = ShiftToPolar(boundary[0] ?? PolyXY.Zero, Domain);
            var up = srcPolar.InverseLaplacian();
            double R = Domain.R;

            var v = new PolarPoly();
            string diag = string.Empty;
            bool solvable = true;

            if (type == BoundaryType.Dirichlet)
            {
                var H = EvaluateAtRo(bPolar, R) - EvaluateAtRo(up, R);
                v.Add(new PolarKey(0, 0, false, false), H.Get(0, false));

                int nMax = H.MaxFrequency;
                for (int n = 1; n <= nMax; n++)
                {
                    foreach (bool sn in new[] { false, true })
                    {
                        double A = H.Get(n, sn);
                        if (Math.Abs(A) < 1e-13) continue;
                        double an = A / Math.Pow(R, n);
                        v.Add(new PolarKey(n, n, sn, false), an);
                    }
                }
            }
            else
            {
                var upDr = up.DerivativeRo();
                var bF = EvaluateAtRo(bPolar, R);
                var upF = EvaluateAtRo(upDr, R);
                var H = bF - upF;

                double h0 = H.Get(0, false);
                if (Math.Abs(h0) > 1e-6)
                {
                    solvable = false;
                    diag = "Условие разрешимости задачи Неймана не выполнено: невязка примерно равно " + Math.Abs(h0).ToString("G4");
                }

                int nMax = H.MaxFrequency;
                for (int n = 1; n <= nMax; n++)
                {
                    foreach (bool sn in new[] { false, true })
                    {
                        double A = H.Get(n, sn);
                        if (Math.Abs(A) < 1e-13) continue;
                        double an = A / (n * Math.Pow(R, n - 1));
                        v.Add(new PolarKey(n, n, sn, false), an);
                    }
                }
            }

            return new SolutionResult
            {
                FullSolution = v + up,
                ParticularSolution = up,
                HomogeneousSolution = v,
                IsSolvable = solvable,
                Diagnostics = diag
            };
        }
    }

    public sealed class OuterDiskSolver : LaplaceSolver
    {
        public new OuterDiskDomain Domain => (OuterDiskDomain)base.Domain;

        public OuterDiskSolver(OuterDiskDomain d) : base(d) { }

        public override SolutionResult Solve(BoundaryType type, PolyXY source, PolyXY[] boundary)
        {
            if (boundary == null || boundary.Length < 1)
                throw new ArgumentException("Требуется одна граничная функция");

            var srcPolar = ShiftToPolar(source ?? PolyXY.Zero, Domain);
            var bPolar = ShiftToPolar(boundary[0] ?? PolyXY.Zero, Domain);
            var up = srcPolar.InverseLaplacian();
            double R = Domain.R;

            var v = new PolarPoly();
            string diag = string.Empty;
            bool solvable = true;

            if (!srcPolar.IsZero)
                diag += "правая часть отлична от нуля. Внешнее решение Пуассона может быть неограниченным на бесконечности";

            if (type == BoundaryType.Dirichlet)
            {
                var H = EvaluateAtRo(bPolar, R) - EvaluateAtRo(up, R);
                v.Add(new PolarKey(0, 0, false, false), H.Get(0, false));

                int nMax = H.MaxFrequency;
                for (int n = 1; n <= nMax; n++)
                {
                    foreach (bool sn in new[] { false, true })
                    {
                        double A = H.Get(n, sn);
                        if (Math.Abs(A) < 1e-13) continue;
                        double bn = A * Math.Pow(R, n);
                        v.Add(new PolarKey(-n, n, sn, false), bn);
                    }
                }
            }
            else
            {
                var upDr = up.DerivativeRo();
                var bF = EvaluateAtRo(bPolar, R);
                var upF = EvaluateAtRo(upDr, R);
                var dV = (-bF) - upF;

                double h0 = dV.Get(0, false);
                if (Math.Abs(h0) > 1e-6)
                {
                    solvable = false;
                    diag += "Условие разрешимости задачи Неймана не выполнено: невязка примерно " + Math.Abs(h0).ToString("G4");
                }

                int nMax = dV.MaxFrequency;
                for (int n = 1; n <= nMax; n++)
                {
                    foreach (bool sn in new[] { false, true })
                    {
                        double A = dV.Get(n, sn);
                        if (Math.Abs(A) < 1e-13) continue;
                        double bn = -A * Math.Pow(R, n + 1) / n;
                        v.Add(new PolarKey(-n, n, sn, false), bn);
                    }
                }
            }

            return new SolutionResult
            {
                FullSolution = v + up,
                ParticularSolution = up,
                HomogeneousSolution = v,
                IsSolvable = solvable,
                Diagnostics = diag
            };
        }
    }
}
