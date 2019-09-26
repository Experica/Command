using UnityEngine;
using System.Collections;
using System;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;

namespace Experica
{
    public class FrameRecorder
    {
        public RenderTexture rendertexture;
        public ConcurrentQueue<Tuple<string, byte[]>> frames = new ConcurrentQueue<Tuple<string, byte[]>>();
        public Rect ROI;
        public EnvironmentRecordMode EnvironmentRecordMode = EnvironmentRecordMode.None;
        public float RecordFrameRate = 25;
        int framecount = 0;
        bool grab = false;
        string recorddir = "";
        public string RecordDir
        {
            get { return recorddir; }
            set { if (!Directory.Exists(value)) { Directory.CreateDirectory(value); } recorddir = value; }
        }
        public string FramePrefix = "";
        double lastframetime = 0;
        Timer timer = new Timer();
        public RecordStatus RecordStatus = RecordStatus.Stopped;
        Thread savethread;


        public FrameRecorder(EnvironmentRecordMode recordmode = EnvironmentRecordMode.None)
        {
            EnvironmentRecordMode = recordmode;
            savethread = new Thread(_threadsave);
        }

        public Rect RenderTextureRect
        {
            get { return new Rect(0, 0, rendertexture.width, rendertexture.height); }
        }

        public void Grab()
        {
            if (!grab) { return; }

            var roiframe = new Texture2D(ROI.width.Convert<int>(), ROI.height.Convert<int>(), TextureFormat.RGB24, false, true);
            roiframe.ReadPixels(ROI, 0, 0, false);
            var framepath = Path.Combine(recorddir, FramePrefix+ framecount   + ".jpg");
            frames.Enqueue(new Tuple<string, byte[]>(framepath, roiframe.EncodeToJPG()));
            grab = false;
        }

        public void tick()
        {
            if (RecordStatus == RecordStatus.Stopped) { return; }

            var now = timer.ElapsedMillisecond;
            if (framecount == 0 || now - lastframetime >= 1000f / RecordFrameRate)
            {
                framecount++;
                lastframetime = now;
                grab = true;
            }
        }

        public void Reset()
        {
            grab = false;
            framecount = 0;
            timer.Restart();
            if (!savethread.IsAlive)
            {
                savethread.Start();
            }
        }

        void _threadsave()
        {
            while (true)
            {
                var l = frames.Count;
                if (l == 0) { Thread.Sleep(1000); continue; }
                Tuple<string, byte[]> frame;
                for (var i = 0; i < l; i++)
                {
                    if (frames.TryDequeue(out frame))
                    {
                        File.WriteAllBytes(frame.Item1, frame.Item2);
                    }
                }
            }
        }
    }
}