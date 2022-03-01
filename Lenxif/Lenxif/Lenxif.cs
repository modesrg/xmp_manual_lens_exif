using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using XmpCore;
using XmpCore.Impl;
using XmpCore.Options;

namespace LenxifCore
{
    public class Lenxif : ILenxif
    {
        string focalLengthKey = @"exif:FocalLength";
        string fNumberKey = @"exif:FNumber";
        string lensKey = @"aux:Lens";
        string lensMakeKey = @"exif:LensMake";
        string apertureKey = @"exif:ApertureValue";
        string lensZeroValue = @"0.0 mm f/0.0";
        string tagsKey = @"lr:hierarchicalSubject";
        IXmpMeta template { get; set; }

        public Lenxif()
        {
            string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Template", "Template.xmp");

            if (File.Exists(templatePath))
            {
                this.template = ReadXMP(templatePath);
            }
            else
            {
                throw new Exception("Missing template file Template.xmp");
            }
        }

        public void UpdateManualLens(string path, bool autoProcess = true, string focalLength = null, string aperture = null, string brand = null)
        {
            //If its a directory the program will try to update all the XMP files that do not contain exif
            bool isDirectory = File.GetAttributes(path).HasFlag(FileAttributes.Directory);

            if (isDirectory)
            {
                var xmpFilePaths = Directory.GetFiles(path, "*.xmp");

                foreach (string xmpPath in xmpFilePaths)
                {
                    switch (autoProcess)
                    {
                        case true:
                            AutoUpdateManualLensInfo(xmpPath);
                            break;
                        case false:
                            ManualUpdateManualLensInfo(xmpPath, focalLength, aperture, brand);
                            break;
                    }
                }
            }
            else if (!isDirectory && Path.GetFileName(path).ToLower().EndsWith("*.xmp"))
            {
                switch (autoProcess)
                {
                    case true:
                        AutoUpdateManualLensInfo(path);
                        break;
                    case false:
                        ManualUpdateManualLensInfo(path, focalLength, aperture, brand);
                        break;
                }
            }

        }

        private void ManualUpdateManualLensInfo(string filePath, string focalLength, string aperture, string brand = null)
        {
            IXmpMeta xmp = ReadXMP(filePath);
            if (HasNoExif(xmp.Properties))
            {
                string newLensMaker = String.Empty;
                string newFocalLength = String.Empty;
                string newFNumber = String.Empty;
                string newLensModel = String.Format(@"{0} {1}mm f{2}", newLensMaker, newFocalLength, newFNumber).Trim();

                WriteNewExifData(filePath, focalLength, aperture, newLensModel, xmp);
            }
        }

        private void AutoUpdateManualLensInfo(string filePath)
        {
            IXmpMeta xmp = ReadXMP(filePath);

            if (HasNoExif(xmp.Properties))
            {
                string newLensMaker = String.Empty;
                string newFocalLength = String.Empty;
                string newFNumber = String.Empty;
                string newLensModel = String.Empty;

                //Parse the Aperture (f1.8, F2.8, F22, f4...) and the Focal Length (8mm, 15MM, 1200Mm) from the TAGs.
                IXmpPropertyInfo etiquetasProperty = xmp.Properties.Where(p => p.Path == tagsKey).FirstOrDefault();

                if (etiquetasProperty != null)
                {
                    int tagsNo = xmp.CountArrayItems(etiquetasProperty.Namespace, etiquetasProperty.Path);
                    Dictionary<string, string> lensInfoRelatedTags = new Dictionary<string, string>();

                    while (tagsNo > 0)
                    {
                        var arrayTag = xmp.GetArrayItem(etiquetasProperty.Namespace, etiquetasProperty.Path, tagsNo);

                        Regex regexFNumber = new Regex(@"([Ff]{1}[0-9]{0,1}[.]{1}[0-9]{1})|([Ff]{1}[0-9]{0,2})");
                        Regex regexFocalLength = new Regex(@"[0-9]{0,4}[Mm]{2}");

                        if (regexFNumber.IsMatch(arrayTag.Value) && !lensInfoRelatedTags.ContainsKey("FNumber"))
                        {
                            lensInfoRelatedTags.Add("FNumber", arrayTag.Value); //TODO: This can go
                            newFNumber = arrayTag.Value.ToLower().Replace("f", String.Empty);
                        }
                        if (regexFocalLength.IsMatch(arrayTag.Value) && !lensInfoRelatedTags.ContainsKey("FocalLength"))
                        {
                            lensInfoRelatedTags.Add("FocalLength", arrayTag.Value); //TODO: This can go
                            newFocalLength = arrayTag.Value.ToLower().Replace("mm", String.Empty);
                        }

                        tagsNo--;
                    }

                    //TODO: Parse LENS MAKER

                    if (!String.IsNullOrEmpty(newFocalLength) && !String.IsNullOrEmpty(newFNumber))
                    {
                        newLensModel = String.Format(@"{0} {1}mm f{2}", newLensMaker, newFocalLength, newFNumber).Trim();
                        WriteNewExifData(filePath, newFocalLength, newFNumber, newLensModel, xmp);
                    }
                }

            }
        }

        private void WriteNewExifData(string filePath, string newFocalLength, string newFNumber, string newLensModel, IXmpMeta xmp)
        {
            XmpCore.Impl.XmpUtils.AppendProperties(template, xmp, true, false, false);

            //Recuperamos las propiedades recien creadas para poderlas modificar
            IXmpPropertyInfo focalLength = xmp.Properties.Where(p => p.Path == focalLengthKey).FirstOrDefault();
            IXmpPropertyInfo aperture = xmp.Properties.Where(p => p.Path == apertureKey).FirstOrDefault();
            IXmpPropertyInfo fNumber = xmp.Properties.Where(p => p.Path == fNumberKey).FirstOrDefault();
            IXmpPropertyInfo lensModel = xmp.Properties.Where(p => p.Path == lensKey).FirstOrDefault();

            newFocalLength = (int.Parse(newFocalLength) * 1000000).ToString() + "/" + "1000000"; //This is how LR formats the f number
            newFNumber = (int.Parse(newFNumber) * 1000000).ToString() + "/" + "1000000"; //This is how LR formats the f number

            xmp.SetProperty(focalLength.Namespace, focalLengthKey, newFocalLength);
            xmp.SetProperty(aperture.Namespace, apertureKey, newFNumber);
            xmp.SetProperty(fNumber.Namespace, fNumberKey, newFNumber);
            xmp.SetProperty(lensModel.Namespace, lensKey, newLensModel);

            SaveModifiedXmp(filePath, xmp);
        }

        private static void SaveModifiedXmp(string filePath, IXmpMeta xmp)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                var serializer = new XmpSerializerRdf();
                serializer.Serialize(xmp, ms, new SerializeOptions());

                using (StreamWriter writer = new StreamWriter(ms))
                using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate))
                {
                    //You have to rewind the MemoryStream before copying
                    ms.Seek(0, SeekOrigin.Begin);
                    ms.CopyTo(fs);
                    fs.Flush();
                }
            }
        }


        private bool HasNoExif(IEnumerable<IXmpPropertyInfo> xmpProperties)
        {
            string focalLength = xmpProperties.Where(p => p.Path == focalLengthKey).Any() ? xmpProperties.FirstOrDefault(p => p.Path == focalLengthKey).Value : null;
            string aperture = xmpProperties.Where(p => p.Path == apertureKey).Any() ? xmpProperties.FirstOrDefault(p => p.Path == apertureKey).Value : null;
            string fNumber = xmpProperties.Where(p => p.Path == fNumberKey).Any() ? xmpProperties.FirstOrDefault(p => p.Path == fNumberKey).Value : null;
            string lensModel = xmpProperties.Where(p => p.Path == lensKey).Any() ? xmpProperties.FirstOrDefault(p => p.Path == lensKey).Value : null;

            bool hasNoExif = aperture == null && fNumber == null && aperture == null && lensModel == lensZeroValue;

            return hasNoExif;
        }

        private IXmpMeta ReadXMP(string filePath)
        {
            IXmpMeta xmp;

            string xmptxt = File.ReadAllText(filePath);

            xmp = XmpMetaFactory.ParseFromString(xmptxt);
            IEnumerable<IXmpPropertyInfo> xmpProperties = xmp.Properties;

            return xmp;
        }
    }
}
