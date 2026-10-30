using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ErrorManager;
using GRF.Core;
using GRF.FileFormats.ActFormat;
using GRF.Image;
using GRF.IO;
using GRFEditor.ApplicationConfiguration;
using GrfToWpfBridge;
using GrfToWpfBridge.ActRenderer;
using GrfToWpfBridge.Application;
using GrfToWpfBridge.DrawingComponents;
using GrfToWpfBridge.PreviewTabs;
using TokeiLibrary;
using Utilities.Extension;
using Utilities.Services;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewAct.xaml
	/// </summary>
	public partial class PreviewAct : FilePreviewTab {
		public class FrameRendererEditor : IFrameRendererEditor {
			public Act Act { get; set; }
			public int SelectedAction => IndexSelector.SelectedAction;
			public int SelectedFrame => IndexSelector.SelectedFrame;
			public IActIndexSelector IndexSelector { get; set; }
			public FrameRenderer FrameRenderer { get; set; }
			public Grid GridPrimary { get; set; }
			public event FrameRendererEventDelegates.ActEditorEventDelegate ActLoaded;
			public void OnActLoaded() => ActLoaded?.Invoke(Act);
			public bool IsLoading { get; set; }
			public int PreferedLoadingAction { get; set; }
			public Canvas Canvas => FrameRenderer.Canvas;
		}

		private FrameRendererEditor _editor = new FrameRendererEditor();
		private FrameRendererConfiguration _config;
		private PreviewCharacter _character;
		private string _animationPath;
		private Act _sourceAnimation;
		private Act _maleEquipment;
		private Act _femaleEquipment;

		public class DefaultDrawModule : IDrawingModule {
			private readonly Func<List<DrawingComponent>> _getComponents;
			public int DrawingPriority => 0;
			public List<DrawingComponent> GetComponents() => _getComponents() ?? new List<DrawingComponent>();
			public bool Permanent => false;

			public DefaultDrawModule(Func<List<DrawingComponent>> getComponents) {
				_getComponents = getComponents;
			}
		}

		public PreviewAct(AsyncOperation asyncOperation) {
			InitializeComponent();
			SettingsDialog.UIPanelPreviewBackgroundPick(_indexSelector._qcsBackground);
			_renderer.Canvas.Background = GrfEditorConfiguration.UIPanelPreviewBackground;

			_indexSelector._buttonExportAsGif.Click += _buttonExportAsGif_Click;
			
			IsVisibleChanged += delegate {
				if (IsVisible)
					_indexSelector.Play();
				else
					_indexSelector.Stop();
			};

			_initializeEditor();
			_bindSettings();

			WpfUtilities.AddMouseInOutUnderline(_cbGrid, _cbCharacter);
			ErrorPanel = _errorPanel;
		}

		private void _bindSettings() {
			_cbCharacterGender.SelectedIndex = Math.Max(0, Math.Min(2, GrfEditorConfiguration.PreviewActCharacterGender));
			_cbCharacterGender.SelectionChanged += delegate {
				GrfEditorConfiguration.PreviewActCharacterGender = _cbCharacterGender.SelectedIndex;
				_character = null;
				_updateCharacter();
			};
			Binder.Bind(_cbCharacter, () => GrfEditorConfiguration.PreviewActShowCharacter, v => GrfEditorConfiguration.PreviewActShowCharacter = v, _updateCharacter, true);
			Binder.Bind(_indexSelector._buttonScale, () => GrfEditorConfiguration.PreviewActScaleType, v => GrfEditorConfiguration.PreviewActScaleType = v, delegate {
				_indexSelector._buttonScale.IsPressed = GrfEditorConfiguration.PreviewActScaleType;
				_config.ActEditorScalingMode.Set(GrfEditorConfiguration.PreviewActScaleType ? BitmapScalingMode.HighQuality : BitmapScalingMode.NearestNeighbor);
			}, true);
			Binder.Bind(_cbGrid, () => GrfEditorConfiguration.PreviewActShowGrid, v => GrfEditorConfiguration.PreviewActShowGrid = v, delegate {
				var color = GrfEditorConfiguration.PreviewActShowGrid ? GrfColors.Black : GrfColors.Transparent;
				_config.ActEditorGridLineHorizontal.Set(color);
				_config.ActEditorGridLineVertical.Set(color);
				_renderer.Update();
			}, true);
		}

		private void _updateCharacter() {
			_cbCharacterGender.IsEnabled = _cbCharacter.IsChecked == true;
			bool female = _cbCharacterGender.SelectedIndex == 2 ||
				(_cbCharacterGender.SelectedIndex == 0 && PreviewCharacter.IsFemalePath(_animationPath));
			Act display = _sourceAnimation;
			if (_cbCharacter.IsChecked == true)
				display = (female ? _femaleEquipment ?? _maleEquipment : _maleEquipment ?? _femaleEquipment) ?? display;

			if (!ReferenceEquals(display, _editor.Act)) {
				int action = _editor.Act == null ? _editor.PreferedLoadingAction : _indexSelector.SelectedAction;
				_indexSelector.Stop();
				_editor.IsLoading = true;
				try {
					_editor.Act = display;
					_character = null;
					_editor.OnActLoaded();
					_editor.IndexSelector.Init(_editor, action, 0);
					if (display != null && IsVisible) _editor.IndexSelector.Play();
				}
				finally {
					_editor.IsLoading = false;
				}
			}

			if (_animationPath != null) {
				bool worn = !ReferenceEquals(display, _sourceAnimation);
				_labelHeader.Text = "Animation: " + System.IO.Path.GetFileName(_animationPath) + (worn ? " (worn preview)" : "");
				_labelHeader.ToolTip = worn ? "Worn sprite: " + display.LoadedPath : null;
			}

			if (_cbCharacter.IsChecked == true && _editor.Act != null && _character == null) {
				try {
					_character = new PreviewCharacter(_editor.Act, female);
				}
				catch (Exception err) {
					_cbCharacter.IsChecked = false;
					ErrorHandler.HandleException("Unable to load the reference character.", err);
				}
			}
			_renderer.Update();
		}

		private void _initializeEditor() {
			_editor.IndexSelector = _indexSelector;
			_editor.FrameRenderer = _renderer;
			_editor.GridPrimary = _gridPrimary;
			
			_indexSelector.Init(_editor, 0, 0);

			_config = new FrameRendererConfiguration(GrfEditorConfiguration.ConfigAsker);
			_renderer.RelativeCenter = new Point(0.5d, 0.6d);
			_renderer.Init(_editor, _config);

			_renderer.DrawingModules.Add(new DefaultDrawModule(delegate {
				if (_editor.Act != null) {
					var components = new List<DrawingComponent>();
					if (_cbCharacter.IsChecked == true && _character != null) {
						components.Add(new ActDraw(_character.Body));
						components.Add(new ActDraw(_character.Head));
					}
					components.Add(new ActDraw(_editor.Act));
					return components;
				}

				return new List<DrawingComponent>();
			}));
		}

		public Action<Brush> BackgroundBrushFunction => v => this.Dispatch(() => _renderer.Canvas.Background = v);

		protected override void _load(FileEntry entry) {
			try {
				_setupUI(entry);

				bool success = _tryLoadAct(entry, out byte[] actData, out byte[] sprData);
				if (!success) {
					_setAnimation(null);
					return;
				}

				if (_isCancelRequired()) return;

				var animation = new Act(actData, sprData);
				var male = _tryLoadEquipment(entry, false);
				var female = _tryLoadEquipment(entry, true);
				if (_isCancelRequired()) return;
				_setAnimation(animation, entry.RelativePath, male, female);
			}
			catch {
				_setAnimation(null);
				throw;
			}
		}

		private Act _tryLoadEquipment(FileEntry entry, bool female) {
			var equipped = PreviewCharacter.FindEquipment(_grfData, entry.RelativePath, female);
			if (equipped == null) return null;
			try {
				if (!_tryLoadAct(equipped, out byte[] actData, out byte[] sprData)) return null;
				var act = new Act(actData, sprData) { LoadedPath = equipped.RelativePath };
				act.Safe();
				return act;
			}
			catch {
				// A missing/incomplete wearable must not prevent viewing the original file.
				return null;
			}
		}

		private void _setAnimation(Act act, string path = null, Act male = null, Act female = null) {
			act?.Safe();

			this.Dispatch(delegate {
				_sourceAnimation = act;
				_maleEquipment = male;
				_femaleEquipment = female;
				_character = null;
				_animationPath = path;
				_updateCharacter();
			});
		}

		private bool _tryLoadAct(FileEntry entry, out byte[] actData, out byte[] sprData) {
			actData = entry.GetDecompressedData();
			sprData = null;

			var sprPath = entry.RelativePath.ReplaceExtension(".spr");
			var sprEntry = _grfData.FileTable.TryGet(sprPath);
			
			if (sprEntry == null) {
				// If a garment/wing, attempt to load the SPR from the parent directory
				if (entry.RelativePath.StartsWith(EncodingService.FromAnyToDisplayEncoding(@"data\sprite\·Îºê\"))) {
					var dirs = GrfPath.SplitDirectories(entry.RelativePath).ToList();
					dirs.RemoveAt(dirs.Count - 1);
					dirs.RemoveAt(dirs.Count - 1);
					dirs.Add(dirs.Last() + ".spr");

					sprEntry = _grfData.FileTable.TryGet(GrfPath.Combine(dirs.ToArray()));
				}
			}

			if (sprEntry == null) {
				throw new Exception("Could not find the corresponding SPR file: '" + sprPath + "'.");
			}

			sprData = sprEntry.GetDecompressedData();
			return true;
		}

		private void _setupUI(FileEntry entry) {
			this.Dispatch(delegate {
				_labelHeader.Dispatch(p => p.Text = "Animation: " + entry.DisplayRelativePath);
			});
		}

		private void _buttonExportAsGif_Click(object sender, RoutedEventArgs e) {
			try {
				var file = GrfToWpfBridge.Imaging.SaveTo(_editor.Act, _indexSelector.SelectedAction, _entry.RelativePath, PathRequest.ExtractSetting);

				if (file != null)
					Utilities.Debug.Ignore(() => Utilities.Services.OpeningService.FileOrFolder(file));
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}
