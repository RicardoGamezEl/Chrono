using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Graphics;

namespace Chrono.Services;

public class NotificationService : INotificationService
{
    public event Action<string, string>? NotificationReceived;

    public void ShowToast(string title, string message, int durationSeconds = 4)
    {
        NotificationReceived?.Invoke(title, message);

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var page = Shell.Current?.CurrentPage as ContentPage;
                if (page == null)
                    return;

                // Ensure toast host container grid
                if (page.Content is not Grid hostGrid || hostGrid.StyleId != "ChronoToastHostGrid")
                {
                    var originalContent = page.Content;
                    if (originalContent == null)
                        return;

                    page.Content = null;
                    hostGrid = new Grid
                    {
                        StyleId = "ChronoToastHostGrid"
                    };
                    hostGrid.Children.Add(originalContent);
                    page.Content = hostGrid;
                }

                // Build toast view
                var isWindows = DeviceInfo.Platform == DevicePlatform.WinUI;

                var toast = new Border
                {
                    BackgroundColor = Color.FromArgb("#161426"),
                    Stroke = Color.FromArgb("#7C4DFF"),
                    StrokeThickness = 1.5,
                    StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) },
                    Padding = new Thickness(14, 12),
                    Shadow = new Shadow
                    {
                        Brush = new SolidColorBrush(Color.FromArgb("#512BD4")),
                        Offset = new Point(0, 4),
                        Radius = 12,
                        Opacity = 0.45f
                    },
                    Opacity = 0,
                    TranslationY = 30
                };

                if (isWindows)
                {
                    toast.HorizontalOptions = LayoutOptions.End;
                    toast.VerticalOptions = LayoutOptions.End;
                    toast.Margin = new Thickness(0, 0, 24, 24);
                    toast.WidthRequest = 360;
                }
                else
                {
                    toast.HorizontalOptions = LayoutOptions.Fill;
                    toast.VerticalOptions = LayoutOptions.End;
                    toast.Margin = new Thickness(16, 0, 16, 24);
                }

                var contentGrid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection
                    {
                        new ColumnDefinition(GridLength.Auto),
                        new ColumnDefinition(GridLength.Star),
                        new ColumnDefinition(GridLength.Auto)
                    },
                    ColumnSpacing = 10,
                    VerticalOptions = LayoutOptions.Center
                };

                // Icon badge
                var iconBorder = new Border
                {
                    WidthRequest = 34,
                    HeightRequest = 34,
                    StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(17) },
                    BackgroundColor = Color.FromArgb("#2B1C52"),
                    Stroke = Color.FromArgb("#7C4DFF"),
                    StrokeThickness = 1,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    Content = new Label
                    {
                        Text = "⏱",
                        FontSize = 16,
                        TextColor = Colors.White,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    }
                };
                Grid.SetColumn(iconBorder, 0);
                contentGrid.Children.Add(iconBorder);

                // Text layout
                var textLayout = new VerticalStackLayout
                {
                    Spacing = 2,
                    VerticalOptions = LayoutOptions.Center
                };

                var titleLabel = new Label
                {
                    Text = title,
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White
                };

                var messageLabel = new Label
                {
                    Text = message,
                    FontSize = 12,
                    TextColor = Color.FromArgb("#D0CDDE"),
                    LineBreakMode = LineBreakMode.WordWrap
                };

                textLayout.Children.Add(titleLabel);
                textLayout.Children.Add(messageLabel);
                Grid.SetColumn(textLayout, 1);
                contentGrid.Children.Add(textLayout);

                // Close button
                var closeButton = new Button
                {
                    Text = "✕",
                    FontSize = 12,
                    TextColor = Color.FromArgb("#A09CB5"),
                    BackgroundColor = Colors.Transparent,
                    Padding = new Thickness(0),
                    WidthRequest = 28,
                    HeightRequest = 28,
                    VerticalOptions = LayoutOptions.Center
                };
                Grid.SetColumn(closeButton, 2);
                contentGrid.Children.Add(closeButton);

                toast.Content = contentGrid;

                // Add to page
                hostGrid.Children.Add(toast);

                // Dismiss logic
                var isDismissed = false;
                async Task DismissToastAsync()
                {
                    if (isDismissed) return;
                    isDismissed = true;
                    try
                    {
                        await Task.WhenAll(
                            toast.FadeToAsync(0, 250),
                            toast.TranslateToAsync(0, 30, 250, Easing.CubicIn)
                        );
                    }
                    catch
                    {
                        // Ignore cancellation if page navigated
                    }
                    finally
                    {
                        hostGrid.Children.Remove(toast);
                    }
                }

                closeButton.Clicked += async (s, e) => await DismissToastAsync();

                // Tap to dismiss
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) => await DismissToastAsync();
                toast.GestureRecognizers.Add(tapGesture);

                // Animate entry
                await Task.WhenAll(
                    toast.FadeToAsync(1, 250),
                    toast.TranslateToAsync(0, 0, 250, Easing.CubicOut)
                );

                // Auto dismiss delay
                await Task.Delay(TimeSpan.FromSeconds(durationSeconds));
                await DismissToastAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing toast: {ex.Message}");
            }
        });
    }
}
