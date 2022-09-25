using System;

namespace Rimionship
{
	public class Configuration
	{
		// ENVIRONMENT VARIABLES
		const string localhost = "RIMIONSHIP-LOCALHOST";            // bool
		const string devMode = "RIMIONSHIP-DEV-MODE";               // bool
		const string apiLogging = "RIMIONSHIP-API-LOGGING";         // bool
		const string customSettings = "RIMIONSHIP-CUSTOM-SETTINGS"; // bool
		const string endpoint = "RIMIONSHIP-ENDPOINT";              // string

		public static bool UseLocalHost => Environment.GetEnvironmentVariable(localhost) == "1";
		public static bool UseDevMode => Environment.GetEnvironmentVariable(devMode) == "1";
		public static bool UseApiLogging => Environment.GetEnvironmentVariable(apiLogging) == "1";
		public static bool CustomSettings => Environment.GetEnvironmentVariable(customSettings) == "1";
		public static string CustomEndpoint => Environment.GetEnvironmentVariable(endpoint);
		public static string LocalHostEndpoint => "localhost:5063";
		public static string ProductionEndpoint => "mod.rimionship.com";
	}
}
