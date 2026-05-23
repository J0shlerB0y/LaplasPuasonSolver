using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using LaplasPuason.MathCore;
using LaplasPuason.UI;

namespace LaplasPuason
{
    public partial class Form1 : Form
    {
        private SolutionResult _lastResult;
        private CircularDomain _lastDomain;
        private BoundaryType _lastType;
        private PolyXY _lastBoundary1;
        private PolyXY _lastBoundary2;

        public Form1()
        {
            InitializeComponent();

            rbRing.CheckedChanged += (s, e) => UpdateFieldVisibility();
            rbInnerDisk.CheckedChanged += (s, e) => UpdateFieldVisibility();
            rbOuterDisk.CheckedChanged += (s, e) => UpdateFieldVisibility();
            rbExplicit.CheckedChanged += (s, e) => UpdateFieldVisibility();
            rbImplicit.CheckedChanged += (s, e) => UpdateFieldVisibility();

            btnDirichlet.Click += (s, e) => Solve(BoundaryType.Dirichlet);
            btnNeumann.Click += (s, e) => Solve(BoundaryType.Neumann);
            btnPlot.Click += (s, e) => RenderPlot();

            UpdateFieldVisibility();
        }

        private void UpdateFieldVisibility()
        {
            bool isRing = rbRing.Checked;
            bool isInner = rbInnerDisk.Checked;
            bool isOuter = rbOuterDisk.Checked;
            bool isImplicit = rbImplicit.Checked;

            lblCenter.Visible = !isImplicit;
            txtCenter.Visible = !isImplicit;

            lblRadius1.Visible = true;
            txtRadius1.Visible = true;
            lblRadius2.Visible = isRing;
            txtRadius2.Visible = isRing;

            lblBoundary1.Visible = isRing;
            txtBoundary1.Visible = isRing;

            if (isImplicit)
            {
                if (isRing)
                {
                    lblRadius1.Text = "Уравнение внутр. окружности";
                    lblRadius2.Text = "Уравнение внеш. окружности";
                }
                else
                {
                    lblRadius1.Text = "Уравнение окружности";
                }
            }
            else
            {
                if (isRing)
                {
                    lblRadius1.Text = "Введите радиус внутр. окружности";
                    lblRadius2.Text = "Введите радиус внеш. окружности";
                }
                else
                {
                    lblRadius1.Text = "Введите радиус окружности";
                }
            }

            if (isRing)
            {
                lblBoundary2.Text = "Введите функцию на внеш. границе";
            }
            else
            {
                lblBoundary2.Text = "Введите функцию на границе";
            }
        }

        private CircularDomain BuildDomain()
        {
            bool isImplicit = rbImplicit.Checked;
            if (rbRing.Checked)
            {
                if (isImplicit)
                {
                    var (cx1, cy1, r1) = CircleParser.ParseImplicitCircle(txtRadius1.Text);
                    var (cx2, cy2, r2) = CircleParser.ParseImplicitCircle(txtRadius2.Text);
                    if (Math.Abs(cx1 - cx2) > 1e-6 || Math.Abs(cy1 - cy2) > 1e-6)
                        throw new ParseException("Центры внутренней и внешней окружностей не совпадают.");
                    if (r1 >= r2) throw new ParseException("Внутренний радиус должен быть меньше внешнего.");
                    return new RingDomain(cx1, cy1, r1, r2);
                }
                else
                {
                    var (cx, cy) = CircleParser.ParseCenter(txtCenter.Text);
                    double r1 = CircleParser.ParseRadius(txtRadius1.Text);
                    double r2 = CircleParser.ParseRadius(txtRadius2.Text);
                    return new RingDomain(cx, cy, r1, r2);
                }
            }
            if (rbInnerDisk.Checked)
            {
                if (isImplicit)
                {
                    var (cx, cy, r) = CircleParser.ParseImplicitCircle(txtRadius1.Text);
                    return new InnerDiskDomain(cx, cy, r);
                }
                else
                {
                    var (cx, cy) = CircleParser.ParseCenter(txtCenter.Text);
                    double r = CircleParser.ParseRadius(txtRadius1.Text);
                    return new InnerDiskDomain(cx, cy, r);
                }
            }
            if (rbOuterDisk.Checked)
            {
                if (isImplicit)
                {
                    var (cx, cy, r) = CircleParser.ParseImplicitCircle(txtRadius1.Text);
                    return new OuterDiskDomain(cx, cy, r);
                }
                else
                {
                    var (cx, cy) = CircleParser.ParseCenter(txtCenter.Text);
                    double r = CircleParser.ParseRadius(txtRadius1.Text);
                    return new OuterDiskDomain(cx, cy, r);
                }
            }
            throw new InvalidOperationException("Тип задачи не выбран.");
        }

        private int GetDecimals()
        {
            if (!int.TryParse(txtDecimals.Text.Trim(), out var d) || d < 0 || d > 12)
                return 4;
            return d;
        }

        private void Solve(BoundaryType type)
        {
            try
            {
                var domain = BuildDomain();
                var source = string.IsNullOrWhiteSpace(txtSource.Text) ? PolyXY.Zero : PolyParser.Parse(txtSource.Text);
                PolyXY[] boundary;
                PolyXY b1 = null, b2 = null;
                if (domain is RingDomain)
                {
                    b1 = PolyParser.Parse(txtBoundary1.Text);
                    b2 = PolyParser.Parse(txtBoundary2.Text);
                    boundary = new[] { b1, b2 };
                }
                else
                {
                    b2 = PolyParser.Parse(txtBoundary2.Text);
                    boundary = new[] { b2 };
                }

                var solver = LaplaceSolver.Create(domain);
                var result = solver.Solve(type, source, boundary);

                _lastResult = result;
                _lastDomain = domain;
                _lastType = type;
                _lastBoundary1 = b1;
                _lastBoundary2 = b2;

                int decimals = GetDecimals();
                string output = result.FullSolution.Format(decimals);
                if (!result.ParticularSolution.IsZero)
                {
                    output += Environment.NewLine + "—" + Environment.NewLine + result.ParticularSolution.Format(decimals);
                }
                if (!string.IsNullOrEmpty(result.Diagnostics))
                {
                    output = "[" + result.Diagnostics.Trim() + "]" + Environment.NewLine + output;
                }
                if (!result.IsSolvable)
                {
                    output = "Решение не существует в классе ограниченных функций." + Environment.NewLine + output;
                }
                txtSolution.Text = output;

                RenderPlot();
            }
            catch (ParseException ex)
            {
                MessageBox.Show(this, ex.Message, "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(this, ex.Message, "Ошибка геометрии", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RenderPlot()
        {
            plotPanel.ClearSeries();
            if (_lastResult == null || _lastDomain == null) { plotPanel.Invalidate(); return; }

            bool isNeumann = _lastType == BoundaryType.Neumann;
            plotPanel.ZLabel = isNeumann ? "∂u/∂ρ" : "u(x, y)";
            plotPanel.Title = isNeumann
                ? "Радиальная производная решения и граничные условия"
                : "Решение и граничные условия";

            PolarPoly displayed = isNeumann
                ? _lastResult.FullSolution.DerivativeRho()
                : _lastResult.FullSolution;

            var solutionSeries = new PlotSeries
            {
                Color = Color.FromArgb(40, 70, 200),
                MarkerSize = 2.0f,
                Label = isNeumann ? "∂u/∂ρ внутри области" : "u(x, y) внутри области"
            };

            int rhoSteps = 50;
            int phiSteps = 120;
            double rhoMin = _lastDomain.RhoMin;
            double rhoMax = _lastDomain.RhoMax;
            if (_lastDomain is InnerDiskDomain) rhoMin = Math.Max(rhoMin, 1e-6);

            for (int i = 0; i <= rhoSteps; i++)
            {
                double rho = rhoMin + (rhoMax - rhoMin) * i / (double)rhoSteps;
                for (int j = 0; j < phiSteps; j++)
                {
                    double phi = 2 * Math.PI * j / (double)phiSteps;
                    double z = displayed.Evaluate(rho, phi);
                    double x = _lastDomain.CenterX + rho * Math.Cos(phi);
                    double y = _lastDomain.CenterY + rho * Math.Sin(phi);
                    if (double.IsNaN(z) || double.IsInfinity(z)) continue;
                    solutionSeries.Points.Add(new PlotPoint(x, y, z));
                }
            }
            plotPanel.Series.Add(solutionSeries);

            AddBoundaryCurve(_lastBoundary1, _lastBoundary2, isNeumann);

            plotPanel.Invalidate();
        }

        private void AddBoundaryCurve(PolyXY b1, PolyXY b2, bool isNeumann)
        {
            int phiSteps = 240;

            void AddCircle(double radius, PolyXY boundaryFunc, double sign, Color color, string label)
            {
                if (boundaryFunc == null) return;
                var s = new PlotSeries { Color = color, MarkerSize = 3.5f, Label = label };
                for (int j = 0; j < phiSteps; j++)
                {
                    double phi = 2 * Math.PI * j / (double)phiSteps;
                    double x = _lastDomain.CenterX + radius * Math.Cos(phi);
                    double y = _lastDomain.CenterY + radius * Math.Sin(phi);
                    double z = sign * boundaryFunc.Evaluate(x, y);
                    s.Points.Add(new PlotPoint(x, y, z));
                }
                plotPanel.Series.Add(s);
            }

            if (_lastDomain is RingDomain ring)
            {
                double s1 = isNeumann ? -1.0 : 1.0;
                double s2 = 1.0;
                string suffix = isNeumann ? " (≡ ∂u/∂ρ)" : string.Empty;
                AddCircle(ring.RInner, b1, s1, Color.FromArgb(0, 160, 60), "Граница ρ = R₁" + suffix);
                AddCircle(ring.ROuter, b2, s2, Color.FromArgb(220, 30, 30), "Граница ρ = R₂" + suffix);
            }
            else if (_lastDomain is InnerDiskDomain inner)
            {
                double sign = 1.0;
                string suffix = isNeumann ? " (≡ ∂u/∂ρ)" : string.Empty;
                AddCircle(inner.R, b2, sign, Color.FromArgb(220, 30, 30), "Граница ρ = R" + suffix);
            }
            else if (_lastDomain is OuterDiskDomain outer)
            {
                double sign = isNeumann ? -1.0 : 1.0;
                string suffix = isNeumann ? " (≡ ∂u/∂ρ)" : string.Empty;
                AddCircle(outer.R, b2, sign, Color.FromArgb(220, 30, 30), "Граница ρ = R" + suffix);
            }
        }
    }
}
