using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using NINA.Image.ImageData;
using NINA.Core.Model;
using NINA.Core.Utility;
using PropertyChanged;
using CollectionViewSource = System.Windows.Data.CollectionViewSource;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Path = System.IO.Path;

namespace XisfRenamer;

#nullable disable

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[AddINotifyPropertyChangedInterface]
public partial class MainWindow : Window
{
    public ObservableCollection<FileItem> FileItems { get; set; }

    public ImagePatterns ImagePatterns { get; set; }

    public string FilePattern { get; set; } = "[N]";
    // public string FilePattern { get; set; } = "$$IMAGETYPE$$_$$DATETIME$$_$$FILTER$$_$$SENSORTEMP$$_$$EXPOSURETIME$$s_$$FRAMENR$$";

    public string ExamplePattern => ImagePatterns.GetImageFileString(FilePattern);

    public MainWindow()
    {
        InitializeComponent();
        FileItems = [];
        // Populate FileItems with your files here


        // ImagePatterns.Items[0].Key
        // MainLabel.Content = ImagePatterns.Items[0].Category;
        ImagePatterns = ImagePatterns.CreateExample();
        ImagePatterns.Add(new ImagePattern("[N]", "Original file name"));
        ImagePatterns.Add(new ImagePattern("[N3-]", "Original file name starting from 3rd character"));
        ImagePatterns.Add(new ImagePattern("[N2-5]", "Substring of 2-5 characters"));
        var groupedItems = from item in ImagePatterns.Items
            group item by item.Category
            into grouped
            select new { GroupName = grouped.Key, Items = grouped.ToList() };


        var groupedPatterns = new CollectionViewSource
        {
            Source = ImagePatterns.Items
        };
        groupedPatterns.GroupDescriptions.Add(new PropertyGroupDescription("Category"));

        // Set the GroupedPatterns as the ItemsSource for the ListView
        ImagePatternList.ItemsSource = groupedPatterns.View;


        DataContext = this;
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
    }

    private static string ApplyRenamePattern(string pattern, string fileName, string fileExtension)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName); // Extract name without extension

        // Replace [N] with the full filename (without extension)
        pattern = pattern.Replace("[N]", nameWithoutExtension);

        // Handle [N4], [N4-], [N4-8] patterns
        pattern = Regex.Replace(pattern, @"\[N(\d+)(-\d*)?\]", match =>
        {
            var startIndex = int.Parse(match.Groups[1].Value) - 1; // Start index (1-based, converted to 0-based)
            var endIndex = -1; // Default for [N4-]

            // Check if there's a range (e.g., [N4-8])
            if (match.Groups[2].Success && !string.IsNullOrEmpty(match.Groups[2].Value))
            {
                var endIndexStr = match.Groups[2].Value.Substring(1); // Extract end index (e.g., 8 in [N4-8])
                if (!string.IsNullOrEmpty(endIndexStr))
                {
                    endIndex = int.Parse(endIndexStr) - 1; // Convert to 0-based index
                }
            }

            // Handle out-of-bounds cases
            if (startIndex < 0 || startIndex >= nameWithoutExtension.Length)
            {
                return ""; // Return empty string if start index is out of bounds
            }

            // Extract substring based on the pattern
            if (endIndex == -1)
            {
                // [N3]: Single character at startIndex (1-based)
                if (!match.Value.Contains('-')) // Check if it's [N3] (no range)
                {
                    return nameWithoutExtension[startIndex].ToString();
                }

                // [N3-]: From startIndex to the end
                return nameWithoutExtension.Substring(startIndex);
            }
            else
            {
                // [N1-3]: From startIndex to endIndex
                if (endIndex >= nameWithoutExtension.Length)
                {
                    endIndex = nameWithoutExtension.Length - 1; // Adjust end index if out of bounds
                }

                var length = endIndex - startIndex + 1; // Include the endIndex character
                if (length <= 0)
                {
                    return ""; // Return empty string if the range is invalid
                }

                return nameWithoutExtension.Substring(startIndex, length);
            }
        });

        return pattern + fileExtension;
    }

    private void OnFilePatternChanged()
    {
        foreach (var fileItem in FileItems)
        {
            var f = CoreUtil.ReplaceInvalidFilenameChars(ApplyRenamePattern(FilePattern, fileItem.FileName, ".xisf")).Trim();
            fileItem.Renamed = fileItem.Meta.GetImagePatterns().GetImageFileString(f);
        }

        FileList.Items.Refresh();
    }

    private async void LoadFilesAsync(string directoryPath)
    {
        // string directoryPath = "e:\\astropics\\processing\\master\\";
        foreach (var filePath in Directory.GetFiles(directoryPath))
        {
            var fileName = Path.GetFileName(filePath);

            try
            {
                var s = await XisfUtils.Load(
                    new Uri(filePath),
                    new CancellationToken());

                FileItems.Add(new FileItem
                {
                    Meta = s,
                    IsChecked = false,
                    FileName = fileName,
                    Path = filePath,
                    Target = s.Target.Name,
                    ExposureStart = s.Image.ExposureStart,
                    ImageType = ImageType(s),
                    Filter = s.FilterWheel.Filter,
                    // Renamed = s.GetImagePatterns()
                    //     .GetImageFileString(
                    //         "$$DATEMINUS12$$\\$$IMAGETYPE$$\\$$DATETIME$$_$$FILTER$$_$$SENSORTEMP$$_$$EXPOSURETIME$$s_$$FRAMENR$$")
                });
                s.Image.ImageType = FileItems.Last().ImageType;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("error " + fileName);
            }
        }

        OnFilePatternChanged();

        string ImageType(ImageMetaData s)
        {
            try
            {
                return ((StringMetaDataHeader)s.GenericHeaders.Find(header => header.Key == "IMAGETYP")).Value.ToUpper()
                    .Replace("MASTER", "").Trim();
            }
            catch (Exception e)
            {
                return "";
            }
        }
    }

    private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        // Ensure that you are accessing the CollectionViewSource after the window is fully loaded
        var groupedPatterns = (CollectionViewSource)this.Resources["GroupedPatterns"];

        // Print out group details to verify
        foreach (var group in groupedPatterns.View.SourceCollection)
        {
            var collection = group as IEnumerable<object>;
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    Console.WriteLine(((ImagePattern)item).Key); // Print out key for verification
                }
            }
        }
    }

    private void ImagePatternList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ImagePatternList.SelectedItem is ImagePattern selectedItem)
        {
            // Get the current cursor position
            var cursorPosition = ImageFilePatternTextBox.SelectionStart;

            // Get the text before and after the cursor
            var textBeforeCursor = FilePattern.Substring(0, cursorPosition);
            var textAfterCursor = FilePattern.Substring(cursorPosition);

            // Insert the selected item's Key at the cursor position
            FilePattern = textBeforeCursor + selectedItem.Key + textAfterCursor;

            // Optionally, reset the cursor position to the end of the inserted text
            ImageFilePatternTextBox.SelectionStart = cursorPosition + selectedItem.Key.Length;
        }
    }

    private void FileList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FileList.SelectedItem is FileItem selectedItem)
        {
            selectedItem.IsChecked = !selectedItem.IsChecked;

            FileList.Items.Refresh();
        }
    }

    private void FileList_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            foreach (var selectedItem in FileList.SelectedItems.Cast<FileItem>())
            {
                selectedItem.IsChecked = !selectedItem.IsChecked;
            }

            FileList.Items.Refresh();

            e.Handled = true;
        }
    }

    private void BrowseFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        // Initialize FolderBrowserDialog
        var folderDialog = new FolderBrowserDialog();


        // Show the dialog and check if the user selected a folder
        if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            FileItems.Clear();
            // Display the selected folder path in the TextBox
            FolderPathTextBox.Text = folderDialog.SelectedPath;
            LoadFilesAsync(folderDialog.SelectedPath);
        }
    }

    private void RenameButton_OnClick(object sender, RoutedEventArgs e)
    {
        foreach (var fileItem in FileItems)
        {
            if (fileItem.IsChecked && !string.IsNullOrEmpty(fileItem.Renamed))
            {
                try
                {
                    string directory = Path.GetDirectoryName(fileItem.Path);
                    string destFileName = Path.Combine(directory, fileItem.Renamed);

                    // Skip if the new name is the same as the current name
                    if (fileItem.Path.Equals(destFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"Skipping rename (no change): {fileItem.FileName}");
                        continue;
                    }

                    // Handle filename conflicts by appending a counter (e.g., "_1", "_2")
                    if (File.Exists(destFileName))
                    {
                        string baseName = Path.GetFileNameWithoutExtension(fileItem.Renamed);
                        string extension = Path.GetExtension(fileItem.Renamed);
                        int counter = 1;
                        string newName;
                        do
                        {
                            newName = $"{baseName}_{counter}{extension}";
                            destFileName = Path.Combine(directory, newName);
                            counter++;
                        } while (File.Exists(destFileName));
                    }

                    // Perform the rename
                    File.Move(fileItem.Path, destFileName);

                    // Update the FileItem properties
                    fileItem.FileName = Path.GetFileName(destFileName);
                    fileItem.Path = destFileName;
                    fileItem.IsChecked = false; // Optional: Uncheck after rename

                    Console.WriteLine($"Renamed to: {fileItem.FileName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error renaming {fileItem.FileName}: {ex.Message}");
                }
            }
        }

        // Refresh UI and re-apply the pattern to reflect new filenames
        FileList.Items.Refresh();
        OnFilePatternChanged();
    }
}