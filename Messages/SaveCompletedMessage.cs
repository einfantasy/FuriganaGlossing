using CommunityToolkit.Mvvm.Messaging;

namespace FuriganaGlossing.Messages
{
    public class SaveCompletedMessage
    {
        public bool IsSuccess { get; }
        public SaveCompletedMessage(bool isSuccess)
        {
            IsSuccess = isSuccess;
        }
    }
}
