using DoanKhoaClient.Models;
using System.Windows;
using System.Windows.Controls;

namespace DoanKhoaClient.Helpers
{
    public class AttachmentTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ImageTemplate { get; set; }
        public DataTemplate FileTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is Attachment attachment)
            {
                if (attachment.IsImage)
                {
                    return ImageTemplate;
                }
                else
                {
                    return FileTemplate;
                }
            }

            return base.SelectTemplate(item, container);
        }
    }
}