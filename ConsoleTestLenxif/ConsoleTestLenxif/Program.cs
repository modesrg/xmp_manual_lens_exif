using System;

namespace ConsoleTestLenxif
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Lenxif.Lenxif lenxif = new Lenxif.Lenxif();
                string testPath = @"F:\RAW Laptop\2022\enero\02";
                lenxif.UpdateManualLens(testPath);
            }
            catch (Exception ex)
            {


            }
        }
    }
}
