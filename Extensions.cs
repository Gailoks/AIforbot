using Microsoft.Extensions.Logging;

namespace TelegramAIBot
{
	internal static class Extensions
	{
		public static EventId Form(this EventId id)
		{
			if (id.Name is null)
				return id;

			//Cuts 'LOG' postfix
			return new EventId(id.Id, id.Name[..^3]);
		}
	}
}
