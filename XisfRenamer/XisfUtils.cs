using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
using NINA.Core.Enum;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Image.FileFormat.XISF;
using NINA.Image.ImageData;

namespace XisfRenamer;

public static class XisfUtils
{
    private static readonly
#nullable disable
        byte[] xisfSignature = new byte[8]
        {
            88,
            73,
            83,
            70,
            48,
            49,
            48,
            48
        };

    public static async Task<ImageMetaData> Load(
        Uri filePath,
        CancellationToken ct)
    {
        return await Task.Run<ImageMetaData>((Func<ImageMetaData>)(() =>
        {
            using (var fileStream = new FileStream(filePath.LocalPath, FileMode.Open, FileAccess.Read))
            {
                var numArray1 = new byte[xisfSignature.Length];
                fileStream.Read(numArray1, 0, numArray1.Length);
                if (!numArray1.SequenceEqual(xisfSignature))
                {
                    Logger.Error("XISF: Opened file \"" + filePath.LocalPath + "\" is not a valid XISF file", nameof(Load),
                        "C:\\Projects\\nina\\NINA.Image\\FileFormat\\XISF\\XISF.cs", 63);
                    throw new InvalidDataException(Loc.Instance["LblXisfInvalidFile"]);
                }

                Logger.Debug("XISF: Opening file \"" + filePath.LocalPath + "\"", nameof(Load),
                    "C:\\Projects\\nina\\NINA.Image\\FileFormat\\XISF\\XISF.cs", 67);
                var buffer = new byte[4];
                fileStream.Read(buffer, 0, buffer.Length);
                var uint32 = BitConverter.ToUInt32(buffer, 0);
                fileStream.Seek(4L, SeekOrigin.Current);
                var numArray2 = new byte[(int)uint32];
                fileStream.Read(numArray2, 0, (int)uint32);
                var xisfHeader = new XISFHeader(XElement.Parse(Encoding.UTF8.GetString(numArray2)));
                var metaData = new ImageMetaData();
                try
                {
                    metaData = xisfHeader.ExtractMetaData();
                }
                catch (Exception ex)
                {
                    Logger.Error("XISF: Error during metadata extraction " + ex.Message, nameof(Load),
                        "C:\\Projects\\nina\\NINA.Image\\FileFormat\\XISF\\XISF.cs", 92);
                }

                int width;
                int height;
                try
                {
                    var strArray = xisfHeader.Image.Attribute((XName)"geometry").Value.Split(':');
                    width = int.Parse(strArray[0]);
                    height = int.Parse(strArray[1]);
                }
                catch (Exception ex)
                {
                    var interpolatedStringHandler = new DefaultInterpolatedStringHandler(37, 1);
                    interpolatedStringHandler.AppendLiteral("XISF: Could not find image geometry: ");
                    interpolatedStringHandler.AppendFormatted(ex);
                    Logger.Error(interpolatedStringHandler.ToStringAndClear(), nameof(Load),
                        "C:\\Projects\\nina\\NINA.Image\\FileFormat\\XISF\\XISF.cs", 106);
                    throw new InvalidDataException(Loc.Instance["LblXisfInvalidGeometry"]);
                }

                var interpolatedStringHandler1 = new DefaultInterpolatedStringHandler(36, 2);
                interpolatedStringHandler1.AppendLiteral("XISF: File geometry: width=");
                interpolatedStringHandler1.AppendFormatted(width);
                interpolatedStringHandler1.AppendLiteral(", height=");
                interpolatedStringHandler1.AppendFormatted(height);
                Logger.Debug(interpolatedStringHandler1.ToStringAndClear(), nameof(Load),
                    "C:\\Projects\\nina\\NINA.Image\\FileFormat\\XISF\\XISF.cs", 110);
                string sampleFormat;
                try
                {
                    sampleFormat = xisfHeader.Image.Attribute((XName)"sampleFormat").Value;
                }
                catch (InvalidDataException ex)
                {
                    var interpolatedStringHandler2 = new DefaultInterpolatedStringHandler(33, 1);
                    interpolatedStringHandler2.AppendLiteral("XISF: Could not read image data: ");
                    interpolatedStringHandler2.AppendFormatted(ex);
                    Logger.Error(interpolatedStringHandler2.ToStringAndClear(), nameof(Load),
                        "C:\\Projects\\nina\\NINA.Image\\FileFormat\\XISF\\XISF.cs", 119);
                    throw;
                }
                catch (Exception ex)
                {
                    var interpolatedStringHandler3 = new DefaultInterpolatedStringHandler(38, 1);
                    interpolatedStringHandler3.AppendLiteral("XISF: Could not find image data type: ");
                    interpolatedStringHandler3.AppendFormatted(ex);
                    Logger.Error(interpolatedStringHandler3.ToStringAndClear(), nameof(Load),
                        "C:\\Projects\\nina\\NINA.Image\\FileFormat\\XISF\\XISF.cs", 122);
                    throw new InvalidDataException("Could not find XISF image data type");
                }

                var cksumType = XISFChecksumTypeEnum.NONE;
                var empty = string.Empty;
                string[] strArray1 = null;
                try
                {
                    if (xisfHeader.Image.Attribute((XName)"checksum") != null)
                    {
                        strArray1 = xisfHeader.Image.Attribute((XName)"checksum").Value.ToLower().Split(':');
                        if (!string.IsNullOrEmpty(strArray1[0]))
                        {
                            //cksumType = XISF.GetChecksumType(strArray1[0]);
                            empty = strArray1[1];
                        }
                    }
                    else
                        Logger.Debug("XISF: Checksummed data block was not encountered", nameof(Load),
                            "C:\\Projects\\nina\\NINA.Image\\FileFormat\\XISF\\XISF.cs", 174);
                }
                catch (InvalidDataException ex)
                {
                    Logger.Error("XISF: Unknown checksum type: " + strArray1[0], nameof(Load),
                        "C:\\Projects\\nina\\NINA.Image\\FileFormat\\XISF\\XISF.cs", 177);
                    throw new InvalidDataException(string.Format(Loc.Instance["LblXisfUnsupportedChecksum"], strArray1[0]));
                }

                if (cksumType != XISFChecksumTypeEnum.NONE)
                {
                    var interpolatedStringHandler4 = new DefaultInterpolatedStringHandler(29, 2);
                    interpolatedStringHandler4.AppendLiteral("XISF: Checksum type: ");
                    interpolatedStringHandler4.AppendFormatted(cksumType);
                    interpolatedStringHandler4.AppendLiteral(", Hash: ");
                    interpolatedStringHandler4.AppendFormatted(empty);
                    Logger.Debug(interpolatedStringHandler4.ToStringAndClear(), nameof(Load),
                        "C:\\Projects\\nina\\NINA.Image\\FileFormat\\XISF\\XISF.cs", 182);
                }


                return metaData;
            }
        }), ct);
    }

    public static ImagePatterns GetImagePatterns(this ImageMetaData metadata)
    {
        var p = new ImagePatterns();

        p.Set(ImagePatternKeys.Filter, metadata.FilterWheel.Filter);
        p.Set(ImagePatternKeys.ExposureTime, metadata.Image.ExposureTime);
        p.Set(ImagePatternKeys.ApplicationStartDate, CoreUtil.ApplicationStartDate.ToString("yyyy-MM-dd"));
        p.Set(ImagePatternKeys.Date, metadata.Image.ExposureStart.ToLocalTime().ToString("yyyy-MM-dd"));

        // ExposureStart is initialized to DateTime.MinValue, and we cannot subtract time from that. Only evaluate
        // the $$DATEMINUS12$$ pattern if the time is at least 12 hours on from DateTime.MinValue.
        if (metadata.Image.ExposureStart > DateTime.MinValue.AddHours(12))
        {
            p.Set(ImagePatternKeys.DateMinus12, metadata.Image.ExposureStart.ToLocalTime().AddHours(-12).ToString("yyyy-MM-dd"));
        }

        p.Set(ImagePatternKeys.DateUtc, metadata.Image.ExposureStart.ToUniversalTime().ToString("yyyy-MM-dd"));
        p.Set(ImagePatternKeys.Time, metadata.Image.ExposureStart.ToLocalTime().ToString("HH-mm-ss"));
        p.Set(ImagePatternKeys.TimeUtc, metadata.Image.ExposureStart.ToUniversalTime().ToString("HH-mm-ss"));
        p.Set(ImagePatternKeys.DateTime, metadata.Image.ExposureStart.ToLocalTime().ToString("yyyy-MM-dd_HH-mm-ss"));
        p.Set(ImagePatternKeys.FrameNr, metadata.Image.ExposureNumber.ToString("0000"));
        p.Set(ImagePatternKeys.ImageType, metadata.Image.ImageType);
        p.Set(ImagePatternKeys.TargetName, metadata.Target.Name);

        if (metadata.Image.RecordedRMS != null)
        {
            p.Set(ImagePatternKeys.RMS, metadata.Image.RecordedRMS.Total);
            p.Set(ImagePatternKeys.RMSArcSec, metadata.Image.RecordedRMS.Total * metadata.Image.RecordedRMS.Scale);
            p.Set(ImagePatternKeys.PeakRA, metadata.Image.RecordedRMS.PeakRA);
            p.Set(ImagePatternKeys.PeakRAArcSec, metadata.Image.RecordedRMS.PeakRA * metadata.Image.RecordedRMS.Scale);
            p.Set(ImagePatternKeys.PeakDec, metadata.Image.RecordedRMS.PeakDec);
            p.Set(ImagePatternKeys.PeakDecArcSec, metadata.Image.RecordedRMS.PeakDec * metadata.Image.RecordedRMS.Scale);
        }

        if (metadata.Focuser.Position.HasValue)
        {
            p.Set(ImagePatternKeys.FocuserPosition, metadata.Focuser.Position.Value);
        }

        if (!double.IsNaN(metadata.Focuser.Temperature))
        {
            p.Set(ImagePatternKeys.FocuserTemp, metadata.Focuser.Temperature);
        }

        if (metadata.Camera.Binning == string.Empty)
        {
            p.Set(ImagePatternKeys.Binning, "1x1");
        }
        else
        {
            p.Set(ImagePatternKeys.Binning, metadata.Camera.Binning);
        }

        if (!double.IsNaN(metadata.Camera.Temperature))
        {
            p.Set(ImagePatternKeys.SensorTemp, metadata.Camera.Temperature);
        }

        if (!double.IsNaN(metadata.Camera.SetPoint))
        {
            p.Set(ImagePatternKeys.TemperatureSetPoint, metadata.Camera.SetPoint);
        }

        if (metadata.Camera.Gain >= 0)
        {
            p.Set(ImagePatternKeys.Gain, metadata.Camera.Gain);
        }

        if (metadata.Camera.Offset >= 0)
        {
            p.Set(ImagePatternKeys.Offset, metadata.Camera.Offset);
        }

        if (metadata.Camera.USBLimit >= 0)
        {
            p.Set(ImagePatternKeys.USBLimit, metadata.Camera.USBLimit);
        }


        if (!double.IsNaN(metadata.WeatherData.SkyQuality))
        {
            p.Set(ImagePatternKeys.SQM, metadata.WeatherData.SkyQuality);
        }

        if (!string.IsNullOrEmpty(metadata.Camera.ReadoutModeName))
        {
            p.Set(ImagePatternKeys.ReadoutMode, metadata.Camera.ReadoutModeName);
        }

        if (!string.IsNullOrEmpty(metadata.Camera.Name))
        {
            p.Set(ImagePatternKeys.Camera, metadata.Camera.Name);
        }

        if (!string.IsNullOrEmpty(metadata.Telescope.Name))
        {
            p.Set(ImagePatternKeys.Telescope, metadata.Telescope.Name);
        }

        if (!double.IsNaN(metadata.Rotator.MechanicalPosition))
        {
            p.Set(ImagePatternKeys.RotatorAngle, metadata.Rotator.MechanicalPosition);
        }


        p.Set(ImagePatternKeys.SequenceTitle, metadata.Sequence.Title);

        return p;
    }
}