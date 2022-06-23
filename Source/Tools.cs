using System.Globalization;

namespace Rimionship
{
	public static class Tools
	{
		static readonly NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;

		public static string DotFormatted(this long nr)
		{
			return nr.ToString("N", nfi);
		}
	}
}
