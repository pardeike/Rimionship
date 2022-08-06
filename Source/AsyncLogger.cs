using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Verse;

namespace Rimionship
{
	public static class AsyncLogger
	{
		struct Entry
		{
			public bool isError;
			public string txt;
			public int lineNumber;
			public string caller;
			public StackTrace trace;
		}

		static readonly ConcurrentQueue<Entry> queue = new();

		public static void Warning(string txt)
		{
			queue.Enqueue(new() { isError = false, txt = txt, lineNumber = 0, caller = null, trace = null });
		}

		public static void WarningWithLine(string txt,
			[CallerLineNumber] int lineNumber = 0,
			[CallerMemberName] string caller = null)
		{
			queue.Enqueue(new() { isError = false, txt = txt, lineNumber = lineNumber, caller = caller, trace = null });
		}

		public static void Error(string txt, bool withStacktrace = false)
		{
			var trace = withStacktrace ? new StackTrace(1) : null;
			queue.Enqueue(new() { isError = true, txt = txt, lineNumber = 0, caller = null, trace = trace });
		}

		public static IEnumerator LogCoroutine()
		{
			while (true)
			{
				while (queue.TryDequeue(out var entry))
					if (entry.isError)
					{
						Log.Error($"{entry.txt}\n{entry.trace}");
					}
					else
					{
						var extra = entry.lineNumber == 0 ? "" : $" at {entry.caller}:{entry.lineNumber}";
						Log.Warning($"{entry.txt}{extra}");
					}
				yield return null;
			}
		}
	}
}
