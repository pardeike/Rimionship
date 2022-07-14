using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using Verse;

namespace Rimionship
{
	public static class AsyncLogger
	{
		static readonly ConcurrentQueue<(string, bool, StackTrace)> queue = new();

		public static void Warning(string txt)
		{
			queue.Enqueue((txt, false, null));
		}

		public static void Error(string txt, StackTrace trace = null)
		{
			queue.Enqueue((txt, true, trace));
		}

		public static IEnumerator LogCoroutine()
		{
			while (true)
			{
				while (queue.TryDequeue(out var tuple))
					if (tuple.Item2)
					{
						Log.Error($"{tuple.Item1}\n{tuple.Item3}");
					}
					else
						Log.Warning(tuple.Item1);
				yield return null;
			}
		}
	}
}
