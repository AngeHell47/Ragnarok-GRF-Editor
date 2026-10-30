using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GRF.Core;
using GRF.FileFormats.ActFormat;
using GRF.Image;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.WPF.PreviewTabs;
using GrfToWpfBridge;
using GrfToWpfBridge.Application;
using Utilities;

internal static class VisualTests {
    private static object Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); Console.WriteLine("PASS: " + message); }
    private static void Pump() { var frame = new DispatcherFrame(); Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new System.Action(() => frame.Continue = false)); Dispatcher.PushFrame(frame); }
    public static int Run(GrfHolder grf, string output) {
        Directory.CreateDirectory(output);
        string config = Path.Combine(output, "preview-test-config.txt");
        GrfEditorConfiguration.ConfigAsker = new ConfigAsker(config);
        GrfEditorConfiguration.PreviewActShowCharacter = true;
        GrfEditorConfiguration.PreviewActCharacterGender = 0;
        GrfEditorConfiguration.PreviewActShowGrid = true;
        TokeiLibrary.Configuration.ConfigAsker = GrfEditorConfiguration.ConfigAsker;
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        TokeiLibrary.ApplicationManager.RegisterAssembly(typeof(PreviewAct).Assembly);
        ImageConverterManager.AddConverter(new DefaultImageConverter());
        foreach (string resource in new[] { "GRFEditorStyles", "StyleLightBlue" })
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/GRF%20Editor;component/WPF/Styles/" + resource + ".xaml") });
        var preview = new PreviewAct(null) { Background = Brushes.White };
        var window = new Window { Content = preview, Width = 690, Height = 640, Title = "Vérification du placement des hats", ShowInTaskbar = false };
        window.Show();
        var entry = grf.FileTable.Entries.First(e => e.RelativePath.EndsWith("\\anya_wig_xmas.act", StringComparison.OrdinalIgnoreCase));
        var originalBytes = entry.GetDecompressedData();
        preview.Load(grf, entry);
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (Field(preview, "_sourceAnimation") == null && DateTime.UtcNow < deadline) { Pump(); Thread.Sleep(10); }
        var editor = (PreviewAct.FrameRendererEditor)Field(preview, "_editor");
        var checkbox = (CheckBox)preview.FindName("_cbCharacter");
        var gender = (ComboBox)preview.FindName("_cbCharacterGender");
        editor.IndexSelector.Stop(); Pump();
        Check(editor.Act != null && editor.Act.NumberOfActions == 104, "anya item resolves to wearable animation (104 actions)");
        Check(Field(preview, "_character") != null, "reference character is loaded by default");
        Check(editor.FrameRenderer.DrawingModules.SelectMany(m => m.GetComponents()).Count() == 3, "body + head + wearable are drawn");
        editor.IndexSelector.SelectedFrame = 0; editor.FrameRenderer.ZoomEngine.SetZoom(2); editor.FrameRenderer.Update(); Pump();
        Save(preview, Path.Combine(output, "anya-personnage.png"));
        for (int ai = 0; ai < editor.Act.NumberOfActions; ai++) {
            editor.IndexSelector.SelectedAction = ai;
            for (int fi = 0; fi < editor.Act[ai].NumberOfFrames; fi++) {
                editor.IndexSelector.SelectedFrame = fi;
                editor.FrameRenderer.Update(); Pump();
            }
        }
        Check(true, "all wearable actions/frames render successfully");
        gender.SelectedIndex = 2; editor.IndexSelector.Stop(); editor.IndexSelector.SelectedAction = 0; editor.IndexSelector.SelectedFrame = 0; Pump();
        Check(editor.Act.LoadedPath.Contains("¿©") || editor.Act.LoadedPath.Contains("여"), "female mode selects the female wearable");
        Save(preview, Path.Combine(output, "anya-feminin.png"));
        checkbox.IsChecked = false; editor.IndexSelector.Stop(); Pump();
        Check(editor.Act.NumberOfActions == 1, "hiding character restores the original item animation");
        Check(editor.FrameRenderer.DrawingModules.SelectMany(m => m.GetComponents()).Count() == 1, "hiding character removes both references");
        Check(!new ConfigAsker(config)["[GRFEditor - PreviewActShowCharacter]"].Equals("True"), "hidden preference is saved");
        Save(preview, Path.Combine(output, "anya-masque.png"));
        checkbox.IsChecked = true; editor.IndexSelector.Stop(); Pump();
        Check(editor.Act.NumberOfActions == 104, "showing character again restores wearable animation");
        Check(originalBytes.SequenceEqual(entry.GetDecompressedData()), "original GRF entry remains byte-for-byte unchanged");
        window.Close(); app.Shutdown();
        return 0;
    }
    private static void Save(FrameworkElement control, string path) {
        control.UpdateLayout(); Pump();
        var bitmap = new RenderTargetBitmap((int)control.ActualWidth, (int)control.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(control);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using (var output = File.Create(path)) encoder.Save(output);
    }
}
