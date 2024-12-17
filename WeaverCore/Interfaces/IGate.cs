namespace WeaverCore.Interfaces
{
    public interface IGate
	{
		void Open();
		void Close();
		void OpenInstant();
		void CloseInstant();
	}
}
