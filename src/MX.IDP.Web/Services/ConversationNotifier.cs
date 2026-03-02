namespace MX.IDP.Web.Services;

public class ConversationNotifier
{
    public event Action? OnChange;

    public void NotifyChanged() => OnChange?.Invoke();
}
