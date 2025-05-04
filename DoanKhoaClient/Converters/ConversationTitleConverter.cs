using DoanKhoaClient.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace DoanKhoaClient.Converters
{
    public class ConversationTitleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Conversation conversation)
            {
                // Get the current user from application properties
                if (Application.Current.Properties.Contains("CurrentUser") &&
                    Application.Current.Properties["CurrentUser"] is User currentUser)
                {
                    // For private (one-to-one) conversations
                    if (!conversation.IsGroup && conversation.ParticipantIds.Count == 2)
                    {
                        // Find the other user's ID (not the current user)
                        string otherUserId = conversation.ParticipantIds
                            .FirstOrDefault(id => id != currentUser.Id);

                        if (otherUserId != null && conversation.Participants != null)
                        {
                            // Find the other user in the participants list
                            var otherUser = conversation.Participants
                                .FirstOrDefault(p => p.Id == otherUserId);

                            if (otherUser != null)
                            {
                                return otherUser.DisplayName ?? otherUser.Username;
                            }
                        }
                    }

                    // For groups or fallback
                    return conversation.Title;
                }

                return conversation.Title ?? "Untitled Conversation";
            }

            return "No Conversation";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}