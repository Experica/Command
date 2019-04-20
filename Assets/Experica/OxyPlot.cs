using UnityEngine;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using System;
using System.IO;
using OxyPlot;
using OxyPlot.WindowsForms;
using OxyPlot.Series;

namespace Experica
{
    public class OxyPlotForm : Form
    {
        protected OxyPlotControl plotview = new OxyPlotControl();

        public OxyPlotForm(int width = 300, int height = 280)
        {
            Width = width;
            Height = height;
            Controls.Add(plotview);
        }

        public void Reset()
        {
            plotview.Reset();
        }

        public void ShowInFront()
        {
            if (Visible)
            {
                WindowState = FormWindowState.Minimized;
                Show();
                WindowState = FormWindowState.Normal;
            }
        }

        public Vector2 Position
        {
            get { return new Vector2(Location.X, Location.Y); }
            set
            {
                if (StartPosition != FormStartPosition.Manual)
                {
                    StartPosition = FormStartPosition.Manual;
                }
                Location = new System.Drawing.Point(Convert.ToInt32(value.x), Convert.ToInt32(value.y));
            }
        }

        public virtual void Visualize(object data)
        {
            if (!Visible)
            {
                Show();
            }
        }

        public void Save(string path, int width, int height, int dpi)
        {
            plotview.Save(path, width, height, dpi);
        }
    }

    public class OxyPlotControl : PlotView
    {
        bool isupdated;
        double ylo = double.NaN, yhi = double.NaN, xlo = double.NaN, xhi = double.NaN;

        public OxyPlotControl()
        {
            Model = new PlotModel
            {
                LegendPosition = LegendPosition.RightTop,
                LegendPlacement = LegendPlacement.Inside,
                PlotType = PlotType.XY
            };

            ContextMenuStrip = new ContextMenuStrip();
            GenerateContextMenuStrip();
            Dock = DockStyle.Fill;
        }

        public void Reset()
        {
            Visible = false;
            Parent.Visible = false;
            isupdated = false;
            Model.Series.Clear();
            ylo = double.NaN; yhi = double.NaN; xlo = double.NaN; xhi = double.NaN;

            ContextMenuStrip.Items.Clear();
        }

        void GenerateContextMenuStrip()
        {
            var save = new ToolStripMenuItem("Save");
            save.Click += Save_Click;

            ContextMenuStrip.Items.Add(save);
        }

        void Save_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        public void Visualize(double[] x, Dictionary<string, double[]> y, Dictionary<string, double[]> yse,
            Dictionary<string, OxyColor> color, Dictionary<string, Type> seriestype, Dictionary<string, double> linewidth, Dictionary<string, LineStyle> linestyle,
            string title = "", string xtitle = "", string ytitle = "", bool isclear = true, LegendPosition legendposition = LegendPosition.RightTop)
        {
            Visualize(y.ToDictionary(i => i.Key, i => x), y, yse, color, seriestype, linewidth, linestyle, title, xtitle, ytitle, isclear);
        }

        public void Visualize(Dictionary<string, double[]> x, Dictionary<string, double[]> y, Dictionary<string, double[]> yse,
            Dictionary<string, OxyColor> color, Dictionary<string, Type> seriestype, Dictionary<string, double> linewidth, Dictionary<string, LineStyle> linestyle,
            string title = "", string xtitle = "", string ytitle = "", bool isclear = true, LegendPosition legendposition = LegendPosition.RightTop)
        {
            if (!Visible)
            {
                Visible = true;
                Parent.Visible = true;
            }
            if (isclear)
            {
                Model.Series.Clear();
                ylo = double.NaN; yhi = double.NaN; xlo = double.NaN; xhi = double.NaN;
            }

            foreach (var u in y.Keys.ToList())
            {
                var point = new ScatterSeries()
                {
                    Title = u,
                    MarkerSize = linewidth[u],
                    MarkerFill = color[u],
                    MarkerStrokeThickness = 0,
                    MarkerType = MarkerType.Circle,
                    TrackerFormatString = "{0}\nX: {2:0.0}\nY: {4:0.0}"
                };
                var line = new LineSeries()
                {
                    Title = u,
                    StrokeThickness = linewidth[u],
                    Color = color[u],
                    LineStyle = linestyle[u],
                    TrackerFormatString = "{0}\nX: {2:0.0}\nY: {4:0.0}"
                };
                var error = new ScatterErrorSeries()
                {
                    ErrorBarStopWidth = 2,
                    ErrorBarStrokeThickness = linewidth[u],
                    ErrorBarColor = OxyColor.FromAColor(180, line.Color),
                    MarkerSize = 0,
                    TrackerFormatString = "{0}\nX: {2:0.0}\nY: {4:0.0}"
                };

                for (var i = 0; i < x[u].Length; i++)
                {
                    if (seriestype[u] == typeof(ScatterSeries))
                    {
                        point.Points.Add(new ScatterPoint(x[u][i], y[u][i]));
                    }
                    else if (seriestype[u] == typeof(LineSeries))
                    {
                        line.Points.Add(new DataPoint(x[u][i], y[u][i]));
                    }
                    var cyse = 0.0;
                    if (yse != null && yse.ContainsKey(u))
                    {
                        error.Points.Add(new ScatterErrorPoint(x[u][i], y[u][i], 0, yse[u][i]));
                        cyse = yse[u][i];
                    }
                    if (yhi == double.NaN)
                    {
                        yhi = y[u][i] + cyse;
                        ylo = y[u][i] - cyse;
                    }
                    if (xhi == double.NaN)
                    {
                        xhi = x[u][i];
                        xlo = x[u][i];
                    }
                    yhi = Math.Max(yhi, y[u][i] + cyse);
                    ylo = Math.Min(ylo, y[u][i] - cyse);
                    xhi = Math.Max(xhi, x[u][i]);
                    xlo = Math.Min(xlo, x[u][i]);
                }
                if (point.Points.Count > 0)
                {
                    Model.Series.Add(point);
                }
                if (line.Points.Count > 0)
                {
                    Model.Series.Add(line);
                }
                if (error.Points.Count > 0)
                {
                    Model.Series.Add(error);
                }
            }

            if (Model.DefaultXAxis != null)
            {
                Model.DefaultXAxis.Maximum = xhi + 0.01 * (xhi - xlo);
                Model.DefaultXAxis.Minimum = xlo - 0.01 * (xhi - xlo);
                Model.DefaultYAxis.Maximum = yhi + 0.01 * (yhi - ylo);
                Model.DefaultYAxis.Minimum = ylo - 0.01 * (yhi - ylo);
                Model.DefaultXAxis.Reset();
                Model.DefaultYAxis.Reset();
            }

            if (!isupdated)
            {
                if (Model.DefaultXAxis != null)
                {
                    Model.DefaultXAxis.MaximumPadding = 0.005;
                    Model.DefaultXAxis.MinimumPadding = 0.005;
                    Model.DefaultYAxis.MaximumPadding = 0.005;
                    Model.DefaultYAxis.MinimumPadding = 0.005;
                    Model.DefaultXAxis.TickStyle = OxyPlot.Axes.TickStyle.Outside;
                    Model.DefaultXAxis.Title = xtitle;
                    Model.DefaultYAxis.TickStyle = OxyPlot.Axes.TickStyle.Outside;
                    Model.DefaultYAxis.Title = ytitle;
                    Model.Title = title;
                    Model.LegendPosition = legendposition;
                    isupdated = true;
                }
            }
            Model.InvalidatePlot(true);
        }

        public void Save(string path, int width, int height, int dpi)
        {
            using (var stream = File.Create(path + ".png"))
            {
                var pngexporter = new OxyPlot.WindowsForms.PngExporter
                {
                    Width = width,
                    Height = height,
                    Resolution = dpi
                };
                pngexporter.Export(Model, stream);
            }
            using (var stream = File.Create(path + ".svg"))
            {
                var svgexporter = new OxyPlot.WindowsForms.SvgExporter
                {
                    Width = width,
                    Height = height,
                    IsDocument = true
                };
                svgexporter.Export(Model, stream);
            }
        }

    }
}