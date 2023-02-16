﻿using System.Globalization;
using System.IO;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SkiaSharp;

namespace Uno.Resizetizer
{
	/// <summary>
	/// Generates Resources/values/uno_colors.xml and Resources/drawable/uno_splash_image.xml for Android splash screens
	/// </summary>
	public class GenerateSplashAndroidResources : Task, ILogger
	{
		[Required]
		public string IntermediateOutputPath { get; set; }

		[Required]
		public ITaskItem[] MauiSplashScreen { get; set; }

		public override bool Execute()
		{
#if DEBUG_RESIZETIZER
			System.Diagnostics.Debugger.Launch();
#endif
			var splash = MauiSplashScreen[0];

			var info = ResizeImageInfo.Parse(splash);

			var tools = SkiaSharpTools.Create(info.IsVector, info.Filename, info.BaseSize, info.Color, info.TintColor, this);

			WriteColors(info, tools);
			WriteDrawable(info, tools);
			WriteDrawable_v31(info, tools);

			return !Log.HasLoggedErrors;
		}

		static readonly XmlWriterSettings Settings = new XmlWriterSettings { Indent = true };
		const string Namespace = "http://schemas.android.com/apk/res/android";
		const string Comment = "This file was auto-generated by Uno Platform.";
		const float PreferredImageSize = 108f;

		void WriteColors(ResizeImageInfo splash, SkiaSharpTools tools)
		{
			var dir = Path.Combine(IntermediateOutputPath, "values");
			Directory.CreateDirectory(dir);

			var colorsFile = Path.Combine(dir, "uno_colors.xml");
			using var writer = XmlWriter.Create(colorsFile, Settings);
			writer.WriteComment(Comment);
			writer.WriteStartElement("resources");

			if (splash.Color is not null)
			{
				writer.WriteStartElement("color");
				writer.WriteAttributeString("name", "uno_splash_color");
				writer.WriteString(splash.Color.ToString());
				writer.WriteEndElement();
			}

			writer.WriteEndDocument();
		}

		void WriteDrawable(ResizeImageInfo splash, SkiaSharpTools tools)
		{
			var dir = Path.Combine(IntermediateOutputPath, "drawable");
			Directory.CreateDirectory(dir);

			var drawableFile = Path.Combine(dir, "uno_splash_image.xml");
			using var writer = XmlWriter.Create(drawableFile, Settings);
			writer.WriteComment(Comment);
			writer.WriteStartElement("layer-list");
			writer.WriteAttributeString("xmlns", "android", ns: null, value: Namespace);

			writer.WriteStartElement("item");
			writer.WriteStartElement("color");
			writer.WriteAttributeString("android", "color", Namespace, "@color/uno_splash_color");
			writer.WriteEndElement();
			writer.WriteFullEndElement();

			writer.WriteStartElement("item");
			writer.WriteStartElement("bitmap");
			writer.WriteAttributeString("android", "gravity", Namespace, "center");
			writer.WriteAttributeString("android", "src", Namespace, "@drawable/" + splash.OutputName);
			writer.WriteAttributeString("android", "tileMode", Namespace, "disabled");
			writer.WriteAttributeString("android", "mipMap", Namespace, "true");
			writer.WriteEndDocument();
		}

		void WriteDrawable_v31(ResizeImageInfo splash, SkiaSharpTools tools)
		{
			var size = CalculateScaledSize(tools);

			var dir = Path.Combine(IntermediateOutputPath, "drawable-v31");
			Directory.CreateDirectory(dir);

			var drawableFile = Path.Combine(dir, "uno_splash_image.xml");
			using var writer = XmlWriter.Create(drawableFile, Settings);
			writer.WriteComment(Comment);
			writer.WriteStartElement("layer-list");
			writer.WriteAttributeString("xmlns", "android", ns: null, value: Namespace);

			writer.WriteStartElement("item");
			writer.WriteStartElement("color");
			writer.WriteAttributeString("android", "color", Namespace, "@color/uno_splash_color");
			writer.WriteEndElement();
			writer.WriteFullEndElement();

			writer.WriteStartElement("item");
			writer.WriteAttributeString("android", "width", Namespace, size.Width.ToString("0", CultureInfo.InvariantCulture) + "dp");
			writer.WriteAttributeString("android", "height", Namespace, size.Height.ToString("0", CultureInfo.InvariantCulture) + "dp");
			writer.WriteAttributeString("android", "gravity", Namespace, "center");

			writer.WriteStartElement("bitmap");
			writer.WriteAttributeString("android", "gravity", Namespace, "fill");
			writer.WriteAttributeString("android", "src", Namespace, "@drawable/" + splash.OutputName);
			writer.WriteAttributeString("android", "mipMap", Namespace, "true");
			writer.WriteAttributeString("android", "tileMode", Namespace, "disabled");

			writer.WriteEndDocument();
		}

		void ILogger.Log(string message)
		{
			Log?.LogMessage(message);
		}

		static SKSize CalculateScaledSize(SkiaSharpTools tools)
		{
			var size = tools.BaseSize ?? tools.GetOriginalSize();

			var width = PreferredImageSize;
			var height = PreferredImageSize;

			if (size.Width > size.Height)
				height = size.Height / size.Width * width;
			else
				width = size.Width / size.Height * height;

			return new SKSize(width, height);
		}
	}
}
