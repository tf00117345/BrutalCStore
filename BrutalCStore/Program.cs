// See https://aka.ms/new-console-template for more information

using System.Collections.Concurrent;
using System.Drawing;
using System.Text.Json;
using BrutalCStore;
using Dicom;
using Dicom.Imaging;
using Dicom.Network;
using DicomClient = Dicom.Network.Client.DicomClient;

string projectDirectory;
string dcmDirectory;
string tmpDirectory;
var dcmTagPairList = new List<DcmTagPair>();


void InitialEnvironment()
{
    projectDirectory = Environment.CurrentDirectory;
    dcmDirectory = Path.Combine(projectDirectory, "dcm", Guid.NewGuid().ToString());
    tmpDirectory = Path.Combine(projectDirectory, "temp");

    var exists = Directory.Exists(dcmDirectory);
    if (!exists) Directory.CreateDirectory(dcmDirectory);

    exists = Directory.Exists(tmpDirectory);
    if (!exists) Directory.CreateDirectory(tmpDirectory);

    using (StreamReader r = new StreamReader(projectDirectory + "/dcmTagValuePair.json"))
    {
        string json = r.ReadToEnd();
        dcmTagPairList = JsonSerializer.Deserialize<List<DcmTagPair>>(json);
    }
}

void CreateDicomFromBitmap(int idx)
{
    var fileName = $"{idx}.dcm";
    if (File.Exists(Path.Combine(tmpDirectory, fileName))) return;

    // 製作一個Bitmap，並且將其存成Dicom檔案
    int width = 1024;
    int height = 1024;
    int borderWidth = 20;

    Bitmap bitmap = new Bitmap(width, height);

    //使用Graphics類將圖像畫在Bitmap上
    using (Graphics g = Graphics.FromImage(bitmap))
    {
        g.Clear(Color.White); //設置背景為白色

        // 畫出邊界
        Pen borderPen = new Pen(Color.Red, borderWidth);
        g.DrawRectangle(borderPen, 0, 0, width - 1, height - 1); // 減去1是因為DrawRectangle的邊界會超出指定的範圍

        string text = $"Hello, DICOM {idx}";
        Font font = new Font("Arial", 64);

        SizeF textSize = g.MeasureString(text, font);
        float textX = (width - textSize.Width) / 2; // 計算文字的水平位置
        float textY = (height - textSize.Height) / 2; // 計算文字的垂直位置

        g.DrawString(text, font, Brushes.Red, new PointF(textX, textY)); // 在Bitmap上畫出置中的文字
    }

    //將Bitmap轉換為Byte數組
    var imageData = new byte[width * height];
    var imageDataIndex = 0;

    for (var y = 0; y < height; y++)
    {
        for (var x = 0; x < width; x++)
        {
            var color = bitmap.GetPixel(x, y);
            //此處使用簡單的灰度轉換，實際情況下可能需要更複雜的轉換
            var grayscale = (byte)(0.3 * color.R + 0.59 * color.G + 0.11 * color.B);
            imageData[imageDataIndex++] = grayscale;
        }
    }

    //創建DICOM圖像並將Byte數組添加為圖像像素
    var dicomImage = new DicomDataset
    {
        { DicomTag.PhotometricInterpretation, PhotometricInterpretation.Monochrome2.Value },
        { DicomTag.Rows, (ushort)height },
        { DicomTag.Columns, (ushort)width },
        { DicomTag.BitsAllocated, (ushort)8 },
        { DicomTag.BitsStored, (ushort)8 },
        { DicomTag.HighBit, (ushort)7 },
        { DicomTag.PixelRepresentation, (ushort)0 },
        { DicomTag.PixelData, imageData },
        //為DICOM圖像添加必要的元數據
        { DicomTag.PatientID, "P" },
        { DicomTag.PatientName, "Test^Patient" },
        { DicomTag.StudyID, "1" },
        { DicomTag.SeriesNumber, "1" },
        { DicomTag.SOPInstanceUID, DicomUID.Generate().UID },
        { DicomTag.InstanceNumber, idx.ToString() },
        { DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage }
    };

    //儲存DICOM文件
    var dicomFile = new DicomFile(dicomImage);
    dicomFile.Save(Path.Combine(tmpDirectory, fileName));
}

string GetDicom(string studyInstanceUid, string seriesInstanceUid, int interval, int idx, DateTime datetime)
{
    var fileName = $"{idx}.dcm";
    var filePath = Path.Combine(tmpDirectory, fileName);
    if (!File.Exists(fileName)) CreateDicomFromBitmap(idx);

    //創建DICOM圖像並將Byte數組添加為圖像像素
    var dicomFile = DicomFile.Open(filePath);

    //為DICOM圖像添加必要的元數據
    var sopInstanceUid = $"{seriesInstanceUid}.{idx}";
    var name = datetime.ToString("HHmmss") + interval.ToString("0000");
    dicomFile.Dataset.AddOrUpdate(DicomTag.PatientID, "P" + name);
    dicomFile.Dataset.AddOrUpdate(DicomTag.PatientName, "Test^" + name);
    dicomFile.Dataset.AddOrUpdate(DicomTag.StudyID, "1");
    dicomFile.Dataset.AddOrUpdate(DicomTag.SeriesNumber, "1");
    dicomFile.Dataset.AddOrUpdate(DicomTag.AccessionNumber, "A" + name);
    dicomFile.Dataset.AddOrUpdate(DicomTag.StudyDate, DateTime.Now.ToString("yyyyMMdd"));
    dicomFile.Dataset.AddOrUpdate(DicomTag.StudyTime, DateTime.Now.ToString("HHmmss"));
    dicomFile.Dataset.AddOrUpdate(DicomTag.StudyDescription, "StudyDescription");
    dicomFile.Dataset.AddOrUpdate(DicomTag.SeriesDate, DateTime.Now.ToString("yyyyMMdd"));
    dicomFile.Dataset.AddOrUpdate(DicomTag.SeriesTime, DateTime.Now.ToString("HHmmss"));
    dicomFile.Dataset.AddOrUpdate(DicomTag.SeriesDescription, "SeriesDescription");
    dicomFile.Dataset.AddOrUpdate(DicomTag.Modality, "SC");
    dicomFile.Dataset.AddOrUpdate(DicomTag.StudyInstanceUID, studyInstanceUid);
    dicomFile.Dataset.AddOrUpdate(DicomTag.SeriesInstanceUID, seriesInstanceUid);
    dicomFile.Dataset.AddOrUpdate(DicomTag.SOPInstanceUID, sopInstanceUid);
    dicomFile.Dataset.AddOrUpdate(DicomTag.InstanceNumber, idx.ToString());
    dicomFile.Dataset.AddOrUpdate(DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage);

    foreach (var dcnDcmTagPair in dcmTagPairList)
    {
        ushort group = Convert.ToUInt16(dcnDcmTagPair.Group, 16);
        ushort element = Convert.ToUInt16(dcnDcmTagPair.Element, 16);
        dicomFile.Dataset.AddOrUpdate(new DicomTag(group, element), dcnDcmTagPair.Value);
    }

    //儲存DICOM文件
    fileName = $"{sopInstanceUid}.dcm";
    dicomFile.Save(Path.Combine(dcmDirectory, fileName));
    return Path.Combine(dcmDirectory, fileName);
}


void SendDcm(string ip, int port, string callingAe, string calledAe, ConcurrentBag<string> dcmList)
{
    var client = new DicomClient(ip, port, false, callingAe, calledAe);
    client.NegotiateAsyncOps();

    foreach (var dcmPath in dcmList)
    {
        try
        {
            var request = new DicomCStoreRequest(dcmPath);
            request.OnResponseReceived += (req, response) =>
            {
                Console.WriteLine($"{dcmPath}, C-Store Response Received, Status:{response.Status}");
                File.Delete(dcmPath);
            };

            client.AddRequestAsync(request);
        }
        catch (Exception exception)
        {
            Console.WriteLine();
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine("Error storing file. Exception Details:");
            Console.WriteLine(exception.ToString());
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine();
        }
    }

    client.SendAsync();
}

InitialEnvironment();
StoreConfig? storeConfig;
using (var r = new StreamReader(projectDirectory + "/setting.json"))
{
    var json = r.ReadToEnd();
    storeConfig = JsonSerializer.Deserialize<StoreConfig>(json);
}

Console.WriteLine("***************************************************");
Console.WriteLine("Server AE Title: " + storeConfig.CalledAe);
Console.WriteLine("Server Host Address: " + storeConfig.IP);
Console.WriteLine("Server Port: " + storeConfig.Port);
Console.WriteLine("Client AE Title: " + storeConfig.CallingAe);
Console.WriteLine("CountOfDcm: " + storeConfig.CountOfDcm);
Console.WriteLine("Interval: " + storeConfig.Interval);
Console.WriteLine("***************************************************");

for (var i = 1; i <= storeConfig.Interval; i++)
{
    Console.WriteLine("Interval: " + i);

    var studyInstanceUid = DicomUID.Generate().UID;
    var seriesInstanceUid = $"{studyInstanceUid}.1";
    var datetime = DateTime.Now;

    var dcmList = new ConcurrentBag<string>();
    Parallel.For(1, storeConfig.CountOfDcm + 1,
        j => { dcmList.Add(GetDicom(studyInstanceUid, seriesInstanceUid, i, j, datetime)); });

    Console.WriteLine(dcmList.Count);
    SendDcm(storeConfig.IP, storeConfig.Port, storeConfig.CallingAe, storeConfig.CalledAe, dcmList);
}

Console.WriteLine("Finish");
Console.ReadLine();
Console.Read();