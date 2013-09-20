using System;
using System.Drawing;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using System.Diagnostics;

namespace Photon.Data.Sample
{
    class MainClass
    {
        static void Main(string[] args)
        {
            RecordSet ColumnStore = new RecordSet(new Type[]
                                                {
                typeof(int), typeof(int?), typeof(bool), typeof(string)
            });

            var record = new Record();
            ColumnStore.Add(record);
            var sw = new Stopwatch();

            record.Field<int>(0);
            record.Field<string>(0);
            record.Field<long>(0);
            sw.Start();
            for (var i=0; i<1000000; i++) {
                record.Field<int>(0);
                record.Field<string>(0);
                record.Field<long>(0);
            }
            Console.WriteLine(sw.ElapsedMilliseconds);
            NSApplication.Init();
            NSApplication.Main(args);
            
        }
    }
}	

