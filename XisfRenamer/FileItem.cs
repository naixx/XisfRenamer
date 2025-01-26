#nullable disable
using NINA.Image.ImageData;
using PropertyChanged;

namespace XisfRenamer;

[AddINotifyPropertyChangedInterface]
public class FileItem
{
    public ImageMetaData Meta { get; set; }
    public bool IsChecked { get; set; }
    public string FileName { get; set; }
    public string Target { get; set; }
    public string Path { get; set; }
    public string Filter { get; set; }
    public string ImageType { get; set; }
    public string Renamed { get; set; }
    public DateTime ExposureStart { get; set; }
}