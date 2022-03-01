using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LenxifCore
{
    public interface ILenxif
    {
        public void UpdateManualLens(string path, bool autoProcess = true, string focalLength = null, string aperture = null, string brand = null);
    }
}
