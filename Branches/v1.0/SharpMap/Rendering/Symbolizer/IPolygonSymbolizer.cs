﻿using GeoAPI.Geometries;
using SharpMap.Geometries;

namespace SharpMap.Rendering.Symbolizer
{
    /// <summary>
    /// Interface for classes that can symbolize polygons
    /// </summary>
    public interface IPolygonSymbolizer : ISymbolizer<IPolygonal>
    { 
    }
}