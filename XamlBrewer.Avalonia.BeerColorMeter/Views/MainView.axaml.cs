using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using SkiaSharp;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using XamlBrewer.Avalonia.BeerColorMeter.Models;

namespace XamlBrewer.Avalonia.BeerColorMeter.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private ICommand PickFileCommand => new AsyncRelayCommand(PickFileAsync);
    private ICommand CalculateCommand => new AsyncRelayCommand(CalculateColor);

    private async Task PickFileAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
        {
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = false });
            if (files != null && files.Any())
            {
                var file = files.First();
                await OpenFile(file);
            }
        }
    }

    private void PickImage_Click(object sender, RoutedEventArgs e)
    {
        PickFileCommand.Execute(null);
    }

    private void Calculate_Click(object sender, RoutedEventArgs e)
    {
        CalculateCommand.Execute(null);
    }

    private async Task OpenFile(IStorageFile file)
    {
        if (file != null)
        {
            await using var stream = await file.OpenReadAsync();
            FullImage.Source = new Bitmap(stream);
        }
    }

    private async Task CalculateColor()
    {
        if (FullImage.Source is not Bitmap bitmap)
        {
            return;
        }

        var stream = new MemoryStream();
        bitmap.Save(stream);
        var skb = SKBitmap.Decode(stream.ToArray());

        // Calculate average color
        byte[] sourcePixels = skb.Bytes;
        var nbrOfPixels = sourcePixels.Length / 4;
        int color1 = 0, color2 = 0, color3 = 0;
        for (int i = 0; i < sourcePixels.Length; i += 4)
        {
            color1 += sourcePixels[i];
            color2 += sourcePixels[i + 1];
            color3 += sourcePixels[i + 2];
        }

        Color color;
        if (skb.ColorType == SKColorType.Bgra8888)
        {
            color = Color.FromArgb(255, (byte)(color3 / nbrOfPixels), (byte)(color2 / nbrOfPixels), (byte)(color1 / nbrOfPixels));
        }
        else if (skb.ColorType == SKColorType.Rgba8888)
        {
            color = Color.FromArgb(255, (byte)(color1 / nbrOfPixels), (byte)(color2 / nbrOfPixels), (byte)(color3 / nbrOfPixels));
        }
        else
        {
            throw new Exception("Unsupported color type");
        }

        Result.Background = new SolidColorBrush(color);

        // Calculate nearest beer color
        double distance = int.MaxValue;
        BeerColor closest = DAL.BeerColors[0];
        foreach (var beerColor in DAL.BeerColors)
        {
            double d = Math.Sqrt(Math.Pow(beerColor.B - color.B, 2)
                               + Math.Pow(beerColor.G - color.G, 2)
                               + Math.Pow(beerColor.R - color.R, 2));
            if (d < distance)
            {
                distance = d;
                closest = beerColor;
            }
        }

        DisplayResult(closest);
    }

    private void BeerColorSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        var closest = DAL.BeerColors.Where(c => c.SRM >= e.NewValue).FirstOrDefault();
        if (closest != null)
        {
            DisplayResult(closest);
        }
    }

    private void DisplayResult(BeerColor closest)
    {
        ClosestBeerColor.Background = new SolidColorBrush(Color.FromArgb(255, closest.R, closest.G, closest.B));
        ClosestBeerColorText.Text = $"SRM: {(int)closest.SRM}{Environment.NewLine}EBC: {(int)closest.EBC}{Environment.NewLine}{Environment.NewLine}{closest.ColorName}";

        // Contrasting text color.
        if (closest.EBC < 12)
        {
            ClosestBeerColorText.Foreground = new SolidColorBrush(Colors.Maroon);
        }
        else
        {
            ClosestBeerColorText.Foreground = new SolidColorBrush(Colors.Beige);
        }

        BeerColorSlider.Value = closest.SRM;
    }
}
