using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace WatermarkApp;

internal static class WatermarkEngine
{
    public static Bitmap ApplyWatermark(Bitmap source, WatermarkMetadata metadata)
    {
        var output = new Bitmap(source.Width, source.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        using var graphics = Graphics.FromImage(output);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        graphics.DrawImage(source, 0, 0, source.Width, source.Height);

        var shortSide = Math.Min(source.Width, source.Height);
        var isTiny = shortSide < 300;

        DrawCornerText(graphics, source, metadata, shortSide, isTiny);

        if (!isTiny)
        {
            DrawPatternText(graphics, source, metadata, shortSide);
        }

        return output;
    }

    private static void DrawCornerText(Graphics g, Bitmap image, WatermarkMetadata metadata, int shortSide, bool isTiny)
    {
        var fontPx = isTiny ? Clamp(shortSide * 0.018f, 10f, 18f) : Clamp(shortSide * 0.022f, 12f, 36f);
        var opacity = isTiny ? 0.14f : 0.18f;
        var margin = Clamp(shortSide * 0.02f, 8f, 32f);
        var maxWidth = image.Width * 0.58f;

        var isDarkBackground = EstimateBottomRightLuminance(image) < 128.0f;
        var baseColor = isDarkBackground ? Color.White : Color.Black;
        var fallbackColor = isDarkBackground ? Color.Black : Color.White;
        var textColor = Color.FromArgb((int)(255 * opacity), baseColor);
        var backupColor = Color.FromArgb((int)(255 * (opacity * 0.58f)), fallbackColor);

        using var font = new Font("Segoe UI", fontPx, FontStyle.Regular, GraphicsUnit.Pixel);
        var line1 = metadata.HeaderText;
        var line2 = FitTextWithEllipsis(g, metadata.FileName, font, maxWidth);
        var line1Size = g.MeasureString(line1, font);
        var line2Size = g.MeasureString(line2, font);
        var lineGap = Math.Max(2f, fontPx * 0.20f);
        var blockWidth = Math.Max(line1Size.Width, line2Size.Width);
        var blockHeight = line1Size.Height + lineGap + line2Size.Height;

        var x = Math.Max(margin, image.Width - margin - blockWidth);
        var y = Math.Max(margin, image.Height - margin - blockHeight);
        DrawContrastedText(g, line1, font, x, y, textColor, backupColor);
        DrawContrastedText(g, line2, font, x, y + line1Size.Height + lineGap, textColor, backupColor);
    }

    private static void DrawPatternText(Graphics g, Bitmap image, WatermarkMetadata metadata, int shortSide)
    {
        var patternFontPx = Clamp(shortSide * 0.03f, 14f, 48f);
        var patternText = BuildPatternText(metadata);

        var darkOverlay = Color.FromArgb((int)(255 * 0.032f), Color.Black);
        var lightOverlay = Color.FromArgb((int)(255 * 0.032f), Color.White);

        using var font = new Font("Segoe UI", patternFontPx, FontStyle.Regular, GraphicsUnit.Pixel);
        var sampleSize = g.MeasureString(patternText, font);
        var xStep = Math.Max(60f, sampleSize.Width * 1.8f);
        var yStep = Math.Max(60f, sampleSize.Height * 2.2f);

        var state = g.Save();
        g.TranslateTransform(image.Width / 2f, image.Height / 2f);
        g.RotateTransform(-28f);
        g.TranslateTransform(-image.Width / 2f, -image.Height / 2f);

        using var darkBrush = new SolidBrush(darkOverlay);
        using var lightBrush = new SolidBrush(lightOverlay);
        for (var y = -image.Height; y < image.Height * 2; y += (int)yStep)
        {
            for (var x = -image.Width; x < image.Width * 2; x += (int)xStep)
            {
                g.DrawString(patternText, font, darkBrush, x, y);
                g.DrawString(patternText, font, lightBrush, x + 1f, y + 1f);
            }
        }

        g.Restore(state);
    }

    private static void DrawContrastedText(Graphics g, string text, Font font, float x, float y, Color mainColor, Color backupColor)
    {
        using var backupBrush = new SolidBrush(backupColor);
        using var mainBrush = new SolidBrush(mainColor);
        g.DrawString(text, font, backupBrush, x + 1f, y + 1f);
        g.DrawString(text, font, mainBrush, x, y);
    }

    private static string FitTextWithEllipsis(Graphics g, string text, Font font, float maxWidth)
    {
        if (g.MeasureString(text, font).Width <= maxWidth)
        {
            return text;
        }

        const string ellipsis = "...";
        var keepLeft = Math.Max(6, text.Length / 2 - 2);
        var keepRight = Math.Max(5, text.Length / 2 - 3);

        while (keepLeft > 2 && keepRight > 2)
        {
            var candidate = text[..keepLeft] + ellipsis + text[^keepRight..];
            if (g.MeasureString(candidate, font).Width <= maxWidth)
            {
                return candidate;
            }

            if (keepLeft >= keepRight)
            {
                keepLeft--;
            }
            else
            {
                keepRight--;
            }
        }

        return text.Length <= 12 ? text : text[..9] + ellipsis;
    }

    private static string BuildPatternText(WatermarkMetadata metadata)
    {
        var compactFile = metadata.FileName.Length <= 24 ? metadata.FileName : metadata.FileName[..21] + "...";
        return $"{metadata.CreationTime:yyyy-MM-dd HH:mm} | {metadata.UserName} | {compactFile}";
    }

    private static float EstimateBottomRightLuminance(Bitmap image)
    {
        var startX = (int)(image.Width * 0.70);
        var startY = (int)(image.Height * 0.70);
        return EstimateRegionLuminance(image, startX, startY, image.Width, image.Height, 8);
    }

    private static float EstimateRegionLuminance(Bitmap image, int left, int top, int right, int bottom, int stride)
    {
        var sum = 0.0;
        var count = 0;

        for (var y = top; y < bottom; y += stride)
        {
            for (var x = left; x < right; x += stride)
            {
                var px = image.GetPixel(x, y);
                var luma = px.R * 0.299 + px.G * 0.587 + px.B * 0.114;
                sum += luma;
                count++;
            }
        }

        return count == 0 ? 128f : (float)(sum / count);
    }

    private static float Clamp(float value, float min, float max)
    {
        if (value < min)
        {
            return min;
        }

        return value > max ? max : value;
    }
}
