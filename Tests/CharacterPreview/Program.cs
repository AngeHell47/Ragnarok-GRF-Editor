using System;
using System.IO;
using System.Linq;
using System.Reflection;
using GRF.Core;
using GRF.FileFormats.ActFormat;
using GRFEditor.WPF.PreviewTabs;

internal static class Program {
    [STAThread]
    private static int Main(string[] args) {
        try {
            using (var grf = new GrfHolder(args[0])) {
                if (args.Length > 2 && args[2] == "visual") { int code = VisualTests.Run(grf, args[1]); Environment.Exit(code); return code; }
                if (args.Length > 2 && args[2] == "inspect") {
                    foreach (var e in grf.FileTable.Entries.Where(e => e.RelativePath.IndexOf(args[3], StringComparison.OrdinalIgnoreCase) >= 0 && e.RelativePath.EndsWith(".act"))) {
                        var s = grf.FileTable.TryGet(Path.ChangeExtension(e.RelativePath, ".spr"));
                        if (s == null) continue;
                        var a = new Act((GRF.MultiType)e.GetDecompressedData(), (GRF.MultiType)s.GetDecompressedData());
                        Console.WriteLine(e.RelativePath + " | actions=" + a.NumberOfActions + " frames=" + a[0].NumberOfFrames + " anchor=" + String.Join(";", a[0, 0].Anchors.Select(x => x.OffsetX + "," + x.OffsetY)) + " layers=" + String.Join(";", a[0, 0].Layers.Select(x => x.OffsetX + "," + x.OffsetY)));
                    }
                    return 0;
                }
                var hats = grf.FileTable.Entries.Where(e => e.RelativePath.EndsWith(".act", StringComparison.OrdinalIgnoreCase) &&
                    (e.RelativePath.Contains("악세사리") || e.RelativePath.Contains("¾Ç¼¼»ç¸®") || e.RelativePath.IndexOf("redreindeer", StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
                Console.WriteLine("Hat entries: " + hats.Count);
                foreach (var entry in hats.Take(15)) Console.WriteLine(entry.RelativePath);
                Directory.CreateDirectory(args[1]);
                var assembly = typeof(PreviewAct).Assembly;
                Console.WriteLine("Resources: " + String.Join(",", assembly.GetManifestResourceNames().Where(n => n.Contains("CharacterPreview"))));
                var characterType = assembly.GetType("GRFEditor.WPF.PreviewTabs.PreviewCharacter", true);
                int frames = 0, tested = 0;
                foreach (var entry in hats.Take(20)) {
                    var spr = grf.FileTable.TryGet(Path.ChangeExtension(entry.RelativePath, ".spr"));
                    if (spr == null) continue;
                    var act = new Act((GRF.MultiType)entry.GetDecompressedData(), (GRF.MultiType)spr.GetDecompressedData());
                    act.Safe();
                    byte[] before = Serialize(act);
                    foreach (bool female in new[] { false, true }) {
                        var character = Activator.CreateInstance(characterType, new object[] { act, female });
                        foreach (string part in new[] { "Body", "Head" }) {
                            var reference = (Act)characterType.GetProperty(part).GetValue(character);
                            if (reference.NumberOfActions != act.NumberOfActions) throw new Exception("Action mismatch");
                            for (int ai = 0; ai < act.NumberOfActions; ai++) {
                                if (reference[ai].NumberOfFrames != act[ai].NumberOfFrames) throw new Exception("Frame mismatch");
                                for (int fi = 0; fi < reference[ai].NumberOfFrames; fi++) {
                                    var diff = Act.CalculateAnchorDiff(reference, ai, fi, null);
                                    foreach (var layer in reference[ai, fi]) {
                                        if (layer.SpriteIndex < 0) continue;
                                        int index = layer.GetAbsoluteSpriteId(reference.Sprite);
                                        if (index < 0 || index >= reference.Sprite.Images.Count) throw new Exception("Invalid reference sprite " + part + ": " + index + "/" + reference.Sprite.Images.Count);
                                    }
                                    frames++;
                                }
                            }
                        }
                    }
                    if (!before.SequenceEqual(Serialize(act))) throw new Exception("Preview changed original ACT");
                    if (tested == 0) {
                        File.WriteAllBytes(Path.Combine(args[1], "sample.act"), entry.GetDecompressedData());
                        File.WriteAllBytes(Path.Combine(args[1], "sample.spr"), spr.GetDecompressedData());
                        Console.WriteLine("Sample: " + entry.RelativePath);
                        Console.WriteLine("Sample anchor: " + String.Join(";", act[0, 0].Anchors.Select(a => a.OffsetX + "," + a.OffsetY)));
                        foreach (string part in new[] { "Body", "Head" }) {
                            var character = Activator.CreateInstance(characterType, new object[] { act, false });
                            var reference = (Act)characterType.GetProperty(part).GetValue(character);
                            Console.WriteLine(part + " anchor: " + String.Join(";", reference[0, 0].Anchors.Select(a => a.OffsetX + "," + a.OffsetY)));
                        }
                    }
                    tested++;
                }
                Console.WriteLine("PASS: " + tested + " real hats, both genders, " + frames + " reference frames; original ACTs unchanged.");
            }
            return 0;
        }
        catch (Exception ex) { Console.WriteLine(ex); return 1; }
    }

    private static byte[] Serialize(Act act) { using (var stream = new MemoryStream()) { act.Save(stream); return stream.ToArray(); } }
}
