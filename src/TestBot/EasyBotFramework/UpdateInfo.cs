using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TestBot.EasyBotFramework
{
    public enum UpdateKind { None, NewMessage, EditedMessage, CallbackQuery, OtherUpdate }
    public enum MsgCategory { Other, Text, MediaOrDoc, StickerOrDice, Sharing, ChatStatus, VideoChat }
    public enum UserType {Admin, Client}

    public class UpdateInfo : IGetNext
    {
        public UpdateKind UpdateKind;
        public CallbackQuery CallbackQuery;
        public Message Message;
        public string CallbackData;
        public Update Update;

        private readonly long[]? _adminIds;

        public MsgCategory MsgCategory => (Message?.Type) switch
        {
            MessageType.Text => MsgCategory.Text,
            MessageType.Photo or MessageType.Audio or MessageType.Video or MessageType.Voice or MessageType.Document or MessageType.VideoNote
                => MsgCategory.MediaOrDoc,
            MessageType.Sticker or MessageType.Dice
                => MsgCategory.StickerOrDice,
            MessageType.Location or MessageType.Contact or MessageType.Venue or MessageType.Game or MessageType.Invoice or
                MessageType.SuccessfulPayment or MessageType.WebsiteConnected
                => MsgCategory.Sharing,
            MessageType.ChatMembersAdded or MessageType.ChatMemberLeft or MessageType.ChatTitleChanged or MessageType.ChatPhotoChanged or
                MessageType.MessagePinned or MessageType.ChatPhotoDeleted or MessageType.GroupCreated or MessageType.SupergroupCreated or
                MessageType.ChannelCreated or MessageType.MigratedToSupergroup or MessageType.MigratedFromGroup
                => MsgCategory.ChatStatus,
            MessageType.VideoChatScheduled or MessageType.VideoChatStarted or MessageType.VideoChatEnded or MessageType.VideoChatParticipantsInvited
                => MsgCategory.VideoChat,
            _ => MsgCategory.Other,
        };
        
        public UserType UserType => _adminIds.Contains(Message?.Chat.Id ?? 0) ? UserType.Admin : UserType.Client;

        private readonly TaskInfo _taskInfo;
        internal UpdateInfo(TaskInfo taskInfo, IConfiguration configuration)
        {
            _taskInfo = taskInfo;
            _adminIds = configuration.GetSection("AdminIds").Get<long[]>();
        }
        async Task<UpdateInfo> IGetNext.NextUpdate(CancellationToken cancel)
        {
            try
            {
                await _taskInfo.Semaphore.WaitAsync(cancel);
                UpdateInfo newUpdate;
                lock (_taskInfo)
                    newUpdate = _taskInfo.Updates.Dequeue();
                return newUpdate;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }
    }
}