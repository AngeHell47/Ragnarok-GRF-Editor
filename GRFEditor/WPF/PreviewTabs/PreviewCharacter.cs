using System;
using System.IO;
using System.Linq;
using GRF.Core;
using GRF.FileFormats.ActFormat;
using Utilities.Services;
using ActAction = GRF.FileFormats.ActFormat.Action;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Display-only references: the selected ACT and its SPR are never modified.
	/// References follow the selected animation's timeline and attachment anchors.
	/// </summary>
	internal sealed class PreviewCharacter {
		private static readonly Lazy<Act> _maleBody = new Lazy<Act>(() => _load("ref_body_m"));
		private static readonly Lazy<Act> _maleHead = new Lazy<Act>(() => _load("ref_head_m"));
		private static readonly Lazy<Act> _femaleBody = new Lazy<Act>(() => _load("ref_body_f"));
		private static readonly Lazy<Act> _femaleHead = new Lazy<Act>(() => _load("ref_head_f"));

		public Act Body { get; private set; }
		public Act Head { get; private set; }

		public PreviewCharacter(Act animation, bool female) {
			Body = _matchTimeline((female ? _femaleBody : _maleBody).Value, animation, "PreviewBody");
			Head = _matchTimeline((female ? _femaleHead : _maleHead).Value, animation, "PreviewHead");
		}

		public static bool IsFemalePath(string path) {
			if (String.IsNullOrEmpty(path)) return false;
			string female = EncodingService.FromAnyToDisplayEncoding("¿©");
			return path.Split('\\', '/').Any(part => part == "여" || part == "¿©" || part == female ||
				String.Equals(part, "female", StringComparison.OrdinalIgnoreCase));
		}

		public static FileEntry FindEquipment(GrfHolder grf, string path, bool female) {
			string itemRoot = EncodingService.FromAnyToDisplayEncoding(@"data\sprite\¾ÆÀÌÅÛ\");
			if (!path.StartsWith(itemRoot, StringComparison.OrdinalIgnoreCase) &&
				!path.StartsWith(@"data\sprite\아이템\", StringComparison.OrdinalIgnoreCase)) return null;

			string file = Path.GetFileNameWithoutExtension(path);
			var roots = new[] { EncodingService.FromAnyToDisplayEncoding(@"data\sprite\¾Ç¼¼»ç¸®\"), @"data\sprite\악세사리\" };
			var genders = new[] { EncodingService.FromAnyToDisplayEncoding(female ? "¿©" : "³²"), female ? "여" : "남" };
			foreach (string root in roots) {
				foreach (string gender in genders) {
					foreach (string name in new[] { gender + "_" + file, file }) {
						var entry = grf.FileTable.TryGet(root + gender + "\\" + name + ".act");
						if (entry != null && grf.FileTable.TryGet(Path.ChangeExtension(entry.RelativePath, ".spr")) != null)
							return entry;
					}
				}
			}
			return null;
		}

		private static Act _load(string name) {
			var act = new Act(_resource(name + ".act"), _resource(name + ".spr"));
			act.Safe();
			return act;
		}

		private static byte[] _resource(string name) {
			using (var stream = typeof(PreviewCharacter).Assembly.GetManifestResourceStream("GRFEditor.Resources.CharacterPreview." + name)) {
				if (stream == null) throw new FileNotFoundException("Missing character reference: " + name);
				using (var output = new MemoryStream()) {
					stream.CopyTo(output);
					return output.ToArray();
				}
			}
		}

		private static Act _matchTimeline(Act source, Act animation, string name) {
			// Share immutable reference images, but own all frame/layer data.
			// Custom reference names bypass Act Editor's three-frame remapping;
			// the timeline is already expanded to match the selected hat exactly.
			var result = new Act(source.Sprite) { Name = name, AnchoredTo = animation };
			for (int ai = 0; ai < animation.NumberOfActions; ai++) {
				var target = new ActAction { AnimationSpeed = animation[ai].AnimationSpeed };
				var reference = source.TryGetAction(ai) ?? source.TryGetAction(ai % 8);
				if (reference == null || reference.NumberOfFrames == 0)
					reference = source.Actions.FirstOrDefault(action => action.NumberOfFrames > 0);

				int count = animation[ai].NumberOfFrames;
				for (int fi = 0; fi < count; fi++) {
					if (reference == null) {
						target.Frames.Add(new Frame());
						continue;
					}
					// Idle and sitting hats may contain several effect frames for
					// each of the three head poses. Keep the matching pose throughout.
					int group = count / 3;
					int index = reference.NumberOfFrames == 3 && group > 0 && (ai < 8 || (ai >= 16 && ai < 24))
						? Math.Min(2, fi / group)
						: fi % reference.NumberOfFrames;
					target.Frames.Add(new Frame(reference[index]));
				}
				result.Actions.Add(target);
			}
			return result;
		}
	}
}
