namespace StockTvBlazor.Components.Extensions
{
	public static class StructExtension
	{
		public static T Next<T>(this T src) where T : struct
		{
			if (!typeof(T).IsEnum) throw new ArgumentException($"Argument {typeof(T).FullName} is not an Enum");

			T[] arr = (T[])Enum.GetValues(src.GetType());
			int j = (Array.IndexOf(arr, src) + 1) % arr.Length;
			return arr[j];
		}

		public static T Previous<T>(this T src) where T : struct
		{
			if (!typeof(T).IsEnum) throw new ArgumentException($"Argument {typeof(T).FullName} is not an Enum");

			T[] arr = (T[])Enum.GetValues(src.GetType());
			int j = (Array.IndexOf(arr, src) - 1 + arr.Length) % arr.Length;
			return arr[j];
		}
	}
}
