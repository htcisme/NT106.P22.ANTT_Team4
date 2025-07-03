using System.Windows;
using System.Windows.Controls;
using DoanKhoaClient.Models;

namespace DoanKhoaClient.Selectors
{
    public class AttachmentTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ImageTemplate { get; set; }
        public DataTemplate FileTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is Attachment attachment)
            {
                // Log selection for debugging
                System.Diagnostics.Debug.WriteLine($"Template selection: {attachment.FileName}, IsImage: {attachment.IsImage}");

                if (attachment.IsImage)
                {
                    System.Diagnostics.Debug.WriteLine("Using IMAGE template");
                    return ImageTemplate;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Using FILE template");
                    return FileTemplate;
                }
            }
            return null;
        }
    }
}