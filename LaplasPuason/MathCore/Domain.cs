using System;
using System.Globalization;

namespace LaplasPuason.MathCore
{
    public abstract class CircularDomain
    {
        public double CenterX { get; }
        public double CenterY { get; }

        protected CircularDomain(double cx, double cy)
        {
            CenterX = cx;
            CenterY = cy;
        }

        public abstract double RhoMin { get; }
        public abstract double RhoMax { get; }
        public abstract bool ContainsPolar(double rho);
        public abstract string Description { get; }
    }

    public sealed class RingDomain : CircularDomain
    {
        public double RInner { get; }
        public double ROuter { get; }

        public RingDomain(double cx, double cy, double rIn, double rOut) : base(cx, cy)
        {
            if (rIn <= 0) throw new ArgumentException("Внутренний радиус должен быть положительным.");
            if (rOut <= 0) throw new ArgumentException("Внешний радиус должен быть положительным.");
            if (rIn >= rOut) throw new ArgumentException("Внутренний радиус должен быть меньше внешнего.");
            RInner = rIn;
            ROuter = rOut;
        }

        public override double RhoMin => RInner;
        public override double RhoMax => ROuter;
        public override bool ContainsPolar(double rho) => rho >= RInner && rho <= ROuter;
        public override string Description =>
            string.Format(CultureInfo.InvariantCulture,
                "Кольцо с центром ({0}, {1}), R₁ = {2}, R₂ = {3}", CenterX, CenterY, RInner, ROuter);
    }

    public sealed class InnerDiskDomain : CircularDomain
    {
        public double R { get; }

        public InnerDiskDomain(double cx, double cy, double r) : base(cx, cy)
        {
            if (r <= 0) throw new ArgumentException("Радиус должен быть положительным.");
            R = r;
        }

        public override double RhoMin => 0;
        public override double RhoMax => R;
        public override bool ContainsPolar(double rho) => rho <= R;
        public override string Description =>
            string.Format(CultureInfo.InvariantCulture,
                "Внутренняя задача в круге с центром ({0}, {1}), R = {2}", CenterX, CenterY, R);
    }

    public sealed class OuterDiskDomain : CircularDomain
    {
        public double R { get; }

        public OuterDiskDomain(double cx, double cy, double r) : base(cx, cy)
        {
            if (r <= 0) throw new ArgumentException("Радиус должен быть положительным.");
            R = r;
        }

        public override double RhoMin => R;
        public override double RhoMax => 3.0 * R;
        public override bool ContainsPolar(double rho) => rho >= R;
        public override string Description =>
            string.Format(CultureInfo.InvariantCulture,
                "Внешняя задача в круге с центром ({0}, {1}), R = {2}", CenterX, CenterY, R);
    }

    public static class CircleParser
    {
        public static (double cx, double cy) ParseCenter(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                throw new ParseException("Не указан центр окружности.");
            var parts = s.Split(',', ';');
            if (parts.Length != 2)
                throw new ParseException("Центр должен быть задан в виде 'x,y'.");
            if (!double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var cx) ||
                !double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var cy))
                throw new ParseException("Координаты центра должны быть числами.");
            return (cx, cy);
        }

        public static double ParseRadius(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                throw new ParseException("Не указан радиус.");
            if (!double.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var r))
                throw new ParseException("Радиус должен быть числом.");
            if (r <= 0)
                throw new ParseException("Радиус должен быть положительным.");
            return r;
        }

        public static (double cx, double cy, double r) ParseImplicitCircle(string equation)
        {
            var (left, right) = PolyParser.ParseEquation(equation);
            var poly = left - right;

            double cxx = poly.Coefficient(2, 0);
            double cyy = poly.Coefficient(0, 2);
            double cxy = poly.Coefficient(1, 1);

            foreach (var t in poly.Terms)
            {
                if (t.Key.P + t.Key.Q > 2)
                    throw new ParseException("Уравнение содержит члены степени выше второй — это не уравнение окружности.");
            }

            if (Math.Abs(cxx) < 1e-12)
                throw new ParseException("В уравнении отсутствуют квадратичные члены — это не уравнение окружности.");
            if (Math.Abs(cxy) > 1e-9)
                throw new ParseException("В уравнении присутствует член xy — это не окружность, а эллипс или гипербола.");
            if (Math.Abs(cxx - cyy) > 1e-9)
                throw new ParseException("Коэффициенты при x² и y² различны — это не уравнение окружности.");

            double a = poly.Coefficient(1, 0) / cxx;
            double b = poly.Coefficient(0, 1) / cxx;
            double c = poly.Coefficient(0, 0) / cxx;

            double cxOut = -a / 2.0;
            double cyOut = -b / 2.0;
            double r2 = cxOut * cxOut + cyOut * cyOut - c;
            if (r2 <= 1e-12)
                throw new ParseException("Получился неположительный квадрат радиуса. Проверьте уравнение.");

            return (cxOut, cyOut, Math.Sqrt(r2));
        }
    }
}
