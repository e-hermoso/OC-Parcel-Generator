using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParcelCreation
{
    public class SegmentLineClass
    {
        public int oid { get; set; }
        public string shapeType { get; set; }
        public double start_x { get; set; }
        public double start_y { get; set; }
        public double end_x { get; set; }
        public double end_y { get; set; }
        public double mid_x { get; set; }
        public double mid_y { get; set; }
        public double length { get; set; }
        public double azimuth_DD { get; set;}
        public double bearing_DD { get; set; }
        public string bearing_DMS { get; set; }
    }
    public class SegmentCurveClass
    {
        public int oid { get; set; }
        public string shapeType { get; set; }
        public double start_x { get; set; }
        public double start_y { get; set; }
        public double center_x { get; set; }
        public double center_y { get; set; }
        public double end_x { get; set; }
        public double end_y { get; set; }
        public double arcLength { get; set; }
        public double arcRadius { get; set; }
        public double arcDelta_DD { get; set; }
        public string arcDelta_DMS { get; set; }
        public double mid_x { get; set; }
        public double mid_y { get; set; }
        public bool isClockwise { get; set; }
        public double azimuthRadiusIn_DD { get; set; }
        public double bearingRadiusIn_DD { get; set; }
        public string bearingRadiusIn_DMS { get; set; }
        public double azimuthRadiusOut_DD { get; set; }
        public double bearingRadiusOut_DMS { get; set; }
        public double chordAzimuth_DD { get; set; }
        public double chordBearing_DD { get; set; }
        public double midAzimuth_DD { get; set; }
        public string radtangent_start { get; set; }
        public string radtangent_end { get; set; }
    }
    public class RowDataClass
    {
        public int pointNumber { get; set; }
        public bool startFig { get; set; }
        public bool endFig { get; set; }
        public bool startCurve { get; set; }
        public bool endCurve { get; set; }
        public double start_x { get; set; }
        public double start_y { get; set; }
        public double start_z { get; set; }
        //public double end_x { get; set; }
        //public double end_y { get; set; }
        public double radius { get; set; }
        public bool isCounterClockWise { get; set; }
        //public bool isHeader { get; set; }
        public object pointCheck { get; set; }
        public string errorMsg { get; set; }
    }
    public class ParcelClass
    {

    }
}
