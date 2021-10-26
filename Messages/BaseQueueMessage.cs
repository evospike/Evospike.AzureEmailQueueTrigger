namespace AzureEmailQueueTrigger.Messages
{
	public abstract class BaseQueueMessage
	{
		public string Route { get; set; }

		public BaseQueueMessage(string route)
		{
			Route = route;
		}
	}
}
