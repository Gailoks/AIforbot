namespace TelegramAIBot.UserData
{
	internal sealed class ObjectHolder<TObject> : IDisposable
	{
		private readonly object _parameter;
		private readonly Action<ObjectHolder<TObject>, object> _finalizer;


		public ObjectHolder(TObject obj, object parameter, Action<ObjectHolder<TObject>, object> finalizer)
		{
			Object = obj;
			_parameter = parameter;
			_finalizer = finalizer;
		}


		public TObject Object { get; set; }


		public void Dispose()
		{
			_finalizer(this, _parameter);
		}
	}
}
