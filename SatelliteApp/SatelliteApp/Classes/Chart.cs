using ScottPlot;
using ScottPlot.Plottable;

namespace SatelliteApp.Classes
{
    public class Chart
    {
        public double[] Data;
        public SignalPlot SignalPlot = new SignalPlot();
        public int DataIndex;
        public WpfPlot wpfPlot;
    }
}
