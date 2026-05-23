using System.Drawing;
using System.Windows.Forms;
using LaplasPuason.UI;

namespace LaplasPuason
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private Label lblHeader;
        private RadioButton rbRing;
        private RadioButton rbInnerDisk;
        private RadioButton rbOuterDisk;

        private Label lblParamsHeader;
        private RadioButton rbExplicit;
        private RadioButton rbImplicit;

        private Label lblCenter;
        private TextBox txtCenter;

        private Label lblSource;
        private TextBox txtSource;

        private Label lblRadius1;
        private TextBox txtRadius1;

        private Label lblRadius2;
        private TextBox txtRadius2;

        private Label lblBoundary1;
        private TextBox txtBoundary1;

        private Label lblBoundary2;
        private TextBox txtBoundary2;

        private Label lblDecimals;
        private TextBox txtDecimals;

        private Button btnDirichlet;
        private Button btnNeumann;
        private Button btnPlot;

        private TextBox txtSolution;
        private PlotPanel plotPanel;

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            SuspendLayout();

            Font baseFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            Font headerFont = new Font("Segoe UI", 11f, FontStyle.Regular);
            Color labelColor = Color.FromArgb(30, 30, 30);
            Color subtleBg = Color.FromArgb(248, 248, 250);

            this.Font = baseFont;
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1240, 900);
            this.Text = "Решение задачи Лапласа и Пуассона";
            this.BackColor = Color.White;
            this.MinimumSize = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            int colWidth = 380;
            int colGap = 30;
            int col1 = 30;
            int col2 = col1 + colWidth + colGap;
            int col3 = col2 + colWidth + colGap;

            lblHeader = new Label
            {
                Text = "Выберите тип задачи",
                Font = headerFont,
                ForeColor = labelColor,
                AutoSize = true,
                Location = new Point((1240 - 200) / 2, 14)
            };

            rbRing = new RadioButton
            {
                Text = "кольцо",
                Location = new Point(col1 + 90, 44),
                AutoSize = true,
                Checked = true
            };
            rbInnerDisk = new RadioButton
            {
                Text = "круг, внутренняя задача",
                Location = new Point(col2 + 60, 44),
                AutoSize = true
            };
            rbOuterDisk = new RadioButton
            {
                Text = "круг, внешняя задача",
                Location = new Point(col3 + 60, 44),
                AutoSize = true
            };

            lblParamsHeader = new Label
            {
                Text = "Параметры окружности заданы:",
                Font = headerFont,
                AutoSize = true,
                Location = new Point((1240 - 280) / 2, 78)
            };

            rbExplicit = new RadioButton
            {
                Text = "явно",
                Location = new Point(col1 + 110, 108),
                AutoSize = true,
                Checked = true
            };
            rbImplicit = new RadioButton
            {
                Text = "неявно",
                Location = new Point(col2 + 100, 108),
                AutoSize = true
            };

            int rowY1Label = 138;
            int rowY1Field = 158;
            int rowY2Label = 188;
            int rowY2Field = 208;
            int rowY3Label = 238;
            int rowY3Field = 258;

            lblCenter = new Label { Text = "Введите центр окружности", AutoSize = true, Location = new Point(col1, rowY1Label) };
            txtCenter = new TextBox { Location = new Point(col1, rowY1Field), Size = new Size(colWidth - 200, 24), Text = "0,0" };

            lblRadius1 = new Label { Text = "Введите радиус внутр. окружности", AutoSize = true, Location = new Point(col2, rowY1Label) };
            txtRadius1 = new TextBox { Location = new Point(col2, rowY1Field), Size = new Size(colWidth - 200, 24), Text = "2" };

            lblBoundary1 = new Label { Text = "Введите функцию на внутр. границе", AutoSize = true, Location = new Point(col3, rowY1Label) };
            txtBoundary1 = new TextBox { Location = new Point(col3, rowY1Field), Size = new Size(colWidth, 24), Text = "x^2-2*y^2+y-1" };

            lblSource = new Label { Text = "Введите функцию неоднородности", AutoSize = true, Location = new Point(col1, rowY2Label) };
            txtSource = new TextBox { Location = new Point(col1, rowY2Field), Size = new Size(colWidth, 24), Text = "-6*(x^2-y^2)" };

            lblRadius2 = new Label { Text = "Введите радиус внеш. окружности", AutoSize = true, Location = new Point(col2, rowY2Label) };
            txtRadius2 = new TextBox { Location = new Point(col2, rowY2Field), Size = new Size(colWidth - 200, 24), Text = "3" };

            lblBoundary2 = new Label { Text = "Введите функцию на внеш. границе", AutoSize = true, Location = new Point(col3, rowY2Label) };
            txtBoundary2 = new TextBox { Location = new Point(col3, rowY2Field), Size = new Size(colWidth, 24), Text = "2*x^2+4*y^2+x-25" };

            lblDecimals = new Label { Text = "Число знаков после запятой", AutoSize = true, Location = new Point(col3, rowY3Label) };
            txtDecimals = new TextBox { Location = new Point(col3, rowY3Field), Size = new Size(60, 24), Text = "4" };

            btnDirichlet = new Button
            {
                Text = "Решение задачи Дирихле",
                Location = new Point(col1, rowY3Field - 6),
                Size = new Size(220, 36),
                BackColor = Color.FromArgb(60, 80, 220),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular)
            };
            btnDirichlet.FlatAppearance.BorderSize = 0;

            btnNeumann = new Button
            {
                Text = "Решение задачи Неймана",
                Location = new Point(col2, rowY3Field - 6),
                Size = new Size(220, 36),
                BackColor = Color.FromArgb(60, 80, 220),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular)
            };
            btnNeumann.FlatAppearance.BorderSize = 0;

            btnPlot = new Button
            {
                Text = "Показать график",
                Location = new Point(col3 + colWidth - 220, rowY3Field + 50),
                Size = new Size(220, 36),
                BackColor = Color.FromArgb(60, 80, 220),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular)
            };
            btnPlot.FlatAppearance.BorderSize = 0;

            int solutionY = rowY3Field + 50;
            txtSolution = new TextBox
            {
                Location = new Point(col1, solutionY),
                Size = new Size(2 * colWidth + colGap, 100),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 10f),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            int plotY = solutionY + 120;
            plotPanel = new PlotPanel
            {
                Location = new Point(col1, plotY),
                Size = new Size(1240 - 2 * col1, 900 - plotY - 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BorderStyle = BorderStyle.FixedSingle,
                Title = "Граничные условия и решение"
            };

            Controls.Add(lblHeader);
            Controls.Add(rbRing);
            Controls.Add(rbInnerDisk);
            Controls.Add(rbOuterDisk);
            Controls.Add(lblParamsHeader);
            Controls.Add(rbExplicit);
            Controls.Add(rbImplicit);
            Controls.Add(lblCenter); Controls.Add(txtCenter);
            Controls.Add(lblSource); Controls.Add(txtSource);
            Controls.Add(lblRadius1); Controls.Add(txtRadius1);
            Controls.Add(lblRadius2); Controls.Add(txtRadius2);
            Controls.Add(lblBoundary1); Controls.Add(txtBoundary1);
            Controls.Add(lblBoundary2); Controls.Add(txtBoundary2);
            Controls.Add(lblDecimals); Controls.Add(txtDecimals);
            Controls.Add(btnDirichlet);
            Controls.Add(btnNeumann);
            Controls.Add(btnPlot);
            Controls.Add(txtSolution);
            Controls.Add(plotPanel);

            ResumeLayout(false);
            PerformLayout();
        }
    }
}
