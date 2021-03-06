// Copyright 2005, 2006 - Morten Nielsen (www.iter.dk)
//
// This file is part of SharpMap.
// SharpMap is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// SharpMap is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with SharpMap; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Drawing.Imaging;
using System.Drawing;
using GeoAPI.Geometries;
using SharpMap.Api;

namespace SharpMap.Layers
{
	/// <summary>
	/// Web Map Service layer
	/// </summary>
	/// <remarks>
	/// The WmsLayer is currently very basic and doesn't support automatic fetching of the WMS Service Description.
	/// Instead you would have to add the nessesary parameters to the URL,
	/// and the WmsLayer will set the remaining BoundingBox property and proper requests that changes between the requests.
	/// See the example below.
	/// </remarks>
	/// <example>
	/// The following example creates a map with a WMS layer the Demis WMS Server
	/// <code lang="C#">
	/// myMap = new SharpMap.Map(new System.Drawing.Size(500,250);
	/// string wmsUrl = "http://www2.demis.nl/mapserver/request.asp";
	/// SharpMap.Layers.WmsLayer myLayer = new SharpMap.Layers.WmsLayer("Demis WMS", myLayer);
	/// myLayer.AddLayer("Bathymetry");
	/// myLayer.AddLayer("Countries");
	/// myLayer.AddLayer("Topography");
	/// myLayer.AddLayer("Hillshading");
	/// myLayer.SetImageFormat(layWms.OutputFormats[0]);
	/// myLayer.SpatialReferenceSystem = "EPSG:4326";	
    /// myMap.Layers.Add(myLayer);
	/// myMap.Center = new SharpMap.Geometries.Point(0, 0);
	/// myMap.Zoom = 360;
	/// myMap.MaximumZoom = 360;
	/// myMap.MinimumZoom = 0.1;
	/// </code>
	/// </example>
	public class WmsLayer : SharpMap.Layers.Layer
	{
		private SharpMap.Web.Wms.Client wmsClient;
		private string mimeType = "";
		
		/// <summary>
		/// Initializes a new layer, and downloads and parses the service description
		/// </summary>
		/// <remarks>In and ASP.NET application the service description is automatically cached for 24 hours when not specified</remarks>
        public WmsLayer()
            : this("", "", new TimeSpan(24, 0, 0))
		{
		}
	    /// <summary>
		/// Initializes a new layer, and downloads and parses the service description
		/// </summary>
		/// <remarks>In and ASP.NET application the service description is automatically cached for 24 hours when not specified</remarks>
		/// <param name="layername">Layername</param>
		/// <param name="url">Url of WMS server</param>
		public WmsLayer(string layername, string url)
			: this(layername, url, new TimeSpan(24, 0, 0))
		{
		}
		/// <summary>
		/// Initializes a new layer, and downloads and parses the service description
		/// </summary>
		/// <param name="layername">Layername</param>
		/// <param name="url">Url of WMS server</param>
		/// <param name="cachetime">Time for caching Service Description (ASP.NET only)</param>
		public WmsLayer(string layername, string url, TimeSpan cachetime)
			: this(layername, url, cachetime, null)
		{
		}
		/// <summary>
		/// Initializes a new layer, and downloads and parses the service description
		/// </summary>
		/// <remarks>In and ASP.NET application the service description is automatically cached for 24 hours when not specified</remarks>
		/// <param name="layername">Layername</param>
		/// <param name="url">Url of WMS server</param>
		/// <param name="proxy">Proxy</param>
		public WmsLayer(string layername, string url, System.Net.WebProxy proxy)
			: this(layername, url, new TimeSpan(24,0,0), proxy)
		{
		}
		/// <summary>
		/// Initializes a new layer, and downloads and parses the service description
		/// </summary>
		/// <param name="layername">Layername</param>
		/// <param name="url">Url of WMS server</param>
		/// <param name="cachetime">Time for caching Service Description (ASP.NET only)</param>
		/// <param name="proxy">Proxy</param>
		public WmsLayer(string layername, string url, TimeSpan cachetime, System.Net.WebProxy proxy)
		{
			this.proxy = proxy;
			timeOut = 10000;
			this.Name = layername;
		    this.Cachetime = cachetime;
		    this.Url = url; // Also does the initialization if an non-empty url was given
		}

        private void Initialize()
        {
            continueOnError = true;
            if (System.Web.HttpContext.Current != null && System.Web.HttpContext.Current.Cache["SharpMap_WmsClient_" + url] != null)
            {
                wmsClient = (SharpMap.Web.Wms.Client)System.Web.HttpContext.Current.Cache["SharpMap_WmsClient_" + url];
            }
            else
            {
                wmsClient = new SharpMap.Web.Wms.Client(url, proxy);
                if (System.Web.HttpContext.Current != null)
                    System.Web.HttpContext.Current.Cache.Insert("SharpMap_WmsClient_" + url, wmsClient, null,
                        System.Web.Caching.Cache.NoAbsoluteExpiration, cachetime);
            }
            //Set default mimetype - We prefer compressed formats
            if (OutputFormats.Contains("image/jpeg")) mimeType = "image/jpeg";
            else if (OutputFormats.Contains("image/png")) mimeType = "image/png";
            else if (OutputFormats.Contains("image/gif")) mimeType = "image/gif";
            else //None of the default formats supported - Look for the first supported output format
            {
                bool formatSupported = false;
                foreach (System.Drawing.Imaging.ImageCodecInfo encoder in System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders())
                    if (OutputFormats.Contains(encoder.MimeType.ToLower()))
                    {
                        formatSupported = true;
                        mimeType = encoder.MimeType;
                        break;
                    }
                if (!formatSupported)
                    throw new ArgumentException("GDI+ doesn't not support any of the mimetypes supported by this WMS service");
            }
            layerList = new Collection<string>();
            stylesList = new Collection<string>();
        }

	    private TimeSpan cachetime;

	    public virtual TimeSpan Cachetime
	    {
            get { return cachetime; }
            set { cachetime = value; }
	    }

	    private string url;

        public virtual string Url
	    {
            get { return url; }
            set
            {
                url = value;
                if (url != string.Empty)
                {
                    Initialize();
                }
            }
	    }

	    public virtual string MimeType
	    {
            get { return mimeType; }
            set { mimeType = value; }
	    }

        private IList<string> layerList;

		/// <summary>
		/// Gets the list of enabled layers
		/// </summary>
        public virtual IList<string> LayerList
		{
			get { return layerList; }
            set { layerList = value; }
		}

		/// <summary>
		/// Adds a layer to WMS request
		/// </summary>
		/// <remarks>Layer names are case sensitive.</remarks>
		/// <param name="name">Name of layer</param>
		/// <exception cref="System.ArgumentException">Throws an exception is an unknown layer is added</exception>
        public virtual void AddLayer(string name)
		{			
			if(!LayerExists(wmsClient.Layer,name))
				throw new ArgumentException("Cannot add WMS Layer - Unknown layername");
			
			layerList.Add(name);
		}
		/// <summary>
		/// Recursive method for checking whether a layername exists
		/// </summary>
		/// <param name="layer"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		private bool LayerExists(SharpMap.Web.Wms.Client.WmsServerLayer layer, string name)
		{
			if(name == layer.Name) return true;
			foreach (SharpMap.Web.Wms.Client.WmsServerLayer childlayer in layer.ChildLayers)
				if (LayerExists(childlayer,name)) return true;
			return false;
		}
		/// <summary>
		/// Removes a layer from the layer list
		/// </summary>
		/// <param name="name">Name of layer to remove</param>
        public virtual void RemoveLayer(string name)
		{
			layerList.Remove(name);
		}
		/// <summary>
		/// Removes the layer at the specified index
		/// </summary>
		/// <param name="index"></param>
        public virtual void RemoveLayerAt(int index)
		{
			layerList.RemoveAt(index);
		}
		/// <summary>
		/// Removes all layers
		/// </summary>
        public virtual void RemoveAllLayers()
		{
			layerList.Clear();
		}

		private Collection<string> stylesList;

		/// <summary>
		/// Gets the list of enabled styles
		/// </summary>
        public virtual Collection<string> StylesList
		{
			get { return stylesList; }
		}

		/// <summary>
		/// Adds a style to the style collection
		/// </summary>
		/// <param name="name">Name of style</param>
		/// <exception cref="System.ArgumentException">Throws an exception is an unknown layer is added</exception>
        public virtual void AddStyle(string name)
		{
			if (!StyleExists(wmsClient.Layer, name))
				throw new ArgumentException("Cannot add WMS Layer - Unknown layername");
			stylesList.Add(name);
		}

		/// <summary>
		/// Recursive method for checking whether a layername exists
		/// </summary>
		/// <param name="layer">layer</param>
		/// <param name="name">name of style</param>
		/// <returns>True of style exists</returns>
		private bool StyleExists(SharpMap.Web.Wms.Client.WmsServerLayer layer, string name)
		{			
			foreach(SharpMap.Web.Wms.Client.WmsLayerStyle style in layer.Style)
				if (name == style.Name) return true;
			foreach (SharpMap.Web.Wms.Client.WmsServerLayer childlayer in layer.ChildLayers)
				if (StyleExists(childlayer, name)) return true;
			return false;
		}
		/// <summary>
		/// Removes a style from the collection
		/// </summary>
		/// <param name="name">Name of style</param>
        public virtual void RemoveStyle(string name)
		{
			stylesList.Remove(name);
		}
		/// <summary>
		/// Removes a style at specified index
		/// </summary>
		/// <param name="index">Index</param>
        public virtual void RemoveStyleAt(int index)
		{
			stylesList.RemoveAt(index);
		}
		/// <summary>
		/// Removes all styles from the list
		/// </summary>
        public virtual void RemoveAllStyles()
		{
			stylesList.Clear();
		}
		
		/// <summary>
		/// Sets the image type to use when requesting images from the WMS server
		/// </summary>
		/// <remarks>
		/// <para>See the <see cref="OutputFormats"/> property for a list of available mime types supported by the WMS server</para>
		/// </remarks>
		/// <exception cref="ArgumentException">Throws an exception if either the mime type isn't offered by the WMS
		/// or GDI+ doesn't support this mime type.</exception>
		/// <param name="mimeType">Mime type of image format</param>
		public virtual void SetImageFormat(string mimeType)
		{
			if (!OutputFormats.Contains(mimeType))
				throw new ArgumentException("WMS service doesn't not offer mimetype '" + mimeType + "'");
			//Check whether SharpMap supports the specified mimetype
			bool formatSupported = false;
			foreach (System.Drawing.Imaging.ImageCodecInfo encoder in System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders())
				if (encoder.MimeType.ToLower() == mimeType.ToLower())
				{
					formatSupported = true;
					break;
				}
			if (!formatSupported)
				throw new ArgumentException("GDI+ doesn't not support mimetype '" + mimeType + "'");
			this.mimeType = mimeType;
		}

		/// <summary>
		/// Gets the hiarchial list of available WMS layers from this service
		/// </summary>
        public virtual SharpMap.Web.Wms.Client.WmsServerLayer RootLayer
		{
			get { return wmsClient.Layer; }
		}
		/// <summary>
		/// Gets the list of available formats
		/// </summary>
        public virtual Collection<string> OutputFormats
		{
			get { return wmsClient.GetMapOutputFormats; }
		}

		private string spatialReferenceSystem;

		/// <summary>
		/// Gets or sets the spatial reference used for the WMS server request
		/// </summary>
        public virtual string SpatialReferenceSystem
		{
			get { return spatialReferenceSystem; }
			set { spatialReferenceSystem = value; }
		}
	

		/// <summary>
		/// Gets the service description from this server
		/// </summary>
		public virtual SharpMap.Web.Wms.Capabilities.WmsServiceDescription ServiceDescription
		{
			get { return wmsClient.ServiceDescription; }
		}
		/// <summary>
		/// Gets the WMS Server version of this service
		/// </summary>
        public virtual string Version
		{
			get { return wmsClient.WmsVersion; }
		}

		private ImageAttributes imageAttributes;

		/// <summary>
		/// When specified, applies image attributes at image (fx. make WMS layer semi-transparent)
		/// </summary>
		/// <remarks>
		/// <para>You can make the WMS layer semi-transparent by settings a up a ColorMatrix,
		/// or scale/translate the colors in any other way you like.</para>
		/// <example>
		/// Setting the WMS layer to be semi-transparent.
		/// <code lang="C#">
		/// float[][] colorMatrixElements = { 
		///				new float[] {1,  0,  0,  0, 0},
		///				new float[] {0,  1,  0,  0, 0},
		///				new float[] {0,  0,  1,  0, 0},
		///				new float[] {0,  0,  0,  0.5, 0},
		///				new float[] {0, 0, 0, 0, 1}};
		/// ColorMatrix colorMatrix = new ColorMatrix(colorMatrixElements);
		/// ImageAttributes imageAttributes = new ImageAttributes();
		/// imageAttributes.SetColorMatrix(
		/// 	   colorMatrix,
		/// 	   ColorMatrixFlag.Default,
		/// 	   ColorAdjustType.Bitmap);
		/// myWmsLayer.ImageAttributes = imageAttributes;
		/// </code>
		/// </example>
		/// </remarks>
        public virtual ImageAttributes ImageAttributes
		{
			get { return imageAttributes; }
			set { imageAttributes = value; }
		}
	

		#region ILayer Members

		/// <summary>
		/// Renders the layer
		/// </summary>
		/// <param name="g">Graphics object reference</param>
		/// <param name="map">Map which is rendered</param>
		public override void OnRender(System.Drawing.Graphics g, IMap map)
		{
			SharpMap.Web.Wms.Client.WmsOnlineResource resource = GetPreferredMethod();			
			Uri myUri = new Uri(GetRequestUrl(map.Envelope, map.Size));
			System.Net.WebRequest myWebRequest = System.Net.WebRequest.Create(myUri);
			myWebRequest.Method = resource.Type;
			myWebRequest.Timeout = timeOut;
			if (credentials != null)
				myWebRequest.Credentials = credentials;
			else
				myWebRequest.Credentials = System.Net.CredentialCache.DefaultCredentials;

			if (proxy != null)
				myWebRequest.Proxy = proxy;

			try
			{
				System.Net.HttpWebResponse myWebResponse = (System.Net.HttpWebResponse)myWebRequest.GetResponse();
				System.IO.Stream dataStream = myWebResponse.GetResponseStream();

				if (myWebResponse.ContentType.StartsWith("image"))
				{
					System.Drawing.Image img = System.Drawing.Image.FromStream(myWebResponse.GetResponseStream());
					if (imageAttributes != null)
						g.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height), 0, 0,
						   img.Width, img.Height, GraphicsUnit.Pixel, this.ImageAttributes);
					else
						g.DrawImageUnscaled(img, 0, 0,map.Size.Width,map.Size.Height);
				}
				dataStream.Close();
				myWebResponse.Close();
			}
			catch (System.Net.WebException webEx)
			{
				if (!continueOnError)
					throw (new SharpMap.Rendering.Exceptions.RenderException("There was a problem connecting to the WMS server when rendering layer '" + this.Name + "'", webEx));
				else
					//Write out a trace warning instead of throwing an error to help debugging WMS problems
					System.Diagnostics.Trace.Write("There was a problem connecting to the WMS server when rendering layer '" + this.Name + "': " + webEx.Message);
			}
			catch (System.Exception ex)
			{
				if (!continueOnError)
					throw (new SharpMap.Rendering.Exceptions.RenderException("There was a problem rendering layer '" + this.Name + "'", ex));
				else
					//Write out a trace warning instead of throwing an error to help debugging WMS problems
					System.Diagnostics.Trace.Write("There was a problem connecting to the WMS server when rendering layer '" + this.Name + "': " + ex.Message);
			}
		}

		/// <summary>
		/// Gets the URL for a map request base on current settings, the image size and boundingbox
		/// </summary>
		/// <param name="box">Area the WMS request should cover</param>
		/// <param name="size">Size of image</param>
		/// <returns>URL for WMS request</returns>
        public virtual string GetRequestUrl(IEnvelope box, System.Drawing.Size size)
		{
			SharpMap.Web.Wms.Client.WmsOnlineResource resource = GetPreferredMethod();			
			System.Text.StringBuilder strReq = new StringBuilder(resource.OnlineResource);
			if(!resource.OnlineResource.Contains("?"))
				strReq.Append("?");
			if (!strReq.ToString().EndsWith("&") && !strReq.ToString().EndsWith("?"))
				strReq.Append("&");

			strReq.AppendFormat(SharpMap.Map.numberFormat_EnUS, "REQUEST=GetMap&BBOX={0},{1},{2},{3}",
				box.MinX, box.MinY, box.MaxX, box.MaxY);
			strReq.AppendFormat("&WIDTH={0}&Height={1}", size.Width, size.Height);
			strReq.Append("&Layers=");
			if (layerList != null && layerList.Count > 0)
			{
				foreach (string layer in layerList)
					strReq.AppendFormat("{0},", layer);
				strReq.Remove(strReq.Length - 1, 1);
			}
			strReq.AppendFormat("&FORMAT={0}", mimeType);
			if (spatialReferenceSystem == string.Empty)
				throw new ApplicationException("Spatial reference system not set");
			if(wmsClient.WmsVersion=="1.3.0")
				strReq.AppendFormat("&CRS={0}", spatialReferenceSystem);
			else
				strReq.AppendFormat("&SRS={0}", spatialReferenceSystem);
			strReq.AppendFormat("&VERSION={0}", wmsClient.WmsVersion);
			strReq.Append("&Styles=");
			if (stylesList != null && stylesList.Count > 0)
			{
				foreach (string style in stylesList)
					strReq.AppendFormat("{0},", style);
				strReq.Remove(strReq.Length - 1, 1);
			}
			return strReq.ToString();
		}

		/// <summary>
		/// Returns the extent of the layer
		/// </summary>
		/// <returns>Bounding box corresponding to the extent of the features in the layer</returns>
        public override IEnvelope Envelope
		{
			get
			{
				return RootLayer.LatLonBoundingBox;
			}
		}

		private Boolean continueOnError;

		/// <summary>
		/// Specifies whether to throw an exception if the Wms request failed, or to just skip rendering the layer
		/// </summary>
        public virtual Boolean ContinueOnError
		{
			get { return continueOnError; }
			set { continueOnError = value; }
		}

		/// <summary>
		/// Returns the type of the layer
		/// </summary>
		//public override SharpMap.Layers.Layertype LayerType
		//{
		//    get { return SharpMap.Layers.Layertype.Wms; }
		//}

		#endregion

		private SharpMap.Web.Wms.Client.WmsOnlineResource GetPreferredMethod()
		{
			//We prefer posting. Seek for supported post method
			for (int i = 0; i < wmsClient.GetMapRequests.Length; i++)
				if (wmsClient.GetMapRequests[i].Type.ToLower() == "post")
				{
				    return wmsClient.GetMapRequests[i];
				}
			//Next we prefer the 'get' method
			for (int i = 0; i < wmsClient.GetMapRequests.Length; i++)
            {
                if (wmsClient.GetMapRequests[i].Type.ToLower() == "get")
                {
                    return wmsClient.GetMapRequests[i];
                }
            }
			return wmsClient.GetMapRequests[0];
		}

		private System.Net.ICredentials credentials;

		/// <summary>
		/// Provides the base authentication interface for retrieving credentials for Web client authentication.
		/// </summary>
        public virtual System.Net.ICredentials Credentials
		{
			get { return credentials; }
			set { credentials = value; }
		}

		private System.Net.WebProxy proxy;

		/// <summary>
		/// Gets or sets the proxy used for requesting a webresource
		/// </summary>
        public virtual System.Net.WebProxy Proxy
		{
			get { return proxy; }
			set { proxy = value; }
		}
	

		private int timeOut;

		/// <summary>
		/// Timeout of webrequest in milliseconds. Defaults to 10 seconds
		/// </summary>
		public virtual int TimeOut
		{
			get { return timeOut; }
			set { timeOut = value; }
		}

		#region ICloneable Members

		/// <summary>
		/// Clones the object
		/// </summary>
		/// <returns></returns>
		public override object Clone()
		{
		    return new WmsLayer(Name, Url);
		}

		#endregion

	    public virtual string LayerTitle
	    {
	        get { return RootLayer.Name; }
	    }
	}
}
