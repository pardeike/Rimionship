using System;
using System.Globalization;
using Verse.AI;

namespace Rimionship
{
	public static class Tools
	{
		static readonly NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;

		public static string DotFormatted(this long nr)
		{
			return nr.ToString("N", nfi);
		}

		public static void EndOnDespawnedOrNull<T>(this T f, Action cleanupAction, params TargetIndex[] indices) where T : IJobEndable
		{
			foreach (var ind in indices)
				f.AddEndCondition(delegate
				{
					var target = f.GetActor().jobs.curJob.GetTarget(ind);
					var thing = target.Thing;
					if (thing == null && target.IsValid)
					{
						return JobCondition.Ongoing;
					}
					if (thing == null || !thing.Spawned || thing.Map != f.GetActor().Map)
					{
						cleanupAction();
						return JobCondition.Incompletable;
					}
					return JobCondition.Ongoing;
				});
		}
	}
}
