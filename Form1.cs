using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Pfim;
using SoulsFormats;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

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

    private static FLVER2? ReadFLVERFromBND(string bndFilePath)
    {
        if (!File.Exists(bndFilePath)) return null;
        BND4 bnd = BND4.Read(bndFilePath);
        BinderFile? flverBinderFile = bnd.Files.Find(i => i.Name.EndsWith(".flver"));
        if (flverBinderFile == null) return null;
        FLVER2 flver = FLVER2.Read(flverBinderFile.Bytes);
        return flver;
    }

    private async Task ConvertMapToOBJ(string mapFilePath)
    {
        var mapStudioFolderPath = $@"{Path.GetDirectoryName(mapFilePath)}";
        var mapFolderPath = $@"{Path.GetDirectoryName(mapStudioFolderPath)}";
        var gameFolderPath = $@"{Path.GetDirectoryName(mapFolderPath)}";
        string mapName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(mapFilePath));
        var mapTpfsFolderPath = $"{mapFolderPath}\\{mapName[..3]}";
        var mapBndsFolderPath = $@"{mapFolderPath}\{mapName}";
        var gameObjFolderPath = $@"{gameFolderPath}\obj";
        statusLabel.Invoke(() => statusLabel.Text = @$"Converting {mapName} to OBJ...");
        MSB3 msb = MSB3.Read(mapFilePath);
        foreach (MSB3.Model.MapPiece mapPiece in msb.Models.MapPieces)
        {
            string mapPieceFolderPath = $@"{mapStudioFolderPath}\{mapName}\map_pieces\{mapPiece.Name}";
            var mapPieceObjFilePath = $@"{mapPieceFolderPath}\{mapPiece.Name}.obj";
            var mapPieceTexFolderPath = $@"{mapPieceFolderPath}\textures";
            string mapBndFilePath = $@"{mapBndsFolderPath}\{mapName}_{mapPiece.Name.TrimStart('m')}.mapbnd.dcx";
            FLVER2? mapPieceFlver = ReadFLVERFromBND(mapBndFilePath);
            if (mapPieceFlver == null) continue;
            ConvertFLVERToOBJ(mapPieceFlver, mapPieceObjFilePath);
            ExtractFLVERTextures(mapPieceFlver, mapTpfsFolderPath, mapPieceTexFolderPath);
        }
        foreach (MSB3.Model.Object obj in msb.Models.Objects)
        {
            string objFolderPath = $@"{mapStudioFolderPath}\{mapName}\objects\{obj.Name}";
            var objectObjFilePath = $@"{objFolderPath}\{obj.Name}.obj";
            var objTexFolderPath = $@"{objFolderPath}\textures";
            var objBndFilePath = $@"{gameObjFolderPath}\{obj.Name}.objbnd.dcx";
            FLVER2? objFlver = ReadFLVERFromBND(objBndFilePath);
            if (objFlver == null) continue;
            ConvertFLVERToOBJ(objFlver, objectObjFilePath);
            ExtractFLVERTextures(objFlver, mapTpfsFolderPath, objTexFolderPath);
        }
        statusLabel.Invoke(() => statusLabel.Text = @"Conversion complete!");
        await Task.Delay(2000);
        statusLabel.Invoke(() => statusLabel.Text = @"Waiting...");
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

    private static Bitmap ReadDDSAsBitmap(Stream stream)
    {
        IImage image = Pfim.Pfim.FromStream(stream);
        PixelFormat format;
        switch (image.Format)
        {
            case Pfim.ImageFormat.Rgb24:
                format = PixelFormat.Format24bppRgb;
                break;
            case Pfim.ImageFormat.Rgba32:
                format = PixelFormat.Format32bppArgb;
                break;
            case Pfim.ImageFormat.R5g5b5:
                format = PixelFormat.Format16bppRgb555;
                break;
            case Pfim.ImageFormat.R5g6b5:
                format = PixelFormat.Format16bppRgb565;
                break;
            case Pfim.ImageFormat.R5g5b5a1:
                format = PixelFormat.Format16bppArgb1555;
                break;
            case Pfim.ImageFormat.Rgb8:
                format = PixelFormat.Format8bppIndexed;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(image.Data, 0);
        var bitmap = new Bitmap(image.Width, image.Height, image.Stride, format, ptr);
        return bitmap;
    }

    private static void ExtractFLVERTextures(FLVER2 flver, string tpfsPath, string outputTexFolderPath)
    {
        string[] tpfFilePaths = Directory.GetFiles(tpfsPath);
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
                    if (!flvTexName.Contains("_a")) continue;
                    BinderFile? tpfFile = bxf4.Files.FirstOrDefault(i => i.Name.Contains(flvTexName));
                    if (tpfFile == null) continue;
                    TPF tpf = TPF.Read(tpfFile.Bytes);
                    var texFilePath = $@"{outputTexFolderPath}\{tpf.Textures[0].Name}.png";
                    Directory.CreateDirectory(Path.GetDirectoryName(texFilePath) ?? "");
                    Bitmap texBitMap = ReadDDSAsBitmap(new MemoryStream(tpf.Textures[0].Bytes));
                    texBitMap.Save(texFilePath, ImageFormat.Png);
                }
            }
        }
    }

    private static void ConvertFLVERToOBJ(FLVER2 flver, string outputObjFilePath)
    {
        var obj = new OBJ();
        var boneMatrices = new Matrix4x4[flver.Bones.Count];
        for (var i = 0; i < flver.Bones.Count; i++)
        {
            FLVER.Bone bone = flver.Bones[i];
            Matrix4x4 global = Matrix4x4.Identity;
            if (bone.ParentIndex != -1)
                global = boneMatrices[bone.ParentIndex];
            boneMatrices[i] = bone.ComputeLocalTransform() * global;
        }
        var meshCount = 0;
        var currentFaceIndex = 0;
        foreach (FLVER2.Mesh? flverMesh in flver.Meshes)
        {
            var mesh = new OBJ.Mesh
            {
                Indices = flverMesh.FaceSets.Find(x => x.Flags == FLVER2.FaceSet.FSFlags.None)?.Triangulate(false)
            };
            if (flverMesh.Vertices.Length == 0 || flverMesh.FaceSets.Count == 0 || mesh.Indices!.Count == 0 || mesh.Indices.All(x => x == mesh.Indices[0]))
                continue;
            mesh.Name = meshCount.ToString();
            meshCount++;
            mesh.MaterialName = mesh.Name;
            FLVER2.Material material = flver.Materials[flverMesh.MaterialIndex];
            FLVER2.Texture? diffuse = material.Textures.Find(i => Path.GetFileName(i.Path).Contains("_a"));
            if (diffuse != null) obj.AddNewMaterial(mesh.MaterialName, $"{Path.GetFileNameWithoutExtension(diffuse.Path)}.png");
            for (var q = 0; q < mesh.Indices.Count; q++)
                mesh.Indices[q] += currentFaceIndex + 1;
            currentFaceIndex += flverMesh.Vertices.Length;
            foreach (FLVER.Vertex vert in flverMesh.Vertices)
                mesh.Vertices.Add(vert.Position);
            obj.Meshes.Add(mesh);
        }
        string outputObjFolderPath = Path.GetDirectoryName(outputObjFilePath) ?? "";
        if (!Directory.Exists(outputObjFolderPath)) Directory.CreateDirectory(outputObjFolderPath);
        obj.Write(outputObjFilePath, Matrix4x4.Identity);
    }

    public class OBJ
    {
        internal OBJ()
        {
            MTL = "";
            Name = "";
            Meshes = new List<Mesh>();
        }

        public string Name { get; set; }
        public List<Mesh> Meshes { get; set; }
        public string MTL { get; set; }

        public void AddNewMaterial(string name, string diffuseTexName)
        {
            string newMaterialEntry = $"newmtl {name}\r\n"
                + "Ka 1.000000 1.000000 1.000000\r\n"
                + "Kd 0.800000 0.800000 0.800000\r\n"
                + "Ks 0.500000 0.500000 0.500000\r\n"
                + "Ns 200.000000\r\n"
                + "Ni 1.000000\r\n"
                + "d 1.000000\r\n"
                + "illum 2\r\n"
                + $"map_Kd textures\\\\{diffuseTexName}\r\n\r\n";
            MTL += newMaterialEntry;
        }

        public void Write(string path, Matrix4x4 transform)
        {
            var mtlFileName = $"{Path.GetFileNameWithoutExtension(path)}.mtl";
            var objSb = new StringBuilder();
            objSb.AppendLine($"mtllib {mtlFileName}");
            foreach (Mesh mesh in Meshes)
            {
                foreach (Vector3 v in mesh.Vertices.Select(vert => Vector3.Transform(vert, transform) * new Vector3(-1, 1, 1)))
                    objSb.AppendLine($"v  {v.X} {v.Y} {v.Z}");
                objSb.AppendLine($"g {mesh.Name}");
                objSb.AppendLine($"usemtl {mesh.MaterialName}");
                for (var i = 0; i < mesh.Indices!.Count - 2; i += 3)
                    objSb.AppendLine($"f {mesh.Indices[i]} {mesh.Indices[i + 1]} {mesh.Indices[i + 2]}");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "");
            File.WriteAllText(path, objSb.ToString());
            var mtlSb = new StringBuilder();
            mtlSb.AppendLine(MTL);
            File.WriteAllText($@"{Path.GetDirectoryName(path)}\{mtlFileName}", MTL);
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