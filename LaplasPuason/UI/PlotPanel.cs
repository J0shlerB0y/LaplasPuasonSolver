using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace LaplasPuason.UI
{
    public sealed class PlotSeries
    {
        public List<PlotPoint> Points = new List<PlotPoint>();
        public Color Color = Color.Blue;
        public float MarkerSize = 3.0f;
        public string Label;
    }

    public struct PlotPoint
    {
        public double X, Y, Z;
        public PlotPoint(double x, double y, double z) { X = x; Y = y; Z = z; }
    }

    public sealed class PlotPanel : Panel
    {
        public List<PlotSeries> Series { get; } = new List<PlotSeries>();
        public string XLabel { get; set; } = "x";
        public string YLabel { get; set; } = "y";
        public string ZLabel { get; set; } = "u";
        public string Title { get; set; } = string.Empty;

        private double _yawDeg = 35.0;
        private double _pitchDeg = 22.0;

        public PlotPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            ResizeRedraw = true;
        }

        public void ClearSeries()
        {
            Series.Clear();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            if (Series.Count == 0 || AllEmpty())
            {
                using (var f = new Font("Segoe UI", 10f, FontStyle.Italic))
                using (var br = new SolidBrush(Color.Gray))
                    g.DrawString("Нет данных для отображения", f, br, 12, 12);
                return;
            }

            var (minX, maxX, minY, maxY, minZ, maxZ) = ComputeBounds();
            if (Math.Abs(maxX - minX) < 1e-12) { maxX += 1; minX -= 1; }
            if (Math.Abs(maxY - minY) < 1e-12) { maxY += 1; minY -= 1; }
            if (Math.Abs(maxZ - minZ) < 1e-12) { maxZ += 1; minZ -= 1; }

            int margin = 20;
            int w = ClientSize.Width - 2 * margin;
            int h = ClientSize.Height - 2 * margin;
            if (w <= 50 || h <= 50) return;
            int cx = ClientSize.Width / 2;
            int cy = ClientSize.Height / 2 + 10;

            double yaw = _yawDeg * Math.PI / 180.0;
            double pitch = _pitchDeg * Math.PI / 180.0;
            double cosY = Math.Cos(yaw), sinY = Math.Sin(yaw);
            double cosP = Math.Cos(pitch), sinP = Math.Sin(pitch);

            double midX = (minX + maxX) * 0.5;
            double midY = (minY + maxY) * 0.5;
            double midZ = (minZ + maxZ) * 0.5;

            double rangeX = maxX - minX;
            double rangeY = maxY - minY;
            double rangeZ = maxZ - minZ;
            double rangeXY = Math.Max(rangeX, rangeY);

            if (rangeXY < 1e-12) rangeXY = 1.0;

            double zScaleVis = (rangeZ > 1e-12) ? (rangeXY / rangeZ) * 0.7 : 1.0;

            Func<double, double, double, PointF> project = (x, y, z) =>
            {
                double xc = x - midX;
                double yc = y - midY;
                double zc = z - midZ;

                double xRot = xc * cosY - yc * sinY;
                double yRot = xc * sinY + yc * cosY;
                double zRot = zc * zScaleVis;

                double sx = xRot;
                double sy = -(yRot * sinP) - zRot * cosP;

                double scale = Math.Min(w, h) * 0.85 / rangeXY;
                return new PointF((float)(cx + sx * scale), (float)(cy + sy * scale));
            };

            DrawAxes(g, project, minX, maxX, minY, maxY, minZ, maxZ);

            using (var titleFont = new Font("Segoe UI", 11f, FontStyle.Bold))
            using (var br = new SolidBrush(Color.FromArgb(50, 50, 50)))
            {
                if (!string.IsNullOrEmpty(Title))
                {
                    var sz = g.MeasureString(Title, titleFont);
                    g.DrawString(Title, titleFont, br, (ClientSize.Width - sz.Width) / 2, 8);
                }
            }

            foreach (var s in Series)
            {
                using (var br = new SolidBrush(s.Color))
                {
                    foreach (var p in s.Points)
                    {
                        var pt = project(p.X, p.Y, p.Z);
                        if (float.IsNaN(pt.X) || float.IsInfinity(pt.X)) continue;
                        g.FillEllipse(br, pt.X - s.MarkerSize, pt.Y - s.MarkerSize, s.MarkerSize * 2, s.MarkerSize * 2);
                    }
                }
            }

            DrawLegend(g);
        }

        private void DrawAxes(Graphics g, Func<double, double, double, PointF> project,
                              double minX, double maxX, double minY, double maxY, double minZ, double maxZ)
        {
            using (var pen = new Pen(Color.FromArgb(120, 120, 120), 1.0f))
            using (var thinPen = new Pen(Color.FromArgb(200, 200, 200), 1.0f) { DashStyle = DashStyle.Dot })
            using (var lblFont = new Font("Segoe UI", 8.5f))
            using (var lblBrush = new SolidBrush(Color.FromArgb(80, 80, 80)))
            {
                double midZ = (minZ + maxZ) * 0.5;

                int gridN = 4;
                for (int i = 0; i <= gridN; i++)
                {
                    double x = minX + (maxX - minX) * i / (double)gridN;
                    g.DrawLine(thinPen, project(x, minY, minZ), project(x, maxY, minZ));
                    double y = minY + (maxY - minY) * i / (double)gridN;
                    g.DrawLine(thinPen, project(minX, y, minZ), project(maxX, y, minZ));
                    double z = minZ + (maxZ - minZ) * i / (double)gridN;
                    g.DrawLine(thinPen, project(minX, minY, z), project(maxX, minY, z));
                    g.DrawLine(thinPen, project(minX, minY, z), project(minX, maxY, z));
                }

                g.DrawLine(pen, project(minX, minY, minZ), project(maxX, minY, minZ));
                g.DrawLine(pen, project(minX, minY, minZ), project(minX, maxY, minZ));
                g.DrawLine(pen, project(minX, minY, minZ), project(minX, minY, maxZ));

                int ticks = 4;
                for (int i = 0; i <= ticks; i++)
                {
                    double x = minX + (maxX - minX) * i / (double)ticks;
                    var p = project(x, minY, minZ);
                    g.DrawString(x.ToString("0.##", CultureInfo.InvariantCulture), lblFont, lblBrush, p.X - 10, p.Y + 4);
                }
                for (int i = 0; i <= ticks; i++)
                {
                    double y = minY + (maxY - minY) * i / (double)ticks;
                    var p = project(minX, y, minZ);
                    g.DrawString(y.ToString("0.##", CultureInfo.InvariantCulture), lblFont, lblBrush, p.X - 28, p.Y - 6);
                }
                for (int i = 0; i <= ticks; i++)
                {
                    double z = minZ + (maxZ - minZ) * i / (double)ticks;
                    var p = project(minX, minY, z);
                    g.DrawString(z.ToString("0.##", CultureInfo.InvariantCulture), lblFont, lblBrush, p.X - 35, p.Y - 7);
                }

                using (var axFont = new Font("Segoe UI", 10f, FontStyle.Bold))
                {
                    var px = project(maxX, minY, minZ);
                    g.DrawString(XLabel, axFont, lblBrush, px.X - 4, px.Y + 18);

                    var py = project(minX, maxY, minZ);
                    g.DrawString(YLabel, axFont, lblBrush, py.X - 18, py.Y - 4);

                    var pz = project(minX, minY, maxZ);
                    g.DrawString(ZLabel, axFont, lblBrush, pz.X - 10, pz.Y - 28);
                }
            }
        }

        private void DrawLegend(Graphics g)
        {
            int x = 12;
            int y = 12;
            using (var f = new Font("Segoe UI", 8.5f))
            using (var bg = new SolidBrush(Color.FromArgb(220, 255, 255, 255)))
            using (var border = new Pen(Color.FromArgb(180, 180, 180)))
            {
                int boxW = 0;
                int rows = 0;
                foreach (var s in Series)
                {
                    if (string.IsNullOrEmpty(s.Label)) continue;
                    var sz = g.MeasureString(s.Label, f);
                    if (sz.Width > boxW) boxW = (int)sz.Width;
                    rows++;
                }
                if (rows == 0) return;
                int boxH = rows * 18 + 10;
                int totalW = boxW + 30;
                g.FillRectangle(bg, x - 4, y - 4, totalW, boxH);
                g.DrawRectangle(border, x - 4, y - 4, totalW, boxH);
                int row = 0;
                foreach (var s in Series)
                {
                    if (string.IsNullOrEmpty(s.Label)) continue;
                    using (var br = new SolidBrush(s.Color))
                        g.FillEllipse(br, x, y + row * 18 + 5, 10, 10);
                    using (var br = new SolidBrush(Color.Black))
                        g.DrawString(s.Label, f, br, x + 14, y + row * 18 + 1);
                    row++;
                }
            }
        }

        private bool AllEmpty()
        {
            foreach (var s in Series) if (s.Points.Count > 0) return false;
            return true;
        }

        private (double, double, double, double, double, double) ComputeBounds()
        {
            double minX = double.PositiveInfinity, maxX = double.NegativeInfinity;
            double minY = double.PositiveInfinity, maxY = double.NegativeInfinity;
            double minZ = double.PositiveInfinity, maxZ = double.NegativeInfinity;
            foreach (var s in Series)
                foreach (var p in s.Points)
                {
                    if (double.IsNaN(p.X) || double.IsInfinity(p.X)) continue;
                    if (double.IsNaN(p.Y) || double.IsInfinity(p.Y)) continue;
                    if (double.IsNaN(p.Z) || double.IsInfinity(p.Z)) continue;
                    if (p.X < minX) minX = p.X;
                    if (p.X > maxX) maxX = p.X;
                    if (p.Y < minY) minY = p.Y;
                    if (p.Y > maxY) maxY = p.Y;
                    if (p.Z < minZ) minZ = p.Z;
                    if (p.Z > maxZ) maxZ = p.Z;
                }
            if (double.IsInfinity(minX)) { minX = -1; maxX = 1; minY = -1; maxY = 1; minZ = -1; maxZ = 1; }
            return (minX, maxX, minY, maxY, minZ, maxZ);
        }

        private bool _dragging;
        private Point _lastDrag;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _dragging = true;
            _lastDrag = e.Location;
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _dragging = false;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_dragging) return;
            int dx = e.X - _lastDrag.X;
            int dy = e.Y - _lastDrag.Y;
            _lastDrag = e.Location;
            _yawDeg += dx * 0.5;
            _pitchDeg = Math.Max(-85, Math.Min(85, _pitchDeg + dy * 0.4));
            Invalidate();
        }
    }
}
