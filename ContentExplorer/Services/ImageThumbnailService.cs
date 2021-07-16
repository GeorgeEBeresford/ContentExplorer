using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace ContentExplorer.Services
{
    public class ImageThumbnailService : IThumbnailService
    {
        private const int MaximumThumbnailSize = 400;

        public void CreateThumbnail(FileInfo fileInfo)
        {
            FileInfo fileThumbnail = GetFileThumbnail(fileInfo);
            if (fileThumbnail.Exists)
            {
                return;
            }

            using (Image imageOnDisk = Image.FromFile(fileInfo.FullName))
            {
                Size thumbnailSize = GetThumbnailDimensions(imageOnDisk.Width, imageOnDisk.Height);
                using (Image thumbnailImage = new Bitmap(thumbnailSize.Width, thumbnailSize.Height))
                {
                    using (Graphics thumbnailGraphics = Graphics.FromImage(thumbnailImage))
                    {
                        thumbnailGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        thumbnailGraphics.SmoothingMode = SmoothingMode.HighQuality;
                        thumbnailGraphics.CompositingQuality = CompositingQuality.HighQuality;
                        thumbnailGraphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                        using (ImageAttributes imageAttributes = new ImageAttributes())
                        {
                            imageAttributes.SetWrapMode(WrapMode.TileFlipXY);
                            Rectangle newBounds = new Rectangle(new Point(0, 0), thumbnailSize);
                            thumbnailGraphics.DrawImage(
                                imageOnDisk,
                                newBounds,
                                0,
                                0,
                                imageOnDisk.Width,
                                imageOnDisk.Height,
                                GraphicsUnit.Pixel,
                                imageAttributes
                            );

                            EncoderParameters encoderParameters = new EncoderParameters(1);
                            encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 90L);
                            ImageCodecInfo imageCodecInfo = GetImageCodeInfo("image/jpeg");

                            thumbnailImage.Save(fileThumbnail.FullName, imageCodecInfo, encoderParameters);
                        }
                    }
                }
            }
        }

        public void DeleteThumbnailIfExists(FileInfo fileInfo)
        {
            FileInfo thumbnail = GetFileThumbnail(fileInfo);

            if (thumbnail.Exists)
            {
                thumbnail.Delete();
            }
        }

        public FileInfo GetDirectoryThumbnail(DirectoryInfo directory)
        {
            FileTypeService fileTypeService = new FileTypeService();

            FileInfo previewImage = directory
                .EnumerateFiles()
                .FirstOrDefault(file => fileTypeService.IsFileImage(file.Name));

            if (previewImage != null)
            {
                return previewImage;
            }

            IEnumerable<DirectoryInfo> subDirectories =
                directory.EnumerateDirectories("*", SearchOption.AllDirectories);

            FileInfo firstPreview = subDirectories
                .Select(GetDirectoryThumbnail)
                .FirstOrDefault(thumbnail => thumbnail != null);

            return firstPreview;
        }

        public FileInfo GetFileThumbnail(FileInfo fileInfo)
        {
            string thumbnailPath = $"{fileInfo.FullName}.thumb.jpg";
            FileInfo fileThumbnail = new FileInfo(thumbnailPath);

            return fileThumbnail;
        }

        public bool IsThumbnail(FileInfo fileInfo)
        {
            bool isThumbnail = fileInfo.Name.EndsWith(".thumb.jpg");
            return isThumbnail;
        }

        private Size GetThumbnailDimensions(int originalWidth, int originalHeight)
        {
            int newWidth;
            int newHeight;

            if (originalWidth > originalHeight)
            {
                newWidth = MaximumThumbnailSize;
                newHeight = Convert.ToInt32((double)originalHeight / originalWidth * MaximumThumbnailSize);
            }
            else
            {
                newHeight = MaximumThumbnailSize;
                newWidth = Convert.ToInt32((double)originalWidth / originalHeight * MaximumThumbnailSize);
            }

            return new Size(newWidth, newHeight);
        }

        private ImageCodecInfo GetImageCodeInfo(string mimeType)
        {
            ImageCodecInfo[] imageEncoders = ImageCodecInfo.GetImageEncoders();
            ImageCodecInfo matchingCodecInfo = imageEncoders.FirstOrDefault(potentialCodec =>

                potentialCodec.MimeType.Equals(mimeType, StringComparison.OrdinalIgnoreCase)
            );

            return matchingCodecInfo;
        }
    }
}