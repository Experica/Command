using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VLab;

namespace Xippmex
{
    class Program
    {
        static void Main(string[] args)
        {
            VLTimer timer = new VLTimer();
            timer.Start();

            var rm = new RecordManager(RecordSystem.Ripple);
            List<double>[] dt;List<int>[] dv;

            rm.recorder.RecordPath = "C:\\Data\\xippmex";
            rm.recorder.RecordStatus = RecordStatus.recording;
            Console.WriteLine("Press \"q\" to quit ...");
            while(Console.ReadLine()!="q")
            {
                Console.WriteLine("Record Status: "+rm.recorder.RecordStatus);

                var start = timer.ElapsedMillisecond;
                var isd = rm.recorder.din(out dt, out dv);
                Console.WriteLine("Digital Input Retrieve Time: " + (timer.ElapsedMillisecond - start)+" ms");

                if(isd)
                {
                    for(var i=0;i<dt.Length;i++)
                    {
                        if(dt[i]!=null)
                        {
                            Console.WriteLine("Digital Input Channel_" + (i + 1) + " Time: ");
                            Console.WriteLine(string.Join(", ", dt[i].Select(t=>t.ToString()).ToArray()));
                            Console.WriteLine("Digital Input Channel_" + (i + 1) + " Value: ");
                            Console.WriteLine(string.Join(", ", dv[i].Select(v=>v.ToString()).ToArray()));
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No Digital Input In Any Channel.");
                }
            }
            rm.recorder.RecordStatus = RecordStatus.stopped;
            Console.ReadLine();
        }
    }
}
