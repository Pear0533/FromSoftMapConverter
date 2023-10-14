using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Pfim;
using SoulsFormats;
using WitchyFormats;
using ImageFormat = System.Drawing.Imaging.ImageFormat;
using MATBIN = SoulsFormats.MATBIN;
using TPF = WitchyFormats.TPF;

namespace DS3MapConverter;

public partial class DS3MapConverter : Form
{
    public DS3MapConverter()
    {
        InitializeComponent();
        CenterToScreen();
    }

    private static bool IsEldenRingMsb;
    private static string MapTpfsPath;
    private static string GameFolderPath;

    private async Task OpenMapToConvert()
    {
        OpenFileDialog dialog = new() { Filter = @"Map File (*.msb.dcx)|*.msb.dcx" };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        await Task.Run(async () => await ConvertMapToOBJ(dialog.FileName));
    }

    private static TPF ReadTPFFromBND(string bndFilePath)
    {
        if (!File.Exists(bndFilePath)) return null;
        BND4 bnd = BND4.Read(bndFilePath);
        BinderFile tpfBinderFile = bnd.Files.Find(i => i.Name.EndsWith(".tpf"));
        if (tpfBinderFile == null) return null;
        TPF tpf = TPF.Read(tpfBinderFile.Bytes);
        return tpf;
    }

    private static FLVER2 ReadFLVERFromBND(string bndFilePath)
    {
        if (!File.Exists(bndFilePath)) return null;
        BND4 bnd = BND4.Read(bndFilePath);
        BinderFile flverBinderFile = bnd.Files.Find(i => i.Name.EndsWith(".flver"));
        if (flverBinderFile == null) return null;
        FLVER2 flver = FLVER2.Read(flverBinderFile.Bytes);
        return flver;
    }

    private async Task ConvertMapToOBJ(string mapFilePath)
    {
        string mapStudioFolderPath = $"{Path.GetDirectoryName(mapFilePath)}";
        string mapFolderPath = $"{Path.GetDirectoryName(mapStudioFolderPath)}";
        GameFolderPath = $"{Path.GetDirectoryName(mapFolderPath)}";
        dynamic msb;
        string mapName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(mapFilePath));
        try
        {
            msb = MSBE.Read(mapFilePath);
            IsEldenRingMsb = true;
        }
        catch
        {
            try
            {
                msb = MSB3.Read(mapFilePath);
                IsEldenRingMsb = false;
            }
            catch
            {
                statusLabel.Invoke(() => statusLabel.Text = @"Selected file is not a map!");
                await Task.Delay(2000);
                return;
            }
        }
        MapTpfsPath = $"{mapFolderPath}\\{mapName?[..3]}";
        string mapBndsFolderPath;
        if (IsEldenRingMsb)
        {
            mapBndsFolderPath = $"{MapTpfsPath}\\{mapName}";
            MapTpfsPath = $"{GameFolderPath}\\asset\\aet";
        }
        else mapBndsFolderPath = $"{mapFolderPath}\\{mapName}";
        // string gameObjFolderPath = IsEldenRingMsb ? $"{GameFolderPath}\\asset\\aeg" : $"{GameFolderPath}\\obj";
        statusLabel.Invoke(() => statusLabel.Text = $@"Converting {mapName} to OBJ...");
        foreach (dynamic mapPiece in msb.Models.MapPieces)
        {
            string mapPieceFolderPath = $"{mapBndsFolderPath}\\map_pieces\\{mapPiece.Name}";
            string mapPieceObjFilePath = $"{mapPieceFolderPath}\\{mapPiece.Name}.obj";
            string mapPieceTexFolderPath = $"{mapPieceFolderPath}\\textures";
            string mapBndFilePath = $"{mapBndsFolderPath}\\{mapName}_{mapPiece.Name.TrimStart('m')}.mapbnd.dcx";
            FLVER2 mapPieceFlver = ReadFLVERFromBND(mapBndFilePath);
            if (mapPieceFlver == null) continue;
            ConvertFLVERToOBJ(mapPieceFlver, mapPieceObjFilePath);
            if (!IsEldenRingMsb) ExtractDS3FLVERMapPieceTextures(mapPieceFlver, mapPieceTexFolderPath);
        }
        // TODO: Implement support for ELDEN RING object exporting
        /*
        foreach (MSB3.Model.Object obj in msb.Models.Objects)
        {
            string objFolderPath = $"{mapBndsFolderPath}\\objects\\{obj.Name}";
            string objectObjFilePath = $"{objFolderPath}\\{obj.Name}.obj";
            string objTexFolderPath = $"{objFolderPath}\\textures";
            string objBndFilePath = $"{gameObjFolderPath}\\{obj.Name}.objbnd.dcx";
            FLVER2 objFlver = ReadFLVERFromBND(objBndFilePath);
            if (objFlver == null) continue;
            ConvertFLVERToOBJ(objFlver, objectObjFilePath);
            ExtractDS3FLVERObjectTextures(objBndFilePath, objTexFolderPath);
        }
        */
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

    private static void SaveDDS(Stream stream, string texFilePath)
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
        GCHandle handle = GCHandle.Alloc(image.Data, GCHandleType.Pinned);
        try
        {
            IntPtr data = Marshal.UnsafeAddrOfPinnedArrayElement(image.Data, 0);
            Bitmap bitmap = new(image.Width, image.Height, image.Stride, format, data);
            bitmap.Save(texFilePath, ImageFormat.Png);
        }
        finally
        {
            handle.Free();
        }
    }

    private static void ExportTPFTexture(TPF.Texture texture, string outputTexFolderPath)
    {
        string texFilePath = $"{outputTexFolderPath}\\{texture?.Name}.png";
        Directory.CreateDirectory(Path.GetDirectoryName(texFilePath) ?? "");
        SaveDDS(new MemoryStream(texture?.Bytes ?? Array.Empty<byte>()), texFilePath);
    }

    private static void ExtractDS3FLVERObjectTextures(string objBndFilePath, string outputTexFolderPath)
    {
        TPF tpf = ReadTPFFromBND(objBndFilePath);
        List<TPF.Texture> diffuseTextures = tpf.Textures.FindAll(i => i.Name.Contains("_a"));
        diffuseTextures.ForEach(i => ExportTPFTexture(i, outputTexFolderPath));
    }

    private static void ExtractDS3FLVERMapPieceTextures(FLVER2 flver, string outputTexFolderPath)
    {
        string[] tpfFilePaths = Directory.GetFiles(MapTpfsPath);
        foreach (string path in tpfFilePaths)
        {
            if (!path.EndsWith(".tpfbhd")) continue;
            string bdtFilePath = path.Replace("tpfbhd", "tpfbdt");
            BXF4 bxf4 = BXF4.Read(path, bdtFilePath);
            foreach (FLVER2.Material material in flver.Materials)
            {
                foreach (FLVER2.Texture texture in material.Textures)
                {
                    string flvTexName = Path.GetFileNameWithoutExtension(texture.Path) ?? "";
                    if (!flvTexName.Contains("_a")) continue;
                    BinderFile tpfFile = bxf4.Files.FirstOrDefault(i => i.Name.Contains(flvTexName));
                    if (tpfFile == null) continue;
                    TPF tpf = TPF.Read(tpfFile.Bytes);
                    tpf.Textures.ForEach(i => ExportTPFTexture(i, outputTexFolderPath));
                }
            }
        }
    }

    private static void ConvertFLVERToOBJ(FLVER2 flver, string outputObjFilePath)
    {
        OBJ obj = new();
        Matrix4x4[] boneMatrices = new Matrix4x4[flver.Bones.Count];
        for (int i = 0; i < flver.Bones.Count; i++)
        {
            FLVER.Bone bone = flver.Bones[i];
            Matrix4x4 global = Matrix4x4.Identity;
            if (bone.ParentIndex != -1)
                global = boneMatrices[bone.ParentIndex];
            boneMatrices[i] = bone.ComputeLocalTransform() * global;
        }
        int meshCount = 0;
        int currentFaceIndex = 0;
        string outputObjFolderPath = Path.GetDirectoryName(outputObjFilePath) ?? "";
        string outputTexFolderPath = $"{outputObjFolderPath}\\textures";
        foreach (FLVER2.Mesh flverMesh in flver.Meshes)
        {
            OBJ.Mesh mesh = new()
            {
                Indices = flverMesh.FaceSets.Find(x => x.Flags == FLVER2.FaceSet.FSFlags.None)?.Triangulate(false)
            };
            if (flverMesh.Vertices.Count == 0 || flverMesh.FaceSets.Count == 0 || mesh.Indices!.Count == 0 || mesh.Indices.All(x => x == mesh.Indices[0]))
                continue;
            mesh.Name = meshCount.ToString();
            meshCount++;
            mesh.MaterialName = mesh.Name;
            FLVER2.Material material = flver.Materials[flverMesh.MaterialIndex];
            FLVER2.Texture diffuse = IsEldenRingMsb ? material.Textures.Find(i => i.Type.Contains("AlbedoMap"))
                : material.Textures.Find(i => (Path.GetFileName(i.Path) ?? "").Contains("_a"));
            if (diffuse != null)
            {
                string diffuseTexName = $"{Path.GetFileNameWithoutExtension(diffuse.Path)}.png";
                if (IsEldenRingMsb)
                {
                    string matbinBndFilePath = $"{GameFolderPath}\\material\\allmaterial.matbinbnd.dcx";
                    BND4 matbinBnd = BND4.Read(matbinBndFilePath);
                    // TODO: Determine how to best utilize unused textures
                    BinderFile matbinFile = matbinBnd.Files.FirstOrDefault(i => i.Name.Contains(material.Name));
                    if (!MATBIN.IsRead(matbinFile?.Bytes, out MATBIN matbin)) continue;
                    MATBIN.Sampler diffuseTexSampler = matbin.Samplers.Find(i => i.Path.Contains("_a"));
                    if (diffuseTexSampler != null)
                    {
                        string tpfFileName = $"{Path.GetFileNameWithoutExtension(diffuseTexSampler.Path)?.Replace("_a", "").ToLower()}.tpf.dcx";
                        string[] tpfFilePaths = Directory.GetFiles(MapTpfsPath, "*.*", SearchOption.AllDirectories);
                        string tpfFilePath = tpfFilePaths.ToList().Find(i => i.Contains(tpfFileName));
                        if (!TPF.IsRead(tpfFilePath, out TPF tpf)) continue;
                        TPF.Texture diffuseTexture = tpf.Textures.Find(i => i.Name.Contains("_a"));
                        diffuseTexName = $"{diffuseTexture?.Name}.png";
                        ExportTPFTexture(diffuseTexture, outputTexFolderPath);
                        obj.AddNewMaterial(mesh.MaterialName, diffuseTexName);
                    }
                    else mesh.MaterialName = meshCount.ToString();
                }
            }
            for (int q = 0; q < mesh.Indices.Count; q++)
                mesh.Indices[q] += currentFaceIndex + 1;
            currentFaceIndex += flverMesh.Vertices.Count;
            foreach (FLVER.Vertex vert in flverMesh.Vertices)
                mesh.Vertices.Add(vert.Position);
            obj.Meshes.Add(mesh);
        }
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
            string mtlFileName = $"{Path.GetFileNameWithoutExtension(path)}.mtl";
            StringBuilder objSb = new();
            objSb.AppendLine($"mtllib {mtlFileName}");
            foreach (Mesh mesh in Meshes)
            {
                foreach (Vector3 v in mesh.Vertices.Select(vert => Vector3.Transform(vert, transform) * new Vector3(-1, 1, 1)))
                    objSb.AppendLine($"v  {v.X} {v.Y} {v.Z}");
                objSb.AppendLine($"g {mesh.Name}");
                objSb.AppendLine($"usemtl {mesh.MaterialName}");
                for (int i = 0; i < mesh.Indices!.Count - 2; i += 3)
                    objSb.AppendLine($"f {mesh.Indices[i]} {mesh.Indices[i + 1]} {mesh.Indices[i + 2]}");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "");
            File.WriteAllText(path, objSb.ToString());
            StringBuilder mtlSb = new();
            mtlSb.AppendLine(MTL);
            File.WriteAllText($"{Path.GetDirectoryName(path)}\\{mtlFileName}", MTL);
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
            public List<int> Indices { get; set; }
            public List<Vector3> Vertices { get; set; }
        }
    }
}