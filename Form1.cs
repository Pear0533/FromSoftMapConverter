using System.Numerics;
using System.Text;
using SoulsFormats;

namespace DS3MapConverter;

public partial class DS3MapConverter : Form
{
    public DS3MapConverter()
    {
        InitializeComponent();
        CenterToScreen();
    }

    private async Task OpenMapToConvert()
    {
        var dialog = new OpenFileDialog { Filter = @"Map File (*.msb.dcx)|*.msb.dcx" };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        await Task.Run(async () => await ConvertMapToOBJ(dialog.FileName));
    }

    private async Task ConvertMapToOBJ(string mapFilePath)
    {
        var mapstudioFolder = $@"{Path.GetDirectoryName(mapFilePath)}";
        var mapFolder = $@"{Path.GetDirectoryName(mapstudioFolder)}";
        var gameFolder = $@"{Path.GetDirectoryName(mapFolder)}";
        string mapName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(mapFilePath));
        statusLabel.Invoke(() => statusLabel.Text = @$"Converting {mapName} to OBJ...");
        var mapTexFolder = $"{mapFolder}\\{mapName[..3]}";
        var modelFolder = $@"{mapFolder}\{mapName}";
        var objFolder = $@"{gameFolder}\obj";
        if (!MSB3.Is(mapFilePath))
        {
            Console.WriteLine($@"{Path.GetFileName(mapFilePath)} - is not msb3!");
            return;
        }
        if (!Directory.Exists(modelFolder))
            Console.WriteLine($@"{modelFolder} - folder doesn't exist!");
        if (!Directory.Exists(objFolder))
            Console.WriteLine($@"{objFolder} - folder doesn't exist!");
        Console.WriteLine(@"Exporting...");
        MSB3 msb = MSB3.Read(mapFilePath);
        var models = new List<OBJ>();
        foreach (MSB3.Model.MapPiece? model in msb.Models.MapPieces)
        {
            string objTexFolderPath = $@"{mapstudioFolder}\{mapName}\map_pieces\{model.Name}\textures";
            string modelPath = $@"{modelFolder}\{mapName}_{model.Name.TrimStart('m')}.mapbnd.dcx";
            if (!File.Exists(modelPath))
                continue;
            BND4 bnd = BND4.Read(modelPath);
            if (bnd.Files.Count == 0 || !FLVER2.Is(bnd.Files[0].Bytes))
                continue;
            FLVER2 flver = FLVER2.Read(bnd.Files[0].Bytes);
            OBJ obj = FlverToObj(flver);
            obj.Name = model.Name;
            models.Add(obj);
            // TODO: Assign all material texture references in the OBJ
            string[] tpfFilePaths = Directory.GetFiles(mapTexFolder);
            foreach (string path in tpfFilePaths)
            {
                if (!path.EndsWith(".tpfbhd")) continue;
                string bdtFilePath = path.Replace("tpfbhd", "tpfbdt");
                BXF4 bxf4 = BXF4.Read(path, bdtFilePath);
                foreach (FLVER2.Material material in flver.Materials)
                {
                    foreach (FLVER2.Texture texture in material.Textures)
                    {
                        string flvTexName = Path.GetFileNameWithoutExtension(texture.Path);
                        BinderFile? tpfFile = bxf4.Files.FirstOrDefault(i => i.Name.Contains(flvTexName));
                        if (tpfFile == null) continue;
                        TPF tpf = TPF.Read(tpfFile.Bytes);
                        var texFilePath = $@"{objTexFolderPath}\{tpf.Textures[0].Name}.dds";
                        Directory.CreateDirectory(Path.GetDirectoryName(texFilePath) ?? "");
                        await File.WriteAllBytesAsync(texFilePath, tpf.Textures[0].Bytes);
                    }
                }
            }
        }
        foreach (MSB3.Model.Object? model in msb.Models.Objects)
        {
            string objTexFolderPath = $@"{mapstudioFolder}\{mapName}\objects\{model.Name}\textures";
            var modelPath = $@"{objFolder}\{model.Name}.objbnd.dcx";
            if (!File.Exists(modelPath))
                continue;
            BND4 bnd = BND4.Read(modelPath);
            byte[]? flvBytes = bnd.Files.Find(x => x.Name.ToLower().EndsWith(".flver"))?.Bytes;
            if (flvBytes == null || !FLVER2.Is(flvBytes))
                continue;
            OBJ obj = FlverToObj(FLVER2.Read(flvBytes));
            obj.Name = model.Name;
            models.Add(obj);
            BinderFile? tpfFile = bnd.Files.FirstOrDefault(i => i.Name.EndsWith(".tpf"));
            if (tpfFile == null) continue;
            TPF tpf = TPF.Read(tpfFile.Bytes);
            foreach (TPF.Texture? texture in tpf.Textures)
            {
                if (texture == null) continue;
                var texFilePath = $@"{objTexFolderPath}\{texture.Name}.dds";
                Directory.CreateDirectory(Path.GetDirectoryName(texFilePath) ?? "");
                await File.WriteAllBytesAsync(texFilePath, texture.Bytes);
            }
        }
        foreach (MSB3.Part.MapPiece? part in msb.Parts.MapPieces)
        {
            OBJ? obj = models.Find(x => x.Name == part.ModelName);
            if (obj == null)
                continue;
            Matrix4x4 matrix = Matrix4x4.Identity;
            matrix = matrix
                * Matrix4x4.CreateScale(part.Scale)
                * Matrix4x4.CreateRotationX((float)(part.Rotation.X * Math.PI / 180f))
                * Matrix4x4.CreateRotationZ((float)(part.Rotation.Z * Math.PI / 180f))
                * Matrix4x4.CreateRotationY((float)(part.Rotation.Y * Math.PI / 180f))
                * Matrix4x4.CreateTranslation(part.Position);
            obj.Write($@"{mapstudioFolder}\{mapName}\map_pieces\{part.ModelName}\{part.Name}.obj", matrix);
        }
        foreach (MSB3.Part.Object? part in msb.Parts.Objects)
        {
            OBJ? obj = models.Find(x => x.Name == part.ModelName);
            if (obj == null)
                continue;
            Matrix4x4 matrix = Matrix4x4.Identity;
            matrix = matrix
                * Matrix4x4.CreateScale(part.Scale)
                * Matrix4x4.CreateRotationX((float)(part.Rotation.X * Math.PI / 180f))
                * Matrix4x4.CreateRotationZ((float)(part.Rotation.Z * Math.PI / 180f))
                * Matrix4x4.CreateRotationY((float)(part.Rotation.Y * Math.PI / 180f))
                * Matrix4x4.CreateTranslation(part.Position);
            obj.Write($@"{mapstudioFolder}\{mapName}\objects\{part.ModelName}\{part.Name}.obj", matrix);
        }
        statusLabel.Invoke(() => statusLabel.Text = @"Conversion complete!");
        await Task.Delay(2000);
        statusLabel.Invoke(() => statusLabel.Text = @"Waiting...");
    }

    public static OBJ FlverToObj(FLVER2 flv)
    {
        var obj = new OBJ();
        var boneMatrices = new Matrix4x4[flv.Bones.Count];
        for (var i = 0; i < flv.Bones.Count; i++)
        {
            FLVER.Bone bone = flv.Bones[i];
            Matrix4x4 global = Matrix4x4.Identity;
            if (bone.ParentIndex != -1)
                global = boneMatrices[bone.ParentIndex];
            boneMatrices[i] = bone.ComputeLocalTransform() * global;
        }
        var meshCount = 0;
        var currentFaceIndex = 0;
        foreach (FLVER2.Mesh? fmesh in flv.Meshes)
        {
            var mesh = new OBJ.Mesh
            {
                Indices = fmesh.FaceSets.Find(x => x.Flags == FLVER2.FaceSet.FSFlags.None)?.Triangulate(false)
            };
            if (fmesh.Vertices.Length == 0 || fmesh.FaceSets.Count == 0 || mesh.Indices!.Count == 0 || mesh.Indices.All(x => x == mesh.Indices[0]))
                continue;
            mesh.Name = meshCount.ToString();
            meshCount++;
            mesh.MaterialName = mesh.Name;
            for (var q = 0; q < mesh.Indices.Count; q++)
                mesh.Indices[q] += currentFaceIndex + 1;
            currentFaceIndex += fmesh.Vertices.Length;
            foreach (FLVER.Vertex vert in fmesh.Vertices)
                mesh.Vertices.Add(vert.Position);
            obj.Meshes.Add(mesh);
        }
        return obj;
    }

    private async void DS3MapConverter_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Modifiers == Keys.Control && e.KeyCode == Keys.O)
            await OpenMapToConvert();
    }

    private async void OpenCtrlOToolStripMenuItem_Click(object sender, EventArgs e)
    {
        await OpenMapToConvert();
    }

    public class OBJ
    {
        internal OBJ()
        {
            Name = "";
            Meshes = new List<Mesh>();
        }

        public string Name { get; set; }
        public List<Mesh> Meshes { get; set; }

        public void Write(string path, Matrix4x4 transform)
        {
            var sb = new StringBuilder();
            foreach (Mesh mesh in Meshes)
            {
                foreach (Vector3 v in mesh.Vertices.Select(vert => Vector3.Transform(vert, transform) * new Vector3(-1, 1, 1)))
                    sb.AppendLine($"v  {v.X} {v.Y} {v.Z}");
                sb.AppendLine($"g {mesh.Name}");
                sb.AppendLine($"usemtl {mesh.MaterialName}");
                for (var i = 0; i < mesh.Indices!.Count - 2; i += 3)
                    sb.AppendLine($"f {mesh.Indices[i]} {mesh.Indices[i + 1]} {mesh.Indices[i + 2]}");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, sb.ToString());
        }

        public class Mesh
        {
            internal Mesh()
            {
                Name = "";
                MaterialName = "";
                Indices = new List<int>();
                Vertices = new List<Vector3>();
            }

            public string Name { get; set; }
            public string MaterialName { get; set; }
            public List<int>? Indices { get; set; }
            public List<Vector3> Vertices { get; set; }
        }
    }
}