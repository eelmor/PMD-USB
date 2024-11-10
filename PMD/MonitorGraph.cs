using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Linq;

namespace PMD {
    public partial class MonitorGraph : UserControl {

        private DBPanel panel1;
        private List<double> values;

        public Sensor Sensor;

        public double minValue = double.MaxValue;
        public double maxValue = double.MinValue;

        private int maxWidth, maxHeight;

        private int displayX;

        private int _maxPoints = 100;

        private int paddingY = 10;
        private int paddingX = 1;

        public int MaxPoints
        {
            get
            {
                return _maxPoints;
            }
            set
            {
                if (MaxPoints > 0)
                {
                    _maxPoints = value;
                }
            }
        }

        public MonitorGraph(Sensor sensor, int width, int height) {

            InitializeComponent();

            this.Size = new Size(width, height);

            maxWidth = this.Size.Width - paddingX*2;
            maxHeight = this.Size.Height - paddingY*2;

            this.Sensor = sensor;
            sensor.SensorUpdated += Sensor_SensorUpdated;

            panel1 = new DBPanel();
            panel1.Size = this.Size;
            panel1.Location = new Point(0, 0);
            panel1.MouseMove += MonitorGraph_MouseMove;
            panel1.MouseLeave += MonitorGraph_MouseLeave;
            this.Controls.Add(panel1);

            values = new List<double>();

            panel1.Paint += new PaintEventHandler(panel1_Paint);
        }

        private void Sensor_SensorUpdated(double value)
        {
            addValue(value);
        }

        private Pen graphPen = new Pen(Color.Purple, 1);
        private Pen graphPen2 = new Pen(Color.Purple, 1);
        Font drawFont = new Font("Courier New", 8);
        Font drawFontLarge = new Font("Courier New", 14, FontStyle.Bold);
        SolidBrush drawBrush = new SolidBrush(Color.Black);
        Brush markBrush = new SolidBrush(Color.Purple);

        long time, time2;
        long timeavg;

        private void panel1_Paint(object sender, PaintEventArgs e) {

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            stopwatch.Stop();

            time = (long)(stopwatch.ElapsedTicks * 1000000.0 / Stopwatch.Frequency);
            timeavg = (timeavg * 9 + time) / 10;

            string minValueString = minValue.ToString(Sensor.Format);
            string maxValueString = maxValue.ToString(Sensor.Format);

            g.DrawString(Sensor.DescriptionLong, drawFont, drawBrush, 0, 0);
            g.DrawString($"min: {minValueString} max: {maxValueString}", drawFont, drawBrush, 150, 0);

            if (values.Count < 2) return;

            // Calculate graph points
            PointF[] graphPoints = new PointF[values.Count];

            float x_step = (float)maxWidth / (float)(MaxPoints - 1);
            for (int i = 0; i < graphPoints.Length; i++)
            {
                float y = maxHeight / 2 + paddingY;
                if (maxValue != minValue && maxValue != double.MinValue && minValue != double.MaxValue)
                {
                    y = (float)(maxHeight - (values[i] - minValue) * maxHeight / (maxValue - minValue)) + paddingY;
                }
                float x = (MaxPoints - values.Count + i) * x_step + paddingX;
                graphPoints[i] = new PointF(x, y);
            }

            g.DrawLines(graphPen, graphPoints);

            // Find closest graphPoint to displayX
            int x_sel = -1;
            for (int i = 0; i < graphPoints.Length; i++)
            {
                if (graphPoints[i].X >= displayX)
                {
                    x_sel = i;
                    break;
                }
            }

            string valueString;

            if (x_sel > 0 && x_sel < values.Count) {
                valueString = values[x_sel].ToString(Sensor.Format);
                g.FillEllipse(markBrush, displayX - 3, graphPoints[x_sel].Y - 3, 6, 6);
            } else
            {
                valueString = values.Last().ToString(Sensor.Format);
            }

            g.DrawString(String.Format("{0,10}", valueString), drawFontLarge, markBrush, new Point(this.Size.Width - 75 - 50, 0));

        }

        public void addValue(double val) {

            values.Add(val);

            while(values.Count > MaxPoints)
            {
                values.RemoveAt(0);
            }

            maxValue = values.Max();
            minValue = values.Min();

        }

        private void MonitorGraph_MouseMove(object sender, MouseEventArgs e) {
            displayX = e.X;
        }
        private void MonitorGraph_MouseLeave(object sender, EventArgs e) {
            displayX = -1;
        }

        public void Redraw()
        {
            panel1.Invalidate();
        }
    }
}
