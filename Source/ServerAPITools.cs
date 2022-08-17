using Grpc.Core;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Rimionship
{
	public static class ServerAPITools
	{
		public static bool modTooOld = false;

		static readonly Regex apiTooOldDetails = new(@"Expected API version (\d+) but got (\d+)", RegexOptions.Compiled);
		public static async Task WrapCall(Func<Task> call)
		{
			if (Communications.Client == null)
				return;
			try
			{
				await call();
			}
			catch (RpcException e)
			{
				if (e.Status.StatusCode == StatusCode.Aborted)
				{
					AsyncLogger.Error($"Aborted gRPC call: {e.Status.Detail}");
					var match = apiTooOldDetails.Match(e.Status.Detail);
					if (match.Success)
					{
						modTooOld = true;
						ServerAPI.CancelAll();
						Communications.Stop();
						return;
					}
				}

				if (e.ShouldReport())
				{
					PlayState.errorCount++;
					AsyncLogger.Error($"gRPC error: {e}");
				}
			}
			catch (Exception e)
			{
				AsyncLogger.Error($"Exception: {e}");
			}
		}
	}
}
