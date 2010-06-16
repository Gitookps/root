using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using SharpMap;
using SharpMap.Data;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;

namespace WinFormSamples.Samples
{
    public static partial class ShapefileSample
    {
        //Disclaimer
        //The used GeoData is from www.openstreetmap.org licenced CC-by-SA
        //Transformed to shapefile by Geofabrik (http://www.geofabrik.de)
        private const string PathOsm = "GeoData/OSM";

        //Could have used SharpMap.Rendering.Thematics.CustomTheme,
        //but that one does not perserve styles for fast retrieval.
        //Maybe a category theme would be a good idea...
        private delegate IStyle GetStyleHandler(FeatureDataRow row);
        class ThemeViaDelegate : ITheme
        {
            public GetStyleHandler GetStyleFunction;
            private readonly IStyle _default;
            private readonly String _columnName;
            private readonly IDictionary<String, IStyle> _stylePreserver;

            public ThemeViaDelegate(IStyle defaultStyle, String columnName)
            {
                _default = defaultStyle;
                _stylePreserver = new Dictionary<string, IStyle>();
                _columnName = columnName;
            }
            

            #region Implementation of ITheme

            public IStyle GetStyle(FeatureDataRow attribute)
            {
                IStyle returnStyle;
                String value = Convert.ToString(attribute[_columnName]);
                if (!_stylePreserver.TryGetValue(value, out returnStyle))
                {
                    if (GetStyleFunction != null)
                    {
                        returnStyle = GetStyleFunction(attribute);
                        if (returnStyle == null) returnStyle = _default;
                        _stylePreserver.Add(value, returnStyle);
                    }
                    else
                        returnStyle = _default;
                }
                returnStyle.MinVisible = _default.MinVisible;
                returnStyle.MaxVisible = _default.MaxVisible;
                return returnStyle;
            }

            #endregion
        }


        private static Map InitializeMapOsm()
        {
            //Transparent style
            VectorStyle transparentStyle = new VectorStyle();
            transparentStyle.Fill = Brushes.Transparent;
            transparentStyle.EnableOutline = true; //otherwise all the fancy theming stuff won't work!
            transparentStyle.Line.Brush = Brushes.Transparent;
            transparentStyle.Outline.Brush = Brushes.Transparent;
            transparentStyle.Symbol = null;

            VectorStyle transparentStyle2 = new VectorStyle();
            transparentStyle2.Fill = Brushes.Transparent;
            transparentStyle2.EnableOutline = true; //otherwise all the fancy theming stuff won't work!
            transparentStyle2.Line.Brush = Brushes.Transparent;
            transparentStyle2.Outline.Brush = Brushes.Transparent;
            transparentStyle2.Symbol = null;

            //Initialize a new map
            Map map = new Map();
            map.BackColor = Color.Cornsilk;

            //Set up the countries layer
            VectorLayer layNatural = new VectorLayer("Natural");
            //Set the datasource to a shapefile in the App_data folder
            layNatural.DataSource = new ShapeFile(string.Format("{0}/natural.shp", PathOsm), true);
            //Set default style to draw nothing
            layNatural.Style = transparentStyle;
            //Set theme
            ThemeViaDelegate theme = new ThemeViaDelegate(layNatural.Style, "type");
            theme.GetStyleFunction = delegate(FeatureDataRow row)
                                         {
                                             VectorStyle returnStyle = new VectorStyle();
                                             switch ((String) row["type"])
                                             {
                                                 case "forest":
                                                     returnStyle.Fill = Brushes.ForestGreen;
                                                     returnStyle.EnableOutline = true;
                                                     returnStyle.Outline.Brush = Brushes.DarkGreen;
                                                     break;
                                                 case "water":
                                                     returnStyle.Fill = Brushes.Aqua;
                                                     returnStyle.EnableOutline = true;
                                                     returnStyle.Outline.Brush = Brushes.DarkBlue;
                                                     break;
                                                 case "riverbank":
                                                     returnStyle.Fill = Brushes.Peru;
                                                     returnStyle.EnableOutline = true;
                                                     returnStyle.Outline.Brush = Brushes.OrangeRed;
                                                     break;
                                                 case "park":
                                                     returnStyle.Fill = Brushes.PaleGreen;
                                                     returnStyle.EnableOutline = true;
                                                     returnStyle.Outline.Brush = Brushes.DarkGreen;
                                                     break;
                                                 default:
                                                     returnStyle = null;
                                                     break;
                                             }
                                             return returnStyle;
                                         };
            layNatural.Theme = theme;
            layNatural.SRID = 31466;

            VectorLayer layRoads = new VectorLayer("Roads");
            layRoads.DataSource = new ShapeFile(string.Format("{0}/roads.shp", PathOsm));
            layRoads.Style = transparentStyle;
            ThemeViaDelegate themeRoads = new ThemeViaDelegate(transparentStyle, "type");
            themeRoads.GetStyleFunction = delegate(FeatureDataRow row)
                                              {
                                                  VectorStyle returnStyle = new VectorStyle();

                                                  switch ((String)row["type"])
                                                  {
                                                      case "rail":
                                                          returnStyle.Fill = Brushes.White;
                                                          returnStyle.Line.DashPattern = new float[] { 4f, 4f };//;System.Drawing.Drawing2D.DashStyle.Dash;
                                                          returnStyle.Line.Width = 4;
                                                          returnStyle.EnableOutline = true;
                                                          returnStyle.Outline.Brush = Brushes.Black;
                                                          returnStyle.Outline.Width = 6;
                                                          break;
                                                      case "canal":
                                                          returnStyle.Fill = Brushes.Aqua;
                                                          returnStyle.Outline.Brush = Brushes.DarkBlue;
                                                          returnStyle.Outline.Width = 5;
                                                          break;
                                                      case "cycleway":
                                                      case "footway":
                                                      case "pedestrian":
                                                          returnStyle.Line.Brush = Brushes.DarkGray;
                                                          returnStyle.Line.DashStyle =
                                                              DashStyle.Dot;
                                                          returnStyle.Line.Width = 1;
                                                          break;
                                                      case "living_street":
                                                      case "residential":
                                                          returnStyle.Line.Brush = Brushes.LightGoldenrodYellow;
                                                          returnStyle.Line.Width = 2;
                                                          returnStyle.EnableOutline = true;
                                                          returnStyle.Outline.Brush = Brushes.DarkGray;
                                                          returnStyle.Outline.Width = 4;
                                                          break;
                                                      case "primary":
                                                          returnStyle.Line.Brush = Brushes.LightGoldenrodYellow;
                                                          returnStyle.Line.Width = 7;
                                                          returnStyle.EnableOutline = true;
                                                          returnStyle.Outline.Brush = Brushes.Black;
                                                          returnStyle.Outline.Width = 11;
                                                          break;
                                                      case "secondary":
                                                          returnStyle.Line.Brush = Brushes.LightGoldenrodYellow;
                                                          returnStyle.Line.Width = 6;
                                                          returnStyle.EnableOutline = true;
                                                          returnStyle.Outline.Brush = Brushes.Black;
                                                          returnStyle.Outline.Width = 10;
                                                          break;
                                                      case "tertiary":
                                                          returnStyle.Line.Brush = Brushes.LightGoldenrodYellow;
                                                          returnStyle.Line.Width = 5;
                                                          returnStyle.EnableOutline = true;
                                                          returnStyle.Outline.Brush = Brushes.Black;
                                                          returnStyle.Outline.Width = 9;
                                                          break;
                                                      case "path":
                                                      case "track":
                                                      case "unclassified":
                                                          returnStyle.Line.Brush = Brushes.DarkGray;
                                                          returnStyle.Line.DashStyle =
                                                              DashStyle.DashDotDot;
                                                          returnStyle.Line.Width = 1;
                                                          break;
                                                      default:
                                                          returnStyle = null;
                                                          break;
                                                  }
                                                  return returnStyle;
                                              };
            layRoads.Theme = themeRoads;
            layRoads.SRID = 31466;

            VectorLayer layRail = new VectorLayer("Railways");
            layRail.DataSource = new ShapeFile(string.Format("{0}/railways.shp", PathOsm));
            layRail.Style.Line.Brush = Brushes.White;
            layRail.Style.Line.DashPattern = new float[] {4f, 4f};//;System.Drawing.Drawing2D.DashStyle.Dash;
            layRail.Style.Line.Width = 4;
            layRail.Style.EnableOutline = true;
            layRail.Style.Outline.Brush = Brushes.Black;
            layRail.Style.Outline.Width = 6;

            VectorLayer layWaterways = new VectorLayer("Waterways");
            layWaterways.Style = transparentStyle;
            layWaterways.DataSource = new ShapeFile(string.Format("{0}/waterways.shp", PathOsm));
            layRoads.Style = transparentStyle;
            ThemeViaDelegate themeWater = new ThemeViaDelegate(transparentStyle, "type");
            themeWater.GetStyleFunction = delegate(FeatureDataRow row)
            {
                VectorStyle returnStyle = new VectorStyle();
                returnStyle.Line.Brush = Brushes.Aqua;
                returnStyle.EnableOutline = true;
                Int32 lineWidth = 1;
                switch ((String)row["type"])
                {
                    case "canal":
                    case "derelict_canal":
                        lineWidth = 2;
                        break;
                    case "drain":
                        returnStyle.EnableOutline = false;
                        break;
                    case "stream":
                        lineWidth = 2;
                        break;
                    default:
                        //returnStyle = null;
                        break;
                }
                returnStyle.Line.Width = lineWidth;
                returnStyle.Outline.Brush = Brushes.DarkBlue;
                returnStyle.Outline.Width = lineWidth + 1;
                return returnStyle;
            };
            layWaterways.Theme = themeWater;
            layWaterways.SRID = 31466;

            VectorLayer layPoints = new VectorLayer("Points");
            layPoints.DataSource = new ShapeFile(string.Format("{0}/points.shp", PathOsm));
            layPoints.Style = transparentStyle2;
            ThemeViaDelegate themePoints = new ThemeViaDelegate(transparentStyle2, "type");
            themePoints.GetStyleFunction = delegate(FeatureDataRow row)
            {
                VectorStyle returnStyle = new VectorStyle();
                switch ((String)row["type"])
                {
                    case "bank":
                        returnStyle.Symbol = new Bitmap("Images/Bank.gif");
                        break;
                    case "hospital":
                        returnStyle.Symbol = new Bitmap("Images/medical-facility.gif");
                        break;
                    case "hotel":
                        returnStyle.Symbol = new Bitmap("Images/hotel.gif");
                        break;
                    case "restaurant":
                    case "fast-food":
                        returnStyle.Symbol = new Bitmap("Images/restaurant.gif");
                        break;
                    case "parking":
                        returnStyle.Symbol = new Bitmap("Images/car.gif");
                        break;
                    default:
                        Bitmap tmp = new Bitmap(1,1);
                        tmp.SetPixel(0,0, Color.Transparent);
                        returnStyle.Symbol = tmp;
                        break;
                }
                return returnStyle;
            };
            layPoints.Theme = themePoints;
            layWaterways.SRID = 31466;

            //Add layers to Map
            map.Layers.Add(layNatural);
            map.Layers.Add(layWaterways);
            map.Layers.Add(layRail);
            map.Layers.Add(layRoads);
            map.Layers.Add(layPoints);

            //Restrict zoom
            map.MaximumZoom = layRoads.Envelope.Width * 0.75d;
            map.Zoom = layRoads.Envelope.Width * 0.2d; ;
            map.Center = layRoads.Envelope.GetCentroid();

            map.Disclaimer = "Geodata from OpenStreetMap (CC-by-SA)\nTransformed to Shapefile by geofabrik.de";
            map.DisclaimerFont = new Font("Arial", 7f, FontStyle.Italic);
            map.DisclaimerLocation = 1;
            transparentStyle2.MaxVisible = map.MaximumZoom*0.3;
            return map;
        }
    }
}