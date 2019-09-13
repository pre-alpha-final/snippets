using System;
using System.Globalization;

namespace Namespace
{
	public static class ObjectExtensions
	{
		public static string ToInvariantString(this object obj)
		{
			return obj is IConvertible convertible ? convertible.ToString(CultureInfo.InvariantCulture)
				: obj is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture)
				: obj.ToString();
		}
	}
}
