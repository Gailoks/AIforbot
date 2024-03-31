namespace TelegramAIBot.UserData.InMemory
{
	internal sealed class InMemoryUserDataRepository : IUserDataRepository
	{
		private readonly Dictionary<string, Store> _stores = [];
		private readonly SemaphoreSlim _lock = new(1);


		public ObjectHolder<TObject> Get<TObject>(string storageId)
			where TObject : notnull
		{
			TObject result;

			try
			{
				AutoResetEvent syncRoot;

				_lock.Wait();

				if (_stores.TryGetValue(storageId, out var value))
				{
					syncRoot = value.SyncRoot;
					result = (TObject)value.Object;
				}
				else
				{
					var store = Store.CreateDefault<TObject>();
					syncRoot = store.SyncRoot;
					_stores.Add(storageId, store);
					result = store.Read<TObject>();
				}

				syncRoot.WaitOne();
			}
			finally
			{
				_lock.Release();
			}

			return new ObjectHolder<TObject>(result, storageId, FinalizeObjectHolder);
		}

		private void FinalizeObjectHolder<TObject>(ObjectHolder<TObject> holder, object parameter) where TObject : notnull
		{
			try
			{
				_lock.Wait();

				var storageId = (string)parameter;

				var store = _stores[storageId];

				store.Object = holder.Object;

				store.SyncRoot.Set();
			}
			finally
			{
				_lock.Release();
			}
		}


		private class Store
		{
			private Store(object obj)
			{
				Object = obj;
			}


			public object Object { get; set; }

			public AutoResetEvent SyncRoot { get; } = new(true);


			public static Store CreateDefault<TObject>() where TObject : notnull
			{
				return new Store(Activator.CreateInstance<TObject>());
			}

			public static Store CreateWithValue<TObject>(TObject obj) where TObject : notnull
			{
				return new Store(obj);
			}

			public TObject Read<TObject>() where TObject : notnull => (TObject)Object;
		}
	}
}
