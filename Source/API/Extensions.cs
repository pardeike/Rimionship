using System.Collections.Generic;

namespace Api
{
	public static partial class Extensions
	{
		public static void AddEvents(this FutureEventsRequest request, IEnumerable<FutureEvent> events)
		{
			request.Event.AddRange(events);
		}
	}
}
