using RimWorld;

namespace Rimionship
{
	public class Thought_MemoryFear : Thought_Memory
	{
		public override bool ShouldDiscard => BloodGod.Instance.punishLevel == 0;
	}
}
